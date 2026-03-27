using System;

/// <summary>
/// 背包格子数据
/// </summary>
[Serializable]
public class InventorySlot
{
    #region 字段

    private int m_SlotIndex; // 格子索引
    private ItemStack m_ItemStack; // 物品堆叠
    #endregion

    #region 属性

    /// <summary>
    /// 格子索引
    /// </summary>
    public int SlotIndex => m_SlotIndex;

    /// <summary>
    /// 物品堆叠
    /// </summary>
    public ItemStack ItemStack => m_ItemStack;

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => m_ItemStack == null || m_ItemStack.IsEmpty;

    /// <summary>
    /// 物品ID
    /// </summary>
    public int ItemId => m_ItemStack?.ItemId ?? 0;

    /// <summary>
    /// 物品数量
    /// </summary>
    public int Count => m_ItemStack?.Count ?? 0;

    #endregion

    #region 构造函数

    public InventorySlot(int slotIndex)
    {
        m_SlotIndex = slotIndex;
        m_ItemStack = null;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置物品
    /// </summary>
    public void SetItem(ItemBase item, int count = 1)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        m_ItemStack = new ItemStack(item, count);
        DebugEx.Log("InventorySlot", $"格子 {m_SlotIndex} 设置物品: {item.Name}, 数量:{count}");
    }

    /// <summary>
    /// 设置物品堆叠
    /// </summary>
    public void SetItemStack(ItemStack itemStack)
    {
        m_ItemStack = itemStack;
        if (itemStack != null && !itemStack.IsEmpty)
        {
            DebugEx.Log(
                "InventorySlot",
                $"格子 {m_SlotIndex} 设置物品堆叠: {itemStack.Item.Name}, 数量:{itemStack.Count}"
            );
        }
    }

    /// <summary>
    /// 添加物品
    /// </summary>
    public int AddItem(int amount)
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
        {
            DebugEx.Warning("InventorySlot", $"格子 {m_SlotIndex} 为空，无法添加");
            return 0;
        }

        return m_ItemStack.Add(amount);
    }

    /// <summary>
    /// 移除物品
    /// </summary>
    public int RemoveItem(int amount)
    {
        if (m_ItemStack == null || m_ItemStack.IsEmpty)
        {
            return 0;
        }

        int removed = m_ItemStack.Remove(amount);

        if (m_ItemStack.IsEmpty)
        {
            Clear();
        }

        return removed;
    }

    /// <summary>
    /// 清空格子
    /// </summary>
    public void Clear()
    {
        DebugEx.Log("InventorySlot", $"清空格子 {m_SlotIndex}");
        m_ItemStack = null;
    }

    /// <summary>
    /// 检查是否可以与另一个格子合并
    /// </summary>
    public bool CanMergeWith(InventorySlot other)
    {
        if (other == null || IsEmpty || other.IsEmpty)
        {
            return false;
        }

        return m_ItemStack.CanMergeWith(other.ItemStack);
    }

    #endregion
}
