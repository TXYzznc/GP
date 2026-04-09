using UnityEngine;

/// <summary>
/// 冰霜新星 (ID=1007)
/// 冰冻范围内所有敌人
/// </summary>
public class FrostNovaCardEffect : ICardEffect
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

        float aoeRadius = m_CardData.AreaRadius;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= aoeRadius)
                {
                    // HitBuffs：命中目标时施加
                    foreach (int buffId in m_CardData.HitBuffIds)
                    {
                        CardEffectHelper.ApplyBuff(chess, buffId);
                    }
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
