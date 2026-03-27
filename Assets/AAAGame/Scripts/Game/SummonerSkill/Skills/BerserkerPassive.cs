using System.Collections.Generic;

/// <summary>
/// 狂怒之心——狂战士固定被动
///
/// 条件 A：任意友方棋子 HP &lt; Params[0] × 最大 HP
///   → 全体友方获得 BuffIds[0]（伤害 +15%）
/// 条件 B：条件 A 满足 且 召唤师自身 HP &lt; Params[1] × 最大 HP
///   → 切换为 BuffIds[1]（伤害 +30%），召唤师自身也获得 BuffIds[0]
/// 条件解除时移除对应 Buff，重置标记
/// </summary>
public class BerserkerPassive : SummonerPassiveBase
{
    /// <summary>条件 A 是否已激活（友方低血 Buff 已施加）</summary>
    private bool m_IsAlliesActive;

    /// <summary>条件 B 是否已激活（翻倍 Buff 已施加）</summary>
    private bool m_IsSelfActive;

    private const int PLAYER_CAMP = 0;

    protected override void OnTick(float dt)
    {
        if (m_Ctx?.EntityTracker == null || m_Ctx.RuntimeData == null)
            return;

        float[] p = m_Config.Params;
        if (p == null || p.Length < 2)
            return;

        int[] buffIds = m_Config.BuffIds;
        if (buffIds == null || buffIds.Length < 2)
            return;

        // 检测条件
        bool anyAllyLow = CheckAnyAllyLowHP(p[0]);
        bool selfLow = m_Ctx.RuntimeData.HPPercent < p[1];

        // ── 条件 A 变化处理 ──
        if (anyAllyLow && !m_IsAlliesActive)
        {
            // 首次满足条件 A：施加基础 Buff
            ApplyBuffToAllAllies(buffIds[0]);
            m_IsAlliesActive = true;
            m_IsSelfActive = false;
        }
        else if (!anyAllyLow && m_IsAlliesActive)
        {
            // 条件 A 解除：移除所有 BerserkerRage Buff
            RemoveBuffFromAllAllies(m_IsSelfActive ? buffIds[1] : buffIds[0]);
            m_IsAlliesActive = false;
            m_IsSelfActive = false;
            return;
        }

        if (!m_IsAlliesActive)
            return;

        // ── 条件 B 变化处理（条件 A 已满足的前提下）──
        if (selfLow && !m_IsSelfActive)
        {
            // 首次满足条件 B：替换为翻倍 Buff，召唤师自身也获得基础 Buff
            RemoveBuffFromAllAllies(buffIds[0]);
            ApplyBuffToAllAllies(buffIds[1]);
            m_IsSelfActive = true;
        }
        else if (!selfLow && m_IsSelfActive)
        {
            // 条件 B 解除：回退到基础 Buff
            RemoveBuffFromAllAllies(buffIds[1]);
            ApplyBuffToAllAllies(buffIds[0]);
            m_IsSelfActive = false;
        }
    }

    protected override void OnDispose()
    {
        if (m_Config?.BuffIds == null || m_Config.BuffIds.Length < 2)
            return;

        if (m_IsAlliesActive)
        {
            int activeBuffId = m_IsSelfActive ? m_Config.BuffIds[1] : m_Config.BuffIds[0];
            RemoveBuffFromAllAllies(activeBuffId);
        }

        m_IsAlliesActive = false;
        m_IsSelfActive = false;
    }

    // ── 辅助方法 ──

    private bool CheckAnyAllyLowHP(float threshold)
    {
        List<ChessEntity> allies = m_Ctx.EntityTracker.GetAllies(PLAYER_CAMP);
        for (int i = 0; i < allies.Count; i++)
        {
            var chess = allies[i];
            if (chess?.Attribute == null)
                continue;
            if (chess.Attribute.MaxHp > 0 &&
                chess.Attribute.CurrentHp < threshold * chess.Attribute.MaxHp)
                return true;
        }
        return false;
    }

    private void ApplyBuffToAllAllies(int buffId)
    {
        List<ChessEntity> allies = m_Ctx.EntityTracker.GetAllies(PLAYER_CAMP);
        for (int i = 0; i < allies.Count; i++)
        {
            var bm = allies[i]?.GetComponent<BuffManager>();
            bm?.AddBuff(buffId);
        }
        DebugEx.Log($"[BerserkerPassive] 施加 Buff {buffId} 到全体 {allies.Count} 个友方棋子");
    }

    private void RemoveBuffFromAllAllies(int buffId)
    {
        List<ChessEntity> allies = m_Ctx.EntityTracker.GetAllies(PLAYER_CAMP);
        for (int i = 0; i < allies.Count; i++)
        {
            var bm = allies[i]?.GetComponent<BuffManager>();
            bm?.RemoveBuff(buffId);
        }
        DebugEx.Log($"[BerserkerPassive] 移除 Buff {buffId} 从全体 {allies.Count} 个友方棋子");
    }
}
