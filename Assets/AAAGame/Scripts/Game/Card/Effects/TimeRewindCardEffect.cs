using UnityEngine;

/// <summary>
/// 时间回溯 (ID=1003)
/// 恢复一个友方单位的生命值
/// </summary>
public class TimeRewindCardEffect : ICardEffect
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

        float radius = m_CardData.AreaRadius;
        ChessEntity closestAlly = null;
        float closestDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestAlly = chess;
                }
            }
        }

        if (closestAlly != null)
        {
            float healAmount = m_CardData.GetParam("healAmount", 200f);
            CardEffectHelper.HealTarget(closestAlly, healAmount);
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
