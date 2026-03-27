using System.Collections.Generic;
using System.Linq;
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

    #endregion

    #region Unity生命周期

    private void Awake()
    {
        base.Awake();
        DebugEx.LogModule("EnemyAlertUIManager", "初始化完成");
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

        DebugEx.LogModule(
            "EnemyAlertUIManager",
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
            DebugEx.WarningModule("EnemyAlertUIManager", "无法获取指示器（池为空且无法实例化）");
            return;
        }

        // 设置指示器
        indicator.Setup(enemy, GetEnemyIcon(enemy), alertProgress);
        indicator.transform.SetParent(m_IndicatorContainer);
        indicator.gameObject.SetActive(true);

        // 添加到映射
        m_ActiveIndicators[enemy] = indicator;

        DebugEx.LogModule("EnemyAlertUIManager", $"显示警示: {enemy.Config.Name}");
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

            DebugEx.LogModule("EnemyAlertUIManager", $"隐藏警示: {enemy.Config.Name}");
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
            DebugEx.WarningModule("EnemyAlertUIManager", "指示器预制体未设置");
            return;
        }

        for (int i = 0; i < POOL_INITIAL_SIZE; i++)
        {
            EnemyMask indicator = CreateIndicator();
            indicator.gameObject.SetActive(false);
            ReturnToPool(indicator);
        }

        DebugEx.LogModule(
            "EnemyAlertUIManager",
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
        // 创建待删除列表，避免在迭代时修改字典
        List<EnemyEntity> toRemove = new List<EnemyEntity>();

        foreach (var kvp in m_ActiveIndicators)
        {
            EnemyEntity enemy = kvp.Key;
            EnemyMask indicator = kvp.Value;

            // 检查敌人是否仍然有效
            if (enemy == null || enemy.VisionDetector == null)
            {
                toRemove.Add(enemy);
                continue;
            }

            // 更新进度条
            float alertProgress = enemy.VisionDetector.AlertLevel;

            // 如果警觉度降到0，移除指示器
            if (alertProgress <= 0f)
            {
                toRemove.Add(enemy);
                continue;
            }

            indicator.UpdateProgress(alertProgress);
        }

        // 移除失效的指示器
        foreach (var enemy in toRemove)
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

        // 按距离排序活跃指示器
        var sortedIndicators = m_ActiveIndicators
            .OrderBy(kvp =>
                Vector3.Distance(m_PlayerTransform.position, kvp.Key.transform.position)
            )
            .Take(MAX_DISPLAY_COUNT)
            .ToList();

        // 只保留距离最近的指示器
        var toRemove = m_ActiveIndicators.Keys.Except(sortedIndicators.Select(x => x.Key)).ToList();

        foreach (var enemy in toRemove)
        {
            HideAlert(enemy);
        }

        // 重新排序UI
        int siblingIndex = 0;
        foreach (var kvp in sortedIndicators)
        {
            kvp.Value.transform.SetSiblingIndex(siblingIndex++);
        }
    }

    /// <summary>
    /// 获取敌人的头像Sprite
    /// （需要从敌人配置或资源管理器获取）
    /// </summary>
    private Sprite GetEnemyIcon(EnemyEntity enemy)
    {
        if (enemy == null || enemy.Config == null)
            return null;

        // TODO: 从资源管理器或配置中获取敌人头像
        // 临时返回null，UI会使用默认显示
        return null;
    }

    #endregion
}
