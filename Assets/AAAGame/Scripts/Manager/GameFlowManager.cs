using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 游戏流程管理器 - 集中管理游戏状态转换和场景切换
/// </summary>
public static class GameFlowManager
{
    /// <summary>
    /// 进入游戏 - 加载游戏场景并切换到游戏流程
    /// </summary>
    public static void EnterGame()
    {
        Log.Info("=== 进入游戏 ===");

        // 打印当前存档信息（用于调试）
        var currentSave = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (currentSave != null)
        {
            Log.Info($"存档名称: {currentSave.SaveName}");
            Log.Info($"玩家等级: {currentSave.GlobalLevel}");
            Log.Info($"金币: {currentSave.Gold}");
            Log.Info($"灵石: {currentSave.OriginStone}");
            Log.Info($"召唤师ID: {currentSave.CurrentSummonerId}");
            Log.Info($"是否完成引导: {currentSave.HasCompletedTutorial}");
            Log.Info($"当前场景ID: {currentSave.CurrentSceneId}");
        }
        else
        {
            Log.Warning("当前没有加载存档数据");
        }

        // 关闭所有菜单相关的 UI
        GF.UI.CloseAllLoadedUIForms();

        // 根据存档数据获取应该进入的场景
        int sceneId = SceneStateManager.Instance.GetInitialSceneId();

        // 从配置表获取场景名称
        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
        {
            Log.Error("GameFlowManager: 场景配置表未加载，使用默认场景");
            ChangeScene("Test");
            return;
        }

        var sceneRow = sceneTable.GetDataRow(sceneId);
        if (sceneRow == null)
        {
            Log.Error($"GameFlowManager: 场景ID {sceneId} 不存在，使用默认场景");
            ChangeScene("Test");
            return;
        }

        Log.Info(
            $"GameFlowManager: 进入场景 {sceneRow.SceneName} (ID={sceneId}, Type={sceneRow.GetSceneTypeEnum()})"
        );

        // 切换到对应场景
        ChangeScene(sceneRow.SceneName);
    }

    /// <summary>
    /// 进入指定场景（通过场景ID）
    /// </summary>
    /// <param name="sceneId">场景ID</param>
    public static void EnterScene(int sceneId)
    {
        Log.Info($"=== 进入场景 ID={sceneId} ===");

        // 从配置表获取场景名称
        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
        {
            Log.Error("GameFlowManager: 场景配置表未加载");
            return;
        }

        var sceneRow = sceneTable.GetDataRow(sceneId);
        if (sceneRow == null)
        {
            Log.Error($"GameFlowManager: 场景ID {sceneId} 不存在");
            return;
        }

        Log.Info(
            $"GameFlowManager: 切换到场景 {sceneRow.SceneName} (ID={sceneId}, Type={sceneRow.GetSceneTypeEnum()})"
        );

        // 切换到对应场景
        ChangeScene(sceneRow.SceneName);
    }

    /// <summary>
    /// 切换场景
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public static void ChangeScene(string sceneName)
    {
        Log.Info($"准备切换场景: {sceneName}");

        GFBuiltin.BuiltinView.ShowLoadingProgress();

        // 根据当前激活的 Procedure 选择正确的切换入口
        // GameProcedure（局内）：通过 GameProcedure.RequestChangeScene
        // StartGameProcedure（主菜单）：通过 StartGameProcedure.RequestChangeScene
        if (GameProcedure.IsActive)
            GameProcedure.RequestChangeScene(sceneName);
        else
            StartGameProcedure.RequestChangeScene(sceneName);
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public static void BackToMenu()
    {
        Log.Info("返回主菜单");
        ChangeScene("StartGame");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public static void QuitGame()
    {
        Log.Info("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
