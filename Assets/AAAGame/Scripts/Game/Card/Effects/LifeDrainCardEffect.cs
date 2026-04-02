using UnityEngine;

/// <summary>
/// 生命汲取 (ID=1006)
/// 对敌方全体造成伤害并为自身回复生命
/// </summary>
public class LifeDrainCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("LifeDrainCardEffect", "初始化生命汲取效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("LifeDrainCardEffect", "执行生命汲取: 对敌方全体造成伤害并回复生命");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("LifeDrainCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("LifeDrainCardEffect", "没有找到任何棋子");
            return;
        }

        float totalDamage = 0f;
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float damage = m_CardData.TableRow.BaseDamage;
                CardEffectHelper.DealDamage(chess, damage, 1);
                totalDamage += damage;
            }
        }

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                float healAmount = totalDamage * 0.5f;
                CardEffectHelper.HealTarget(chess, healAmount);
                break;
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
