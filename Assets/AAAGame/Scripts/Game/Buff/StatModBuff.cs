using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 通用属性修改 Buff
/// 用于烈焰箭(104)、日月长弓(105)、玄冰减益(106)等
/// </summary>
public class StatModBuff : BuffBase
{
    #region 属性修改定义

    public enum StatType
    {
        AtkDamage,
        AtkSpeed,
        AtkRange,
        Armor,
        MagicResist,
        MoveSpeed,
        SpellPower,
        CritRate,
        CritDamage,
        CooldownReduce,
        Shield,
        DamageTakenMultiplier
    }

    public struct StatMod
    {
        public StatType Type;
        public double Value;
        public bool IsPercent;

        public StatMod(StatType type, double value, bool isPercent = false)
        {
            Type = type;
            Value = value;
            IsPercent = isPercent;
        }
    }

    #endregion

    #region 私有字段

    private StatMod[] m_Mods;
    private double[] m_AppliedValues;
    private bool m_IsApplied;

    #endregion

    #region 初始化

    /// <summary>
    /// 设置属性修改列表（在工厂创建后Init之前调用）
    /// </summary>
    public void SetMods(params StatMod[] mods)
    {
        m_Mods = mods ?? Array.Empty<StatMod>();
        m_AppliedValues = new double[m_Mods.Length];
    }

    #endregion

    #region 公共方法

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        if (m_Mods == null || m_Mods.Length == 0)
        {
            TryInitModsFromConfig();
        }
    }

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyMods();
    }

    public override void OnExit()
    {
        RestoreMods();
        base.OnExit();
    }

    public override bool OnStack()
    {
        // 刷新持续时间，重新计算属性修改
        bool result = base.OnStack();
        RestoreMods();
        ApplyMods();
        return result;
    }

    #endregion

    #region 私有方法

    private void ApplyMods()
    {
        if (m_Mods == null || Ctx?.OwnerAttribute == null) return;
        if (m_IsApplied) return;

        for (int i = 0; i < m_Mods.Length; i++)
        {
            var mod = m_Mods[i];
            double actualValue = mod.Value;

            // 百分比修改，基于当前属性值计算
            if (mod.IsPercent)
            {
                actualValue = GetStatValue(mod.Type) * mod.Value;
            }

            ApplyStatChange(mod.Type, actualValue);
            m_AppliedValues[i] = actualValue;
        }

        m_IsApplied = true;
        DebugEx.LogModule("StatModBuff", $"Buff(ID={BuffId}) 属性修改已应用，共{m_Mods.Length}项");
    }

    private void RestoreMods()
    {
        if (!m_IsApplied || m_Mods == null || Ctx?.OwnerAttribute == null) return;

        for (int i = 0; i < m_Mods.Length; i++)
        {
            ApplyStatChange(m_Mods[i].Type, -m_AppliedValues[i]);
        }

        m_IsApplied = false;
    }

    private double GetStatValue(StatType type)
    {
        var attr = Ctx.OwnerAttribute;
        switch (type)
        {
            case StatType.AtkDamage: return attr.AtkDamage;
            case StatType.AtkSpeed: return attr.AtkSpeed;
            case StatType.AtkRange: return attr.AtkRange;
            case StatType.Armor: return attr.Armor;
            case StatType.MagicResist: return attr.MagicResist;
            case StatType.MoveSpeed: return attr.MoveSpeed;
            case StatType.SpellPower: return attr.SpellPower;
            case StatType.CritRate: return attr.CritRate;
            case StatType.CritDamage: return attr.CritDamage;
            case StatType.CooldownReduce: return attr.CooldownReduce;
            case StatType.Shield: return attr.Shield;
            case StatType.DamageTakenMultiplier: return attr.DamageTakenMultiplier;
            default: return 0;
        }
    }

    private void ApplyStatChange(StatType type, double value)
    {
        var attr = Ctx.OwnerAttribute;
        switch (type)
        {
            case StatType.AtkDamage: attr.ModifyAtkDamage(value); break;
            case StatType.AtkSpeed: attr.ModifyAtkSpeed(value); break;
            case StatType.AtkRange: attr.ModifyAtkRange(value); break;
            case StatType.Armor: attr.ModifyArmor(value); break;
            case StatType.MagicResist: attr.ModifyMagicResist(value); break;
            case StatType.MoveSpeed: attr.ModifyMoveSpeed(value); break;
            case StatType.SpellPower: attr.ModifySpellPower(value); break;
            case StatType.CritRate: attr.ModifyCritRate(value); break;
            case StatType.CritDamage: attr.ModifyCritDamage(value); break;
            case StatType.CooldownReduce: attr.ModifyCooldownReduce(value); break;
            case StatType.Shield: attr.ModifyShield(value); break;
            case StatType.DamageTakenMultiplier: attr.ModifyDamageTakenMultiplier(value); break;
        }
    }

    private void TryInitModsFromConfig()
    {
        if (Config == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(Config.StatMods) || Config.StatMods == "{}")
        {
            return;
        }

        JObject obj;
        try
        {
            obj = JObject.Parse(Config.StatMods);
        }
        catch (Exception e)
        {
            DebugEx.WarningModule("StatModBuff", $"Buff(ID={BuffId}) StatMods JSON解析失败: {e.Message}");
            return;
        }

        var mods = new List<StatMod>();
        foreach (var property in obj.Properties())
        {
            if (!Enum.TryParse(property.Name, out StatType statType))
            {
                DebugEx.WarningModule("StatModBuff", $"Buff(ID={BuffId}) 未知StatType: {property.Name}");
                continue;
            }

            if (!TryParseModValue(property.Value, out double value, out bool isPercent))
            {
                DebugEx.WarningModule("StatModBuff", $"Buff(ID={BuffId}) StatMods[{property.Name}] 值解析失败: {property.Value}");
                continue;
            }

            mods.Add(new StatMod(statType, value, isPercent));
        }

        if (mods.Count <= 0)
        {
            return;
        }

        SetMods(mods.ToArray());
        DebugEx.LogModule("StatModBuff", $"Buff(ID={BuffId}) 从配置初始化属性修改，共{mods.Count}项");
    }

    private bool TryParseModValue(JToken token, out double value, out bool isPercent)
    {
        value = 0;
        isPercent = false;

        if (token == null)
        {
            return false;
        }

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            value = token.ToObject<double>();
            isPercent = false;
            return true;
        }

        if (token.Type == JTokenType.String)
        {
            string s = token.ToString().Trim();
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (s.EndsWith("%"))
            {
                string numStr = s.Substring(0, s.Length - 1);
                if (!double.TryParse(numStr, out var p))
                {
                    return false;
                }

                value = p / 100.0;
                isPercent = true;
                return true;
            }

            if (!double.TryParse(s, out var v))
            {
                return false;
            }

            value = v;
            isPercent = false;
            return true;
        }

        return false;
    }

    #endregion
}
