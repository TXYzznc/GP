using UnityEngine;

/// <summary>
/// 暗影突袭 (ID=1005)
/// 对单个敌人造成高额物理伤害并眩晕
/// </summary>
public class ShadowAssaultCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("ShadowAssaultCardEffect", "初始化暗影突袭效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("ShadowAssaultCardEffect", "执行暗影突袭: 对单个敌人造成高额物理伤害并眩晕");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("ShadowAssaultCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("ShadowAssaultCardEffect", "没有找到任何棋子");
            return;
        }

        ChessEntity closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = chess;
                }
            }
        }

        if (closestEnemy != null)
        {
            float damage = m_CardData.TableRow.BaseDamage + m_CardData.TableRow.DamageCoeff * 100f;
            CardEffectHelper.DealDamage(closestEnemy, damage, 0);

            var buffManager = closestEnemy.GetComponent<BuffManager>();
            if (buffManager != null)
            {
                buffManager.AddBuff(10305);
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
