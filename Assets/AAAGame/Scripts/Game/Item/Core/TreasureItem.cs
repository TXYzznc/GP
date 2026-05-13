using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 宝物
/// </summary>
[Serializable]
public class TreasureItem : ItemBase
{
    #region 字段

    private TreasureData m_TreasureData;
    private List<AffixEffect> m_Affixes;
    private bool m_IsEquipped;

    #endregion

    #region 属性

    public List<AffixEffect> Affixes => m_Affixes;

    public bool IsEquipped
    {
        get => m_IsEquipped;
        set => m_IsEquipped = value;
    }

    public int SpecialEffectId => m_TreasureData?.SpecialEffectId ?? 0;
    public List<int> SynergyIds => m_TreasureData?.SynergyIds;
    public Dictionary<AttributeType, float> BaseAttributes => m_TreasureData?.BaseAttributes;

    public override bool CanUse => false;
    public override bool CanStack => false;
    public override bool CanEquip => true;

    #endregion

    #region 构造函数

    public TreasureItem(int itemId, ItemData itemData, TreasureData treasureData)
        : base(itemId, itemData)
    {
        m_TreasureData = treasureData;
        m_Affixes = new List<AffixEffect>();
        m_IsEquipped = false;

        DebugEx.Log(nameof(TreasureItem), $"创建宝物: {Name}");
    }

    #endregion

    #region 词条方法

    /// <summary>
    /// 设置词条列表（创建时由AffixGenerator调用）
    /// </summary>
    public void SetAffixes(List<AffixEffect> affixes)
    {
        m_Affixes = affixes ?? new List<AffixEffect>();
    }

    /// <summary>
    /// 获取词条数据用于持久化（转换运行时词条为持久化格式）
    /// </summary>
    public List<AffixData> GetAffixDataForPersistence()
    {
        var result = new List<AffixData>();
        if (m_Affixes == null || m_Affixes.Count == 0)
            return result;

        foreach (var affix in m_Affixes)
        {
            result.Add(new AffixData
            {
                Id = affix.AffixId,
                Name = affix.Name,
                Description = affix.Description,
                AffixType = affix.AffixType,
                AttributeType = affix.AttributeType,
                ValueType = affix.ValueType,
                ValueMin = affix.Value,
                ValueMax = affix.Value,
                Weight = 0
            });
        }

        return result;
    }

    /// <summary>
    /// 获取词条对棋子的属性加成信息（用于UI显示）
    /// </summary>
    public List<string> GetAffixBonusStrings()
    {
        var bonuses = new List<string>();
        if (m_Affixes == null) return bonuses;

        foreach (var affix in m_Affixes)
        {
            bonuses.Add(affix.GetFormattedDescription());
        }

        return bonuses;
    }

    /// <summary>
    /// 将词条属性应用到棋子
    /// </summary>
    public void ApplyAffixesToChess(ChessAttribute chessAttr)
    {
        if (chessAttr == null || m_Affixes == null) return;

        foreach (var affix in m_Affixes)
        {
            ApplySingleAffix(chessAttr, affix, 1f);
        }

        DebugEx.Log(nameof(TreasureItem), $"宝物 {Name} 应用 {m_Affixes.Count} 条词条属性");
    }

    /// <summary>
    /// 从棋子移除词条属性
    /// </summary>
    public void RemoveAffixesFromChess(ChessAttribute chessAttr)
    {
        if (chessAttr == null || m_Affixes == null) return;

        foreach (var affix in m_Affixes)
        {
            ApplySingleAffix(chessAttr, affix, -1f);
        }

        DebugEx.Log(nameof(TreasureItem), $"宝物 {Name} 移除 {m_Affixes.Count} 条词条属性");
    }

    private void ApplySingleAffix(ChessAttribute chessAttr, AffixEffect affix, float sign)
    {
        double delta = affix.Value * sign;

        // 百分比类型转换为小数（如 20% → 0.2）
        if (affix.ValueType == ValueType.Percent)
            delta /= 100.0;

        switch (affix.AttributeType)
        {
            case AttributeType.MaxHP:
                chessAttr.ModifyHp(delta);
                break;
            case AttributeType.Attack:
                chessAttr.ModifyAtkDamage(delta);
                break;
            case AttributeType.MaxMP:
                chessAttr.ModifyMp(delta);
                break;
            case AttributeType.AttackSpeed:
                chessAttr.ModifyAtkSpeed(delta);
                break;
            case AttributeType.CritRate:
                chessAttr.ModifyCritRate(delta);
                break;
            case AttributeType.CritDamage:
                chessAttr.ModifyCritDamage(delta);
                break;
            case AttributeType.Defense:
                chessAttr.ModifyArmor(delta);
                break;
            case AttributeType.MagicResist:
                chessAttr.ModifyMagicResist(delta);
                break;
            case AttributeType.SpellPower:
                chessAttr.ModifySpellPower(delta);
                break;
            case AttributeType.MoveSpeed:
                chessAttr.ModifyMoveSpeed(delta);
                break;
            case AttributeType.CooldownReduce:
                chessAttr.ModifyCooldownReduce(delta);
                break;
        }
    }

    #endregion

    #region 重写方法

    protected override bool OnUse()
    {
        DebugEx.Warning(nameof(TreasureItem), $"宝物不可使用: {Name}");
        return false;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();

        if (BaseAttributes != null && BaseAttributes.Count > 0)
        {
            baseInfo += "\n\n[基础属性]";
            foreach (var attr in BaseAttributes)
            {
                baseInfo += $"\n• {attr.Key}: +{attr.Value}";
            }
        }

        if (SpecialEffectId > 0)
        {
            var effectData = ItemManager.Instance?.GetSpecialEffectData(SpecialEffectId);
            if (effectData != null)
                baseInfo += $"\n\n[特殊效果]\n{effectData.Description}";
        }

        if (m_Affixes != null && m_Affixes.Count > 0)
        {
            baseInfo += "\n\n[词条效果]";
            foreach (var affix in m_Affixes)
                baseInfo += $"\n• {affix.GetFormattedDescription()}";
        }

        if (SynergyIds != null && SynergyIds.Count > 0)
        {
            baseInfo += "\n\n[羁绊]";
            foreach (int synergyId in SynergyIds)
            {
                var synergyData = ItemManager.Instance?.GetSynergyData(synergyId);
                if (synergyData != null)
                    baseInfo += $"\n• {synergyData.Name}";
            }
        }

        return baseInfo;
    }

    #endregion
}
