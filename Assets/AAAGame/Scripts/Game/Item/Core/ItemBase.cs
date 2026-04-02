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
    protected int m_ItemId; // 物品ID

    [SerializeField]
    protected int m_UniqueId; // 唯一ID（用于区分同ID的不同实例）

    [SerializeField]
    protected ItemData m_ItemData; // 物品配置数据
    #endregion

    #region 属性

    /// <summary>
    /// 物品ID
    /// </summary>
    public int ItemId => m_ItemId;

    /// <summary>
    /// 唯一ID
    /// </summary>
    public int UniqueId => m_UniqueId;

    /// <summary>
    /// 物品配置数据
    /// </summary>
    public ItemData ItemData => m_ItemData;

    /// <summary>
    /// 物品名称
    /// </summary>
    public string Name => m_ItemData?.Name ?? "未知物品";

    /// <summary>
    /// 物品类型
    /// </summary>
    public ItemType Type => m_ItemData.Type;

    /// <summary>
    /// 物品品质
    /// </summary>
    public ItemQuality Quality => m_ItemData?.Quality ?? ItemQuality.Common;

    /// <summary>
    /// 是否可堆叠
    /// </summary>
    public virtual bool CanStack => m_ItemData?.CanStack ?? false;

    /// <summary>
    /// 最大堆叠数量
    /// </summary>
    public virtual int MaxStackCount => m_ItemData?.MaxStackCount ?? 1;

    /// <summary>
    /// 是否可使用
    /// </summary>
    public virtual bool CanUse => m_ItemData?.CanUse ?? false;

    /// <summary>
    /// 是否可装备
    /// </summary>
    public virtual bool CanEquip => m_ItemData?.CanEquip ?? false;

    #endregion

    #region 构造函数

    protected ItemBase(int itemId, ItemData itemData)
    {
        m_ItemId = itemId;
        m_ItemData = itemData;
        m_UniqueId = GenerateUniqueId();

        DebugEx.Log("ItemBase", $"创建物品实例: {Name} (ID:{itemId}, UniqueId:{m_UniqueId})");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 使用物品
    /// </summary>
    public virtual bool Use()
    {
        if (!CanUse)
        {
            DebugEx.Warning("ItemBase", $"物品不可使用: {Name}");
            return false;
        }

        DebugEx.Log("ItemBase", $"使用物品: {Name}");
        return OnUse();
    }

    /// <summary>
    /// 获取物品描述
    /// </summary>
    public virtual string GetDescription()
    {
        return m_ItemData?.Description ?? "";
    }

    /// <summary>
    /// 获取详细信息
    /// </summary>
    public virtual string GetDetailInfo()
    {
        return $"[{Quality}] {Name}\n{GetDescription()}";
    }

    #endregion

    #region 受保护方法

    /// <summary>
    /// 使用物品的具体实现（由子类重写）
    /// </summary>
    protected abstract bool OnUse();

    /// <summary>
    /// 生成唯一ID
    /// </summary>
    private int GenerateUniqueId()
    {
        return Guid.NewGuid().GetHashCode();
    }

    #endregion
}
