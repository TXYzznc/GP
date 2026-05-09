using System;
using System.Collections.Generic;

/// <summary>
/// 装备
/// </summary>
[Serializable]
public class EquipmentItem : ItemBase
{
    #region 字段

    private EquipmentData m_EquipmentData;
    private bool m_IsEquipped;

    #endregion

    #region 属性

    public bool IsEquipped
    {
        get => m_IsEquipped;
        set => m_IsEquipped = value;
    }

    public int SpecialEffectId => m_EquipmentData?.SpecialEffectId ?? 0;
    public Dictionary<AttributeType, float> BaseAttributes => m_EquipmentData?.BaseAttributes;

    public override bool CanUse => false;
    public override bool CanStack => false;
    public override bool CanEquip => true;

    #endregion

    #region 构造函数

    public EquipmentItem(int itemId, ItemData itemData, EquipmentData equipmentData)
        : base(itemId, itemData)
    {
        m_EquipmentData = equipmentData;
        m_IsEquipped = false;
        DebugEx.Log(nameof(EquipmentItem), $"创建装备: {Name}");
    }

    #endregion

    #region 重写方法

    protected override bool OnUse()
    {
        DebugEx.Warning(nameof(EquipmentItem), $"装备不可使用: {Name}");
        return false;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();

        if (BaseAttributes != null && BaseAttributes.Count > 0)
        {
            baseInfo += "\n\n[基础属性]";
            foreach (var attr in BaseAttributes)
                baseInfo += $"\n• {attr.Key}: +{attr.Value}";
        }

        if (SpecialEffectId > 0)
        {
            var effectData = ItemManager.Instance?.GetSpecialEffectData(SpecialEffectId);
            if (effectData != null)
                baseInfo += $"\n\n[特殊效果]\n{effectData.Description}";
        }

        return baseInfo;
    }

    #endregion
}
