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

    /// <summary>目标丢失时的宽限期计时器（秒）</summary>
    private float m_TargetGraceTimer;

    /// <summary>目标保持宽限期（秒）— 条件不满足后仍保持目标的时间</summary>
    private const float TARGET_GRACE_PERIOD = 1.0f;

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

        // ===== 诊断日志 =====
        DebugEx.LogModule("CombatOpportunityDetector",
            $"<color=cyan>[诊断] 初始化完成 | " +
            $"EnemyLayerMask={(int)m_EnemyLayerMask} (value={m_EnemyLayerMask.value}) | " +
            $"SneakDist={m_SneakAttackDistance} | EncounterDist={m_EncounterDistance} | " +
            $"BehindAngle={m_BehindAngleThreshold} | FacingAngle={m_PlayerFacingAngleThreshold} | " +
            $"PlayerPos={m_PlayerTransform.position} | " +
            $"GameObject={gameObject.name} Layer={LayerMask.LayerToName(gameObject.layer)}</color>");

        if (m_EnemyLayerMask.value == 0)
        {
            DebugEx.ErrorModule("CombatOpportunityDetector",
                "<color=red>[诊断] ⚠️ EnemyLayerMask 为 0！OverlapSphere 不会检测到任何敌人！" +
                "这是动态 AddComponent 导致 SerializeField 未赋值的问题。</color>");
        }
    }

    /// <summary>诊断日志计时器，每5秒输出一次状态</summary>
    private float m_DiagnosticLogTimer;

    private void Update()
    {
        // 检查是否已初始化
        if (!m_IsInitialized) return;

        // ===== 定期诊断日志（每5秒） =====
        m_DiagnosticLogTimer += Time.deltaTime;
        if (m_DiagnosticLogTimer >= 5f)
        {
            m_DiagnosticLogTimer = 0f;
            var inputMgr = PlayerInputManager.Instance;
            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=yellow>[诊断-周期] " +
                $"Initialized={m_IsInitialized} | InExploration={m_IsInExploration} | " +
                $"CurrentTarget={m_CurrentTarget?.Config?.Name ?? "null"} | " +
                $"TriggerType={m_CurrentTriggerType} | " +
                $"EnemyLayerMask={m_EnemyLayerMask.value} | " +
                $"InputMgr={inputMgr != null} | " +
                $"SpaceKeyDown={inputMgr?.SpaceKeyDown} | " +
                $"GamePauseTestTriggered={inputMgr?.GamePauseTestTriggered}</color>");
        }

        // 仅在探索状态时进行检测
        if (!m_IsInExploration) return;

        // ⭐ 空格键检测必须每帧执行（GetKeyDown 只在按下那一帧为true）
        // 放在频率限制之前，避免被 0.1s 检测间隔吞掉按键
        var inputManager = PlayerInputManager.Instance;
        if (inputManager != null && inputManager.SpaceKeyDown && m_CurrentTarget != null)
        {
            DebugEx.LogModule("CombatOpportunityDetector",
                "<color=green>[诊断] SpaceKeyDown=true 且有目标，触发战斗</color>");
            TriggerCombat();
            return; // 触发后不再做检测
        }

        // 降低物理检测频率（不影响按键响应）
        if (Time.time - m_LastDetectionTime < m_DetectionUpdateInterval)
            return;

        m_LastDetectionTime = Time.time;

        DetectCombatOpportunities();
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

        // 1. 通过CombatTriggerManager处理战斗效果（偷袭Debuff/先手Buff等）
        CombatTriggerManager.Instance.TriggerCombat(m_CurrentTarget, m_CurrentTriggerType);

        // 2. 通过EnemyEntityManager进入战斗状态（纯状态设置，不会再调用CombatTriggerManager）
        EnemyEntityManager.Instance.EnterCombatState(m_CurrentTarget);

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
        DebugEx.LogModule("CombatOpportunityDetector",
            "<color=cyan>[诊断] 收到 ExplorationEnter 事件，m_IsInExploration=true</color>");
    }

    /// <summary>
    /// 事件处理：离开探索
    /// </summary>
    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        m_IsInExploration = false;
        ClearOpportunity();
        DebugEx.LogModule("CombatOpportunityDetector",
            "<color=cyan>[诊断] 收到 ExplorationLeave 事件，m_IsInExploration=false</color>");
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
            m_TargetGraceTimer = 0f; // 有目标时重置宽限计时
            if (m_CurrentTarget != sneakTarget)
            {
                m_CurrentTarget = sneakTarget;
                m_CurrentTriggerType = CombatTriggerType.SneakAttack;
                DebugEx.LogModule("CombatOpportunityDetector",
                    $"<color=lime>[诊断-检测] 检测到新的偷袭目标: {sneakTarget.Config?.Name}, 调用 ShowOpportunityUI</color>");
                ShowOpportunityUI(CombatTriggerType.SneakAttack);
            }
            return;
        }

        // 优先级2：检测遭遇战机会
        if (CheckEncounterOpportunity(out EnemyEntity encounterTarget))
        {
            m_TargetGraceTimer = 0f; // 有目标时重置宽限计时
            if (m_CurrentTarget != encounterTarget)
            {
                m_CurrentTarget = encounterTarget;
                m_CurrentTriggerType = CombatTriggerType.Encounter;
                DebugEx.LogModule("CombatOpportunityDetector",
                    $"<color=lime>[诊断-检测] 检测到新的遭遇战目标: {encounterTarget.Config?.Name}, 调用 ShowOpportunityUI</color>");
                ShowOpportunityUI(CombatTriggerType.Encounter);
            }
            return;
        }

        // 无机会：使用宽限期，防止条件波动导致目标频繁丢失
        if (m_CurrentTarget != null)
        {
            m_TargetGraceTimer += m_DetectionUpdateInterval;
            if (m_TargetGraceTimer >= TARGET_GRACE_PERIOD)
            {
                DebugEx.LogModule("CombatOpportunityDetector",
                    $"<color=yellow>[诊断-检测] 目标丢失(宽限期到期): {m_CurrentTarget.Config?.Name} 不再满足条件</color>");
                ClearOpportunity();
                m_TargetGraceTimer = 0f;
            }
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

        // 诊断：物理检测结果
        if (m_DiagnosticLogTimer < 0.2f) // 每个周期只输出一次
        {
            // 额外用无LayerMask检测做对比
            int hitCountNoMask = Physics.OverlapSphereNonAlloc(
                m_PlayerTransform.position,
                m_SneakAttackDistance,
                new Collider[20]);

            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=yellow>[诊断-偷袭检测] " +
                $"OverlapSphere: 位置={m_PlayerTransform.position}, 半径={m_SneakAttackDistance}, " +
                $"LayerMask={m_EnemyLayerMask.value} → 命中={hitCount} | " +
                $"无LayerMask命中={hitCountNoMask}</color>");
        }

        for (int i = 0; i < hitCount; i++)
        {
            EnemyEntity enemy = m_OverlapResults[i].GetComponent<EnemyEntity>();
            if (enemy == null)
            {
                // 诊断：碰撞体不是敌人
                if (m_DiagnosticLogTimer < 0.2f)
                {
                    DebugEx.LogModule("CombatOpportunityDetector",
                        $"<color=gray>[诊断-偷袭] 碰撞体 {m_OverlapResults[i].name} 不是EnemyEntity</color>");
                }
                continue;
            }

            // 诊断：检查每个敌人的偷袭条件
            if (m_DiagnosticLogTimer < 0.2f)
            {
                DiagnoseSneakAttackConditions(enemy);
            }

            // 检查敌人是否满足条件
            bool isSneakTarget = IsSneakAttackTarget(enemy);
            if (isSneakTarget)
            {
                DebugEx.LogModule("CombatOpportunityDetector",
                    $"<color=lime>[诊断-偷袭检测] 检测到偷袭目标: {enemy.Config?.Name}, 调用 ShowOpportunityUI</color>");
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

        // 诊断：遭遇战物理检测
        if (m_DiagnosticLogTimer < 0.2f)
        {
            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=yellow>[诊断-遭遇检测] " +
                $"OverlapSphere: 半径={m_EncounterDistance}, LayerMask={m_EnemyLayerMask.value} → 命中={hitCount}</color>");
        }

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
        {
            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=gray>[诊断-IsSneakAttackTarget] enemy={enemy?.name ?? "null"}, VisionDetector={enemy?.VisionDetector != null} → false</color>");
            return false;
        }

        // 条件1：敌人未警觉（AlertLevel < 0.3）
        if (enemy.VisionDetector.AlertLevel >= 0.3f)
        {
            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=gray>[诊断-IsSneakAttackTarget] {enemy.Config?.Name}: AlertLevel={enemy.VisionDetector.AlertLevel} >= 0.3 → false</color>");
            return false;
        }

        // 条件2：玩家在敌人背后（Vector3.Angle(敌人forward, 玩家方向) < 60度）
        Vector3 toPlayer = m_PlayerTransform.position - enemy.transform.position;
        toPlayer.y = 0;
        float angleToPlayer = Vector3.Angle(-toPlayer.normalized, enemy.transform.forward);
        if (angleToPlayer > m_BehindAngleThreshold)
        {
            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=gray>[诊断-IsSneakAttackTarget] {enemy.Config?.Name}: 身后角度={angleToPlayer:F1}° > {m_BehindAngleThreshold}° → false</color>");
            return false;
        }

        // 条件3：玩家面向敌人（玩家forward与向敌人方向夹角 < 45度）
        // 注意：toPlayer 是从敌人到玩家的方向，取反得到从玩家到敌人的方向
        float angleFromPlayer = Vector3.Angle(m_PlayerTransform.forward, -toPlayer.normalized);
        if (angleFromPlayer > m_PlayerFacingAngleThreshold)
        {
            DebugEx.LogModule("CombatOpportunityDetector",
                $"<color=gray>[诊断-IsSneakAttackTarget] {enemy.Config?.Name}: 面向角度={angleFromPlayer:F1}° > {m_PlayerFacingAngleThreshold}° → false</color>");
            return false;
        }

        DebugEx.LogModule("CombatOpportunityDetector",
            $"<color=green>[诊断-IsSneakAttackTarget] {enemy.Config?.Name}: ✓ 所有条件都满足 → true</color>");
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
        DebugEx.LogModule("CombatOpportunityDetector",
            $"<color=cyan>[诊断-ShowOpportunityUI] 开始 | triggerType={triggerType}</color>");

        string uiAssetName = GF.UI.GetUIFormAssetName(UIViews.GamePlayInfoUI);
        var uiForm = GF.UI.GetUIForm(uiAssetName);
        if (uiForm == null)
        {
            DebugEx.ErrorModule("CombatOpportunityDetector",
                $"<color=red>[诊断-ShowOpportunityUI] ❌ GamePlayInfoUI 未打开！assetName={uiAssetName}</color>");
            return;
        }

        DebugEx.LogModule("CombatOpportunityDetector",
            $"<color=cyan>[诊断-ShowOpportunityUI] UIForm 找到</color>");

        var gameplayUI = uiForm.Logic as GamePlayInfoUI;
        if (gameplayUI == null)
        {
            DebugEx.ErrorModule("CombatOpportunityDetector",
                $"<color=red>[诊断-ShowOpportunityUI] ❌ UIForm.Logic 不是 GamePlayInfoUI 类型，实际类型={uiForm.Logic?.GetType().Name}</color>");
            return;
        }

        DebugEx.LogModule("CombatOpportunityDetector",
            $"<color=cyan>[诊断-ShowOpportunityUI] ✓ GamePlayInfoUI 获取成功，调用 ShowCombatInteract({triggerType})</color>");

        gameplayUI.ShowCombatInteract(triggerType);
    }

    /// <summary>
    /// 清空战斗机会
    /// </summary>
    private void ClearOpportunity()
    {
        m_CurrentTarget = null;
        m_CurrentTriggerType = CombatTriggerType.Normal;

        string uiAssetName = GF.UI.GetUIFormAssetName(UIViews.GamePlayInfoUI);
        var uiForm = GF.UI.GetUIForm(uiAssetName);
        if (uiForm == null)
        {
            DebugEx.WarningModule("CombatOpportunityDetector",
                "<color=yellow>[诊断] ClearOpportunity: GamePlayInfoUI 未打开</color>");
            return;
        }

        var gameplayUI = uiForm.Logic as GamePlayInfoUI;
        if (gameplayUI == null)
        {
            DebugEx.WarningModule("CombatOpportunityDetector",
                "<color=yellow>[诊断] ClearOpportunity: UIForm.Logic 不是 GamePlayInfoUI</color>");
            return;
        }

        DebugEx.LogModule("CombatOpportunityDetector",
            "<color=cyan>[诊断] 清空战斗机会，隐藏UI</color>");

        gameplayUI.HideCombatInteract();
    }

    /// <summary>
    /// 诊断：输出偷袭条件判断细节
    /// </summary>
    private void DiagnoseSneakAttackConditions(EnemyEntity enemy)
    {
        if (enemy == null) return;

        string enemyName = enemy.Config?.Name ?? enemy.gameObject.name;
        bool hasVision = enemy.VisionDetector != null;
        float alertLevel = hasVision ? enemy.VisionDetector.AlertLevel : -1f;
        float distance = Vector3.Distance(m_PlayerTransform.position, enemy.transform.position);

        // 身后角度检查（与 IsSneakAttackTarget 一致）
        Vector3 toPlayer = m_PlayerTransform.position - enemy.transform.position;
        toPlayer.y = 0;
        float behindAngle = toPlayer.sqrMagnitude > 0.01f
            ? Vector3.Angle(-toPlayer.normalized, enemy.transform.forward)
            : -1f;

        // 玩家面向角度检查（与 IsSneakAttackTarget 一致，取反得到从玩家到敌人方向）
        float facingAngle = toPlayer.sqrMagnitude > 0.01f
            ? Vector3.Angle(m_PlayerTransform.forward, -toPlayer.normalized)
            : -1f;

        bool alertOk = hasVision && alertLevel < 0.3f;
        bool behindOk = behindAngle <= m_BehindAngleThreshold;
        bool facingOk = facingAngle <= m_PlayerFacingAngleThreshold;

        DebugEx.LogModule("CombatOpportunityDetector",
            $"<color=magenta>[诊断-偷袭条件] {enemyName}: " +
            $"距离={distance:F1}m | " +
            $"VisionDetector={hasVision} | " +
            $"AlertLevel={alertLevel:F2}(需<0.3 {(alertOk ? "✓" : "✗")}) | " +
            $"身后角度={behindAngle:F1}°(需<{m_BehindAngleThreshold}° {(behindOk ? "✓" : "✗")}) | " +
            $"面向角度={facingAngle:F1}°(需<{m_PlayerFacingAngleThreshold}° {(facingOk ? "✓" : "✗")}) | " +
            $"敌人Layer={LayerMask.LayerToName(enemy.gameObject.layer)}({enemy.gameObject.layer})" +
            $"</color>");
    }

    #endregion
}
