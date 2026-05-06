using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 不屈意志 (ID=1012)
/// 复活一个阵亡的友方单位
///
/// 使用 ClosestDeadAllySelector 获取目标
/// </summary>
public class ResurrectionCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return;

        // 使用 Selector 获取目标
        var selector = new ClosestDeadAllySelector();
        List<ChessEntity> targets = selector.SelectTargets(null, m_CardData, targetPosition);

        if (targets == null || targets.Count == 0)
        {
            DebugEx.Warning("ResurrectionCardEffect", "没有找到可复活的目标");
            return;
        }

        var target = targets[0];
        float reviveHpRatio = m_CardData.GetParam("reviveHpRatio", 0.5f);
        float reviveHp = (float)(target.Attribute.MaxHp * reviveHpRatio);
        CardEffectHelper.HealTarget(target, reviveHp);
        target.ChangeState(ChessState.Idle);
        DebugEx.Log("ResurrectionCardEffect", $"复活 {target.Config?.Name}，恢复 {reviveHpRatio * 100}% HP");

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
