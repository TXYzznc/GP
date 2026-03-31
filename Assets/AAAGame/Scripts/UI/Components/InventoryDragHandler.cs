using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 背包/仓库物品拖拽处理器，挂在 InventorySlotUI 上。
/// 支持：背包内移动、背包→仓库存入、仓库→背包取出。
/// </summary>
public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 字段

    private Image m_DragIcon;
    private InventorySlotUI m_SourceSlot;
    private Canvas m_TopCanvas;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_TopCanvas = FindTopCanvas();
    }

    #endregion

    #region 拖拽接口

    public void OnBeginDrag(PointerEventData eventData)
    {
        m_SourceSlot = GetComponent<InventorySlotUI>();
        if (m_SourceSlot == null || m_SourceSlot.GetItemUI() == null)
        {
            m_SourceSlot = null;
            return;
        }

        CreateDragIcon();
        DebugEx.Log("DragHandler", $"开始拖拽 容器={m_SourceSlot.ContainerType} 格子={m_SourceSlot.SlotIndex}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_DragIcon != null)
            m_DragIcon.rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_SourceSlot == null)
        {
            CleanupDrag();
            return;
        }

        var targetSlot = GetTargetSlot(eventData.position);
        if (targetSlot != null && targetSlot != m_SourceSlot)
            HandleDrop(m_SourceSlot, targetSlot);

        CleanupDrag();
    }

    #endregion

    #region 拖放逻辑

    private void HandleDrop(InventorySlotUI src, InventorySlotUI dst)
    {
        var srcType = src.ContainerType;
        var dstType = dst.ContainerType;

        if (srcType == SlotContainerType.Inventory && dstType == SlotContainerType.Inventory)
        {
            // 背包内移动
            InventoryManager.Instance?.MoveItem(src.SlotIndex, dst.SlotIndex);
            DebugEx.Log("DragHandler", $"背包内移动: {src.SlotIndex} -> {dst.SlotIndex}");
        }
        else if (srcType == SlotContainerType.Inventory && dstType == SlotContainerType.Warehouse)
        {
            // 背包 → 仓库：取出对应格子的物品存入仓库
            var inv = InventoryManager.Instance;
            var wh = WarehouseManager.Instance;
            if (inv == null || wh == null) return;
            var invSlot = inv.GetSlot(src.SlotIndex);
            if (invSlot != null && !invSlot.IsEmpty)
            {
                bool ok = wh.StoreItem(invSlot.ItemId, invSlot.Count);
                if (ok)
                    inv.RemoveItem(invSlot.ItemId, invSlot.Count);
                else
                    DebugEx.Warning("DragHandler", "仓库已满，存入失败");
            }
        }
        else if (srcType == SlotContainerType.Warehouse && dstType == SlotContainerType.Inventory)
        {
            // 仓库 → 背包：取出仓库格子的物品放回背包
            var wh = WarehouseManager.Instance;
            if (wh == null) return;
            var item = wh.GetItemBySlot(src.SlotIndex);
            if (item != null)
            {
                bool ok = wh.RetrieveItem(item.ItemId, item.Count);
                if (!ok)
                    DebugEx.Warning("DragHandler", "背包已满，取出失败");
            }
        }
        // 仓库→仓库 暂不支持（仓库无排序需求）
    }

    #endregion

    #region 工具方法

    private void CreateDragIcon()
    {
        if (m_TopCanvas == null)
            return;

        var go = new GameObject("DragIcon");
        go.transform.SetParent(m_TopCanvas.transform, false);

        m_DragIcon = go.AddComponent<Image>();
        m_DragIcon.raycastTarget = false;

        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI != null)
        {
            itemUI.TryGetComponent(out Image srcImg);
            if (srcImg == null)
                srcImg = itemUI.GetComponentInChildren<Image>();
            if (srcImg != null)
            {
                m_DragIcon.sprite = srcImg.sprite;
                m_DragIcon.color = new Color(1f, 1f, 1f, 0.8f);
            }
        }

        m_DragIcon.rectTransform.sizeDelta = new Vector2(60, 60);
    }

    private InventorySlotUI GetTargetSlot(Vector2 position)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(
            new PointerEventData(EventSystem.current) { position = position },
            results
        );

        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<InventorySlotUI>();
            if (slot != null)
                return slot;
        }

        return null;
    }

    private void CleanupDrag()
    {
        if (m_DragIcon != null)
        {
            Destroy(m_DragIcon.gameObject);
            m_DragIcon = null;
        }
        m_SourceSlot = null;
    }

    private Canvas FindTopCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>();
        Canvas top = null;
        int maxOrder = int.MinValue;
        foreach (var c in canvases)
        {
            if (c.sortingOrder > maxOrder)
            {
                maxOrder = c.sortingOrder;
                top = c;
            }
        }
        return top;
    }

    #endregion
}
