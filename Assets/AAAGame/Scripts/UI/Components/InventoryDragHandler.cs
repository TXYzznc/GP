using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包物品拖拽处理器
/// 实现物品在背包/仓库/快捷栏之间的拖拽逻辑
/// </summary>
public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 字段

    /// <summary>拖拽时显示的临时图标</summary>
    private Image m_DragIcon;

    /// <summary>拖拽的源格子</summary>
    private InventorySlotUI m_SourceSlot;

    /// <summary>拖拽的源物品</summary>
    private InventoryItem m_DraggedItem;

    /// <summary>顶层 Canvas（用于显示拖拽图标）</summary>
    private Canvas m_TopCanvas;

    /// <summary>原始父对象</summary>
    private Transform m_OriginalParent;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        // 获取顶层 Canvas
        m_TopCanvas = FindTopCanvas();
    }

    #endregion

    #region 拖拽接口实现

    /// <summary>
    /// 拖拽开始
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 获取源格子
        m_SourceSlot = GetComponent<InventorySlotUI>();
        if (m_SourceSlot == null || m_SourceSlot.IsEmpty())
        {
            DebugEx.Warning("InventoryDragHandler", "源格子为空或不存在");
            return;
        }

        m_DraggedItem = m_SourceSlot.GetCurrentItem();
        if (m_DraggedItem == null)
        {
            DebugEx.Warning("InventoryDragHandler", "拖拽物品为空");
            return;
        }

        DebugEx.Log(
            "InventoryDragHandler",
            $"开始拖拽物品: ID={m_DraggedItem.ItemId}, 格子={m_SourceSlot.GetSlotIndex()}"
        );

        // 创建拖拽图标
        CreateDragIcon();

        // 禁用源格子的交互
        var sourceButton = m_SourceSlot.GetComponent<Button>();
        if (sourceButton != null)
            sourceButton.interactable = false;
    }

    /// <summary>
    /// 拖拽中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (m_DragIcon == null)
            return;

        // 更新拖拽图标位置
        m_DragIcon.rectTransform.position = eventData.position;
    }

    /// <summary>
    /// 拖拽结束
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_DraggedItem == null || m_SourceSlot == null)
        {
            CleanupDrag();
            return;
        }

        DebugEx.Log("InventoryDragHandler", $"拖拽结束: 物品ID={m_DraggedItem.ItemId}");

        // 检测目标格子
        var targetSlot = GetTargetSlot(eventData.position);

        if (targetSlot != null && targetSlot != m_SourceSlot)
        {
            // 执行物品移动
            HandleItemMove(m_SourceSlot, targetSlot);
        }
        else
        {
            DebugEx.Log("InventoryDragHandler", "无效的拖拽目标，物品返回原位置");
        }

        CleanupDrag();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 创建拖拽图标
    /// </summary>
    private void CreateDragIcon()
    {
        if (m_TopCanvas == null)
        {
            DebugEx.Error("InventoryDragHandler", "未找到顶层 Canvas");
            return;
        }

        // 创建临时图标对象
        var iconGO = new GameObject("DragIcon");
        iconGO.transform.SetParent(m_TopCanvas.transform, false);

        m_DragIcon = iconGO.AddComponent<Image>();
        m_DragIcon.raycastTarget = false;

        // 复制源格子的图标
        var sourceImage = m_SourceSlot.GetComponent<Image>();
        if (sourceImage != null)
        {
            m_DragIcon.sprite = sourceImage.sprite;
            m_DragIcon.color = new Color(1f, 1f, 1f, 0.8f); // 半透明
        }

        // 设置大小
        var rectTransform = m_DragIcon.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(60, 60);

        DebugEx.Log("InventoryDragHandler", "拖拽图标已创建");
    }

    /// <summary>
    /// 获取目标格子
    /// </summary>
    private InventorySlotUI GetTargetSlot(Vector2 position)
    {
        // 使用 Raycast 检测目标格子
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(
            new PointerEventData(EventSystem.current) { position = position },
            results
        );

        foreach (var result in results)
        {
            var slotUI = result.gameObject.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                return slotUI;
            }
        }

        return null;
    }

    /// <summary>
    /// 处理物品移动
    /// </summary>
    private void HandleItemMove(InventorySlotUI sourceSlot, InventorySlotUI targetSlot)
    {
        int fromSlot = sourceSlot.GetSlotIndex();
        int toSlot = targetSlot.GetSlotIndex();

        DebugEx.Log("InventoryDragHandler", $"移动物品: 格子 {fromSlot} -> {toSlot}");

        // 调用 InventoryManager 执行移动
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null && inventoryManager.IsInitialized)
        {
            inventoryManager.MoveItem(fromSlot, toSlot);
        }
    }

    /// <summary>
    /// 清理拖拽状态
    /// </summary>
    private void CleanupDrag()
    {
        // 销毁拖拽图标
        if (m_DragIcon != null)
        {
            Destroy(m_DragIcon.gameObject);
            m_DragIcon = null;
        }

        // 恢复源格子的交互
        if (m_SourceSlot != null)
        {
            var sourceButton = m_SourceSlot.GetComponent<Button>();
            if (sourceButton != null)
                sourceButton.interactable = true;
        }

        m_DraggedItem = null;
        m_SourceSlot = null;
    }

    /// <summary>
    /// 查找顶层 Canvas
    /// </summary>
    private Canvas FindTopCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>();
        Canvas topCanvas = null;
        int maxSortingOrder = int.MinValue;

        foreach (var canvas in canvases)
        {
            if (canvas.sortingOrder > maxSortingOrder)
            {
                maxSortingOrder = canvas.sortingOrder;
                topCanvas = canvas;
            }
        }

        return topCanvas;
    }

    #endregion
}
