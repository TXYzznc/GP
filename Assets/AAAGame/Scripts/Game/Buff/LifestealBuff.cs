using Newtonsoft.Json.Linq;

/// <summary>
/// 吸血之刃 Buff（ID=5015）
/// 造成伤害时，恢复等比例的生命值。
/// CustomData 格式：{"lifestealRatio":0.4}
/// </summary>
public class LifestealBuff : BuffBase
{
    private double m_LifestealRatio;
    private ChessAttribute m_OwnerAttr;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        m_LifestealRatio = 0.4;
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("lifestealRatio", out var token))
                    m_LifestealRatio = token.ToObject<double>();
            }
            catch { }
        }

        m_OwnerAttr = ctx?.OwnerAttribute;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        if (m_OwnerAttr != null)
            m_OwnerAttr.OnDamageDealt += OnDamageDealt;
    }

    public override void OnExit()
    {
        if (m_OwnerAttr != null)
            m_OwnerAttr.OnDamageDealt -= OnDamageDealt;
        base.OnExit();
    }

    private void OnDamageDealt(double damage, ChessAttribute target)
    {
        if (damage <= 0 || m_OwnerAttr == null) return;

        double heal = damage * m_LifestealRatio;
        m_OwnerAttr.ModifyHp(heal);

        DebugEx.LogModule("LifestealBuff", $"吸血恢复 {heal:F1} HP");
    }
}
