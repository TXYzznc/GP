using UnityEngine;

/// <summary>
/// 装备栏容器实现（CombatPreparationUI / CombatUI 的装备面板）
/// 规则：左键拖拽到 Chess 或 Equip，右键无效果
/// 数据来源：InventoryManager（装备面板显示的是背包中的装备类物品）
/// </summary>
public class EquipSlotContainerImpl : SlotContainerBase
{
    public override SlotContainerType ContainerType => SlotContainerType.Equip;

    public override InventorySlot GetSlot(int slotIndex)
    {
        var invMgr = InventoryManager.Instance;
        if (invMgr == null) return null;
        return invMgr.GetSlot(slotIndex);
    }

    public override bool CanInteractWith(SlotContainerType otherContainerType)
    {
        // 装备栏可以拖到棋子装备槽（Chess）或其他装备栏（Equip）
        return otherContainerType == SlotContainerType.Chess ||
               otherContainerType == SlotContainerType.Equip;
    }

    protected override bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        var fromSlot = GetSlot(fromSlotIndex);
        if (fromSlot == null || fromSlot.IsEmpty)
            return false;

        var item = fromSlot.ItemStack?.Item;
        if (item is not EquipmentItem equipItem)
            return false;

        // 装备栏 → 棋子装备槽（Chess）：穿戴到棋子
        if (targetContainer is ChessSlotContainerImpl chessContainer)
        {
            var equipMgr = ChessEquipmentManager.Instance;
            int chessId = chessContainer.CurrentChessId;
            if (chessId < 0) return false;

            var oldItem = equipMgr.EquipItem(chessId, equipItem, targetSlotIndex);

            // 从背包数据中移除源物品
            InventoryManager.Instance.RemoveItem(equipItem.ItemId, 1);

            // 旧装备回到背包
            if (oldItem != null)
            {
                InventoryManager.Instance.AddItem(oldItem.ItemId, 1);
                DebugEx.Log("EquipSlotContainer", $"旧装备 {oldItem.Name} 回到背包");
            }

            DebugEx.Log("EquipSlotContainer", $"装备 {equipItem.Name} → 棋子 {chessId} 槽位 {targetSlotIndex}");
            return true;
        }

        return false;
    }
}
