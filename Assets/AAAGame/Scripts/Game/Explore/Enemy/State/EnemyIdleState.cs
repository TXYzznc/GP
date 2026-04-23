using UnityEngine;

/// <summary>
/// 敌人休息状态
/// 原地待机，随机时长后切换到巡逻状态
/// </summary>
public class EnemyIdleState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;
    private float m_IdleTimer;
    private float m_IdleDuration;

    #endregion

    #region 构造函数

    public EnemyIdleState(EnemyEntityAI ai)
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
        // 停止移动（需检查是否在 NavMesh 上，SetActive 恢复后第一帧可能未就绪）
        var agent = m_AI.Entity.NavAgent;
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;

        // 播放待机动画
        var animator = m_AI.Entity.GetComponent<EnemyAnimator>();
        animator?.PlayAnimation(EnemyAnimationType.Idle);

        // 随机休息时长
        EnemyEntityTable config = m_AI.Entity.Config;
        m_IdleDuration = Random.Range(config.RestDuration * 0.5f, config.RestDuration * 1.5f);
        m_IdleTimer = 0f;

        DebugEx.LogModule("EnemyIdleState",
            $"{m_AI.Entity.Config.Name} 开始休息，时长={m_IdleDuration:F1}秒");
    }

    public void OnUpdate(float deltaTime)
    {
        // 检查是否检测到玩家
        if (m_AI.IsPlayerDetected)
        {
            m_AI.ChangeState(EnemyAIState.Alert);
            return;
        }

        // 更新休息计时器
        m_IdleTimer += deltaTime;

        // 休息时间结束，切换到巡逻或深度休息
        if (m_IdleTimer >= m_IdleDuration)
        {
            // 根据配置的休息概率决定是否进入深度休息
            EnemyEntityTable config = m_AI.Entity.Config;
            if (Random.value < config.RestProbability)
            {
                DebugEx.LogModule("EnemyIdleState", 
                    $"{m_AI.Entity.Config.Name} 进入深度休息状态");
                m_AI.ChangeState(EnemyAIState.Rest);
            }
            else
            {
                m_AI.ChangeState(EnemyAIState.Patrol);
            }
        }
    }

    public void OnExit()
    {
        // 恢复移动
        m_AI.Entity.NavAgent.isStopped = false;
    }

    #endregion
}
