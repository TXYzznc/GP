using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人巡逻状态
/// 在巡逻范围内随机移动，到达目标点后可能休息
/// </summary>
public class EnemyPatrolState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;
    private Vector3 m_PatrolTarget;
    private bool m_HasTarget;
    private float m_StuckTimer;
    private Vector3 m_LastPosition;

    private const float STUCK_CHECK_INTERVAL = 2f;  // 卡住检测间隔
    private const float STUCK_DISTANCE_THRESHOLD = 0.5f;  // 卡住距离阈值

    #endregion

    #region 构造函数

    public EnemyPatrolState(EnemyEntityAI ai)
    {
        m_AI = ai;
    }

    #endregion

    #region IEnemyState 实现

    public void OnInitialize()
    {
        // 初始化时不需要做什么
    }

    public void OnEnter()
    {
        // 设置巡逻速度
        m_AI.Entity.NavAgent.speed = m_AI.Entity.Config.PatrolSpeed;

        // 播放巡逻动画
        var animator = m_AI.Entity.GetComponent<EnemyAnimator>();
        animator?.PlayAnimation(EnemyAnimationType.Walk, m_AI.Entity.Config.PatrolSpeed);

        // 选择新的巡逻目标
        SelectNewPatrolTarget();

        m_StuckTimer = 0f;
        m_LastPosition = m_AI.Entity.transform.position;

        DebugEx.LogModule("EnemyPatrolState",
            $"{m_AI.Entity.Config.Name} 开始巡逻");
    }

    public void OnUpdate(float deltaTime)
    {
        // 检查是否检测到玩家
        if (m_AI.IsPlayerDetected)
        {
            m_AI.ChangeState(EnemyAIState.Alert);
            return;
        }

        // 检查是否有目标
        if (!m_HasTarget)
        {
            SelectNewPatrolTarget();
            return;
        }

        // 检查是否到达目标
        if (HasReachedTarget())
        {
            OnReachedTarget();
            return;
        }

        // 检查是否卡住
        CheckIfStuck(deltaTime);
    }

    public void OnExit()
    {
        m_HasTarget = false;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 选择新的巡逻目标点
    /// </summary>
    private void SelectNewPatrolTarget()
    {
        EnemyEntityTable config = m_AI.Entity.Config;
        Vector3 spawnPos = m_AI.Entity.SpawnPosition;

        // 在巡逻半径内随机选择一个点
        Vector3 randomDirection = Random.insideUnitSphere * config.PatrolRadius;
        randomDirection.y = 0;  // 保持在同一高度
        Vector3 targetPos = spawnPos + randomDirection;

        // 使用 NavMesh 采样确保目标点可达
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, config.PatrolRadius, NavMesh.AllAreas))
        {
            m_PatrolTarget = hit.position;
            m_AI.Entity.NavAgent.SetDestination(m_PatrolTarget);
            m_HasTarget = true;

            DebugEx.LogModule("EnemyPatrolState", 
                $"{m_AI.Entity.Config.Name} 选择新巡逻点: {m_PatrolTarget}");
        }
        else
        {
            // DebugEx.WarningModule("EnemyPatrolState", 
            //     $"{m_AI.Entity.Config.Name} 无法找到有效的巡逻点");
            m_HasTarget = false;
        }
    }

    /// <summary>
    /// 检查是否到达目标
    /// </summary>
    private bool HasReachedTarget()
    {
        if (!m_HasTarget) return false;

        NavMeshAgent agent = m_AI.Entity.NavAgent;

        // 检查是否接近目标且路径计算完成
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 到达目标点的处理
    /// </summary>
    private void OnReachedTarget()
    {
        m_HasTarget = false;

        DebugEx.LogModule("EnemyPatrolState", 
            $"{m_AI.Entity.Config.Name} 到达巡逻点");

        // 随机决定是否休息
        EnemyEntityTable config = m_AI.Entity.Config;
        if (Random.value < config.RestProbability)
        {
            m_AI.ChangeState(EnemyAIState.Idle);
        }
        else
        {
            // 继续巡逻，选择新目标
            SelectNewPatrolTarget();
        }
    }

    /// <summary>
    /// 检查是否卡住
    /// </summary>
    private void CheckIfStuck(float deltaTime)
    {
        m_StuckTimer += deltaTime;

        if (m_StuckTimer >= STUCK_CHECK_INTERVAL)
        {
            float movedDistance = Vector3.Distance(m_AI.Entity.transform.position, m_LastPosition);

            if (movedDistance < STUCK_DISTANCE_THRESHOLD)
            {
                DebugEx.WarningModule("EnemyPatrolState", 
                    $"{m_AI.Entity.Config.Name} 可能卡住了，重新选择目标");
                SelectNewPatrolTarget();
            }

            m_LastPosition = m_AI.Entity.transform.position;
            m_StuckTimer = 0f;
        }
    }

    #endregion
}
