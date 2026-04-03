using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using UnityEngine.EventSystems;

public partial class CardSlotItem : UIItemBase, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region 字段

    private CardData m_CardData;
    private bool m_IsSelected;
    private Vector3 m_BtnOriginalPosition;
    private const float SELECTED_OFFSET = 20f;

    // 扇形容器相关
    private CardSlotContainer m_Container;
    private Vector2 m_BaseAnchoredPos;      // Container 分配的基准位置
    private float m_BaseRotZ;               // 基准旋转

    // 拖拽相关字段
    private GameObject m_DragPreview;
    private Image m_DragPreviewImage;
    private CanvasGroup m_DragPreviewCanvasGroup;
    private Vector3 m_DragStartPosition;
    private float m_LastRaycastTime;
    private const float RAYCAST_INTERVAL = 0.1f;

    // 动效相关字段
    private Tween m_SelectTween;
    private Tween m_HoverTween;
    private Tween m_DragPreviewTween;
    private Tween m_PositionTween;         // 位置动画（悬停上移）
    private CanvasGroup m_BtnCanvasGroup;
    private RectTransform m_ItemRectTransform;
    private const float HOVER_SCALE = 1.05f;
    private const float HOVER_DURATION = 0.2f;
    private const float HOVER_OFFSET_Y = 30f;  // 悬停上移距离
    private const float PULSE_SCALE = 1.1f;
    private const float PULSE_DURATION = 0.3f;
    private const float DRAG_ALPHA = 0.5f;
    private const float PREVIEW_ROTATION = 5f;
    private const float FLASH_DURATION = 0.2f;
    private const float FLASH_ALPHA = 1.5f;

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
    /// 重置卡牌状态（战斗结束时调用）
    /// </summary>
    public void ResetState()
    {
        m_IsSelected = false;
        m_Container = null;
        m_BaseAnchoredPos = Vector2.zero;
        m_BaseRotZ = 0f;

        // 杀死所有动画
        m_SelectTween?.Kill();
        m_HoverTween?.Kill();
        m_PositionTween?.Kill();
        m_DragPreviewTween?.Kill();

        DebugEx.LogModule("CardSlotItem", $"卡牌状态已重置: {m_CardData?.Name ?? "unknown"}");
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

        // 绑定按钮事件
        if (varBtn != null)
        {
            varBtn.onClick.RemoveAllListeners();
            varBtn.onClick.AddListener(OnCardClicked);

            // 加载卡牌图标到 Btn 的 Image 组件
            var btnImage = varBtn.GetComponent<Image>();
            if (btnImage != null && m_CardData.IconId > 0)
            {
                _ = GameExtension.ResourceExtension.LoadSpriteAsync(m_CardData.IconId, btnImage);
            }
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
        // TODO: 当 CardSlotItem.Variables 中添加了以下字段后，取消注释
        // if (varNameText != null)
        // {
        //     varNameText.text = m_CardData.Name;
        // }
        //
        // if (varDescText != null)
        // {
        //     varDescText.text = m_CardData.Desc;
        // }
        //
        // if (varSpiritCostText != null)
        // {
        //     varSpiritCostText.text = $"灵力: {m_CardData.SpiritCost}";
        // }
    }

    #endregion

    #region 销毁动画

    /// <summary>
    /// 播放卡牌销毁动画（淡出 + 缩小）
    /// </summary>
    public async UniTaskVoid PlayDestroyAnimationAsync()
    {
        if (varBtn == null)
        {
            Destroy(gameObject);
            return;
        }

        var btnImage = varBtn.GetComponent<Image>();
        var btnTransform = varBtn.transform;
        
        if (btnImage != null)
        {
            // 销毁动画：0.3 秒内透明度从 1 变为 0，同时缩小到 0.5
            var sequence = DOTween.Sequence();
            sequence.Append(btnImage.DOFade(0f, 0.3f).SetEase(Ease.InQuad));
            sequence.Join(btnTransform.DOScale(Vector3.one * 0.5f, 0.3f).SetEase(Ease.InQuad));
            
            await UniTask.Delay(300);  // 等待 0.3 秒
        }

        // 销毁游戏对象
        Destroy(gameObject);
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
            m_SelectTween = itemRectTransform.DOAnchorPos(targetPosition, 0.3f).SetEase(Ease.OutQuad);
            PlayPulseAnimation();

            DebugEx.LogModule("CardSlotItem", $"选中卡牌: {m_CardData.Name}, 目标位置: {targetPosition}");
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
            DebugEx.LogModule("CardSlotItem", $"取消选中卡牌: {m_CardData.Name}, 恢复位置: {m_BaseAnchoredPos}");
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

    /// <summary>
    /// 开始拖拽
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("CardSlotItem", $"开始拖拽卡牌: {m_CardData.Name}");

        // 取消选中状态（如果已选中）
        if (m_IsSelected)
        {
            DeselectCard();
        }

        // 保存拖拽起始位置
        m_DragStartPosition = transform.localPosition;

        // 拖拽时卡牌变暗
        if (m_BtnCanvasGroup != null)
        {
            m_BtnCanvasGroup.DOFade(DRAG_ALPHA, 0.2f).SetEase(Ease.OutQuad);
        }

        // 创建拖拽预览对象
        CreateDragPreview();

        // 通知容器拖拽开始
        m_Container?.OnCardBeginDrag(this);
    }

    /// <summary>
    /// 拖拽中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (m_DragPreview == null)
            return;

        // 更新拖拽预览位置，跟随鼠标
        m_DragPreview.transform.position = eventData.position;

        // 限制射线检测频率（0.1 秒/次）
        if (Time.time - m_LastRaycastTime >= RAYCAST_INTERVAL)
        {
            m_LastRaycastTime = Time.time;
            PerformRaycast(eventData.position);
        }

        // 检测吸附区域并高亮
        bool isInAdsorptionArea = IsPositionInAdsorptionArea(eventData.position);
        UpdateAdsorptionAreaHighlight(isInAdsorptionArea);

        // 通知容器拖拽中的位置
        m_Container?.OnCardDrag(this, eventData.position);
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("CardSlotItem", $"结束拖拽卡牌: {m_CardData.Name}");

        // 恢复卡牌透明度
        if (m_BtnCanvasGroup != null)
        {
            m_BtnCanvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
        }

        // 销毁拖拽预览
        if (m_DragPreview != null)
        {
            Destroy(m_DragPreview);
            m_DragPreview = null;
        }

        // 通知容器拖拽结束
        m_Container?.OnCardEndDrag(this);

        // 判断释放位置
        bool isInBattleArea = IsPositionInBattleArea(eventData.position);
        bool isInAdsorptionArea = IsPositionInAdsorptionArea(eventData.position);

        if (isInBattleArea)
        {
            // 战场区域释放：执行卡牌效果
            ExecuteCardEffect(eventData.position);
        }
        else if (isInAdsorptionArea)
        {
            // 吸附区域释放：返回卡槽
            ReturnToSlot();
        }
        else
        {
            // 无效区域释放：返回卡槽
            ReturnToSlot();
        }
    }

    /// <summary>
    /// 创建拖拽预览对象
    /// </summary>
    private void CreateDragPreview()
    {
        // 创建预览对象
        m_DragPreview = new GameObject("CardDragPreview");
        m_DragPreview.transform.SetParent(transform.root);
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

        // 播放预览旋转动画（轻微旋转）
        var previewTransform = m_DragPreview.transform;
        m_DragPreviewTween?.Kill();
        m_DragPreviewTween = previewTransform.DORotate(new Vector3(0, 0, PREVIEW_ROTATION), 0.5f)
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo);

        DebugEx.LogModule("CardSlotItem", "拖拽预览对象已创建");
    }

    /// <summary>
    /// 执行射线检测
    /// </summary>
    private void PerformRaycast(Vector3 screenPosition)
    {
        // 这里可以添加更复杂的射线检测逻辑
        // 例如检测是否在特定区域上方
    }

    /// <summary>
    /// 检查位置是否在战场区域
    /// </summary>
    private bool IsPositionInBattleArea(Vector3 screenPosition)
    {
        // TODO: 根据实际战场区域的 RectTransform 进行检测
        // 这里简化处理：假设屏幕中心上方为战场区域
        return screenPosition.y > Screen.height * 0.4f;
    }

    /// <summary>
    /// 检查位置是否在卡槽吸附区域
    /// </summary>
    private bool IsPositionInAdsorptionArea(Vector3 screenPosition)
    {
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI == null)
            return false;

        // 获取吸附区域的 RectTransform
        var adsorptionArea = combatUI.GetCardSlotAdsorptionArea();
        if (adsorptionArea == null)
            return false;

        // 检查屏幕位置是否在吸附区域内
        return RectTransformUtility.RectangleContainsScreenPoint(
            adsorptionArea.rectTransform,
            screenPosition,
            null
        );
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

        // 播放销毁动画
        PlayDestroyAnimationAsync().Forget();

        // 从 CardManager 移除卡牌
        if (CardManager.Instance != null)
        {
            CardManager.Instance.RemoveCard(m_CardData.CardId);
        }
        else
        {
            DebugEx.WarningModule("CardSlotItem", "CardManager 为空");
        }
    }

    /// <summary>
    /// 返回卡槽
    /// </summary>
    private void ReturnToSlot()
    {
        DebugEx.LogModule("CardSlotItem", $"卡牌返回卡槽: {m_CardData.Name}");

        // 恢复到原始位置
        transform.DOLocalMove(m_DragStartPosition, 0.2f).SetEase(Ease.OutQuad);
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
        sequence.Append(btnTransform.DOScale(new Vector3(PULSE_SCALE, PULSE_SCALE, 1f), PULSE_DURATION * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(btnTransform.DOScale(Vector3.one, PULSE_DURATION * 0.5f).SetEase(Ease.InQuad));
        
        m_HoverTween = sequence;
        DebugEx.LogModule("CardSlotItem", $"播放脉冲动画: {m_CardData.Name}");
    }

    /// <summary>
    /// 播放悬停动画（鼠标进入时）
    /// </summary>
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (m_IsSelected || varBtn == null)
            return;

        var btnTransform = varBtn.transform;
        var itemRectTransform = GetComponent<RectTransform>();

        // 杀死之前的悬停动画
        m_HoverTween?.Kill();
        m_PositionTween?.Kill();

        // 缩放到 1.05
        m_HoverTween = btnTransform.DOScale(new Vector3(HOVER_SCALE, HOVER_SCALE, 1f), HOVER_DURATION).SetEase(Ease.OutQuad);

        // 位置上移（基于基准位置的偏移）
        if (itemRectTransform != null)
        {
            var targetPos = m_BaseAnchoredPos + Vector2.up * 30f;
            m_PositionTween = itemRectTransform.DOAnchorPos(targetPos, HOVER_DURATION).SetEase(Ease.OutQuad);
        }

        DebugEx.LogModule("CardSlotItem", $"悬停放大: {m_CardData.Name}");
    }

    /// <summary>
    /// 取消悬停动画（鼠标离开时）
    /// </summary>
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
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
            m_PositionTween = itemRectTransform.DOAnchorPos(m_BaseAnchoredPos, HOVER_DURATION).SetEase(Ease.OutQuad);
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
        sequence.Append(btnImage.DOColor(originalColor, FLASH_DURATION * 0.5f).SetEase(Ease.InQuad));

        DebugEx.LogModule("CardSlotItem", $"播放闪光效果: {m_CardData.Name}");
    }

    /// <summary>
    /// 更新吸附区域高亮状态
    /// </summary>
    private void UpdateAdsorptionAreaHighlight(bool isInAdsorptionArea)
    {
        var combatUI = GetComponentInParent<CombatUI>();
        if (combatUI == null)
            return;

        var adsorptionArea = combatUI.GetCardSlotAdsorptionArea();
        if (adsorptionArea == null)
            return;

        var areaImage = adsorptionArea.GetComponent<Image>();
        if (areaImage == null)
            return;

        // 进入吸附区域时高亮，离开时恢复
        if (isInAdsorptionArea)
        {
            // 高亮：增加透明度和亮度
            var highlightColor = areaImage.color;
            highlightColor.a = Mathf.Clamp01(highlightColor.a + 0.3f);
            areaImage.color = highlightColor;
            
            DebugEx.LogModule("CardSlotItem", "进入吸附区域，高亮显示");
        }
        else
        {
            // 恢复：降低透明度
            var normalColor = areaImage.color;
            normalColor.a = Mathf.Max(0.2f, normalColor.a - 0.3f);
            areaImage.color = normalColor;
        }
    }

    #endregion
}
