using System;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

/// <summary>
/// 羁绊管理器
/// 负责检测和管理宝物羁绊和棋子羁绊的激活/失活
/// 宝物羁绊：基于棋子装备的宝物组合
/// 棋子羁绊：基于场上出战棋子的种族/职业组合
/// </summary>
public class SynergyManager : SingletonBase<SynergyManager>
{
    #region 事件

    /// <summary>
    /// 羁绊状态变化事件
    /// </summary>
    public event Action<int, bool> OnSynergyStateChanged;

    #endregion

    #region 字段

    /// <summary>
    /// 棋子的激活中宝物羁绊 (chessId → 激活中的羁绊ID集合)
    /// </summary>
    private Dictionary<int, HashSet<int>> m_ActiveTreasureSynergies = new();

    /// <summary>
    /// 当前激活中的棋子羁绊ID集合
    /// </summary>
    private HashSet<int> m_ActiveChessSynergies = new();

    /// <summary>
    /// 羁绊效果对应的 Buff ID 映射（synergyId → buffIds）
    /// 用于失活时移除对应的 Buff
    /// </summary>
    private Dictionary<int, List<int>> m_SynergyBuffMapping = new();

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();

        // 订阅宝物装备变化事件
        ChessStateEvents.OnEquipmentChanged += OnEquipmentChanged;

        // 订阅棋子上阵/撤出事件
        if (ChessDeploymentTracker.Instance != null)
        {
            ChessDeploymentTracker.Instance.OnChessDeployed += OnChessDeployed;
            ChessDeploymentTracker.Instance.OnChessRecalled += OnChessRecalled;
        }
        else
        {
            DebugEx.Warning(nameof(SynergyManager), "ChessDeploymentTracker.Instance 为 null");
        }

        DebugEx.Log(nameof(SynergyManager), "羁绊管理器已初始化");
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 棋子装备变化处理
    /// </summary>
    private void OnEquipmentChanged(int chessId, int slotIndex)
    {
        DebugEx.Log(nameof(SynergyManager), $"棋子 [{chessId}] 装备槽 [{slotIndex}] 变化，重新检测宝物羁绊");
        RefreshTreasureSynergies(chessId);
    }

    /// <summary>
    /// 棋子上阵处理
    /// </summary>
    private void OnChessDeployed(object chessData)
    {
        DebugEx.Log(nameof(SynergyManager), $"棋子上阵，重新检测棋子羁绊");
        RefreshChessSynergies();
    }

    /// <summary>
    /// 棋子撤出处理
    /// </summary>
    private void OnChessRecalled(object chessData)
    {
        DebugEx.Log(nameof(SynergyManager), $"棋子撤出，重新检测棋子羁绊");
        RefreshChessSynergies();
    }

    #endregion

    #region 宝物羁绊检测

    /// <summary>
    /// 检测和刷新指定棋子的宝物羁绊
    /// </summary>
    private void RefreshTreasureSynergies(int chessId)
    {
        if (!m_ActiveTreasureSynergies.ContainsKey(chessId))
        {
            m_ActiveTreasureSynergies[chessId] = new HashSet<int>();
        }

        var previousActiveSynergies = new HashSet<int>(m_ActiveTreasureSynergies[chessId]);
        m_ActiveTreasureSynergies[chessId].Clear();

        // 获取该棋子的所有装备宝物的羁绊ID
        var treasureSynergyIds = GetTreasureSynergyIdsForChess(chessId);

        // 统计每个羁绊ID出现次数
        var synergyCount = new Dictionary<int, int>();
        foreach (int synergyId in treasureSynergyIds)
        {
            if (!synergyCount.ContainsKey(synergyId))
                synergyCount[synergyId] = 0;
            synergyCount[synergyId]++;
        }

        // 检查每个羁绊是否激活
        var synergyTable = GF.DataTable.GetDataTable<SynergyTable>();
        if (synergyTable == null)
        {
            DebugEx.Warning(nameof(SynergyManager), "SynergyTable 未加载");
            return;
        }

        foreach (var synergyRow in synergyTable.GetAllDataRows())
        {
            // 只处理宝物羁绊（IsTreasureSynergy=1）
            if (synergyRow.IsTreasureSynergy != 1)
                continue;

            bool isActive = synergyCount.ContainsKey(synergyRow.Id) && synergyCount[synergyRow.Id] >= synergyRow.RequireCount;

            if (isActive && !previousActiveSynergies.Contains(synergyRow.Id))
            {
                // 新激活
                ActivateSynergy(synergyRow, new List<GameObject> { GetChessGameObject(chessId) });
                m_ActiveTreasureSynergies[chessId].Add(synergyRow.Id);
                OnSynergyStateChanged?.Invoke(synergyRow.Id, true);
            }
            else if (!isActive && previousActiveSynergies.Contains(synergyRow.Id))
            {
                // 失活
                DeactivateSynergy(synergyRow, new List<GameObject> { GetChessGameObject(chessId) });
                m_ActiveTreasureSynergies[chessId].Remove(synergyRow.Id);
                OnSynergyStateChanged?.Invoke(synergyRow.Id, false);
            }
        }
    }

    /// <summary>
    /// 获取指定棋子装备的所有宝物的羁绊ID
    /// 目前宝物装备系统未实现，该方法作为预留接口
    /// TODO: 待宝物装备系统实现后，从装备系统获取宝物列表
    /// </summary>
    private List<int> GetTreasureSynergyIdsForChess(int chessId)
    {
        var synergyIds = new List<int>();

        // TODO: 当宝物装备系统实现后，通过以下方式获取宝物：
        // 1. 通过专门的宝物装备管理器
        // 2. 或者通过容器系统获取已穿戴的宝物
        // 3. 遍历宝物，收集其 SynergyIds

        return synergyIds;
    }

    #endregion

    #region 棋子羁绊检测

    /// <summary>
    /// 检测和刷新棋子羁绊
    /// 基于场上出战棋子的种族/职业组合
    /// </summary>
    private void RefreshChessSynergies()
    {
        var previousActiveSynergies = new HashSet<int>(m_ActiveChessSynergies);
        m_ActiveChessSynergies.Clear();

        // 获取所有出战棋子的种族和职业
        var deployedChesses = GetDeployedChesses();
        var raceCount = new Dictionary<int, int>();
        var classCount = new Dictionary<int, int>();

        foreach (var chess in deployedChesses)
        {
            if (chess == null || chess.Config == null)
                continue;

            // 统计种族
            if (chess.Config.Races != null)
            {
                foreach (int race in chess.Config.Races)
                {
                    if (!raceCount.ContainsKey(race))
                        raceCount[race] = 0;
                    raceCount[race]++;
                }
            }

            // 统计职业
            if (chess.Config.Classes != null)
            {
                foreach (int cls in chess.Config.Classes)
                {
                    if (!classCount.ContainsKey(cls))
                        classCount[cls] = 0;
                    classCount[cls]++;
                }
            }
        }

        // 检查所有棋子羁绊
        var synergyTable = GF.DataTable.GetDataTable<SynergyTable>();
        if (synergyTable == null)
        {
            DebugEx.Warning(nameof(SynergyManager), "SynergyTable 未加载");
            return;
        }

        var deployedGameObjects = new List<GameObject>();
        foreach (var chess in deployedChesses)
        {
            if (chess != null)
                deployedGameObjects.Add(chess.gameObject);
        }

        foreach (var synergyRow in synergyTable.GetAllDataRows())
        {
            // 只处理棋子羁绊（IsTreasureSynergy=0）
            if (synergyRow.IsTreasureSynergy != 0)
                continue;

            // 检查该羁绊的条件是否满足
            bool isSatisfied = CheckChessSynergyCondition(synergyRow, raceCount, classCount);

            if (isSatisfied && !previousActiveSynergies.Contains(synergyRow.Id))
            {
                // 新激活
                ActivateSynergy(synergyRow, deployedGameObjects);
                m_ActiveChessSynergies.Add(synergyRow.Id);
                OnSynergyStateChanged?.Invoke(synergyRow.Id, true);
                DebugEx.Success(nameof(SynergyManager), $"激活棋子羁绊: [{synergyRow.Id}] {synergyRow.Name}");
            }
            else if (!isSatisfied && previousActiveSynergies.Contains(synergyRow.Id))
            {
                // 失活
                DeactivateSynergy(synergyRow, deployedGameObjects);
                m_ActiveChessSynergies.Remove(synergyRow.Id);
                OnSynergyStateChanged?.Invoke(synergyRow.Id, false);
                DebugEx.Log(nameof(SynergyManager), $"失活棋子羁绊: [{synergyRow.Id}] {synergyRow.Name}");
            }
        }
    }

    /// <summary>
    /// 检查棋子羁绊条件是否满足
    /// RequireIds 中的值需要达到 RequireCount 个
    /// </summary>
    private bool CheckChessSynergyCondition(SynergyTable synergyRow, Dictionary<int, int> raceCount, Dictionary<int, int> classCount)
    {
        if (synergyRow.RequireIds == null || synergyRow.RequireIds.Length == 0)
            return false;

        // 统计条件中的种族/职业数量
        // RequireIds 中的值既可能是种族ID，也可能是职业ID
        // 这里假设需要的种族/职业达到 RequireCount 个即可激活
        int count = 0;
        foreach (int requireId in synergyRow.RequireIds)
        {
            if (raceCount.ContainsKey(requireId) && raceCount[requireId] > 0)
                count++;
            else if (classCount.ContainsKey(requireId) && classCount[requireId] > 0)
                count++;
        }

        return count >= synergyRow.RequireCount;
    }

    /// <summary>
    /// 获取所有出战棋子
    /// </summary>
    private List<ChessEntity> GetDeployedChesses()
    {
        var deployedChesses = new List<ChessEntity>();

        if (ChessDeploymentTracker.Instance == null)
        {
            DebugEx.Warning(nameof(SynergyManager), "ChessDeploymentTracker.Instance 为 null");
            return deployedChesses;
        }

        var deployedInstances = ChessDeploymentTracker.Instance.GetDeployedChess();
        if (deployedInstances == null || deployedInstances.Count == 0)
            return deployedChesses;

        foreach (var instance in deployedInstances)
        {
            if (instance != null && instance.Entity != null)
            {
                deployedChesses.Add(instance.Entity);
            }
        }

        return deployedChesses;
    }

    #endregion

    #region 羁绊激活/失活

    /// <summary>
    /// 激活羁绊效果
    /// </summary>
    private void ActivateSynergy(SynergyTable synergyRow, List<GameObject> targets)
    {
        if (synergyRow.EffectId <= 0)
            return;

        // 通过 GameEffectService 执行羁绊效果
        var context = GameEffectContext.CreateMultiTarget(EffectSource.Synergy, targets, null);
        GameEffectService.Instance.Execute(synergyRow.EffectId, context);

        // 记录这个羁绊应用的 Buff ID，便于后续移除
        var effectData = ItemManager.Instance?.GetSpecialEffectData(synergyRow.EffectId);
        if (effectData != null)
        {
            var buffIds = new List<int>();
            var rowBuffIds = effectData.GetParamValue<int[]>("BuffIds", null);
            if (rowBuffIds != null)
                buffIds.AddRange(rowBuffIds);

            var selfBuffIds = effectData.GetParamValue<int[]>("SelfBuffIds", null);
            if (selfBuffIds != null)
                buffIds.AddRange(selfBuffIds);

            if (buffIds.Count > 0)
            {
                m_SynergyBuffMapping[synergyRow.Id] = buffIds;
            }
        }

        DebugEx.Success(nameof(SynergyManager), $"激活羁绊效果: [{synergyRow.Id}] {synergyRow.Name}");
    }

    /// <summary>
    /// 失活羁绊效果
    /// </summary>
    private void DeactivateSynergy(SynergyTable synergyRow, List<GameObject> targets)
    {
        // 移除羁绊相关的 Buff
        if (m_SynergyBuffMapping.TryGetValue(synergyRow.Id, out var buffIds))
        {
            foreach (var buffId in buffIds)
            {
                foreach (var target in targets)
                {
                    if (target == null)
                        continue;

                    var buffManager = target.GetComponent<BuffManager>();
                    if (buffManager != null)
                    {
                        buffManager.RemoveBuff(buffId);
                        DebugEx.Log(nameof(SynergyManager), $"移除羁绊 Buff: [{buffId}]");
                    }
                }
            }

            m_SynergyBuffMapping.Remove(synergyRow.Id);
        }

        DebugEx.Log(nameof(SynergyManager), $"失活羁绊效果: [{synergyRow.Id}] {synergyRow.Name}");
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 获取棋子的 GameObject
    /// 优先级：全局玩家棋子 → 已出战的敌方棋子
    /// 如果棋子还未出战，返回 null
    /// </summary>
    private GameObject GetChessGameObject(int chessId)
    {
        // 1. 尝试从全局棋子管理器获取（玩家棋子）
        var globalChessState = GlobalChessManager.Instance?.GetChessState(chessId);
        if (globalChessState != null)
        {
            // TODO: 需要通过 chessId 从场景中找到对应的 ChessEntity
            // 目前返回 null，需要后续扩展
        }

        // 2. 从出战棋子中查找
        var deployedChesses = GetDeployedChesses();
        foreach (var chess in deployedChesses)
        {
            if (chess != null && chess.ChessId == chessId)
            {
                return chess.gameObject;
            }
        }

        // 棋子尚未出战或不存在
        return null;
    }

    #endregion

    #region 清理

    protected override void OnDestroy()
    {
        // 取消事件订阅
        ChessStateEvents.OnEquipmentChanged -= OnEquipmentChanged;

        if (ChessDeploymentTracker.Instance != null)
        {
            ChessDeploymentTracker.Instance.OnChessDeployed -= OnChessDeployed;
            ChessDeploymentTracker.Instance.OnChessRecalled -= OnChessRecalled;
        }

        m_ActiveTreasureSynergies.Clear();
        m_ActiveChessSynergies.Clear();
        m_SynergyBuffMapping.Clear();

        base.OnDestroy();
    }

    #endregion
}
