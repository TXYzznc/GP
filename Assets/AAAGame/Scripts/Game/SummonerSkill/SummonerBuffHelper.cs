using System.Collections.Generic;

/// <summary>
/// 召唤师技能 Buff 施加/移除工具类
/// 根据 BuffTargetEntry.TargetType 将 Buff 分发到正确目标
/// </summary>
public static class SummonerBuffHelper
{
    private const int PLAYER_CAMP = 0;
    private const int ENEMY_CAMP  = 1;

    /// <summary>施加单条 Buff 条目</summary>
    public static void ApplyBuff(SummonerSkillContext ctx, BuffTargetEntry entry)
        => ApplyBuff(ctx, entry.BuffId, entry.TargetType);

    /// <summary>施加 Buff 数组中所有条目</summary>
    public static void ApplyBuffs(SummonerSkillContext ctx, BuffTargetEntry[] entries)
    {
        if (entries == null) return;
        foreach (var e in entries) ApplyBuff(ctx, e.BuffId, e.TargetType);
    }

    /// <summary>移除单条 Buff 条目</summary>
    public static void RemoveBuff(SummonerSkillContext ctx, BuffTargetEntry entry)
        => RemoveBuff(ctx, entry.BuffId, entry.TargetType);

    /// <summary>移除 Buff 数组中所有条目</summary>
    public static void RemoveBuffs(SummonerSkillContext ctx, BuffTargetEntry[] entries)
    {
        if (entries == null) return;
        foreach (var e in entries) RemoveBuff(ctx, e.BuffId, e.TargetType);
    }

    // ── 带明确目标的重载（供 HitBuffs 等传入具体命中目标使用）──

    /// <summary>
    /// 施加单条 HitBuff：TargetType=5 时作用于 hitTarget，其余同 ApplyBuff
    /// </summary>
    public static void ApplyHitBuff(SummonerSkillContext ctx, BuffTargetEntry entry, BuffManager hitTarget)
    {
        if (entry.TargetType == 5)
            hitTarget?.AddBuff(entry.BuffId);
        else
            ApplyBuff(ctx, entry.BuffId, entry.TargetType);
    }

    public static void ApplyHitBuffs(SummonerSkillContext ctx, BuffTargetEntry[] entries, BuffManager hitTarget)
    {
        if (entries == null) return;
        foreach (var e in entries) ApplyHitBuff(ctx, e, hitTarget);
    }

    // ── 内部实现 ──

    private static void ApplyBuff(SummonerSkillContext ctx, int buffId, int targetType)
    {
        switch (targetType)
        {
            case 1: ctx.SummonerBuffManager?.AddBuff(buffId); break;
            case 2: ApplyToAllies(ctx, buffId, false); break;
            case 3: ApplyToAllies(ctx, buffId, true);  break;
            case 4: ApplyToCamp(ctx, ENEMY_CAMP, buffId); break;
        }
    }

    private static void RemoveBuff(SummonerSkillContext ctx, int buffId, int targetType)
    {
        switch (targetType)
        {
            case 1: ctx.SummonerBuffManager?.RemoveBuff(buffId); break;
            case 2: RemoveFromAllies(ctx, buffId, false); break;
            case 3: RemoveFromAllies(ctx, buffId, true);  break;
            case 4: RemoveFromCamp(ctx, ENEMY_CAMP, buffId); break;
        }
    }

    private static void ApplyToAllies(SummonerSkillContext ctx, int buffId, bool includeSummoner)
    {
        ApplyToCamp(ctx, PLAYER_CAMP, buffId);
        if (includeSummoner) ctx.SummonerBuffManager?.AddBuff(buffId);
    }

    private static void RemoveFromAllies(SummonerSkillContext ctx, int buffId, bool includeSummoner)
    {
        RemoveFromCamp(ctx, PLAYER_CAMP, buffId);
        if (includeSummoner) ctx.SummonerBuffManager?.RemoveBuff(buffId);
    }

    private static void ApplyToCamp(SummonerSkillContext ctx, int camp, int buffId)
    {
        List<ChessEntity> list = ctx.EntityTracker.GetAllies(camp);
        for (int i = 0; i < list.Count; i++)
            list[i]?.GetComponent<BuffManager>()?.AddBuff(buffId);
    }

    private static void RemoveFromCamp(SummonerSkillContext ctx, int camp, int buffId)
    {
        List<ChessEntity> list = ctx.EntityTracker.GetAllies(camp);
        for (int i = 0; i < list.Count; i++)
            list[i]?.GetComponent<BuffManager>()?.RemoveBuff(buffId);
    }
}
