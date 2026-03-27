using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子部署状态追踪器
/// 追踪在战斗准备阶段和战斗阶段的棋子部署状态（未出战/已出战/已死亡）
/// </summary>
public class ChessDeploymentTracker
{
    #region 单例

    private static ChessDeploymentTracker s_Instance;
    public static ChessDeploymentTracker Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ChessDeploymentTracker();
            }
            return s_Instance;
        }
    }

    private ChessDeploymentTracker() { }

    #endregion

    #region 数据结构

    /// <summary>
    /// 棋子实例数据
    /// </summary>
    public class ChessInstanceData
    {
        public int ChessId;              // 棋子配置ID
        public string InstanceId;        // 实例唯一ID（用于区分同一配置的多个实例）
        public bool IsDeployed;          // 是否已出战
        public bool IsDead;              // 是否已死亡（死亡后不可再使用）
        public ChessEntity Entity;       // 棋子实体引用（未出战时为null）

        public ChessInstanceData(int chessId)
        {
            ChessId = chessId;
            InstanceId = Guid.NewGuid().ToString();
            IsDeployed = false;
            IsDead = false;
            Entity = null;
        }

        /// <summary>
        /// 是否可以出战（未出战且未死亡）
        /// </summary>
        public bool CanDeploy => !IsDeployed && !IsDead;
    }

    #endregion

    #region 私有字段

    /// <summary>所有棋子实例（包括已出战和已出战）</summary>
    private List<ChessInstanceData> m_AllChessInstances = new List<ChessInstanceData>();

    /// <summary>实例ID到数据的映射</summary>
    private Dictionary<string, ChessInstanceData> m_InstanceDict = new Dictionary<string, ChessInstanceData>();

    /// <summary>Entity到实例ID的反向映射</summary>
    private Dictionary<ChessEntity, string> m_EntityToInstanceId = new Dictionary<ChessEntity, string>();

    /// <summary>是否已初始化（一场游戏只初始化一次）(重新进入时刷新)</summary>
    private bool m_IsInitialized = false;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化库存（从备战列表加载）
    /// 一场游戏只初始化一次，不清空用户数据
    /// </summary>
    public void Initialize()
    {
        // 如果已初始化，不做任何操作，保持当前状态
        if (m_IsInitialized)
        {
            DebugEx.Log($"[ChessDeploymentTracker] 已初始化，保持当前状态 (已出战={GetDeployedCount()}, 未出战={GetAvailableCount()})");
            return;
        }

        Clear();

        // 从出战资源提供者获取备战棋子列表
        var preparedChessIds = BattleLoadoutProvider.Instance.GetPreparedChessIds();

        foreach (var chessId in preparedChessIds)
        {
            var instance = new ChessInstanceData(chessId);
            m_AllChessInstances.Add(instance);
            m_InstanceDict[instance.InstanceId] = instance;
        }

        m_IsInitialized = true;

        // 订阅全局血量恢复事件（回基地时自动清除死亡标志）
        ChessStateEvents.OnAllChessHPRestored -= OnAllChessHPRestored;
        ChessStateEvents.OnAllChessHPRestored += OnAllChessHPRestored;

        DebugEx.Log($"[ChessDeploymentTracker] 初始化完成，共 {m_AllChessInstances.Count} 个棋子实例");
    }

    /// <summary>
    /// 重置所有棋子的出战状态（保留实体数据）
    /// </summary>
    public void ResetDeploymentState()
    {
        foreach (var instance in m_AllChessInstances)
        {
            instance.IsDeployed = false;
            instance.Entity = null;
        }
        m_EntityToInstanceId.Clear();
        DebugEx.Log($"[ChessDeploymentTracker] 已重置所有棋子的出战状态");
    }

    /// <summary>
    /// 战斗结束时调用，销毁场景实体引用
    /// 重置出战状态，保留死亡状态
    /// </summary>
    public void OnBattleEnd()
    {
        foreach (var instance in m_AllChessInstances)
        {
            // 只重置出战状态和实体引用，保留死亡状态
            instance.IsDeployed = false;
            instance.Entity = null;
        }
        m_EntityToInstanceId.Clear();
        DebugEx.Log($"[ChessDeploymentTracker] 战斗结束，已重置出战状态 (存活={GetAvailableCount()}, 死亡={GetDeadCount()})");
    }

    /// <summary>
    /// 标记棋子死亡
    /// </summary>
    public void MarkChessDead(string instanceId)
    {
        var instance = GetInstance(instanceId);
        if (instance != null)
        {
            instance.IsDead = true;
            DebugEx.Log($"[ChessDeploymentTracker] 棋子死亡: chessId={instance.ChessId}, instanceId={instanceId}");
            OnChessDied?.Invoke(instanceId);
        }
    }

    /// <summary>
    /// 获取已死亡棋子数量
    /// </summary>
    public int GetDeadCount()
    {
        int count = 0;
        foreach (var instance in m_AllChessInstances)
        {
            if (instance.IsDead)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 重置所有棋子的死亡状态（回到基地、全体血量恢复后调用）
    /// </summary>
    public void ResetAllDeathState()
    {
        int resetCount = 0;
        foreach (var instance in m_AllChessInstances)
        {
            if (instance.IsDead)
            {
                instance.IsDead = false;
                resetCount++;
            }
        }
        DebugEx.LogModule("ChessDeploymentTracker", $"死亡状态已重置：{resetCount} 个棋子复活");
        OnDeathStateReset?.Invoke();
    }

    /// <summary>
    /// 死亡状态重置事件（ResetAllDeathState 后触发，UI 可订阅此事件刷新显示）
    /// </summary>
    public event System.Action OnDeathStateReset;

    /// <summary>
    /// 清空库存
    /// </summary>
    public void Clear()
    {
        // 取消订阅全局事件
        ChessStateEvents.OnAllChessHPRestored -= OnAllChessHPRestored;

        m_AllChessInstances.Clear();
        m_InstanceDict.Clear();
        m_EntityToInstanceId.Clear();  // 清空反向映射
        m_IsInitialized = false;  // 重置初始化标志
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 获取所有棋子实例（包括已出战和未出战）
    /// </summary>
    public List<ChessInstanceData> GetAllChessInstances()
    {
        return new List<ChessInstanceData>(m_AllChessInstances);
    }

    /// <summary>
    /// 获取所有未出战的棋子实例
    /// </summary>
    public List<ChessInstanceData> GetAvailableChess()
    {
        var result = new List<ChessInstanceData>();
        foreach (var instance in m_AllChessInstances)
        {
            if (!instance.IsDeployed)
            {
                result.Add(instance);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取所有已出战的棋子实例
    /// </summary>
    public List<ChessInstanceData> GetDeployedChess()
    {
        var result = new List<ChessInstanceData>();
        foreach (var instance in m_AllChessInstances)
        {
            if (instance.IsDeployed)
            {
                result.Add(instance);
            }
        }
        return result;
    }

    /// <summary>
    /// 根据实例ID获取数据
    /// </summary>
    public ChessInstanceData GetInstance(string instanceId)
    {
        m_InstanceDict.TryGetValue(instanceId, out var instance);
        return instance;
    }

    /// <summary>
    /// 获取未出战棋子数量
    /// </summary>
    public int GetAvailableCount()
    {
        int count = 0;
        foreach (var instance in m_AllChessInstances)
        {
            if (!instance.IsDeployed)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 获取已出战棋子数量
    /// </summary>
    public int GetDeployedCount()
    {
        int count = 0;
        foreach (var instance in m_AllChessInstances)
        {
            if (instance.IsDeployed)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region 出战操作

    /// <summary>
    /// 标记棋子为已出战
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="entity">棋子实体引用</param>
    /// <returns>是否成功</returns>
    public bool DeployChess(string instanceId, ChessEntity entity)
    {
        var instance = GetInstance(instanceId);
        if (instance == null)
        {
            DebugEx.Error($"[ChessDeploymentTracker] DeployChess failed: instance not found, instanceId={instanceId}");
            return false;
        }

        if (!instance.CanDeploy)
        {
            DebugEx.Warning($"[ChessDeploymentTracker] DeployChess failed: cannot deploy (IsDeployed={instance.IsDeployed}, IsDead={instance.IsDead}), instanceId={instanceId}");
            return false;
        }

        instance.IsDeployed = true;
        instance.Entity = entity;

        // 添加反向映射
        if (entity != null)
        {
            m_EntityToInstanceId[entity] = instanceId;
        }

        // 触发事件
        OnChessDeployed?.Invoke(instance);

        DebugEx.Log($"[ChessDeploymentTracker] DeployChess success: chessId={instance.ChessId}, instanceId={instanceId}");
        return true;
    }

    /// <summary>
    /// 召回棋子，从场景中移除并返回到备战列表
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>是否成功</returns>
    public bool RecallChess(string instanceId)
    {
        var instance = GetInstance(instanceId);
        if (instance == null)
        {
            DebugEx.Error($"[ChessDeploymentTracker] RecallChess failed: instance not found, instanceId={instanceId}");
            return false;
        }

        if (!instance.IsDeployed)
        {
            DebugEx.Warning($"[ChessDeploymentTracker] RecallChess failed: not deployed, instanceId={instanceId}");
            return false;
        }

        // 移除反向映射
        if (instance.Entity != null)
        {
            m_EntityToInstanceId.Remove(instance.Entity);
        }

        instance.IsDeployed = false;
        instance.Entity = null;

        // 触发事件
        OnChessRecalled?.Invoke(instance);

        DebugEx.Log($"[ChessDeploymentTracker] RecallChess success: chessId={instance.ChessId}, instanceId={instanceId}");
        return true;
    }

    /// <summary>
    /// 根据 Entity 获取实例ID
    /// </summary>
    public string GetInstanceIdByEntity(ChessEntity entity)
    {
        if (entity != null && m_EntityToInstanceId.TryGetValue(entity, out var instanceId))
        {
            return instanceId;
        }
        return null;
    }

    #endregion

    #region 事件

    /// <summary>棋子出战事件</summary>
    public event Action<ChessInstanceData> OnChessDeployed;

    /// <summary>棋子召回事件</summary>
    public event Action<ChessInstanceData> OnChessRecalled;

    /// <summary>棋子死亡事件（参数为 instanceId）</summary>
    public event Action<string> OnChessDied;

    /// <summary>
    /// 全局血量全员恢复事件处理（由 ChessStateEvents.OnAllChessHPRestored 触发）
    /// 自动清除所有棋子的死亡标志，使其可在下一局被再次使用
    /// </summary>
    private void OnAllChessHPRestored()
    {
        ResetAllDeathState();
    }

    #endregion

    #region 调试

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[ChessDeploymentTracker] Total={m_AllChessInstances.Count}, Available={GetAvailableCount()}, Deployed={GetDeployedCount()}";
    }

    #endregion
}
