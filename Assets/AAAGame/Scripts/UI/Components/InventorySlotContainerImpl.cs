/// <summary>
/// 背包容器实现
/// 规则：背包 → 背包/仓库/快捷栏
/// </summary>
public class InventorySlotContainerImpl : SlotContainerBase
{
    private InventoryManager m_InventoryManager;

    public override SlotContainerType ContainerType => SlotContainerType.Inventory;

    private void Awake()
    {
        m_InventoryManager = InventoryManager.Instance;
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        if (m_InventoryManager == null)
            return null;
        return m_InventoryManager.GetSlot(slotIndex);
    }

    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        return otherContainerType == SlotContainerType.Inventory ||
               otherContainerType == SlotContainerType.Warehouse ||
               otherContainerType == SlotContainerType.FastBar;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromSlot = GetSlot(fromSlotIndex);
        if (fromSlot == null || fromSlot.IsEmpty)
            return false;

        var itemId = fromSlot.ItemId;
        var count = fromSlot.Count;

        // 检查目标格子是否为空（决定是存入还是交换）
        var targetSlot = targetContainer.GetSlot(targetSlotIndex);
        bool targetIsEmpty = targetSlot == null || targetSlot.IsEmpty;

        string targetStatus = targetIsEmpty ? "为空" : "非空";
        DebugEx.Log("InventorySlotContainer",
            $"[背包→{targetContainer.ContainerType}] {fromSlotIndex} → {targetSlotIndex} (目标{targetStatus})");

        bool success = targetContainer switch
        {
            InventorySlotContainerImpl inv => MoveToInventory(inv, fromSlotIndex, targetSlotIndex),
            WarehouseSlotContainerImpl wh => MoveToWarehouse(wh, itemId, count, targetSlotIndex),
            FastBarSlotContainerImpl fb => MoveToFastBar(fb, fromSlot, targetSlotIndex),
            _ => false
        };

        // ⚠️ 清空条件：目标格子为空 OR 目标是同种物品（堆叠）
        // 不清空：背包→背包（MoveItem 已处理）或 目标是不同物品（交换失败）
        if (success && targetContainer is not InventorySlotContainerImpl &&
            (targetIsEmpty || targetSlot.ItemId == itemId))
        {
            m_InventoryManager.RemoveItem(itemId, count);
        }

        return success;
    }

    private bool MoveToInventory(InventorySlotContainerImpl targetInv, int fromIndex, int toIndex)
    {
        // MoveItem 内部已处理：清空源格子、设置目标格子或交换
        bool ok = m_InventoryManager.MoveItem(fromIndex, toIndex);
        DebugEx.Log("InventorySlotContainer", $"[背包→背包] MoveItem {ok}");
        return ok;
    }

    private bool MoveToWarehouse(WarehouseSlotContainerImpl targetWh, int itemId, int count, int targetSlotIndex)
    {
        var wh = WarehouseManager.Instance;
        bool ok = wh != null && wh.StoreItemToSlot(itemId, count, targetSlotIndex, 0);
        DebugEx.Log("InventorySlotContainer", $"[背包→仓库] StoreItemToSlot {ok}");
        return ok;
    }

    private bool MoveToFastBar(FastBarSlotContainerImpl targetFb, InventorySlot fromSlot, int targetSlotIndex)
    {
        var item = fromSlot.ItemStack?.Item;
        var count = fromSlot.Count;

        if (item == null)
            return false;

        var fastBar = FastBarManager.Instance;
        bool ok = fastBar != null && fastBar.StoreItemToSlot(item, count, targetSlotIndex);
        DebugEx.Log("InventorySlotContainer", $"[背包→快捷栏] StoreItemToSlot {ok}");
        return ok;
    }
}
