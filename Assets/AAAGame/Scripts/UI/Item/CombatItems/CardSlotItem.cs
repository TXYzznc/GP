using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public partial class CardSlotItem
    : UIItemBase,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler
{
    #region 字段

    private CardData m_CardData;
    private bool m_IsSelected;
    private Vector3 m_BtnOriginalPosition;
    private const float SELECTED_OFFSET = 20f;

    // 扇形容器相关
    private CardSlotContainer m_Container;
    private Vector2 m_BaseAnchoredPos; // Container 分配的基准位置
    private float m_BaseRotZ; // 基准旋转

    // 拖拽相关字段
    private GameObject m_DragPreview;
    private Image m_DragPreviewImage;
    private CanvasGroup m_DragPreviewCanvasGroup;
    private float m_LastRaycastTime;
    private const float RAYCAST_INTERVAL = 0.1f;

    // 策略卡目标描边缓存
    private List<ChessEntity> m_PreviewTargets = new List<ChessEntity>();

    // 动效相关字段
    private Tween m_SelectTween;
    private Tween m_HoverTween;
    private Tween m_DragPreviewTween;
    private Tween m_PositionTween; // 位置动画（悬停上移）
    private CanvasGroup m_BtnCanvasGroup;
    private RectTransform m_ItemRectTransform;
    private const float HOVER_SCALE = 1.05f;           // 鼠标悬停时放大倍数
    private const float HOVER_DURATION = 0.2f;         // 悬停动画时长（秒）
    private const float HOVER_OFFSET_Y = 30f;          // 悬停时向上移动距离（像素）
    private const float PULSE_SCALE = 1.1f;            // 选中卡牌脉冲效果最大放大倍数
    private const float PULSE_DURATION = 0.3f;         // 脉冲动画时长（秒）
    private const float DRAG_ALPHA = 0.5f;             // 拖拽时卡牌透明度（0-1）
    private const float PREVIEW_ROTATION = 5f;         // 拖拽预览旋转角度（度）
    private const float FLASH_DURATION = 0.2f;         // 卡牌使用时闪光效果时长（秒）
    private const float FLASH_ALPHA = 1.5f;            // 闪光时最大亮度倍数

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置卡牌数据
    /// </summary>
    public void SetData(CardData cardData)
    {
        m_CardData = cardData;
        m_IsSelected = false;
        RefreshUI();
    }

    /// <summary>
    /// 获取卡牌数据
    /// </summary>
    public CardData GetCardData()
    {
        return m_CardData;
    }

    /// <summary>
    /// 设置扇形容器和基准位置（由 CardSlotContainer 调用）
    /// </summary>
    public void SetBaseFanTransform(CardSlotContainer container, Vector2 anchoredPos, float rotZ)
    {
        m_Container = container;
        m_BaseAnchoredPos = anchoredPos;
        m_BaseRotZ = rotZ;

        // 更新 RectTransform
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPos;
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotZ);
        }

        m_ItemRectTransform = rectTransform;
    }

    /// <summary>
    /// 设置卡牌交互状态（禁用/启用）
    /// </summary>
    public void SetInteractable(bool enabled)
    {
        if (varBtn != null && varBtn.TryGetComponent<Image>(out var image))
        {
            image.raycastTarget = enabled;
        }
    }

    /// <summary>
    /// 重置卡牌状态（从对象池回收时调用）
    /// </summary>
    public void ResetState()
    {
        m_IsSelected = false;
        m_CardData = null;
        m_Container = null;
        m_BaseAnchoredPos = Vector2.zero;
        m_BaseRotZ = 0f;

        // 杀死所有动画
        m_SelectTween?.Kill();
        m_HoverTween?.Kill();
        m_PositionTween?.Kill();
        m_DragPreviewTween?.Kill();

        // 重置 RectTransform
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
        }

        // 重置 CanvasGroup 透明度
        if (m_BtnCanvasGroup != null)
        {
            m_BtnCanvasGroup.alpha = 1f;
        }

        // 重置 Button 的 Image 和 scale
        if (varBtn != null)
        {
            var btnImage = varBtn.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = new Color(1f, 1f, 1f, 0f);  // 按钮Image应保持透明（alpha=0）
                btnImage.raycastTarget = true;
            }
            varBtn.transform.localScale = Vector3.one;
        }

        m_BtnOriginalPosition = Vector3.zero;

        DebugEx.LogModule("CardSlotItem", $"卡牌状态已重置");
    }

    /// <summary>
    /// 对象销毁时清理所有资源和动画
    /// </summary>
    private void OnDestroy()
    {
        // 清理所有 DOTween 动画，防止访问已销毁对象
        DOTween.Kill(this);

        // 清理拖拽预览对象
        if (m_DragPreview != null)
        {
            m_DragPreviewTween?.Kill();
            Destroy(m_DragPreview);
            m_DragPreview = null;
        }

        DebugEx.LogModule("CardSlotItem", $"卡牌UI已销毁: {m_CardData?.Name ?? "unknown"}");
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新UI
    /// </summary>
    private void RefreshUI()
    {
        if (m_CardData == null)
        {
            DebugEx.WarningModule("CardSlotItem", "卡牌数据为空");
            return;
        }

        // 仅在首次设置时保存 Btn 的原始位置（避免重复刷新时被覆盖）
        if (varBtn != null && m_BtnOriginalPosition == Vector3.zero)
        {
            var btnRectTransform = varBtn.GetComponent<RectTransform>();
            if (btnRectTransform != null)
            {
                m_BtnOriginalPosition = btnRectTransform.anchoredPosition;
                DebugEx.LogModule("CardSlotItem", $"保存 Btn 原始位置: {m_BtnOriginalPosition}");
            }

            // 获取或添加 CanvasGroup 用于控制透明度
            m_BtnCanvasGroup = varBtn.GetComponent<CanvasGroup>();
            if (m_BtnCanvasGroup == null)
            {
                m_BtnCanvasGroup = varBtn.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 绑定按钮事件（仅用于交互监听）
        if (varBtn != null)
        {
            varBtn.onClick.RemoveAllListeners();
            varBtn.onClick.AddListener(OnCardClicked);
        }

        // 加载卡牌图标到 varCardImg
        if (varCardImg != null && m_CardData.IconId > 0)
        {
            _ = GameExtension.ResourceExtension.LoadSpriteAsync(m_CardData.IconId, varCardImg);
        }

        // 显示卡牌信息（如果 Variables 中有这些字段）
        RefreshCardInfo();

        DebugEx.LogModule("CardSlotItem", $"设置卡牌数据: {m_CardData.Name}");
    }

    /// <summary>
    /// 刷新卡牌信息显示
    /// </summary>
    private void RefreshCardInfo()
    {
        if (m_CardData == null)
            return;

        // 显示卡牌名称
        if (varNameText != null)
        {
            varNameText.text = m_CardData.Name;
        }

        // 显示卡牌描述
        if (varDecsText != null)
        {
            varDecsText.text = m_CardData.Desc;
        }

        // 显示卡牌故事文本
        if (varStoryText != null)
        {
            varStoryText.text = m_CardData.StoryText;
        }

        // 显示灵力消耗
        if (varCost != null)
        {
            varCost.text = m_CardData.SpiritCost.ToString();
        }

        DebugEx.LogModule(
            "CardSlotItem",
            $"刷新卡牌信息: {m_CardData.Name}, 灵力: {m_CardData.SpiritCost}"
        );
    }

    #endregion

    #region 销毁动画

    /// <summary>
    /// 播放销毁动画并回收到对象池
    /// 关键：RemoveCard 和 销毁动画 并行执行，提升用户体验
    /// </summary>
    private async UniTaskVoid PlayDestroyAnimationAndRemoveAsync()
    {
        // 保存必要信息
        int cardId = m_CardData?.CardId ?? -1;
        string cardName = m_CardData?.Name ?? "unknown";

        // ==================== 立即执行（不等待） ====================
        // 第1步：从 Container 中移除卡牌 → 立即启动重排
        // 这样其他卡牌会立即开始向中心靠拢
        if (m_Container != null)
        {
            m_Container.RemoveCard(this);
            DebugEx.LogModule("CardSlotItem", $"[立即] 从容器移除卡牌，启动重排: {cardName}");
        }

        // 第2步：通知 CardManager 移除卡牌数据
        if (CardManager.Instance != null && cardId > 0)
        {
            CardManager.Instance.RemoveCard(cardId);
            DebugEx.LogModule("CardSlotItem", $"[立即] 从 CardManager 移除卡牌: {cardName}");
        }

        // ==================== 并行执行：销毁动画 ====================
        // 第3步：播放销毁动画（同时，其他卡牌在重排，形成流畅的视觉效果）
        await PlayDestroyAnimationAsync();
        DebugEx.LogModule("CardSlotItem", $"[动画完成] 销毁动画播放完成: {cardName}");

        // ==================== 后续清理（异步进行） ====================
        // 第4步：等待容器重排完成，然后归还到对象池
        if (m_Container != null)
        {
            try
            {
                await m_Container.WaitForLatestRearrangeAsync();
                DebugEx.LogModule("CardSlotItem", $"[后台] 容器重排完成: {cardName}");
            }
            catch (OperationCanceledException)
            {
                DebugEx.LogModule("CardSlotItem", $"[后台] 重排被取消: {cardName}");
            }
        }

        // 第5步：归还到对象池
        if (CardSlotItemPool.Instance != null)
        {
            CardSlotItemPool.Instance.ReturnCard(this);
            DebugEx.LogModule("CardSlotItem", $"[完成] 卡牌已归还到对象池: {cardName}");
        }
    }

    /// <summary>
    /// 播放卡牌销毁动画（淡出 + 缩小）
    /// </summary>
    private async UniTask PlayDestroyAnimationAsync()
    {
        if (varBtn == null)
            return;

        var btnImage = varBtn.GetComponent<Image>();
        var btnTransform = varBtn.transform;

        if (btnImage != null)
        {
            // 销毁动画：0.1 秒内透明度从 1 变为 0，同时缩小到 0.5
            var sequence = DOTween.Sequence();
            sequence.Append(btnImage.DOFade(0f, 0.1f).SetEase(Ease.InQuad));
            sequence.Join(btnTransform.DOScale(Vector3.one * 0.5f, 0.1f).SetEase(Ease.InQuad));

            await sequence.AsyncWaitForCompletion();
        }

        DebugEx.LogModule("CardSlotItem", $"卡牌销毁动画完成: {m_CardData?.Name ?? "unknown"}");
    }

    #endregion

    #region 选中交互

    /// <summary>
    /// 卡牌点击回调
    /// </summary>
    private void OnCardClicked()
    {
        if (m_CardData == null)
            return;

        if (m_IsSelected)
        {
            // 取消选中
            DeselectCard();
        }
        else
        {
            // 选中卡牌
            SelectCard();
        }
    }

    /// <summary>
    /// 选中卡牌
    /// </summary>
    private void SelectCard()
    {
        m_IsSelected = true;

        // 取消场景中选中的棋子（点策略卡时应取消棋子选中）
        ChessSelectionManager.Instance?.ForceDeselect();

        // 实现单选逻辑：取消之前选中的卡牌
        if (CardManager.Instance != null && CardManager.Instance.CurrentSelectedCard != null)
        {
            var previousCard = CardManager.Instance.CurrentSelectedCard;
            // 查找之前选中卡牌对应的 UI 并取消选中
            var previousSlot = FindCardSlotByCardData(previousCard);
            if (previousSlot != null && previousSlot != this)
            {
                previousSlot.DeselectCard();
            }
        }

        // 更新 CardManager 的选中状态
        if (CardManager.Instance != null)
        {
            CardManager.Instance.CurrentSelectedCard = m_CardData;
        }

        // 在基准坐标基础上，向上偏移 20 单位
        var itemRectTransform = GetComponent<RectTransform>();
        if (itemRectTransform != null)
        {
            var targetPosition = m_BaseAnchoredPos + Vector2.up * SELECTED_OFFSET;

            // 杀死之前的动画
            m_SelectTween?.Kill();

            // 播放选中动画：移动 + 脉冲
            m_SelectTween = itemRectTransform
                .DOAnchorPos(targetPosition, 0.3f)
                .SetEase(Ease.OutQuad);
            PlayPulseAnimation();

            DebugEx.LogModule(
                "CardSlotItem",
                $"选中卡牌: {m_CardData.Name}, 目标位置: {targetPosition}"
            );
        }

        // 显示 DetailInfoUI 并播放滑入动画
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI != null)
        {
            var detailUI = combatUI.GetDetailInfoUI();
            if (detailUI != null)
            {
                detailUI.SetData(m_CardData);
                detailUI.RefreshUI();
                detailUI.ShowWithAnimation();
            }
        }
    }

    /// <summary>
    /// 取消选中卡牌
    /// </summary>
    private void DeselectCard()
    {
        m_IsSelected = false;

        // 恢复到基准坐标
        var itemRectTransform = GetComponent<RectTransform>();
        if (itemRectTransform != null)
        {
            // 杀死之前的动画
            m_SelectTween?.Kill();

            // 恢复到基准位置
            itemRectTransform.DOAnchorPos(m_BaseAnchoredPos, 0.3f).SetEase(Ease.OutQuad);
            DebugEx.LogModule(
                "CardSlotItem",
                $"取消选中卡牌: {m_CardData.Name}, 恢复位置: {m_BaseAnchoredPos}"
            );
        }

        // 隐藏 DetailInfoUI
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI != null)
        {
            var detailUI = combatUI.GetDetailInfoUI();
            if (detailUI != null)
            {
                detailUI.gameObject.SetActive(false);
            }
        }

        // 更新 CardManager 的选中状态
        if (CardManager.Instance != null && CardManager.Instance.CurrentSelectedCard == m_CardData)
        {
            CardManager.Instance.CurrentSelectedCard = null;
        }
    }

    /// <summary>
    /// 查找卡牌对应的 UI 槽
    /// </summary>
    private CardSlotItem FindCardSlotByCardData(CardData cardData)
    {
        if (cardData == null)
            return null;

        var allSlots = FindObjectsOfType<CardSlotItem>();
        foreach (var slot in allSlots)
        {
            if (slot.m_CardData == cardData)
            {
                return slot;
            }
        }

        return null;
    }

    #endregion

    #region 拖拽交互

    private bool m_IsDragging;  // 正在拖拽标志
    private bool m_HasLeftGreenArea;  // 拖拽后是否已离开过 GreenArea

    /// <summary>
    /// 开始拖拽
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_CardData == null)
            return;

        m_IsDragging = true;
        m_HasLeftGreenArea = false;

        DebugEx.LogModule("CardSlotItem",
            $"[拖拽开始] 卡牌: {m_CardData.Name}, 鼠标位置: {eventData.position}");

        // 取消选中状态（如果已选中）
        if (m_IsSelected)
        {
            DeselectCard();
        }

        // 停止所有位置相关的动画，避免干扰拖拽
        m_SelectTween?.Kill();
        m_PositionTween?.Kill();

        // 拖拽时卡牌变暗
        if (m_BtnCanvasGroup != null)
        {
            m_BtnCanvasGroup.DOFade(DRAG_ALPHA, 0.2f).SetEase(Ease.OutQuad);
        }

        // 创建拖拽预览对象
        CreateDragPreview();

        // 立即更新卡牌位置，跟随鼠标（避免延迟）
        Vector2 screenPos = eventData.position;
        if (m_ItemRectTransform != null)
        {
            var canvasRectTransform = m_ItemRectTransform.parent as RectTransform;
            var canvas = GetComponentInParent<Canvas>();

            if (canvasRectTransform != null && canvas != null)
            {
                // 获取卡牌尺寸，计算右下角位置
                Vector2 cardSize = m_ItemRectTransform.sizeDelta;
                Vector2 cardRightBottomOffset = new Vector2(cardSize.x / 2f, -cardSize.y / 2f);

                // 计算所需的卡牌锚点位置
                Vector2 canvasSize = canvasRectTransform.sizeDelta;
                Vector2 targetCardAnchoredPos = screenPos - cardRightBottomOffset - new Vector2(canvasSize.x / 2f, canvasSize.y / 2f);
                m_ItemRectTransform.anchoredPosition = targetCardAnchoredPos;

                DebugEx.LogModule("CardSlotItem",
                    $"[拖拽开始] 鼠标屏幕: {screenPos:F2}, 卡牌尺寸: {cardSize:F2}, 右下角偏移: {cardRightBottomOffset:F2}, 目标锚点: {targetCardAnchoredPos:F2}");
            }

            // 旋转回正（取消扇形布局的旋转）
            m_ItemRectTransform.localRotation = Quaternion.identity;
            DebugEx.LogModule("CardSlotItem", "[拖拽开始] 卡牌旋转已重置为正常");
        }

        // 通知容器拖拽开始
        m_Container?.OnCardBeginDrag(this);
    }

    /// <summary>
    /// 拖拽中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        DebugEx.LogModule("CardSlotItem", $"[OnDrag] 鼠标位置={eventData.position}");

        if (m_DragPreview == null)
            return;

        // 屏幕坐标
        Vector2 screenPos = eventData.position;

        // 计算卡牌位置，使卡牌右下角坐标与鼠标屏幕坐标一致
        if (m_ItemRectTransform != null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform parentRect = m_ItemRectTransform.parent as RectTransform;
                if (parentRect != null &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRect, screenPos, canvas.worldCamera, out Vector2 mouseLocalPos))
                {
                    // 当前卡牌右下角在父级本地坐标系中的位置
                    Vector3[] corners = new Vector3[4];
                    m_ItemRectTransform.GetWorldCorners(corners);
                    // corners[3] = 右下角（世界坐标）
                    Vector2 currentRBLocal = parentRect.InverseTransformPoint(corners[3]);

                    // 偏移 = 鼠标目标位置 - 当前右下角位置（都在父级本地坐标系）
                    Vector2 delta = mouseLocalPos - currentRBLocal;
                    m_ItemRectTransform.anchoredPosition += delta;
                }
            }
        }


        // 更新拖拽预览位置，跟随鼠标
        var dragPreviewRect = m_DragPreview.GetComponent<RectTransform>();
        if (dragPreviewRect != null)
        {
            // Screen Space - Camera 模式：屏幕坐标 → anchoredPosition
            var canvasRectTransform = dragPreviewRect.parent as RectTransform;
            if (canvasRectTransform != null)
            {
                Vector2 canvasSize = canvasRectTransform.sizeDelta;
                Vector2 anchoredPos = screenPos - new Vector2(canvasSize.x / 2f, canvasSize.y / 2f);
                dragPreviewRect.anchoredPosition = anchoredPos;
            }
        }

        // 限制射线检测频率（0.1 秒/次）
        if (Time.time - m_LastRaycastTime >= RAYCAST_INTERVAL)
        {
            m_LastRaycastTime = Time.time;
            PerformRaycast();
        }

        // 检测区域并更新预览（优先级：吸附区 > 无效区 > 战场区）
        bool isInGreenAreaRaw = IsPositionInAdsorptionArea(eventData.position);
        bool isInInvalidArea = IsPositionInInvalidArea(eventData.position);

        // 拖拽后必须先离开 GreenArea 一次，再回来才算"吸附"
        if (!m_HasLeftGreenArea && !isInGreenAreaRaw)
        {
            m_HasLeftGreenArea = true;
        }
        bool isInRetractArea = m_HasLeftGreenArea && isInGreenAreaRaw;

        UpdateAreaHighlight(isInRetractArea, isInInvalidArea);

        // 通知容器拖拽中的位置
        m_Container?.OnCardDrag(this, eventData.position);

        // ⭐ 新增：显示卡牌预览
        UpdateCardPreview(eventData.position);
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_CardData == null)
            return;

        m_IsDragging = false;

        DebugEx.LogModule("CardSlotItem", $"结束拖拽卡牌: {m_CardData.Name}");

        // 清除策略卡目标描边
        ClearCardTargetOutlines();

        // ⭐ 新增：隐藏蓝色圆形预览
        CardPreviewDisplayShader.Instance?.HideAll();

        // 恢复卡牌透明度
        if (m_BtnCanvasGroup != null)
        {
            m_BtnCanvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
        }

        // 销毁拖拽预览（先清理动画，再销毁对象）
        if (m_DragPreview != null)
        {
            m_DragPreviewTween?.Kill();
            Destroy(m_DragPreview);
            m_DragPreview = null;
            m_DragPreviewTween = null;
        }

        // 判断释放位置：只有离开过 GreenArea 后回来才算吸附区
        bool isInGreenAreaRaw = IsPositionInAdsorptionArea(eventData.position);
        bool isInRetractArea = m_HasLeftGreenArea && isInGreenAreaRaw;
        bool isInInvalidArea = IsPositionInInvalidArea(eventData.position);

        // 重置标记
        m_HasLeftGreenArea = false;

        if (isInRetractArea || isInInvalidArea)
        {
            // 吸附区或无效区：返回卡槽
            DebugEx.LogModule("CardSlotItem",
                $"释放位置在保留/无效区 (吸附={isInRetractArea}, 无效={isInInvalidArea})，卡牌返回卡槽");
            ReturnToSlot();
            // 通知容器拖拽结束（true 表示返回卡槽）
            if (m_Container != null)
                m_Container.OnCardEndDrag(this, true);
        }
        else
        {
            // 战场区域：执行卡牌效果
            DebugEx.LogModule("CardSlotItem", $"释放位置在战场，执行卡牌效果");
            ExecuteCardEffect(GetWorldPosFromScreen(eventData.position));
            // 注意：不调用 OnCardEndDrag，重排由 PlayDestroyAnimationAndExecuteAsync 中的 RemoveCard 管理
        }

        // 隐藏区域高亮显示
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI != null)
        {
            var greenArea = combatUI.GetCardSlotAdsorptionArea();
            var redArea = combatUI.GetInvalidAreaPreview();
            if (greenArea != null)
                greenArea.color = new Color(greenArea.color.r, greenArea.color.g, greenArea.color.b, 0f);
            if (redArea != null)
                redArea.color = new Color(redArea.color.r, redArea.color.g, redArea.color.b, 0f);
            DebugEx.LogModule("CardSlotItem", "拖拽结束，隐藏区域高亮");
        }
    }

    /// <summary>
    /// 创建拖拽预览对象
    /// </summary>
    private void CreateDragPreview()
    {
        // 显式找到 Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            DebugEx.ErrorModule("CardSlotItem", "找不到Canvas，无法创建拖拽预览");
            return;
        }

        // 创建预览对象，放到 Canvas 下
        m_DragPreview = new GameObject("CardDragPreview");
        m_DragPreview.transform.SetParent(canvas.transform);
        m_DragPreview.transform.localScale = Vector3.one;

        // 添加 Image 组件
        m_DragPreviewImage = m_DragPreview.AddComponent<Image>();
        m_DragPreviewImage.raycastTarget = false;

        // 添加 CanvasGroup 用于控制透明度
        m_DragPreviewCanvasGroup = m_DragPreview.AddComponent<CanvasGroup>();
        m_DragPreviewCanvasGroup.alpha = 0.7f;

        // 复制卡牌图标
        if (varBtn != null)
        {
            var btnImage = varBtn.GetComponent<Image>();
            if (btnImage != null)
            {
                m_DragPreviewImage.sprite = btnImage.sprite;
                m_DragPreviewImage.color = btnImage.color;
            }
        }

        // 设置预览大小
        var rectTransform = m_DragPreview.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        // 将预览对象设置为最后一个子对象，确保在最上层显示且不挡住交互
        m_DragPreview.transform.SetAsLastSibling();

        // 播放预览旋转动画（轻微旋转）
        var previewTransform = m_DragPreview.transform;
        m_DragPreviewTween?.Kill();
        m_DragPreviewTween = previewTransform
            .DORotate(new Vector3(0, 0, PREVIEW_ROTATION), 0.5f)
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo);

        DebugEx.LogModule("CardSlotItem", "拖拽预览对象已创建");
    }

    /// <summary>
    /// 执行射线检测
    /// </summary>
    private void PerformRaycast()
    {
        // 这里可以添加更复杂的射线检测逻辑
        // 例如检测是否在特定区域上方
    }

    /// <summary>
    /// 检查位置是否在无效区域（红色区域内）
    /// </summary>
    private bool IsPositionInInvalidArea(Vector3 screenPosition)
    {
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI == null)
            return false;

        var invalidAreaImage = combatUI.GetInvalidAreaPreview();
        if (invalidAreaImage == null)
            return false;

        var rectTransform = invalidAreaImage.GetComponent<RectTransform>();
        if (rectTransform == null)
            return false;

        var canvas = GetComponentInParent<Canvas>();
        Camera canvasCamera = canvas != null ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            screenPosition,
            canvasCamera
        );
    }

    /// <summary>
    /// 检查位置是否在卡槽吸附区域（绿色矩形区域）
    /// </summary>
    private bool IsPositionInAdsorptionArea(Vector3 screenPosition)
    {
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI == null)
            return false;

        var canvas = GetComponentInParent<Canvas>();
        Camera canvasCamera = canvas != null ? canvas.worldCamera : null;

        // 获取绿色矩形区域的 Image（吸附区域）
        var retractAreaImage = combatUI.GetCardSlotAdsorptionArea();
        if (retractAreaImage == null)
        {
            return false;
        }

        bool result = RectTransformUtility.RectangleContainsScreenPoint(
                retractAreaImage.rectTransform,
                screenPosition,
                canvasCamera
            );
        DebugEx.LogModule("CardSlotItem",
            $"[IsPositionInAdsorptionArea] 使用 GetCardSlotAdsorptionArea | 鼠标={screenPosition} | 结果={result}");
        return result;
    }

    /// <summary>
    /// 执行卡牌效果
    /// </summary>
    private void ExecuteCardEffect(Vector3 releasePosition)
    {
        if (m_CardData == null)
        {
            DebugEx.ErrorModule("CardSlotItem", "卡牌数据为空，无法执行效果");
            return;
        }

        // 灵力消耗检查
        float spiritCost = m_CardData.SpiritCost;
        if (spiritCost > 0)
        {
            bool consumed = SummonerRuntimeDataManager.Instance.ConsumeMP(spiritCost);
            if (!consumed)
            {
                DebugEx.LogModule("CardSlotItem", $"灵力不足，无法使用卡牌: {m_CardData.Name} (需要 {spiritCost})");
                ReturnToSlot();
                return;
            }
        }

        DebugEx.LogModule("CardSlotItem", $"执行卡牌效果: {m_CardData.Name}");

        // 播放卡牌使用闪光效果
        PlayFlashEffect();

        // 创建临时 CardEffectExecutor 执行卡牌效果（参考 Buff 的动态创建方式）
        var executor = new GameObject("CardEffectExecutor_Temp").AddComponent<CardEffectExecutor>();
        if (executor != null)
        {
            executor.ExecuteCardEffect(m_CardData, releasePosition);
            // 执行完毕后销毁临时对象
            Destroy(executor.gameObject);
        }
        else
        {
            DebugEx.ErrorModule("CardSlotItem", "无法创建 CardEffectExecutor");
        }

        // 播放销毁动画并自管理销毁流程
        PlayDestroyAnimationAndRemoveAsync().Forget();
    }

    /// <summary>
    /// 返回卡槽
    /// </summary>
    private void ReturnToSlot()
    {
        DebugEx.LogModule("CardSlotItem", $"卡牌返回卡槽: {m_CardData.Name}");

        // 恢复到基准位置
        if (m_ItemRectTransform != null)
        {
            m_ItemRectTransform.DOAnchorPos(m_BaseAnchoredPos, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    #endregion

    #region 动效方法

    /// <summary>
    /// 播放脉冲动画（选中时）
    /// </summary>
    private void PlayPulseAnimation()
    {
        if (varBtn == null)
            return;

        var btnTransform = varBtn.transform;

        // 杀死之前的脉冲动画
        m_HoverTween?.Kill();

        // 脉冲序列：1.0 → 1.1 → 1.0
        var sequence = DOTween.Sequence();
        sequence.Append(
            btnTransform
                .DOScale(new Vector3(PULSE_SCALE, PULSE_SCALE, 1f), PULSE_DURATION * 0.5f)
                .SetEase(Ease.OutQuad)
        );
        sequence.Append(
            btnTransform.DOScale(Vector3.one, PULSE_DURATION * 0.5f).SetEase(Ease.InQuad)
        );

        m_HoverTween = sequence;
        DebugEx.LogModule("CardSlotItem", $"播放脉冲动画: {m_CardData.Name}");
    }

    /// <summary>
    /// 播放悬停动画（鼠标进入时）
    /// </summary>
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // 拖拽中禁止处理悬停事件
        if (m_IsDragging)
            return;

        if (m_IsSelected || varBtn == null)
            return;

        var btnTransform = varBtn.transform;
        var itemRectTransform = GetComponent<RectTransform>();

        // 杀死之前的悬停动画
        m_HoverTween?.Kill();
        m_PositionTween?.Kill();

        // 缩放到 1.05
        m_HoverTween = btnTransform
            .DOScale(new Vector3(HOVER_SCALE, HOVER_SCALE, 1f), HOVER_DURATION)
            .SetEase(Ease.OutQuad);

        // 位置上移（基于基准位置的偏移）
        if (itemRectTransform != null)
        {
            var targetPos = m_BaseAnchoredPos + Vector2.up * 30f;
            m_PositionTween = itemRectTransform
                .DOAnchorPos(targetPos, HOVER_DURATION)
                .SetEase(Ease.OutQuad);
        }

        DebugEx.LogModule("CardSlotItem", $"悬停放大: {m_CardData.Name}");
    }

    /// <summary>
    /// 取消悬停动画（鼠标离开时）
    /// </summary>
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // 拖拽中禁止处理悬停事件
        if (m_IsDragging)
            return;

        if (m_IsSelected || varBtn == null)
            return;

        var btnTransform = varBtn.transform;
        var itemRectTransform = GetComponent<RectTransform>();

        // 杀死之前的悬停动画
        m_HoverTween?.Kill();
        m_PositionTween?.Kill();

        // 恢复到 1.0
        m_HoverTween = btnTransform.DOScale(Vector3.one, HOVER_DURATION).SetEase(Ease.OutQuad);

        // 位置恢复到基准位置
        if (itemRectTransform != null)
        {
            m_PositionTween = itemRectTransform
                .DOAnchorPos(m_BaseAnchoredPos, HOVER_DURATION)
                .SetEase(Ease.OutQuad);
        }

        DebugEx.LogModule("CardSlotItem", $"悬停缩小: {m_CardData.Name}");
    }

    /// <summary>
    /// 播放卡牌使用闪光效果
    /// </summary>
    private void PlayFlashEffect()
    {
        if (varBtn == null)
            return;

        var btnImage = varBtn.GetComponent<Image>();
        if (btnImage == null)
            return;

        // 保存原始颜色
        var originalColor = btnImage.color;

        // 闪光序列：白色闪烁 → 恢复原色
        var sequence = DOTween.Sequence();
        sequence.Append(btnImage.DOColor(Color.white, FLASH_DURATION * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(
            btnImage.DOColor(originalColor, FLASH_DURATION * 0.5f).SetEase(Ease.InQuad)
        );

        DebugEx.LogModule("CardSlotItem", $"播放闪光效果: {m_CardData.Name}");
    }

    /// <summary>
    /// 更新区域高亮状态（GreenArea 和 RedArea）
    /// </summary>
    /// <summary>
    /// 更新区域高亮显示
    /// 优先级：吸附区(绿色) > 无效区(红色) > 战场(隐藏)
    /// </summary>
    private void UpdateAreaHighlight(bool isInRetractArea, bool isInInvalidArea)
    {
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI == null)
        {
            DebugEx.WarningModule("CardSlotItem", "无法获取 CombatUI，区域预览无法更新");
            return;
        }

        var greenArea = combatUI.GetCardSlotAdsorptionArea();
        var redArea = combatUI.GetInvalidAreaPreview();

        if (greenArea == null || redArea == null)
        {
            DebugEx.WarningModule("CardSlotItem", $"区域预览对象为空: greenArea={greenArea}, redArea={redArea}");
            return;
        }

        // 优先级：吸附区 > 无效区 > 战场
        if (isInRetractArea)
        {
            // 在吸附区：显示绿色，隐藏红色
            Color newGreenColor = new Color(0.165f, 1f, 0.352f, 0.7f);
            greenArea.color = newGreenColor;
            redArea.color = new Color(redArea.color.r, redArea.color.g, redArea.color.b, 0f);

            DebugEx.LogModule("CardSlotItem",
                $"✓ 显示 GreenArea (吸附区) | 设置颜色={newGreenColor}");
        }
        else if (isInInvalidArea)
        {
            // 在无效区：显示红色，隐藏绿色
            greenArea.color = new Color(greenArea.color.r, greenArea.color.g, greenArea.color.b, 0f);
            Color newRedColor = new Color(1f, 0f, 0f, 0.7f);
            redArea.color = newRedColor;

            DebugEx.LogModule("CardSlotItem",
                $"✓ 显示 RedArea (无效区) | 设置颜色={newRedColor}");
        }
        else
        {
            // 在战场：隐藏所有预览
            greenArea.color = new Color(greenArea.color.r, greenArea.color.g, greenArea.color.b, 0f);
            redArea.color = new Color(redArea.color.r, redArea.color.g, redArea.color.b, 0f);
            DebugEx.LogModule("CardSlotItem", "→ 在战场区域，隐藏所有预览");
        }
    }

    #endregion

    #region 卡牌预览

    /// <summary>
    /// 更新卡牌预览显示（蓝色圆形只在战场显示）
    /// </summary>
    private void UpdateCardPreview(Vector3 screenPos)
    {
        if (m_CardData == null || m_CardData.TableRow == null || CardPreviewDisplayShader.Instance == null)
            return;

        // 战场区域 = 不在吸附区 且 不在无效区
        bool isInGreenAreaRaw = IsPositionInAdsorptionArea(screenPos);
        bool isInRetractArea = m_HasLeftGreenArea && isInGreenAreaRaw;
        bool isInInvalidArea = IsPositionInInvalidArea(screenPos);
        bool isInBattle = !isInRetractArea && !isInInvalidArea;

        if (isInBattle)
        {
            // 战场区域：显示蓝色作用范围
            Vector3 worldPos = GetWorldPosFromScreen(screenPos);
            float radius = m_CardData.TableRow.AreaRadius;
            CardPreviewDisplayShader.Instance.ShowActionPreview(worldPos, radius);

            // 更新目标描边
            UpdateTargetOutlines(worldPos);
        }
        else
        {
            // 非战场区域：隐藏蓝色圆形 + 清除目标描边
            CardPreviewDisplayShader.Instance.HideActionPreview();
            ClearCardTargetOutlines();
        }
    }

    /// <summary>
    /// 将屏幕坐标转换为世界坐标
    /// </summary>
    private Vector3 GetWorldPosFromScreen(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // 检测战斗场景的地面（Y = 0 平面）
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }

        // 如果射线检测失败，假设击中 Y = 0 平面
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.origin + ray.direction * distance;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 更新策略卡目标描边（增量更新）
    /// </summary>
    private void UpdateTargetOutlines(Vector3 worldPos)
    {
        var newTargets = GetAffectedTargets(m_CardData, worldPos);

        // 移除不再是目标的描边
        for (int i = m_PreviewTargets.Count - 1; i >= 0; i--)
        {
            var old = m_PreviewTargets[i];
            if (old == null || !newTargets.Contains(old))
            {
                if (old != null && old.OutlineController != null)
                {
                    old.OutlineController.HideOutline();
                }
                m_PreviewTargets.RemoveAt(i);
            }
        }

        // 添加新目标的描边
        Color allyColor = Color.green;
        Color enemyColor = Color.red;
        float outlineSize = OutlineController.DefaultSize;

        foreach (var target in newTargets)
        {
            if (target == null || target.OutlineController == null)
                continue;

            if (!m_PreviewTargets.Contains(target))
            {
                m_PreviewTargets.Add(target);
            }

            // 根据阵营决定颜色
            Color color = target.Camp == (int)CampType.Player ? allyColor : enemyColor;
            target.OutlineController.ShowOutline(color, outlineSize);
        }
    }

    /// <summary>
    /// 清除所有策略卡目标描边，并恢复选中棋子的描边
    /// </summary>
    private void ClearCardTargetOutlines()
    {
        foreach (var target in m_PreviewTargets)
        {
            if (target != null && target.OutlineController != null)
            {
                target.OutlineController.HideOutline();
            }
        }
        m_PreviewTargets.Clear();

        // 恢复选中棋子的黄色描边
        RestoreSelectionOutline();
    }

    /// <summary>
    /// 恢复当前选中棋子的选中描边
    /// </summary>
    private void RestoreSelectionOutline()
    {
        var selected = ChessSelectionManager.Instance?.SelectedChess;
        if (selected != null && selected.OutlineController != null)
        {
            selected.OutlineController.ShowOutline(new Color(1f, 0.85f, 0f), OutlineController.DefaultSize);
        }
    }

    /// <summary>
    /// 获取卡牌作用的目标列表
    /// </summary>
    private List<ChessEntity> GetAffectedTargets(CardData cardData, Vector3 targetPos)
    {
        var targets = new List<ChessEntity>();
        var allChess = BattleChessManager.Instance?.GetAllChessEntities();

        if (allChess == null || allChess.Count == 0)
            return targets;

        float radius = cardData.TableRow.AreaRadius;
        CardTargetType targetType = cardData.CTargetType;

        switch (targetType)
        {
            case CardTargetType.Self: // 自身（召唤师）— 暂用最近友方代替
            {
                ChessEntity nearest = null;
                float nearestDist = float.MaxValue;
                foreach (var chess in allChess)
                {
                    if (chess == null || chess.Camp != (int)CampType.Player)
                        continue;
                    float dist = Vector3.Distance(chess.transform.position, targetPos);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = chess;
                    }
                }
                if (nearest != null)
                    targets.Add(nearest);
                break;
            }
            case CardTargetType.AllAllyExcludeSummoner: // 全体友方（不含召唤师）
            case CardTargetType.AllAlly: // 全体友方
            {
                foreach (var chess in allChess)
                {
                    if (chess != null && chess.Camp == (int)CampType.Player)
                        targets.Add(chess);
                }
                break;
            }
            case CardTargetType.AllEnemy: // 敌方全体
            {
                foreach (var chess in allChess)
                {
                    if (chess != null && chess.Camp == (int)CampType.Enemy)
                        targets.Add(chess);
                }
                break;
            }
            case CardTargetType.SingleAlly: // 单体友方（最近友方）
            {
                ChessEntity nearest = null;
                float nearestDist = float.MaxValue;
                foreach (var chess in allChess)
                {
                    if (chess == null || chess.Camp != (int)CampType.Player)
                        continue;
                    float dist = Vector3.Distance(chess.transform.position, targetPos);
                    if (dist <= radius && dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = chess;
                    }
                }
                if (nearest != null)
                    targets.Add(nearest);
                break;
            }
            case CardTargetType.SingleEnemy: // 单体敌方（最近敌方）
            {
                ChessEntity nearest = null;
                float nearestDist = float.MaxValue;
                foreach (var chess in allChess)
                {
                    if (chess == null || chess.Camp != (int)CampType.Enemy)
                        continue;
                    float dist = Vector3.Distance(chess.transform.position, targetPos);
                    if (dist <= radius && dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = chess;
                    }
                }
                if (nearest != null)
                    targets.Add(nearest);
                break;
            }
            case CardTargetType.AreaAlly: // 范围内友方
            {
                foreach (var chess in allChess)
                {
                    if (chess != null && chess.Camp == (int)CampType.Player)
                    {
                        float dist = Vector3.Distance(chess.transform.position, targetPos);
                        if (dist <= radius)
                            targets.Add(chess);
                    }
                }
                break;
            }
            case CardTargetType.AreaEnemy: // 范围内敌方
            {
                foreach (var chess in allChess)
                {
                    if (chess != null && chess.Camp == (int)CampType.Enemy)
                    {
                        float dist = Vector3.Distance(chess.transform.position, targetPos);
                        if (dist <= radius)
                            targets.Add(chess);
                    }
                }
                break;
            }
        }

        return targets;
    }

    #endregion
}
