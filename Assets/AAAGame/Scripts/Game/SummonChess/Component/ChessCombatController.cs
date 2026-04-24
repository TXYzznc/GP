using System;
using UnityEngine;

/// <summary>
/// 棋子战斗控制器
/// 协调棋子攻击、AI行为、玩家控制等优先级
/// </summary>
public class ChessCombatController : MonoBehaviour
{
    #region 私有字段

    private ChessEntity m_Entity;
    private ChessContext m_Context;

    /// <summary>是否启用战斗AI</summary>
    private bool m_IsEnabled;

    /// <summary>玩家指定的移动目标位置</summary>
    private Vector3? m_PlayerMoveTarget;

    /// <summary>是否有待执行的玩家移动指令（等待技能完成）</summary>
    private bool m_HasPendingPlayerMove;

    /// <summary>AI状态</summary>
    private CombatAIState m_AIState = CombatAIState.Idle;

    /// <summary>待处理攻击目标（用于动画事件回调）</summary>
    private ChessEntity m_PendingAttackTarget;

    /// <summary>当前使用的命中检测器（仅用于近战攻击结束回调）</summary>
    private IHitDetector m_CurrentHitDetector;

    /// <summary>攻击目标修改器链：按顺序调用，每个修改器可替换目标（null = 本次攻击miss）</summary>
    private readonly System.Collections.Generic.List<Func<ChessEntity, ChessEntity>> m_AttackTargetModifiers = new();

    public void AddAttackTargetModifier(Func<ChessEntity, ChessEntity> modifier)
    {
        if (modifier != null && !m_AttackTargetModifiers.Contains(modifier))
            m_AttackTargetModifiers.Add(modifier);
    }

    public void RemoveAttackTargetModifier(Func<ChessEntity, ChessEntity> modifier)
    {
        m_AttackTargetModifiers.Remove(modifier);
    }

    #endregion

    #region 属性

    /// <summary>是否启用战斗AI</summary>
    public bool IsEnabled => m_IsEnabled;

    /// <summary>当前AI状态</summary>
    public CombatAIState AIState => m_AIState;

    /// <summary>是否有玩家移动指令</summary>
    public bool HasPlayerMoveCommand => m_PlayerMoveTarget.HasValue;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化战斗控制器
    /// </summary>
    /// <param name="entity">棋子实体</param>
    /// <param name="context">棋子上下文</param>
    public void Initialize(ChessEntity entity, ChessContext context)
    {
        m_Entity = entity;
        m_Context = context;
        m_IsEnabled = false;
        m_PlayerMoveTarget = null;
        m_AIState = CombatAIState.Idle;

        // 订阅移动完成事件
        if (m_Entity.Movement is SimpleChessMovement movement)
        {
            movement.OnArrived += OnMovementArrived;
        }

        // ⭐ 订阅动画事件
        if (m_Entity.Animator?.EventReceiver != null)
        {
            // 普攻事件
            m_Entity.Animator.EventReceiver.OnAttackExecute += OnAttackExecute;
            m_Entity.Animator.EventReceiver.OnAnimationComplete += OnAnimationComplete;
            m_Entity.Animator.EventReceiver.OnMeleeAttackEnd += OnMeleeAttackEnd;

            // ⭐ 技能事件
            m_Entity.Animator.EventReceiver.OnSkill1Execute += OnSkill1Execute;
            m_Entity.Animator.EventReceiver.OnSkill2Execute += OnSkill2Execute;

            DebugEx.LogModule("ChessCombatController",
                $"初始化完成: {m_Entity.Config?.Name}");
        }
        else
        {
            DebugEx.ErrorModule("ChessCombatController",
                $"初始化失败: {m_Entity.Config?.Name}, Animator.EventReceiver 为 null！");
        }
    }

    #endregion

    #region 启用/禁用

    /// <summary>
    /// 启用战斗AI
    /// </summary>
    public void Enable()
    {
        if (m_IsEnabled) return;

        // 跳过召唤师AI（AIType=3）
        if (m_Entity != null && m_Entity.Config != null && m_Entity.Config.AIType == 3)
        {
            DebugEx.LogModule("ChessCombatController",
                $"跳过启用（召唤师AI）: {m_Entity.Config?.Name}");
            return;
        }

        m_IsEnabled = true;
        m_AIState = CombatAIState.Idle;
        m_PlayerMoveTarget = null;

        DebugEx.LogModule("ChessCombatController", $"启用: {m_Entity.Config?.Name}");
    }

    /// <summary>
    /// 禁用战斗AI
    /// </summary>
    public void Disable()
    {
        if (!m_IsEnabled) return;

        m_IsEnabled = false;
        m_AIState = CombatAIState.Idle;
        m_PlayerMoveTarget = null;

        // 取消当前命中检测
        m_CurrentHitDetector?.Cancel();
        m_CurrentHitDetector = null;

        // 停止移动
        m_Entity.Movement?.Stop();

        DebugEx.LogModule("ChessCombatController", $"禁用: {m_Entity.Config?.Name}");
    }

    #endregion

    #region 玩家控制

    /// <summary>
    /// 设置玩家移动指令
    /// 玩家控制优先级最高，覆盖当前AI行为
    /// </summary>
    /// <param name="targetPosition">目标位置</param>
    public void SetPlayerMoveCommand(Vector3 targetPosition)
    {
        m_PlayerMoveTarget = targetPosition;

        // ⭐ 检查当前动作类型
        if (m_Entity.Animator != null)
        {
            if (m_Entity.Animator.IsPlayingSkill)
            {
                // 正在使用技能，缓存移动指令，等待技能完成
                m_HasPendingPlayerMove = true;
                DebugEx.LogModule("ChessCombatController",
                    $"{m_Entity.Config?.Name} 正在使用技能，移动指令已缓存，等待技能完成");
                return;
            }
            else if (m_Entity.Animator.IsPlayingAttack)
            {
                // 正在普攻，强制打断
                bool interrupted = m_Entity.Animator.ForceInterruptAction();
                if (interrupted)
                {
                    DebugEx.LogModule("ChessCombatController",
                        $"{m_Entity.Config?.Name} 普攻被玩家移动打断");

                    // ⭐ 清理攻击状态（重要！）
                    m_PendingAttackTarget = null;
                    m_CurrentHitDetector?.Cancel();
                    m_CurrentHitDetector = null;

                    // ⭐ 新版状态机AI会在下一帧自动处理状态
                    // 不需要显式通知AI，状态机会自动检测目标有效性
                }
            }
        }

        // 执行移动
        ExecutePlayerMove(targetPosition);
    }

    /// <summary>
    /// 执行玩家移动
    /// </summary>
    private void ExecutePlayerMove(Vector3 targetPosition)
    {
        m_AIState = CombatAIState.PlayerMoving;
        m_HasPendingPlayerMove = false;

        // 取消当前命中检测
        m_CurrentHitDetector?.Cancel();
        m_CurrentHitDetector = null;

        // 立即开始移动
        m_Entity.Movement?.MoveTo(targetPosition);

        DebugEx.LogModule("ChessCombatController",
            $"{m_Entity.Config?.Name} 开始玩家移动 → {targetPosition}");
    }

    /// <summary>
    /// 取消玩家移动指令
    /// </summary>
    public void CancelPlayerMoveCommand()
    {
        if (!m_PlayerMoveTarget.HasValue && !m_HasPendingPlayerMove) return;

        m_PlayerMoveTarget = null;
        m_HasPendingPlayerMove = false;
        m_AIState = CombatAIState.Idle;

        // 停止移动
        m_Entity.Movement?.Stop();

        DebugEx.LogModule("ChessCombatController",
            $"{m_Entity.Config?.Name} 取消玩家移动指令");

        // ⭐ 恢复 AI 行动
        if (m_Entity.AI != null)
        {
            if (m_Entity.AI is ChessAIBase aiBase)
            {
                aiBase.ResetTargetAfterPlayerMove();
            }
            else
            {
                m_Entity.AI.ResetTargetAfterPlayerMove();
            }
        }
    }

    #endregion

    #region AI 触发攻击

    /// <summary>
    /// AI 触发攻击（由 AI.Attack() 调用）
    /// </summary>
    /// <param name="target">攻击目标</param>
    public void TriggerAttackFromAI(ChessEntity target)
    {
        if (target == null)
        {
            DebugEx.LogModule("ChessCombatController", "AI触发攻击时目标为null");
            return;
        }

        // 缓存攻击目标，依次通过修改器链（混乱/失准等），null 表示 miss
        ChessEntity resolved = target;
        for (int i = 0; i < m_AttackTargetModifiers.Count; i++)
            resolved = m_AttackTargetModifiers[i](resolved);
        m_PendingAttackTarget = resolved;

        // ❌ 移除伤害计算逻辑（移到 OnAttackExecute）
        // 伤害将在动画执行帧、应用"执行时"Buff 之后计算

        // 更新攻速并播放动画
        float atkSpeed = (float)m_Entity.Attribute.AtkSpeed;
        m_Entity.Animator?.UpdateAttackSpeed(atkSpeed);
        m_Entity.Animator?.PlayAttack();

        DebugEx.LogModule("ChessCombatController",
            $"{m_Entity.Config?.Name} AI触发攻击 → {target.Config?.Name}");
    }

    #endregion

    #region 技能触发

    /// <summary>
    /// AI 触发技能1（由 AI 调用）
    /// </summary>
    public void TriggerSkill1FromAI()
    {
        if (m_Entity.Skill1 == null)
        {
            DebugEx.WarningModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 没有技能1");
            return;
        }

        if (!m_Entity.Skill1.CanCast())
        {
            return;  // 静默失败，避免频繁日志
        }

        // 尝试释放技能
        if (m_Entity.Skill1.TryCast())
        {
            // 播放技能动画
            m_Entity.Animator?.PlaySkill1();

            DebugEx.LogModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 释放技能1");
        }
    }

    /// <summary>
    /// AI 触发技能2/大招（由 AI 调用）
    /// </summary>
    public void TriggerSkill2FromAI()
    {
        if (m_Entity.Skill2 == null)
        {
            DebugEx.WarningModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 没有大招");
            return;
        }

        if (!m_Entity.Skill2.CanCast())
        {
            return;  // 静默失败，避免频繁日志
        }

        // 尝试释放技能
        if (m_Entity.Skill2.TryCast())
        {
            // 播放技能动画
            m_Entity.Animator?.PlaySkill2();

            DebugEx.LogModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 释放大招");
        }
    }

    #endregion

    #region 每帧更新

    /// <summary>
    /// 每帧更新（由ChessEntity调用）
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public void Tick(float deltaTime)
    {
        if (!m_IsEnabled) return;
        if (m_Entity == null || m_Entity.CurrentState == ChessState.Dead) return;

        // ⭐ 只处理玩家移动状态
        switch (m_AIState)
        {
            case CombatAIState.PlayerMoving:
                UpdatePlayerMoving(deltaTime);
                break;
        }
    }

    #endregion

    #region AI状态更新

    /// <summary>
    /// 玩家移动状态：等待到达目标位置
    /// </summary>
    private void UpdatePlayerMoving(float deltaTime)
    {
        // 玩家移动状态下，不执行AI逻辑
        // 等待 OnMovementArrived 事件来恢复AI
    }

    #endregion

    #region 攻击逻辑

    /// <summary>
    /// 攻击执行回调（由动画帧事件触发）
    /// 职责：转发给普攻实现类
    /// </summary>
    private void OnAttackExecute()
    {
        if (m_Entity != null && m_Entity.IsIncapacitated)
        {
            EndAttack();
            return;
        }

        if (m_PendingAttackTarget == null)
        {
            DebugEx.WarningModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 攻击目标为空");
            EndAttack();
            return;
        }

        if (m_Entity.NormalAttack == null)
        {
            DebugEx.ErrorModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 没有普攻实现类！");
            EndAttack();
            return;
        }

        // ⭐ 转发给普攻实现类执行完整流程（包括回蓝逻辑）
        m_Entity.NormalAttack.ExecuteAttack(m_Entity, m_PendingAttackTarget);
    }

    /// <summary>
    /// 动画播放完成回调（由动画帧事件触发）
    /// 职责：通知AI攻击行为结束，这是攻击完成的唯一信号
    /// </summary>
    /// <param name="animName">动画名称</param>
    private void OnAnimationComplete(string animName)
    {
        switch (animName)
        {
            case "Attack":
                DebugEx.LogModule("ChessCombatController",
                    $"{m_Entity.Config?.Name} 攻击动画完成");
                EndAttack();
                break;

            case "Skill1":
                DebugEx.LogModule("ChessCombatController",
                    $"{m_Entity.Config?.Name} 技能1动画完成");
                EndSkill(1);

                // ⭐ 检查是否有待执行的玩家移动
                CheckPendingPlayerMove();
                break;

            case "Skill2":
                DebugEx.LogModule("ChessCombatController",
                    $"{m_Entity.Config?.Name} 大招动画完成");
                EndSkill(2);

                // ⭐ 检查是否有待执行的玩家移动
                CheckPendingPlayerMove();
                break;
        }
    }

    /// <summary>
    /// 技能1执行回调（由动画帧事件触发）
    /// 职责：转发给技能实现类
    /// </summary>
    private void OnSkill1Execute()
    {
        if (m_Entity != null && m_Entity.IsIncapacitated)
        {
            return;
        }

        if (m_Entity.Skill1 == null)
        {
            DebugEx.WarningModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 技能1不存在");
            return;
        }

        // ⭐ 转发给技能实现类执行完整流程
        m_Entity.Skill1.ExecuteSkill(m_Entity);
    }

    /// <summary>
    /// 技能2/大招执行回调（由动画帧事件触发）
    /// 职责：转发给技能实现类
    /// </summary>
    private void OnSkill2Execute()
    {
        if (m_Entity != null && m_Entity.IsIncapacitated)
        {
            return;
        }

        if (m_Entity.Skill2 == null)
        {
            DebugEx.WarningModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 大招不存在");
            return;
        }

        // ⭐ 转发给技能实现类执行完整流程
        m_Entity.Skill2.ExecuteSkill(m_Entity);
    }

    /// <summary>
    /// 近战攻击结束回调（由动画帧事件触发）
    /// 用于关闭持续碰撞
    /// </summary>
    private void OnMeleeAttackEnd()
    {
        if (m_CurrentHitDetector is MeleeHitDetector meleeDetector)
        {
            meleeDetector.EndMeleeDetection();
        }
    }

    /// <summary>
    /// 结束攻击（清理状态，通知AI）
    /// </summary>
    private void EndAttack()
    {
        m_PendingAttackTarget = null;
        m_CurrentHitDetector = null;

        // ⭐ 通知 AI 攻击完成（支持新旧AI）
        if (m_Entity.AI != null)
        {
            // 优先检查新版状态机AI
            if (m_Entity.AI is ChessAIBase aiBase)
            {
                aiBase.OnAttackComplete();
            }
            else
            {
                // 其他AI类型，尝试调用接口方法
                m_Entity.AI.OnAttackComplete();
            }
        }

        DebugEx.LogModule("ChessCombatController",
            $"{m_Entity.Config?.Name} 攻击流程结束，AI 可以继续行动");
    }

    private void EndSkill(int skillIndex)
    {
        DebugEx.LogModule("ChessCombatController",
            $"{m_Entity.Config?.Name} 技能{skillIndex}流程结束");

        // ⭐ 通知 AI 技能完成（支持新版状态机AI）
        if (m_Entity.AI != null && m_Entity.AI is ChessAIBase aiBase)
        {
            aiBase.OnSkillComplete();
        }
    }

    /// <summary>
    /// 检查并执行待处理的玩家移动指令
    /// </summary>
    private void CheckPendingPlayerMove()
    {
        if (m_HasPendingPlayerMove && m_PlayerMoveTarget.HasValue)
        {
            DebugEx.LogModule("ChessCombatController",
                $"{m_Entity.Config?.Name} 技能完成，执行缓存的玩家移动指令");

            ExecutePlayerMove(m_PlayerMoveTarget.Value);
        }
    }

    /// <summary>
    /// 移动完成事件处理
    /// </summary>
    private void OnMovementArrived()
    {
        // 如果是玩家移动指令完成，恢复AI行为
        if (m_AIState == CombatAIState.PlayerMoving)
        {
            m_PlayerMoveTarget = null;
            m_AIState = CombatAIState.Idle;

            DebugEx.LogModule("ChessCombatController", 
                $"玩家移动完成，恢复AI: {m_Entity.Config?.Name}, " +
                $"PlayerMoveTarget={m_PlayerMoveTarget}, AIState={m_AIState}, " +
                $"HasPlayerMoveCommand={HasPlayerMoveCommand}");
            
            // 通知AI重置目标状态，立即重新搜索目标
            if (m_Entity.AI != null)
            {
                // 强制类型转换以确保编译器识别新方法
                var ai = m_Entity.AI as ChessAIBase;
                if (ai != null)
                {
                    ai.ResetTargetAfterPlayerMove();
                }
                else
                {
                    // 如果不是ChessAIBase类型，调用接口方法
                    m_Entity.AI.ResetTargetAfterPlayerMove();
                }
            }
        }
    }

    #endregion

    #region 清理

    private void OnDestroy()
    {
        // 取消移动事件订阅
        if (m_Entity?.Movement is SimpleChessMovement movement)
        {
            movement.OnArrived -= OnMovementArrived;
        }

        // ⭐ 取消动画事件订阅
        if (m_Entity?.Animator?.EventReceiver != null)
        {
            m_Entity.Animator.EventReceiver.OnAttackExecute -= OnAttackExecute;
            m_Entity.Animator.EventReceiver.OnAnimationComplete -= OnAnimationComplete;
            m_Entity.Animator.EventReceiver.OnMeleeAttackEnd -= OnMeleeAttackEnd;
            m_Entity.Animator.EventReceiver.OnSkill1Execute -= OnSkill1Execute;
            m_Entity.Animator.EventReceiver.OnSkill2Execute -= OnSkill2Execute;
        }

        // 取消当前命中检测
        m_CurrentHitDetector?.Cancel();
    }

    #endregion
}

/// <summary>
/// 战斗AI状态
/// </summary>
public enum CombatAIState
{
    /// <summary>空闲（寻找目标）</summary>
    Idle,

    /// <summary>玩家移动中（优先级最高）</summary>
    PlayerMoving,

    /// <summary>追击目标</summary>
    Chasing,

    /// <summary>攻击中</summary>
    Attacking
}
