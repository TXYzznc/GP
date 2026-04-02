using UnityEngine;

/// <summary>
/// 时间回溯 (ID=1003)
/// 使一个友方单位恢复到3秒前的状态
/// </summary>
public class TimeRewindCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("TimeRewindCardEffect", "初始化时间回溯效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("TimeRewindCardEffect", "执行时间回溯: 恢复友方单位状态");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("TimeRewindCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("TimeRewindCardEffect", "没有找到任何棋子");
            return;
        }

        ChessEntity closestChess = null;
        float closestDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestChess = chess;
                }
            }
        }

        if (closestChess != null)
        {
            CardEffectHelper.HealTarget(closestChess, 200f);
            DebugEx.LogModule("TimeRewindCardEffect", $"对 {closestChess.Config?.Name} 执行时间回溯");
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
