using UnityEngine;
using System;

/// <summary>
/// 召唤师运行时数据管理器
/// 管理召唤师在战斗中的运行时数据（HP、MP等）
/// 注意：这些数据不会持久化到存档，仅在战斗中有效
/// </summary>
public class SummonerRuntimeDataManager
{
    #region 单例

    private static SummonerRuntimeDataManager s_Instance;
    public static SummonerRuntimeDataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new SummonerRuntimeDataManager();
            }
            return s_Instance;
        }
    }

    #endregion

    #region 字段

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized = false;

    /// <summary>当前生命值</summary>
    private float m_CurrentHP = 0f;

    /// <summary>最大生命值</summary>
    private float m_MaxHP = 100f;

    /// <summary>当前灵力值</summary>
    private float m_CurrentMP = 0f;

    /// <summary>最大灵力值</summary>
    private float m_MaxMP = 50f;

    /// <summary>灵力恢复速度（每秒）</summary>
    private float m_MPRegen = 1f;

    /// <summary>HP变化事件 (旧值, 新值)</summary>
    public event Action<float, float> OnHPChanged;

    /// <summary>MP变化事件 (旧值, 新值)</summary>
    public event Action<float, float> OnMPChanged;

    #endregion

    #region 属性

    /// <summary>是否已初始化</summary>
    public bool IsInitialized => m_IsInitialized;

    /// <summary>当前生命值</summary>
    public float CurrentHP => m_CurrentHP;

    /// <summary>最大生命值</summary>
    public float MaxHP => m_MaxHP;

    /// <summary>生命值百分比（0-1）</summary>
    public float HPPercent => m_MaxHP > 0 ? m_CurrentHP / m_MaxHP : 0f;

    /// <summary>当前灵力值</summary>
    public float CurrentMP => m_CurrentMP;

    /// <summary>最大灵力值</summary>
    public float MaxMP => m_MaxMP;

    /// <summary>灵力值百分比（0-1）</summary>
    public float MPPercent => m_MaxMP > 0 ? m_CurrentMP / m_MaxMP : 0f;

    /// <summary>灵力恢复速度</summary>
    public float MPRegen => m_MPRegen;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化召唤师运行时数据（进入战斗时调用）
    /// </summary>
    public void Initialize()
    {
        if (m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "已经初始化，跳过重复初始化");
            return;
        }

        // 从召唤师配置读取数据
        var summonerConfig = PlayerAccountDataManager.Instance?.GetCurrentSummonerConfig();
        if (summonerConfig != null)
        {
            m_MaxHP = summonerConfig.BaseHP;
            m_CurrentHP = m_MaxHP; // 进入战斗时HP回满

            m_MaxMP = summonerConfig.BaseMP;
            m_CurrentMP = m_MaxMP; // 进入战斗时MP回满

            m_MPRegen = summonerConfig.MPRegen;

            // DebugEx.LogModule("SummonerRuntimeDataManager", 
            //     $"从召唤师配置读取数据 - HP:{m_CurrentHP}/{m_MaxHP}, MP:{m_CurrentMP}/{m_MaxMP}, MPRegen:{m_MPRegen}");
        }
        else
        {
            // 使用默认值
            m_MaxHP = 100f;
            m_CurrentHP = m_MaxHP;
            m_MaxMP = 50f;
            m_CurrentMP = m_MaxMP;
            m_MPRegen = 1f;

            DebugEx.WarningModule("SummonerRuntimeDataManager", 
                "未找到召唤师配置，使用默认值 - HP:100/100, MP:50/50, MPRegen:1");
        }

        m_IsInitialized = true;

        DebugEx.LogModule("SummonerRuntimeDataManager", 
            $"召唤师运行时数据初始化完成 - HP:{m_CurrentHP}/{m_MaxHP}, MP:{m_CurrentMP}/{m_MaxMP}");
    }

    /// <summary>
    /// 战斗开始时重置 HP/MP 至满值（每场战斗调用，不继承上一场状态）
    /// 与 Initialize() 的区别：不读配置，只做回满；即使已初始化也会执行
    /// </summary>
    public void InitializeForBattle()
    {
        float oldHP = m_CurrentHP;
        float oldMP = m_CurrentMP;

        m_CurrentHP = m_MaxHP;
        m_CurrentMP = m_MaxMP;

        if (oldHP != m_CurrentHP)
            OnHPChanged?.Invoke(oldHP, m_CurrentHP);
        if (oldMP != m_CurrentMP)
            OnMPChanged?.Invoke(oldMP, m_CurrentMP);

        DebugEx.LogModule("SummonerRuntimeDataManager",
            $"战斗初始化回满 - HP:{m_CurrentHP}/{m_MaxHP}, MP:{m_CurrentMP}/{m_MaxMP}");
    }

    /// <summary>
    /// 清理运行时数据（离开战斗时调用）
    /// </summary>
    public void Cleanup()
    {
        if (!m_IsInitialized)
        {
            return;
        }

        m_CurrentHP = 0f;
        m_MaxHP = 100f;
        m_CurrentMP = 0f;
        m_MaxMP = 50f;
        m_MPRegen = 1f;
        m_IsInitialized = false;

        DebugEx.LogModule("SummonerRuntimeDataManager", "召唤师运行时数据已清理");
    }

    #endregion

    #region HP管理

    /// <summary>
    /// 增加生命值
    /// </summary>
    /// <param name="amount">增加的数值</param>
    public void AddHP(float amount)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "未初始化，无法增加生命值");
            return;
        }

        float oldValue = m_CurrentHP;
        m_CurrentHP = Mathf.Clamp(m_CurrentHP + amount, 0f, m_MaxHP);

        DebugEx.LogModule("SummonerRuntimeDataManager", 
            $"生命值增加: {oldValue:F1} -> {m_CurrentHP:F1} (+{amount:F1})");

        // 触发事件
        OnHPChanged?.Invoke(oldValue, m_CurrentHP);
    }

    /// <summary>
    /// 减少生命值
    /// </summary>
    /// <param name="amount">减少的数值</param>
    public void ReduceHP(float amount)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "未初始化，无法减少生命值");
            return;
        }

        float oldValue = m_CurrentHP;
        m_CurrentHP = Mathf.Clamp(m_CurrentHP - amount, 0f, m_MaxHP);

        DebugEx.LogModule("SummonerRuntimeDataManager", 
            $"生命值减少: {oldValue:F1} -> {m_CurrentHP:F1} (-{amount:F1})");

        // 触发事件
        OnHPChanged?.Invoke(oldValue, m_CurrentHP);

        // 检查是否死亡
        if (m_CurrentHP <= 0f)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "召唤师生命值归零！");
        }
    }

    /// <summary>
    /// 设置生命值
    /// </summary>
    /// <param name="value">新的生命值</param>
    public void SetHP(float value)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "未初始化，无法设置生命值");
            return;
        }

        float oldValue = m_CurrentHP;
        m_CurrentHP = Mathf.Clamp(value, 0f, m_MaxHP);

        DebugEx.LogModule("SummonerRuntimeDataManager", 
            $"生命值设置: {oldValue:F1} -> {m_CurrentHP:F1}");

        // 触发事件
        OnHPChanged?.Invoke(oldValue, m_CurrentHP);
    }

    #endregion

    #region MP管理

    /// <summary>
    /// 增加灵力值
    /// </summary>
    /// <param name="amount">增加的数值</param>
    public void AddMP(float amount)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "未初始化，无法增加灵力值");
            return;
        }

        float oldValue = m_CurrentMP;
        m_CurrentMP = Mathf.Clamp(m_CurrentMP + amount, 0f, m_MaxMP);

        // DebugEx.LogModule("SummonerRuntimeDataManager", 
        //     $"灵力值增加: {oldValue:F1} -> {m_CurrentMP:F1} (+{amount:F1})");

        // 触发事件
        OnMPChanged?.Invoke(oldValue, m_CurrentMP);
    }

    /// <summary>
    /// 减少灵力值（消耗技能）
    /// </summary>
    /// <param name="amount">减少的数值</param>
    /// <returns>是否成功消耗</returns>
    public bool ConsumeMP(float amount)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "未初始化，无法消耗灵力值");
            return false;
        }

        if (m_CurrentMP < amount)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", 
                $"灵力值不足: 需要{amount:F1}, 当前{m_CurrentMP:F1}");
            return false;
        }

        float oldValue = m_CurrentMP;
        m_CurrentMP = Mathf.Clamp(m_CurrentMP - amount, 0f, m_MaxMP);

        DebugEx.LogModule("SummonerRuntimeDataManager", 
            $"灵力值消耗: {oldValue:F1} -> {m_CurrentMP:F1} (-{amount:F1})");

        // 触发事件
        OnMPChanged?.Invoke(oldValue, m_CurrentMP);

        return true;
    }

    /// <summary>
    /// 设置灵力值
    /// </summary>
    /// <param name="value">新的灵力值</param>
    public void SetMP(float value)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("SummonerRuntimeDataManager", "未初始化，无法设置灵力值");
            return;
        }

        float oldValue = m_CurrentMP;
        m_CurrentMP = Mathf.Clamp(value, 0f, m_MaxMP);

        DebugEx.LogModule("SummonerRuntimeDataManager", 
            $"灵力值设置: {oldValue:F1} -> {m_CurrentMP:F1}");

        // 触发事件
        OnMPChanged?.Invoke(oldValue, m_CurrentMP);
    }

    /// <summary>
    /// 更新灵力恢复（每帧调用）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void UpdateMPRegen(float deltaTime)
    {
        if (!m_IsInitialized)
        {
            return;
        }

        if (m_CurrentMP < m_MaxMP)
        {
            float regenAmount = m_MPRegen * deltaTime;
            AddMP(regenAmount);
        }
    }

    #endregion
}
