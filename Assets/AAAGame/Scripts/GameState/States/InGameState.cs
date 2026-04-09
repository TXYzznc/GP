using GameFramework.Fsm;
using UnityGameFramework.Runtime;
using GameFramework;
using GameFramework.Event;
using System.Reflection;

/// <summary>
/// 局内状态 - 游戏进行中
/// </summary>
public class InGameState : FsmState<GameStateManager>
{
    #region 字段

    private IFsm<InGameState> m_SubFsm;

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前子状态
    /// </summary>
    public InGameStateType CurrentSubState
    {
        get
        {
            if (m_SubFsm == null || m_SubFsm.CurrentState == null)
                return InGameStateType.Exploration;

            if (m_SubFsm.CurrentState is ExplorationState)
                return InGameStateType.Exploration;

            if (m_SubFsm.CurrentState is CombatPreparationState)
                return InGameStateType.CombatPreparation;

            if (m_SubFsm.CurrentState is CombatState)
                return InGameStateType.Combat;

            return InGameStateType.Exploration;
        }
    }

    #endregion

    #region FSM 生命周期

    protected override void OnInit(IFsm<GameStateManager> fsm)
    {
        base.OnInit(fsm);
        DebugEx.LogModule("InGameState", "初始化");
    }

    protected override void OnEnter(IFsm<GameStateManager> fsm)
    {
        base.OnEnter(fsm);
        DebugEx.LogModule("InGameState", "进入局内状态");

        // 初始化玩家运行时数据管理器
        PlayerRuntimeDataManager.Instance.Initialize();

        // ⭐ 新增：初始化棋子库存（进入局内时就加载备战阵容）
        ChessDeploymentTracker.Instance.Initialize();
        DebugEx.LogModule("InGameState", "棋子库存已初始化");

        // ⭐ 新增：为所有棋子初始化全局状态（满血）
        // 这样在准备阶段选中棋子时已有全局状态，无需临时创建
        InitializeAllChessGlobalStates();

        // 初始化背包与仓库（容量从 PlayerInitTable 读取）
        InitInventoryAndWarehouse();

        // 订阅战斗结束事件
        GF.Event.Subscribe(CombatEndEventArgs.EventId, OnCombatEnd);

        // 创建子状态机
        CreateSubStateMachine();

        // 触发进入局内状态事件
        GF.Event.Fire(this, ReferencePool.Acquire<InGameEnterEventArgs>());

        // 子状态机启动，默认进入探索状态
        if (m_SubFsm != null)
        {
            m_SubFsm.Start<ExplorationState>();
        }
    }

    protected override void OnLeave(IFsm<GameStateManager> fsm, bool isShutdown)
    {
        DebugEx.LogModule("InGameState", "离开局内状态");

        // 取消订阅战斗结束事件
        GF.Event.Unsubscribe(CombatEndEventArgs.EventId, OnCombatEnd);

        // 停止并销毁子状态机
        if (!isShutdown)
        {
            DestroySubStateMachine();
        }

        // 清理玩家运行时数据管理器
        PlayerRuntimeDataManager.Instance.Cleanup();

        // 清理仓库数据（WarehouseManager 是纯 C# 单例，需手动清理）
        WarehouseManager.Instance.Cleanup();

        // 回到基地（局外）：恢复所有棋子血量到满值（含已死亡棋子）
        GlobalChessManager.Instance.RestoreAllChessHP();

        // 触发离开局内状态事件
        GF.Event.Fire(this, ReferencePool.Acquire<InGameLeaveEventArgs>());

        base.OnLeave(fsm, isShutdown);
    }

    protected override void OnDestroy(IFsm<GameStateManager> fsm)
    {
        DebugEx.LogModule("InGameState", "销毁");
        base.OnDestroy(fsm);
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理战斗结束事件
    /// </summary>
    private void OnCombatEnd(object sender, GameEventArgs e)
    {
        CombatEndEventArgs eventArgs = (CombatEndEventArgs)e;
        DebugEx.LogModule("InGameState", $"收到战斗结束事件 - {(eventArgs.IsVictory ? "胜利" : "失败")}");

        // 如果已经不在战斗状态，忽略此事件（防止测试强制退出后重复触发）
        if (!(m_SubFsm?.CurrentState is CombatState))
        {
            DebugEx.WarningModule("InGameState", "当前不在战斗状态，忽略战斗结束事件");
            return;
        }

        // ⭐ 在切换状态前先恢复玩家位置
        if (PlayerCharacterManager.Instance != null)
        {
            PlayerCharacterManager.Instance.RestorePositionAfterCombat();
        }

        var uiParams = UIParams.Create(false);
        uiParams.Set<VarBoolean>(EndCombatUI.P_IsVictory, eventArgs.IsVictory);
        int formId = GF.UI.OpenUIForm(UIViews.EndCombatUI, uiParams);
        if (formId == -1)
        {
            SwitchToExploration();
        }
    }

    #endregion

    #region 子状态机管理

    /// <summary>
    /// 创建子状态机
    /// </summary>
    private void CreateSubStateMachine()
    {
        if (m_SubFsm != null)
        {
            DebugEx.WarningModule("InGameState", "子状态机已存在");
            return;
        }

        // 创建子状态
        FsmState<InGameState>[] subStates = new FsmState<InGameState>[]
        {
            new ExplorationState(),
            new CombatPreparationState(),
            new CombatState()
        };

        // 创建子状态机
        m_SubFsm = GF.Fsm.CreateFsm(this, subStates);

        DebugEx.LogModule("InGameState", "子状态机已创建");
    }

    /// <summary>
    /// 销毁子状态机
    /// </summary>
    private void DestroySubStateMachine()
    {
        if (m_SubFsm == null)
            return;

        GF.Fsm.DestroyFsm(m_SubFsm);
        m_SubFsm = null;

        DebugEx.LogModule("InGameState", "子状态机已销毁");
    }

    #endregion

    #region 状态切换

    /// <summary>
    /// 切换到探索状态
    /// </summary>
    public void SwitchToExploration()
    {
        if (m_SubFsm == null)
        {
            DebugEx.ErrorModule("InGameState", "子状态机未初始化");
            return;
        }

        DebugEx.LogModule("InGameState", "切换到探索状态");

        // 如果状态机未运行，使用 Start，否则使用反射调用 ChangeState
        if (!m_SubFsm.IsRunning)
        {
            m_SubFsm.Start<ExplorationState>();
        }
        else
        {
            ChangeSubStateByReflection<ExplorationState>();
        }
    }

    /// <summary>
    /// 切换到战斗准备状态（外部触发战斗时调用）
    /// </summary>
    public void SwitchToCombatPreparation()
    {
        if (m_SubFsm == null)
        {
            DebugEx.ErrorModule("InGameState", "子状态机未初始化");
            return;
        }

        DebugEx.LogModule("InGameState", "切换到战斗准备状态");

        if (!m_SubFsm.IsRunning)
        {
            m_SubFsm.Start<CombatPreparationState>();
        }
        else
        {
            ChangeSubStateByReflection<CombatPreparationState>();
        }
    }

    /// <summary>
    /// 切换到战斗状态
    /// </summary>
    public void SwitchToCombat()
    {
        if (m_SubFsm == null)
        {
            DebugEx.ErrorModule("InGameState", "子状态机未初始化");
            return;
        }

        DebugEx.LogModule("InGameState", "切换到战斗状态");

        // 如果状态机未运行，使用 Start，否则使用反射调用 ChangeState
        if (!m_SubFsm.IsRunning)
        {
            m_SubFsm.Start<CombatState>();
        }
        else
        {
            ChangeSubStateByReflection<CombatState>();
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 为所有棋子初始化全局状态（满血）
    /// 进入局内时为所有备战棋子创建全局状态，确保准备阶段有真实的全局数据
    /// </summary>
    private void InitializeAllChessGlobalStates()
    {
        var allInstances = ChessDeploymentTracker.Instance.GetAllChessInstances();
        if (allInstances == null || allInstances.Count == 0)
        {
            DebugEx.WarningModule("InGameState", "没有棋子实例，无法初始化全局状态");
            return;
        }

        int initializedCount = 0;
        foreach (var instance in allInstances)
        {
            if (ChessDataManager.Instance.TryGetConfig(instance.ChessId, out var config))
            {
                // 如果还没注册过，就注册满血状态
                if (GlobalChessManager.Instance.GetChessState(instance.ChessId) == null)
                {
                    GlobalChessManager.Instance.RegisterChess(instance.ChessId, config.MaxHp);
                    initializedCount++;
                }
            }
        }

        DebugEx.LogModule("InGameState", $"棋子全局状态初始化完成 - 共{initializedCount}个棋子");
    }

    /// <summary>
    /// 初始化背包与仓库，容量从 PlayerInitTable 读取
    /// </summary>
    private void InitInventoryAndWarehouse()
    {
        int warehouseCapacity = 50; // 默认值

        var initTable = GF.DataTable.GetDataTable<PlayerInitTable>();
        if (initTable != null)
        {
            var initConfig = initTable.GetDataRow(1);
            if (initConfig != null)
                warehouseCapacity = initConfig.InitWarehouseCapacity;
        }

        // InventoryManager 是 MonoBehaviour 单例，在 Awake 中自动初始化，无需手动调用
        WarehouseManager.Instance.Initialize(warehouseCapacity);

        DebugEx.LogModule("InGameState", $"仓库初始化完成 - 容量:{warehouseCapacity}");
    }

    /// <summary>
    /// 通过反射调用子状态机的 ChangeState 方法
    /// </summary>
    private void ChangeSubStateByReflection<TState>() where TState : FsmState<InGameState>
    {
        try
        {
            // 获取 Fsm<T> 类型
            var fsmType = m_SubFsm.GetType();

            // 获取所有 ChangeState 方法
            var methods = fsmType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // 查找无参数的泛型 ChangeState 方法
            MethodInfo targetMethod = null;
            foreach (var method in methods)
            {
                if (method.Name == "ChangeState" &&
                    method.IsGenericMethodDefinition &&
                    method.GetParameters().Length == 0)
                {
                    targetMethod = method;
                    break;
                }
            }

            if (targetMethod != null)
            {
                // 构造泛型方法
                var genericMethod = targetMethod.MakeGenericMethod(typeof(TState));

                // 调用方法
                genericMethod.Invoke(m_SubFsm, null);

                DebugEx.LogModule("InGameState", $"成功切换到子状态 {typeof(TState).Name}");
            }
            else
            {
                DebugEx.ErrorModule("InGameState", "未找到 ChangeState<T>() 方法");
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.ErrorModule("InGameState", $"切换子状态失败 - {ex.Message}");
        }
    }

    #endregion
}
