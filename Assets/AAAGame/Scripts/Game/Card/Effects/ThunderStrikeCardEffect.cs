using UnityEngine;

/// <summary>
/// 雷霆一击 (ID=1010)
/// 召唤闪电对单个敌人造成真实伤害
/// </summary>
public class ThunderStrikeCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("ThunderStrikeCardEffect", "初始化雷霆一击效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("ThunderStrikeCardEffect", "执行雷霆一击: 对单个敌人造成真实伤害");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("ThunderStrikeCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("ThunderStrikeCardEffect", "没有找到任何棋子");
            return;
        }

        float radius = m_CardData.TableRow.AreaRadius;
        ChessEntity closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = chess;
                }
            }
        }

        if (closestEnemy != null)
        {
            float damage = m_CardData.TableRow.BaseDamage;
            CardEffectHelper.DealDamage(closestEnemy, damage, 2);
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
