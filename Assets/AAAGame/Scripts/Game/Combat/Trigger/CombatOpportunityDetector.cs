using UnityEngine;
using GameFramework.Event;

/// <summary>
/// 战斗机会检测器
/// 挂载到玩家身上，检测偷袭和遭遇战机会
/// 优先级：偷袭 > 遭遇战 > 敌人追击
/// </summary>
public class CombatOpportunityDetector : MonoBehaviour
{
    #region 配置参数

    [Header("检测参数")]
    [SerializeField]
    [Tooltip("偷袭检测距离（米）")]
    private float m_SneakAttackDistance = 3f;

    [SerializeField]
    [Tooltip("遭遇战检测距离（米）")]
    private float m_EncounterDistance = 5f;

    [SerializeField]
    [Tooltip("身后判定角度阈值（度）")]
    private float m_BehindAngleThreshold = 60f;

    [SerializeField]
    [Tooltip("玩家面向角度阈值（度）")]
    private float m_PlayerFacingAngleThreshold = 45f;

    [SerializeField]
    [Tooltip("敌人检测的Layer")]
    private LayerMask m_EnemyLayerMask;

    [SerializeField]
    [Tooltip("检测更新频率（秒）")]
    private float m_DetectionUpdateInterval = 0.1f;

    #endregion

    #region 私有字段

    /// <summary>当前目标敌人</summary>
    private EnemyEntity m_CurrentTarget;

    /// <summary>当前触发类型</summary>
    private CombatTriggerType m_CurrentTriggerType;

    /// <summary>上次检测时间</summary>
    private float m_LastDetectionTime;

    /// <summary>用于OverlapSphere的缓存数组</summary>
    private Collider[] m_OverlapResults = new Collider[20];

    /// <summary>玩家Transform缓存</summary>
    private Transform m_PlayerTransform;

    /// <summary>是否是在探索状态</summary>
    private bool m_IsInExploration;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region 属性

    /// <summary>当前目标敌人</summary>
    public EnemyEntity CurrentTarget => m_CurrentTarget;

    /// <summary>当前触发类型</summary>
    public CombatTriggerType CurrentTriggerType => m_CurrentTriggerType;

    /// <summary>是否有可用的战斗机会</summary>
    public bool HasCombatOpportunity => m_CurrentTarget != null;

    #endregion

    #region 初始化方法

    /// <summary>
    /// 初始化战斗机会检测器
    /// 在玩家角色生成后调用（由GameProcedure或相关管理器调用）
    /// </summary>
    public void Initialize()
    {
        if (m_IsInitialized)
        {
            DebugEx.WarningModule("CombatOpportunityDetector", "检测器已初始化，跳过重复初始化");
            return;
        }

        // 缓存玩家Transform
        m_PlayerTransform = transform;
        m_LastDetectionTime = Time.time;

        // 订阅状态变化事件
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);

        m_IsInitialized = true;
        DebugEx.LogModule("CombatOpportunityDetector", "检测器初始化完成");
    }

    private void Update()
    {
        // 检查是否已初始化
        if (!m_IsInitialized) return;

        // 仅在探索状态时进行检测
        if (!m_IsInExploration) return;

        // 降低检测频率
        if (Time.time - m_LastDetectionTime < m_DetectionUpdateInterval)
            return;

        m_LastDetectionTime = Time.time;

        DetectCombatOpportunities();

        // 检测空格键触发战斗
        if (Input.GetKeyDown(KeyCode.Space) && m_CurrentTarget != null)
        {
            TriggerCombat();
        }
    }

    private void OnDestroy()
    {
        if (!m_IsInitialized) return;

        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 触发战斗
    /// </summary>
    public void TriggerCombat()
    {
        if (m_CurrentTarget == null)
        {
            DebugEx.WarningModule("CombatOpportunityDetector", "当前没有目标");
            return;
        }

        // 通过CombatTriggerManager触发战斗
        CombatTriggerManager.Instance.TriggerCombat(m_CurrentTarget, m_CurrentTriggerType);

        DebugEx.LogModule("CombatOpportunityDetector", $"触发战斗: {m_CurrentTarget.Config.Name}, 类型={m_CurrentTriggerType}");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 事件处理：进入探索
    /// </summary>
    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        m_IsInExploration = true;
    }

    /// <summary>
    /// 事件处理：离开探索
    /// </summary>
    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        m_IsInExploration = false;
        ClearOpportunity();
    }

    /// <summary>
    /// 检测战斗机会
    /// 优先级：偷袭 > 遭遇战 > 无机会
    /// </summary>
    private void DetectCombatOpportunities()
    {
        // 优先级1：检测偷袭机会
        if (CheckSneakAttackOpportunity(out EnemyEntity sneakTarget))
        {
            if (m_CurrentTarget != sneakTarget)
            {
                m_CurrentTarget = sneakTarget;
                m_CurrentTriggerType = CombatTriggerType.SneakAttack;
                ShowOpportunityUI(CombatTriggerType.SneakAttack);
            }
            return;
        }

        // 优先级2：检测遭遇战机会
        if (CheckEncounterOpportunity(out EnemyEntity encounterTarget))
        {
            if (m_CurrentTarget != encounterTarget)
            {
                m_CurrentTarget = encounterTarget;
                m_CurrentTriggerType = CombatTriggerType.Encounter;
                ShowOpportunityUI(CombatTriggerType.Encounter);
            }
            return;
        }

        // 无机会：清空
        if (m_CurrentTarget != null)
        {
            ClearOpportunity();
        }
    }

    /// <summary>
    /// 检查偷袭机会
    /// 条件：
    /// - 距离 < 3米
    /// - 玩家在敌人背后
    /// - 敌人未警觉（AlertLevel < 0.3）
    /// - 玩家面向敌人
    /// </summary>
    private bool CheckSneakAttackOpportunity(out EnemyEntity target)
    {
        target = null;

        int hitCount = Physics.OverlapSphereNonAlloc(
            m_PlayerTransform.position,
            m_SneakAttackDistance,
            m_OverlapResults,
            m_EnemyLayerMask
        );

        for (int i = 0; i < hitCount; i++)
        {
            EnemyEntity enemy = m_OverlapResults[i].GetComponent<EnemyEntity>();
            if (enemy == null) continue;

            // 检查敌人是否满足条件
            if (IsSneakAttackTarget(enemy))
            {
                target = enemy;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查遭遇战机会
    /// 条件：
    /// - 距离 < 5米
    /// - 敌人未警觉（AlertLevel < 0.5）
    /// - 不满足偷袭条件（不在背后）
    /// - 玩家面向敌人
    /// </summary>
    private bool CheckEncounterOpportunity(out EnemyEntity target)
    {
        target = null;

        int hitCount = Physics.OverlapSphereNonAlloc(
            m_PlayerTransform.position,
            m_EncounterDistance,
            m_OverlapResults,
            m_EnemyLayerMask
        );

        for (int i = 0; i < hitCount; i++)
        {
            EnemyEntity enemy = m_OverlapResults[i].GetComponent<EnemyEntity>();
            if (enemy == null) continue;

            // 检查敌人是否满足条件
            if (IsEncounterTarget(enemy))
            {
                target = enemy;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否是偷袭目标
    /// </summary>
    private bool IsSneakAttackTarget(EnemyEntity enemy)
    {
        if (enemy == null || enemy.VisionDetector == null)
            return false;

        // 条件1：敌人未警觉（AlertLevel < 0.3）
        if (enemy.VisionDetector.AlertLevel >= 0.3f)
            return false;

        // 条件2：玩家在敌人背后（Vector3.Angle(敌人forward, 玩家方向) < 60度）
        Vector3 toPlayer = m_PlayerTransform.position - enemy.transform.position;
        toPlayer.y = 0;
        float angleToPlayer = Vector3.Angle(-toPlayer.normalized, enemy.transform.forward);
        if (angleToPlayer > m_BehindAngleThreshold)
            return false;

        // 条件3：玩家面向敌人（玩家forward与向敌人方向夹角 < 45度）
        float angleFromPlayer = Vector3.Angle(m_PlayerTransform.forward, toPlayer.normalized);
        if (angleFromPlayer > m_PlayerFacingAngleThreshold)
            return false;

        return true;
    }

    /// <summary>
    /// 判断是否是遭遇战目标
    /// </summary>
    private bool IsEncounterTarget(EnemyEntity enemy)
    {
        if (enemy == null || enemy.VisionDetector == null)
            return false;

        // 条件1：敌人未警觉（AlertLevel < 0.5）
        if (enemy.VisionDetector.AlertLevel >= 0.5f)
            return false;

        // 条件2：不满足偷袭条件（不在背后）
        if (IsSneakAttackTarget(enemy))
            return false;

        // 条件3：玩家面向敌人
        Vector3 toPlayer = enemy.transform.position - m_PlayerTransform.position;
        toPlayer.y = 0;
        float angle = Vector3.Angle(m_PlayerTransform.forward, toPlayer.normalized);
        if (angle > m_PlayerFacingAngleThreshold)
            return false;

        return true;
    }

    /// <summary>
    /// 显示战斗机会UI
    /// </summary>
    private void ShowOpportunityUI(CombatTriggerType triggerType)
    {
        var uiForm = GF.UI.GetUIForm((int)UIViews.GamePlayInfoUI);
        if (uiForm == null) return;

        var gameplayUI = uiForm.Logic as GamePlayInfoUI;
        if (gameplayUI == null) return;

        gameplayUI.ShowCombatInteract(triggerType);
    }

    /// <summary>
    /// 清空战斗机会
    /// </summary>
    private void ClearOpportunity()
    {
        m_CurrentTarget = null;
        m_CurrentTriggerType = CombatTriggerType.Normal;

        var uiForm = GF.UI.GetUIForm((int)UIViews.GamePlayInfoUI);
        if (uiForm == null) return;

        var gameplayUI = uiForm.Logic as GamePlayInfoUI;
        if (gameplayUI == null) return;

        gameplayUI.HideCombatInteract();
    }

    #endregion
}
