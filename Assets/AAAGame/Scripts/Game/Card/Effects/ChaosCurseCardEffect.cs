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
        DebugEx.LogModule("ChaosCurseCardEffect", "初始化混乱诅咒效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("ChaosCurseCardEffect", "执行混乱诅咒: 使敌方全体陷入混乱");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("ChaosCurseCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("ChaosCurseCardEffect", "没有找到任何棋子");
            return;
        }

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                var buffManager = chess.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.AddBuff(10310);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
