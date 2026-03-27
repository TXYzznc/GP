using System.Reflection;
using GameFramework.Fsm;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 游戏状态管理器 - 管理游戏主状态机
/// </summary>
public class GameStateManager : SingletonBase<GameStateManager>
{
    #region 单例已由基类提供

    // 使用 SingletonBase<GameStateManager> 提供的 Instance 属性

    #endregion

    #region 字段

    private IFsm<GameStateManager> m_MainFsm;

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前主状态
    /// </summary>
    public GameStateType CurrentState
    {
        get
        {
            if (m_MainFsm == null || m_MainFsm.CurrentState == null)
                return GameStateType.OutOfGame;

            if (m_MainFsm.CurrentState is OutOfGameState)
                return GameStateType.OutOfGame;

            if (m_MainFsm.CurrentState is InGameState)
                return GameStateType.InGame;

            return GameStateType.OutOfGame;
        }
    }

    /// <summary>
    /// 获取当前游戏内状态（仅在局内）
    /// </summary>
    public InGameStateType? CurrentInGameState
    {
        get
        {
            if (m_MainFsm?.CurrentState is InGameState inGameState)
            {
                return inGameState.CurrentSubState;
            }
            return null;
        }
    }

    /// <summary>
    /// 获取InGameState实例（仅在局内状态时有效）
    /// </summary>
    public InGameState GetInGameState()
    {
        return m_MainFsm?.CurrentState as InGameState;
    }

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        InitializeStateMachine();
    }

    protected override void OnDestroy()
    {
        // 销毁状态机
        if (m_MainFsm != null)
        {
            GF.Fsm.DestroyFsm(m_MainFsm);
            m_MainFsm = null;
        }

        base.OnDestroy();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化状态机
    /// </summary>
    private void InitializeStateMachine()
    {
        // 创建主状态机
        FsmState<GameStateManager>[] states = new FsmState<GameStateManager>[]
        {
            new OutOfGameState(),
            new InGameState(),
        };

        m_MainFsm = GF.Fsm.CreateFsm(this, states);

        DebugEx.LogModule("GameStateManager", "主状态机已创建");
    }

    #endregion

    #region 状态切换

    /// <summary>
    /// 切换到局外状态
    /// </summary>
    public void SwitchToOutOfGame()
    {
        if (m_MainFsm == null)
        {
            DebugEx.ErrorModule("GameStateManager", "状态机未初始化");
            return;
        }

        DebugEx.LogModule("GameStateManager", "切换到局外状态");

        // 如果状态机未运行，使用 Start，否则使用反射调用 ChangeState
        if (!m_MainFsm.IsRunning)
        {
            m_MainFsm.Start<OutOfGameState>();
        }
        else
        {
            ChangeStateByReflection<OutOfGameState>();
        }
    }

    /// <summary>
    /// 切换到局内状态
    /// </summary>
    public void SwitchToInGame()
    {
        if (m_MainFsm == null)
        {
            DebugEx.ErrorModule("GameStateManager", "状态机未初始化");
            return;
        }

        DebugEx.LogModule("GameStateManager", "切换到局内状态");

        // 如果状态机未运行，使用 Start，否则使用反射调用 ChangeState
        if (!m_MainFsm.IsRunning)
        {
            m_MainFsm.Start<InGameState>();
        }
        else
        {
            ChangeStateByReflection<InGameState>();
        }
    }

    /// <summary>
    /// 切换到探索状态（仅局内状态有效）
    /// </summary>
    public void SwitchToExploration()
    {
        if (m_MainFsm?.CurrentState is InGameState inGameState)
        {
            inGameState.SwitchToExploration();
        }
        else
        {
            DebugEx.WarningModule("GameStateManager", "当前不在局内状态，无法切换到探索状态");
        }
    }

    /// <summary>
    /// 切换到战斗准备状态（外部触发战斗时调用）
    /// </summary>
    public void SwitchToCombatPreparation()
    {
        if (m_MainFsm?.CurrentState is InGameState inGameState)
        {
            inGameState.SwitchToCombatPreparation();
        }
        else
        {
            DebugEx.WarningModule("GameStateManager", "当前不在局内状态，无法切换到战斗准备状态");
        }
    }

    /// <summary>
    /// 切换到战斗状态（由准备状态内部调用，外部一般不直接调用）
    /// </summary>
    public void SwitchToCombat()
    {
        if (m_MainFsm?.CurrentState is InGameState inGameState)
        {
            inGameState.SwitchToCombat();
        }
        else
        {
            DebugEx.WarningModule("GameStateManager", "当前不在局内状态，无法切换到战斗状态");
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 通过反射调用状态机的 ChangeState 方法
    /// </summary>
    private void ChangeStateByReflection<TState>()
        where TState : FsmState<GameStateManager>
    {
        try
        {
            // 获取 Fsm<T> 类型
            var fsmType = m_MainFsm.GetType();

            // 获取所有 ChangeState 方法
            var methods = fsmType.GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            // 查找无参数的泛型 ChangeState 方法
            MethodInfo targetMethod = null;
            foreach (var method in methods)
            {
                if (
                    method.Name == "ChangeState"
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 0
                )
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
                genericMethod.Invoke(m_MainFsm, null);

                DebugEx.LogModule("GameStateManager", $"成功切换到状态 {typeof(TState).Name}");
            }
            else
            {
                DebugEx.ErrorModule("GameStateManager", "未找到 ChangeState<T>() 方法");
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.ErrorModule("GameStateManager", $"切换状态失败 - {ex.Message}");
        }
    }

    #endregion
}
