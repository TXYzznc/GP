using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillFactory
{
    private static readonly Dictionary<int, Func<IPlayerSkill>> s_Creators = new();

    /// <summary>
    /// 在游戏启动时注册所有技能（建议只调用一次）
    /// </summary>
    public static void RegisterAll()
    {
        s_Creators.Clear();

        // 示例：将技能ID与具体技能类型绑定，绑定的技能从通用配置表中获取
        Register(1, () => new DashSkill());
        Register(2, () => new HealSkill());
        Register(3, () => new FireBallSkill());
    }

    public static void Register(int skillId, Func<IPlayerSkill> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"SkillFactory.Register creator is null, id={skillId}");
            return;
        }
        s_Creators[skillId] = creator;
    }

    public static IPlayerSkill Create(int skillId)
    {
        if (s_Creators.TryGetValue(skillId, out var creator))
            return creator();

        return null;
    }
}
