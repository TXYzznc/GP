/// <summary>
/// 快捷栏容器实现
/// 规则：快捷栏 → 背包/快捷栏
/// </summary>
public class FastBarSlotContainerImpl : SlotContainerBase
{
    private FastBarManager m_FastBarManager;

    public override SlotContainerType ContainerType => SlotContainerType.FastBar;

    private void Awake()
    {
        m_FastBarManager = FastBarManager.Instance;
        if (!m_FastBarManager.IsInitialized)
            m_FastBarManager.Initialize();
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        if (m_FastBarManager == null)
            return null;
        return m_FastBarManager.GetSlot(slotIndex);
    }

    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        return otherContainerType == SlotContainerType.Inventory ||
               otherContainerType == SlotContainerType.FastBar;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromSlot = GetSlot(fromSlotIndex);
        if (fromSlot == null || fromSlot.IsEmpty)
            return false;

        var itemId = fromSlot.ItemId;

        // 检查目标格子是否为空（决定是存入还是交换）
        var targetSlot = targetContainer.GetSlot(targetSlotIndex);
        bool targetIsEmpty = targetSlot == null || targetSlot.IsEmpty;

        string targetStatus = targetIsEmpty ? "为空" : "非空";
        DebugEx.Log("FastBarSlotContainer",
            $"[快捷栏→{targetContainer.ContainerType}] {fromSlotIndex} → {targetSlotIndex} (目标{targetStatus})");

        bool success = targetContainer switch
        {
            InventorySlotContainerImpl inv => MoveToInventory(inv, fromSlot, targetSlotIndex),
            FastBarSlotContainerImpl => MoveToFastBar(fromSlotIndex, targetSlotIndex),
            _ => false
        };

        // ⚠️ 清空条件：目标格子为空 OR 目标是同种物品（堆叠）
        // 不清空：快捷栏→快捷栏（SwapSlots 交换已处理）或 目标是不同物品（操作失败）
        if (success && targetContainer is not FastBarSlotContainerImpl &&
            (targetIsEmpty || targetSlot.ItemId == itemId))
        {
            m_FastBarManager.ClearSlot(fromSlotIndex);
        }

        return success;
    }

    private bool MoveToInventory(InventorySlotContainerImpl _, InventorySlot fromSlot, int targetSlotIndex)
    {
        var inv = InventoryManager.Instance;
        var targetSlot = inv?.GetSlot(targetSlotIndex);

        if (targetSlot == null)
            return false;

        var item = fromSlot.ItemStack?.Item;
        var count = fromSlot.Count;

        if (item == null)
            return false;

        if (targetSlot.IsEmpty)
        {
            var newItem = ItemManager.Instance?.CreateItem(item.ItemId);
            if (newItem == null)
                return false;

            targetSlot.SetItem(newItem, count);
            return true;
        }
        else if (targetSlot.ItemId == item.ItemId && item.MaxStackCount > 1)
        {
            targetSlot.AddItem(count);
            return true;
        }

        return false;
    }

    private bool MoveToFastBar(int fromSlotIndex, int targetSlotIndex)
    {
        return m_FastBarManager.SwapSlots(fromSlotIndex, targetSlotIndex);
    }

}
