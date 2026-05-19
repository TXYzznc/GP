using System.Collections.Generic;
using AAAGame.Audio;
using Cysharp.Threading.Tasks;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 游戏流程 - 处理游戏场景中进行的游戏
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameProcedure : ProcedureBase
{
    private static IFsm<IProcedureManager> s_ProcedureOwner;
    private IFsm<IProcedureManager> m_ProcedureFsm;
    private PlayerSkillManager m_SkillManager; // 玩家技能管理器引用
    private SceneSpawnManager m_SceneSpawnManager; // 场景生成管理器

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        m_ProcedureFsm = procedureOwner;
        s_ProcedureOwner = procedureOwner;

        Log.Info("进入 GameProcedure - 游戏开始");

        // 确保游戏未暂停
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }

        // 锁定鼠标（进入游戏流程）
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorLock(true);
            Log.Info("GameProcedure: 鼠标已锁定");
        }

        // 订阅游戏事件
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);

        // 0. 加载日志配置（仅编辑器模式）
#if UNITY_EDITOR
        LoadLogConfig();
#endif

        // 0.5. 初始化音乐系统
        InitializeAudioSystem();

        // 1. 初始化物品效果工厂
        ItemEffectFactory.RegisterAll();

        // 2. 初始化卡牌系统
        InitializeCardSystem();

        // 3. 初始化战斗特效系统
        InitializeCombatVFXSystem().Forget();

        // 4. 初始化场景生成管理器
        InitializeSceneSpawnManager();

        // 5. 打开常驻游戏UI，等所有UI加载完后再切换游戏状态
        // 注意：状态切换会Fire事件，UI必须已订阅（OnOpen中订阅）才能收到
        OpenGameUIsAndInitStateAsync().Forget();

        // 6. 异步加载SkillManager，完成后再生成角色（只有角色生成需要等SkillManager）
        SpawnCharacterAfterSkillManagerAsync().Forget();

        Log.Info("GameProcedure 初始化完成");
    }

    protected override void OnUpdate(
        IFsm<IProcedureManager> procedureOwner,
        float elapseSeconds,
        float realElapseSeconds
    )
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        // TODO: 游戏逻辑更新
        // 例如：检查游戏结束条件，更新游戏状态等
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        Log.Info("离开 GameProcedure");

        // ⭐ 第一步：关闭所有由 OpenGameUIs 打开的 UI（防止场景切换时 UI 重复）
        CloseGameUIs();

        // ⭐ 第二步：清理敌人管理器的场景敌人列表（防止跨场景残留）
        if (EnemyEntityManager.Instance != null)
        {
            EnemyEntityManager.Instance.Clear();
            DebugEx.Log(nameof(GameProcedure), "EnemyEntityManager 已清理");
        }

        // ⭐ 第三步：防御性清理纯 C# 单例的脏状态
        // 如果在战斗中切换场景，这些清理会确保下一场战斗有干净的初始状态
        SummonerRuntimeDataManager.Instance.Cleanup();
        DebugEx.Log(nameof(GameProcedure), "SummonerRuntimeDataManager 已清理");

        CombatSessionData.Instance.Clear();
        DebugEx.Log(nameof(GameProcedure), "CombatSessionData 已清理");

        BattleChessManager.Instance.Clear();
        DebugEx.Log(nameof(GameProcedure), "BattleChessManager 已清理");

        // 清理战斗触发上下文
        CombatTriggerManager.Instance?.ClearContext();
        DebugEx.Log(nameof(GameProcedure), "CombatTriggerManager 上下文已清理");

        // 清理技能管理器
        if (m_SkillManager != null)
        {
            Object.Destroy(m_SkillManager.gameObject);
            m_SkillManager = null;
            Log.Info("GameProcedure: 技能管理器已清理");
        }

        // 清理场景生成管理器
        if (m_SceneSpawnManager != null)
        {
            Object.Destroy(m_SceneSpawnManager.gameObject);
            m_SceneSpawnManager = null;
            Log.Info("GameProcedure: 场景生成管理器已清理");
        }

        // 确保游戏未暂停
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }

        // 解锁鼠标（离开游戏流程）
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorLock(false);
            Log.Info("GameProcedure: 鼠标已解锁");
        }

        // 取消订阅事件
        GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);

        s_ProcedureOwner = null;

        base.OnLeave(procedureOwner, isShutdown);
    }

    /// <summary>
    /// 在游戏内切换场景（场景A → 场景B）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public static void RequestChangeScene(string sceneName)
    {
        if (s_ProcedureOwner == null)
        {
            Log.Error("GameProcedure 未初始化，无法切换场景");
            return;
        }

        Log.Info($"GameProcedure: 请求场景切换到 {sceneName}");

        // 显示加载进度条
        GFBuiltin.BuiltinView.ShowLoadingProgress();

        // 设置场景名流程参数
        s_ProcedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, sceneName);

        // 获取当前 Procedure 并切换到 ChangeSceneProcedure
        var currentProcedure = s_ProcedureOwner.CurrentState as GameProcedure;
        if (currentProcedure != null)
        {
            currentProcedure.ChangeState<ChangeSceneProcedure>(s_ProcedureOwner);
        }
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void BackToMenu()
    {
        Log.Info("从游戏返回主菜单");
        GameFlowManager.BackToMenu();
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        Log.Info("重新开始游戏");
        ChangeState<GameProcedure>(m_ProcedureFsm);
    }

    private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        // 由 OpenGameUIsAndInitStateAsync 通过 OpenUIFormAwait 精确等待，此处不再使用全局计数
    }

    private void OnCloseUIForm(object sender, GameEventArgs e)
    {
        // TODO: 处理UI关闭事件
        // 例如：关闭暂停菜单时恢复游戏
    }

    #region 技能管理器初始化

    /// <summary>
    /// 初始化技能管理器
    /// </summary>
    public async UniTask InitializeSkillManagerAsync()
    {
        // 创建技能管理器对象
        GameObject skillManagerObj = new GameObject("PlayerSkillManager");
        m_SkillManager = skillManagerObj.AddComponent<PlayerSkillManager>();

        // 使用 ResourceExtension 异步加载技能参数注册表
        try
        {
            var paramRegistry =
                await GameExtension.ResourceExtension.LoadScriptableObjectAsync<SkillParamRegistrySO>(
                    ResourceIds.SO_SKILL_PARAM_REGISTRY
                );

            if (paramRegistry != null)
            {
                m_SkillManager.SetParamRegistry(paramRegistry);
                Log.Info("GameProcedure: 技能参数注册表已加载");
            }
            else
            {
                Log.Warning("GameProcedure: 未找到技能参数注册表，技能可能无法正常工作");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"GameProcedure: 加载技能参数注册表失败: {ex.Message}");
        }

        Log.Info("GameProcedure: 技能管理器已创建");
    }

    /// <summary>
    /// 等待技能参数注册表加载完成后，再执行后续初始化和角色生成
    /// 解决打包后异步加载比编辑器慢导致的时序问题
    /// </summary>
    private async UniTask SpawnCharacterAfterSkillManagerAsync()
    {
        await InitializeSkillManagerAsync();

        var testModeData = m_ProcedureFsm.GetData<VarString>("IsExploreAITestMode");
        bool isTestMode = testModeData != null && testModeData.Value == "true";

        if (!isTestMode)
        {
            PlayerCharacterManager.Instance.SpawnPlayerCharacterFromSave(OnCharacterSpawned);
        }
        else
        {
            DebugEx.Log("GameProcedure", "✓ 敌人AI测试模式已识别，跳过自动玩家生成");
        }
    }

    #endregion

    #region 卡牌系统初始化

    /// <summary>
    /// 初始化卡牌系统
    /// </summary>
    private void InitializeCardSystem()
    {
        // 如果 CardManager 不存在，动态添加
        if (CardManager.Instance == null)
        {
            GameObject cardManagerObj = new GameObject("CardManager");
            cardManagerObj.AddComponent<CardManager>();
            Log.Info("GameProcedure: 动态添加 CardManager");
        }
        else
        {
            Log.Info("GameProcedure: CardManager 已存在");
        }
    }

    /// <summary>
    /// 初始化战斗特效系统
    /// </summary>
    private async UniTaskVoid InitializeCombatVFXSystem()
    {
        // CombatVFXManager 是静态类，调用初始化方法
        // 使用 InitializeAndWaitAsync 确保初始化完成后再继续
        await CombatVFXManager.InitializeAndWaitAsync();
        Log.Info("GameProcedure: CombatVFXManager 已初始化");
    }

    #endregion

    #region 场景生成管理器初始化

    /// <summary>
    /// 初始化场景生成管理器
    /// </summary>
    private void InitializeSceneSpawnManager()
    {
        Log.Info("GameProcedure: [开始] 初始化场景生成管理器");

        // 获取当前场景的 MapId
        int mapId = GetCurrentMapId();
        Log.Info($"GameProcedure: [查询] 当前场景 MapId={mapId}");

        if (mapId < 0)
        {
            Log.Warning("GameProcedure: [失败] 无法获取当前场景的 MapId，检查 SceneTable 配置");
            return;
        }

        // 创建场景生成管理器
        GameObject spawnManagerObj = new GameObject("SceneSpawnManager");
        m_SceneSpawnManager = spawnManagerObj.AddComponent<SceneSpawnManager>();
        Log.Info("GameProcedure: [创建] SceneSpawnManager GameObject 已创建");

        // 初始化生成管理器
        m_SceneSpawnManager.Initialize(mapId);
        Log.Info($"GameProcedure: [完成] 场景生成管理器已初始化 (MapId={mapId})");
    }

    /// <summary>
    /// 获取当前场景的 MapId（对应 SceneTable.Id）
    /// </summary>
    private int GetCurrentMapId()
    {
        // 从 SceneStateManager 或通过场景名称查询 SceneTable
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Log.Info($"GameProcedure: [查表] 当前场景名称 = '{currentSceneName}'");

        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
        {
            Log.Error("GameProcedure: [错误] SceneTable 未加载");
            return -1;
        }

        Log.Info($"GameProcedure: [查表] SceneTable 已加载，开始匹配...");

        var allScenes = sceneTable.GetAllDataRows();
        Log.Info($"GameProcedure: [查表] SceneTable 中共有 {allScenes.Length} 个场景");

        foreach (var scene in allScenes)
        {
            Log.Info($"GameProcedure:   - Scene: Id={scene.Id}, Name='{scene.SceneName}'");
            if (scene.SceneName == currentSceneName)
            {
                Log.Info(
                    $"GameProcedure: [匹配成功] 场景 '{currentSceneName}' 对应 MapId={scene.Id}"
                );
                return (int)scene.Id;
            }
        }

        Log.Warning($"GameProcedure: [匹配失败] 在 SceneTable 中找不到场景 '{currentSceneName}'");
        return -1;
    }

    #endregion

    /// <summary>
    /// 角色生成完成回调
    /// </summary>
    private void OnCharacterSpawned(GameObject character)
    {
        if (character != null)
        {
            Log.Info("角色生成成功，开始游戏流程");

            // 局外状态下确保 PlayerController 启用（角色生成时状态已切换，OnEnter里角色还不存在）
            var currentState = GameStateManager.Instance.CurrentState;
            if (currentState == GameStateType.OutOfGame)
            {
                var controller = character.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.enabled = true;
                    DebugEx.Log(nameof(GameProcedure), "局外场景角色生成完成，PlayerController 已启用");
                }
            }

            // 为玩家角色添加战斗机会检测器
            AddCombatOpportunityDetector(character);

            // 使用场景中的技能管理器
            if (m_SkillManager != null)
            {
                // 将角色对象传递给技能管理器（如果需要的话）
                // m_SkillManager.SetPlayerCharacter(character);

                // 从配置表获取所有技能ID
                List<int> playerSkillIds = DataTableExtension.GetAllIds<PlayerSkillTable>();

                if (playerSkillIds.Count > 0)
                {
                    // 先给玩家角色设置，加载技能（顺序很重要）
                    m_SkillManager.SetPlayerCharacter(character);
                    m_SkillManager.UpdateSkillsFromPlayerData(playerSkillIds);
                    Log.Info($"GameProcedure: 已加载 {playerSkillIds.Count} 个技能");

                    // UI会自动监听并绑定技能，不需要手动刷新
                    Log.Info("GameProcedure: 技能加载完成，UI会自动刷新");
                }
                else
                {
                    Log.Warning("GameProcedure: 配置表中没有找到任何技能！");
                }
            }
            else
            {
                Log.Error("GameProcedure: 技能管理器未初始化！");
            }
        }
        else
        {
            Log.Error("角色生成失败");
            // 生成失败，返回主菜单或重启游戏
            GameFlowManager.BackToMenu();
        }
    }

    /// <summary>
    /// 初始化玩家角色的战斗机会检测器
    /// </summary>
    private void AddCombatOpportunityDetector(GameObject playerCharacter)
    {
        if (playerCharacter == null)
        {
            Log.Error("GameProcedure: 玩家角色为空，无法初始化CombatOpportunityDetector");
            return;
        }

        // 获取或动态添加 CombatOpportunityDetector（文件迁移后预制体引用可能丢失，用 AddComponent 兜底）
        CombatOpportunityDetector detector =
            playerCharacter.GetComponent<CombatOpportunityDetector>();
        if (detector == null)
        {
            detector = playerCharacter.AddComponent<CombatOpportunityDetector>();
            Log.Info("GameProcedure: 动态添加 CombatOpportunityDetector 到玩家角色");
            DebugEx.Warning(
                nameof(GameProcedure),
                "<color=red>[诊断] ⚠️ CombatOpportunityDetector 是动态 AddComponent 的！"
                    + "SerializeField（如 EnemyLayerMask）不会有值！需要手动设置或在预制体上预先挂载。</color>"
            );
        }
        else
        {
            DebugEx.Log(
                nameof(GameProcedure),
                "<color=cyan>[诊断] CombatOpportunityDetector 已在预制体上存在</color>"
            );
        }

        // 初始化检测器
        detector.Initialize();
        Log.Info("GameProcedure: 玩家角色的CombatOpportunityDetector已初始化");

        // 诊断：输出玩家角色信息
        DebugEx.Log(
            nameof(GameProcedure),
            $"<color=cyan>[诊断] 玩家角色: {playerCharacter.name}, "
                + $"Layer={LayerMask.LayerToName(playerCharacter.layer)}({playerCharacter.layer}), "
                + $"Position={playerCharacter.transform.position}</color>"
        );
    }

    /// <summary>
    /// 刷新技能UI显示
    /// </summary>
    private void RefreshSkillUI()
    {
        // 查找已打开的技能UI
        string uiAssetName = GF.UI.GetUIFormAssetName(UIViews.PlayerSkillUI);
        if (string.IsNullOrEmpty(uiAssetName))
        {
            Log.Warning("GameProcedure: 无法获取 PlayerSkillUI 的资源名称");
            return;
        }

        var uiForm = GF.UI.GetUIForm(uiAssetName);
        if (uiForm != null)
        {
            PlayerSkillUI skillUI = uiForm.Logic as PlayerSkillUI;
            if (skillUI != null)
            {
                skillUI.RefreshSkills();
                Log.Info("GameProcedure: 技能UI已刷新");
            }
        }
        else
        {
            Log.Warning("GameProcedure: 未找到已打开的 PlayerSkillUI");
        }
    }

    #region 游戏状态初始化

    /// <summary>
    /// 根据当前场景类型初始化游戏状态
    /// </summary>
    private void InitializeGameStateByScene()
    {
        var sceneType = SceneStateManager.Instance.CurrentSceneType;
        var gameState = SceneStateManager.Instance.GetGameStateBySceneType(sceneType);

        Log.Info($"GameProcedure: 场景类型={sceneType}, 游戏状态={gameState}");

        // 根据游戏状态切换
        switch (gameState)
        {
            case GameStateType.OutOfGame:
                GameStateManager.Instance.SwitchToOutOfGame();
                Log.Info("GameProcedure: 已切换到游戏外状态");
                break;

            case GameStateType.InGame:
                GameStateManager.Instance.SwitchToInGame();
                Log.Info("GameProcedure: 已切换到游戏内状态（探索）");
                break;

            default:
                Log.Warning($"GameProcedure: 未知游戏状态 {gameState}");
                break;
        }
    }

    #endregion

    #region UI 管理

    /// <summary>
    /// 关闭所有由 OpenGameUIs 打开的 UI（场景切换时调用）
    /// </summary>
    private void CloseGameUIs()
    {
        // 关闭所有可能被打开的 UI（无论当前游戏状态是什么）
        // 使用 HasUIForm 判断存在再关闭，避免报错

        UIViews[] uiViewsToClose = new UIViews[]
        {
            UIViews.GamePlayInfoUI,
            UIViews.CurrencyUI,
            UIViews.StarPhoneUI,
            UIViews.CombatUI,
            UIViews.PlayerSkillUI,
            UIViews.OutsiderFunctionUI,
        };

        int closedCount = 0;
        foreach (var uiView in uiViewsToClose)
        {
            string uiAssetName = GF.UI.GetUIFormAssetName(uiView);
            if (string.IsNullOrEmpty(uiAssetName))
                continue;

            if (GF.UI.HasUIForm(uiAssetName))
            {
                GF.UI.CloseUIForms(uiView);
                closedCount++;
                DebugEx.Log(nameof(GameProcedure), $"已关闭 UI: {uiView}");
            }
        }

        DebugEx.Log(nameof(GameProcedure), $"场景切换时关闭了 {closedCount} 个 UI");
    }

    /// <summary>
    /// 并行打开所有UI，等全部加载完成后再切换游戏状态
    /// 用 UniTask.WhenAll 精确等待，避免全局事件计数的干扰
    /// </summary>
    private async UniTaskVoid OpenGameUIsAndInitStateAsync()
    {
        // 用场景类型判断要打开哪些UI（此时gameState还没切换，不能用它）
        var sceneType = SceneStateManager.Instance.CurrentSceneType;
        var targetGameState = SceneStateManager.Instance.GetGameStateBySceneType(sceneType);

        if (targetGameState == GameStateType.OutOfGame)
        {
            await UniTask.WhenAll(
                GF.UI.OpenUIFormAwait(UIViews.CurrencyUI),
                GF.UI.OpenUIFormAwait(UIViews.GamePlayInfoUI),
                GF.UI.OpenUIFormAwait(UIViews.StarPhoneUI),
                GF.UI.OpenUIFormAwait(UIViews.OutsiderFunctionUI)
            );
        }
        else if (targetGameState == GameStateType.InGame)
        {
            await UniTask.WhenAll(
                GF.UI.OpenUIFormAwait(UIViews.GamePlayInfoUI),
                GF.UI.OpenUIFormAwait(UIViews.CurrencyUI),
                GF.UI.OpenUIFormAwait(UIViews.StarPhoneUI),
                GF.UI.OpenUIFormAwait(UIViews.CombatUI),
                GF.UI.OpenUIFormAwait(UIViews.PlayerSkillUI)
            );
        }

        // 所有UI都已 OnOpen（SubscribeEvents已执行），现在切换状态才能让UI收到事件
        InitializeGameStateByScene();
        Log.Info("GameProcedure: 所有UI加载完成，游戏状态已初始化");
    }

    #region 音乐系统初始化

    /// <summary>
    /// 初始化音乐系统
    /// </summary>
    private void InitializeAudioSystem()
    {
        // 检查 AudioManager 是否已存在
        if (AudioManager.Instance != null)
        {
            Log.Info("GameProcedure: AudioManager 已初始化");
            return;
        }

        // 创建 AudioManager GameObject
        var audioManagerGo = new GameObject("AudioManager");
        var audioManager = audioManagerGo.AddComponent<AudioManager>();
        Object.DontDestroyOnLoad(audioManagerGo);

        // 创建 AudioEventListener 用于流程事件响应
        var audioListenerGo = new GameObject("AudioEventListener");
        audioListenerGo.transform.SetParent(audioManagerGo.transform);
        audioListenerGo.AddComponent<AudioEventListener>();

        Log.Info("GameProcedure: 音乐系统已初始化");

        // 播放游戏流程 BGM
        AudioEventListener.Instance?.PlayBGMForProcedure("GameProcedure");
    }

    #endregion

    /// <summary>
    /// 加载日志配置并应用到 DebugEx（仅编辑器模式）
    /// </summary>
#if UNITY_EDITOR
    private void LoadLogConfig()
    {
        var config = LogConfigManager.LoadConfigFromFile();
        if (config.Count > 0)
        {
            DebugEx.SetAllScriptLogEnabled(config);
            Log.Info($"GameProcedure: 已加载日志配置，共 {config.Count} 个脚本");
        }
        else
        {
            Log.Info("GameProcedure: 未找到日志配置文件，使用默认设置");
        }
    }
#endif

    #endregion
}
