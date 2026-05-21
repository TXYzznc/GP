using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人警示UI管理器
/// 管理敌人警觉指示器的创建、更新、销毁
/// 实现对象池和距离排序
/// </summary>
public class EnemyAlertUIManager : SingletonBase<EnemyAlertUIManager>
{
    #region 私有字段

    /// <summary>警示指示器预制体</summary>
    private EnemyMask m_IndicatorPrefab;

    /// <summary>指示器对象池</summary>
    private Queue<EnemyMask> m_IndicatorPool = new Queue<EnemyMask>();

    /// <summary>活跃指示器映射（敌人 -> 指示器）</summary>
    private Dictionary<EnemyEntity, EnemyMask> m_ActiveIndicators =
        new Dictionary<EnemyEntity, EnemyMask>();

    /// <summary>警示指示器的父容器（GamePlayInfoUI中的varEnemyWarningHead）</summary>
    private RectTransform m_IndicatorContainer;

    /// <summary>最多同时显示的指示器数量</summary>
    private const int MAX_DISPLAY_COUNT = 5;

    /// <summary>对象池初始大小</summary>
    private const int POOL_INITIAL_SIZE = 5;

    /// <summary>玩家Transform缓存</summary>
    private Transform m_PlayerTransform;

    /// <summary>上次距离排序的时间</summary>
    private float m_LastSortTime;

    /// <summary>排序间隔（秒）</summary>
    private const float SORT_INTERVAL = 0.5f;

    /// <summary>实例化计数器，用于生成唯一编号</summary>
    private int m_IndicatorCounter = 0;

    // 复用列表，避免每帧/每0.5s分配
    private readonly List<EnemyEntity> m_ToRemoveBuffer = new List<EnemyEntity>();
    private readonly List<KeyValuePair<EnemyEntity, EnemyMask>> m_SortBuffer =
        new List<KeyValuePair<EnemyEntity, EnemyMask>>();

    #endregion

    #region Unity生命周期

    private void Awake()
    {
        base.Awake();
        DebugEx.Log(nameof(EnemyAlertUIManager), "初始化完成");
    }

    private void Update()
    {
        // 定期更新所有活跃指示器
        UpdateAllIndicators();

        // 定期重新排序（按距离）
        if (Time.time - m_LastSortTime >= SORT_INTERVAL)
        {
            SortIndicatorsByDistance();
            m_LastSortTime = Time.time;
        }
    }

    private void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化管理器（由GamePlayInfoUI调用）
    /// </summary>
    public void Initialize(RectTransform indicatorContainer, EnemyMask indicatorPrefab)
    {
        m_IndicatorContainer = indicatorContainer;
        m_IndicatorPrefab = indicatorPrefab;

        // 初始化对象池
        InitializePool();

        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            m_PlayerTransform = playerObj.transform;
        }

        DebugEx.Log(
            nameof(EnemyAlertUIManager),
            $"管理器已初始化，容器: {indicatorContainer.name}"
        );
    }

    /// <summary>
    /// 显示或更新敌人警示
    /// 警觉度>0.1f时调用（由VisionConeDetector调用）
    /// </summary>
    public void ShowOrUpdateAlert(EnemyEntity enemy, float alertProgress)
    {
        if (enemy == null)
            return;

        // 如果已经有指示器，直接更新
        if (m_ActiveIndicators.TryGetValue(enemy, out EnemyMask indicator))
        {
            indicator.UpdateProgress(alertProgress);
            return;
        }

        // 如果已到达最大显示数，不再创建新指示器
        if (m_ActiveIndicators.Count >= MAX_DISPLAY_COUNT)
        {
            return;
        }

        // 从对象池获取指示器
        indicator = GetFromPool();
        if (indicator == null)
        {
            DebugEx.Warning(nameof(EnemyAlertUIManager), "无法获取指示器（池为空且无法实例化）");
            return;
        }

        // 设置指示器
        indicator.Setup(enemy, alertProgress);
        indicator.transform.SetParent(m_IndicatorContainer);
        indicator.gameObject.SetActive(true);

        // 添加到映射
        m_ActiveIndicators[enemy] = indicator;

        DebugEx.Log(nameof(EnemyAlertUIManager), $"显示警示: {enemy.Config.Name}");
    }

    /// <summary>
    /// 隐藏敌人警示
    /// 警觉度降到0时调用（由VisionConeDetector调用）
    /// </summary>
    public void HideAlert(EnemyEntity enemy)
    {
        if (enemy == null)
            return;

        if (m_ActiveIndicators.TryGetValue(enemy, out EnemyMask indicator))
        {
            m_ActiveIndicators.Remove(enemy);
            ReturnToPool(indicator);

            DebugEx.Log(nameof(EnemyAlertUIManager), $"隐藏警示: {enemy.Config.Name}");
        }
    }

    /// <summary>
    /// 清空所有指示器（场景切换时调用）
    /// </summary>
    public void ClearAll()
    {
        foreach (var indicator in m_ActiveIndicators.Values)
        {
            ReturnToPool(indicator);
        }
        m_ActiveIndicators.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePool()
    {
        if (m_IndicatorPrefab == null)
        {
            DebugEx.Warning(nameof(EnemyAlertUIManager), "指示器预制体未设置");
            return;
        }

        for (int i = 0; i < POOL_INITIAL_SIZE; i++)
        {
            EnemyMask indicator = CreateIndicator();
            indicator.gameObject.SetActive(false);
            ReturnToPool(indicator);
        }

        DebugEx.Log(
            nameof(EnemyAlertUIManager),
            $"对象池初始化完成，预热{POOL_INITIAL_SIZE}个对象"
        );
    }

    /// <summary>
    /// 从对象池获取指示器
    /// </summary>
    private EnemyMask GetFromPool()
    {
        if (m_IndicatorPool.Count > 0)
        {
            return m_IndicatorPool.Dequeue();
        }

        // 池空，尝试创建新对象（在容器中实例化，保持Canvas空间）
        if (m_IndicatorPrefab != null)
        {
            EnemyMask indicator = CreateIndicator();
            indicator.gameObject.SetActive(false);
            return indicator;
        }

        return null;
    }

    /// <summary>
    /// 实例化一个新的指示器并分配编号
    /// </summary>
    private EnemyMask CreateIndicator()
    {
        EnemyMask indicator = Instantiate(m_IndicatorPrefab, m_IndicatorContainer);
        indicator.gameObject.name = $"{m_IndicatorPrefab.gameObject.name}_{++m_IndicatorCounter}";
        return indicator;
    }

    /// <summary>
    /// 回收指示器到对象池
    /// </summary>
    private void ReturnToPool(EnemyMask indicator)
    {
        if (indicator == null)
            return;

        indicator.gameObject.SetActive(false);
        indicator.transform.SetParent(transform); // 设置为管理器的子对象
        m_IndicatorPool.Enqueue(indicator);
    }

    /// <summary>
    /// 更新所有活跃指示器
    /// </summary>
    private void UpdateAllIndicators()
    {
        m_ToRemoveBuffer.Clear();

        foreach (var kvp in m_ActiveIndicators)
        {
            EnemyEntity enemy = kvp.Key;
            EnemyMask indicator = kvp.Value;

            if (enemy == null || enemy.VisionDetector == null)
            {
                m_ToRemoveBuffer.Add(enemy);
                continue;
            }

            float alertProgress = enemy.VisionDetector.AlertLevel;
            if (alertProgress <= 0f)
            {
                m_ToRemoveBuffer.Add(enemy);
                continue;
            }

            indicator.UpdateProgress(alertProgress);
        }

        foreach (var enemy in m_ToRemoveBuffer)
        {
            HideAlert(enemy);
        }
    }

    /// <summary>
    /// 按距离排序指示器
    /// </summary>
    private void SortIndicatorsByDistance()
    {
        if (m_PlayerTransform == null || m_ActiveIndicators.Count == 0)
            return;

        Vector3 playerPos = m_PlayerTransform.position;

        // 填充缓冲区并排序
        m_SortBuffer.Clear();
        foreach (var kvp in m_ActiveIndicators)
            m_SortBuffer.Add(kvp);

        m_SortBuffer.Sort((a, b) =>
        {
            float da = (a.Key.transform.position - playerPos).sqrMagnitude;
            float db = (b.Key.transform.position - playerPos).sqrMagnitude;
            return da.CompareTo(db);
        });

        // 超出上限的移除
        m_ToRemoveBuffer.Clear();
        for (int i = MAX_DISPLAY_COUNT; i < m_SortBuffer.Count; i++)
            m_ToRemoveBuffer.Add(m_SortBuffer[i].Key);
        foreach (var enemy in m_ToRemoveBuffer)
            HideAlert(enemy);

        // 重新排序 UI
        for (int i = 0; i < m_SortBuffer.Count && i < MAX_DISPLAY_COUNT; i++)
            m_SortBuffer[i].Value.transform.SetSiblingIndex(i);
    }

    #endregion
}
