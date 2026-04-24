using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 嘲讽 Buff（嘲讽之声）
/// 强制目标索敌 Buff 来源
/// CustomData 格式：{"SpecialState":"Taunt"}
/// </summary>
public class TauntBuff : BuffBase
{
    public const int BUFF_ID = 5020;

    private bool m_Applied;
    private string m_StateKey;
    private ChessEntity m_Entity;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);
        m_Entity = ctx?.Owner != null ? ctx.Owner.GetComponent<ChessEntity>() : null;
        m_StateKey = ParseStateKey(config);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        if (m_Applied || m_Entity == null) return;

        m_Entity.AddSpecialState(m_StateKey);
        m_Applied = true;
    }

    public override void OnExit()
    {
        if (m_Applied)
        {
            if (m_Entity != null) m_Entity.RemoveSpecialState(m_StateKey);
            m_Applied = false;
        }
        base.OnExit();
    }

    private static string ParseStateKey(BuffTable config)
    {
        if (string.IsNullOrEmpty(config?.CustomData) || config.CustomData == "{}") return "Taunt";
        try
        {
            var token = JObject.Parse(config.CustomData)["SpecialState"];
            string value = token?.ToString();
            if (!string.IsNullOrEmpty(value)) return value;
        }
        catch { }
        return "Taunt";
    }

    /// <summary>
    /// 获取施加嘲讽的源（用于强制索敌）
    /// </summary>
    public GameObject GetTauntSource() => Ctx?.Caster;
}
