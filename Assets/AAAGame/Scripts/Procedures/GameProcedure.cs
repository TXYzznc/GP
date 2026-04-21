using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Event;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏流程 - 处理游戏场景中进行的游戏
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameProcedure : ProcedureBase
{
    private IFsm<IProcedureManager> m_ProcedureFsm;
    private PlayerSkillManager m_SkillManager; // 玩家技能管理器引用
    private SceneSpawnManager m_SceneSpawnManager; // 场景生成管理器

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        m_ProcedureFsm = procedureOwner;

        Log.Info("进入 GameProcedure - 游戏开始");

        // 确保游戏未暂停
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }

        // 根据当前场景类型初始化游戏状态
        InitializeGameStateByScene();

        // 锁定鼠标（进入游戏流程）
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorLock(true);
            Log.Info("GameProcedure: 鼠标已锁定");
        }

        // 订阅游戏事件
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);

        // TODO: 这里开始初始化游戏相关的内容
        // 例如：打开游戏UI，开始加载关卡，生成实体等
        // GF.UI.OpenUIForm(UIViews.GameUIForm);
        // 建议：先生成后再加载，生成角色

        // 1. 先实例化技能管理器（在场景中）
        InitializeSkillManager();

        // 2. 初始化卡牌系统（动态添加 CardManager）
        InitializeCardSystem();

        // 3. 初始化战斗特效系统
        InitializeCombatVFXSystem();

        // 4. 初始化场景生成管理器（根据当前地图 ID 生成敌人/宝箱）
        InitializeSceneSpawnManager();

        // 5. 打开常驻游戏UI（这些UI会根据状态事件自动显示/隐藏）
        OpenGameUIs();

        // 6. 最后生成角色
        PlayerCharacterManager.Instance.SpawnPlayerCharacterFromSave(OnCharacterSpawned);

        Log.Info("GameProcedure 初始化完成");
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        // TODO: 游戏逻辑更新
        // 例如：检查游戏结束条件，更新游戏状态等
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        Log.Info("离开 GameProcedure");

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

        base.OnLeave(procedureOwner, isShutdown);
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
        // TODO: 处理UI打开事件
        // 例如：打开暂停菜单时暂停游戏
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
    private async void InitializeSkillManager()
    {
        // 创建技能管理器对象
        GameObject skillManagerObj = new GameObject("PlayerSkillManager");
        m_SkillManager = skillManagerObj.AddComponent<PlayerSkillManager>();

        // 使用 ResourceExtension 异步加载技能参数注册表
        try
        {
            var paramRegistry = await GameExtension.ResourceExtension.LoadScriptableObjectAsync<SkillParamRegistrySO>(ResourceIds.SO_SKILL_PARAM_REGISTRY);

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
    private async void InitializeCombatVFXSystem()
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
                Log.Info($"GameProcedure: [匹配成功] 场景 '{currentSceneName}' 对应 MapId={scene.Id}");
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
        CombatOpportunityDetector detector = playerCharacter.GetComponent<CombatOpportunityDetector>();
        if (detector == null)
        {
            detector = playerCharacter.AddComponent<CombatOpportunityDetector>();
            Log.Info("GameProcedure: 动态添加 CombatOpportunityDetector 到玩家角色");
            DebugEx.WarningModule("GameProcedure",
                "<color=red>[诊断] ⚠️ CombatOpportunityDetector 是动态 AddComponent 的！" +
                "SerializeField（如 EnemyLayerMask）不会有值！需要手动设置或在预制体上预先挂载。</color>");
        }
        else
        {
            DebugEx.LogModule("GameProcedure",
                "<color=cyan>[诊断] CombatOpportunityDetector 已在预制体上存在</color>");
        }

        // 初始化检测器
        detector.Initialize();
        Log.Info("GameProcedure: 玩家角色的CombatOpportunityDetector已初始化");

        // 诊断：输出玩家角色信息
        DebugEx.LogModule("GameProcedure",
            $"<color=cyan>[诊断] 玩家角色: {playerCharacter.name}, " +
            $"Layer={LayerMask.LayerToName(playerCharacter.layer)}({playerCharacter.layer}), " +
            $"Position={playerCharacter.transform.position}</color>");
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
    /// 打开常驻游戏UI
    /// </summary>
    private void OpenGameUIs()
    {
        var gameState = GameStateManager.Instance.CurrentState;

        // 局外状态UI（基地场景）
        if (gameState == GameStateType.OutOfGame)
        {
            // 打开经常通用UI（右上角 - 货币显示）
            GF.UI.OpenUIForm(UIViews.CurrencyUI);
            Log.Info("GameProcedure: 已打开 CurrencyUI");

            // 打开游戏信息UI
            GF.UI.OpenUIForm(UIViews.GamePlayInfoUI);
            Log.Info("GameProcedure: 已打开 GamePlayInfoUI");

            // 打开星机UI
            GF.UI.OpenUIForm(UIViews.StarPhoneUI);
            Log.Info("GameProcedure: 已打开 StarPhoneUI");

            // 打开局外功能UI
            GF.UI.OpenUIForm(UIViews.OutsiderFunctionUI);
            Log.Info("GameProcedure: 已打开 OutsiderFunctionUI");
        }
        // 局内状态UI（探索/战斗场景）
        else if (gameState == GameStateType.InGame)
        {
            // 打开经常通用UI（左上角 - 游戏信息）
            GF.UI.OpenUIForm(UIViews.GamePlayInfoUI);
            Log.Info("GameProcedure: 已打开 GamePlayInfoUI");

            // 打开经常通用UI（右上角 - 货币显示）
            GF.UI.OpenUIForm(UIViews.CurrencyUI);
            Log.Info("GameProcedure: 已打开 CurrencyUI");

            // 探索UI（右下角 - 星机）
            GF.UI.OpenUIForm(UIViews.StarPhoneUI);
            Log.Info("GameProcedure: 已打开 StarPhoneUI");

            // 战斗UI（开始隐藏，等待战斗状态事件）
            GF.UI.OpenUIForm(UIViews.CombatUI);
            Log.Info("GameProcedure: 已打开 CombatUI");

            // 打开技能UI（底部中间 - 技能快捷栏）
            GF.UI.OpenUIForm(UIViews.PlayerSkillUI);
            Log.Info("GameProcedure: 已打开 PlayerSkillUI");

            // 注意：背包UI不在这里打开，由玩家手动打开
            // 注意：这些UI打开后会被隐藏（SetActive(false)）
            // 在状态切换时，它们会根据订阅的事件自动显示/隐藏
        }
    }

    #endregion
}
