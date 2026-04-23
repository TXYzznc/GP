using UnityEngine;

/// <summary>
/// 敌人警戒状态
/// 发现玩家后进入警戒，持续一段时间后开始追击
/// </summary>
public class EnemyAlertState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;
    private float m_AlertTimer;

    #endregion

    #region 构造函数

    public EnemyAlertState(EnemyEntityAI ai)
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
        // 停止移动，原地警戒
        m_AI.Entity.NavAgent.isStopped = true;

        // 播放警戒动画
        var animator = m_AI.Entity.GetComponent<EnemyAnimator>();
        animator?.PlayAnimation(EnemyAnimationType.Alert);

        m_AlertTimer = 0f;

        DebugEx.LogModule("EnemyAlertState",
            $"{m_AI.Entity.Config.Name} 进入警戒状态！");

        // TODO: 显示感叹号特效
    }

    public void OnUpdate(float deltaTime)
    {
        // 获取视野检测器并检查警觉度
        var detector = m_AI.Entity.VisionDetector;
        if (detector != null)
        {
            // 警觉度衰减到0，返回巡逻
            if (detector.AlertLevel <= 0f)
            {
                DebugEx.LogModule("EnemyAlertState",
                    $"{m_AI.Entity.Config.Name} 警觉度消散，返回巡逻");
                m_AI.ChangeState(EnemyAIState.Patrol);
                return;
            }

            // 警觉度达到阈值，开始追击
            if (detector.AlertLevel >= m_AI.Entity.Config.AlertThreshold)
            {
                DebugEx.LogModule("EnemyAlertState",
                    $"{m_AI.Entity.Config.Name} 警觉度达到阈值，开始追击！");
                m_AI.ChangeState(EnemyAIState.Chase);
                return;
            }
        }

        // 检查玩家是否还在检测范围内
        if (!m_AI.IsPlayerDetected)
        {
            DebugEx.LogModule("EnemyAlertState",
                $"{m_AI.Entity.Config.Name} 玩家离开，返回巡逻");
            m_AI.ChangeState(EnemyAIState.Patrol);
            return;
        }

        // 面向玩家
        if (m_AI.PlayerTransform != null)
        {
            Vector3 direction = m_AI.PlayerTransform.position - m_AI.Entity.transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                m_AI.Entity.transform.rotation = Quaternion.Slerp(
                    m_AI.Entity.transform.rotation,
                    targetRotation,
                    deltaTime * 5f
                );
            }
        }

        // 更新警戒计时器
        m_AlertTimer += deltaTime;

        // 警戒时间结束，开始追击
        if (m_AlertTimer >= m_AI.Entity.Config.AlertTime)
        {
            DebugEx.LogModule("EnemyAlertState",
                $"{m_AI.Entity.Config.Name} 警戒结束，开始追击！");
            m_AI.ChangeState(EnemyAIState.Chase);
        }
    }

    public void OnExit()
    {
        // 恢复移动
        m_AI.Entity.NavAgent.isStopped = false;

        // TODO: 隐藏感叹号特效
    }

    #endregion
}
