using UnityEngine;

/// <summary>
/// 敌人追击状态
/// 追击玩家，接近到战斗距离时触发战斗
/// </summary>
public class EnemyChaseState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;
    private float m_UpdatePathTimer;

    private const float PATH_UPDATE_INTERVAL = 0.5f;  // 路径更新间隔

    private bool m_HasBroadcasted;  // 确保只广播一次

    #endregion

    #region 构造函数

    public EnemyChaseState(EnemyEntityAI ai)
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
        m_HasBroadcasted = false;  // 重置广播标志

        // 立即更新路径
        UpdatePath();

        DebugEx.LogModule("EnemyChaseState",
            $"{m_AI.Entity.Config.Name} 开始追击玩家！");
    }

    public void OnUpdate(float deltaTime)
    {
        if (m_AI.PlayerTransform == null)
        {
            DebugEx.WarningModule("EnemyChaseState", "玩家丢失，返回巡逻");
            m_AI.ChangeState(EnemyAIState.Patrol);
            return;
        }

        float distanceToPlayer = m_AI.GetDistanceToPlayer();
        EnemyEntityTable config = m_AI.Entity.Config;

        // 精英敌人在接近玩家时广播（只广播一次）
        if (config.CanBroadcast && !m_HasBroadcasted)
        {
            // 触发条件：和玩家距离在发现距离内
            if (distanceToPlayer <= config.DetectDistance)
            {
                BroadcastPlayerPosition();  // 广播玩家位置
                DebugEx.LogModule("EnemyChaseState", $"{m_AI.Entity.Config.Name} 广播召集附近敌人");
                m_HasBroadcasted = true;
            }
        }

        // 检查是否接近到战斗距离
        if (distanceToPlayer <= config.CombatDistance)
        {
            // 保底屏蔽或玩家处于隐身状态时放弃追击，返回巡逻
            var playerGo = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
            var stealth = playerGo?.GetComponent<PostCombatStealth>();
            bool blocked = (EnemyEntityManager.Instance != null && EnemyEntityManager.Instance.IsDetectionBlocked)
                           || (stealth != null && stealth.IsActive);
            if (blocked)
            {
                DebugEx.LogModule("EnemyChaseState",
                    $"{m_AI.Entity.Config.Name} 玩家处于隐身/屏蔽状态，放弃追击");
                m_AI.ChangeState(EnemyAIState.Patrol);
                return;
            }

            DebugEx.LogModule("EnemyChaseState",
                $"{m_AI.Entity.Config.Name} 接近玩家，触发战斗！");
            TriggerCombat();
            return;
        }

        // 检查是否超出追击范围
        if (distanceToPlayer > config.ChaseDistance)
        {
            DebugEx.LogModule("EnemyChaseState",
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

        // 通知管理器触发战斗
        EnemyEntityManager manager = EnemyEntityManager.Instance;
        if (manager != null)
        {
            manager.TriggerCombat(m_AI.Entity);
        }
        else
        {
            DebugEx.ErrorModule("EnemyChaseState", "EnemyEntityManager 不存在！");
        }
    }

    private void BroadcastPlayerPosition()
    {
        DebugEx.LogModule("EnemyChaseState",
            $"{m_AI.Entity.Config.Name} 广播玩家位置，召集附近敌人");

        // 通知以自己为中心、BroadcastDistance 范围内的敌人
        EnemyGroupManager.Instance?.BroadcastPlayerPosition(
            m_AI.Entity,                              // 广播者
            m_AI.PlayerTransform.position,            // 玩家位置
            m_AI.Entity.Config.BroadcastDistance      // 广播范围（以广播者为中心）
        );
    }

    #endregion
}
