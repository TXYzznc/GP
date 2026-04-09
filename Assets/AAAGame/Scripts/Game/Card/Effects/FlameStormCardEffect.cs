using UnityEngine;

/// <summary>
/// 烈焰风暴 (ID=1002)
/// 对敌方全体造成魔法伤害，命中时附加灼烧
/// </summary>
public class FlameStormCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return;

        var allChess = BattleChessManager.Instance?.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0) return;

        float damage = m_CardData.TableRow.BaseDamage;
        int damageType = m_CardData.TableRow.DamageType;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                CardEffectHelper.DealDamage(chess, damage, damageType);

                // HitBuffs：命中目标时施加
                foreach (int buffId in m_CardData.HitBuffIds)
                {
                    CardEffectHelper.ApplyBuff(chess, buffId);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
