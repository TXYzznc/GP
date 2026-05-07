using UnityEngine;

/// <summary>
/// 基于状态机的近战AI
/// 行为：追击最近的敌人并进行近战攻击
/// AIType = 11（新类型，避免与旧AI冲突）
/// </summary>
public class FSMMeleeAI : ChessAIBase
{
    #region 调试字段

    private int m_MovingFrameCount = 0;

    #endregion

    #region 状态转换钩子

    protected override void OnEnterState(ChessAIState state)
    {
        base.OnEnterState(state);

        if (state == ChessAIState.Moving)
        {
            m_MovingFrameCount = 0;
        }
    }

    protected override void OnExitState(ChessAIState state)
    {
        base.OnExitState(state);

        if (state == ChessAIState.Moving)
        {
            m_MovingFrameCount = 0;
        }
    }

    #endregion

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
        m_MovingFrameCount++;

        // 检查目标有效性
        if (!IsTargetValid())
        {
            DebugEx.Log("FSMMeleeAI",
                $"{m_Context.Entity.Config.Name} 目标无效，返回待机 (Frame {m_MovingFrameCount})");
            m_MovingFrameCount = 0;
            ChangeState(ChessAIState.Idle);
            return;
        }

        // ⭐ 在 Moving 中检查技能条件（如果满足，立即回到 Idle 进行决策）
        if (CanMakeSkillDecision() && ShouldUseSkill())
        {
            DebugEx.Log("FSMMeleeAI",
                $"{m_Context.Entity.Config.Name} 在Moving中满足技能条件，回到Idle");
            m_LastSkillDecisionTime = Time.time;
            m_SkillDecisionCooldown = SKILL_DECISION_COOLDOWN;
            m_MovingFrameCount = 0;
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 如果有待命技能，检查是否在技能范围内
        if (m_ShouldUseSkillAfterAttack && m_PendingSkillIndex > 0)
        {
            bool inSkillRange = IsSkillInRange(m_PendingSkillIndex, m_CurrentTarget);
            if (m_MovingFrameCount % 10 == 0)  // 每10帧输出一次
            {
                DebugEx.Log("FSMMeleeAI",
                    $"{m_Context.Entity.Config.Name} 检查技能范围: SkillIdx={m_PendingSkillIndex}, Target={m_CurrentTarget.Config?.Name}, InRange={inSkillRange} (Frame {m_MovingFrameCount})");
            }

            if (inSkillRange)
            {
                DebugEx.Log("FSMMeleeAI",
                    $"{m_Context.Entity.Config.Name} 已到达技能范围，返回待机释放技能");
                m_MovingFrameCount = 0;
                ChangeState(ChessAIState.Idle);
                return;
            }
        }
        // 无待命技能，检查是否到达攻击范围
        else
        {
            bool inAttackRange = IsInAttackRange(m_CurrentTarget);
            float dist = Vector3.Distance(m_Context.Entity.transform.position, m_CurrentTarget.transform.position);
            float atkRange = (float)m_Context.Entity.Attribute.AtkRange;

            if (m_MovingFrameCount % 10 == 0)  // 每10帧输出一次
            {
                DebugEx.Log("FSMMeleeAI",
                    $"{m_Context.Entity.Config.Name} 检查攻击范围: 距离={dist:F2}, 范围={atkRange:F2}, InRange={inAttackRange}, ShouldUseSkill={m_ShouldUseSkillAfterAttack}, PendingSkill={m_PendingSkillIndex} (Frame {m_MovingFrameCount})");
            }

            if (inAttackRange)
            {
                DebugEx.Log("FSMMeleeAI",
                    $"{m_Context.Entity.Config.Name} 到达攻击范围，返回待机重新决策");
                m_MovingFrameCount = 0;
                ChangeState(ChessAIState.Idle);
                return;
            }
        }

        // 继续移动到目标
        MoveToTarget(m_CurrentTarget);
    }

    #endregion
}
