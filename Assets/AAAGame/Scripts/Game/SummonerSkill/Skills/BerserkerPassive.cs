using System.Collections.Generic;

/// <summary>
/// 狂怒之心——狂战士固定被动
///
/// 效果：给战场上所有友方棋子（含召唤师）挂上 ID=4002 的条件型 Buff。
/// 条件：该棋子自身 HP &lt; Params[0]（默认 0.5）× MaxHP。
/// 激活/休眠由每个棋子自身的 BuffManager 每帧检测，被动本身不再轮询全场。
/// </summary>
public class BerserkerPassive : SummonerPassiveBase
{
    private const int BUFF_ID    = 4002;
    private const int PLAYER_CAMP = 0;

    // 记录已注册的 BuffManager，Dispose 时清理
    private readonly List<BuffManager> m_RegisteredManagers = new();

    public override void Init(SummonerSkillContext ctx, SummonerSkillTable config)
    {
        base.Init(ctx, config);
    }

    protected override void OnTick(float dt)
    {
        if (m_IsActive) return;  // 已完成注册，不再重复处理

        if (m_Ctx?.EntityTracker == null) return;

        // 首次（或战斗重置后）将条件 Buff 挂到当前所有友方
        RegisterToAllAllies();
        m_IsActive = true;
    }

    protected override void OnDispose()
    {
        // 彻底移除所有已注册目标上的 Buff（激活 + 休眠）
        for (int i = 0; i < m_RegisteredManagers.Count; i++)
        {
            var bm = m_RegisteredManagers[i];
            if (bm != null) bm.RemoveBuff(BUFF_ID);
        }
        m_RegisteredManagers.Clear();
        m_IsActive = false;
    }

    // ── 私有 ─────────────────────────────────────────────────────────
    private void RegisterToAllAllies()
    {
        List<ChessEntity> allies = m_Ctx.EntityTracker.GetAllies(PLAYER_CAMP);
        for (int i = 0; i < allies.Count; i++)
        {
            var ally = allies[i];
            if (ally == null) continue;
            if (!ally.TryGetComponent<BuffManager>(out var bm)) continue;
            RegisterToBM(bm, ally.Attribute);
        }

        // 召唤师自身
        var summonerBM = m_Ctx.SummonerBuffManager;
        if (summonerBM != null)
        {
            if (summonerBM.TryGetComponent<ChessAttribute>(out var summonerAttr))
                RegisterToBM(summonerBM, summonerAttr);
        }
    }

    private void RegisterToBM(BuffManager bm, ChessAttribute attr)
    {
        if (m_RegisteredManagers.Contains(bm)) return;

        // 条件由 BerserkerRageBuff.Init() 自己设置，这里只负责"挂上去"
        bm.AddInactiveBuff(BUFF_ID);
        m_RegisteredManagers.Add(bm);
    }
}
