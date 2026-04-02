using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 卡牌效果辅助类
/// </summary>
public static class CardEffectHelper
{
    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    public static void DealDamage(ChessEntity target, float damage, int damageType = 0)
    {
        if (target == null || target.Attribute == null)
        {
            DebugEx.ErrorModule("CardEffectHelper", "目标或目标属性为空");
            return;
        }

        // damageType: 0=物理, 1=魔法, 2=真实
        bool isMagic = damageType == 1;
        bool isTrueDamage = damageType == 2;

        target.Attribute.TakeDamage(damage, isMagic, isTrueDamage);
        DebugEx.LogModule("CardEffectHelper", $"对 {target.Config?.Name} 造成 {damage} 伤害 (类型: {damageType})");
    }

    /// <summary>
    /// 恢复目标 HP
    /// </summary>
    public static void HealTarget(ChessEntity target, float healAmount)
    {
        if (target == null || target.Attribute == null)
        {
            DebugEx.ErrorModule("CardEffectHelper", "目标或目标属性为空");
            return;
        }

        target.Attribute.ModifyHp(healAmount);
        DebugEx.LogModule("CardEffectHelper", $"恢复 {target.Config?.Name} {healAmount} HP");
    }

    /// <summary>
    /// 应用 Buff 效果
    /// </summary>
    public static void ApplyBuff(ChessEntity target, int buffId, GameObject caster = null)
    {
        if (target == null)
        {
            DebugEx.ErrorModule("CardEffectHelper", "目标为空");
            return;
        }

        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            DebugEx.ErrorModule("CardEffectHelper", $"{target.Config?.Name} 没有 BuffManager 组件");
            return;
        }

        buffManager.AddBuff(buffId, caster);
        DebugEx.LogModule("CardEffectHelper", $"对 {target.Config?.Name} 应用 Buff: {buffId}");
    }

    /// <summary>
    /// 播放特效
    /// </summary>
    public static void PlayEffect(int effectId, Vector3 position, ChessEntity target = null)
    {
        if (effectId <= 0)
        {
            DebugEx.WarningModule("CardEffectHelper", "特效 ID 无效");
            return;
        }

        // 通过 CombatVFXManager 播放特效（静态类，直接调用）
        CombatVFXManager.PlayEffect(effectId, position);
        DebugEx.LogModule("CardEffectHelper", $"播放特效: ID={effectId}, 位置={position}");
    }
}
