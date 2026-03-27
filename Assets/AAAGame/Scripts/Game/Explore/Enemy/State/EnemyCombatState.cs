using UnityEngine;

/// <summary>
/// 敌人战斗状态
/// 敌人进入战斗，停止AI行为，等待战斗结束
/// </summary>
public class EnemyCombatState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;

    #endregion

    #region 构造函数

    public EnemyCombatState(EnemyEntityAI ai)
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
        DebugEx.LogModule("EnemyCombatState", 
            $"{m_AI.Entity.Config.Name} 进入战斗状态");

        // 停止 NavMeshAgent
        m_AI.Entity.NavAgent.isStopped = true;
    }

    public void OnUpdate(float deltaTime)
    {
        // 战斗状态下不需要更新AI
        // 战斗由 EnemyEntityManager 和战斗系统管理
    }

    public void OnExit()
    {
        DebugEx.LogModule("EnemyCombatState",
            $"{m_AI.Entity.Config.Name} 离开战斗状态");

        // 恢复 NavMeshAgent（需检查是否在 NavMesh 上，SetActive(false/true) 后可能未就绪）
        var agent = m_AI.Entity.NavAgent;
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    #endregion
}
