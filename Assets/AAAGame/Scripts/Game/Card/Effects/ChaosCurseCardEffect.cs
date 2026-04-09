using UnityEngine;

/// <summary>
/// 混乱诅咒 (ID=1011)
/// 使敌方全体陷入混乱，有概率攻击队友
/// </summary>
public class ChaosCurseCardEffect : ICardEffect
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

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                // HitBuffs：对敌方全体施加
                foreach (int buffId in m_CardData.HitBuffIds)
                {
                    CardEffectHelper.ApplyBuff(chess, buffId);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
