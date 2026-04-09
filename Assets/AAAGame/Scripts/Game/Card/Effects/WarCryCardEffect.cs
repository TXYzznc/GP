using UnityEngine;

/// <summary>
/// 战争号角 (ID=1004)
/// 提升全体友方攻击力和移动速度
/// </summary>
public class WarCryCardEffect : ICardEffect
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

        // InstantBuffs：对全体友方施加
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                foreach (int buffId in m_CardData.InstantBuffIds)
                {
                    CardEffectHelper.ApplyBuff(chess, buffId);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
