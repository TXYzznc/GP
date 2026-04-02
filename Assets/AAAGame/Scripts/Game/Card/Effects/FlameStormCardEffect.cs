using UnityEngine;

/// <summary>
/// 烈焰风暴 (ID=1002)
/// 对敌方全体造成魔法伤害
/// </summary>
public class FlameStormCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("FlameStormCardEffect", "初始化烈焰风暴效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("FlameStormCardEffect", "执行烈焰风暴: 对敌方全体造成魔法伤害");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("FlameStormCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("FlameStormCardEffect", "没有找到任何棋子");
            return;
        }

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float damage = m_CardData.TableRow.BaseDamage;
                CardEffectHelper.DealDamage(chess, damage, 1);

                var buffManager = chess.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.AddBuff(10302);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
