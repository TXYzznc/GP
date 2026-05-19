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
        DebugEx.Log("OutOfGameState", "初始化");
    }

    protected override void OnEnter(IFsm<GameStateManager> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.Log("OutOfGameState", "进入局外状态");

        // 局外也需要玩家可以行走，启用 PlayerController
        // 注意：角色可能还未生成（异步加载中），此处先尝试启用，角色生成后由 OnCharacterSpawned 再次确保启用
        EnablePlayerController();

        // 触发进入局外状态事件
        GF.Event.Fire(this, ReferencePool.Acquire<OutOfGameEnterEventArgs>());
    }

    private void EnablePlayerController()
    {
        if (PlayerCharacterManager.Instance == null) return;
        var character = PlayerCharacterManager.Instance.CurrentPlayerCharacter;
        if (character == null) return;
        var controller = character.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
            DebugEx.Log("OutOfGameState", "PlayerController 已启用");
        }
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
        DebugEx.Log("OutOfGameState", "离开局外状态");

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
        DebugEx.Log("OutOfGameState", "销毁");
        base.OnDestroy(fsm);
    }

    private void ToggleInventory()
    {
        if (GF.UI.HasUIForm(m_InventoryFormId))
        {
            GF.UI.CloseUIForm(m_InventoryFormId);
            m_InventoryFormId = -1;
            DebugEx.Log("OutOfGameState", "关闭背包");
        }
        else
        {
            m_InventoryFormId = GF.UI.OpenUIForm(UIViews.InventoryUI);
            DebugEx.Log("OutOfGameState", "打开背包");
        }
    }

    private void ToggleWarehouse()
    {
        if (GF.UI.HasUIForm(m_WarehouseFormId))
        {
            GF.UI.CloseUIForm(m_WarehouseFormId);
            m_WarehouseFormId = -1;
            DebugEx.Log("OutOfGameState", "关闭仓库");
        }
        else
        {
            m_WarehouseFormId = GF.UI.OpenUIForm(UIViews.WarehouseUI);
            DebugEx.Log("OutOfGameState", "打开仓库");
        }
    }
}
