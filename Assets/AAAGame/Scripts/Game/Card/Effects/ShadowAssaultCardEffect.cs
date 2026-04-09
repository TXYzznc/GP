using UnityEngine;

/// <summary>
/// 暗影突袭 (ID=1005)
/// 对单个敌人造成高额物理伤害并眩晕
/// 伤害公式：BaseDamage + DamageCoeff × 施法者攻击力
/// </summary>
public class ShadowAssaultCardEffect : ICardEffect
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
            // TODO: 施法者攻击力需要从召唤师或指定棋子获取，暂用 0
            float casterAtk = 0f;
            float damage = m_CardData.TableRow.BaseDamage + m_CardData.TableRow.DamageCoeff * casterAtk;
            int damageType = m_CardData.TableRow.DamageType;
            CardEffectHelper.DealDamage(closestEnemy, damage, damageType);

            // HitBuffs：命中目标时施加
            foreach (int buffId in m_CardData.HitBuffIds)
            {
                CardEffectHelper.ApplyBuff(closestEnemy, buffId);
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
