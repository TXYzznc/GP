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
    /// - 到达目标位置（进入攻击范围） → 待机（由待机重新决策进入攻击状态）
    /// </summary>
    protected override void TickMoving(float dt)
    {
        // 检查目标有效性
        if (!IsTargetValid())
        {
            DebugEx.LogModule("FSMMeleeAI", 
                $"{m_Context.Entity.Config.Name} 目标无效，返回待机");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 检查是否到达目标位置（进入攻击范围）
        if (IsInAttackRange(m_CurrentTarget))
        {
            DebugEx.LogModule("FSMMeleeAI", 
                $"{m_Context.Entity.Config.Name} 到达目标位置，返回待机重新决策");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 继续移动到目标
        MoveToTarget(m_CurrentTarget);
    }

    #endregion
}
