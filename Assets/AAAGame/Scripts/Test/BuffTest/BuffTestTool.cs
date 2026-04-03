using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// Buff 测试工具核心逻辑
/// 提供快速应用、移除、编辑 Buff 的功能
/// </summary>
public class BuffTestTool
{
    #region 单例

    private static BuffTestTool s_Instance;
    public static BuffTestTool Instance => s_Instance ??= new BuffTestTool();

    #endregion

    #region 事件

    /// <summary>Buff 应用事件</summary>
    public event Action<GameObject, int> OnBuffApplied;

    /// <summary>Buff 移除事件</summary>
    public event Action<GameObject, int> OnBuffRemoved;

    /// <summary>Buff 列表变化事件</summary>
    public event Action<GameObject> OnBuffListChanged;

    #endregion

    #region 公共方法

    /// <summary>
    /// 应用单个 Buff 到目标
    /// </summary>
    public void ApplyBuffToTarget(int buffId, GameObject target, GameObject caster = null)
    {
        if (target == null)
        {
            DebugEx.ErrorModule("BuffTestTool", "目标对象为空");
            return;
        }

        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            DebugEx.ErrorModule("BuffTestTool", $"目标 {target.name} 没有 BuffManager 组件");
            return;
        }

        buffManager.AddBuff(buffId, caster);
        OnBuffApplied?.Invoke(target, buffId);

        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        var buffName = buffTable?.GetDataRow(buffId)?.Name ?? $"Buff_{buffId}";
        DebugEx.LogModule("BuffTestTool", $"✓ 应用 Buff: {buffName} (ID={buffId}) 到 {target.name}");

        OnBuffListChanged?.Invoke(target);
    }

    /// <summary>
    /// 批量应用多个 Buff
    /// </summary>
    public void ApplyBuffs(int[] buffIds, GameObject target, GameObject caster = null)
    {
        if (buffIds == null || buffIds.Length == 0)
        {
            DebugEx.WarningModule("BuffTestTool", "Buff ID 列表为空");
            return;
        }

        foreach (var buffId in buffIds)
        {
            ApplyBuffToTarget(buffId, target, caster);
        }

        DebugEx.LogModule("BuffTestTool", $"批量应用 {buffIds.Length} 个 Buff 到 {target.name}");
    }

    /// <summary>
    /// 从目标移除指定 Buff
    /// </summary>
    public bool RemoveBuffFromTarget(int buffId, GameObject target)
    {
        if (target == null)
        {
            DebugEx.ErrorModule("BuffTestTool", "目标对象为空");
            return false;
        }

        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            DebugEx.ErrorModule("BuffTestTool", $"目标 {target.name} 没有 BuffManager 组件");
            return false;
        }

        // 检查是否存在
        if (!buffManager.HasBuff(buffId))
        {
            DebugEx.WarningModule("BuffTestTool", $"✗ 目标 {target.name} 上没有 Buff (ID={buffId})");
            return false;
        }

        buffManager.RemoveBuff(buffId);
        OnBuffRemoved?.Invoke(target, buffId);
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        var buffName = buffTable?.GetDataRow(buffId)?.Name ?? $"Buff_{buffId}";
        DebugEx.LogModule("BuffTestTool", $"✓ 移除 Buff: {buffName} (ID={buffId}) 从 {target.name}");
        OnBuffListChanged?.Invoke(target);

        return true;
    }

    /// <summary>
    /// 清空目标的所有 Buff
    /// </summary>
    public void ClearAllBuffs(GameObject target)
    {
        if (target == null)
        {
            DebugEx.ErrorModule("BuffTestTool", "目标对象为空");
            return;
        }

        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            DebugEx.ErrorModule("BuffTestTool", $"目标 {target.name} 没有 BuffManager 组件");
            return;
        }

        var buffList = GetTargetBuffs(target);
        int count = buffList.Count;

        foreach (var buff in buffList.ToList())
        {
            buffManager.RemoveBuff(buff.BuffId);
        }

        DebugEx.LogModule("BuffTestTool", $"✓ 清空 {target.name} 的所有 Buff（共 {count} 个）");
        OnBuffListChanged?.Invoke(target);
    }

    /// <summary>
    /// 获取目标的所有 Buff 列表
    /// </summary>
    public List<IBuff> GetTargetBuffs(GameObject target)
    {
        if (target == null)
            return new List<IBuff>();

        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null)
            return new List<IBuff>();

        return new List<IBuff>(buffManager.GetAllBuffs());
    }

    /// <summary>
    /// 获取目标的指定 Buff
    /// </summary>
    public IBuff GetBuff(int buffId, GameObject target)
    {
        var buffList = GetTargetBuffs(target);
        return buffList.FirstOrDefault(b => b.BuffId == buffId);
    }

    /// <summary>
    /// 修改 Buff 参数（仅支持基础参数）
    /// </summary>
    public void ModifyBuffParameter(int buffId, GameObject target, BuffParameterType paramType, float value)
    {
        var buff = GetBuff(buffId, target);
        if (buff == null)
        {
            DebugEx.WarningModule("BuffTestTool", $"未找到 Buff (ID={buffId})");
            return;
        }

        // 这里简化处理，只支持修改基础参数
        // 实际复杂的修改需要反射或特定的接口支持
        DebugEx.LogModule("BuffTestTool", $"修改 Buff (ID={buffId}) 参数: {paramType} = {value}");
    }

    /// <summary>
    /// 获取所有可用 Buff 列表
    /// </summary>
    public List<BuffInfo> GetAllAvailableBuffs()
    {
        var result = new List<BuffInfo>();
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();

        if (buffTable == null)
        {
            DebugEx.ErrorModule("BuffTestTool", "BuffTable 未加载");
            return result;
        }

        var allRows = buffTable.GetAllDataRows();
        if (allRows != null)
        {
            foreach (var row in allRows)
            {
                result.Add(new BuffInfo
                {
                    BuffId = row.Id,
                    Name = row.Name,
                    Desc = row.Desc,
                    BuffType = row.BuffType,
                    EffectType = row.EffectType,
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 获取目标的实时属性修正信息
    /// </summary>
    public BuffEffectInfo GetTargetEffectInfo(GameObject target)
    {
        if (target == null)
            return new BuffEffectInfo();

        var attribute = target.GetComponent<ChessAttribute>();
        if (attribute == null)
            return new BuffEffectInfo();

        var info = new BuffEffectInfo();
        var buffList = GetTargetBuffs(target);

        // 收集所有修正值
        foreach (var buff in buffList)
        {
            if (buff is StatModBuff statBuff)
            {
                // 这里需要实现获取 StatModBuff 的修正值
                // 暂时作为扩展接口预留
            }
        }

        info.CurrentHP = (float)attribute.CurrentHp;
        info.MaxHP = (float)attribute.MaxHp;
        info.CurrentMP = (float)attribute.CurrentMp;
        info.MaxMP = (float)attribute.MaxMp;
        info.BuffCount = buffList.Count;

        return info;
    }

    #endregion
}

/// <summary>
/// Buff 信息（用于显示可用的 Buff）
/// </summary>
public struct BuffInfo
{
    public int BuffId;
    public string Name;
    public string Desc;
    public int BuffType; // 1=增益, 2=减益
    public int EffectType; // 1=属性修改, 2=周期性, 3=护盾, 4=状态改变, 5=特殊逻辑
}

/// <summary>
/// Buff 参数类型
/// </summary>
public enum BuffParameterType
{
    Duration,      // 持续时间
    StackCount,    // 堆叠层数
    EffectValue,   // 效果数值
    Interval,      // 触发间隔
}

/// <summary>
/// Buff 效果信息（用于显示当前效果）
/// </summary>
public struct BuffEffectInfo
{
    public float CurrentHP;
    public float MaxHP;
    public float CurrentMP;
    public float MaxMP;
    public int BuffCount;
    // 可扩展其他属性修正信息
}
