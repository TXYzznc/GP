using System.Collections.Generic;

/// <summary>
/// 战意激昂——狂战士固定主动技能
///
/// 条件：冷却为 0 且 灵力足够 且 HP &gt; Params[0]（20）
/// 效果：
///   1. 召唤师 HP 减少 Params[0]
///   2. 全体友方棋子获得 BuffIds[0]（攻速 +20%，持续 Duration 秒）
///   3. 全体友方棋子获得 BuffIds[1]（伤害 +15%，持续 Duration 秒）
/// 数值全部来自配置表，不硬编码
/// </summary>
public class BerserkerActiveSkill : SummonerSkillBase
{
    private const int PLAYER_CAMP = 0;

    public override bool CanCast()
    {
        if (!base.CanCast())
            return false;

        float[] p = m_Config?.Params;
        if (p == null || p.Length < 1)
            return false;

        // 额外检查：当前 HP 必须大于消耗值
        return m_Ctx.RuntimeData.CurrentHP > p[0];
    }

    protected override void ExecuteSkill()
    {
        float[] p = m_Config.Params;
        int[] buffIds = m_Config.BuffIds;

        if (p == null || p.Length < 1 || buffIds == null || buffIds.Length < 2)
        {
            DebugEx.Error("[BerserkerActiveSkill] 配置 Params/BuffIds 不完整");
            return;
        }

        // 1. 扣减生命值
        m_Ctx.RuntimeData.ReduceHP(p[0]);

        // 2 & 3. 全体友方棋子施加 Buff
        ApplyBuffToAllAllies(buffIds[0]);
        ApplyBuffToAllAllies(buffIds[1]);

        DebugEx.Log($"[BerserkerActiveSkill] 战意激昂触发：扣 HP {p[0]}，施加攻速/伤害 Buff 到全体友方");
    }

    private void ApplyBuffToAllAllies(int buffId)
    {
        List<ChessEntity> allies = m_Ctx.EntityTracker.GetAllies(PLAYER_CAMP);
        for (int i = 0; i < allies.Count; i++)
        {
            var bm = allies[i]?.GetComponent<BuffManager>();
            bm?.AddBuff(buffId);
        }
    }
}
