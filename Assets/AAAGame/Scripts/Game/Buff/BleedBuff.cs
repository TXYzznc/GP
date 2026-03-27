using UnityEngine;

public class BleedBuff : BuffBase
{
    protected override void OnTick()
    {
        if (Ctx?.OwnerAttribute == null)
        {
            return;
        }

        double damage = Ctx.OwnerAttribute.MaxHp * Config.EffectValue;
        if (damage <= 0)
        {
            return;
        }

        Ctx.OwnerAttribute.TakeDamage(damage, false, true, false, CombatVFXManager.DamageType.BleedDamage);
    }
}
