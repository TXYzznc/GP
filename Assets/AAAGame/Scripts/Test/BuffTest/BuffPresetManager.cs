using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 预设管理器
/// 用于保存和加载常用的 Buff 组合
/// </summary>
public class BuffPresetManager
{
    #region 单例

    private static BuffPresetManager s_Instance;
    public static BuffPresetManager Instance => s_Instance ??= new BuffPresetManager();

    #endregion

    #region 字段

    private Dictionary<string, BuffPreset> m_Presets = new Dictionary<string, BuffPreset>();

    #endregion

    #region 初始化

    public BuffPresetManager()
    {
        InitializeDefaultPresets();
    }

    /// <summary>
    /// 初始化默认预设
    /// </summary>
    private void InitializeDefaultPresets()
    {
        // 伤害组合：攻击提升 + 出血
        AddPreset("伤害组合", new int[] { 10101, 10102 });

        // 控制组合：眩晕 + 冰冻
        AddPreset("控制组合", new int[] { 10104, 10105 });

        // 防守组合：护盾 + 防御提升
        AddPreset("防守组合", new int[] { 10301, 10103 });

        // 辅助组合：加血 + 速度提升
        AddPreset("辅助组合", new int[] { 10201, 10106 });

        DebugEx.LogModule("BuffPresetManager", "默认预设初始化完成");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加预设
    /// </summary>
    public void AddPreset(string name, int[] buffIds)
    {
        if (string.IsNullOrEmpty(name) || buffIds == null || buffIds.Length == 0)
        {
            DebugEx.WarningModule("BuffPresetManager", "预设名称或 Buff ID 列表为空");
            return;
        }

        var preset = new BuffPreset
        {
            Name = name,
            BuffIds = buffIds,
            CreatedTime = DateTime.Now,
        };

        m_Presets[name] = preset;
        DebugEx.LogModule("BuffPresetManager", $"保存预设: {name} (包含 {buffIds.Length} 个 Buff)");
    }

    /// <summary>
    /// 加载预设
    /// </summary>
    public int[] LoadPreset(string name)
    {
        if (m_Presets.TryGetValue(name, out var preset))
        {
            return preset.BuffIds;
        }

        DebugEx.WarningModule("BuffPresetManager", $"未找到预设: {name}");
        return new int[] { };
    }

    /// <summary>
    /// 删除预设
    /// </summary>
    public bool DeletePreset(string name)
    {
        if (m_Presets.Remove(name))
        {
            DebugEx.LogModule("BuffPresetManager", $"删除预设: {name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有预设
    /// </summary>
    public List<BuffPreset> GetAllPresets()
    {
        return new List<BuffPreset>(m_Presets.Values);
    }

    /// <summary>
    /// 检查预设是否存在
    /// </summary>
    public bool HasPreset(string name)
    {
        return m_Presets.ContainsKey(name);
    }

    /// <summary>
    /// 应用预设到目标
    /// </summary>
    public void ApplyPreset(string presetName, GameObject target, GameObject caster = null)
    {
        var buffIds = LoadPreset(presetName);
        if (buffIds.Length == 0)
        {
            DebugEx.WarningModule("BuffPresetManager", $"预设 {presetName} 不存在或为空");
            return;
        }

        BuffTestTool.Instance.ApplyBuffs(buffIds, target, caster);
        DebugEx.LogModule("BuffPresetManager", $"应用预设 '{presetName}' 到 {target.name}");
    }

    /// <summary>
    /// 清空所有预设
    /// </summary>
    public void ClearAllPresets()
    {
        m_Presets.Clear();
        DebugEx.LogModule("BuffPresetManager", "清空所有预设");
    }

    #endregion
}

/// <summary>
/// Buff 预设结构
/// </summary>
[System.Serializable]
public class BuffPreset
{
    public string Name;
    public int[] BuffIds;
    public DateTime CreatedTime;

    public override string ToString()
    {
        return $"{Name} ({BuffIds.Length} 个 Buff)";
    }
}
