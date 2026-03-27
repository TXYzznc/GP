using UnityEngine;

/// <summary>
/// 敌人深度休息状态
/// 不会被玩家接近惊醒，头上显示睡眠条
/// </summary>
public class EnemyRestState : IEnemyState
{
    #region 私有字段

    private EnemyEntityAI m_AI;
    private float m_RestTimer;
    private float m_RestDuration;

    #endregion

    #region 构造函数

    public EnemyRestState(EnemyEntityAI ai)
    {
        m_AI = ai;
    }

    #endregion

    #region IEnemyState 实现

    public void OnInitialize()
    {
        // 初始化
    }

    public void OnEnter()
    {
        // 停止移动
        m_AI.Entity.NavAgent.isStopped = true;

        // 随机休息时长（配置值的 1.5-2.5 倍，深度休息更久）
        EnemyEntityTable config = m_AI.Entity.Config;
        m_RestDuration = Random.Range(config.RestDuration * 1.5f, config.RestDuration * 2.5f);
        m_RestTimer = 0f;

        DebugEx.LogModule("EnemyRestState", 
            $"{m_AI.Entity.Config.Name} 进入深度休息，时长={m_RestDuration:F1}秒");

        // TODO: 显示睡眠条UI
        ShowSleepBar(true);
    }

    public void OnUpdate(float deltaTime)
    {
        // 深度休息状态不检测玩家，只计时

        // 更新休息计时器
        m_RestTimer += deltaTime;

        // 更新睡眠条进度
        UpdateSleepBarProgress(m_RestTimer / m_RestDuration);

        // 休息时间结束，切换到巡逻
        if (m_RestTimer >= m_RestDuration)
        {
            DebugEx.LogModule("EnemyRestState", 
                $"{m_AI.Entity.Config.Name} 休息结束，开始巡逻");
            m_AI.ChangeState(EnemyAIState.Patrol);
        }
    }

    public void OnExit()
    {
        // 恢复移动
        m_AI.Entity.NavAgent.isStopped = false;

        // 隐藏睡眠条
        ShowSleepBar(false);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 显示/隐藏睡眠条
    /// </summary>
    private void ShowSleepBar(bool show)
    {
        // TODO: 实现睡眠条UI显示逻辑
        DebugEx.LogModule("EnemyRestState", 
            $"睡眠条显示: {show}");
    }

    /// <summary>
    /// 更新睡眠条进度
    /// </summary>
    private void UpdateSleepBarProgress(float progress)
    {
        // TODO: 更新睡眠条进度
    }

    #endregion
}
