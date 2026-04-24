using Newtonsoft.Json.Linq;

/// <summary>
/// 反伤之盾 Buff（ID=5011）
/// 受到伤害时，立即对攻击来源造成等比例真实伤害。
/// CustomData 格式：{"reflectDamageRatio":0.3}
/// </summary>
public class ReflectDamageBuff : BuffBase
{
    private double m_ReflectRatio;
    private ChessAttribute m_OwnerAttr;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        m_ReflectRatio = 0.3;
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("reflectDamageRatio", out var token))
                    m_ReflectRatio = token.ToObject<double>();
            }
            catch { }
        }

        m_OwnerAttr = ctx?.OwnerAttribute;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        if (m_OwnerAttr != null)
            m_OwnerAttr.OnDamageTakenWithSource += OnDamageTaken;
    }

    public override void OnExit()
    {
        if (m_OwnerAttr != null)
            m_OwnerAttr.OnDamageTakenWithSource -= OnDamageTaken;
        base.OnExit();
    }

    private void OnDamageTaken(double damage, bool isMagic, ChessAttribute attacker)
    {
        if (damage <= 0 || attacker == null) return;

        double reflectDamage = damage * m_ReflectRatio;
        // 反伤为真实伤害，来源为 null（避免递归触发）
        attacker.TakeDamage(reflectDamage, false, true,
            damageType: DamageFloatingTextManager.DamageType.反弹伤害);

        DebugEx.LogModule("ReflectDamageBuff", $"反伤 {reflectDamage:F1} → 攻击者");
    }
}
