using UnityEngine;

/// <summary>
/// 生命汲取 (ID=1006)
/// 对敌方全体造成伤害，并为当前 HP 最低的友方棋子回复生命
/// </summary>
public class LifeDrainCardEffect : ICardEffect
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

        float damage = m_CardData.TableRow.BaseDamage;
        int damageType = m_CardData.TableRow.DamageType;
        float healRatio = m_CardData.GetParam("healRatio", 0.5f);

        // 对敌方全体造成伤害
        float totalDamage = 0f;
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                CardEffectHelper.DealDamage(chess, damage, damageType);
                totalDamage += damage;
            }
        }

        // 找到当前 HP 最低的友方棋子进行治疗
        ChessEntity lowestHpAlly = null;
        double lowestHp = double.MaxValue;
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player && chess.Attribute != null)
            {
                double currentHp = chess.Attribute.CurrentHp;
                if (currentHp < lowestHp)
                {
                    lowestHp = currentHp;
                    lowestHpAlly = chess;
                }
            }
        }

        if (lowestHpAlly != null)
        {
            float healAmount = totalDamage * healRatio;
            CardEffectHelper.HealTarget(lowestHpAlly, healAmount);
            DebugEx.LogModule("LifeDrainCardEffect", $"治疗 HP 最低的友方 {lowestHpAlly.Config?.Name}，回复 {healAmount}");
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
