using GameFramework.Fsm;
using UnityGameFramework.Runtime;
using GameFramework;

/// <summary>
/// 局外状态 - 主菜单、角色选择等
/// </summary>
public class OutOfGameState : FsmState<GameStateManager>
{
    private int m_InventoryFormId = -1;
    private int m_WarehouseFormId = -1;

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

    protected override void OnUpdate(IFsm<GameStateManager> fsm, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

        var input = PlayerInputManager.Instance;
        if (input == null)
            return;

        if (input.InventoryToggleTriggered)
            ToggleInventory();

        if (input.WarehouseToggleTriggered)
            ToggleWarehouse();
    }

    protected override void OnLeave(IFsm<GameStateManager> fsm, bool isShutdown)
    {
        DebugEx.LogModule("OutOfGameState", "离开局外状态");

        // 关闭打开的菜单UI
        if (GF.UI.HasUIForm(m_InventoryFormId))
        {
            GF.UI.CloseUIForm(m_InventoryFormId);
            m_InventoryFormId = -1;
        }

        if (GF.UI.HasUIForm(m_WarehouseFormId))
        {
            GF.UI.CloseUIForm(m_WarehouseFormId);
            m_WarehouseFormId = -1;
        }

        // 触发离开局外状态事件
        GF.Event.Fire(this, ReferencePool.Acquire<OutOfGameLeaveEventArgs>());

        base.OnLeave(fsm, isShutdown);
    }

    protected override void OnDestroy(IFsm<GameStateManager> fsm)
    {
        DebugEx.LogModule("OutOfGameState", "销毁");
        base.OnDestroy(fsm);
    }

    private void ToggleInventory()
    {
        if (GF.UI.HasUIForm(m_InventoryFormId))
        {
            GF.UI.CloseUIForm(m_InventoryFormId);
            m_InventoryFormId = -1;
            DebugEx.LogModule("OutOfGameState", "关闭背包");
        }
        else
        {
            m_InventoryFormId = GF.UI.OpenUIForm(UIViews.InventoryUI);
            DebugEx.LogModule("OutOfGameState", "打开背包");
        }
    }

    private void ToggleWarehouse()
    {
        if (GF.UI.HasUIForm(m_WarehouseFormId))
        {
            GF.UI.CloseUIForm(m_WarehouseFormId);
            m_WarehouseFormId = -1;
            DebugEx.LogModule("OutOfGameState", "关闭仓库");
        }
        else
        {
            m_WarehouseFormId = GF.UI.OpenUIForm(UIViews.WarehouseUI);
            DebugEx.LogModule("OutOfGameState", "打开仓库");
        }
    }
}
