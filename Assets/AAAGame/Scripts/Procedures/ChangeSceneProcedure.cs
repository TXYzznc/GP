using GameFramework;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ChangeSceneProcedure : ProcedureBase
{
    /// <summary>
    /// 要加载的场景资源名，保存在流程目录
    /// </summary>
    internal const string P_SceneName = "SceneName";
    private bool loadSceneOver = false;
    private string nextScene = string.Empty;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        loadSceneOver = false;

        GF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
        GF.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        GF.Event.Subscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);

        // 停止所有声音
        GF.Sound.StopAllLoadingSounds();
        GF.Sound.StopAllLoadedSounds();

        // 隐藏所有实体
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();

        // 卸载所有场景
        string[] loadedSceneAssetNames = GF.Scene.GetLoadedSceneAssetNames();
        for (int i = 0; i < loadedSceneAssetNames.Length; i++)
        {
            GF.Scene.UnloadScene(loadedSceneAssetNames[i]);
        }

        // 还原游戏速度
        GF.Base.ResetNormalGameSpeed();

        if (!procedureOwner.HasData(P_SceneName))
        {
            throw new GameFrameworkException("未设置要加载的场景资源名!");
        }
        nextScene = procedureOwner.GetData<VarString>(P_SceneName);
        procedureOwner.RemoveData(P_SceneName);

        // 根据场景名获取场景信息并设置
        SetSceneInfoByName(nextScene);

        GFBuiltin.BuiltinView.SetLoadingProgress(0f);
        GF.Scene.LoadScene(UtilityBuiltin.AssetsPath.GetScenePath(nextScene), this);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (!loadSceneOver)
        {
            return;
        }

        // 场景加载完成，根据场景名切换到对应的 Procedure
        if (nextScene == "StartGame")
        {
            // 主菜单场景 → StartGameProcedure
            Log.Info("ChangeSceneProcedure: 切换到 StartGameProcedure（主菜单）");
            ChangeState<StartGameProcedure>(procedureOwner);
        }
        else
        {
            // 游戏场景 → GameProcedure
            // GameProcedure 会根据场景类型自动启动游戏状态
            Log.Info("ChangeSceneProcedure: 切换到 GameProcedure（游戏场景）");
            ChangeState<GameProcedure>(procedureOwner);
        }
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GF.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
        GF.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        GF.Event.Unsubscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
        base.OnLeave(procedureOwner, isShutdown);
    }

    private void OnLoadSceneUpdate(object sender, GameEventArgs e)
    {
        var arg = (LoadSceneUpdateEventArgs)e;
        if (arg.UserData != this)
        {
            return;
        }
        GFBuiltin.BuiltinView.SetLoadingProgress(arg.Progress);
    }

    private void OnLoadSceneSuccess(object sender, GameEventArgs e)
    {
        var arg = (LoadSceneSuccessEventArgs)e;
        if (arg.UserData != this)
        {
            return;
        }
        Log.Info("场景资源加载成功:{0}", arg.SceneAssetName);
        GFBuiltin.BuiltinView.SetLoadingProgress(1f);
        loadSceneOver = true;
        HideLoadingProgressAsync().Forget();
    }

    // TODO: 加载界面隐藏延迟目前为固定 1s，后续应替换为监听"场景各系统初始化完成"信号来精确控制
    private async Cysharp.Threading.Tasks.UniTaskVoid HideLoadingProgressAsync()
    {
        await Cysharp.Threading.Tasks.UniTask.Delay(300);//0.3s
        GFBuiltin.BuiltinView.HideLoadingProgress();
        loadSceneOver = true;
    }

    // 加载场景资源失败，返回游戏菜单
    private void OnLoadSceneFailure(object sender, GameEventArgs e)
    {
        var arg = (LoadSceneFailureEventArgs)e;
        if (arg.UserData != this)
        {
            return;
        }

        Log.Error("加载场景失败，自动重启游戏：", arg.SceneAssetName);
        GameEntry.Shutdown(ShutdownType.Restart);
    }

    #region 场景配置

    /// <summary>
    /// 根据场景名设置场景信息
    /// </summary>
    private void SetSceneInfoByName(string sceneName)
    {
        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
        {
            Log.Warning("ChangeSceneProcedure: 场景配置表未加载，使用默认场景配置");
            SceneStateManager.Instance.SetCurrentScene(0, SceneType.Unknown);
            return;
        }

        // 在场景配置表中查找场景
        var allRows = sceneTable.GetAllDataRows();
        foreach (var row in allRows)
        {
            if (row.SceneName == sceneName)
            {
                SceneStateManager.Instance.SetCurrentScene(row.Id, row.GetSceneTypeEnum());
                Log.Info($"ChangeSceneProcedure: 找到场景配置 ID={row.Id}, Type={row.GetSceneTypeEnum()}");
                return;
            }
        }

        Log.Warning($"ChangeSceneProcedure: 未找到场景 {sceneName} 的配置，使用默认配置");
        SceneStateManager.Instance.SetCurrentScene(0, SceneType.Unknown);
    }

    #endregion
}
