using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人实体AI状态机
/// 管理敌人的行为状态和状态转换
/// </summary>
public class EnemyEntityAI
{
    #region 私有字段

    /// <summary>敌人实体引用</summary>
    private EnemyEntity m_Entity;

    /// <summary>当前状态</summary>
    private EnemyAIState m_CurrentState = (EnemyAIState)(-1);  // 初始化为无效值，确保第一次切换生效

    /// <summary>状态实例字典</summary>
    private Dictionary<EnemyAIState, IEnemyState> m_States;

    /// <summary>当前状态实例</summary>
    private IEnemyState m_CurrentStateInstance;

    /// <summary>玩家Transform缓存</summary>
    private Transform m_PlayerTransform;

    /// <summary>检测到玩家的累计时间</summary>
    private float m_DetectTimer;

    /// <summary>是否已检测到玩家</summary>
    private bool m_IsPlayerDetected;

    /// <summary>查找玩家的间隔（秒）</summary>
    private const float FIND_PLAYER_INTERVAL = 0.5f;

    /// <summary>查找玩家的计时器</summary>
    private float m_FindPlayerTimer;

    /// <summary>是否收到过广播</summary>
    private bool m_HasReceivedBroadcast;

    #endregion

    #region 属性

    /// <summary>当前状态</summary>
    public EnemyAIState CurrentState => m_CurrentState;

    /// <summary>敌人实体</summary>
    public EnemyEntity Entity => m_Entity;

    /// <summary>玩家Transform</summary>
    public Transform PlayerTransform => m_PlayerTransform;

    /// <summary>是否已检测到玩家</summary>
    public bool IsPlayerDetected => m_IsPlayerDetected;

    /// <summary>是否收到过广播</summary>
    public bool HasReceivedBroadcast => m_HasReceivedBroadcast;

    #endregion

    #region 构造函数

    public EnemyEntityAI(EnemyEntity entity)
    {
        m_Entity = entity;
        m_States = new Dictionary<EnemyAIState, IEnemyState>();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化AI状态机
    /// </summary>
    public void Initialize()
    {
        // 创建所有状态实例
        m_States[EnemyAIState.Idle] = new EnemyIdleState(this);
        m_States[EnemyAIState.Patrol] = new EnemyPatrolState(this);
        m_States[EnemyAIState.Alert] = new EnemyAlertState(this);
        m_States[EnemyAIState.Chase] = new EnemyChaseState(this);
        m_States[EnemyAIState.AlertedByBroadcast] = new EnemyAlertedByBroadcastState(this);
        m_States[EnemyAIState.Combat] = new EnemyCombatState(this);
        m_States[EnemyAIState.Rest] = new EnemyRestState(this);

        // 初始化所有状态
        foreach (var state in m_States.Values)
        {
            state.OnInitialize();
        }

        // 尝试查找玩家（可能失败，没关系）
        FindPlayer();

        // 切换到初始状态
        ChangeState(EnemyAIState.Idle);

        DebugEx.LogModule("EnemyEntityAI", $"{m_Entity.Config.Name} AI初始化完成");
    }

    /// <summary>
    /// 更新AI（每帧调用）
    /// </summary>
    public void Tick(float deltaTime)
    {
        // 如果还没找到玩家，定期尝试查找
        if (m_PlayerTransform == null)
        {
            m_FindPlayerTimer += deltaTime;

            if (m_FindPlayerTimer >= FIND_PLAYER_INTERVAL)
            {
                FindPlayer();
                m_FindPlayerTimer = 0f;
            }

            // 没找到玩家，跳过本帧AI更新
            return;
        }

        if (m_CurrentStateInstance == null) return;

        // 更新玩家检测
        UpdatePlayerDetection(deltaTime);

        // 更新当前状态
        m_CurrentStateInstance.OnUpdate(deltaTime);
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ChangeState(EnemyAIState newState)
    {
        if (m_CurrentState == newState) return;

        // 退出当前状态
        if (m_CurrentStateInstance != null)
        {
            m_CurrentStateInstance.OnExit();
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 状态切换: {m_CurrentState} → {newState}");
        }

        // 切换状态
        EnemyAIState oldState = m_CurrentState;
        m_CurrentState = newState;

        // 进入新状态
        if (m_States.TryGetValue(newState, out m_CurrentStateInstance))
        {
            m_CurrentStateInstance.OnEnter();
        }
        else
        {
            DebugEx.ErrorModule("EnemyEntityAI", $"状态不存在: {newState}");
        }
    }

    /// <summary>
    /// 重置到待机状态
    /// </summary>
    public void ResetToIdle()
    {
        ClearPlayerDetection();
        ChangeState(EnemyAIState.Idle);
    }

    /// <summary>
    /// 清除玩家检测状态（丢失目标）
    /// </summary>
    public void ClearPlayerDetection()
    {
        m_IsPlayerDetected = false;
        m_DetectTimer = 0f;

        DebugEx.LogModule("EnemyEntityAI",
            $"{m_Entity.Config.Name} 清除玩家检测状态（丢失目标）");
    }

    /// <summary>
    /// 获取到玩家的距离
    /// </summary>
    public float GetDistanceToPlayer()
    {
        // 检查玩家是否还存在
        if (m_PlayerTransform == null)
        {
            return float.MaxValue;
        }

        // 检查玩家对象是否被销毁
        if (m_PlayerTransform.gameObject == null)
        {
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 玩家对象已销毁，清除引用");
            m_PlayerTransform = null;
            return float.MaxValue;
        }

        return Vector3.Distance(m_Entity.transform.position, m_PlayerTransform.position);
    }

    /// <summary>
    /// 获取到出生点的距离
    /// </summary>
    public float GetDistanceToSpawn()
    {
        return Vector3.Distance(m_Entity.transform.position, m_Entity.SpawnPosition);
    }

    /// <summary>
    /// 接收广播（被其他敌人召集）
    /// </summary>
    public void OnReceiveBroadcast(Vector3 playerPosition)
    {
        // 如果已经在追击玩家，标记收到广播但不改变状态
        if (m_CurrentState == EnemyAIState.Chase)
        {
            m_HasReceivedBroadcast = true;
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 已在追击玩家，标记收到广播");
            return;
        }

        // 如果在战斗中，直接返回
        if (m_CurrentState == EnemyAIState.Combat)
        {
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 正在战斗中，忽略广播");
            return;
        }

        // 检查玩家是否在追击距离内
        float distanceToPlayer = Vector3.Distance(m_Entity.transform.position, playerPosition);
        if (distanceToPlayer > m_Entity.Config.ChaseDistance)
        {
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 距离玩家太远({distanceToPlayer:F1}m)，不响应广播");
            return;
        }

        // 记录玩家位置
        DebugEx.LogModule("EnemyEntityAI",
            $"{m_Entity.Config.Name} 收到广播，玩家位置={playerPosition}，距离={distanceToPlayer:F1}m");

        // 标记为已检测到玩家和收到广播
        m_IsPlayerDetected = true;
        m_DetectTimer = m_Entity.Config.AlertTime;
        m_HasReceivedBroadcast = true;

        // 切换到 AlertedByBroadcast 状态
        ChangeState(EnemyAIState.AlertedByBroadcast);
    }

    /// <summary>
    /// 玩家进入战斗时调用（让追击敌人恢复巡逻）
    /// </summary>
    public void OnPlayerEnteredCombat()
    {
        // 只处理追击和被广播警戒状态的敌人
        if (m_CurrentState != EnemyAIState.Chase &&
            m_CurrentState != EnemyAIState.AlertedByBroadcast)
        {
            return;
        }

        DebugEx.LogModule("EnemyEntityAI",
            $"{m_Entity.Config.Name} 玩家进入战斗，恢复巡逻状态");

        // 清除检测状态
        ClearPlayerDetection();
        m_HasReceivedBroadcast = false;

        // 切换到巡逻状态
        ChangeState(EnemyAIState.Patrol);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 查找玩家
    /// </summary>
    private void FindPlayer()
    {
        // 通过Tag查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            m_PlayerTransform = playerObj.transform;
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 找到玩家");
        }
        // 不输出警告，因为玩家可能还没生成
    }

    /// <summary>
    /// 更新玩家检测
    /// 使用VisionConeDetector进行扇形视野检测
    /// </summary>
    private void UpdatePlayerDetection(float deltaTime)
    {
        if (m_PlayerTransform == null) return;

        // 通过VisionConeDetector更新检测
        var detector = m_Entity.VisionDetector;
        if (detector == null) return;

        detector.UpdateDetection(m_PlayerTransform, deltaTime);

        // 根据警觉度判断是否检测到玩家
        float alertThreshold = m_Entity.Config.AlertThreshold;
        bool shouldDetect = detector.AlertLevel >= alertThreshold;

        if (shouldDetect && !m_IsPlayerDetected)
        {
            m_IsPlayerDetected = true;
            float distance = GetDistanceToPlayer();
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 检测到玩家！距离={distance:F1}m, 警觉度={detector.AlertLevel:F2}");
        }
        else if (!shouldDetect && m_IsPlayerDetected)
        {
            m_IsPlayerDetected = false;
            DebugEx.LogModule("EnemyEntityAI",
                $"{m_Entity.Config.Name} 玩家离开检测范围");
        }

        // 保持兼容性：更新检测计时器
        m_DetectTimer = detector.AlertLevel;
    }

    #endregion
}

/// <summary>
/// 敌人状态接口
/// </summary>
public interface IEnemyState
{
    /// <summary>初始化状态</summary>
    void OnInitialize();

    /// <summary>进入状态</summary>
    void OnEnter();

    /// <summary>更新状态</summary>
    void OnUpdate(float deltaTime);

    /// <summary>退出状态</summary>
    void OnExit();
}
