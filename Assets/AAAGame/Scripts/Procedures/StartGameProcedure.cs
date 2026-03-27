using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

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

        // 初始化游戏状态管理器，切换到游戏外状态
        GameStateManager.Instance.SwitchToOutOfGame();
        Log.Info("StartGameProcedure: 已切换到游戏外状态");

        // TODO: 这里可以添加其他游戏开始的逻辑
        // 例如：
        // - 打开游戏 UI
        // - 播放音乐
        // - 开始游戏动画
        // - 加载背景音乐

        // 示例：打开一个游戏 UI（根据项目的话）
        // GF.UI.OpenUIForm(UIViews.GameUI);

        // 示例：播放背景音乐
        // GF.Sound.PlayMusic("bgm/game_music.mp3");

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
        // TODO: 离开游戏流程时的清理工作
        // 例如：
        // - 关闭游戏 UI
        // - 清理游戏数据
        // - 停止音乐

        GF.Log("离开游戏流程 - StartGame");

        s_ProcedureOwner = null;

        base.OnLeave(procedureOwner, isShutdown);
    }
}
