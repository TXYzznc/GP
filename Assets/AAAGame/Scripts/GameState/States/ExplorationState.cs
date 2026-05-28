using GameFramework.Fsm;
using UnityGameFramework.Runtime;
using GameFramework;
using UnityEngine;

/// <summary>
/// 探索状态 - 玩家自由控制
/// </summary>
public class ExplorationState : FsmState<InGameState>
{
    #region FSM 生命周期

    protected override void OnInit(IFsm<InGameState> fsm)
    {
        base.OnInit(fsm);
        DebugEx.Log("ExplorationState", "初始化");
    }

    protected override void OnEnter(IFsm<InGameState> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.Log("ExplorationState", "进入探索状态");

        // ⭐ 恢复战斗前的视角模式
        RestoreCameraViewMode();

        // ⭐ 注意：玩家位置已在 CombatState.OnLeave() 中恢复，这里不需要再次恢复

        // ⭐ 注意：PlayerController 已在 CombatState.OnLeave() 中启用，这里不需要再次启用
        // 但为了保持状态一致性，仍然调用一次（幂等操作）
        EnablePlayerController();

        // 启用技能管理器
        EnablePlayerSkillManager();

        // 启用输入系统
        EnablePlayerInput();

        // 锁定光标
        LockCursor();

        // 触发探索进入事件
        GF.Event.Fire(this, ReferencePool.Acquire<ExplorationEnterEventArgs>());
    }

    protected override void OnLeave(IFsm<InGameState> fsm, bool isShutdown)
    {
        DebugEx.Log("ExplorationState", "离开探索状态");

        // 禁用玩家控制器
        DisablePlayerController();

        // 触发探索离开事件
        GF.Event.Fire(this, ReferencePool.Acquire<ExplorationLeaveEventArgs>());

        base.OnLeave(fsm, isShutdown);
    }

    protected override void OnUpdate(IFsm<InGameState> fsm, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

        // Tab 键开关背包
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.InventoryToggleTriggered)
        {
            ToggleInventory();
        }

        // Z 键开关操作指南
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.GuideToggleTriggered)
        {
            ToggleGuide();
        }
    }

    protected override void OnDestroy(IFsm<InGameState> fsm)
    {
        DebugEx.Log("ExplorationState", "销毁");
        base.OnDestroy(fsm);
    }

    #endregion

    #region 玩家控制器管理

    /// <summary>
    /// 启用玩家控制器
    /// </summary>
    private void EnablePlayerController()
    {
        // 通过 PlayerCharacterManager 获取当前玩家角色
        if (PlayerCharacterManager.Instance != null)
        {
            GameObject playerCharacter = PlayerCharacterManager.Instance.CurrentPlayerCharacter;
            if (playerCharacter != null)
            {
                PlayerController controller = playerCharacter.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.enabled = true;
                    DebugEx.Log("ExplorationState", "PlayerController 已启用");
                }
                else
                {
                    DebugEx.Warning("ExplorationState", "玩家角色上未找到 PlayerController 组件");
                }
            }
            else
            {
                DebugEx.Warning("ExplorationState", "未找到当前玩家角色");
            }
        }
        else
        {
            DebugEx.Warning("ExplorationState", "PlayerCharacterManager 未初始化");
        }
    }

    /// <summary>
    /// 禁用玩家控制器
    /// </summary>
    private void DisablePlayerController()
    {
        if (PlayerCharacterManager.Instance != null)
        {
            GameObject playerCharacter = PlayerCharacterManager.Instance.CurrentPlayerCharacter;
            if (playerCharacter != null)
            {
                PlayerController controller = playerCharacter.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.enabled = false;
                    DebugEx.Log("ExplorationState", "PlayerController 已禁用");
                }
            }
        }
    }

    #endregion

    #region 技能管理器管理

    /// <summary>
    /// 启用技能管理器
    /// </summary>
    private void EnablePlayerSkillManager()
    {
        var skillManager = Object.FindObjectOfType<PlayerSkillManager>();
        if (skillManager != null)
        {
            skillManager.enabled = true;
            DebugEx.Log("ExplorationState", "PlayerSkillManager 已启用");
        }
        else
        {
            DebugEx.Warning("ExplorationState", "未找到 PlayerSkillManager");
        }
    }

    #endregion

    #region 输入系统管理

    /// <summary>
    /// 启用输入系统
    /// </summary>
    private void EnablePlayerInput()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetEnable(true);
            DebugEx.Log("ExplorationState", "PlayerInputManager 已启用");
        }
        else
        {
            DebugEx.Warning("ExplorationState", "PlayerInputManager 未初始化");
        }
    }

    #endregion

    #region 光标管理

    /// <summary>
    /// 锁定光标
    /// </summary>
    private void LockCursor()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorLock(true);
            DebugEx.Log("ExplorationState", "光标已锁定");
        }
    }

    #endregion

    #region 背包开关

    private int m_InventoryFormId = -1;

    private void ToggleInventory()
    {
        if (GF.UI.HasUIForm(m_InventoryFormId))
        {
            GF.UI.CloseUIForm(m_InventoryFormId);
            m_InventoryFormId = -1;
            DebugEx.Log("ExplorationState", "关闭背包");
        }
        else
        {
            m_InventoryFormId = GF.UI.OpenUIForm(UIViews.InventoryUI);
            DebugEx.Log("ExplorationState", "打开背包");
        }
    }

    #endregion

    #region 操作指南开关

    private int m_GuideFormId = -1;

    private void ToggleGuide()
    {
        if (GF.UI.HasUIForm(m_GuideFormId))
        {
            GF.UI.CloseUIForm(m_GuideFormId);
            m_GuideFormId = -1;
            DebugEx.Log("ExplorationState", "关闭操作指南");
        }
        else
        {
            m_GuideFormId = GF.UI.OpenUIForm(UIViews.GuideUI);
            DebugEx.Log("ExplorationState", "打开操作指南");
        }
    }

    #endregion

    #region 相机管理

    /// <summary>
    /// 恢复相机视角模式
    /// </summary>
    private void RestoreCameraViewMode()
    {
        ThirdPersonCamera cameraController = CameraRegistry.ThirdPersonCamera;
        if (cameraController != null)
        {
            cameraController.RestoreCachedViewMode();
            DebugEx.Log("ExplorationState", "已恢复战斗前的视角模式");
        }
        else
        {
            DebugEx.Warning("ExplorationState", "未找到第三人称相机控制器");
        }
    }

    #endregion
}
