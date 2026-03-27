/// <summary>
/// 效果执行器
/// 统一处理技能/普攻的效果应用
/// </summary>
public static class EffectExecutor
{
    /// <summary>
    /// 在执行时应用 Buff（动画执行帧）
    /// 仅当 BuffTriggerType = 0 时应用
    /// </summary>
    /// <param name="config">技能配置</param>
    /// <param name="attacker">攻击者</param>
    /// <param name="target">目标（可为空）</param>
    public static void ApplyBuffsOnExecute(
        SummonChessSkillTable config,
        ChessEntity attacker,
        ChessEntity target)
    {
        if (config == null || attacker == null) return;

        // 只有 BuffTriggerType = 0 时才在执行时应用
        if (config.BuffTriggerType != 0) return;

        ApplyBuffs(config, attacker, target, "执行时");
    }

    /// <summary>
    /// 在命中时应用 Buff（HitDetector 检测到命中）
    /// 仅当 BuffTriggerType = 1 时应用
    /// </summary>
    /// <param name="config">技能配置</param>
    /// <param name="attacker">攻击者</param>
    /// <param name="target">目标（可为空）</param>
    public static void ApplyBuffsOnHit(
        SummonChessSkillTable config,
        ChessEntity attacker,
        ChessEntity target)
    {
        if (config == null || attacker == null) return;

        // 只有 BuffTriggerType = 1 时才在命中时应用
        if (config.BuffTriggerType != 1) return;

        ApplyBuffs(config, attacker, target, "命中时");
    }

    /// <summary>
    /// 实际应用 Buff 的内部方法
    /// </summary>
    private static void ApplyBuffs(
        SummonChessSkillTable config,
        ChessEntity attacker,
        ChessEntity target,
        string timingName)
    {
        // 1. 给目标添加 Buff
        if (target != null && config.BuffIds != null && config.BuffIds.Length > 0)
        {
            foreach (int buffId in config.BuffIds)
            {
                if (buffId == 0) continue; // 跳过无效ID

                target.BuffManager?.AddBuff(
                    buffId, 
                    attacker.gameObject, 
                    attacker.Attribute);
                    
                DebugEx.LogModule("EffectExecutor", 
                    $"[{timingName}] {attacker.Config?.Name} 给 {target.Config?.Name} 添加 Buff {buffId}");
            }
        }

        // 2. 给自己添加 Buff
        if (config.SelfBuffIds != null && config.SelfBuffIds.Length > 0)
        {
            foreach (int buffId in config.SelfBuffIds)
            {
                if (buffId == 0) continue; // 跳过无效ID

                attacker.BuffManager?.AddBuff(
                    buffId, 
                    attacker.gameObject, 
                    attacker.Attribute);
                    
                DebugEx.LogModule("EffectExecutor", 
                    $"[{timingName}] {attacker.Config?.Name} 给自己添加 Buff {buffId}");
            }
        }
    }
}
