using GameFramework.Fsm;
using UnityGameFramework.Runtime;
using GameFramework;

/// <summary>
/// 局外状态 - 主菜单、角色选择等
/// </summary>
public class OutOfGameState : FsmState<GameStateManager>
{
    protected override void OnInit(IFsm<GameStateManager> fsm)
    {
        base.OnInit(fsm);
        DebugEx.LogModule("OutOfGameState", "初始化");
    }

    protected override void OnEnter(IFsm<GameStateManager> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.LogModule("OutOfGameState", "进入局外状态");

        // 触发进入局外状态事件
        GF.Event.Fire(this, ReferencePool.Acquire<OutOfGameEnterEventArgs>());
    }

    protected override void OnLeave(IFsm<GameStateManager> fsm, bool isShutdown)
    {
        DebugEx.LogModule("OutOfGameState", "离开局外状态");

        // 触发离开局外状态事件
        GF.Event.Fire(this, ReferencePool.Acquire<OutOfGameLeaveEventArgs>());

        base.OnLeave(fsm, isShutdown);
    }

    protected override void OnDestroy(IFsm<GameStateManager> fsm)
    {
        DebugEx.LogModule("OutOfGameState", "销毁");
        base.OnDestroy(fsm);
    }
}
