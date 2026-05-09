using Newtonsoft.Json.Linq;
using System;
using GameFramework;
using UnityEngine;

/// <summary>
/// 游戏效果执行服务
/// 统一的效果执行入口，负责：
/// 1. Buff 类型效果的应用（通过 BuffApplyHelper）
/// 2. 自定义效果的执行（通过 ItemEffectFactory）
/// </summary>
public class GameEffectService : SingletonBase<GameEffectService>
{
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="effectId">效果 ID，指向 SpecialEffectTable</param>
    /// <param name="context">效果执行上下文</param>
    /// <returns>是否执行成功</returns>
    public bool Execute(int effectId, GameEffectContext context)
    {
        if (context == null || context.Targets == null)
        {
            DebugEx.Warning(nameof(GameEffectService), $"执行效果失败：上下文无效 effectId={effectId}");
            return false;
        }

        var effectData = ItemManager.Instance?.GetSpecialEffectData(effectId);
        if (effectData == null)
        {
            DebugEx.Warning(nameof(GameEffectService), $"执行效果失败：效果数据不存在 effectId={effectId}");
            return false;
        }

        DebugEx.Log(nameof(GameEffectService), $"执行效果 [{effectData.Name}] (ID:{effectId}, Source:{context.Source})");

        bool result = true;

        // 路径 1：应用 Buff 类型效果
        result &= ApplyBuffs(effectData, context);

        // 路径 2：执行自定义效果（JSON 驱动）
        result &= ExecuteCustomEffect(effectData, context);

        return result;
    }

    /// <summary>
    /// 应用 Buff 效果
    /// </summary>
    private bool ApplyBuffs(SpecialEffectData effectData, GameEffectContext context)
    {
        // BuffIds → 应用给目标列表
        if (!string.IsNullOrEmpty(effectData.EffectParams))
        {
            var buffIds = effectData.GetParamValue<int[]>("BuffIds", null);
            if (buffIds != null && buffIds.Length > 0)
            {
                foreach (int buffId in buffIds)
                {
                    if (buffId <= 0)
                        continue;

                    foreach (var target in context.Targets)
                    {
                        if (target == null)
                            continue;

                        BuffApplyHelper.ApplyBuff(buffId, target, true, context.Caster);
                        DebugEx.Log(nameof(GameEffectService), $"应用 Buff[{buffId}] 到目标");
                    }
                }
            }
        }

        // SelfBuffIds → 应用给来源方（Caster）
        if (!string.IsNullOrEmpty(effectData.EffectParams) && context.Caster != null)
        {
            var selfBuffIds = effectData.GetParamValue<int[]>("SelfBuffIds", null);
            if (selfBuffIds != null && selfBuffIds.Length > 0)
            {
                foreach (int buffId in selfBuffIds)
                {
                    if (buffId <= 0)
                        continue;

                    BuffApplyHelper.ApplyBuff(buffId, context.Caster, true, null);
                    DebugEx.Log(nameof(GameEffectService), $"应用自身 Buff[{buffId}] 到来源方");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 执行自定义效果（JSON 参数驱动）
    /// 例如：AddGold、RestoreHP、UnlockCard 等
    /// </summary>
    private bool ExecuteCustomEffect(SpecialEffectData effectData, GameEffectContext context)
    {
        string effectType = effectData.GetParamValue<string>("type", "");
        if (string.IsNullOrEmpty(effectType))
        {
            // 没有自定义效果类型，仅执行 Buff
            return true;
        }

        var effect = ItemEffectFactory.Create(effectType);
        if (effect == null)
        {
            DebugEx.Warning(nameof(GameEffectService), $"未知的自定义效果类型: {effectType}");
            return false;
        }

        // 构建 ItemEffectContext 供 IItemEffect 使用
        var itemContext = new ItemEffectContext(effectData, 0, context.Caster, context.SingleTarget);

        return effect.Execute(itemContext);
    }
}
