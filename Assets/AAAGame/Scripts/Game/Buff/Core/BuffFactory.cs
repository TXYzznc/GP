using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 工厂，负责创建 Buff 实例
/// </summary>
public static class BuffFactory
{
    private static readonly Dictionary<int, Func<IBuff>> s_Creators = new();

    /// <summary>
    /// 注册所有 Buff（在游戏启动时调用）
    /// </summary>
    public static void RegisterAll()
    {
        s_Creators.Clear();

        // 元素效果 Buff
        Register(1, () => new BurnBuff());       // 灼烧
        Register(2, () => new FrostBuff());       // 冰霜
        Register(3, () => new MeltBuff());        // 融化

        Register(4, () => new StatModBuff());      // 神力增益（配置驱动）
        Register(5, () => new StatModBuff());      // 日落长弓（配置驱动）
        Register(6, () => new StatModBuff());      // 九天玄冰（配置驱动）

        Register(2001, () => new StatModBuff());
        Register(2002, () => new StatModBuff());
        Register(2003, () => new StatModBuff());
        Register(2004, () => new StatModBuff());

        Register(3001, () => new StatModBuff());
        Register(3002, () => new StunBuff());
        Register(3003, () => new BleedBuff());

        // 狂战士技能 Buff
        Register(4001, () => new StatModBuff());      // 战意激昂（攻速+20% 伤害+15%，配置驱动）
        Register(4003, () => new BerserkerRageBuff()); // 狂怒之心（普通单位+15%，召唤师+30%）

        DebugEx.LogModule("BuffFactory", $"注册了 {s_Creators.Count} 个 Buff");
    }

    public static void Register(int buffId, Func<IBuff> creator)
    {
        if (creator == null)
        {
            DebugEx.ErrorModule("BuffFactory", $"Register creator is null, id={buffId}");
            return;
        }

        if (s_Creators.ContainsKey(buffId))
        {
            DebugEx.WarningModule("BuffFactory", $"Buff ID {buffId} 已经注册过了，将被覆盖");
        }

        s_Creators[buffId] = creator;
    }

    /// <summary>
    /// 创建 Buff 实例（仅实例化，未初始化）
    /// </summary>
    public static IBuff Create(int buffId)
    {
        if (s_Creators.TryGetValue(buffId, out var creator))
        {
            return creator();
        }

        // 如果没有特定的实现，可以返回一个通用的 Buff 类（纯逻辑，不依赖表）
        // 或者返回 null
        // DebugEx.Warning("BuffFactory", $"未找到 ID 为 {buffId} 的 Buff 实现类");
        return null;
    }
}

