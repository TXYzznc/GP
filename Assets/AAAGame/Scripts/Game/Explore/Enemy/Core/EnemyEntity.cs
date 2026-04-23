using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人实体组件
/// 负责初始化敌人实体，管理AI状态机和战斗触发
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyEntity : MonoBehaviour
{
    #region 配置

    [Header("配置")]
    [SerializeField]
    [Tooltip("敌人实体配置ID（对应 EnemyEntityTable）")]
    private int m_EntityConfigId = 1001;

    [Header("调试")]
    [SerializeField]
    [Tooltip("是否显示调试信息")]
    private bool m_ShowDebug = true;

    #endregion

    #region 私有字段

    /// <summary>配置数据</summary>
    private EnemyEntityTable m_Config;

    /// <summary>AI状态机</summary>
    private EnemyEntityAI m_AI;

    /// <summary>NavMesh代理</summary>
    private NavMeshAgent m_NavAgent;

    /// <summary>出生点位置</summary>
    private Vector3 m_SpawnPosition;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    /// <summary>是否在战斗中</summary>
    private bool m_IsInCombat;

    /// <summary>敌人类型</summary>
    private EnemyType m_EnemyType;

    /// <summary>敌人状态</summary>
    private EnemyStatus m_Status = EnemyStatus.Alive;

    /// <summary>视野检测器</summary>
    private VisionConeDetector m_VisionDetector;

    /// <summary>实体唯一标识符（Awake 中生成）</summary>
    private string m_EntityGuid;

    #endregion

    #region 属性

    /// <summary>配置数据</summary>
    public EnemyEntityTable Config => m_Config;

    /// <summary>AI状态机</summary>
    public EnemyEntityAI AI => m_AI;

    /// <summary>NavMesh代理</summary>
    public NavMeshAgent NavAgent => m_NavAgent;

    /// <summary>出生点位置</summary>
    public Vector3 SpawnPosition => m_SpawnPosition;

    /// <summary>是否在战斗中</summary>
    public bool IsInCombat => m_IsInCombat;

    /// <summary>实体配置ID</summary>
    public int EntityConfigId => m_EntityConfigId;

    /// <summary>敌人类型</summary>
    public EnemyType EnemyType => m_EnemyType;

    /// <summary>敌人状态</summary>
    public EnemyStatus Status => m_Status;

    /// <summary>视野检测器</summary>
    public VisionConeDetector VisionDetector => m_VisionDetector;

    /// <summary>实体唯一标识符</summary>
    public string EntityGuid => m_EntityGuid;

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置敌人配置ID（运行时动态生成用）
    /// 必须在 Awake 和 Start 之间调用，以便 Start 能读到正确值
    /// </summary>
    public void SetEntityConfigId(int id)
    {
        m_EntityConfigId = id;
    }

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_NavAgent = GetComponent<NavMeshAgent>();
        m_SpawnPosition = transform.position;
        m_EntityGuid = Guid.NewGuid().ToString();
    }

    private void Start()
    {
        Initialize();

        // 注册到管理器
        if (m_IsInitialized)
        {
            EnemyEntityManager.Instance.RegisterEntity(this);
        }
    }

    private void Update()
    {
        if (!m_IsInitialized || m_IsInCombat) return;

        // 更新AI
        m_AI?.Tick(Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (!m_ShowDebug || m_Config == null) return;

        Vector3 center = Application.isPlaying ? m_SpawnPosition : transform.position;

        // 绘制巡逻范围（绿色）
        Gizmos.color = Color.green;
        DrawCircle(center, m_Config.PatrolRadius);

        // 绘制警戒范围（黄色）
        Gizmos.color = Color.yellow;
        DrawCircle(center, m_Config.AlertDistance);

        // 绘制追击范围（红色）
        Gizmos.color = Color.red;
        DrawCircle(center, m_Config.ChaseDistance);

        // 绘制战斗距离（橙色）
        Gizmos.color = new Color(1f, 0.5f, 0f);
        DrawCircle(transform.position, m_Config.CombatDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (!m_ShowDebug || !Application.isPlaying || m_AI == null) return;

        // 显示当前状态
        Vector3 textPos = transform.position + Vector3.up * 3f;
        UnityEditor.Handles.Label(textPos, $"State: {m_AI.CurrentState}");
    }

    private void OnDestroy()
    {
        // 从管理器注销
        if (EnemyEntityManager.Instance != null)
        {
            EnemyEntityManager.Instance.UnregisterEntity(this);
        }

        // 清理棋子数据（玩家胜利时 Destroy 触发此清理）
        EnemyChessDataManager.Instance.RemoveAllForEntity(m_EntityGuid);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化敌人实体
    /// </summary>
    public void Initialize()
    {
        if (m_IsInitialized)
        {
            DebugEx.WarningModule("EnemyEntity", "已经初始化过了");
            return;
        }

        // 加载配置
        if (!LoadConfig())
        {
            DebugEx.ErrorModule("EnemyEntity", $"加载配置失败: EntityConfigId={m_EntityConfigId}");
            return;
        }

        // 设置敌人类型
        m_EnemyType = (EnemyType)m_Config.EnemyType;

        // 配置 NavMeshAgent
        ConfigureNavAgent();

        // 初始化视野检测器
        m_VisionDetector = GetComponent<VisionConeDetector>();
        if (m_VisionDetector == null)
        {
            m_VisionDetector = gameObject.AddComponent<VisionConeDetector>();
        }
        m_VisionDetector.Initialize();

        // 创建AI状态机
        m_AI = new EnemyEntityAI(this);
        m_AI.Initialize();

        // 初始化动画控制器
        var animator = GetComponent<EnemyAnimator>();
        if (animator != null)
        {
            animator.Initialize(this);
        }

        m_IsInitialized = true;

        // 从 EnemyTable 读取棋子列表并注册到 EnemyChessDataManager
        RegisterChessData();

        DebugEx.LogModule("EnemyEntity",
            $"初始化完成: {m_Config.Name}, 类型={m_EnemyType}, 可广播={m_Config.CanBroadcast}, 奖励等级={m_Config.RewardTier}");
    }

    /// <summary>
    /// 进入战斗状态
    /// </summary>
    public void EnterCombat()
    {
        if (m_IsInCombat) return;

        m_IsInCombat = true;
        m_NavAgent.isStopped = true;

        DebugEx.LogModule("EnemyEntity", $"{m_Config.Name} 进入战斗状态");
    }

    /// <summary>
    /// 离开战斗状态
    /// </summary>
    public void ExitCombat()
    {
        if (!m_IsInCombat) return;

        m_IsInCombat = false;

        // SetActive(false) 后重新 SetActive(true) 时 agent 需要一帧才能重新放置到 NavMesh
        if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
            m_NavAgent.isStopped = false;

        // 重置AI状态
        m_AI?.ResetToIdle();

        DebugEx.LogModule("EnemyEntity", $"{m_Config.Name} 离开战斗状态");
    }

    /// <summary>
    /// 设置敌人状态
    /// </summary>
    public void SetStatus(EnemyStatus status)
    {
        if (m_Status == status) return;

        EnemyStatus oldStatus = m_Status;
        m_Status = status;

        DebugEx.LogModule("EnemyEntity", 
            $"{m_Config.Name} 状态变更: {oldStatus} → {status}");

        // 根据状态执行相应逻辑
        OnStatusChanged(oldStatus, status);
    }

    /// <summary>
    /// 状态变更回调
    /// </summary>
    private void OnStatusChanged(EnemyStatus oldStatus, EnemyStatus newStatus)
    {
        switch (newStatus)
        {
            case EnemyStatus.Defeated:
                // 被击败，切换到Defeated状态
                m_AI?.ChangeState(EnemyAIState.Defeated);
                DebugEx.LogModule("EnemyEntity", $"{m_Config.Name} 被击败");
                break;

            case EnemyStatus.Purified:
                // 已净化
                DebugEx.LogModule("EnemyEntity", $"{m_Config.Name} 已净化");
                break;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 从 EnemyTable 读取 ChessIds，向 EnemyChessDataManager 注册棋子数据
    /// </summary>
    private void RegisterChessData()
    {
        var enemyTable = GF.DataTable.GetDataTable<EnemyTable>();
        if (enemyTable == null)
        {
            DebugEx.WarningModule("EnemyEntity", "EnemyTable 未加载，跳过棋子数据注册");
            return;
        }

        var enemyData = enemyTable.GetDataRow(m_Config.BattleConfigId);
        if (enemyData == null || enemyData.ChessIds == null || enemyData.ChessIds.Length == 0)
        {
            DebugEx.WarningModule("EnemyEntity",
                $"EnemyTable 中未找到棋子数据: BattleConfigId={m_Config.BattleConfigId}");
            return;
        }

        var chessTable = GF.DataTable.GetDataTable<SummonChessTable>();

        for (int i = 0; i < enemyData.ChessIds.Length; i++)
        {
            int chessId = enemyData.ChessIds[i];
            double maxHp = 100; // 默认值

            if (chessTable != null)
            {
                var chessRow = chessTable.GetDataRow(chessId);
                if (chessRow != null)
                {
                    maxHp = chessRow.MaxHp;
                }
            }

            EnemyChessDataManager.Instance.Register(m_EntityGuid, i, chessId, maxHp);
        }

        DebugEx.LogModule("EnemyEntity",
            $"已注册 {enemyData.ChessIds.Length} 个棋子到 EnemyChessDataManager (guid={m_EntityGuid})");
    }

    /// <summary>
    /// 加载配置数据
    /// </summary>
    private bool LoadConfig()
    {
        var dataTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
        if (dataTable == null)
        {
            DebugEx.ErrorModule("EnemyEntity", "EnemyEntityTable 未加载");
            return false;
        }

        m_Config = dataTable.GetDataRow(m_EntityConfigId);
        if (m_Config == null)
        {
            DebugEx.ErrorModule("EnemyEntity", $"未找到配置: EntityConfigId={m_EntityConfigId}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 配置 NavMeshAgent
    /// </summary>
    private void ConfigureNavAgent()
    {
        if (m_NavAgent == null) return;

        m_NavAgent.speed = m_Config.PatrolSpeed;
        m_NavAgent.angularSpeed = 120f;
        m_NavAgent.acceleration = 8f;
        m_NavAgent.stoppingDistance = 0.5f;
        m_NavAgent.autoBraking = true;
    }

    /// <summary>
    /// 绘制圆形（用于Gizmos）
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #endregion
}
