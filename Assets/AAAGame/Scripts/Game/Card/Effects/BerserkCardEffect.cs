using UnityEngine;

/// <summary>
/// 狂暴 (ID=1008)
/// 使自身进入狂暴状态，大幅提升攻击力但降低防御
/// </summary>
public class BerserkCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("BerserkCardEffect", "初始化狂暴效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("BerserkCardEffect", "执行狂暴: 进入狂暴状态");

        var allChess = BattleChessManager.Instance.GetAllChessEntities();
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                var buffManager = chess.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.AddBuff(10308);
                }
                break;
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
