using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 流血 Buff
/// 每秒损失最大生命值的一定百分比
/// CustomData 格式：{"DamageRatio":0.1}
/// </summary>
public class BleedBuff : BuffBase
{
    private double m_DamageRatio;

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 从 CustomData 读取伤害比例
        m_DamageRatio = 0.1; // 默认 10%
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("DamageRatio", out var token))
                {
                    m_DamageRatio = token.ToObject<double>();
                }
            }
            catch { }
        }
    }

    protected override void OnTick()
    {
        if (Ctx?.OwnerAttribute == null) return;

        double damage = Ctx.OwnerAttribute.MaxHp * m_DamageRatio;
        if (damage <= 0) return;

        Ctx.OwnerAttribute.TakeDamage(damage, false, true, false, CombatVFXManager.DamageType.BleedDamage);
    }
}
