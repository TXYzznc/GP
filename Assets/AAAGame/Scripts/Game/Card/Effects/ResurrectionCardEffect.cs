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

    public bool Execute(Vector3 targetPosition)
    {
        if (m_CardData == null) return false;

        var selector = new ClosestDeadAllySelector();
        List<ChessEntity> targets = selector.SelectTargets(null, m_CardData, targetPosition);

        if (targets == null || targets.Count == 0)
        {
            DebugEx.Warning("ResurrectionCardEffect", "没有找到可复活的目标，返回手牌");
            return false;
        }

        var target = targets[0];
        float reviveHpRatio = m_CardData.GetParam("reviveHpRatio", 0.5f);
        float reviveHp = (float)(target.Attribute.MaxHp * reviveHpRatio);

        // 1. 恢复 HP（先恢复，再改状态，避免 HP>0 时 IsDead 仍为 true）
        target.Attribute.ModifyHp(reviveHp);

        // 2. 改变棋子状态
        target.ChangeState(ChessState.Idle);

        // 3. 重新注册到 CombatEntityTracker（从死亡列表移回存活列表）
        CombatEntityTracker.Instance?.ReviveChess(target);

        // 4. 重新启用碰撞器
        var colliders = target.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = true;

        // 5. 恢复动画（重置 m_IsDead，强制播放 Idle）
        target.GetComponent<ChessAnimator>()?.PlayRevive();

        // 6. 恢复 AI（解除死亡保护，重新开始行动）
        if (target.AI is ChessAIBase aiBase)
            aiBase.ForceRevive();

        // 6. 清除 ChessDeploymentTracker 的死亡标记（让该棋子下次可以重新部署）
        if (target.Camp == 0)
        {
            string instanceId = ChessDeploymentTracker.Instance?.GetInstanceIdByEntity(target);
            if (!string.IsNullOrEmpty(instanceId))
                ChessDeploymentTracker.Instance.MarkChessAlive(instanceId);
        }

        DebugEx.Log("ResurrectionCardEffect", $"复活 {target.Config?.Name}，恢复 {reviveHpRatio * 100}% HP");
        CardEffectHelper.PlayEffect(m_CardData.TableRow.EffectId, targetPosition);
        return true;
    }
}
