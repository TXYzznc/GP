using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 卡牌容器：管理卡牌的扇形排列和动效（进场、补位、拖拽让位）
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardSlotContainer : MonoBehaviour
{
    #region 参数配置

    [Header("扇形排列")]
    [SerializeField] private float m_FanRadius = 1500f;           // 圆弧半径
    [SerializeField] private float m_MaxFanAngle = 30f;          // 最大展开角度（度）
    [SerializeField] private Vector2 m_CircleCenter = Vector2.zero;  // 圆心偏移
    [SerializeField] private float m_CircleCenterOffsetY = 120f;    // 圆心在容器底部偏下距离

    [Header("动效时长")]
    [SerializeField] private float m_EnterDuration = 0.1f;      // 进场动画时长
    [SerializeField] private float m_CardDealInterval = 0.03f;    // 卡牌发牌间隔时间（秒）
    [SerializeField] private float m_RefreshEmitInterval = 0.1f; // 刷新时卡牌发射间隔（秒）
    [SerializeField] private float m_RearrangeDuration = 0.1f;  // 补位动画时长
    [SerializeField] private Ease m_RearrangeEase = Ease.OutCubic;

    // 缓存旧参数，用于检测参数变化
    private float m_CachedFanRadius;
    private float m_CachedMaxFanAngle;
    private Vector2 m_CachedCircleCenter;
    private float m_CachedCircleCenterOffsetY;

    #endregion

    #region 字段

    private RectTransform m_RectTransform;
    private List<CardSlotItem> m_Cards = new List<CardSlotItem>();

    // 拖拽上下文：记录当前谁在拖拽、插入位置在哪（仅缓存，不控制）
    private DragContext m_DragContext;

    // 动效控制
    private Tween m_RearrangeTween;
    private CancellationTokenSource m_RearrangeCts;
    private UniTask m_LatestRearrangeTask = UniTask.CompletedTask;

    // 调试：位置追踪
    private float m_DebugTimer = 0f;
    private const float m_DebugInterval = 0.5f;

    #endregion

    #region 生命周期

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
        if (m_RectTransform == null)
        {
            DebugEx.ErrorModule("CardSlotContainer", "缺少 RectTransform 组件");
        }

        // 初始化缓存参数
        CacheParameters();
    }

    private void OnEnable()
    {
        // 当容器启用时订阅事件（而不是在 Awake 中）
        // 这样可以避免时序问题，确保只在真正需要时才订阅
        SubscribeToCardEvents();
        DebugEx.LogModule("CardSlotContainer", "[启用] 已订阅所有卡牌事件");
    }

    private void OnDisable()
    {
        // 当容器禁用时取消订阅
        UnsubscribeFromCardEvents();
        DebugEx.LogModule("CardSlotContainer", "[禁用] 已取消事件订阅");
    }

    /// <summary>
    /// 订阅卡牌事件（从 EventDispatcher）
    /// </summary>
    private void SubscribeToCardEvents()
    {
        CardSlotItemEventDispatcher.OnDragStarted += OnCardDragStarted;
        CardSlotItemEventDispatcher.OnDragPositionChanged += OnCardDragPositionChanged;
        CardSlotItemEventDispatcher.OnDragEnded += OnCardDragEnded;
        CardSlotItemEventDispatcher.OnAboutToDestroy += OnCardAboutToDestroy;
        DebugEx.LogModule("CardSlotContainer", "[订阅] 已订阅所有卡牌事件");
    }

    /// <summary>
    /// 取消订阅卡牌事件
    /// </summary>
    private void UnsubscribeFromCardEvents()
    {
        CardSlotItemEventDispatcher.OnDragStarted -= OnCardDragStarted;
        CardSlotItemEventDispatcher.OnDragPositionChanged -= OnCardDragPositionChanged;
        CardSlotItemEventDispatcher.OnDragEnded -= OnCardDragEnded;
        CardSlotItemEventDispatcher.OnAboutToDestroy -= OnCardAboutToDestroy;
    }

    private void Update()
    {
        // 仅在运行时检测参数变化（用于实时调参）
        if (!Application.isPlaying)
            return;

        // 检测参数是否有变化
        if (HasParametersChanged())
        {
            DebugEx.LogModule("CardSlotContainer", "检测到参数变化，立即更新卡牌位置");
            CacheParameters();
            // 立即更新位置（不播放动画，实时反馈）
            RefreshCardPositionsImmediate();
        }

        // 调试：每 0.5s 输出一次卡牌位置
        m_DebugTimer += Time.deltaTime;
        if (m_DebugTimer >= m_DebugInterval)
        {
            m_DebugTimer = 0f;
            DebugCardPositions();
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

    #region Gizmo调试

    private void OnDrawGizmos()
    {
        if (m_RectTransform == null)
            return;

        // 圆心在本地坐标系（rect坐标）中的位置
        float circleCenterX_local = 0; // rect.center.x
        float circleCenterY_local = -m_FanRadius + m_CircleCenterOffsetY;
        Vector3 circleCenterLocal = new Vector3(circleCenterX_local + m_CircleCenter.x, circleCenterY_local, 0);

        // 转换到世界坐标进行绘制
        Vector3 circleCenterWorld = m_RectTransform.TransformPoint(circleCenterLocal);

        // 绘制圆心
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(circleCenterWorld, 30f);

        // 绘制发射方向（从圆心出发，角度范围从 -MaxFanAngle/2 到 +MaxFanAngle/2）
        float fanRangeRad = m_MaxFanAngle * Mathf.Deg2Rad;
        float halfFanRangeRad = fanRangeRad * 0.5f;

        Gizmos.color = Color.yellow;
        float rayLength = m_FanRadius * 0.3f; // 射线长度为半径的30%

        // 绘制左侧边界（-MaxFanAngle/2）
        float leftAngle = -halfFanRangeRad;
        Vector3 leftDir = new Vector3(Mathf.Sin(leftAngle), Mathf.Cos(leftAngle), 0);
        Gizmos.DrawLine(circleCenterWorld, circleCenterWorld + leftDir * rayLength);

        // 绘制中心线（0°）
        Gizmos.color = Color.green;
        Vector3 centerDir = new Vector3(0, 1, 0);
        Gizmos.DrawLine(circleCenterWorld, circleCenterWorld + centerDir * rayLength);

        // 绘制右侧边界（+MaxFanAngle/2）
        Gizmos.color = Color.yellow;
        float rightAngle = halfFanRangeRad;
        Vector3 rightDir = new Vector3(Mathf.Sin(rightAngle), Mathf.Cos(rightAngle), 0);
        Gizmos.DrawLine(circleCenterWorld, circleCenterWorld + rightDir * rayLength);

        // 绘制扇形弧线
        Gizmos.color = Color.cyan;
        int segments = 20;
        Vector3 prevPoint = circleCenterWorld + new Vector3(Mathf.Sin(-halfFanRangeRad), Mathf.Cos(-halfFanRangeRad), 0) * rayLength;
        for (int i = 1; i <= segments; i++)
        {
            float angle = -halfFanRangeRad + (fanRangeRad / segments) * i;
            Vector3 nextPoint = circleCenterWorld + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0) * rayLength;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    #endregion

    #region 调试

    /// <summary>
    /// 输出所有卡牌的当前位置
    /// </summary>
    private void DebugCardPositions()
    {
        if (m_Cards.Count == 0)
            return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[CardSlotContainer] 卡牌位置调试（共 {m_Cards.Count} 张）:");
        for (int i = 0; i < m_Cards.Count; i++)
        {
            var card = m_Cards[i];
            if (card == null)
                continue;

            var rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                var pos = rect.anchoredPosition;
                var rot = rect.localRotation.eulerAngles.z;
                sb.AppendLine($"  [{i}] {card.name}: pos=({pos.x:F0}, {pos.y:F0}), rot={rot:F0}°");
            }
        }

        DebugEx.LogModule("CardSlotContainer", sb.ToString());
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

        // 重置所有卡牌的状态
        foreach (var card in m_Cards)
        {
            if (card != null)
            {
                card.ResetState();
            }
        }

        // 清理卡牌列表
        m_Cards.Clear();

        // 重置拖拽上下文
        m_DragContext.Clear();

        // 重新缓存参数（以便下次参数检测）
        CacheParameters();

        // 注意：不在此取消订阅，事件订阅应该在容器销毁时才取消
        // 这样可以确保容器还在运行时，事件始终保持订阅状态

        DebugEx.LogModule("CardSlotContainer", "容器状态已清理");
    }

    #endregion

    #region 事件回调（从 EventDispatcher）

    /// <summary>
    /// 卡牌开始拖拽（事件回调）
    /// </summary>
    private void OnCardDragStarted(CardSlotItem card, int startIndex)
    {
        if (card == null || !m_Cards.Contains(card))
            return;

        m_DragContext = new DragContext { Card = card, StartIndex = startIndex, CurrentInsertIndex = startIndex };
        DebugEx.LogModule("CardSlotContainer", $"[事件] 卡牌拖拽开始: {card.GetCardData()?.Name}");
    }

    /// <summary>
    /// 卡牌拖拽中，插入位置改变（事件回调）
    /// </summary>
    private void OnCardDragPositionChanged(CardSlotItem card, int newInsertIndex)
    {
        if (m_DragContext.Card != card)
            return;

        if (m_DragContext.CurrentInsertIndex == newInsertIndex)
            return;

        m_DragContext.CurrentInsertIndex = newInsertIndex;
        DebugEx.LogModule("CardSlotContainer", $"[事件] 拖拽位置改变: {newInsertIndex}");

        // 立即启动重排动画
        RearrangeAsync(default).Forget();
    }

    /// <summary>
    /// 卡牌拖拽结束（事件回调）
    /// </summary>
    private void OnCardDragEnded(CardSlotItem card, Vector3 worldPos, bool isValid)
    {
        if (m_DragContext.Card != card)
            return;

        DebugEx.LogModule("CardSlotContainer", $"[事件] 拖拽结束: isValid={isValid}");

        // 清理拖拽上下文
        m_DragContext.Clear();

        // 如果拖拽成功（释放在战场），容器不需要做什么
        // 如果拖拽失败（返回卡槽），由 RearrangeAsync 处理位置恢复
        if (!isValid)
        {
            // 立即启动重排，让卡牌回到正常位置
            RearrangeAsync(default).Forget();
        }
    }

    /// <summary>
    /// 卡牌即将销毁（事件回调）
    /// </summary>
    private void OnCardAboutToDestroy(CardSlotItem card)
    {
        if (!m_Cards.Contains(card))
            return;

        DebugEx.LogModule("CardSlotContainer", $"[事件] 卡牌即将销毁: {card.GetCardData()?.Name}");

        // 立即从列表移除（这样重排计算时不会包含这张卡）
        m_Cards.Remove(card);

        // 立即启动重排动画，其他卡向中心靠拢
        RearrangeAsync(default).Forget();
    }

    #endregion

    #region 外部接口

    /// <summary>
    /// 新增卡牌并播放进场动画
    /// </summary>
    /// <summary>
    /// 添加卡牌且播放进场动画（普通模式，单张添加带延迟）
    /// </summary>
    public async UniTask AddCardAsync(CardSlotItem card, bool isRefreshMode = false)
    {
        if (card == null)
            return;

        m_Cards.Add(card);
        card.transform.SetParent(transform);

        DebugEx.LogModule("CardSlotContainer", $"添加卡牌: {m_Cards.Count} 张");

        int cardIndex = m_Cards.IndexOf(card);

        // 刷新模式下，延迟逻辑由 PlayCardEnterAnimationAsync 处理
        // 正常模式下，使用发牌间隔
        if (!isRefreshMode)
        {
            float delayTime = cardIndex * m_CardDealInterval;
            if (delayTime > 0)
            {
                await UniTask.Delay((int)(delayTime * 1000));
            }
        }

        // 播放新卡进场动画
        await PlayCardEnterAnimationAsync(card, isRefreshMode);

        // 最后一张卡播放完后，重新排列所有卡（确保位置准确）
        if (cardIndex == m_Cards.Count - 1)
        {
            await RearrangeAsync(default);
        }
    }

    /// <summary>
    /// 仅添加卡牌到容器，不播放动画（用于批量添加）
    /// </summary>
    public void AddCardSilent(CardSlotItem card)
    {
        if (card == null)
            return;

        m_Cards.Add(card);
        card.transform.SetParent(transform);

        DebugEx.LogModule("CardSlotContainer", $"静默添加卡牌: {m_Cards.Count} 张");
    }

    /// <summary>
    /// 批量启动所有卡牌的进场动画（刷新模式）
    /// </summary>
    public async UniTask PlayAllCardAnimationsAsync()
    {
        if (m_Cards.Count == 0)
            return;

        var animationTasks = new List<UniTask>();

        for (int i = 0; i < m_Cards.Count; i++)
        {
            animationTasks.Add(PlayCardEnterAnimationAsync(m_Cards[i], isRefreshMode: true));
        }

        // 等待所有动画完成
        await UniTask.WhenAll(animationTasks);

        // 动画都播放完后，重新排列所有卡（确保位置准确）
        await RearrangeAsync(default);

        DebugEx.LogModule("CardSlotContainer", "所有卡牌动画播放完成");
    }

    /// <summary>
    /// 移除卡牌（通过卡牌ID）— 触发卡牌销毁流程
    /// </summary>
    public void RemoveCardById(int cardId)
    {
        DebugEx.LogModule("CardSlotContainer", $"[RemoveCardById] 尝试移除卡牌 ID={cardId}");

        var cardSlot = m_Cards.FirstOrDefault(c => c.GetCardData()?.CardId == cardId);
        if (cardSlot == null)
        {
            DebugEx.WarningModule("CardSlotContainer", $"[RemoveCardById] 未找到卡牌 ID={cardId}");
            return;
        }

        // 触发卡牌销毁流程（卡牌会发出 OnAboutToDestroy 事件）
        cardSlot.BeginDestruction();
    }

    /// <summary>
    /// 移除卡牌（内部使用，由 EventDispatcher 触发）
    /// </summary>
    public void RemoveCard(CardSlotItem card)
    {
        if (card == null || !m_Cards.Contains(card))
        {
            DebugEx.WarningModule("CardSlotContainer", $"[RemoveCard] 卡牌为空或不在容器中");
            return;
        }

        DebugEx.LogModule("CardSlotContainer", $"[RemoveCard] 移除卡牌，当前数量={m_Cards.Count}");

        // 从活跃卡列表中移除（已在 OnCardAboutToDestroy 中完成）
        // 这个方法现在保留用于兼容性
    }

    /// <summary>
    /// 等待最新的重排任务完成（用于 CardSlotItem 销毁时同步）
    /// </summary>
    public async UniTask WaitForLatestRearrangeAsync()
    {
        await m_LatestRearrangeTask;
    }



    #endregion

    #region 核心算法

    /// <summary>
    /// 根据卡牌的anchor和pivot灵活计算圆心的anchoredPosition
    /// </summary>
    private Vector2 GetCircleCenterAnchoredPos(RectTransform cardRect)
    {
        if (cardRect == null || m_RectTransform == null)
            return m_CircleCenter;

        // 圆心想要的位置（在CardSlots的rect本地坐标系中）
        float circleCenterX_local = 0; // rect.center.x，加上手动偏移
        float circleCenterY_local = -m_FanRadius + m_CircleCenterOffsetY;

        // 卡牌的anchor位置（在CardSlots的rect坐标系中）
        // anchor在[0,1]范围，转换到rect坐标系
        float anchorX = cardRect.anchorMin.x;
        float anchorY = cardRect.anchorMin.y;
        float cardAnchorX = Mathf.Lerp(m_RectTransform.rect.min.x, m_RectTransform.rect.max.x, anchorX);
        float cardAnchorY = Mathf.Lerp(m_RectTransform.rect.min.y, m_RectTransform.rect.max.y, anchorY);

        // 圆心相对于卡牌anchor的anchoredPosition
        float circleCenterX = circleCenterX_local - cardAnchorX + m_CircleCenter.x;
        float circleCenterY = circleCenterY_local - cardAnchorY;

        return new Vector2(circleCenterX, circleCenterY);
    }

    /// <summary>
    /// 计算所有卡的扇形位置（基于世界坐标转换，确保anchor/pivot一致性）
    /// </summary>
    private FanTransform[] CalculateFanPositions(bool includeDragCard = false)
    {
        int cardCount = includeDragCard ? m_Cards.Count : m_Cards.Count - (m_DragContext.IsActive ? 1 : 0);
        if (cardCount == 0)
            return new FanTransform[0];

        var transforms = new FanTransform[cardCount];

        // 获取卡牌的anchor位置（在rect本地坐标系中）
        float anchorX = 0; // 卡牌anchor.x = 0
        float anchorY = 0; // 卡牌anchor.y = 0
        float cardAnchorX = Mathf.Lerp(m_RectTransform.rect.min.x, m_RectTransform.rect.max.x, anchorX);
        float cardAnchorY = Mathf.Lerp(m_RectTransform.rect.min.y, m_RectTransform.rect.max.y, anchorY);

        // 圆心在rect本地坐标系中的位置
        float circleCenterX_local = 0;
        float circleCenterY_local = -m_FanRadius + m_CircleCenterOffsetY;

        // 计算角度范围
        float fanRangeRad = m_MaxFanAngle * Mathf.Deg2Rad;
        float halfFanRangeRad = fanRangeRad * 0.5f;

        // 计算每张卡的角度步长
        float angleStep = cardCount > 1 ? fanRangeRad / (cardCount - 1) : 0;

        for (int i = 0; i < cardCount; i++)
        {
            float angle = -halfFanRangeRad + i * angleStep;

            // 卡牌在rect本地坐标系中的目标位置
            float offsetX = Mathf.Sin(angle) * m_FanRadius;
            float offsetY = Mathf.Cos(angle) * m_FanRadius;
            float cardTargetX_local = circleCenterX_local + offsetX;
            float cardTargetY_local = circleCenterY_local + offsetY;

            // anchoredPosition = 本地坐标 - anchor位置
            float cardAnchoredX = cardTargetX_local - cardAnchorX + m_CircleCenter.x;
            float cardAnchoredY = cardTargetY_local - cardAnchorY;

            transforms[i].AnchoredPos = new Vector2(cardAnchoredX, cardAnchoredY);
            transforms[i].RotationZ = -angle * Mathf.Rad2Deg;
            transforms[i].Scale = Vector3.one;
        }

        return transforms;
    }

    /// <summary>
    /// 获取卡牌在容器中的索引（用于拖拽开始时）
    /// </summary>
    public int GetCardIndex(CardSlotItem card)
    {
        return m_Cards.IndexOf(card);
    }

    /// <summary>
    /// 根据鼠标屏幕位置计算卡牌应该插入的索引
    /// </summary>
    public int CalculateInsertIndex(Vector2 screenPos)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_RectTransform, screenPos, null, out Vector2 localPos))
        {
            return m_DragContext.StartIndex;
        }

        // 根据 X 坐标判断插入位置
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < m_Cards.Count; i++)
        {
            if (m_Cards[i] == m_DragContext.Card)
                continue;

            var cardRect = m_Cards[i].GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            float distance = Mathf.Abs(cardRect.anchoredPosition.x - localPos.x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        // 根据距离判断是左还是右
        var closestCard = m_Cards[closestIndex];
        var closestCardRect = closestCard.GetComponent<RectTransform>();
        if (localPos.x > closestCardRect.anchoredPosition.x)
        {
            closestIndex = (closestIndex + 1) % m_Cards.Count;
        }

        return closestIndex;
    }


    #endregion

    #region 动效播放

    /// <summary>
    /// 播放卡牌进场动画
    /// </summary>
    private async UniTask PlayCardEnterAnimationAsync(CardSlotItem card, bool isRefreshMode = false)
    {
        if (card == null)
            return;

        var cardRect = card.GetComponent<RectTransform>();
        var cardImage = card.GetComponent<CanvasGroup>();

        if (cardRect == null)
            return;

        // 初始透明度
        if (cardImage == null)
        {
            cardImage = card.gameObject.AddComponent<CanvasGroup>();
        }
        cardImage.alpha = 0f;

        // 计算目标位置
        var fanTransforms = CalculateFanPositions(includeDragCard: true);
        int cardIndex = m_Cards.IndexOf(card);
        if (cardIndex < 0 || cardIndex >= fanTransforms.Length)
            return;

        var targetTransform = fanTransforms[cardIndex];

        // 更新卡牌的基准位置
        card.SetBaseFanTransform(this, targetTransform.AnchoredPos, targetTransform.RotationZ);

        // 圆心发射点（根据卡牌的anchor和pivot灵活计算）
        Vector2 circleCenter = GetCircleCenterAnchoredPos(cardRect);
        cardRect.anchoredPosition = circleCenter;

        // 刷新模式：等待发射延迟
        if (isRefreshMode && cardIndex > 0)
        {
            float emitDelay = cardIndex * m_RefreshEmitInterval;
            await UniTask.Delay((int)(emitDelay * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        // 播放进场动画：淡入 + 移动
        var sequence = DOTween.Sequence();
        sequence.Join(cardImage.DOFade(1f, m_EnterDuration).SetEase(Ease.OutQuad));
        sequence.Join(cardRect.DOAnchorPos(targetTransform.AnchoredPos, m_EnterDuration).SetEase(Ease.OutQuad));
        sequence.Join(cardRect.DORotate(new Vector3(0, 0, targetTransform.RotationZ), m_EnterDuration).SetEase(Ease.OutQuad));

        await sequence.AsyncWaitForCompletion();

        DebugEx.LogModule("CardSlotContainer", $"卡牌进场动画完成");
    }

    /// <summary>
    /// 重新排列所有卡牌（只负责排列，销毁由 CardSlotItem 自管理）
    /// </summary>
    public async UniTask RearrangeAsync(CancellationToken ct)
    {
        // 清理已销毁的卡牌引用（防止位置计算时count不匹配）
        m_Cards.RemoveAll(c => c == null || c.gameObject == null);

        DebugEx.LogModule("CardSlotContainer", $"[RearrangeAsync] 开始重排，当前卡牌数={m_Cards.Count}");

        // 创建快照：只对当前活跃卡进行动画（拖拽中的卡不动画）
        var activeCards = m_Cards.Where(c => c != m_DragContext.Card).ToList();

        if (activeCards.Count == 0)
        {
            DebugEx.LogModule("CardSlotContainer", $"[RearrangeAsync] 活跃卡数为0，直接返回");
            return;
        }

        // 杀死之前的重排动画
        m_RearrangeTween?.Kill();

        // 禁用所有卡牌的交互（防止悬停动画干扰重排）
        foreach (var card in activeCards)
        {
            card.SetInteractable(false);
        }

        // 基于活跃卡的快照计算位置
        var fanTransforms = CalculateFanPositions(includeDragCard: false);
        DebugEx.LogModule("CardSlotContainer", $"[RearrangeAsync] 计算出 {fanTransforms.Length} 个目标位置，实际活跃卡={activeCards.Count}");

        var sequence = DOTween.Sequence();

        for (int i = 0; i < activeCards.Count; i++)
        {
            var card = activeCards[i];
            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            Vector2 targetPos = fanTransforms[i].AnchoredPos;

            float targetRotZ = fanTransforms[i].RotationZ;
            card.SetBaseFanTransform(this, fanTransforms[i].AnchoredPos, targetRotZ);

            sequence.Join(cardRect.DOAnchorPos(targetPos, m_RearrangeDuration).SetEase(m_RearrangeEase));
            sequence.Join(cardRect.DORotate(new Vector3(0, 0, targetRotZ), m_RearrangeDuration).SetEase(m_RearrangeEase));
        }

        m_RearrangeTween = sequence;

        DebugEx.LogModule("CardSlotContainer", $"[RearrangeAsync] 播放重排动画，共 {activeCards.Count} 张卡牌");
        await sequence.AsyncWaitForCompletion();

        // 检查是否被新的 RearrangeAsync 取消
        ct.ThrowIfCancellationRequested();

        // 重新启用交互
        foreach (var card in activeCards)
        {
            card.SetInteractable(true);
        }

        DebugEx.LogModule("CardSlotContainer", $"[RearrangeAsync] 重排完成");
    }


    /// <summary>
    /// 立即更新卡牌位置（不播放动画）- 用于快速参数调整
    /// </summary>
    public void RefreshCardPositionsImmediate()
    {
        if (m_Cards.Count == 0)
            return;

        // 杀死重排动画
        m_RearrangeTween?.Kill();

        var fanTransforms = CalculateFanPositions(includeDragCard: false);

        int transformIndex = 0;
        for (int i = 0; i < m_Cards.Count; i++)
        {
            var card = m_Cards[i];
            if (card == m_DragContext.Card)
                continue;

            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            Vector2 targetPos = fanTransforms[transformIndex].AnchoredPos;

            float targetRotZ = fanTransforms[transformIndex].RotationZ;

            // 直接设置位置和旋转，不播放动画
            cardRect.anchoredPosition = targetPos;
            cardRect.localRotation = Quaternion.Euler(0, 0, targetRotZ);
            card.SetBaseFanTransform(this, fanTransforms[transformIndex].AnchoredPos, targetRotZ);

            transformIndex++;
        }

        DebugEx.LogModule("CardSlotContainer", "卡牌位置已立即更新");
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
