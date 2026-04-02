/// <summary>
/// 装备栏容器实现
/// 规则：装备 → 装备（后续可扩展为 → 棋子）
/// </summary>
public class EquipSlotContainerImpl : SlotContainerBase
{
    private InventoryManager m_InventoryManager;

    public override SlotContainerType ContainerType => SlotContainerType.Equip;

    private void Awake()
    {
        m_InventoryManager = InventoryManager.Instance;
    }

    public override InventorySlot GetSlot(int slotIndex)
    {
        if (m_InventoryManager == null)
            return null;

        var allSlots = m_InventoryManager.GetAllSlots();
        var itemTable = GF.DataTable.GetDataTable<ItemTable>();

        int equipIndex = 0;
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot.IsEmpty)
                continue;

            var row = itemTable?.GetDataRow(slot.ItemStack.Item.ItemId);
            if (row != null && row.Type == (int)ItemType.Equipment)
            {
                if (equipIndex == slotIndex)
                    return slot;

                equipIndex++;
            }
        }

        return null;
    }

    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        // 装备栏暂时只能与自己交互
        return otherContainerType == SlotContainerType.Equip;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromSlot = GetSlot(fromSlotIndex);
        if (fromSlot == null || fromSlot.IsEmpty)
            return false;

        DebugEx.Log("EquipSlotContainer",
            $"[装备→{targetContainer.ContainerType}] {fromSlotIndex} → {targetSlotIndex}");

        return targetContainer switch
        {
            EquipSlotContainerImpl eq => MoveToEquip(fromSlot, targetSlotIndex),
            _ => false
        };
    }

    private bool MoveToEquip(InventorySlot fromSlot, int targetSlotIndex)
    {
        DebugEx.Warning("EquipSlotContainer", "装备栏内交换暂未实现");
        return false;
    }
}
