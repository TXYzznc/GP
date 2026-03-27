using GameFramework.Fsm;
using UnityGameFramework.Runtime;
using GameFramework;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 战斗状态 - 自动战斗
/// </summary>
public class CombatState : FsmState<InGameState>
{
    #region FSM 生命周期

    protected override void OnInit(IFsm<InGameState> fsm)
    {
        base.OnInit(fsm);
        DebugEx.LogModule("CombatState", "初始化");
    }

    protected override async void OnEnter(IFsm<InGameState> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.LogModule("CombatState", "进入战斗状态");

        // 初始化召唤师运行时数据管理器
        SummonerRuntimeDataManager.Instance.Initialize();

        // ⭐ 检查棋子管理器（应该在战斗准备阶段已创建）
        if (CombatEntityTracker.Instance == null)
        {
            DebugEx.WarningModule("CombatState",
                "CombatEntityTracker 不存在！这不应该发生，尝试创建...");

            GameObject managerObj = new GameObject("CombatEntityTracker");
            managerObj.AddComponent<CombatEntityTracker>();
        }
        else
        {
            DebugEx.LogModule("CombatState",
                $"CombatEntityTracker 已存在，当前已注册棋子数量: " +
                $"阵营0={CombatEntityTracker.Instance.GetAllies(0)?.Count ?? 0}, " +
                $"阵营1={CombatEntityTracker.Instance.GetAllies(1)?.Count ?? 0}");
        }

        // 禁用玩家控制器
        //DisablePlayerController();
        // 启用玩家控制器（战斗阶段允许移动）
        EnablePlayerController();

        // 禁用玩家技能（战斗阶段不使用技能）
        DisablePlayerSkillManager();

        // 解锁光标（战斗阶段需要点选棋子和指挥移动目标）
        UnlockCursor();

        // 启用选择系统
        EnableSelectionSystem();

        // 启用鼠标位置预览（仅预览模式）
        EnableMousePreview();

        // 启用战斗管理器
        EnableCombatManager();

        await ChessStateUIWorldManager.EnsureExistsAsync();
        await ChessStateUIWorldManager.Instance.EnterCombatAsync();

        // 触发战斗进入事件
        GF.Event.Fire(this, ReferencePool.Acquire<CombatEnterEventArgs>());

        // 订阅战斗结束事件
        GF.Event.Subscribe(CombatEndEventArgs.EventId, OnCombatEnd);

        // 生成敌人并等待完成，然后启用所有棋子的战斗AI
        await SpawnEnemiesAndEnableAI();

        // 初始化战斗特效管理器（异步等待完成）
        InitializeCombatVFXAsync().Forget();
    }

    /// <summary>
    /// 异步初始化战斗特效并开始战斗
    /// </summary>
    private async UniTaskVoid InitializeCombatVFXAsync()
    {
        await CombatVFXManager.InitializeAndWaitAsync();
        CombatVFXUpdater.EnsureExists();
        DebugEx.LogModule("CombatState", "战斗特效管理器已初始化");

        // 初始化完成后再开始战斗
        StartCombat();
    }

    protected override async void OnLeave(IFsm<InGameState> fsm, bool isShutdown)
    {
        DebugEx.LogModule("CombatState", "离开战斗状态");

        // 取消订阅战斗结束事件
        GF.Event.Unsubscribe(CombatEndEventArgs.EventId, OnCombatEnd);

        if (ChessStateUIWorldManager.HasInstance)
        {
            ChessStateUIWorldManager.Instance.LeaveCombat();
        }

        // 1. 禁用 PlayerController（准备过渡）
        DisablePlayerController();

        // 2. 禁用所有棋子的战斗AI
        DisableAllChessCombatAI();

        // 3. 销毁所有棋子的 GameObject 实例
        DestroyAllChessInstances();

        // 4. 清空 CombatEntityTracker 的注册表
        if (CombatEntityTracker.Instance != null)
        {
            CombatEntityTracker.Instance.Clear();
            DebugEx.LogModule("CombatState", "CombatEntityTracker 已清空");
        }

        // 5. 重置玩家棋子的出战状态
        if (ChessDeploymentTracker.Instance != null)
        {
            ChessDeploymentTracker.Instance.OnBattleEnd();
            DebugEx.LogModule("CombatState", "玩家棋子出战状态已重置");
        }

        // 6. 禁用选择系统
        DisableSelectionSystem();

        // 7. 禁用鼠标位置预览
        DisableMousePreview();

        // 8. 禁用战斗管理器
        DisableCombatManager();

        // ⭐ 9. 恢复玩家位置（在溶解前）
        if (PlayerCharacterManager.Instance != null)
        {
            PlayerCharacterManager.Instance.RestorePositionAfterCombat();
            DebugEx.LogModule("CombatState", "玩家位置已恢复");
        }

        // ⭐ 10. 播放溶解过渡并等待完成
        DebugEx.LogModule("CombatState", "开始溶解过渡...");
        await DissolveTransitionManager.Instance.TransitionToExploration();
        DebugEx.LogModule("CombatState", "溶解过渡完成");

        // ⭐ 11. 解锁视角并恢复视角模式
        ThirdPersonCamera cameraController = CameraRegistry.ThirdPersonCamera;
        if (cameraController != null)
        {
            cameraController.SetViewModeLocked(false);
            cameraController.ClearOverrideFOV(); // 清除FOV覆盖
            DebugEx.LogModule("CombatState", "已解锁视角切换并清除FOV覆盖");

            cameraController.RestoreCachedViewMode();
            DebugEx.LogModule("CombatState", $"已恢复相机视角为 {cameraController.GetViewMode()}");

            // ⭐ 如果是第三人称视角，同步相机 Yaw 到玩家旋转
            if (cameraController.GetViewMode() == CameraViewMode.ThirdPerson)
            {
                cameraController.SyncYawToTarget();
                DebugEx.LogModule("CombatState", "已同步第三人称相机 Yaw 到玩家旋转");
            }
        }

        // ⭐ 12. 同步 PlayerInputManager 的视角模式
        if (PlayerInputManager.Instance != null && cameraController != null)
        {
            CameraViewMode restoredMode = cameraController.GetViewMode();
            PlayerInputManager.Instance.SetViewMode(restoredMode);
            DebugEx.LogModule("CombatState", $"已同步 PlayerInputManager 视角为 {restoredMode}");
        }

        // ⭐ 13. 启用 PlayerController
        EnablePlayerController();

        // 14. 销毁战斗场地
        DestroyBattleArenaImmediate();

        // 15. 清理战斗特效管理器
        CleanupCombatVFX();

        // 16. 清理战斗管理器
        CleanupCombatManagers();

        // 17. 清理召唤师运行时数据管理器
        SummonerRuntimeDataManager.Instance.Cleanup();

        // 18. 清理临时数据
        CombatSessionData.Instance.Clear();

        // 19. 触发战斗离开事件
        GF.Event.Fire(this, ReferencePool.Acquire<CombatLeaveEventArgs>());

        base.OnLeave(fsm, isShutdown);
    }

    protected override void OnDestroy(IFsm<InGameState> fsm)
    {
        DebugEx.LogModule("CombatState", "销毁");
        base.OnDestroy(fsm);
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 战斗结束事件回调
    /// </summary>
    private void OnCombatEnd(object sender, GameFramework.Event.GameEventArgs e)
    {
        CombatEndEventArgs args = (CombatEndEventArgs)e;

        // 通过 IsVictory 判断玩家是否胜利
        // IsVictory 表示本地玩家阵营是否胜利
        bool playerWin = args.IsVictory;

        DebugEx.LogModule("CombatState",
            $"战斗结束: 玩家{(playerWin ? "胜利" : "失败")}");

        // 如果战斗失败，增加一半污染值
        if (!playerWin)
        {
            if (PlayerRuntimeDataManager.Instance.IsInitialized)
            {
                var ruleRow = DataTableExtension.GetRowById<CombatRuleTable>(1);
                float defeatCorruptionAdd = ruleRow != null ? ruleRow.DefeatCorruptionAdd : 50f;
                PlayerRuntimeDataManager.Instance.AddCorruption(defeatCorruptionAdd);
            }
        }

        // 通知敌人管理器
        if (EnemyEntityManager.Instance != null)
        {
            EnemyEntityManager.Instance.OnCombatEnd(playerWin);
        }
    }

    #endregion

    #region 玩家控制器管理

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
                    DebugEx.LogModule("CombatState", "PlayerController 已禁用");
                }
            }
        }
    }

    #endregion

    #region 战斗管理器管理

    /// <summary>
    /// 启用战斗管理器
    /// </summary>
    private void EnableCombatManager()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.enabled = true;
            DebugEx.LogModule("CombatState", "CombatManager 已启用");
        }
        else
        {
            DebugEx.WarningModule("CombatState", "CombatManager 未初始化");
        }
    }

    /// <summary>
    /// 禁用战斗管理器
    /// </summary>
    private void DisableCombatManager()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.enabled = false;
            DebugEx.LogModule("CombatState", "CombatManager 已禁用");
        }
    }

    #endregion

    #region 战斗逻辑

    /// <summary>
    /// 开始战斗
    /// </summary>
    private void StartCombat()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.StartCombat();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 通知测试管理器（如果存在），用于自动化测试或自动战斗
            GameTestManager.Instance?.OnCombatStarted();
#endif
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 清理战斗管理器
    /// </summary>
    private void CleanupCombatManagers()
    {
        ChessPlacementManager.Instance.Cleanup();
        ChessSelectionManager.Instance.Cleanup();
        BattleArenaManager.Instance.Cleanup();
        // 清理敌人管理器
        EnemySpawnManager.Instance.Cleanup();

        DebugEx.LogModule("CombatState", "战斗管理器已清理");
    }

    /// <summary>
    /// 解锁光标
    /// </summary>
    private void UnlockCursor()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorLock(false);
            DebugEx.LogModule("CombatState", "光标已解锁");
        }
    }

    /// <summary>
    /// 启用选择系统（仅选择模式）
    /// </summary>
    private void EnableSelectionSystem()
    {
        if (ChessSelectionManager.Instance != null)
        {
            ChessSelectionManager.Instance.EnableSelectionOnly(); // 使用仅选择模式
        }
    }

    /// <summary>
    /// 禁用选择系统
    /// </summary>
    private void DisableSelectionSystem()
    {
        if (ChessSelectionManager.Instance != null)
        {
            ChessSelectionManager.Instance.Disable();
        }
    }

    /// <summary>
    /// 销毁战斗场地（立即销毁，不播放溶解）
    /// </summary>
    private void DestroyBattleArenaImmediate()
    {
        // 销毁战斗场地
        if (BattleArenaManager.Instance != null)
        {
            BattleArenaManager.Instance.DestroyArena();
            DebugEx.LogModule("CombatState", "战斗场地已销毁");
        }

        // 清理溶解管理器
        DissolveTransitionManager.Instance.Cleanup();
        DebugEx.LogModule("CombatState", "溶解管理器已清理");
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
            DebugEx.LogModule("CombatState", "PlayerSkillManager 已禁用");
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
                    DebugEx.LogModule("CombatState", "PlayerController 已启用");
                }
            }
        }
    }

    /// <summary>
    /// 启用鼠标位置预览（仅预览模式）
    /// </summary>
    private void EnableMousePreview()
    {
        if (ChessPlacementManager.Instance != null)
        {
            ChessPlacementManager.Instance.EnablePreviewOnly();
        }
    }

    /// <summary>
    /// 禁用鼠标位置预览
    /// </summary>
    private void DisableMousePreview()
    {
        if (ChessPlacementManager.Instance != null)
        {
            ChessPlacementManager.Instance.Disable();
        }
    }

    /// <summary>
    /// 生成敌人
    /// </summary>
    private async UniTask SpawnEnemiesAndEnableAI()
    {
        DebugEx.LogModule("CombatState", "开始生成敌人...");
        await EnemySpawnManager.Instance.SpawnWaveAsync();
        DebugEx.LogModule("CombatState", "敌人生成完成");
        
        // 敌人生成完成后，启用所有棋子的战斗AI
        EnableAllChessCombatAI();
    }

    private async void SpawnEnemies()
    {
        await EnemySpawnManager.Instance.SpawnWaveAsync();
        DebugEx.LogModule("CombatState", "敌人生成完成");
    }

    #endregion

    #region 棋子战斗AI管理

    /// <summary>
    /// 启用所有棋子的战斗AI
    /// </summary>
    private void EnableAllChessCombatAI()
    {
        if (SummonChessManager.Instance == null)
            return;

        var allChess = SummonChessManager.Instance.GetAllChess();
        for (int i = 0; i < allChess.Count; i++)
        {
            var chess = allChess[i];
            if (chess != null && chess.CombatController != null)
            {
                chess.CombatController.Enable();
            }
        }

        DebugEx.LogModule("CombatState", $"已启用 {allChess.Count} 个棋子的战斗AI");
    }

    /// <summary>
    /// 禁用所有棋子的战斗AI
    /// </summary>
    private void DisableAllChessCombatAI()
    {
        if (SummonChessManager.Instance == null)
            return;

        var allChess = SummonChessManager.Instance.GetAllChess();
        for (int i = 0; i < allChess.Count; i++)
        {
            var chess = allChess[i];
            if (chess != null && chess.CombatController != null)
            {
                chess.CombatController.Disable();
            }
        }

        DebugEx.LogModule("CombatState", "已禁用所有棋子的战斗AI");
    }

    /// <summary>
    /// 销毁所有棋子实例
    /// </summary>
    private void DestroyAllChessInstances()
    {
        if (SummonChessManager.Instance == null)
        {
            DebugEx.WarningModule("CombatState", "SummonChessManager 不存在，跳过棋子销毁");
            return;
        }

        // 获取所有棋子
        var allChess = SummonChessManager.Instance.GetAllChess();
        int count = allChess.Count;

        // 销毁所有棋子
        SummonChessManager.Instance.DestroyAllChess();

        DebugEx.LogModule("CombatState", $"已销毁 {count} 个棋子实例");
    }

    #endregion

    #region 战斗特效管理

    /// <summary>
    /// 初始化战斗特效管理器
    /// </summary>
    private void InitializeCombatVFX()
    {
        CombatVFXManager.Initialize();
        CombatVFXUpdater.EnsureExists();
        DebugEx.LogModule("CombatState", "战斗特效管理器已初始化");
    }

    /// <summary>
    /// 清理战斗特效管理器
    /// </summary>
    private void CleanupCombatVFX()
    {
        CombatVFXManager.Cleanup();
        DebugEx.LogModule("CombatState", "战斗特效管理器已清理");
    }

    #endregion
}
