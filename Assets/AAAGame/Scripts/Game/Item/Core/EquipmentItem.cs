using System;
using System.Collections.Generic;

/// <summary>
/// 装备
/// </summary>
[Serializable]
public class EquipmentItem : ItemBase
{
    #region 字段

    private bool m_IsEquipped; // 是否已装备
    #endregion

    #region 属性

    /// <summary>
    /// 是否已装备
    /// </summary>
    public bool IsEquipped
    {
        get => m_IsEquipped;
        set => m_IsEquipped = value;
    }

    /// <summary>
    /// 特殊效果ID
    /// </summary>
    public int SpecialEffectId => ItemData?.SpecialEffectId ?? 0;

    /// <summary>
    /// 基础属性
    /// </summary>
    public Dictionary<AttributeType, float> BaseAttributes => ItemData?.BaseAttributes;

    #endregion

    #region 构造函数

    public EquipmentItem(int itemId, ItemData itemData)
        : base(itemId, itemData)
    {
        m_IsEquipped = false;
        DebugEx.Log("EquipmentItem", $"创建装备: {Name}");
    }

    #endregion

    #region 重写方法

    public override bool CanUse => false; // 装备不可使用

    public override bool CanStack => false; // 装备不可堆叠

    public override bool CanEquip => true; // 装备可装备

    protected override bool OnUse()
    {
        DebugEx.Warning("EquipmentItem", $"装备不可使用: {Name}");
        return false;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();

        // 添加基础属性信息
        if (BaseAttributes != null && BaseAttributes.Count > 0)
        {
            baseInfo += "\n\n[基础属性]";
            foreach (var attr in BaseAttributes)
            {
                baseInfo += $"\n• {attr.Key}: +{attr.Value}";
            }
        }

        // 添加特殊效果信息
        if (SpecialEffectId > 0)
        {
            var effectData = ItemManager.Instance?.GetSpecialEffectData(SpecialEffectId);
            if (effectData != null)
            {
                baseInfo += $"\n\n[特殊效果]\n{effectData.Description}";
            }
        }

        return baseInfo;
    }

    #endregion
}
