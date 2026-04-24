using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 混乱 Buff（ID=5009）
/// 正常索敌，但每次发起攻击时有 50% 概率将目标随机替换为场上任意一个存活棋子（含友军）。
/// 通过 ChessCombatController.AttackTargetModifier 委托注入，不耦合 AI 逻辑。
/// CustomData 格式：{"SpecialState":"Confused"}
/// </summary>
public class ConfusedBuff : BuffBase
{
    private bool m_Applied;
    private string m_StateKey;
    private ChessEntity m_Entity;
    private ChessCombatController m_CombatController;

    private const float RANDOM_TARGET_CHANCE = 0.5f;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);
        m_Entity = ctx?.Owner != null ? ctx.Owner.GetComponent<ChessEntity>() : null;
        m_CombatController = m_Entity != null ? m_Entity.CombatController : null;
        m_StateKey = ParseStateKey(config);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        if (m_Applied || m_Entity == null) return;

        m_Entity.AddSpecialState(m_StateKey);
        m_CombatController?.AddAttackTargetModifier(ModifyAttackTarget);
        m_Applied = true;
    }

    public override void OnExit()
    {
        if (m_Applied)
        {
            m_Entity?.RemoveSpecialState(m_StateKey);
            m_CombatController?.RemoveAttackTargetModifier(ModifyAttackTarget);
            m_Applied = false;
        }
        base.OnExit();
    }

    // ── 攻击目标修改器 ────────────────────────────────────────────────
    private ChessEntity ModifyAttackTarget(ChessEntity originalTarget)
    {
        if (Random.value > RANDOM_TARGET_CHANCE)
            return originalTarget;

        if (CombatEntityTracker.Instance == null || m_Entity == null)
            return originalTarget;

        var all = CombatEntityTracker.Instance.GetAllAliveChess();
        if (all == null || all.Count == 0)
            return originalTarget;

        // 随机选一个存活的棋子（排除自己）
        int startIdx = Random.Range(0, all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            var candidate = all[(startIdx + i) % all.Count];
            if (candidate != m_Entity && !candidate.Attribute.IsDead)
            {
                DebugEx.LogModule("ConfusedBuff", $"{m_Entity.Config.Name} 混乱：攻击随机目标 {candidate.Config.Name}");
                return candidate;
            }
        }

        return originalTarget;
    }

    // ── 私有 ──────────────────────────────────────────────────────────
    private static string ParseStateKey(BuffTable config)
    {
        if (string.IsNullOrEmpty(config?.CustomData) || config.CustomData == "{}") return "Confused";
        try
        {
            var token = JObject.Parse(config.CustomData)["SpecialState"];
            string value = token?.ToString();
            if (!string.IsNullOrEmpty(value)) return value;
        }
        catch { }
        return "Confused";
    }
}
