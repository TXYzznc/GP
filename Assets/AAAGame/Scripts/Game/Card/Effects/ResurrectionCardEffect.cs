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
        DebugEx.LogModule("ResurrectionCardEffect", "初始化不屈意志效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("ResurrectionCardEffect", "执行不屈意志: 复活阵亡的友方单位");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("ResurrectionCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("ResurrectionCardEffect", "没有找到任何棋子");
            return;
        }

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player && chess.CurrentState == ChessState.Dead)
            {
                float reviveHp = (float)(chess.Attribute.MaxHp * 0.5f);
                CardEffectHelper.HealTarget(chess, reviveHp);
                chess.ChangeState(ChessState.Idle);
                DebugEx.LogModule("ResurrectionCardEffect", $"复活 {chess.Config?.Name}");
                break;
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
