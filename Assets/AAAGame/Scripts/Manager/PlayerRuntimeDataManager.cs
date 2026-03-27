using UnityEngine;
using System;

/// <summary>
/// 玩家运行时数据管理器
/// 管理玩家在局内的运行时数据（污染值、移速等）
/// 注意：这些数据不会持久化到存档，仅在局内有效
/// </summary>
public class PlayerRuntimeDataManager
{
    #region 单例

    private static PlayerRuntimeDataManager s_Instance;
    public static PlayerRuntimeDataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new PlayerRuntimeDataManager();
            }
            return s_Instance;
        }
    }

    #endregion

    #region 字段

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized = false;

    /// <summary>当前污染值</summary>
    private float m_CurrentCorruption = 0f;

    /// <summary>最大污染值</summary>
    private float m_MaxCorruption = 100f;

    /// <summary>污染值增长速度（每秒增加的数值）</summary>
    private float m_CorruptionGrowthRate = 1f;

    /// <summary>当前移速</summary>
    private float m_CurrentMoveSpeed = 5f;

    /// <summary>污染值变化事件</summary>
    public event Action<float, float> OnCorruptionChanged;

    #endregion

    #region 属性

    /// <summary>是否已初始化</summary>
    public bool IsInitialized => m_IsInitialized;

    /// <summary>当前污染值</summary>
    public float CurrentCorruption => m_CurrentCorruption;

    /// <summary>最大污染值</summary>
    public float MaxCorruption => m_MaxCorruption;

    /// <summary>污染值百分比（0-1）</summary>
    public float CorruptionPercent => m_MaxCorruption > 0 ? m_CurrentCorruption / m_MaxCorruption : 0f;

    /// <summary>污染值增长速度（每秒）</summary>
    public float CorruptionGrowthRate => m_CorruptionGrowthRate;

    /// <summary>当前移速</summary>
    public float CurrentMoveSpeed => m_CurrentMoveSpeed;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化玩家运行时数据（进入局内时调用）
    /// </summary>
    public void Initialize()
    {
        if (m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "已经初始化，跳过重复初始化");
            return;
        }

        // 从召唤师配置读取移速
        var summonerConfig = PlayerAccountDataManager.Instance?.GetCurrentSummonerConfig();
        if (summonerConfig != null)
        {
            m_CurrentMoveSpeed = summonerConfig.PlayerMoveSpeed;
            DebugEx.LogModule("PlayerRuntimeDataManager", $"从召唤师配置读取移速: {m_CurrentMoveSpeed}");
        }
        else
        {
            m_CurrentMoveSpeed = 5f; // 默认移速
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未找到召唤师配置，使用默认移速: 5");
        }

        // 初始化污染值为0（局外时为0）
        m_CurrentCorruption = 0f;
        m_MaxCorruption = 100f;
        m_CorruptionGrowthRate = 1f; // 默认每秒增加1点污染值

        m_IsInitialized = true;

        DebugEx.LogModule("PlayerRuntimeDataManager", 
            $"玩家运行时数据初始化完成 - 移速:{m_CurrentMoveSpeed}, 污染值:{m_CurrentCorruption}/{m_MaxCorruption}, 增长速度:{m_CorruptionGrowthRate}/秒");
    }

    /// <summary>
    /// 清理运行时数据（离开局内时调用）
    /// </summary>
    public void Cleanup()
    {
        if (!m_IsInitialized)
        {
            return;
        }

        m_CurrentCorruption = 0f;
        m_CurrentMoveSpeed = 5f;
        m_CorruptionGrowthRate = 1f;
        m_IsInitialized = false;

        DebugEx.LogModule("PlayerRuntimeDataManager", "玩家运行时数据已清理");
    }

    /// <summary>
    /// 更新污染值增长（每帧调用）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void UpdateCorruptionGrowth(float deltaTime)
    {
        if (!m_IsInitialized)
        {
            return;
        }

        // 如果污染值未达到最大值，则持续增长
        if (m_CurrentCorruption < m_MaxCorruption)
        {
            float growthAmount = m_CorruptionGrowthRate * deltaTime;
            float oldValue = m_CurrentCorruption;
            m_CurrentCorruption = Mathf.Clamp(m_CurrentCorruption + growthAmount, 0f, m_MaxCorruption);

            // 只有当污染值实际发生变化时才触发事件
            if (Mathf.Abs(m_CurrentCorruption - oldValue) > 0.01f)
            {
                OnCorruptionChanged?.Invoke(oldValue, m_CurrentCorruption);
            }
        }
    }

    #endregion

    #region 污染值增长速度管理

    /// <summary>
    /// 设置污染值增长速度
    /// </summary>
    /// <param name="growthRate">新的增长速度（每秒）</param>
    public void SetCorruptionGrowthRate(float growthRate)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法设置污染值增长速度");
            return;
        }

        float oldRate = m_CorruptionGrowthRate;
        m_CorruptionGrowthRate = Mathf.Max(0f, growthRate);

        DebugEx.LogModule("PlayerRuntimeDataManager", 
            $"污染值增长速度设置: {oldRate:F2}/秒 -> {m_CorruptionGrowthRate:F2}/秒");
    }

    /// <summary>
    /// 修改污染值增长速度（增加或减少）
    /// </summary>
    /// <param name="delta">变化量</param>
    public void ModifyCorruptionGrowthRate(float delta)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法修改污染值增长速度");
            return;
        }

        SetCorruptionGrowthRate(m_CorruptionGrowthRate + delta);
    }

    #endregion

    #region 污染值管理

    /// <summary>
    /// 增加污染值
    /// </summary>
    /// <param name="amount">增加的数值</param>
    public void AddCorruption(float amount)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法增加污染值");
            return;
        }

        float oldValue = m_CurrentCorruption;
        m_CurrentCorruption = Mathf.Clamp(m_CurrentCorruption + amount, 0f, m_MaxCorruption);

        DebugEx.LogModule("PlayerRuntimeDataManager", 
            $"污染值增加: {oldValue:F1} -> {m_CurrentCorruption:F1} (+{amount:F1})");

        // 触发事件
        OnCorruptionChanged?.Invoke(oldValue, m_CurrentCorruption);
    }

    /// <summary>
    /// 减少污染值
    /// </summary>
    /// <param name="amount">减少的数值</param>
    public void ReduceCorruption(float amount)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法减少污染值");
            return;
        }

        float oldValue = m_CurrentCorruption;
        m_CurrentCorruption = Mathf.Clamp(m_CurrentCorruption - amount, 0f, m_MaxCorruption);

        DebugEx.LogModule("PlayerRuntimeDataManager", 
            $"污染值减少: {oldValue:F1} -> {m_CurrentCorruption:F1} (-{amount:F1})");

        // 触发事件
        OnCorruptionChanged?.Invoke(oldValue, m_CurrentCorruption);
    }

    /// <summary>
    /// 设置污染值
    /// </summary>
    /// <param name="value">新的污染值</param>
    public void SetCorruption(float value)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法设置污染值");
            return;
        }

        float oldValue = m_CurrentCorruption;
        m_CurrentCorruption = Mathf.Clamp(value, 0f, m_MaxCorruption);

        DebugEx.LogModule("PlayerRuntimeDataManager", 
            $"污染值设置: {oldValue:F1} -> {m_CurrentCorruption:F1}");

        // 触发事件
        OnCorruptionChanged?.Invoke(oldValue, m_CurrentCorruption);
    }

    /// <summary>
    /// 战斗失败时增加一半污染值
    /// </summary>
    public void OnCombatDefeat()
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法处理战斗失败");
            return;
        }

        float halfCorruption = m_CurrentCorruption * 0.5f;
        AddCorruption(halfCorruption);

        DebugEx.WarningModule("PlayerRuntimeDataManager", 
            $"战斗失败！污染值增加一半: +{halfCorruption:F1}");
    }

    #endregion

    #region 移速管理

    /// <summary>
    /// 设置移速
    /// </summary>
    /// <param name="speed">新的移速</param>
    public void SetMoveSpeed(float speed)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法设置移速");
            return;
        }

        float oldSpeed = m_CurrentMoveSpeed;
        m_CurrentMoveSpeed = Mathf.Max(0f, speed);

        DebugEx.LogModule("PlayerRuntimeDataManager", 
            $"移速设置: {oldSpeed:F1} -> {m_CurrentMoveSpeed:F1}");
    }

    /// <summary>
    /// 修改移速（增加或减少）
    /// </summary>
    /// <param name="delta">变化量</param>
    public void ModifyMoveSpeed(float delta)
    {
        if (!m_IsInitialized)
        {
            DebugEx.WarningModule("PlayerRuntimeDataManager", "未初始化，无法修改移速");
            return;
        }

        SetMoveSpeed(m_CurrentMoveSpeed + delta);
    }

    #endregion
}
