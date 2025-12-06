using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

/// <summary>
/// 游戏开始流程 - 你的游戏主逻辑在这里
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class StartGameProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        
        GF.Log("进入游戏流程 - StartGame");
        
        // TODO: 在这里添加你的游戏初始化逻辑
        // 例如：
        // - 打开游戏 UI
        // - 生成玩家
        // - 初始化游戏数据
        // - 播放背景音乐
        
        // 示例：打开一个游戏 UI（如果你有的话）
        // GF.UI.OpenUIForm(UIViews.GameUI);
        
        // 示例：播放背景音乐
        // GF.Sound.PlayMusic("bgm/game_music.mp3");
        
        GF.UI.OpenUIForm(UIViews.StartMenuUI);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        
        // TODO: 游戏主循环逻辑
        // 例如：
        // - 检测游戏状态
        // - 处理游戏输入
        // - 更新游戏逻辑
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        // TODO: 离开游戏流程时的清理工作
        // 例如：
        // - 关闭游戏 UI
        // - 清理游戏对象
        // - 停止音乐
        
        GF.Log("离开游戏流程 - StartGame");
        
        base.OnLeave(procedureOwner, isShutdown);
    }
}
