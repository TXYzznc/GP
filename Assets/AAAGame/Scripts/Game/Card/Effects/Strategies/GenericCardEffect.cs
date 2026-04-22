using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用卡牌效果处理器
/// 通过组合策略模式，支持灵活的目标选择和效果应用
/// </summary>
public class GenericCardEffect : ICardEffect
{
    private CardData m_CardData;
    private ICardTargetSelector m_TargetSelector;
    private List<ICardEffectApplier> m_EffectAppliers;

    /// <summary>
    /// 初始化通用效果
    /// </summary>
    public void Init(CardData cardData, ICardTargetSelector selector, params ICardEffectApplier[] appliers)
    {
        m_CardData = cardData;
        m_TargetSelector = selector;
        m_EffectAppliers = new List<ICardEffectApplier>(appliers);
    }

    /// <summary>
    /// 实现 ICardEffect.Init 接口
    /// </summary>
    void ICardEffect.Init(CardData cardData)
    {
        // 这个接口主要用于反射创建，GenericCardEffect 应该用 Init(cardData, selector, appliers) 初始化
        // 此处不做处理
    }

    public void Execute(Vector3 targetPosition)
    {
        if (m_CardData == null || m_TargetSelector == null || m_EffectAppliers.Count == 0)
        {
            DebugEx.WarningModule("GenericCardEffect", "未正确初始化（缺少选择器或应用器）");
            return;
        }

        // 1. 选择目标（allChess 参数不再使用，已由 TargetSelectors 改为使用 CombatEntityTracker）
        var targets = m_TargetSelector.SelectTargets(null, m_CardData, targetPosition);
        if (targets == null || targets.Count == 0)
        {
            DebugEx.LogModule("GenericCardEffect", $"卡牌 {m_CardData.CardId} 未找到目标");
            CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
            return;
        }

        // 2. 应用所有效果
        foreach (var applier in m_EffectAppliers)
        {
            applier.ApplyEffect(targets, m_CardData);
        }

        // 3. 播放特效
        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
    }
}
