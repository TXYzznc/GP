using UnityEngine;
using UnityGameFramework.Runtime;
using System;

/// <summary>
/// 单局战斗会话临时数据
/// 存储当前战斗期间的统计值、金币等临时数据
/// 每次战斗开始时初始化，战斗结束时清空
/// </summary>
public class CombatSessionData
{
    #region 单例

    private static CombatSessionData s_Instance;
    public static CombatSessionData Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new CombatSessionData();
            }
            return s_Instance;
        }
    }

    private CombatSessionData() { }

    #endregion

    #region 私有字段

    /// <summary>当前统计值上限（初始值 + 升级增加值）</summary>
    private int m_CurrentMaxDomination;

    /// <summary>已使用的人口（已放置棋子的PopCost之和）</summary>
    private int m_UsedPopulation;

    /// <summary>拥有金币</summary>
    private int m_Gold;

    /// <summary>升级统计值的金币花费</summary>
    private int m_PopulationUpgradeCost = 2;

    /// <summary>每次增加的统计值</summary>
    private int m_PopulationUpgradeAmount = 1;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region 属性

    /// <summary>当前统计值上限</summary>
    public int CurrentMaxDomination => m_CurrentMaxDomination;

    /// <summary>已使用人口</summary>
    public int UsedPopulation => m_UsedPopulation;

    /// <summary>剩余可用人口</summary>
    public int AvailablePopulation => m_CurrentMaxDomination - m_UsedPopulation;

    /// <summary>拥有金币</summary>
    public int Gold => m_Gold;

    /// <summary>升级统计值的金币花费</summary>
    public int PopulationUpgradeCost => m_PopulationUpgradeCost;

    /// <summary>是否已初始化</summary>
    public bool IsInitialized => m_IsInitialized;

    #endregion

    #region 事件

    /// <summary>统计值变化事件（旧值, 新值）</summary>
    public event Action<int, int> OnMaxDominationChanged;

    /// <summary>已用人口变化事件（旧值, 新值）</summary>
    public event Action<int, int> OnUsedPopulationChanged;

    /// <summary>金币变化事件（旧值, 新值）</summary>
    public event Action<int, int> OnGoldChanged;

    #endregion

    #region 初始化与清理

    /// <summary>
    /// 初始化会话数据（在战斗准备时调用）
    /// </summary>
    /// <param name="initialMaxDomination">初始统计值上限（来自PlayerDataTable）</param>
    /// <param name="initialGold">初始金币</param>
    public void Initialize(int initialMaxDomination, int initialGold = 30)
    {
        m_CurrentMaxDomination = initialMaxDomination;
        m_UsedPopulation = 0;
        m_Gold = initialGold;
        m_IsInitialized = true;

        Log.Info($"CombatSessionData: 初始化完成 - 统计值={m_CurrentMaxDomination}, 金币={m_Gold}");
    }

    /// <summary>
    /// 清空会话数据（战斗结束时调用）
    /// </summary>
    public void Clear()
    {
        m_CurrentMaxDomination = 0;
        m_UsedPopulation = 0;
        m_Gold = 0;
        m_IsInitialized = false;

        OnMaxDominationChanged = null;
        OnUsedPopulationChanged = null;
        OnGoldChanged = null;

        Log.Info("CombatSessionData: 数据已清空");
    }

    #endregion

    #region 统计值管理

    /// <summary>
    /// 花费金币升级统计值上限
    /// </summary>
    /// <returns>是否成功</returns>
    public bool TryUpgradePopulation()
    {
        if (m_Gold < m_PopulationUpgradeCost)
        {
            Log.Warning($"CombatSessionData: 金币不足，需要{m_PopulationUpgradeCost}，当前{m_Gold}");
            return false;
        }

        // 扣除金币
        int oldGold = m_Gold;
        m_Gold -= m_PopulationUpgradeCost;
        OnGoldChanged?.Invoke(oldGold, m_Gold);

        // 增加统计值
        int oldMax = m_CurrentMaxDomination;
        m_CurrentMaxDomination += m_PopulationUpgradeAmount;
        OnMaxDominationChanged?.Invoke(oldMax, m_CurrentMaxDomination);

        Log.Info($"CombatSessionData: 统计值升级 {oldMax} -> {m_CurrentMaxDomination}，剩余金币={m_Gold}");
        return true;
    }

    /// <summary>
    /// 检查是否可以放置指定PopCost的棋子
    /// </summary>
    /// <param name="popCost">棋子的人口花费</param>
    /// <returns>是否可以放置</returns>
    public bool CanPlace(int popCost)
    {
        return m_UsedPopulation + popCost <= m_CurrentMaxDomination;
    }

    /// <summary>
    /// 消耗人口（放置棋子时调用）
    /// </summary>
    /// <param name="popCost">消耗的人口</param>
    /// <returns>是否成功</returns>
    public bool ConsumePopulation(int popCost)
    {
        if (!CanPlace(popCost))
        {
            Log.Warning($"CombatSessionData: 人口不足，需要{popCost}，可用{AvailablePopulation}");
            return false;
        }

        int oldUsed = m_UsedPopulation;
        m_UsedPopulation += popCost;
        OnUsedPopulationChanged?.Invoke(oldUsed, m_UsedPopulation);

        Log.Info($"CombatSessionData: 消耗人口 {popCost}，已用={m_UsedPopulation}/{m_CurrentMaxDomination}");
        return true;
    }

    /// <summary>
    /// 归还人口（移除棋子时调用）
    /// </summary>
    /// <param name="popCost">归还的人口</param>
    public void ReturnPopulation(int popCost)
    {
        int oldUsed = m_UsedPopulation;
        m_UsedPopulation = Mathf.Max(0, m_UsedPopulation - popCost);
        OnUsedPopulationChanged?.Invoke(oldUsed, m_UsedPopulation);

        Log.Info($"CombatSessionData: 归还人口 {popCost}，已用={m_UsedPopulation}/{m_CurrentMaxDomination}");
    }

    #endregion

    #region 金币管理

    /// <summary>
    /// 增加金币
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        int oldGold = m_Gold;
        m_Gold += amount;
        OnGoldChanged?.Invoke(oldGold, m_Gold);
    }

    /// <summary>
    /// 尝试消耗金币
    /// </summary>
    /// <returns>是否成功</returns>
    public bool TryConsumeGold(int amount)
    {
        if (m_Gold < amount) return false;

        int oldGold = m_Gold;
        m_Gold -= amount;
        OnGoldChanged?.Invoke(oldGold, m_Gold);
        return true;
    }

    #endregion

    #region 调试

    public string GetDebugInfo()
    {
        return $"[CombatSessionData] 统计值={m_UsedPopulation}/{m_CurrentMaxDomination}, 金币={m_Gold}";
    }

    #endregion
}
