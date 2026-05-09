using System;
using UnityEngine;

/// <summary>
/// 物品基类
/// </summary>
[Serializable]
public abstract class ItemBase
{
    #region 字段

    [SerializeField]
    protected int m_ItemId;

    [SerializeField]
    protected int m_UniqueId;

    [SerializeField]
    protected ItemData m_ItemData;

    #endregion

    #region 属性

    public int ItemId => m_ItemId;
    public int UniqueId => m_UniqueId;
    public ItemData ItemData => m_ItemData;
    public string Name => m_ItemData?.Name ?? "未知物品";
    public ItemType Type => m_ItemData.Type;
    public ItemRarity Rarity => m_ItemData?.Rarity ?? ItemRarity.Common;

    public virtual bool CanStack => m_ItemData?.CanStack ?? false;
    public virtual int MaxStackCount => m_ItemData?.MaxStackCount ?? 1;

    // CanUse / CanEquip 由子类决定，基类默认均不可
    public virtual bool CanUse => false;
    public virtual bool CanEquip => false;

    #endregion

    #region 构造函数

    protected ItemBase(int itemId, ItemData itemData)
    {
        m_ItemId = itemId;
        m_ItemData = itemData;
        m_UniqueId = GenerateUniqueId();

        DebugEx.Log(nameof(ItemBase), $"创建物品实例: {Name} (ID:{itemId}, UniqueId:{m_UniqueId})");
    }

    #endregion

    #region 公共方法

    public virtual bool Use()
    {
        if (!CanUse)
        {
            DebugEx.Warning(nameof(ItemBase), $"物品不可使用: {Name}");
            return false;
        }

        DebugEx.Log(nameof(ItemBase), $"使用物品: {Name}");
        return OnUse();
    }

    public virtual string GetDescription()
    {
        return m_ItemData?.Description ?? "";
    }

    public virtual string GetDetailInfo()
    {
        return $"[{Rarity}] {Name}\n{GetDescription()}";
    }

    #endregion

    #region 受保护方法

    protected abstract bool OnUse();

    private int GenerateUniqueId()
    {
        return Guid.NewGuid().GetHashCode();
    }

    #endregion
}
