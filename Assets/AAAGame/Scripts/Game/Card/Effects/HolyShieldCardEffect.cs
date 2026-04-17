using UnityEngine;

/// <summary>
/// ⚠️ [可删除] 神圣庇护 (ID=1001)
/// 为所有友方单位提供护盾，吸收伤害
///
/// 该脚本已被通用框架替代（AllAlliesSelector + InstantBuffApplier）
/// 请删除该文件，由 CardEffectExecutor 自动使用框架处理
/// </summary>
public class HolyShieldCardEffect : ICardEffect
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

        // InstantBuffs：对全体友方施加
        foreach (var chess in allChess)
        {
            if (chess != null && chess.Camp == (int)CampType.Player)
            {
                foreach (int buffId in m_CardData.InstantBuffIds)
                {
                    CardEffectHelper.ApplyBuff(chess, buffId);
                }
            }
        }

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
