using System.Collections.Generic;

/// <summary>
/// 狂怒之心——狂战士固定被动
///
/// 条件：战场有任意友方棋子 HP &lt; Params[0] × 最大 HP
///   → 按 InstantBuffs 施加（4003:3 → 全体友方含召唤师）
/// 条件解除时移除所有已施加的 Buff
/// </summary>
public class BerserkerPassive : SummonerPassiveBase
{
    private const int PLAYER_CAMP = 0;
    private float m_HpThreshold;
    private BuffTargetEntry[] m_InstantEntries;

    public override void Init(SummonerSkillContext ctx, SummonerSkillTable config)
    {
        base.Init(ctx, config);

        float[] p = config?.Params;
        m_HpThreshold = (p != null && p.Length >= 1) ? p[0] : 0.5f;
        m_InstantEntries = BuffTargetEntry.ParseArray(config?.InstantBuffs);
    }

    protected override void OnTick(float dt)
    {
        if (m_Ctx?.EntityTracker == null) return;
        if (m_InstantEntries == null || m_InstantEntries.Length == 0) return;

        bool anyAllyLow = CheckAnyAllyLowHP(m_HpThreshold);

        if (anyAllyLow && !m_IsActive)
        {
            SummonerBuffHelper.ApplyBuffs(m_Ctx, m_InstantEntries);
            m_IsActive = true;
            DebugEx.Log("[BerserkerPassive] 激活：条件满足，已施加 InstantBuffs");
        }
        else if (!anyAllyLow && m_IsActive)
        {
            SummonerBuffHelper.RemoveBuffs(m_Ctx, m_InstantEntries);
            m_IsActive = false;
            DebugEx.Log("[BerserkerPassive] 解除：条件不满足，已移除 InstantBuffs");
        }
    }

    protected override void OnDispose()
    {
        if (!m_IsActive || m_InstantEntries == null) return;
        SummonerBuffHelper.RemoveBuffs(m_Ctx, m_InstantEntries);
        m_IsActive = false;
    }

    private bool CheckAnyAllyLowHP(float threshold)
    {
        List<ChessEntity> allies = m_Ctx.EntityTracker.GetAllies(PLAYER_CAMP);
        for (int i = 0; i < allies.Count; i++)
        {
            var chess = allies[i];
            if (chess?.Attribute == null) continue;
            if (chess.Attribute.MaxHp > 0 &&
                chess.Attribute.CurrentHp < threshold * chess.Attribute.MaxHp)
                return true;
        }
        return false;
    }
}
