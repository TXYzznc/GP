using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 战斗准备状态 - 显示准备UI，等待玩家确认后切换到战斗状态
/// </summary>
public class CombatPreparationState : FsmState<InGameState>
{
    #region 字段

    private IFsm<InGameState> m_Fsm;
    private bool m_IsArenaSpawned;
    private int m_CombatPreparationUIFormId;  // 缓存 CombatPreparationUI 的序列号
    // ⭐ 缓存战斗前的视角模式（新增，实际上不需要这个字段，因为在 ThirdPersonCamera 中已经缓存）

    #endregion

    #region FSM 生命周期

    protected override void OnInit(IFsm<InGameState> fsm)
    {
        base.OnInit(fsm);
        DebugEx.LogModule("CombatPreparationState", "初始化");
    }

    protected override void OnEnter(IFsm<InGameState> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.LogModule("CombatPreparationState", "进入战斗准备状态");

        m_Fsm = fsm;

        // ⭐ 记录玩家位置（战斗前）
        if (PlayerCharacterManager.Instance != null)
        {
            PlayerCharacterManager.Instance.RecordPositionBeforeCombat();
        }

        // ⭐ 先显示战斗进入提示，等待提示完成后再继续初始化
        ShowEnterCombatTipAndContinue().Forget();
    }

    /// <summary>
    /// 显示战斗进入提示并等待完成后继续初始化
    /// </summary>
    private async UniTaskVoid ShowEnterCombatTipAndContinue()
    {
        DebugEx.LogModule("CombatPreparationState", "显示战斗进入提示...");

        try
        {
            // 显示提示UI
            var uiParams = UIParams.Create();
            var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
            float displayDurationSeconds =
                ruleRow != null ? ruleRow.EnterCombatTipDisplayDurationSeconds : 0.5f;
            uiParams.Set<VarFloat>("DisplayDuration", displayDurationSeconds);
            var uiFormId = GF.UI.OpenUIForm(UIViews.EnterCombatTip, uiParams);

            if (uiFormId <= 0)
            {
                DebugEx.Error("CombatPreparationState", "战斗进入提示UI打开失败，直接继续初始化");
                await ContinueCombatPreparationInitAsync();
                return;
            }

            DebugEx.LogModule("CombatPreparationState", $"战斗进入提示UI已打开 (ID: {uiFormId})，等待UI关闭...");

            // 使用事件监听机制等待UI关闭，避免轮询延迟
            await WaitForUIFormClosedByEvent(uiFormId);

            DebugEx.Success("CombatPreparationState", "战斗进入提示已完成，开始战斗准备初始化");

            // 继续战斗准备的初始化流程
            await ContinueCombatPreparationInitAsync();
        }
        catch (System.Exception ex)
        {
            DebugEx.Error("CombatPreparationState", $"战斗进入提示流程异常: {ex.Message}");
            // 异常情况下也要继续初始化，确保游戏流程不中断
            await ContinueCombatPreparationInitAsync();
        }
    }

    /// <summary>
    /// 等待UI窗体完全关闭
    /// </summary>
    /// <summary>
    /// 使用事件监听机制等待UI窗体关闭（避免轮询延迟）
    /// </summary>
    /// <param name="uiFormId">UI窗体ID</param>
    private async UniTask WaitForUIFormClosedByEvent(int uiFormId)
    {
        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        float timeoutSeconds = ruleRow != null ? ruleRow.EnterCombatTipCloseTimeoutSeconds : 5f;
        
        DebugEx.LogModule("CombatPreparationState", $"开始监听UI窗体关闭事件，ID: {uiFormId}");

        var startTime = Time.unscaledTime;
        bool uiClosed = false;
        
        // 创建事件监听器 - 使用正确的事件参数类型
        System.EventHandler<GameFramework.Event.GameEventArgs> onUIFormClosed = null;
        onUIFormClosed = (sender, e) =>
        {
            if (e is UnityGameFramework.Runtime.CloseUIFormCompleteEventArgs closeArgs)
            {
                if (closeArgs.SerialId == uiFormId)
                {
                    DebugEx.Success("CombatPreparationState", $"UI窗体关闭事件触发，ID: {uiFormId}，耗时: {Time.unscaledTime - startTime:F3}秒");
                    uiClosed = true;
                    // 移除事件监听
                    GF.Event.Unsubscribe(UnityGameFramework.Runtime.CloseUIFormCompleteEventArgs.EventId, onUIFormClosed);
                }
            }
        };

        // 订阅UI关闭完成事件
        GF.Event.Subscribe(UnityGameFramework.Runtime.CloseUIFormCompleteEventArgs.EventId, onUIFormClosed);

        // 等待UI关闭或超时
        while (!uiClosed && (Time.unscaledTime - startTime) < timeoutSeconds)
        {
            await UniTask.Yield();
        }

        // 超时处理
        if (!uiClosed)
        {
            DebugEx.Warning("CombatPreparationState", $"等待UI窗体关闭超时 ({timeoutSeconds}秒)，强制继续流程");
            
            // 移除事件监听
            GF.Event.Unsubscribe(UnityGameFramework.Runtime.CloseUIFormCompleteEventArgs.EventId, onUIFormClosed);
            
            // 尝试强制关闭UI
            try
            {
                if (GF.UI.HasUIForm(uiFormId))
                {
                    GF.UI.CloseUIForm(uiFormId);
                    DebugEx.LogModule("CombatPreparationState", "已强制关闭UI窗体");
                }
            }
            catch (System.Exception ex)
            {
                DebugEx.Error("CombatPreparationState", $"强制关闭UI窗体失败: {ex.Message}");
            }
        }

        // 额外等待一帧，确保所有清理工作完成
        await UniTask.Yield();
    }

    /// <summary>
    /// 原有的轮询等待方法（保留作为备用）
    /// </summary>
    /// <param name="uiFormId">UI窗体ID</param>
    private async UniTask WaitForUIFormClosed(int uiFormId)
    {
        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        float timeoutSeconds = ruleRow != null ? ruleRow.EnterCombatTipCloseTimeoutSeconds : 5f;
        float elapsedTime = 0f;

        DebugEx.LogModule("CombatPreparationState", $"开始等待UI窗体关闭，ID: {uiFormId}");

        // 等待UI窗体关闭，带超时保护
        while (GF.UI.HasUIForm(uiFormId) && elapsedTime < timeoutSeconds)
        {
            await UniTask.Yield();
            elapsedTime += Time.unscaledDeltaTime;
        }

        if (elapsedTime >= timeoutSeconds)
        {
            DebugEx.Warning(
                "CombatPreparationState",
                $"等待UI窗体关闭超时 ({timeoutSeconds}秒)，强制继续流程"
            );

            // 尝试强制关闭UI
            try
            {
                if (GF.UI.HasUIForm(uiFormId))
                {
                    GF.UI.CloseUIForm(uiFormId);
                    DebugEx.LogModule("CombatPreparationState", "已强制关闭UI窗体");
                }
            }
            catch (System.Exception ex)
            {
                DebugEx.Error("CombatPreparationState", $"强制关闭UI窗体失败: {ex.Message}");
            }
        }
        else
        {
            DebugEx.Success(
                "CombatPreparationState",
                $"UI窗体已正常关闭，耗时: {elapsedTime:F2}秒"
            );
        }

        // 额外等待一帧，确保所有清理工作完成
        await UniTask.Yield();
    }

    /// <summary>
    /// 继续战斗准备的初始化流程（原OnEnter中的后续代码）
    /// </summary>
    private async UniTask ContinueCombatPreparationInitAsync()
    {
        DebugEx.LogModule("CombatPreparationState", "开始战斗准备初始化流程");

        // ⭐ 缓存当前视角模式并切换到战斗视角
        SetupCombatCamera();

        // ⭐ 先销毁敌人实体（在加载战斗场景之前，确保视觉上先消失）
        DebugEx.LogModule("CombatPreparationState", "销毁敌人实体...");
        EnemyEntityManager.Instance?.DestroyCurrentCombatEnemy();

        // 确保CombatTickDriver存在
        var updater = CombatTickDriver.Instance;

        // ⭐ 获取或创建棋子管理器（单例懒加载，不存在则自动创建）
        CombatEntityTracker.Instance.Clear();
        DebugEx.LogModule("CombatPreparationState", "CombatEntityTracker 已准备就绪");

        // 确保 SummonChessManager 存在
        if (SummonChessManager.Instance == null)
        {
            GameObject managerObj = new GameObject("SummonChessManager");
            managerObj.AddComponent<SummonChessManager>();
            DebugEx.LogModule("CombatPreparationState", "已创建 SummonChessManager");
        }

        // ⭐ 注意：棋子库存已在 InGameState.OnEnter() 时初始化，这里不需要重复初始化
        // ChessDeploymentTracker.Instance.Initialize(); // 已移除
        DebugEx.LogModule("CombatPreparationState", "使用已初始化的棋子库存");

        // 初始化战斗管理器
        InitializeCombatManagers();

        // ⭐ 预热棋子状态UI对象池（战斗准备阶段显式指定预热数量）
        ChessStateUIWorldManager.EnsureExistsAsync(10).Forget();

        // 启用玩家控制器（战斗准备阶段允许移动）
        EnablePlayerController();

        // 禁用玩家技能（战斗准备阶段不使用技能）
        DisablePlayerSkillManager();

        // 解锁光标（战斗准备需要拖拽UI和点击棋子）
        UnlockCursor();

        // 初始化本场战斗临时数据
        InitializeCombatSessionData();

        // 生成战斗场地
        SpawnBattleArena();

        // 启用棋子放置系统
        EnablePlacementSystem();

        // 订阅准备完成事件
        GF.Event.Subscribe(CombatPreparationReadyEventArgs.EventId, OnPreparationReady);

        // 打开战斗准备UI
        m_CombatPreparationUIFormId = GF.UI.OpenUIForm(UIViews.CombatPreparationUI);
        DebugEx.LogModule("CombatPreparationState", $"已打开战斗准备UI (序列号: {m_CombatPreparationUIFormId})");

        // 阻塞等待UI完全初始化（GF框架打开UI是异步的，必须等待UI加载完成）
        float waitTime = 0f;
        while (!GF.UI.HasUIForm(m_CombatPreparationUIFormId))
        {
            await UniTask.Delay(50);
            waitTime += 0.05f;
        }

        DebugEx.LogModule("CombatPreparationState", $"CombatPreparationUI已初始化，耗时{waitTime:F2}秒");

        // 在下一帧检查是否敌方先手，如果是则显示敌方先手Buff通知
        // （确保UI已经初始化完成）
        await ShowEnemyInitiativeBuffIfNeededAsync();
    }

    /// <summary>
    /// 异步方法：在下一帧检查并显示敌方先手Buff通知
    /// </summary>
    private async UniTask ShowEnemyInitiativeBuffIfNeededAsync()
    {
        // 等待一帧，确保UI已经初始化完成
        await UniTask.Yield();

        DebugEx.LogModule("CombatPreparationState", "检查敌方先手效果...");

        // 通过 CombatTriggerEvents.LastEnemyInitiativeEffectId 读取（解耦自 CombatTriggerManager）
        int effectId = CombatTriggerEvents.LastEnemyInitiativeEffectId;
        DebugEx.LogModule("CombatPreparationState", $"LastEnemyInitiativeEffectId: {effectId}");

        if (effectId > 0)
        {
            DebugEx.LogModule("CombatPreparationState", "检测到敌方先手，获取CombatPreparationUI...");

            var uiForm = GF.UI.GetUIForm(m_CombatPreparationUIFormId);
            DebugEx.LogModule("CombatPreparationState", $"UIForm: {(uiForm != null ? "存在" : "为null")}");

            if (uiForm != null)
            {
                var preparationUI = uiForm.Logic as CombatPreparationUI;
                DebugEx.LogModule("CombatPreparationState", $"preparationUI: {(preparationUI != null ? "存在" : "为null")}");

                if (preparationUI != null)
                {
                    DebugEx.LogModule("CombatPreparationState", $"调用ShowEnemyInitiativeBuffNotification: {effectId}");
                    preparationUI.ShowEnemyInitiativeBuffNotification(effectId);
                    DebugEx.LogModule("CombatPreparationState", $"显示敌方先手Buff通知: {effectId}");
                }
            }
        }
    }

    protected override void OnLeave(IFsm<InGameState> fsm, bool isShutdown)
    {
        DebugEx.LogModule("CombatPreparationState", "离开战斗准备状态");

        // 禁用放置系统
        DisablePlacementSystem();

        // 禁用选择系统
        if (ChessSelectionManager.Instance != null)
        {
            ChessSelectionManager.Instance.Disable();
        }

        // 取消订阅
        GF.Event.Unsubscribe(CombatPreparationReadyEventArgs.EventId, OnPreparationReady);

        m_Fsm = null;

        base.OnLeave(fsm, isShutdown);
    }

    protected override void OnDestroy(IFsm<InGameState> fsm)
    {
        DebugEx.LogModule("CombatPreparationState", "销毁");
        base.OnDestroy(fsm);
    }

    #endregion

    #region 相机管理

    /// <summary>
    /// 设置战斗相机
    /// </summary>
    private void SetupCombatCamera()
    {
        ThirdPersonCamera cameraController = CameraRegistry.ThirdPersonCamera;
        if (cameraController == null)
        {
            DebugEx.ErrorModule("CombatPreparationState", "未找到第三人称相机控制器");
            return;
        }

        // 缓存当前视角模式
        cameraController.CacheCurrentViewMode();
        DebugEx.LogModule("CombatPreparationState", "已缓存战斗前的视角模式");

        // 切换到战斗视角
        cameraController.SetViewMode(CameraViewMode.Combat);
        DebugEx.LogModule("CombatPreparationState", "已切换到战斗视角");

        // 锁定视角切换
        cameraController.SetViewModeLocked(true);
        DebugEx.LogModule("CombatPreparationState", "已锁定视角切换");

        // ⭐ 同时锁定 PlayerInputManager 的视角模式（新增）
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetViewMode(CameraViewMode.Combat);
            DebugEx.LogModule("CombatPreparationState", "已设置 PlayerInputManager 视角为 Combat");
        }
    }

    /// <summary>
    /// 开始相机平滑移动到 CameraAnchor
    /// </summary>
    private void StartCameraSmoothMove()
    {
        DebugEx.LogModule("CombatPreparationState", "开始相机平滑移动...");

        // 获取战斗场地
        var arena = BattleArenaManager.Instance?.CurrentArena;
        if (arena == null)
        {
            DebugEx.WarningModule("CombatPreparationState", "战斗场地不存在，跳过相机移动");
            return;
        }

        // 查找 CameraAnchor
        Transform cameraAnchor = arena.transform.Find("CameraAnchor");
        if (cameraAnchor == null)
        {
            DebugEx.WarningModule("CombatPreparationState", "未找到 CameraAnchor，跳过相机移动");
            return;
        }

        // 获取相机控制器
        ThirdPersonCamera cameraController = CameraRegistry.ThirdPersonCamera;
        if (cameraController == null)
        {
            DebugEx.ErrorModule("CombatPreparationState", "未找到第三人称相机控制器");
            return;
        }

        // 开始平滑移动（0.8秒）
        cameraController.SmoothMoveTo(cameraAnchor.position, cameraAnchor.rotation, 0.8f);

        // ⭐ 设置相机 FOV 为配置值
        var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
        float cameraView = ruleRow != null ? ruleRow.CameraView : 35f;
        cameraController.SetOverrideFOV(cameraView);

        DebugEx.LogModule(
            "CombatPreparationState",
            $"相机开始移动到 CameraAnchor: Pos={cameraAnchor.position}, Rot={cameraAnchor.rotation.eulerAngles}, FOV={cameraView}"
        );
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 准备完成事件回调 - 切换到战斗状态
    /// </summary>
    private void OnPreparationReady(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("CombatPreparationState", "收到准备完成事件，切换到战斗状态");

        // 通过 InGameState 切换到战斗状态
        if (m_Fsm != null)
        {
            // 获取 InGameState 实例并调用切换方法
            var inGameState = m_Fsm.Owner;
            if (inGameState != null)
            {
                inGameState.SwitchToCombat();
            }
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 初始化战斗管理器
    /// </summary>
    private void InitializeCombatManagers()
    {
        // 初始化战斗场地管理器
        BattleArenaManager.Instance.Initialize(ResourceIds.PREFAB_BATTLE_ARENA); // TODO: 从配置表获取ResourceId

        // 初始化棋子放置管理器
        ChessPlacementManager.Instance.Initialize();

        // 初始化棋子选择管理器
        ChessSelectionManager.Instance.Initialize();
        ChessSelectionManager.Instance.Enable(); // 启用选择系统，允许玩家点击查看棋子

        DebugEx.LogModule("CombatPreparationState", "战斗管理器初始化完成");
    }

    /// <summary>
    /// 禁用玩家控制器
    /// </summary>
    private void DisablePlayerController()
    {
        if (PlayerCharacterManager.Instance != null)
        {
            GameObject playerCharacter = PlayerCharacterManager.Instance.CurrentPlayerCharacter;
            if (playerCharacter != null)
            {
                PlayerController controller = playerCharacter.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.enabled = false;
                    DebugEx.LogModule("CombatPreparationState", "PlayerController 已禁用");
                }
            }
        }
    }

    /// <summary>
    /// 解锁光标
    /// </summary>
    private void UnlockCursor()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorLock(false);
            DebugEx.LogModule("CombatPreparationState", "光标已解锁");
        }
    }

    /// <summary>
    /// 初始化本场战斗临时数据
    /// </summary>
    private void InitializeCombatSessionData()
    {
        // 从PlayerDataTable获取当前等级的统御值上限
        int maxDomination = 3; // 默认值
        var playerData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (playerData != null)
        {
            var dataTable = GF.DataTable.GetDataTable<PlayerDataTable>();
            if (dataTable != null)
            {
                var row = dataTable.GetDataRow(r => r.Level == playerData.GlobalLevel);
                if (row != null)
                {
                    maxDomination = row.MaxDomination;
                }
            }
        }

        CombatSessionData.Instance.Initialize(maxDomination);
    }

    /// <summary>
    /// 生成战斗场地（带溶解过渡）
    /// </summary>
    private async void SpawnBattleArena()
    {
        if (BattleArenaManager.Instance != null)
        {
            // 在玩家当前位置附近生成战斗场地
            Vector3 spawnPos = Vector3.zero;
            if (
                PlayerCharacterManager.Instance != null
                && PlayerCharacterManager.Instance.CurrentPlayerCharacter != null
            )
            {
                await BattleArenaManager.Instance.SpawnArenaAsync(
                    PlayerCharacterManager.Instance.CurrentPlayerCharacter.transform
                );
            }

            // ⭐ 先开始移动相机（不等待，与场景加载并行）
            StartCameraSmoothMove();

            // 播放溶解过渡效果（从探索场景显示战斗场地）
            var arena = BattleArenaManager.Instance.CurrentArena;
            if (arena != null)
            {
                await DissolveTransitionManager.Instance.TransitionToBattle(arena);
            }

            // 加载敌人波次配置（使用保存的战斗数据）
            LoadEnemyWaveConfig();
        }
        else
        {
            DebugEx.WarningModule("CombatPreparationState", "BattleArenaManager 未初始化");
        }
    }

    /// <summary>
    /// 加载敌人波次配置
    /// </summary>
    private void LoadEnemyWaveConfig()
    {
        var enemyManager = EnemyEntityManager.Instance;
        if (enemyManager == null || enemyManager.CurrentCombatData == null)
        {
            DebugEx.WarningModule("CombatPreparationState", "未找到战斗数据，使用默认配置");
            EnemySpawnManager.Instance.LoadWaveFromConfig(1);
            return;
        }

        var combatData = enemyManager.CurrentCombatData;
        string enemyGuid = enemyManager.CurrentCombatEnemyGuid;

        if (combatData.IsGroupCombat)
        {
            // 群体战斗：生成多波敌人
            LoadGroupCombatConfig(combatData);
        }
        else
        {
            // 单敌人战斗：生成单波敌人（使用 LoadFromEnemyTable 保留槽位顺序，支持 HP 继承）
            LoadSingleCombatConfig(combatData.EnemyDataList[0], enemyGuid);
        }
    }

    /// <summary>
    /// 加载单敌人战斗配置
    /// </summary>
    private void LoadSingleCombatConfig(SingleEnemyData enemyData, string enemyGuid = null)
    {
        // 使用 LoadFromEnemyTable 保留槽位顺序，支持跨战斗 HP 继承
        EnemySpawnManager.Instance.LoadFromEnemyTable(enemyData.BattleConfigId, enemyGuid);

        DebugEx.LogModule(
            "CombatPreparationState",
            $"单敌人战斗: {enemyData.EnemyName}, BattleConfigId={enemyData.BattleConfigId}"
        );
    }

    /// <summary>
    /// 加载群体战斗配置
    /// </summary>
    private void LoadGroupCombatConfig(EnemyCombatData combatData)
    {
        // TODO: 实现多波次战斗
        // 当前先生成第一波（触发者）
        var firstEnemy = combatData.EnemyDataList[0];

        // 群体战斗：固定最大人数
        List<int> selectedChessIds = new List<int>();
        for (int i = 0; i < firstEnemy.MaxPopulation; i++)
        {
            int randomIndex = Random.Range(0, firstEnemy.ChessIds.Length);
            selectedChessIds.Add(firstEnemy.ChessIds[randomIndex]);
        }

        EnemySpawnManager.Instance.SetEnemyData(selectedChessIds);

        DebugEx.LogModule(
            "CombatPreparationState",
            $"群体战斗: 第一波={firstEnemy.EnemyName}, 出战人数={firstEnemy.MaxPopulation}, 总敌人数={combatData.EnemyDataList.Count}"
        );
    }

    /// <summary>
    /// 启用棋子放置系统
    /// </summary>
    private void EnablePlacementSystem()
    {
        if (ChessPlacementManager.Instance != null)
        {
            ChessPlacementManager.Instance.Enable();
        }
    }

    /// <summary>
    /// 禁用棋子放置系统
    /// </summary>
    private void DisablePlacementSystem()
    {
        if (ChessPlacementManager.Instance != null)
        {
            ChessPlacementManager.Instance.Disable();
        }
    }

    /// <summary>
    /// 禁用玩家技能管理器
    /// </summary>
    private void DisablePlayerSkillManager()
    {
        var skillManager = Object.FindObjectOfType<PlayerSkillManager>();
        if (skillManager != null)
        {
            skillManager.enabled = false;
            DebugEx.LogModule("CombatPreparationState", "PlayerSkillManager 已禁用");
        }
    }

    /// <summary>
    /// 启用玩家控制器
    /// </summary>
    private void EnablePlayerController()
    {
        if (PlayerCharacterManager.Instance != null)
        {
            GameObject playerCharacter = PlayerCharacterManager.Instance.CurrentPlayerCharacter;
            if (playerCharacter != null)
            {
                PlayerController controller = playerCharacter.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.enabled = true;
                    DebugEx.LogModule("CombatPreparationState", "PlayerController 已启用");
                }
            }
        }
    }

    /// <summary>
    /// 将相机移动到战斗场地的 CameraAnchor 位置
    /// </summary>
    private void MoveCameraToCameraAnchor()
    {
        DebugEx.LogModule("CombatPreparationState", "开始移动相机到 CameraAnchor...");

        // 获取战斗场地
        var arena = BattleArenaManager.Instance?.CurrentArena;
        if (arena == null)
        {
            DebugEx.WarningModule("CombatPreparationState", "战斗场地不存在，跳过相机移动");
            return;
        }

        // 查找 CameraAnchor
        Transform cameraAnchor = arena.transform.Find("CameraAnchor");
        if (cameraAnchor == null)
        {
            DebugEx.WarningModule("CombatPreparationState", "未找到 CameraAnchor，跳过相机移动");
            return;
        }

        // ⭐ 通过 CameraRegistry 获取第三人称相机控制器
        ThirdPersonCamera cameraController = CameraRegistry.ThirdPersonCamera;
        if (cameraController == null)
        {
            DebugEx.ErrorModule(
                "CombatPreparationState",
                "未找到第三人称相机控制器（CameraRegistry.ThirdPersonCamera 为空）"
            );
            return;
        }

        DebugEx.LogModule("CombatPreparationState", $"找到相机控制器: {cameraController.name}");

        // ⭐ 禁用相机控制器，防止它在 LateUpdate 中覆盖我们的设置
        cameraController.enabled = false;
        DebugEx.LogModule("CombatPreparationState", "已禁用相机控制器");

        // 移动相机控制器的 GameObject 到锚点位置（世界坐标）
        cameraController.transform.position = cameraAnchor.position;
        cameraController.transform.rotation = cameraAnchor.rotation;

        DebugEx.LogModule(
            "CombatPreparationState",
            $"相机已移动到 CameraAnchor: Pos={cameraAnchor.position}, Rot={cameraAnchor.rotation.eulerAngles}"
        );
    }

    #endregion
}
