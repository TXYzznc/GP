using UnityEngine;

/// <summary>
/// 群体治疗 (ID=1009)
/// 恢复所有友方单位的生命值
/// </summary>
public class GroupHealCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("GroupHealCardEffect", "初始化群体治疗效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("GroupHealCardEffect", "执行群体治疗: 恢复所有友方单位");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("GroupHealCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("GroupHealCardEffect", "没有找到任何棋子");
            return;
        }

        float healAmount = m_CardData.TableRow.BaseDamage;
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                CardEffectHelper.HealTarget(chess, healAmount);
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
