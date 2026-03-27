using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品配置数据
/// </summary>
[Serializable]
public class ItemData
{
    public int Id; // 物品ID
    public string Name; // 物品名称
    public ItemType Type; // 物品类型
    public ItemQuality Quality; // 品质等级
    public string Description; // 物品描述
    public int IconId; // 缩略图资源ID
    public int DetailIconId; // 详细图资源ID
    public bool CanStack; // 是否可堆叠
    public int MaxStackCount; // 最大堆叠数量
    public bool CanUse; // 是否可使用
    public int UseEffectId; // 使用效果ID
    public bool CanEquip; // 是否可装备
    public EquipType EquipType; // 装备类型
    public int SpecialEffectId; // 特殊效果ID
    public List<int> AffixPoolIds; // 词条池ID列表
    public int AffixMinCount; // 词条最小数量
    public int AffixMaxCount; // 词条最大数量
    public List<int> SynergyIds; // 羁绊ID列表
    public Dictionary<AttributeType, float> BaseAttributes; // 基础属性
    public int SellPrice; // 售价

    /// <summary>
    /// 获取实际使用的图标ID
    /// </summary>
    public int GetIconId()
    {
        return DetailIconId > 0 ? DetailIconId : IconId;
    }

    /// <summary>
    /// 获取品质颜色
    /// </summary>
    public Color GetQualityColor()
    {
        switch (Quality)
        {
            case ItemQuality.Common:
                return Color.white;
            case ItemQuality.Uncommon:
                return Color.green;
            case ItemQuality.Rare:
                return Color.blue;
            case ItemQuality.Epic:
                return new Color(0.64f, 0.21f, 0.93f); // 紫色
            case ItemQuality.Legendary:
                return new Color(1f, 0.5f, 0f); // 橙色
            default:
                return Color.white;
        }
    }
}
