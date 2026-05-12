using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏效果执行服务（兼容层）
/// 保持原有接口，内部委托给 SpecialEffectManager
/// </summary>
public class GameEffectService : SingletonBase<GameEffectService>
{
    /// <summary>
    /// 执行效果（兼容接口）
    /// </summary>
    public bool Execute(int effectId, GameEffectContext context)
    {
        if (context == null || context.Targets == null)
        {
            DebugEx.Warning(nameof(GameEffectService), $"执行效果失败：上下文无效 effectId={effectId}");
            return false;
        }

        var sourceType = MapSourceType(context.Source);
        var manager = SpecialEffectManager.Instance;

        if (context.Targets.Count > 1)
        {
            return manager.ApplyEffect(effectId, context.Targets, sourceType, effectId, context.Caster);
        }
        else
        {
            return manager.ApplyEffect(effectId, context.SingleTarget, sourceType, effectId);
        }
    }

    private EffectSourceType MapSourceType(EffectSource source)
    {
        switch (source)
        {
            case EffectSource.Item:
                return EffectSourceType.Consumable;
            case EffectSource.Synergy:
                return EffectSourceType.Synergy;
            case EffectSource.CombatPrep:
                return EffectSourceType.Combat;
            default:
                return EffectSourceType.Consumable;
        }
    }
}
