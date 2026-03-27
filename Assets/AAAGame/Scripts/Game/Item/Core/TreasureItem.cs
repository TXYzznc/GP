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

    private List<AffixEffect> m_Affixes; // 词条列表
    private bool m_IsEquipped; // 是否已装备
    #endregion

    #region 属性

    /// <summary>
    /// 词条列表
    /// </summary>
    public List<AffixEffect> Affixes => m_Affixes;

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
    /// 羁绊ID列表
    /// </summary>
    public List<int> SynergyIds => ItemData?.SynergyIds;

    #endregion

    #region 构造函数

    public TreasureItem(int itemId, ItemData itemData)
        : base(itemId, itemData)
    {
        m_Affixes = new List<AffixEffect>();
        m_IsEquipped = false;

        DebugEx.Log("TreasureItem", $"创建宝物: {Name}");

        // 生成随机词条
        GenerateRandomAffixes();
    }

    #endregion

    #region 重写方法

    public override bool CanUse => false; // 宝物不可使用

    public override bool CanStack => false; // 宝物不可堆叠

    public override bool CanEquip => true; // 宝物可装备

    protected override bool OnUse()
    {
        DebugEx.Warning("TreasureItem", $"宝物不可使用: {Name}");
        return false;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();

        // 添加特殊效果信息
        if (SpecialEffectId > 0)
        {
            var effectData = ItemManager.Instance?.GetSpecialEffectData(SpecialEffectId);
            if (effectData != null)
            {
                baseInfo += $"\n\n[特殊效果]\n{effectData.Description}";
            }
        }

        // 添加词条信息
        if (m_Affixes != null && m_Affixes.Count > 0)
        {
            baseInfo += "\n\n[词条效果]";
            foreach (var affix in m_Affixes)
            {
                baseInfo += $"\n• {affix.GetFormattedDescription()}";
            }
        }

        // 添加羁绊信息
        if (SynergyIds != null && SynergyIds.Count > 0)
        {
            baseInfo += "\n\n[羁绊]";
            foreach (int synergyId in SynergyIds)
            {
                var synergyData = ItemManager.Instance?.GetSynergyData(synergyId);
                if (synergyData != null)
                {
                    baseInfo += $"\n• {synergyData.Name}";
                }
            }
        }

        return baseInfo;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 生成随机词条
    /// </summary>
    private void GenerateRandomAffixes()
    {
        if (ItemData.AffixPoolIds == null || ItemData.AffixPoolIds.Count == 0)
        {
            DebugEx.Log("TreasureItem", $"宝物没有配置词条池: {Name}");
            return;
        }

        // 确定词条数量
        int affixCount = UnityEngine.Random.Range(
            ItemData.AffixMinCount,
            ItemData.AffixMaxCount + 1
        );
        DebugEx.Log("TreasureItem", $"生成 {affixCount} 个词条");

        // 获取词条池
        var affixPool = new List<AffixData>();
        foreach (int affixId in ItemData.AffixPoolIds)
        {
            var affixData = ItemManager.Instance?.GetAffixData(affixId);
            if (affixData != null)
            {
                affixPool.Add(affixData);
            }
        }

        if (affixPool.Count == 0)
        {
            DebugEx.Warning("TreasureItem", $"词条池为空: {Name}");
            return;
        }

        // 根据权重随机抽取词条
        for (int i = 0; i < affixCount && affixPool.Count > 0; i++)
        {
            var selectedAffix = SelectAffixByWeight(affixPool);
            if (selectedAffix != null)
            {
                float value = selectedAffix.GenerateRandomValue();
                var affixEffect = new AffixEffect(selectedAffix, value);
                m_Affixes.Add(affixEffect);

                // 移除已选择的词条，避免重复
                affixPool.Remove(selectedAffix);
            }
        }

        DebugEx.Success("TreasureItem", $"宝物词条生成完成: {Name}, 共 {m_Affixes.Count} 个词条");
    }

    /// <summary>
    /// 根据权重选择词条
    /// </summary>
    private AffixData SelectAffixByWeight(List<AffixData> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        int totalWeight = pool.Sum(a => a.Weight);
        int randomValue = UnityEngine.Random.Range(0, totalWeight);

        int currentWeight = 0;
        foreach (var affix in pool)
        {
            currentWeight += affix.Weight;
            if (randomValue < currentWeight)
            {
                return affix;
            }
        }

        return pool[pool.Count - 1];
    }

    #endregion
}
