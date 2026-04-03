using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 效果验证器
/// 用于实时显示和验证 Buff 的实际效果
/// </summary>
public class BuffEffectVerifier
{
    #region 单例

    private static BuffEffectVerifier s_Instance;
    public static BuffEffectVerifier Instance => s_Instance ??= new BuffEffectVerifier();

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取目标的当前属性信息
    /// </summary>
    public TargetAttributeInfo GetTargetAttributes(GameObject target)
    {
        if (target == null)
            return new TargetAttributeInfo();

        var attribute = target.GetComponent<ChessAttribute>();
        if (attribute == null)
            return new TargetAttributeInfo();

        var info = new TargetAttributeInfo
        {
            Name = target.name,
            HP = (float)attribute.CurrentHp,
            MaxHP = (float)attribute.MaxHp,
            MP = (float)attribute.CurrentMp,
            MaxMP = (float)attribute.MaxMp,
            AtkDamage = (float)attribute.AtkDamage,
            AtkRange = (float)attribute.AtkRange,
            PhysDef = (float)attribute.Armor,
            MagicDef = (float)attribute.MagicResist,
            Speed = (float)attribute.MoveSpeed,
        };

        return info;
    }

    /// <summary>
    /// 获取目标当前的所有 Buff 详细信息
    /// </summary>
    public List<BuffDetailInfo> GetBuffDetails(GameObject target)
    {
        var result = new List<BuffDetailInfo>();
        var buffList = BuffTestTool.Instance.GetTargetBuffs(target);

        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        if (buffTable == null)
            return result;

        foreach (var buff in buffList)
        {
            var row = buffTable.GetDataRow(buff.BuffId);
            if (row == null)
                continue;

            var info = new BuffDetailInfo
            {
                BuffId = buff.BuffId,
                Name = row.Name,
                Desc = row.Desc,
                BuffType = row.BuffType,
                EffectType = row.EffectType,
                StackCount = buff.StackCount,
            };

            // 根据 Buff 类型获取特定信息
            if (buff is StatModBuff statBuff)
            {
                info.SpecialInfo = $"属性修改 - 堆叠={statBuff.StackCount}";
            }
            else if (buff is BleedBuff bleedBuff)
            {
                info.SpecialInfo = $"流血伤害 - 堆叠={bleedBuff.StackCount}";
            }
            else if (buff is FrostBuff frostBuff)
            {
                info.SpecialInfo = $"冰冻控制 - 堆叠={frostBuff.StackCount}";
            }
            else if (buff is StunBuff stunBuff)
            {
                info.SpecialInfo = $"眩晕状态 - 堆叠={stunBuff.StackCount}";
            }
            else if (buff is BurnBuff burnBuff)
            {
                info.SpecialInfo = $"燃烧伤害 - 堆叠={burnBuff.StackCount}";
            }

            result.Add(info);
        }

        return result;
    }

    /// <summary>
    /// 验证 Buff 是否成功应用
    /// </summary>
    public BuffVerificationResult VerifyBuffApplied(int buffId, GameObject target)
    {
        var result = new BuffVerificationResult();
        result.BuffId = buffId;

        var buff = BuffTestTool.Instance.GetBuff(buffId, target);
        if (buff == null)
        {
            result.IsApplied = false;
            result.Message = "Buff 未找到";
            return result;
        }

        result.IsApplied = true;
        result.StackCount = buff.StackCount;

        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        if (buffTable != null)
        {
            var row = buffTable.GetDataRow(buffId);
            if (row != null)
            {
                result.BuffName = row.Name;
            }
        }

        result.Message = $"✓ Buff 已应用 (堆叠={result.StackCount})";
        return result;
    }

    /// <summary>
    /// 获取目标的 Buff 总数
    /// </summary>
    public int GetBuffCount(GameObject target)
    {
        return BuffTestTool.Instance.GetTargetBuffs(target).Count;
    }

    /// <summary>
    /// 统计增益和减益数量
    /// </summary>
    public (int buff, int debuff) GetBuffAndDebuffCount(GameObject target)
    {
        var buffList = BuffTestTool.Instance.GetTargetBuffs(target);
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();

        int buffCount = 0;
        int debuffCount = 0;

        foreach (var buff in buffList)
        {
            var row = buffTable?.GetDataRow(buff.BuffId);
            if (row == null)
                continue;

            if (row.BuffType == 1) // 增益
                buffCount++;
            else if (row.BuffType == 2) // 减益
                debuffCount++;
        }

        return (buffCount, debuffCount);
    }

    /// <summary>
    /// 获取是否存在控制状态（眩晕、冰冻等）
    /// </summary>
    public List<string> GetControlStates(GameObject target)
    {
        var result = new List<string>();
        var buffList = BuffTestTool.Instance.GetTargetBuffs(target);

        foreach (var buff in buffList)
        {
            if (buff is StunBuff)
                result.Add("眩晕");
            else if (buff is FrostBuff)
                result.Add("冰冻");
            // 可扩展其他控制状态
        }

        return result;
    }

    /// <summary>
    /// 生成测试报告
    /// </summary>
    public string GenerateTestReport(GameObject target)
    {
        var report = new System.Text.StringBuilder();

        var attr = GetTargetAttributes(target);
        var buffs = GetBuffDetails(target);
        var (buffCount, debuffCount) = GetBuffAndDebuffCount(target);
        var controls = GetControlStates(target);

        report.AppendLine($"=== {attr.Name} 的 Buff 测试报告 ===");
        report.AppendLine($"当前生命值: {attr.HP}/{attr.MaxHP}");
        report.AppendLine($"当前魔法值: {attr.MP}/{attr.MaxMP}");
        report.AppendLine($"\n【Buff 统计】");
        report.AppendLine($"增益: {buffCount} 个");
        report.AppendLine($"减益: {debuffCount} 个");
        report.AppendLine($"总计: {buffCount + debuffCount} 个");

        if (controls.Count > 0)
        {
            report.AppendLine($"\n【控制状态】");
            foreach (var control in controls)
            {
                report.AppendLine($"- {control}");
            }
        }

        if (buffs.Count > 0)
        {
            report.AppendLine($"\n【Buff 详情】");
            foreach (var buff in buffs)
            {
                report.AppendLine($"- {buff.Name} (ID={buff.BuffId})");
                report.AppendLine($"  堆叠: {buff.StackCount}");
                if (!string.IsNullOrEmpty(buff.SpecialInfo))
                {
                    report.AppendLine($"  {buff.SpecialInfo}");
                }
            }
        }

        return report.ToString();
    }

    #endregion
}

/// <summary>
/// 目标属性信息
/// </summary>
public struct TargetAttributeInfo
{
    public string Name;
    public float HP;
    public float MaxHP;
    public float MP;
    public float MaxMP;
    public float AtkDamage;
    public float AtkRange;
    public float PhysDef;
    public float MagicDef;
    public float Speed;

    public float HPPercent => MaxHP > 0 ? HP / MaxHP : 0;
    public float MPPercent => MaxMP > 0 ? MP / MaxMP : 0;
}

/// <summary>
/// Buff 详细信息
/// </summary>
public struct BuffDetailInfo
{
    public int BuffId;
    public string Name;
    public string Desc;
    public int BuffType;
    public int EffectType;
    public int StackCount;
    public string SpecialInfo;
}

/// <summary>
/// Buff 验证结果
/// </summary>
public struct BuffVerificationResult
{
    public int BuffId;
    public string BuffName;
    public bool IsApplied;
    public int StackCount;
    public string Message;
}
