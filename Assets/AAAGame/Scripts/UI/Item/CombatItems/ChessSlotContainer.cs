using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 棋子容器：管理棋子卡的扇形排列和进场/重排动效
/// （参考 CardSlotContainer 的实现，去掉拖拽让位逻辑）
/// </summary>
public class ChessSlotContainer : MonoBehaviour
{
    #region 参数配置

    [Header("扇形排列")]
    [SerializeField] private float m_FanRadius = 800f;           // 圆弧半径
    [SerializeField] private float m_MaxFanAngle = 50f;          // 最大展开角度（度）
    [SerializeField] private Vector2 m_CircleCenter = Vector2.zero;  // 圆心偏移
    [SerializeField] private float m_CircleCenterOffsetY = -100f;    // 圆心在容器底部偏下距离

    [Header("动效时长")]
    [SerializeField] private float m_EnterDuration = 0.35f;      // 进场动画时长
    [SerializeField] private float m_CardDealInterval = 0.1f;    // 棋子发牌间隔时间（秒）
    [SerializeField] private float m_RearrangeDuration = 0.25f;  // 补位动画时长
    [SerializeField] private Ease m_RearrangeEase = Ease.OutCubic;

    // 缓存旧参数，用于检测参数变化
    private float m_CachedFanRadius;
    private float m_CachedMaxFanAngle;
    private Vector2 m_CachedCircleCenter;
    private float m_CachedCircleCenterOffsetY;

    #endregion

    #region 字段

    private RectTransform m_RectTransform;
    private List<ChessItemUI> m_Cards = new List<ChessItemUI>();

    // 动效控制
    private Tween m_RearrangeTween;

    #endregion

    #region 生命周期

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
        if (m_RectTransform == null)
        {
            DebugEx.ErrorModule("ChessSlotContainer", "缺少 RectTransform 组件");
        }

        // 初始化缓存参数
        CacheParameters();
    }

    private void Update()
    {
        // 仅在运行时检测参数变化（用于实时调参）
        if (!Application.isPlaying)
            return;

        // 检测参数是否有变化
        if (HasParametersChanged())
        {
            DebugEx.LogModule("ChessSlotContainer", "检测到参数变化，立即更新棋子位置");
            CacheParameters();
            // 立即更新位置（不播放动画，实时反馈）
            RefreshCardPositionsImmediate();
        }
    }

    /// <summary>
    /// 缓存当前参数
    /// </summary>
    private void CacheParameters()
    {
        m_CachedFanRadius = m_FanRadius;
        m_CachedMaxFanAngle = m_MaxFanAngle;
        m_CachedCircleCenter = m_CircleCenter;
        m_CachedCircleCenterOffsetY = m_CircleCenterOffsetY;
    }

    /// <summary>
    /// 检测参数是否有变化
    /// </summary>
    private bool HasParametersChanged()
    {
        return !Mathf.Approximately(m_FanRadius, m_CachedFanRadius)
            || !Mathf.Approximately(m_MaxFanAngle, m_CachedMaxFanAngle)
            || m_CircleCenter != m_CachedCircleCenter
            || !Mathf.Approximately(m_CircleCenterOffsetY, m_CachedCircleCenterOffsetY);
    }

    #endregion

    #region 清理和重置

    /// <summary>
    /// 清理容器状态（战斗结束时调用）
    /// </summary>
    public void ClearState()
    {
        // 杀死所有动画
        m_RearrangeTween?.Kill();

        // 重置所有棋子的状态
        foreach (var card in m_Cards)
        {
            if (card != null)
            {
                // 不需要手动重置，ChessItemUI 有自己的生命周期管理
            }
        }

        // 清理棋子列表
        m_Cards.Clear();

        // 重新缓存参数（以便下次参数检测）
        CacheParameters();

        DebugEx.LogModule("ChessSlotContainer", "容器状态已清理");
    }

    #endregion

    #region 外部接口

    /// <summary>
    /// 新增棋子并播放进场动画
    /// </summary>
    public async UniTask AddCardAsync(ChessItemUI card)
    {
        if (card == null)
            return;

        m_Cards.Add(card);
        card.transform.SetParent(transform);

        DebugEx.LogModule("ChessSlotContainer", $"添加棋子: {m_Cards.Count} 张");

        // 计算延迟时间（按棋子索引从左到右发牌）
        int cardIndex = m_Cards.IndexOf(card);
        float delayTime = cardIndex * m_CardDealInterval;

        // 等待延迟后再播放进场动画
        if (delayTime > 0)
        {
            await UniTask.Delay((int)(delayTime * 1000));
        }

        // 播放新卡进场动画
        await PlayCardEnterAnimationAsync(card);

        // 最后一张卡播放完后，重新排列所有卡（确保位置准确）
        if (cardIndex == m_Cards.Count - 1)
        {
            await RearrangeAsync();
        }
    }

    /// <summary>
    /// 移除棋子并重排
    /// </summary>
    public void RemoveCard(ChessItemUI card)
    {
        if (card == null || !m_Cards.Contains(card))
            return;

        m_Cards.Remove(card);

        DebugEx.LogModule("ChessSlotContainer", $"移除棋子，剩余: {m_Cards.Count} 张");

        // 其他卡补位
        RearrangeAsync().Forget();
    }

    /// <summary>
    /// 获取棋子的基准位置（供选中动画使用）
    /// </summary>
    public Vector2 GetBaseAnchoredPos(ChessItemUI card)
    {
        if (card == null)
            return Vector2.zero;

        return card.GetBaseAnchoredPos();
    }

    #endregion

    #region 核心算法

    /// <summary>
    /// 计算所有卡的扇形位置
    /// </summary>
    private FanTransform[] CalculateFanPositions()
    {
        int cardCount = m_Cards.Count;
        if (cardCount == 0)
            return new FanTransform[0];

        var transforms = new FanTransform[cardCount];

        // 计算圆心位置（容器坐标）
        var rectSize = m_RectTransform.rect;
        Vector2 center = new Vector2(rectSize.width * 0.5f + m_CircleCenter.x, m_CircleCenterOffsetY + m_CircleCenter.y);

        // 计算角度范围
        float fanRangeRad = m_MaxFanAngle * Mathf.Deg2Rad;
        float halfFanRangeRad = fanRangeRad * 0.5f;

        // 计算每张卡的角度步长
        float angleStep = cardCount > 1 ? fanRangeRad / (cardCount - 1) : 0;

        for (int i = 0; i < cardCount; i++)
        {
            float angle = -halfFanRangeRad + i * angleStep;

            // 位置计算：圆弧上的点
            float x = Mathf.Sin(angle) * m_FanRadius;
            float y = Mathf.Cos(angle) * m_FanRadius - m_FanRadius;
            transforms[i].AnchoredPos = center + new Vector2(x, y);

            // 旋转：与圆弧切线对齐
            transforms[i].RotationZ = -angle * Mathf.Rad2Deg;

            // 缩放：可选的深度感
            transforms[i].Scale = Vector3.one;
        }

        return transforms;
    }

    #endregion

    #region 动效播放

    /// <summary>
    /// 播放棋子进场动画
    /// </summary>
    private async UniTask PlayCardEnterAnimationAsync(ChessItemUI card)
    {
        if (card == null)
            return;

        var cardRect = card.GetComponent<RectTransform>();
        var cardImage = card.GetComponent<CanvasGroup>();

        if (cardRect == null)
            return;

        // 初始位置：容器底部中央，Y 偏移
        var rectSize = m_RectTransform.rect;
        Vector2 startPos = new Vector2(rectSize.width * 0.5f, -200f);
        cardRect.anchoredPosition = startPos;

        // 初始透明度
        if (cardImage == null)
        {
            cardImage = card.gameObject.AddComponent<CanvasGroup>();
        }
        cardImage.alpha = 0f;

        // 计算目标位置
        var fanTransforms = CalculateFanPositions();
        int cardIndex = m_Cards.IndexOf(card);
        if (cardIndex < 0 || cardIndex >= fanTransforms.Length)
            return;

        var targetTransform = fanTransforms[cardIndex];

        // 更新卡牌的基准位置
        card.SetBaseFanTransform(targetTransform.AnchoredPos, targetTransform.RotationZ);

        // 播放进场动画：淡入 + 移动
        var sequence = DOTween.Sequence();
        sequence.Join(cardImage.DOFade(1f, m_EnterDuration).SetEase(Ease.OutQuad));
        sequence.Join(cardRect.DOAnchorPos(targetTransform.AnchoredPos, m_EnterDuration).SetEase(Ease.OutQuad));
        sequence.Join(cardRect.DORotate(new Vector3(0, 0, targetTransform.RotationZ), m_EnterDuration).SetEase(Ease.OutQuad));

        await sequence.AsyncWaitForCompletion();

        DebugEx.LogModule("ChessSlotContainer", $"棋子进场动画完成");
    }

    /// <summary>
    /// 重新排列所有棋子
    /// </summary>
    private async UniTask RearrangeAsync()
    {
        if (m_Cards.Count == 0)
            return;

        // 杀死之前的重排动画
        m_RearrangeTween?.Kill();

        // 计算新位置
        var fanTransforms = CalculateFanPositions();

        var sequence = DOTween.Sequence();

        for (int i = 0; i < m_Cards.Count; i++)
        {
            var card = m_Cards[i];
            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            Vector2 targetPos = fanTransforms[i].AnchoredPos;
            float targetRotZ = fanTransforms[i].RotationZ;

            // 更新卡牌的基准位置
            card.SetBaseFanTransform(fanTransforms[i].AnchoredPos, targetRotZ);

            // 添加到序列（位置和旋转动画）
            sequence.Join(cardRect.DOAnchorPos(targetPos, m_RearrangeDuration).SetEase(m_RearrangeEase));
            sequence.Join(cardRect.DORotate(new Vector3(0, 0, targetRotZ), m_RearrangeDuration).SetEase(m_RearrangeEase));
        }

        m_RearrangeTween = sequence;

        await sequence.AsyncWaitForCompletion();

        DebugEx.LogModule("ChessSlotContainer", "棋子重排完成");
    }

    /// <summary>
    /// 立即更新棋子位置（不播放动画）- 用于快速参数调整
    /// </summary>
    public void RefreshCardPositionsImmediate()
    {
        if (m_Cards.Count == 0)
            return;

        // 杀死重排动画
        m_RearrangeTween?.Kill();

        var fanTransforms = CalculateFanPositions();

        for (int i = 0; i < m_Cards.Count; i++)
        {
            var card = m_Cards[i];
            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            Vector2 targetPos = fanTransforms[i].AnchoredPos;
            float targetRotZ = fanTransforms[i].RotationZ;

            // 直接设置位置和旋转，不播放动画
            cardRect.anchoredPosition = targetPos;
            cardRect.localRotation = Quaternion.Euler(0, 0, targetRotZ);
            card.SetBaseFanTransform(fanTransforms[i].AnchoredPos, targetRotZ);
        }

        DebugEx.LogModule("ChessSlotContainer", "棋子位置已立即更新");
    }

    #endregion

    #region 辅助结构

    private struct FanTransform
    {
        public Vector2 AnchoredPos;
        public float RotationZ;
        public Vector3 Scale;
    }

    #endregion
}
