using UnityEngine;

/// <summary>
/// 生命汲取 (ID=1006)
/// 对敌方全体造成伤害，并为当前 HP 最低的友方棋子回复生命
/// </summary>
public class LifeDrainCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
    }

    public bool Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return false;

        float damage = m_CardData.TableRow.BaseDamage;
        int damageType = m_CardData.TableRow.DamageType;
        float healRatio = m_CardData.GetParam("healRatio", 0.5f);

        var enemySelector = new AllEnemiesSelector();
        var enemies = enemySelector.SelectTargets(null, m_CardData, targetPosition);

        if (enemies == null || enemies.Count == 0)
        {
            DebugEx.Log("LifeDrainCardEffect", "没有敌方目标，返回手牌");
            return false;
        }

        float totalDamage = 0f;
        foreach (var enemy in enemies)
        {
            CardEffectHelper.DealDamage(enemy, damage, damageType);
            totalDamage += damage;
        }

        var allySelector = new LowestHpAllySelector();
        var healTargets = allySelector.SelectTargets(null, m_CardData, targetPosition);
        if (healTargets != null && healTargets.Count > 0)
        {
            float healAmount = totalDamage * healRatio;
            CardEffectHelper.HealTarget(healTargets[0], healAmount);
            DebugEx.Log("LifeDrainCardEffect", $"治疗 HP 最低的友方 {healTargets[0].Config?.Name}，回复 {healAmount}");
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
        return true;
    }
}
