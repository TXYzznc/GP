using System.Collections.Generic;

/// <summary>
/// 宝箱格子容器实现
/// 规则：宝箱 → 背包（单向）
/// 物品数据存储在内存中，不持久化
/// </summary>
public class TreasureBoxSlotContainerImpl : SlotContainerBase
{
    private readonly List<InventorySlot> m_Slots = new();

    public override SlotContainerType ContainerType => SlotContainerType.TreasureBox;

    /// <summary>
    /// 初始化格子数据（每次打开宝箱时调用）
    /// </summary>
    public void SetItems(List<ItemStack> items)
    {
        m_Slots.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            var slot = new InventorySlot(i);
            if (items[i] != null && !items[i].IsEmpty)
                slot.SetItemStack(items[i]);
            m_Slots.Add(slot);
        }
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_Slots.Count)
            return null;
        return m_Slots[slotIndex];
    }

    /// <summary>
    /// 宝箱只允许与背包交互（物品只能放入背包）
    /// </summary>
    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        return otherContainerType == SlotContainerType.Inventory;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromSlot = GetSlot(fromSlotIndex);
        if (fromSlot == null || fromSlot.IsEmpty)
            return false;

        int itemId = fromSlot.ItemId;
        int count = fromSlot.Count;

        bool ok = InventoryManager.Instance?.AddItem(itemId, count) ?? false;
        if (ok)
        {
            fromSlot.Clear();
            DebugEx.Log("TreasureBoxContainer", $"[宝箱→背包] 物品 {itemId} x{count} 已放入背包");
        }
        else
        {
            DebugEx.Warning("TreasureBoxContainer", $"[宝箱→背包] 背包已满，无法放入物品 {itemId}");
        }

        return ok;
    }

    /// <summary>
    /// 将所有物品放入背包（全部拿走按钮用）
    /// 返回成功放入的数量
    /// </summary>
    public int TakeAll()
    {
        int successCount = 0;
        foreach (var slot in m_Slots)
        {
            if (slot.IsEmpty) continue;

            bool ok = InventoryManager.Instance?.AddItem(slot.ItemId, slot.Count) ?? false;
            if (ok)
            {
                slot.Clear();
                successCount++;
            }
            else
            {
                DebugEx.Warning("TreasureBoxContainer", "背包已满，剩余物品无法全部放入");
                break;
            }
        }

        DebugEx.Log("TreasureBoxContainer", $"全部拿走: 成功 {successCount} 件");
        return successCount;
    }

    /// <summary>宝箱是否已清空</summary>
    public bool IsEmpty()
    {
        foreach (var slot in m_Slots)
            if (!slot.IsEmpty) return false;
        return true;
    }
}
