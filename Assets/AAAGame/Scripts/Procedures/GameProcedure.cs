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

        // 4. 打开常驻游戏UI（这些UI会根据状态事件自动显示/隐藏）
        OpenGameUIs();

        // 5. 最后生成角色
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
        }

        // 初始化检测器
        detector.Initialize();
        Log.Info("GameProcedure: 玩家角色的CombatOpportunityDetector已初始化");
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

        // 注意：这些UI打开后会被隐藏（SetActive(false)）
        // 在状态切换时，它们会根据订阅的事件自动显示/隐藏
    }

    #endregion
}
