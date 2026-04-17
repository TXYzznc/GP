using UnityEngine;

/// <summary>
/// ⚠️ [可删除] 雷霆一击 (ID=1010)
/// 召唤闪电对单个敌人造成真实伤害
///
/// 该脚本已被通用框架替代（ClosestEnemySelector + DamageApplier）
/// 请删除该文件，由 CardEffectExecutor 自动使用框架处理
/// </summary>
public class ThunderStrikeCardEffect : ICardEffect
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

        float radius = m_CardData.AreaRadius;
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
            int damageType = m_CardData.TableRow.DamageType;
            CardEffectHelper.DealDamage(closestEnemy, damage, damageType);
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
