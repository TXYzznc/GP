using UnityEngine;

/// <summary>
/// 时间回溯 (ID=1003)
/// 【占位实现】当前使用治疗逻辑，后续会实现真正的效果
///
/// 注：实际效果不是简单治疗，不能用通用框架替代
/// </summary>
public class TimeRewindCardEffect : ICardEffect
{
    private CardData m_CardData;

    public void Init(CardData cardData)
    {
        m_CardData = cardData;
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return;

        // 使用 ClosestAllySelector 选择有效目标
        var selector = new ClosestAllySelector();
        var targets = selector.SelectTargets(null, m_CardData, targetPosition);

        if (targets == null || targets.Count == 0)
            return;

        var closestAlly = targets[0];
        float healAmount = m_CardData.GetParam("healAmount", 200f);
        CardEffectHelper.HealTarget(closestAlly, healAmount);

        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
