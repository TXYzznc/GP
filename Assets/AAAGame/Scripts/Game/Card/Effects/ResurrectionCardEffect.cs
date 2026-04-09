using UnityEngine;

/// <summary>
/// 不屈意志 (ID=1012)
/// 复活一个阵亡的友方单位
/// </summary>
public class ResurrectionCardEffect : ICardEffect
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
        ChessEntity closestDead = null;
        float closestDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player && chess.CurrentState == ChessState.Dead)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDead = chess;
                }
            }
        }

        if (closestDead != null)
        {
            float reviveHpRatio = m_CardData.GetParam("reviveHpRatio", 0.5f);
            float reviveHp = (float)(closestDead.Attribute.MaxHp * reviveHpRatio);
            CardEffectHelper.HealTarget(closestDead, reviveHp);
            closestDead.ChangeState(ChessState.Idle);
            DebugEx.LogModule("ResurrectionCardEffect", $"复活 {closestDead.Config?.Name}，恢复 {reviveHpRatio * 100}% HP");
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
