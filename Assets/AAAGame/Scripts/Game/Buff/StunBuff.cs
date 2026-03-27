using UnityEngine;
using Newtonsoft.Json.Linq;

public class StunBuff : BuffBase
{
    private bool m_Applied;
    private string m_StateKey;

    public override void OnEnter()
    {
        base.OnEnter();
        if (m_Applied)
        {
            return;
        }

        var entity = Ctx?.Owner != null ? Ctx.Owner.GetComponent<ChessEntity>() : null;
        if (entity == null)
        {
            return;
        }

        m_StateKey = GetStateKey();
        entity.AddSpecialState(m_StateKey);
        m_Applied = true;
    }

    private string GetStateKey()
    {
        if (Config == null)
        {
            return "Stun";
        }

        if (string.IsNullOrEmpty(Config.CustomData) || Config.CustomData == "{}")
        {
            return "Stun";
        }

        try
        {
            var obj = Newtonsoft.Json.Linq.JObject.Parse(Config.CustomData);
            var token = obj["SpecialState"];
            if (token != null)
            {
                string value = token.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }
        catch
        {
        }

        return "Stun";
    }

    public override void OnExit()
    {
        if (m_Applied)
        {
            var entity = Ctx?.Owner != null ? Ctx.Owner.GetComponent<ChessEntity>() : null;
            if (entity != null)
            {
                entity.RemoveSpecialState(string.IsNullOrEmpty(m_StateKey) ? "Stun" : m_StateKey);
            }

            m_Applied = false;
        }

        base.OnExit();
    }
}
