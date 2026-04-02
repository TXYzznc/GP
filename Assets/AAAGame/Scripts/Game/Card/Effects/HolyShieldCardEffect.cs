using UnityEngine;

/// <summary>
/// 神圣庇护 (ID=1001)
/// 为所有友方单位提供护盾，吸收伤害
/// </summary>
public class HolyShieldCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
        DebugEx.LogModule("HolyShieldCardEffect", "初始化神圣庇护效果");
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null)
            return;

        DebugEx.LogModule("HolyShieldCardEffect", "执行神圣庇护: 为所有友方单位提供护盾");

        var battleChessManager = BattleChessManager.Instance;
        if (battleChessManager == null)
        {
            DebugEx.ErrorModule("HolyShieldCardEffect", "BattleChessManager 为空");
            return;
        }

        var allChess = battleChessManager.GetAllChessEntities();
        if (allChess == null || allChess.Count == 0)
        {
            DebugEx.WarningModule("HolyShieldCardEffect", "没有找到任何棋子");
            return;
        }

        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                var buffManager = chess.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.AddBuff(10301);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
