using UnityEngine;

/// <summary>
/// 基于状态机的近战AI
/// 行为：追击最近的敌人并进行近战攻击
/// AIType = 11（新类型，避免与旧AI冲突）
/// </summary>
public class FSMMeleeAI : ChessAIBase
{
    #region 移动状态逻辑

    /// <summary>
    /// 移动状态逻辑
    /// 转换规则：
    /// - 目标丢失 → 待机
    /// - 有待命技能 + 在技能范围内 → 待机（返回使用技能）
    /// - 无待命技能 + 在攻击范围内 → 待机（由待机重新决策进入攻击状态）
    /// </summary>
    protected override void TickMoving(float dt)
    {
        // 检查目标有效性
        if (!IsTargetValid())
        {
            DebugEx.Log("FSMMeleeAI",
                $"{m_Context.Entity.Config.Name} 目标无效，返回待机");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 如果有待命技能，检查是否在技能范围内
        if (m_ShouldUseSkillAfterAttack && m_PendingSkillIndex > 0)
        {
            if (IsSkillInRange(m_PendingSkillIndex, m_CurrentTarget))
            {
                DebugEx.Log("FSMMeleeAI",
                    $"{m_Context.Entity.Config.Name} 已到达技能范围，返回待机释放技能");
                ChangeState(ChessAIState.Idle);
                return;
            }
        }
        // 无待命技能，检查是否到达攻击范围
        else if (IsInAttackRange(m_CurrentTarget))
        {
            DebugEx.Log("FSMMeleeAI",
                $"{m_Context.Entity.Config.Name} 到达攻击范围，返回待机重新决策");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 继续移动到目标
        MoveToTarget(m_CurrentTarget);
    }

    #endregion
}
