using UnityEngine;

/// <summary>
/// ⚠️ [可删除] 狂暴 (ID=1008)
/// 使自身进入狂暴状态，大幅提升攻击力但降低防御
/// TargetType=1（自身），对释放位置最近的友方施加 Buff
///
/// 该脚本已被通用框架替代（ClosestAllySelector + InstantBuffApplier）
/// 请删除该文件，由 CardEffectExecutor 自动使用框架处理
/// </summary>
public class BerserkCardEffect : ICardEffect
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

        // 找最近的友方棋子（代表"自身"）
        ChessEntity closestAlly = null;
        float closestDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestAlly = chess;
                }
            }
        }

        if (closestAlly != null)
        {
            // InstantBuffs：对自身施加
            foreach (int buffId in m_CardData.InstantBuffIds)
            {
                CardEffectHelper.ApplyBuff(closestAlly, buffId);
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
