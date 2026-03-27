/// <summary>
/// 战意激昂——狂战士固定主动技能
///
/// 条件：冷却为 0 且 灵力足够 且 HP > Params[0]
/// 效果：
///   1. 召唤师 HP 减少 Params[0]（20）
///   2. 按 InstantBuffs 施加 Buff（4001:3 → 全体友方含召唤师）
/// </summary>
public class BerserkerActiveSkill : SummonerSkillBase
{
    private float m_HpCost;
    private BuffTargetEntry[] m_InstantEntries;

    public override void Init(SummonerSkillContext ctx, SummonerSkillTable config)
    {
        base.Init(ctx, config);

        float[] p = config?.Params;
        m_HpCost = (p != null && p.Length >= 1) ? p[0] : 0f;
        m_InstantEntries = BuffTargetEntry.ParseArray(config?.InstantBuffs);
    }

    public override bool CanCast()
    {
        if (!base.CanCast()) return false;
        return m_Ctx.RuntimeData.CurrentHP > m_HpCost;
    }

    protected override void ExecuteSkill()
    {
        if (m_InstantEntries == null || m_InstantEntries.Length == 0)
        {
            DebugEx.Error("[BerserkerActiveSkill] 配置 InstantBuffs 不完整");
            return;
        }

        m_Ctx.RuntimeData.ReduceHP(m_HpCost);
        SummonerBuffHelper.ApplyBuffs(m_Ctx, m_InstantEntries);

        DebugEx.Log($"[BerserkerActiveSkill] 战意激昂触发：扣 HP {m_HpCost}，施加 InstantBuffs 到全体友方（含召唤师）");
    }
}
