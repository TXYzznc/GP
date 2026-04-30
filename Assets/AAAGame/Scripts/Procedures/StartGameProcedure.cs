using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using AAAGame.Audio;

/// <summary>
/// 游戏开始流程 - 主菜单游戏逻辑处理
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class StartGameProcedure : ProcedureBase
{
    private static IFsm<IProcedureManager> s_ProcedureOwner;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        s_ProcedureOwner = procedureOwner;

        GF.Log("进入游戏流程 - StartGame");

        // 输出启动性能诊断信息
        StartupPerformanceProfiler.OnGameReady();

        // 初始化音乐系统
        InitializeAudioSystem();

        // 初始化游戏状态管理器，切换到主菜单状态
        GameStateManager.Instance.SwitchToMenu();
        Log.Info("StartGameProcedure: 已切换到主菜单状态");

        // 播放主菜单 BGM
        AudioEventListener.Instance?.PlayBGMForProcedure("StartGameProcedure");

        GF.UI.OpenUIForm(UIViews.StartMenuUI);
    }

    /// <summary>
    /// 切换场景（外部调用）
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public static void RequestChangeScene(string sceneName)
    {
        if (s_ProcedureOwner == null)
        {
            Log.Error("StartGameProcedure 未初始化，无法切换场景");
            return;
        }

        Log.Info($"请求切换场景: {sceneName}");

        // 设置场景名流程参数
        s_ProcedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, sceneName);

        // 获取当前 Procedure 并切换到 ChangeSceneProcedure
        var currentProcedure = s_ProcedureOwner.CurrentState as StartGameProcedure;
        if (currentProcedure != null)
        {
            currentProcedure.ChangeState<ChangeSceneProcedure>(s_ProcedureOwner);
        }
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        // TODO: 游戏循环逻辑
        // 例如：
        // - 检查游戏状态
        // - 更新游戏数据
        // - 处理游戏逻辑
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GF.Log("离开游戏流程 - StartGame");
        s_ProcedureOwner = null;
        base.OnLeave(procedureOwner, isShutdown);
    }

    /// <summary>
    /// 初始化音乐系统
    /// </summary>
    private void InitializeAudioSystem()
    {
        if (AudioManager.Instance != null)
        {
            Log.Info("StartGameProcedure: AudioManager 已初始化");
            return;
        }

        var audioManagerGo = new GameObject("AudioManager");
        var audioManager = audioManagerGo.AddComponent<AudioManager>();
        Object.DontDestroyOnLoad(audioManagerGo);

        var audioListenerGo = new GameObject("AudioEventListener");
        audioListenerGo.transform.SetParent(audioManagerGo.transform);
        audioListenerGo.AddComponent<AudioEventListener>();

        Log.Info("StartGameProcedure: 音乐系统已初始化");
    }
}
