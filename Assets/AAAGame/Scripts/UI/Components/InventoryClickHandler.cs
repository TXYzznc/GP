using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包格子点击事件处理器，挂在 InventorySlotUI 上。
/// 统一监听点击事件（左右键），并分发给 InventorySlotUI 处理。
/// 参考 InventoryDragHandler 的设计模式。
/// </summary>
public class InventoryClickHandler : MonoBehaviour, IPointerClickHandler
{
    #region 字段

    private InventorySlotUI m_SourceSlot;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        EnsureRaycastable();
    }

    /// <summary>
    /// 确保当前物体能接收点击事件
    /// </summary>
    private void EnsureRaycastable()
    {
        if (TryGetComponent(out Image img))
        {
            img.raycastTarget = true;
            DebugEx.Log("InventoryClickHandler", "[EnsureRaycastable] raycastTarget=true");
        }
    }

    #endregion

    #region 点击事件接口

    /// <summary>
    /// 处理指针点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        DebugEx.Log("InventoryClickHandler", $"[OnPointerClick] 触发，Button={eventData.button}");

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(eventData.position);
        }
    }

    #endregion

    #region 点击处理逻辑

    /// <summary>
    /// 处理左键点击
    /// </summary>
    private void HandleLeftClick()
    {
        DebugEx.Log("InventoryClickHandler", "[HandleLeftClick] 左键点击");

        // 获取源格子
        m_SourceSlot = GetComponent<InventorySlotUI>();
        if (m_SourceSlot == null)
        {
            m_SourceSlot = GetComponentInParent<InventorySlotUI>();
        }

        if (m_SourceSlot == null)
        {
            DebugEx.Warning("InventoryClickHandler", "[HandleLeftClick] 无法找到 InventorySlotUI");
            return;
        }

        DebugEx.Log("InventoryClickHandler", $"[HandleLeftClick] 找到源格子: 格子={m_SourceSlot.SlotIndex}");

        // 检查物品是否存在
        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI == null)
        {
            DebugEx.Warning("InventoryClickHandler", $"[HandleLeftClick] ItemUI 为 null (SlotIndex={m_SourceSlot.SlotIndex})");
            return;
        }

        if (!itemUI.HasItem())
        {
            DebugEx.Log("InventoryClickHandler", $"[HandleLeftClick] 格子为空 (SlotIndex={m_SourceSlot.SlotIndex})");
            return;
        }

        DebugEx.Log("InventoryClickHandler", $"[HandleLeftClick] 格子有物品，分发给 InventorySlotUI");

        // 分发给 InventorySlotUI 处理
        m_SourceSlot.OnLeftClick();
    }

    /// <summary>
    /// 处理右键点击
    /// </summary>
    private void HandleRightClick(Vector2 position)
    {
        DebugEx.Log("InventoryClickHandler", $"[HandleRightClick] 右键点击，位置={position}");

        // 获取源格子
        m_SourceSlot = GetComponent<InventorySlotUI>();
        if (m_SourceSlot == null)
        {
            m_SourceSlot = GetComponentInParent<InventorySlotUI>();
        }

        if (m_SourceSlot == null)
        {
            DebugEx.Warning("InventoryClickHandler", "[HandleRightClick] 无法找到 InventorySlotUI");
            return;
        }

        DebugEx.Log("InventoryClickHandler", $"[HandleRightClick] 找到源格子: 格子={m_SourceSlot.SlotIndex}");

        // 检查物品是否存在
        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI == null)
        {
            DebugEx.Warning("InventoryClickHandler", $"[HandleRightClick] ItemUI 为 null (SlotIndex={m_SourceSlot.SlotIndex})");
            return;
        }

        if (!itemUI.HasItem())
        {
            DebugEx.Log("InventoryClickHandler", $"[HandleRightClick] 格子为空 (SlotIndex={m_SourceSlot.SlotIndex})");
            return;
        }

        DebugEx.Log("InventoryClickHandler", $"[HandleRightClick] 格子有物品，分发给 InventorySlotUI");

        // 装备栏右键 → 无效果
        if (m_SourceSlot.ContainerType == SlotContainerType.Equip)
        {
            return;
        }

        // 棋子装备槽右键 → 卸下装备
        if (m_SourceSlot.ContainerType == SlotContainerType.Chess)
        {
            var detailInfoUI = m_SourceSlot.GetComponentInParent<DetailInfoUI>();
            if (detailInfoUI != null)
            {
                detailInfoUI.UnequipFromSlot(m_SourceSlot.SlotIndex);
                return;
            }
        }

        // 分发给 InventorySlotUI 处理
        m_SourceSlot.OnRightClick(position);
    }

    #endregion
}
