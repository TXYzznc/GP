using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 棋子容器：管理棋子卡的水平排列和进场/重排动效
/// （参考 CardSlotContainer 的实现，去掉拖拽让位逻辑）
/// </summary>
public class ChessSlotContainer : MonoBehaviour
{
    #region 参数配置

    [Header("水平排列")]
    [SerializeField]
    private float m_CardSpacing = 150f; // 卡牌间距

    [SerializeField]
    private float m_StartOffsetX = -400f; // 起始X偏移（左对齐）

    [SerializeField]
    private float m_VerticalOffset = 0f; // 垂直偏移

    [Header("进场动画")]
    [SerializeField]
    private float m_EnterDuration = 0.35f; // 进场动画时长

    [SerializeField]
    private float m_CardDealInterval = 0.1f; // 棋子发牌间隔时间（秒）

    [SerializeField]
    private float m_EnterStartOffsetX = 100f; // 进场起始X偏移（从容器右侧向右）

    [SerializeField]
    private float m_EnterStartOffsetY = 0f; // 进场起始Y偏移（相对于容器Y坐标）

    [SerializeField]
    private Ease m_EnterEase = Ease.OutQuad; // 进场动画缓动函数

    [Header("补位动画")]
    [SerializeField]
    private float m_RearrangeDuration = 0.25f; // 补位动画时长

    [SerializeField]
    private Ease m_RearrangeEase = Ease.OutCubic;

    // 缓存旧参数，用于检测参数变化
    private float m_CachedCardSpacing;
    private float m_CachedStartOffsetX;
    private float m_CachedVerticalOffset;
    private float m_CachedEnterStartOffsetX;
    private float m_CachedEnterStartOffsetY;

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
        m_CachedCardSpacing = m_CardSpacing;
        m_CachedStartOffsetX = m_StartOffsetX;
        m_CachedVerticalOffset = m_VerticalOffset;
        m_CachedEnterStartOffsetX = m_EnterStartOffsetX;
        m_CachedEnterStartOffsetY = m_EnterStartOffsetY;
    }

    /// <summary>
    /// 检测参数是否有变化
    /// </summary>
    private bool HasParametersChanged()
    {
        return !Mathf.Approximately(m_CardSpacing, m_CachedCardSpacing)
            || !Mathf.Approximately(m_StartOffsetX, m_CachedStartOffsetX)
            || !Mathf.Approximately(m_VerticalOffset, m_CachedVerticalOffset)
            || !Mathf.Approximately(m_EnterStartOffsetX, m_CachedEnterStartOffsetX)
            || !Mathf.Approximately(m_EnterStartOffsetY, m_CachedEnterStartOffsetY);
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

        // 立即设置卡牌初始位置和透明度，防止闪烁
        InitializeCardStartPosition(card, cardIndex);

        // 等待延迟后再播放进场动画
        if (delayTime > 0)
        {
            await UniTask.Delay((int)(delayTime * 1000));
        }

        // 延迟后检查卡牌是否已被销毁（UI关闭时可能发生）
        if (card == null)
            return;

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

        for (int i = 0; i < cardCount; i++)
        {
            // 水平排列：X = 起始位置 + 索引 * 间距
            float x = m_StartOffsetX + i * m_CardSpacing;
            float y = m_VerticalOffset;

            transforms[i].AnchoredPos = new Vector2(x, y);
            transforms[i].RotationZ = 0f; // 水平排列不需要旋转
            transforms[i].Scale = Vector3.one;
        }

        return transforms;
    }

    #endregion

    #region 动效播放

    /// <summary>
    /// 初始化卡牌的起始位置和透明度
    /// </summary>
    private void InitializeCardStartPosition(ChessItemUI card, int cardIndex)
    {
        if (card == null)
            return;

        var cardRect = card.GetComponent<RectTransform>();
        var cardImage = card.GetComponent<CanvasGroup>();

        if (cardRect == null)
            return;

        // 计算目标位置
        var fanTransforms = CalculateFanPositions();
        if (cardIndex < 0 || cardIndex >= fanTransforms.Length)
            return;

        var targetTransform = fanTransforms[cardIndex];

        // 设置起始位置：从右侧进入
        var rectSize = m_RectTransform.rect;
        float rightEdgeX = rectSize.width * 0.5f + m_EnterStartOffsetX;  // 容器右侧边界 + 可配置偏移
        float startY = targetTransform.AnchoredPos.y + m_EnterStartOffsetY;  // 目标Y + 可配置偏移
        Vector2 startPos = new Vector2(rightEdgeX, startY);
        cardRect.anchoredPosition = startPos;

        // 设置初始透明度（完全透明，等待动画显示）
        if (cardImage == null)
        {
            cardImage = card.gameObject.AddComponent<CanvasGroup>();
        }
        cardImage.alpha = 0f;

        DebugEx.LogModule("ChessSlotContainer", $"棋子初始位置已设置");
    }

    /// <summary>
    /// 播放棋子进场动画
    /// </summary>
    private async UniTask PlayCardEnterAnimationAsync(ChessItemUI card)
    {
        if (card == null)
            return;

        var cardRect = card.GetComponent<RectTransform>();
        var cardImage = card.GetComponent<CanvasGroup>();

        if (cardRect == null || cardImage == null)
            return;

        // 计算目标位置
        var fanTransforms = CalculateFanPositions();
        int cardIndex = m_Cards.IndexOf(card);
        if (cardIndex < 0 || cardIndex >= fanTransforms.Length)
            return;

        var targetTransform = fanTransforms[cardIndex];

        // 更新卡牌的基准位置
        card.SetBaseFanTransform(targetTransform.AnchoredPos, targetTransform.RotationZ);

        // 播放进场动画：淡入 + 移动
        // 注：起始位置已在 InitializeCardStartPosition 中设置
        var sequence = DOTween.Sequence();
        sequence.Join(cardImage.DOFade(1f, m_EnterDuration).SetEase(m_EnterEase));
        sequence.Join(
            cardRect.DOAnchorPos(targetTransform.AnchoredPos, m_EnterDuration).SetEase(m_EnterEase)
        );
        sequence.Join(
            cardRect
                .DORotate(new Vector3(0, 0, targetTransform.RotationZ), m_EnterDuration)
                .SetEase(m_EnterEase)
        );

        await sequence.AsyncWaitForCompletion();

        // 动画完成后检查对象是否已被销毁
        if (card == null)
            return;

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
            if (card == null)
                continue;
            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            Vector2 targetPos = fanTransforms[i].AnchoredPos;
            float targetRotZ = fanTransforms[i].RotationZ;

            // 更新卡牌的基准位置
            card.SetBaseFanTransform(fanTransforms[i].AnchoredPos, targetRotZ);

            // 添加到序列（位置和旋转动画）
            sequence.Join(
                cardRect.DOAnchorPos(targetPos, m_RearrangeDuration).SetEase(m_RearrangeEase)
            );
            sequence.Join(
                cardRect
                    .DORotate(new Vector3(0, 0, targetRotZ), m_RearrangeDuration)
                    .SetEase(m_RearrangeEase)
            );
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
            if (card == null)
                continue;
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
