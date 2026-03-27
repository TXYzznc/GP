using Newtonsoft.Json.Linq;

public class StunBuff : BuffBase
{
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
        if (string.IsNullOrEmpty(config?.CustomData) || config.CustomData == "{}") return "Stun";
        try
        {
            var token = JObject.Parse(config.CustomData)["SpecialState"];
            string value = token?.ToString();
            if (!string.IsNullOrEmpty(value)) return value;
        }
        catch { }
        return "Stun";
    }
}
