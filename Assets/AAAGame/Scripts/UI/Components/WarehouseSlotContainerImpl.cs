/// <summary>
/// 仓库容器实现
/// 规则：仓库 → 背包/仓库
/// </summary>
public class WarehouseSlotContainerImpl : SlotContainerBase
{
    private WarehouseManager m_WarehouseManager;

    public override SlotContainerType ContainerType => SlotContainerType.Warehouse;

    private void Awake()
    {
        m_WarehouseManager = WarehouseManager.Instance;
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        if (m_WarehouseManager == null)
            return null;

        var slot = new InventorySlot(slotIndex);
        var item = m_WarehouseManager.GetItemBySlot(slotIndex);

        if (item != null && item.Count > 0)
        {
            var itemObj = ItemManager.Instance?.CreateItem(item.ItemId);
            if (itemObj != null)
            {
                slot.SetItem(itemObj, item.Count);
            }
        }

        return slot;
    }

    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        return otherContainerType == SlotContainerType.Inventory ||
               otherContainerType == SlotContainerType.Warehouse;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromItem = m_WarehouseManager.GetItemBySlot(fromSlotIndex);
        if (fromItem == null)
            return false;

        var itemId = fromItem.ItemId;
        var count = fromItem.Count;

        // 检查目标格子是否为空（决定是存入还是交换）
        var targetSlot = targetContainer.GetSlot(targetSlotIndex);
        bool targetIsEmpty = targetSlot == null || targetSlot.IsEmpty;

        string targetStatus = targetIsEmpty ? "为空" : "非空";
        DebugEx.Log("WarehouseSlotContainer",
            $"[仓库→{targetContainer.ContainerType}] {fromSlotIndex} → {targetSlotIndex} (目标{targetStatus})");

        bool success = targetContainer switch
        {
            InventorySlotContainerImpl inv => MoveToInventory(inv, fromSlotIndex, itemId, count, targetSlotIndex),
            WarehouseSlotContainerImpl => MoveToWarehouse(fromSlotIndex, targetSlotIndex),
            _ => false
        };

        // ⚠️ 清空条件：目标格子为空 OR 目标是同种物品（堆叠）
        // 不清空：仓库→仓库（SwapSlots 交换已处理）或 目标是不同物品（操作失败）
        if (success && targetContainer is not WarehouseSlotContainerImpl &&
            (targetIsEmpty || targetSlot.ItemId == itemId))
        {
            m_WarehouseManager.RemoveItem(fromSlotIndex, count);
        }

        return success;
    }

    private bool MoveToInventory(InventorySlotContainerImpl _, int fromSlotIndex, int itemId, int count, int targetSlotIndex)
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return false;

        // 用副本检查目标格子状态
        var targetSlot = inv.GetSlot(targetSlotIndex);
        if (targetSlot == null)
            return false;

        if (targetSlot.IsEmpty)
        {
            var item = ItemManager.Instance?.CreateItem(itemId);
            if (item == null)
                return false;

            return inv.SetItemToSlot(targetSlotIndex, item, count);
        }
        else if (targetSlot.ItemId == itemId && targetSlot.ItemStack?.Item?.MaxStackCount > 1)
        {
            return inv.AddItemToSlot(targetSlotIndex, count);
        }

        return false;
    }

    private bool MoveToWarehouse(int fromSlotIndex, int targetSlotIndex)
    {
        return m_WarehouseManager.SwapSlots(fromSlotIndex, targetSlotIndex);
    }

}
