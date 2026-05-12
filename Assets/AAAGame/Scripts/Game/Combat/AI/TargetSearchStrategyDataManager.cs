using System.Collections.Generic;
using UnityEngine;
using GameFramework;

/// <summary>
/// 索敌策略配置表管理器
/// </summary>
public class TargetSearchStrategyDataManager : SingletonBase<TargetSearchStrategyDataManager>
{
    #region 字段

    private Dictionary<int, TargetSearchStrategyTable> m_StrategyById;
    private Dictionary<int, TargetSearchStrategyTable> m_StrategyByAIType;
    private bool m_Initialized = false;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化配置表数据
    /// </summary>
    public void Initialize()
    {
        if (m_Initialized)
            return;

        m_StrategyById = new Dictionary<int, TargetSearchStrategyTable>();
        m_StrategyByAIType = new Dictionary<int, TargetSearchStrategyTable>();

        // 使用扩展方法获取所有行
        var rows = DataTableExtension.GetAllRows<TargetSearchStrategyTable>();
        if (rows == null || rows.Count == 0)
        {
            DebugEx.Error(nameof(TargetSearchStrategyDataManager), "无法加载 TargetSearchStrategyTable");
            return;
        }

        foreach (var row in rows)
        {
            m_StrategyById[row.Id] = row;

            // 如果是有效的AI类型，添加到按AI类型的映射
            if (!m_StrategyByAIType.ContainsKey(row.AIType))
            {
                m_StrategyByAIType[row.AIType] = row;
            }
        }

        m_Initialized = true;
        DebugEx.Log(nameof(TargetSearchStrategyDataManager),
            $"索敌策略配置表已加载：{m_StrategyById.Count} 条记录");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 根据策略ID获取配置
    /// </summary>
    public TargetSearchStrategyTable GetStrategyById(int id)
    {
        if (!m_Initialized)
            Initialize();

        if (m_StrategyById.TryGetValue(id, out var strategy))
            return strategy;

        DebugEx.Warning(nameof(TargetSearchStrategyDataManager),
            $"找不到索敌策略 ID={id}，使用默认配置");
        return GetDefaultStrategy();
    }

    /// <summary>
    /// 根据AI类型获取配置
    /// </summary>
    public TargetSearchStrategyTable GetStrategyByAIType(int aiType)
    {
        if (!m_Initialized)
            Initialize();

        if (m_StrategyByAIType.TryGetValue(aiType, out var strategy))
            return strategy;

        DebugEx.Warning(nameof(TargetSearchStrategyDataManager),
            $"找不到AI类型 {aiType} 的索敌策略，使用默认配置");
        return GetDefaultStrategy();
    }

    /// <summary>
    /// 获取默认策略（ID=1001）
    /// </summary>
    private TargetSearchStrategyTable GetDefaultStrategy()
    {
        if (!m_Initialized)
            Initialize();

        return m_StrategyById.TryGetValue(1001, out var strategy)
            ? strategy
            : null;
    }

    /// <summary>
    /// 转换配置表行为TargetSearchConfig对象
    /// </summary>
    public TargetSearchConfig ConvertToConfig(TargetSearchStrategyTable strategy)
    {
        if (strategy == null)
            return TargetSearchConfig.CreateDefault();

        return new TargetSearchConfig
        {
            SearchRange = strategy.SearchRange,
            DistanceWeight = strategy.DistanceWeight,
            HpWeight = strategy.HpWeight,
            ThreatWeight = strategy.ThreatWeight,
        };
    }

    #endregion
}
