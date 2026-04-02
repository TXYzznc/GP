using UnityEngine;

/// <summary>
/// 冰霜新星 (ID=1007)
/// 冰冻范围内所有敌人
/// </summary>
public class FrostNovaCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("FrostNovaCardEffect", "初始化冰霜新星效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("FrostNovaCardEffect", "执行冰霜新星: 冰冻范围内所有敌人");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("FrostNovaCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("FrostNovaCardEffect", "没有找到任何棋子");
            return;
        }

        float aoeRadius = m_CardData.TableRow.AreaRadius;
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Enemy)
            {
                float distance = Vector3.Distance(chess.transform.position, targetPosition);
                if (distance <= aoeRadius)
                {
                    var buffManager = chess.GetComponent<BuffManager>();
                    if (buffManager != null)
                    {
                        buffManager.AddBuff(10307);
                    }
                }
            }
        }

        CardRangePreview.Instance?.ShowPreview(targetPosition, aoeRadius);
        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
