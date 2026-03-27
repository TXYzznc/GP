﻿using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子AI状态机基类
/// 提供状态管理、状态转换、通用能力
/// 子类继承后实现具体状态逻辑
/// </summary>
public abstract class ChessAIBase : IChessAI
{
    #region 状态管理

    /// <summary>当前状态</summary>
    protected ChessAIState m_CurrentState = ChessAIState.Summoning;

    /// <summary>上一个状态（用于调试）</summary>
    protected ChessAIState m_PreviousState = ChessAIState.Summoning;

    #endregion

    #region 核心字段

    /// <summary>棋子上下文</summary>
    protected ChessContext m_Context;

    /// <summary>当前攻击目标</summary>
    protected ChessEntity m_CurrentTarget;

    /// <summary>攻击冷却计时器</summary>
    protected float m_AttackCooldownTimer;

    /// <summary>目标搜索间隔计时器</summary>
    protected float m_TargetSearchTimer;

    /// <summary>是否正在执行攻击动作</summary>
    protected bool m_IsAttacking;

    /// <summary>召唤动画持续时间</summary>
    protected float m_SummoningDuration = 0.5f;

    /// <summary>召唤计时器</summary>
    protected float m_SummoningTimer;

    /// <summary>攻击结束后是否应该使用技能</summary>
    protected bool m_ShouldUseSkillAfterAttack;

    /// <summary>是否正在执行技能动作</summary>
    protected bool m_IsUsingSkill;

    /// <summary>技能决策防抖计时器</summary>
    protected float m_SkillDecisionCooldown;

    /// <summary>上次技能决策的时间戳</summary>
    protected float m_LastSkillDecisionTime;

    /// <summary>技能释放策略</summary>
    protected ISkillReleaseStrategy m_SkillStrategy;

    /// <summary>当前要使用的技能类型（1=技能1, 2=大招）</summary>
    protected int m_PendingSkillIndex;

    /// <summary>索敌配置</summary>
    protected TargetSearchConfig m_SearchConfig;

    /// <summary>索敌策略</summary>
    protected ITargetSearchStrategy m_SearchStrategy;

    #endregion

    #region 配置参数

    /// <summary>目标搜索间隔（秒）</summary>
    protected const float TARGET_SEARCH_INTERVAL = 0.5f;

    /// <summary>攻击范围缓冲区比例（防止抖动）</summary>
    protected const float ATTACK_RANGE_BUFFER = 1.2f;

    /// <summary>技能决策防抖间隔（秒）</summary>
    protected const float SKILL_DECISION_COOLDOWN = 0.2f;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化AI
    /// </summary>
    public virtual void Init(ChessContext ctx)
    {
        m_Context = ctx;
        m_CurrentTarget = null;
        m_AttackCooldownTimer = 0f;
        m_TargetSearchTimer = 0f;
        m_IsAttacking = false;
        m_IsUsingSkill = false;
        m_ShouldUseSkillAfterAttack = false;
        m_SummoningTimer = m_SummoningDuration;
        m_PendingSkillIndex = 0;
        m_SkillDecisionCooldown = 0f;
        m_LastSkillDecisionTime = 0f;

        // 初始状态为召唤
        m_CurrentState = ChessAIState.Summoning;
        m_PreviousState = ChessAIState.Summoning;

        // 初始化技能释放策略
        InitSkillStrategy();

        DebugEx.LogModule(
            GetType().Name,
            $"AI初始化完成 - {ctx.Entity.Config.Name}，初始状态: {m_CurrentState}"
        );

        // 初始化索敌配置（可从配置表读取，这里使用默认配置）
        InitSearchConfig();

        // 初始化索敌策略
        m_SearchStrategy = new DefaultTargetSearchStrategy(m_SearchConfig);

        DebugEx.Log(GetType().Name, $"索敌系统初始化完成 - {m_SearchConfig}");
    }

    /// <summary>
    /// 初始化技能释放策略（子类可重写）
    /// </summary>
    protected virtual void InitSkillStrategy()
    {
        // 从工厂创建策略
        m_SkillStrategy = ChessFactory.CreateSkillStrategy(m_Context.Entity.Config.Id, m_Context);
    }

    /// <summary>
    /// 初始化索敌配置（子类可重写以自定义配置）
    /// </summary>
    protected virtual void InitSearchConfig()
    {
        // 默认配置：无索敌距离限制，优先攻击残血
        m_SearchConfig = TargetSearchConfig.CreateDefault();

        // 可以根据 AI 类型选择不同的配置
        // 例如：近战AI使用近战配置，远程AI使用远程配置
    }

    #endregion

    #region 每帧更新

    /// <summary>
    /// 每帧更新AI逻辑（模板方法）
    /// </summary>
    public void Tick(float dt)
    {
        // 死亡状态不执行任何逻辑
        if (m_CurrentState == ChessAIState.Dead)
        {
            return;
        }

        // 前置检查
        if (!CanExecuteAI())
        {
            return;
        }

        // 更新计时器
        UpdateTimers(dt);

        // 根据当前状态执行对应逻辑
        switch (m_CurrentState)
        {
            case ChessAIState.Summoning:
                TickSummoning(dt);
                break;
            case ChessAIState.Idle:
                TickIdle(dt);
                break;
            case ChessAIState.Moving:
                TickMoving(dt);
                break;
            case ChessAIState.Attacking:
                TickAttacking(dt);
                break;
            case ChessAIState.UsingSkill:
                TickUsingSkill(dt);
                break;
        }
    }

    /// <summary>
    /// 检查是否可以执行AI
    /// </summary>
    protected virtual bool CanExecuteAI()
    {
        // 检查 CombatController 是否启用（战斗阶段才能执行AI）
        if (
            m_Context.Entity.CombatController == null
            || !m_Context.Entity.CombatController.IsEnabled
        )
        {
            // DebugEx.LogModule(
            //     "ChessAIBase",
            //     $"CanExecuteAI=false: {m_Context.Entity.Config.Name} - CombatController未启用"
            // );
            return false;
        }

        // 如果 CombatController 有玩家移动指令，AI 暂停工作
        if (m_Context.Entity.CombatController.HasPlayerMoveCommand)
        {
            DebugEx.LogModule(
                "ChessAIBase",
                $"CanExecuteAI=false: {m_Context.Entity.Config.Name} - 有玩家移动指令"
            );
            return false;
        }

        // ⭐ 添加成功执行的日志（仅在状态变化时输出，避免刷屏）
        if (m_CurrentState == ChessAIState.Attacking)
        {
        }

        return true;
    }

    /// <summary>
    /// 更新计时器
    /// </summary>
    protected virtual void UpdateTimers(float dt)
    {
        // 更新攻击冷却
        if (m_AttackCooldownTimer > 0)
        {
            m_AttackCooldownTimer -= dt;
        }

        // 更新目标搜索计时器
        m_TargetSearchTimer -= dt;

        // 更新技能决策防抖计时器
        if (m_SkillDecisionCooldown > 0)
        {
            m_SkillDecisionCooldown -= dt;
        }
    }

    #endregion

    #region 状态执行方法

    /// <summary>
    /// 召唤状态逻辑
    /// 转换规则：召唤完成 → 待机
    /// </summary>
    protected virtual void TickSummoning(float dt)
    {
        m_SummoningTimer -= dt;

        if (m_SummoningTimer <= 0)
        {
            DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 召唤完成");
            ChangeState(ChessAIState.Idle);
        }
    }

    /// <summary>
    /// 待机状态逻辑 - 核心决策状态
    /// 转换规则：
    /// - 有目标 + 应该使用技能 → 使用技能
    /// - 有目标 + 在攻击范围内 → 普攻
    /// - 有目标 + 不在攻击范围 → 移动
    /// - 无目标 → 保持待机
    /// </summary>
    protected virtual void TickIdle(float dt)
    {
        // 定期搜索目标
        if (m_TargetSearchTimer <= 0)
        {
            DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 开始搜索目标...");

            m_CurrentTarget = FindTarget();
            m_TargetSearchTimer = TARGET_SEARCH_INTERVAL;

            if (m_CurrentTarget != null)
            {
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 找到目标: {m_CurrentTarget.Config.Name}"
                );
            }
            else
            {
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 未找到目标，继续待机"
                );
            }
        }

        // 如果有目标，进行决策
        if (m_CurrentTarget != null)
        {
            // 优先级1：检查是否应该使用技能
            if (ShouldUseSkill())
            {
                DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 决策: 使用技能");
                ChangeState(ChessAIState.UsingSkill);
                return;
            }

            // 优先级2：检查是否在攻击范围内
            if (IsInAttackRange(m_CurrentTarget))
            {
                // DebugEx.LogModule(
                //     GetType().Name,
                //     $"{m_Context.Entity.Config.Name} 决策: 进入攻击状态"
                // );
                ChangeState(ChessAIState.Attacking);
                return;
            }

            // 优先级3：需要移动到目标位置
            DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 决策: 移动到目标");
            ChangeState(ChessAIState.Moving);
        }
    }

    /// <summary>
    /// 移动状态逻辑（子类实现具体移动策略）
    /// 转换规则：
    /// - 目标丢失 → 待机
    /// - 到达目标位置 → 待机（由待机重新决策）
    /// </summary>
    protected abstract void TickMoving(float dt);

    /// <summary>
    /// 普攻状态逻辑
    /// 转换规则：
    /// - 目标无效 → 待机
    /// - 攻击中满足技能条件 → 标记，攻击结束后释放技能
    /// - 目标超出范围 → 移动
    /// - 攻击冷却完成 → 继续攻击（保持攻击状态）
    /// </summary>
    protected virtual void TickAttacking(float dt)
    {
        // ⭐ 添加攻击状态执行日志
        // DebugEx.LogModule(
        //     GetType().Name,
        //     $"{m_Context.Entity.Config.Name} TickAttacking执行 - 目标:{m_CurrentTarget?.Config?.Name}, 攻击中:{m_IsAttacking}, 冷却:{m_AttackCooldownTimer:F2}"
        // );

        // 1. 检查目标有效性
        if (!IsTargetValid())
        {
            DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 目标无效，返回待机");
            m_ShouldUseSkillAfterAttack = false; // 清除技能标记
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 2. 检查是否应该使用技能（添加防抖机制）
        if (m_CurrentState == ChessAIState.Attacking && CanMakeSkillDecision())
        {
            if (ShouldUseSkill())
            {
                // 记录技能决策时间，启动防抖
                m_LastSkillDecisionTime = Time.time;
                m_SkillDecisionCooldown = SKILL_DECISION_COOLDOWN;

                // 如果正在攻击中，标记攻击结束后释放技能
                if (m_IsAttacking)
                {
                    if (!m_ShouldUseSkillAfterAttack)
                    {
                        m_ShouldUseSkillAfterAttack = true;
                        DebugEx.LogModule(
                            GetType().Name,
                            $"{m_Context.Entity.Config.Name} 攻击中满足技能条件，标记攻击结束后释放技能"
                        );
                    }
                }
                // 如果不在攻击中，立即切换到技能状态
                else
                {
                    DebugEx.LogModule(
                        GetType().Name,
                        $"{m_Context.Entity.Config.Name} 满足技能条件，切换到使用技能"
                    );
                    ChangeState(ChessAIState.UsingSkill);
                    return;
                }
            }
        }

        // 3. 检查是否超出攻击范围（带缓冲区，防止抖动）
        float attackRange = (float)m_Context.Entity.Attribute.AtkRange;
        float distance = Vector3.Distance(
            m_Context.Entity.transform.position,
            m_CurrentTarget.transform.position
        );

        if (distance > attackRange * ATTACK_RANGE_BUFFER)
        {
            DebugEx.LogModule(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} 目标超出范围，切换到移动"
            );
            m_ShouldUseSkillAfterAttack = false; // 清除技能标记
            ChangeState(ChessAIState.Moving);
            return;
        }

        // 4. 执行攻击（攻击冷却完成且不在攻击中）
        if (m_AttackCooldownTimer <= 0 && !m_IsAttacking)
        {
            Attack(m_CurrentTarget);
        }

        // 注意：攻击状态会持续，直到满足上述转换条件
    }

    /// <summary>
    /// 使用技能状态逻辑
    /// 转换规则：
    /// - 目标无效 → 待机
    /// - 技能释放完成 → 待机（重新决策）
    /// - 技能释放失败 → 移动或待机
    /// </summary>
    protected virtual void TickUsingSkill(float dt)
    {
        // 检查目标有效性
        if (!IsTargetValid())
        {
            DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 目标无效，返回待机");
            ChangeState(ChessAIState.Idle);
            return;
        }

        // 如果还没有开始释放技能，通知 Controller 触发
        if (!m_IsUsingSkill)
        {
            // 面向目标
            FaceTarget(m_CurrentTarget);

            // ✅ 通过 CombatController 触发技能（统一入口）
            if (m_PendingSkillIndex == 2)
            {
                m_Context.Entity.CombatController?.TriggerSkill2FromAI();
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 请求 Controller 执行大招"
                );
            }
            else if (m_PendingSkillIndex == 1)
            {
                m_Context.Entity.CombatController?.TriggerSkill1FromAI();
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 请求 Controller 执行技能1"
                );
            }
            else
            {
                // 异常情况：没有有效的技能索引
                DebugEx.WarningModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 技能索引无效: {m_PendingSkillIndex}"
                );
                ChangeState(ChessAIState.Idle);
                return;
            }

            // 标记正在使用技能
            m_IsUsingSkill = true;
        }

        // 等待技能动画完成（在 OnSkillComplete 回调中处理）
    }

    #endregion

    #region 状态转换

    /// <summary>
    /// 切换状态
    /// </summary>
    protected void ChangeState(ChessAIState newState)
    {
        if (m_CurrentState == newState)
        {
            return;
        }

        // ⭐ 死亡保护：如果当前已经死亡，不允许切换到其他状态（除非重新初始化）
        if (m_CurrentState == ChessAIState.Dead && newState != ChessAIState.Summoning)
        {
            return;
        }

        ChessAIState oldState = m_CurrentState;
        m_PreviousState = oldState;
        m_CurrentState = newState;

        DebugEx.LogModule(
            GetType().Name,
            $"{m_Context.Entity.Config?.Name} 状态切换: {oldState} → {newState}"
        );

        // 退出旧状态
        OnExitState(oldState);

        // 进入新状态
        OnEnterState(newState);
    }

    /// <summary>
    /// 进入状态时的处理
    /// </summary>
    protected virtual void OnEnterState(ChessAIState state)
    {
        switch (state)
        {
            case ChessAIState.Summoning:
                m_SummoningTimer = m_SummoningDuration;
                m_Context.Entity.Movement?.Stop();
                break;

            case ChessAIState.Idle:
                // 进入待机时停止移动
                m_Context.Entity.Movement?.Stop();
                // 立即搜索目标（设置计时器为0）
                m_TargetSearchTimer = 0f;
                break;

            case ChessAIState.Moving:
                // 移动状态初始化（具体移动逻辑在TickMoving中）
                break;

            case ChessAIState.Attacking:
                // 进入攻击状态时停止移动
                m_Context.Entity.Movement?.Stop();
                // 重置攻击状态标志
                m_IsAttacking = false;
                // 重置攻击冷却，确保能够立即攻击
                m_AttackCooldownTimer = 0f;
                break;

            case ChessAIState.UsingSkill:
                // 进入技能状态时停止移动
                m_Context.Entity.Movement?.Stop();
                // 重置技能标记
                m_IsUsingSkill = false;
                break;

            case ChessAIState.Dead:
                // 进入死亡状态时清理所有状态
                m_CurrentTarget = null;
                m_Context.Entity.Movement?.Stop();
                DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config?.Name} 进入死亡状态");
                break;
        }
    }

    /// <summary>
    /// 退出状态时的处理
    /// </summary>
    protected virtual void OnExitState(ChessAIState state)
    {
        switch (state)
        {
            case ChessAIState.Attacking:
                // 退出攻击状态时重置标志，防止状态由于其他原因中断导致标志位残留
                m_IsAttacking = false;
                m_ShouldUseSkillAfterAttack = false;
                break;

            case ChessAIState.UsingSkill:
                // 退出技能状态时重置标志
                m_IsUsingSkill = false;
                break;
        }
    }

    /// <summary>
    /// 强制切换到死亡状态（由外部调用）
    /// </summary>
    public void ForceDead()
    {
        ChangeState(ChessAIState.Dead);
    }

    #endregion

    #region 目标搜索

    /// <summary>
    /// 寻找攻击目标（使用敌人信息缓存优化）
    /// </summary>
    public virtual ChessEntity FindTarget()
    {
        if (m_Context?.Entity == null)
            return null;

        // 检查 CombatEntityTracker 是否可用
        if (CombatEntityTracker.Instance == null)
        {
            DebugEx.Warning(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} 未找到 CombatEntityTracker，无法搜索目标"
            );
            return null;
        }

        int myCamp = m_Context.Entity.Camp;

        // ⭐ 使用敌人信息缓存（如果缓存未构建，会自动构建）
        List<EnemyInfoCache> enemyCache = CombatEntityTracker.Instance.GetEnemyCache(myCamp);

        if (enemyCache.Count == 0)
        {
            return null;
        }

        // ⭐ 使用索敌策略选择最优目标
        if (m_SearchStrategy == null)
        {
            DebugEx.Warning(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} 索敌策略未初始化，使用默认策略"
            );
            m_SearchStrategy = new DefaultTargetSearchStrategy(m_SearchConfig);
        }

        return m_SearchStrategy.SelectBestTarget(m_Context.Entity, enemyCache);
    }

    /// <summary>
    /// 评估目标优先级（已废弃，由 ITargetSearchStrategy 实现）
    /// 保留此方法用于向后兼容或子类重写
    /// </summary>
    [System.Obsolete("请使用 ITargetSearchStrategy 实现索敌逻辑")]
    protected virtual float EvaluateTarget(ChessEntity target)
    {
        float score = 0f;

        Vector3 myPosition = m_Context.Entity.transform.position;

        // 距离因素（30%权重）- 距离越近分数越高
        float distance = Vector3.Distance(myPosition, target.transform.position);
        score += (100f - distance) * 0.3f;

        // 血量因素（50%权重）- 优先攻击残血
        double hpPercent = target.Attribute.CurrentHp / target.Attribute.MaxHp;
        score += (float)(1.0 - hpPercent) * 50f;

        // 威胁度因素（20%权重）
        score += (float)target.Attribute.AtkDamage * 0.2f;

        return score;
    }

    /// <summary>
    /// 检查是否可以进行技能决策（防抖机制）
    /// </summary>
    protected virtual bool CanMakeSkillDecision()
    {
        // 如果防抖计时器还在运行，不允许决策
        if (m_SkillDecisionCooldown > 0)
        {
            return false;
        }

        // 如果已经标记了攻击后使用技能，不再重复决策
        if (m_ShouldUseSkillAfterAttack)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查目标是否有效
    /// </summary>
    protected bool IsTargetValid()
    {
        return m_CurrentTarget != null && !m_CurrentTarget.Attribute.IsDead;
    }

    /// <summary>
    /// 检查目标是否在攻击范围内
    /// </summary>
    protected bool IsInAttackRange(ChessEntity target)
    {
        if (target == null || m_Context?.Entity == null)
            return false;

        float distance = Vector3.Distance(
            m_Context.Entity.transform.position,
            target.transform.position
        );

        return distance <= (float)m_Context.Entity.Attribute.AtkRange;
    }

    #endregion

    #region 移动

    /// <summary>
    /// 执行移动（接口方法）
    /// </summary>
    public virtual void Move(Vector3 targetPosition, float dt)
    {
        // 由子类实现具体移动逻辑
    }

    /// <summary>
    /// 移动到目标位置
    /// </summary>
    protected virtual void MoveToTarget(ChessEntity target)
    {
        if (target == null || m_Context?.Entity == null)
            return;

        Vector3 targetPos = target.transform.position;
        Vector3 selfPos = m_Context.Entity.transform.position;
        Vector3 direction = (targetPos - selfPos).normalized;
        float atkRange = (float)m_Context.Entity.Attribute.AtkRange;

        // 移动到攻击范围边缘
        Vector3 moveTarget = targetPos - direction * (atkRange * 0.8f);

        m_Context.Entity.Movement?.MoveTo(moveTarget);
    }

    #endregion

    #region 攻击

    /// <summary>
    /// 执行攻击
    /// </summary>
    public virtual void Attack(ChessEntity target)
    {
        if (target == null || m_Context?.Entity == null)
        {
            DebugEx.WarningModule(
                GetType().Name,
                $"{m_Context?.Entity?.Config?.Name} 攻击目标为null"
            );
            return;
        }

        // 检查是否在攻击范围内
        if (!IsInAttackRange(target))
        {
            DebugEx.WarningModule(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} 目标不在攻击范围内"
            );
            return;
        }

        // 面向目标
        FaceTarget(target);

        // 标记正在攻击
        m_IsAttacking = true;

        // 通过 CombatController 触发攻击
        if (m_Context.Entity.CombatController != null)
        {
            m_Context.Entity.CombatController.TriggerAttackFromAI(target);
        }

        // 计算攻击冷却
        float atkSpeed = (float)m_Context.Entity.Attribute.AtkSpeed;
        m_AttackCooldownTimer = 1.0f / atkSpeed;

        DebugEx.LogModule(
            GetType().Name,
            $"{m_Context.Entity.Config.Name} 发起攻击 → {target.Config.Name}，冷却: {m_AttackCooldownTimer:F2}秒"
        );
    }

    /// <summary>
    /// 面向目标
    /// </summary>
    protected void FaceTarget(ChessEntity target)
    {
        if (target == null || m_Context?.Entity == null)
            return;

        Vector3 lookDir = target.transform.position - m_Context.Entity.transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            m_Context.Entity.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    /// <summary>
    /// 攻击完成回调
    /// 重置攻击标记，检查是否需要释放技能
    /// </summary>
    public virtual void OnAttackComplete()
    {
        // ⭐ 增加死亡检查，防止死亡后执行回调
        if (m_CurrentState == ChessAIState.Dead)
        {
            return;
        }

        m_IsAttacking = false;

        DebugEx.LogModule(
            GetType().Name,
            $"{m_Context.Entity.Config.Name} 攻击动作完成，重置攻击标记"
        );

        // ✅ 检查是否应该在攻击结束后释放技能
        if (m_ShouldUseSkillAfterAttack)
        {
            m_ShouldUseSkillAfterAttack = false; // 清除标记

            // 再次检查技能条件（可能在攻击过程中条件已不满足）
            if (ShouldUseSkill())
            {
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 攻击结束，切换到使用技能"
                );
                ChangeState(ChessAIState.UsingSkill);
                return;
            }
            else
            {
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 攻击结束，技能条件已不满足，继续攻击"
                );
            }
        }

        // 重置技能决策防抖，允许新的技能决策
        m_SkillDecisionCooldown = 0f;

        // 如果没有技能标记，让 TickAttacking 在下一帧继续决策
        // 如果目标仍有效且在范围内，会继续攻击
    }

    /// <summary>
    /// 技能完成回调（由 ChessCombatController 调用）
    /// </summary>
    public virtual void OnSkillComplete()
    {
        // ⭐ 增加死亡检查
        if (m_CurrentState == ChessAIState.Dead)
        {
            return;
        }

        m_IsUsingSkill = false;
        m_PendingSkillIndex = 0; // 重置技能索引

        DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 技能动画完成，返回待机");

        // 技能完成后返回待机，重新决策
        ChangeState(ChessAIState.Idle);
    }

    #endregion

    #region 目标管理

    /// <summary>
    /// 重置目标状态（用于手动移动完成后）
    /// </summary>
    public virtual void ResetTargetAfterPlayerMove()
    {
        DebugEx.LogModule(
            GetType().Name,
            $"{m_Context.Entity.Config.Name} 手动移动完成，开始立即索敌"
        );

        // ⭐ 玩家手动移动会强制打断当前行为，必须重置动作状态标记
        // 否则如果打断时处于攻击或技能动作中，m_IsAttacking/m_IsUsingSkill 会一直为 true
        m_IsAttacking = false;
        m_IsUsingSkill = false;
        m_AttackCooldownTimer = 0f;
        m_ShouldUseSkillAfterAttack = false;

        // ⭐ 检查当前状态和CombatController状态
        DebugEx.LogModule(
            GetType().Name,
            $"{m_Context.Entity.Config.Name} 当前AI状态: {m_CurrentState}"
        );

        if (m_Context.Entity.CombatController != null)
        {
            DebugEx.LogModule(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} CombatController状态: IsEnabled={m_Context.Entity.CombatController.IsEnabled}, "
                    + $"HasPlayerMoveCommand={m_Context.Entity.CombatController.HasPlayerMoveCommand}"
            );
        }

        // ⭐ 新设计：立即进行索敌，不再依赖定时器
        ChessEntity newTarget = FindTarget();

        if (newTarget != null)
        {
            m_CurrentTarget = newTarget;
            DebugEx.LogModule(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} 立即索敌成功，目标: {newTarget.Config.Name}"
            );

            // 检查是否在攻击范围内
            float distance = Vector3.Distance(
                m_Context.Entity.transform.position,
                newTarget.transform.position
            );
            float attackRange = (float)m_Context.Entity.Attribute.AtkRange;

            if (distance <= attackRange)
            {
                // 在攻击范围内，直接进入攻击状态
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 目标在攻击范围内(距离={distance:F2}, 范围={attackRange:F2})，直接攻击"
                );

                // ⭐ 在状态切换前后添加详细日志
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 准备切换到攻击状态，当前状态: {m_CurrentState}"
                );

                ChangeState(ChessAIState.Attacking);

                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 状态切换完成，新状态: {m_CurrentState}"
                );
            }
            else
            {
                // 不在攻击范围内，先移动到合适位置
                DebugEx.LogModule(
                    GetType().Name,
                    $"{m_Context.Entity.Config.Name} 目标超出攻击范围(距离={distance:F2}, 范围={attackRange:F2})，开始移动"
                );
                ChangeState(ChessAIState.Moving);
            }
        }
        else
        {
            // 没有找到目标，进入待机状态
            DebugEx.LogModule(
                GetType().Name,
                $"{m_Context.Entity.Config.Name} 未找到有效目标，进入待机状态"
            );
            ChangeState(ChessAIState.Idle);

            // 重置搜索计时器，让AI继续定期搜索
            m_TargetSearchTimer = 0f;
        }

        DebugEx.LogModule(GetType().Name, $"{m_Context.Entity.Config.Name} 手动移动后索敌完成");
    }

    #endregion

    #region 技能

    /// <summary>
    /// 判断是否应该使用技能（委托给策略）
    /// </summary>
    protected virtual bool ShouldUseSkill()
    {
        if (m_SkillStrategy == null)
            return false;

        // 委托给策略决策
        int skillIndex = m_SkillStrategy.GetPrioritySkill();

        if (skillIndex > 0)
        {
            m_PendingSkillIndex = skillIndex;
            return true;
        }

        return false;
    }

    #endregion
}
