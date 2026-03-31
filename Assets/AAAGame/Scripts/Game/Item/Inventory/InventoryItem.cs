using System;

/// <summary>
/// 背包物品运行时数据类
/// 包含物品的动态状态（数量、耐久度、格子位置等）
/// </summary>
[Serializable]
public class InventoryItem
{
    /// <summary>物品ID（对应ItemTable）</summary>
    public int ItemId;

    /// <summary>物品数量</summary>
    public int Count;

    /// <summary>当前耐久度</summary>
    public int Durability;

    /// <summary>当前所在格子索引（-1表示不在背包中）</summary>
    public int SlotIndex;

    /// <summary>
    /// 构造函数
    /// </summary>
    public InventoryItem(int itemId, int count = 1, int durability = 0, int slotIndex = -1)
    {
        ItemId = itemId;
        Count = count;
        Durability = durability;
        SlotIndex = slotIndex;
    }

    /// <summary>
    /// 复制一个物品实例
    /// </summary>
    public InventoryItem Clone()
    {
        return new InventoryItem(ItemId, Count, Durability, SlotIndex);
    }

    /// <summary>
    /// 获取物品的显示名称
    /// </summary>
    public override string ToString()
    {
        return $"InventoryItem(ID:{ItemId}, Count:{Count}, Durability:{Durability}, Slot:{SlotIndex})";
    }
}
