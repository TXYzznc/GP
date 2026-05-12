using System.Collections.Generic;
using GameFramework.DataTable;
using UnityEngine;

/// <summary>
/// 词条生成器 - 根据品质生成随机词条
/// </summary>
public static class AffixGenerator
{
    /// <summary>
    /// 根据品质生成词条列表
    /// </summary>
    public static List<AffixEffect> Generate(ItemRarity rarity)
    {
        var result = new List<AffixEffect>();

        var ruleTable = GF.DataTable.GetDataTable<AffixRuleTable>();
        if (ruleTable == null)
        {
            DebugEx.Error("AffixGenerator", "AffixRuleTable 未加载");
            return result;
        }

        var rule = ruleTable.GetDataRow((int)rarity);
        if (rule == null)
        {
            DebugEx.Error("AffixGenerator", $"AffixRuleTable 中无品质 {rarity} 对应的规则");
            return result;
        }

        var allAffixes = ItemManager.Instance.GetAllAffixData();
        if (allAffixes == null || allAffixes.Count == 0)
        {
            DebugEx.Error("AffixGenerator", "无可用词条数据");
            return result;
        }

        int count = Random.Range(rule.AffixCountMin, rule.AffixCountMax + 1);
        if (count <= 0)
            return result;

        int totalWeight = 0;
        foreach (var affix in allAffixes)
            totalWeight += affix.Weight;

        for (int i = 0; i < count; i++)
        {
            var selected = SelectByWeight(allAffixes, totalWeight);
            if (selected == null)
                continue;

            float value = GenerateValue(selected, rule.ValueScaleMin, rule.ValueScaleMax);
            result.Add(new AffixEffect(selected, value));
        }

        DebugEx.Log("AffixGenerator", $"品质 {rarity} 生成 {result.Count} 条词条");
        return result;
    }

    private static AffixData SelectByWeight(List<AffixData> affixes, int totalWeight)
    {
        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var affix in affixes)
        {
            cumulative += affix.Weight;
            if (roll < cumulative)
                return affix;
        }

        return affixes[affixes.Count - 1];
    }

    private static float GenerateValue(AffixData affix, float scaleMin, float scaleMax)
    {
        float scale = Random.Range(scaleMin, scaleMax);
        float value = affix.ValueMin + (affix.ValueMax - affix.ValueMin) * scale;

        if (affix.ValueType == ValueType.Percent)
            value = Mathf.Round(value * 10f) / 10f;
        else
            value = Mathf.Round(value);

        return value;
    }
}
