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
        DebugEx.LogModule("WarCryCardEffect", "初始化战争号角效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("WarCryCardEffect", "执行战争号角: 提升全体友方攻击力和移动速度");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("WarCryCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("WarCryCardEffect", "没有找到任何棋子");
            return;
        }

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                var buffManager = chess.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.AddBuff(10303);
                    buffManager.AddBuff(10304);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
