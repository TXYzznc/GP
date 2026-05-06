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

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return;

        float damage = m_CardData.TableRow.BaseDamage;
        int damageType = m_CardData.TableRow.DamageType;
        float healRatio = m_CardData.GetParam("healRatio", 0.5f);

        // 使用 AllEnemiesSelector 选择有效敌方目标进行伤害
        var enemySelector = new AllEnemiesSelector();
        var enemies = enemySelector.SelectTargets(null, m_CardData, targetPosition);

        float totalDamage = 0f;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                CardEffectHelper.DealDamage(enemy, damage, damageType);
                totalDamage += damage;
            }
        }

        // 使用 LowestHpAllySelector 选择有效友方进行治疗
        var allySelector = new LowestHpAllySelector();
        var healTargets = allySelector.SelectTargets(null, m_CardData, targetPosition);

        if (healTargets != null && healTargets.Count > 0)
        {
            var lowestHpAlly = healTargets[0];
            float healAmount = totalDamage * healRatio;
            CardEffectHelper.HealTarget(lowestHpAlly, healAmount);
            DebugEx.Log("LifeDrainCardEffect", $"治疗 HP 最低的友方 {lowestHpAlly.Config?.Name}，回复 {healAmount}");
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
