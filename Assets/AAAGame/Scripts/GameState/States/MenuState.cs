using GameFramework.Fsm;

/// <summary>
/// 主菜单状态
/// </summary>
public class MenuState : FsmState<GameStateManager>
{
    protected override void OnEnter(IFsm<GameStateManager> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.LogModule("MenuState", "进入主菜单状态");
    }

    protected override void OnLeave(IFsm<GameStateManager> fsm, bool isShutdown)
    {
        DebugEx.LogModule("MenuState", "离开主菜单状态");
        base.OnLeave(fsm, isShutdown);
    }
}
