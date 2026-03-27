using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 棋子描边控制器
/// 根据阵营关系自动管理描边显示
/// </summary>
public class ChessOutlineController : MonoBehaviour
{
    #region 配置

    [Header("描边配置资源")]
    [Tooltip("敌人描边配置（红色）")]
    [SerializeField] private OutlineConfig m_EnemyOutlineConfig;

    [Tooltip("友军描边配置（绿色）")]
    [SerializeField] private OutlineConfig m_AllyOutlineConfig;

    [Tooltip("选中描边配置（特殊颜色）")]
    [SerializeField] private OutlineConfig m_SelectedOutlineConfig;

    [Tooltip("中立/可交互对象描边配置（黄色）")]
    [SerializeField] private OutlineConfig m_NeutralOutlineConfig;

    [Header("配置资源路径（如果未直接赋值则从此路径加载）")]
    [SerializeField] private string m_EnemyConfigPath = "Assets/TA/OuterGlow/Enemy.asset";
    [SerializeField] private string m_AllyConfigPath = "Assets/TA/OuterGlow/Ally.asset";
    [SerializeField] private string m_SelectedConfigPath = "Assets/TA/OuterGlow/Selected.asset";
    [SerializeField] private string m_NeutralConfigPath = "Assets/TA/OuterGlow/Interactive.asset";

    #endregion

    #region 私有字段

    /// <summary>所属棋子实体</summary>
    private ChessEntity m_Entity;

    /// <summary>Renderer列表</summary>
    private List<Renderer> m_Renderers = new List<Renderer>();

    /// <summary>是否被选中</summary>
    private bool m_IsSelected;

    /// <summary>当前显示的描边配置</summary>
    private OutlineConfig m_CurrentConfig;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region 公共属性

    /// <summary>是否被选中</summary>
    public bool IsSelected => m_IsSelected;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化描边控制器
    /// </summary>
    /// <param name="entity">所属棋子实体</param>
    public void Initialize(ChessEntity entity)
    {
        m_Entity = entity;

        // 缓存所有Renderer
        CacheRenderers();

        // 加载描边配置
        LoadOutlineConfigs();

        m_IsInitialized = true;

        // 根据阵营关系设置初始描边
        RefreshOutline();

        Debug.Log($"[ChessOutlineController] 初始化完成: {entity.Config?.Name}, Camp={entity.Camp}");
    }

    /// <summary>
    /// 缓存所有Renderer
    /// </summary>
    private void CacheRenderers()
    {
        m_Renderers.Clear();
        var renderers = GetComponentsInChildren<Renderer>();
        m_Renderers.AddRange(renderers);
    }

    /// <summary>
    /// 加载描边配置
    /// </summary>
    private void LoadOutlineConfigs()
    {
#if UNITY_EDITOR
        if (m_EnemyOutlineConfig == null)
        {
            m_EnemyOutlineConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<OutlineConfig>(m_EnemyConfigPath);
        }
        if (m_AllyOutlineConfig == null)
        {
            m_AllyOutlineConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<OutlineConfig>(m_AllyConfigPath);
        }
        if (m_SelectedOutlineConfig == null)
        {
            m_SelectedOutlineConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<OutlineConfig>(m_SelectedConfigPath);
        }
        if (m_NeutralOutlineConfig == null)
        {
            m_NeutralOutlineConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<OutlineConfig>(m_NeutralConfigPath);
        }
#endif

        // 警告日志
        if (m_EnemyOutlineConfig == null)
        {
            Debug.LogWarning("[ChessOutlineController] 未找到敌人描边配置");
        }
        if (m_AllyOutlineConfig == null)
        {
            Debug.LogWarning("[ChessOutlineController] 未找到友军描边配置");
        }
        if (m_NeutralOutlineConfig == null)
        {
            Debug.LogWarning("[ChessOutlineController] 未找到中立/可交互描边配置");
        }
    }

    #endregion

    #region 公共API

    /// <summary>
    /// 设置选中状态
    /// </summary>
    /// <param name="selected">是否选中</param>
    public void SetSelected(bool selected)
    {
        if (m_IsSelected == selected) return;

        m_IsSelected = selected;
        RefreshOutline();

        Debug.Log($"[ChessOutlineController] 选中状态变化: {m_Entity?.Config?.Name}, Selected={selected}");
    }

    /// <summary>
    /// 刷新描边显示
    /// 当阵营关系变化时调用
    /// </summary>
    public void RefreshOutline()
    {
        if (!m_IsInitialized || m_Entity == null) return;

        OutlineConfig targetConfig = GetTargetOutlineConfig();

        // 如果配置没变，不需要更新
        if (targetConfig == m_CurrentConfig) return;

        // 移除旧描边
        RemoveOutline();

        // 应用新描边
        if (targetConfig != null)
        {
            ApplyOutline(targetConfig);
        }

        m_CurrentConfig = targetConfig;
    }

    /// <summary>
    /// 强制移除描边
    /// </summary>
    public void RemoveOutline()
    {
        if (m_Renderers.Count == 0) return;

        OutlineRenderFeature.Instance?.RemoveDrawOutlines(m_Renderers);
        m_CurrentConfig = null;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取目标描边配置
    /// </summary>
    private OutlineConfig GetTargetOutlineConfig()
    {
        // 选中状态优先
        if (m_IsSelected && m_SelectedOutlineConfig != null)
        {
            return m_SelectedOutlineConfig;
        }

        // 根据阵营关系决定描边
        CampRelation relation = CampRelationService.GetRelationToLocalPlayer(m_Entity.Camp);

        switch (relation)
        {
            case CampRelation.Enemy:
                return m_EnemyOutlineConfig;

            case CampRelation.Ally:
                return m_AllyOutlineConfig;

            case CampRelation.Neutral:
                return m_NeutralOutlineConfig; // ⭐ 中立/可交互对象也显示描边

            case CampRelation.Self:
            default:
                return null;
        }
    }

    /// <summary>
    /// 应用描边
    /// </summary>
    private void ApplyOutline(OutlineConfig config)
    {
        if (config == null || m_Renderers.Count == 0) return;

        float outlineSize = config.OutlineSize;

        // 如果启用了距离缩放，计算实际大小
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (config.EnableDistanceScaling && playerCamera != null)
        {
            float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
            outlineSize = config.CalculateOutlineSize(distance);
        }

        OutlineRenderFeature.Instance?.DrawOrUpdateOutlines(
            m_Renderers,
            config.OutlineColor,
            outlineSize
        );
    }

    #endregion

    #region Unity生命周期

    private void OnDestroy()
    {
        RemoveOutline();
    }

    #endregion
}
