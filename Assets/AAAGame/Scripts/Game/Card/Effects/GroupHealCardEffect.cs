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
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return;

        var allChess = BattleChessManager.Instance?.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0) return;

        // 从 ParamsConfig 读取治疗量
        float healAmount = m_CardData.GetParam("healAmount", 150f);

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
