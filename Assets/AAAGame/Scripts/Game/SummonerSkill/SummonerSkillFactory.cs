using System;
using System.Collections.Generic;

/// <summary>
/// 召唤师技能工厂
/// 注册并创建主动技能与被动技能实例
/// </summary>
public static class SummonerSkillFactory
{
    private static readonly Dictionary<int, Func<ISummonerSkill>> s_SkillCreators = new();
    private static readonly Dictionary<int, Func<ISummonerPassive>> s_PassiveCreators = new();

    /// <summary>注册所有已实现的召唤师技能（游戏启动时调用一次）</summary>
    public static void RegisterAll()
    {
        s_SkillCreators.Clear();
        s_PassiveCreators.Clear();

        // 狂战士技能
        RegisterPassive(101, () => new BerserkerPassive());     // 狂怒之心（被动）
        Register(102, () => new BerserkerActiveSkill());        // 战意激昂（主动）
    }

    public static void Register(int id, Func<ISummonerSkill> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"SummonerSkillFactory.Register: creator is null, id={id}");
            return;
        }
        s_SkillCreators[id] = creator;
    }

    public static void RegisterPassive(int id, Func<ISummonerPassive> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"SummonerSkillFactory.RegisterPassive: creator is null, id={id}");
            return;
        }
        s_PassiveCreators[id] = creator;
    }

    public static ISummonerSkill Create(int id)
    {
        if (s_SkillCreators.TryGetValue(id, out var creator))
            return creator();

        DebugEx.Error($"SummonerSkillFactory.Create: 未注册的主动技能 id={id}");
        return null;
    }

    public static ISummonerPassive CreatePassive(int id)
    {
        if (s_PassiveCreators.TryGetValue(id, out var creator))
            return creator();

        DebugEx.Error($"SummonerSkillFactory.CreatePassive: 未注册的被动技能 id={id}");
        return null;
    }
}
