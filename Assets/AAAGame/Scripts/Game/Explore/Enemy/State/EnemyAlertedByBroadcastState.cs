using UnityEngine;

/// <summary>
/// 敌人被广播警戒状态
/// 收到广播后追击玩家，可被拉入群体战斗
/// </summary>
public class EnemyAlertedByBroadcastState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;
    private float m_UpdatePathTimer;

    private const float PATH_UPDATE_INTERVAL = 0.5f;  // 路径更新间隔

    #endregion

    #region 构造函数

    public EnemyAlertedByBroadcastState(EnemyEntityAI ai)
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
        // 设置追击速度
        m_AI.Entity.NavAgent.speed = m_AI.Entity.Config.ChaseSpeed;

        m_UpdatePathTimer = 0f;

        // 立即更新路径
        UpdatePath();

        DebugEx.LogModule("EnemyAlertedByBroadcastState",
            $"{m_AI.Entity.Config.Name} 收到广播，开始追击玩家！");
    }

    public void OnUpdate(float deltaTime)
    {
        if (m_AI.PlayerTransform == null)
        {
            DebugEx.WarningModule("EnemyAlertedByBroadcastState", "玩家丢失，返回巡逻");
            m_AI.ChangeState(EnemyAIState.Patrol);
            return;
        }

        float distanceToPlayer = m_AI.GetDistanceToPlayer();
        EnemyEntityTable config = m_AI.Entity.Config;

        // 检查是否接近到战斗距离
        if (distanceToPlayer <= config.CombatDistance)
        {
            DebugEx.LogModule("EnemyAlertedByBroadcastState",
                $"{m_AI.Entity.Config.Name} 接近玩家，触发战斗！");
            TriggerCombat();
            return;
        }

        // 检查是否超出追击范围
        if (distanceToPlayer > config.ChaseDistance)
        {
            DebugEx.LogModule("EnemyAlertedByBroadcastState",
                $"{m_AI.Entity.Config.Name} 玩家逃离，放弃追击，清除目标");

            // 清除玩家检测状态
            m_AI.ClearPlayerDetection();

            m_AI.ChangeState(EnemyAIState.Patrol);
            return;
        }

        // 定期更新路径
        m_UpdatePathTimer += deltaTime;
        if (m_UpdatePathTimer >= PATH_UPDATE_INTERVAL)
        {
            UpdatePath();
            m_UpdatePathTimer = 0f;
        }
    }

    public void OnExit()
    {
        // 停止移动
        m_AI.Entity.NavAgent.isStopped = true;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新追击路径
    /// </summary>
    private void UpdatePath()
    {
        if (m_AI.PlayerTransform == null) return;

        m_AI.Entity.NavAgent.SetDestination(m_AI.PlayerTransform.position);
    }

    /// <summary>
    /// 触发战斗
    /// </summary>
    private void TriggerCombat()
    {
        // 切换到战斗状态
        m_AI.ChangeState(EnemyAIState.Combat);

        // 检测群体战斗
        EnemyGroupManager manager = EnemyGroupManager.Instance;
        if (manager != null)
        {
            manager.DetectAndTriggerGroupCombat(m_AI.Entity);
        }
        else
        {
            DebugEx.ErrorModule("EnemyAlertedByBroadcastState", "EnemyGroupManager 不存在！");
        }
    }

    #endregion
}
