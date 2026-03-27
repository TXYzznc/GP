using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 场景状态管理器 - 负责场景与游戏状态的映射
/// </summary>
public class SceneStateManager : SingletonBase<SceneStateManager>
{
    #region 字段

    private int m_CurrentSceneId = 0;
    private SceneType m_CurrentSceneType = SceneType.Unknown;

    #endregion

    #region 属性

    /// <summary>
    /// 当前场景ID
    /// </summary>
    public int CurrentSceneId => m_CurrentSceneId;

    /// <summary>
    /// 当前场景类型
    /// </summary>
    public SceneType CurrentSceneType => m_CurrentSceneType;

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前场景信息
    /// </summary>
    public void SetCurrentScene(int sceneId, SceneType sceneType)
    {
        m_CurrentSceneId = sceneId;
        m_CurrentSceneType = sceneType;

        Log.Info($"SceneStateManager: 当前场景 ID={sceneId}, Type={sceneType}");

        // 保存到存档
        SaveCurrentSceneToPlayerData();
    }

    /// <summary>
    /// 根据场景类型获取对应的游戏状态
    /// </summary>
    public GameStateType GetGameStateBySceneType(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Base:
                return GameStateType.OutOfGame;

            case SceneType.World:
            case SceneType.Tutorial:
            case SceneType.Dungeon:
                return GameStateType.InGame;

            default:
                Log.Warning($"未知场景类型: {sceneType}，默认返回局外状态");
                return GameStateType.OutOfGame;
        }
    }

    /// <summary>
    /// 切换到指定场景
    /// </summary>
    public void ChangeToScene(int sceneId)
    {
        // 从配置表读取场景信息
        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
        {
            Log.Error("SceneStateManager: 场景配置表未加载");
            return;
        }

        var sceneRow = sceneTable.GetDataRow(sceneId);
        if (sceneRow == null)
        {
            Log.Error($"SceneStateManager: 场景ID {sceneId} 不存在");
            return;
        }

        // 检查进入条件
        if (!CheckSceneCondition(sceneRow))
        {
            Log.Warning($"SceneStateManager: 不满足场景 {sceneRow.SceneName} 的进入条件");
            ShowConditionNotMetUI(sceneRow);
            return;
        }

        // 设置当前场景信息
        SetCurrentScene(sceneId, sceneRow.GetSceneTypeEnum());

        // 触发场景切换
        Log.Info($"SceneStateManager: 切换到场景 {sceneRow.SceneName}");
        StartGameProcedure.RequestChangeScene(sceneRow.SceneName);
    }

    /// <summary>
    /// 获取玩家应该进入的初始场景ID
    /// </summary>
    public int GetInitialSceneId()
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null)
        {
            Log.Warning("SceneStateManager: 存档数据为空，默认进入引导场景");
            return 3; // TutorialScene
        }

        // 如果已完成引导，进入基地
        if (saveData.HasCompletedTutorial)
        {
            // 如果有保存的场景ID，优先使用
            if (saveData.CurrentSceneId > 0)
            {
                return saveData.CurrentSceneId;
            }
            return 1; // BaseScene
        }

        // 未完成引导，进入引导场景
        return 3; // TutorialScene
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检查场景进入条件
    /// </summary>
    private bool CheckSceneCondition(SceneTable sceneRow)
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null)
        {
            Log.Warning("SceneStateManager: 存档数据为空");
            return false;
        }

        // 使用 SceneTable 的扩展方法检查条件
        bool canEnter = sceneRow.CheckCondition(saveData);

        if (!canEnter)
        {
            Log.Info($"SceneStateManager: 不满足场景 {sceneRow.SceneName} 的进入条件");
            Log.Info($"  条件类型: {sceneRow.GetConditionTypeEnum()}");
            Log.Info($"  条件参数: {sceneRow.ConditionParam}");
        }

        // 如果是自定义条件，需要额外检查
        if (sceneRow.GetConditionTypeEnum() == SceneConditionType.Custom)
        {
            string customCheckName = sceneRow.ConditionParam;
            return CheckCustomCondition(customCheckName, saveData);
        }

        return canEnter;
    }

    /// <summary>
    /// 检查自定义条件
    /// </summary>
    private bool CheckCustomCondition(string conditionName, PlayerSaveData saveData)
    {
        // 这里可以根据条件名称调用不同的检查函数
        switch (conditionName)
        {
            case "HasCompletedMainStory":
                // 检查是否完成主线剧情
                return saveData.CompletedQuestIds.Contains(9999);

            default:
                Log.Warning($"SceneStateManager: 未知自定义条件 {conditionName}");
                return false;
        }
    }

    /// <summary>
    /// 显示条件不满足的提示UI
    /// </summary>
    private void ShowConditionNotMetUI(SceneTable sceneRow)
    {
        // 使用 SceneTable 的扩展方法获取提示文本
        string message = sceneRow.GetConditionNotMetMessage();

        // 显示提示
        GF.UI.ShowToast(message, UIExtension.ToastStyle.Red);
    }

    /// <summary>
    /// 保存当前场景到玩家存档
    /// </summary>
    private void SaveCurrentSceneToPlayerData()
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData != null)
        {
            saveData.CurrentSceneId = m_CurrentSceneId;
            PlayerAccountDataManager.Instance.SaveCurrentSave();
        }
    }

    #endregion
}
