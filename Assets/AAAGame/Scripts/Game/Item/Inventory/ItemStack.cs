using System;

/// <summary>
/// 物品堆叠数据
/// </summary>
[Serializable]
public class ItemStack
{
    #region 字段

    private ItemBase m_Item; // 物品实例
    private int m_Count; // 数量
    #endregion

    #region 属性

    /// <summary>
    /// 物品实例
    /// </summary>
    public ItemBase Item => m_Item;

    /// <summary>
    /// 物品ID
    /// </summary>
    public int ItemId => m_Item?.ItemId ?? 0;

    /// <summary>
    /// 数量
    /// </summary>
    public int Count
    {
        get => m_Count;
        set => m_Count = Math.Max(0, value);
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => m_Item == null || m_Count <= 0;

    /// <summary>
    /// 是否已满
    /// </summary>
    public bool IsFull => m_Item != null && m_Count >= m_Item.MaxStackCount;

    /// <summary>
    /// 剩余容量
    /// </summary>
    public int RemainingCapacity => m_Item != null ? m_Item.MaxStackCount - m_Count : 0;

    #endregion

    #region 构造函数

    public ItemStack(ItemBase item, int count = 1)
    {
        m_Item = item;
        m_Count = count;

        DebugEx.Log("ItemStack", $"创建物品堆叠: {item?.Name}, 数量:{count}");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加数量
    /// </summary>
    public int Add(int amount)
    {
        if (m_Item == null || amount <= 0)
        {
            return 0;
        }

        int maxAdd = m_Item.MaxStackCount - m_Count;
        int actualAdd = Math.Min(amount, maxAdd);
        m_Count += actualAdd;

        DebugEx.Log(
            "ItemStack",
            $"添加物品: {m_Item.Name}, 添加数量:{actualAdd}, 当前数量:{m_Count}"
        );
        return actualAdd;
    }

    /// <summary>
    /// 减少数量
    /// </summary>
    public int Remove(int amount)
    {
        if (m_Item == null || amount <= 0)
        {
            return 0;
        }

        int actualRemove = Math.Min(amount, m_Count);
        m_Count -= actualRemove;

        DebugEx.Log(
            "ItemStack",
            $"移除物品: {m_Item.Name}, 移除数量:{actualRemove}, 剩余数量:{m_Count}"
        );

        if (m_Count <= 0)
        {
            Clear();
        }

        return actualRemove;
    }

    /// <summary>
    /// 清空
    /// </summary>
    public void Clear()
    {
        DebugEx.Log("ItemStack", $"清空物品堆叠: {m_Item?.Name}");
        m_Item = null;
        m_Count = 0;
    }

    /// <summary>
    /// 检查是否可以与另一个堆叠合并
    /// </summary>
    public bool CanMergeWith(ItemStack other)
    {
        if (other == null || other.IsEmpty || IsEmpty)
        {
            return false;
        }

        // 必须是相同物品且可堆叠
        return m_Item.ItemId == other.ItemId && m_Item.CanStack;
    }

    #endregion
}
