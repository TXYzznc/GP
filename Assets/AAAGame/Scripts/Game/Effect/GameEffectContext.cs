using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏效果来源分类
/// </summary>
public enum EffectSource
{
    Item,        // 消耗品使用
    Synergy,     // 羁绊激活
    CombatPrep,  // 战斗准备（先手/偷袭效果）
}

/// <summary>
/// 游戏效果执行上下文
/// 统一的效果执行参数封装，支持 Buff 应用、自定义效果执行等
/// </summary>
public class GameEffectContext
{
    /// <summary>
    /// 效果来源
    /// </summary>
    public EffectSource Source { get; set; }

    /// <summary>
    /// 目标对象列表（可以是棋子、玩家等）
    /// 多目标用于：棋子羁绊（作用于全体出战棋子）、AOE 效果等
    /// </summary>
    public List<GameObject> Targets { get; set; } = new();

    /// <summary>
    /// 效果来源对象（施法者）
    /// 用于 SelfBuff 的应用对象，可以为 null
    /// 例如：玩家先手的 SelfBuff 应该应用给玩家方
    /// </summary>
    public GameObject Caster { get; set; }

    /// <summary>
    /// 便捷属性：单个目标
    /// 如果只有一个目标，可以用这个代替 Targets[0]
    /// </summary>
    public GameObject SingleTarget => Targets?.Count > 0 ? Targets[0] : null;

    /// <summary>
    /// 创建单目标上下文
    /// </summary>
    public static GameEffectContext CreateSingleTarget(EffectSource source, GameObject target, GameObject caster = null)
    {
        return new GameEffectContext
        {
            Source = source,
            Targets = target != null ? new List<GameObject> { target } : new List<GameObject>(),
            Caster = caster
        };
    }

    /// <summary>
    /// 创建多目标上下文
    /// </summary>
    public static GameEffectContext CreateMultiTarget(EffectSource source, List<GameObject> targets, GameObject caster = null)
    {
        return new GameEffectContext
        {
            Source = source,
            Targets = targets ?? new List<GameObject>(),
            Caster = caster
        };
    }
}
