using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Buff 诊断工具
/// 详细分析和显示 Buff 对属性的具体修改过程
/// </summary>
public class BuffDiagnoser
{
    #region 单例

    private static BuffDiagnoser s_Instance;
    public static BuffDiagnoser Instance => s_Instance ??= new BuffDiagnoser();

    #endregion

    #region 数据结构

    public struct AttributeSnapshot
    {
        public double AtkDamage;
        public double AtkSpeed;
        public double Armor;
        public double MagicResist;
        public double MoveSpeed;
        public double SpellPower;
        public double CritRate;
        public double CritDamage;
    }

    public struct BuffModificationDetail
    {
        public int BuffId;
        public string BuffName;
        public string StatType;
        public double OriginalValue;        // 应用此Buff前的属性值
        public double ModificationValue;   // Buff修改的值（绝对值）
        public double ModificationPercent; // 百分比（如果是百分比修改）
        public double ResultValue;         // 修改后的属性值
        public bool IsPercent;             // 是否为百分比修改
    }

    public class BuffApplicationReport
    {
        public AttributeSnapshot BaselineAttributes;
        public AttributeSnapshot CurrentAttributes;
        public List<BuffModificationDetail> ModificationDetails = new List<BuffModificationDetail>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Buff 应用诊断报告 ===");
            sb.AppendLine($"基础攻击力: {BaselineAttributes.AtkDamage:F1}");
            sb.AppendLine($"当前攻击力: {CurrentAttributes.AtkDamage:F1}");
            sb.AppendLine($"总增幅: {CurrentAttributes.AtkDamage - BaselineAttributes.AtkDamage:F1} ({(CurrentAttributes.AtkDamage / BaselineAttributes.AtkDamage - 1) * 100:F1}%)");
            sb.AppendLine();

            sb.AppendLine("=== Buff 修改详情 ===");
            foreach (var detail in ModificationDetails)
            {
                sb.AppendLine($"Buff: {detail.BuffName} (ID={detail.BuffId})");
                sb.AppendLine($"  属性: {detail.StatType}");
                sb.AppendLine($"  修改前: {detail.OriginalValue:F1}");
                if (detail.IsPercent)
                {
                    sb.AppendLine($"  修改幅度: +{detail.ModificationPercent * 100:F1}% (实际值: +{detail.ModificationValue:F1})");
                }
                else
                {
                    sb.AppendLine($"  修改幅度: +{detail.ModificationValue:F1}");
                }
                sb.AppendLine($"  修改后: {detail.ResultValue:F1}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取目标的基础属性快照（无Buff状态）
    /// 注意：此方法会临时清除所有Buff，然后恢复
    /// </summary>
    public AttributeSnapshot GetBaselineAttributes(GameObject target)
    {
        if (target == null)
            return default;

        var attribute = target.GetComponent<ChessAttribute>();
        if (attribute == null)
            return default;

        return new AttributeSnapshot
        {
            AtkDamage = attribute.AtkDamage,
            AtkSpeed = attribute.AtkSpeed,
            Armor = attribute.Armor,
            MagicResist = attribute.MagicResist,
            MoveSpeed = attribute.MoveSpeed,
            SpellPower = attribute.SpellPower,
            CritRate = attribute.CritRate,
            CritDamage = attribute.CritDamage,
        };
    }

    /// <summary>
    /// 生成完整的Buff应用诊断报告
    /// 显示每个Buff如何修改了属性
    /// </summary>
    public BuffApplicationReport GenerateReport(GameObject target)
    {
        var report = new BuffApplicationReport();

        if (target == null)
            return report;

        var attribute = target.GetComponent<ChessAttribute>();
        var buffManager = target.GetComponent<BuffManager>();
        if (attribute == null || buffManager == null)
            return report;

        // 获取当前属性（包含所有Buff修改后的）
        report.CurrentAttributes = GetBaselineAttributes(target);

        // 初始基础属性为当前属性（用于计算Buff之间的相互作用）
        report.BaselineAttributes = report.CurrentAttributes;

        // 获取所有Buff
        var allBuffs = new List<IBuff>(buffManager.GetAllBuffs());
        if (allBuffs.Count == 0)
            return report;

        // 获取BuffTable用于查询Buff配置
        object buffTable = null;
        try
        {
            buffTable = GF.DataTable.GetDataTable<BuffTable>();
        }
        catch
        {
            return report;
        }

        if (buffTable == null)
            return report;

        // 分析每个Buff的修改
        var attributeBefore = report.BaselineAttributes;

        foreach (var buff in allBuffs)
        {
            if (buff is StatModBuff statModBuff)
            {
                AnalyzeStatModBuff(statModBuff, attribute, buffTable, report, ref attributeBefore);
            }
        }

        return report;
    }

    /// <summary>
    /// 简单的Buff应用验证（不需要复杂诊断，直接看应用前后的差异）
    /// </summary>
    public string VerifyBuffApplication(GameObject target)
    {
        var attribute = target?.GetComponent<ChessAttribute>();
        if (attribute == null)
            return "目标没有 ChessAttribute";

        var buffManager = target?.GetComponent<BuffManager>();
        if (buffManager == null)
            return "目标没有 BuffManager";

        var buffs = new List<IBuff>(buffManager.GetAllBuffs());
        if (buffs.Count == 0)
            return "没有应用任何 Buff";

        var sb = new StringBuilder();
        sb.AppendLine("=== Buff 应用验证 ===");
        sb.AppendLine($"当前攻击力: {attribute.AtkDamage:F2}");
        sb.AppendLine($"当前攻速: {attribute.AtkSpeed:F2}");
        sb.AppendLine($"护甲: {attribute.Armor:F2}");
        sb.AppendLine();

        sb.AppendLine($"应用的 Buff 数: {buffs.Count}");
        sb.AppendLine();

        // 统计攻击力相关的修改
        double totalAtkModification = 0;
        double totalAtkSpeedModification = 0;

        foreach (var buff in buffs)
        {
            if (buff is StatModBuff statModBuff)
            {
                var mods = statModBuff.GetModDetails();
                var appliedValues = statModBuff.GetAppliedValues();

                var buffName = $"Buff_{buff.BuffId}";
                sb.AppendLine($"[{buffName}]");

                for (int i = 0; i < mods.Count; i++)
                {
                    var mod = mods[i];
                    var value = appliedValues[i];

                    if (mod.Type == StatModBuff.StatType.AtkDamage)
                    {
                        totalAtkModification += value;
                        sb.AppendLine($"  攻击力: +{value:F2} ({(mod.IsPercent ? $"{mod.Value*100:F0}%" : "固定值")})");
                    }
                    else if (mod.Type == StatModBuff.StatType.AtkSpeed)
                    {
                        totalAtkSpeedModification += value;
                        sb.AppendLine($"  攻速: +{value:F2} ({(mod.IsPercent ? $"{mod.Value*100:F0}%" : "固定值")})");
                    }
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine($"攻击力总修改: +{totalAtkModification:F2}");
        sb.AppendLine($"攻速总修改: +{totalAtkSpeedModification:F2}");
        sb.AppendLine();

        // 诊断：如果所有值都被正确应用，当前攻击力应该 = 基础值 + 总修改
        if (Math.Abs(totalAtkModification) < 0.01)
        {
            sb.AppendLine("⚠️ 警告：没有检测到任何攻击力修改！");
            sb.AppendLine("  可能原因：");
            sb.AppendLine("  1. Buff 没有包含攻击力修改");
            sb.AppendLine("  2. ApplyMods 没有被执行");
            sb.AppendLine("  3. ModifyAtkDamage 没有实际改变值");
        }
        else
        {
            sb.AppendLine("✓ 已检测到攻击力修改");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 显示攻击力计算推导过程
    /// </summary>
    public string GenerateDamageCalculationAnalysis(GameObject caster, GameObject target)
    {
        if (caster == null || target == null)
            return "无效的目标";

        var casterAttr = caster.GetComponent<ChessAttribute>();
        if (casterAttr == null)
            return "目标没有 ChessAttribute";

        var sb = new StringBuilder();
        sb.AppendLine("=== 伤害计算推导 ===");
        sb.AppendLine($"攻击者: {caster.name}");
        sb.AppendLine($"当前攻击力: {casterAttr.AtkDamage:F1}");
        sb.AppendLine();

        // 假设一个标准的攻击（需要从技能配置获取系数）
        sb.AppendLine("假设标准普攻系数为 0.5，基础伤害为 50:");
        double baseDamage = casterAttr.AtkDamage * 0.5 + 50;
        sb.AppendLine($"  伤害 = 攻击力 × 系数 + 基础伤害");
        sb.AppendLine($"  伤害 = {casterAttr.AtkDamage:F1} × 0.5 + 50 = {baseDamage:F1}");
        sb.AppendLine();

        // 护甲减伤
        var targetAttr = target.GetComponent<ChessAttribute>();
        if (targetAttr != null)
        {
            double actualDamage = targetAttr.CalculatePhysicalDamage(baseDamage);
            sb.AppendLine($"目标防御: {targetAttr.Armor:F1}");
            sb.AppendLine($"实际伤害 = 基础伤害 × 100 / (100 + 防御)");
            sb.AppendLine($"实际伤害 = {baseDamage:F1} × 100 / (100 + {targetAttr.Armor:F1}) = {actualDamage:F1}");
        }

        return sb.ToString();
    }

    #endregion

    #region 私有方法

    private void AnalyzeStatModBuff(
        StatModBuff buff,
        ChessAttribute attribute,
        object buffTable,
        BuffApplicationReport report,
        ref AttributeSnapshot snapshot)
    {
        if (buff == null)
            return;

        var buffId = buff.BuffId;
        var buffName = $"Buff_{buffId}";

        // 尝试从buffTable获取名称
        if (buffTable != null)
        {
            try
            {
                // 这里我们使用反射或其他方式获取BuffTable中的名称
                // 为了简单起见，我们保持缺省名称
                var tableType = buffTable.GetType();
                var method = tableType.GetMethod("GetDataRow", new[] { typeof(int) });
                if (method != null)
                {
                    var row = method.Invoke(buffTable, new object[] { buffId });
                    if (row != null)
                    {
                        var nameProperty = row.GetType().GetProperty("Name");
                        if (nameProperty != null)
                        {
                            buffName = nameProperty.GetValue(row) as string ?? buffName;
                        }
                    }
                }
            }
            catch { }
        }

        // 获取Buff的修改详情
        var modDetails = buff.GetModDetails();
        var appliedValues = buff.GetAppliedValues();

        if (modDetails.Count == 0)
            return;

        // 分析每个修改
        for (int i = 0; i < modDetails.Count; i++)
        {
            var mod = modDetails[i];
            var appliedValue = appliedValues[i];

            double originalValue = 0;
            double resultValue = 0;

            // 获取对应属性的值（基于快照的前值，而不是当前属性值）
            // 这样才能正确反映这个Buff具体修改了多少
            switch (mod.Type)
            {
                case StatModBuff.StatType.AtkDamage:
                    originalValue = snapshot.AtkDamage;
                    resultValue = snapshot.AtkDamage + appliedValue;
                    break;
                case StatModBuff.StatType.AtkSpeed:
                    originalValue = snapshot.AtkSpeed;
                    resultValue = snapshot.AtkSpeed + appliedValue;
                    break;
                case StatModBuff.StatType.Armor:
                    originalValue = snapshot.Armor;
                    resultValue = snapshot.Armor + appliedValue;
                    break;
                case StatModBuff.StatType.MagicResist:
                    originalValue = snapshot.MagicResist;
                    resultValue = snapshot.MagicResist + appliedValue;
                    break;
                case StatModBuff.StatType.MoveSpeed:
                    originalValue = snapshot.MoveSpeed;
                    resultValue = snapshot.MoveSpeed + appliedValue;
                    break;
                case StatModBuff.StatType.SpellPower:
                    originalValue = snapshot.SpellPower;
                    resultValue = snapshot.SpellPower + appliedValue;
                    break;
                default:
                    continue;
            }

            var detail = new BuffModificationDetail
            {
                BuffId = buffId,
                BuffName = buffName,
                StatType = mod.Type.ToString(),
                OriginalValue = originalValue,
                ModificationValue = appliedValue,
                ModificationPercent = mod.IsPercent ? mod.Value : 0,
                ResultValue = resultValue,
                IsPercent = mod.IsPercent
            };

            report.ModificationDetails.Add(detail);

            // 更新快照以用于下一个Buff（这样可以显示Buff之间的相互作用）
            switch (mod.Type)
            {
                case StatModBuff.StatType.AtkDamage:
                    snapshot.AtkDamage = resultValue;
                    break;
                case StatModBuff.StatType.AtkSpeed:
                    snapshot.AtkSpeed = resultValue;
                    break;
                case StatModBuff.StatType.Armor:
                    snapshot.Armor = resultValue;
                    break;
                case StatModBuff.StatType.MagicResist:
                    snapshot.MagicResist = resultValue;
                    break;
                case StatModBuff.StatType.MoveSpeed:
                    snapshot.MoveSpeed = resultValue;
                    break;
                case StatModBuff.StatType.SpellPower:
                    snapshot.SpellPower = resultValue;
                    break;
            }
        }
    }

    #endregion
}
