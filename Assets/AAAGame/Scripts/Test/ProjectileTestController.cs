using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 目标中心计算方式
/// </summary>
public enum TargetCenterMode
{
    /// <summary>自动检测（优先 Renderer，其次 Collider，最后 Offset）</summary>
    Auto,
    /// <summary>使用渲染器包围盒中心</summary>
    Renderer,
    /// <summary>使用碰撞器包围盒中心</summary>
    Collider,
    /// <summary>使用固定高度偏移</summary>
    Offset
}

/// <summary>
/// 投射物测试控制器（增强版）
/// 支持批量测试文件夹中的所有投射物预制体
/// 
/// 快捷键：
/// - Space: 发射单发投射物（随机方向）
/// - A: 切换自动连发模式
/// - R: 重置统计数据
/// - H: 显示/隐藏调试UI
/// - 左右方向键: 切换投射物
/// </summary>
public class ProjectileTestController : MonoBehaviour
{
    #region 配置参数

    [Header("投射物配置")]
    [SerializeField]
    [Tooltip("投射物预制体文件夹路径（相对于 Resources）")]
    private string m_ProjectileFolderPath = "";

    [SerializeField]
    [Tooltip("当前使用的投射物预制体")]
    private GameObject m_CurrentProjectilePrefab;

    [SerializeField]
    [Tooltip("投射物发射速度")]
    private float m_ProjectileSpeed = 10f;

    [SerializeField]
    [Tooltip("最大命中数（穿透数）")]
    private int m_MaxHitCount = 1;

    [Header("发射配置")]
    [SerializeField]
    [Tooltip("随机目标最小距离")]
    private float m_MinTargetDistance = 5f;

    [SerializeField]
    [Tooltip("随机目标最大距离")]
    private float m_MaxTargetDistance = 15f;

    [SerializeField]
    [Tooltip("随机方向角度范围（度）")]
    private float m_RandomAngleRange = 60f;

    [SerializeField]
    [Tooltip("发射高度偏移")]
    private float m_SpawnHeightOffset = 1f;

    [Header("追踪配置")]
    [SerializeField]
    [Tooltip("启用追踪模式")]
    private bool m_EnableHoming = false;

    [SerializeField]
    [Tooltip("追踪目标对象（可以是任意 Transform）")]
    private Transform m_HomingTarget;

    [SerializeField]
    [Tooltip("如果目标是 ChessEntity，自动获取")]
    private ChessEntity m_HomingTargetEntity;

    [SerializeField]
    [Tooltip("目标中心计算方式：Auto=自动检测, Renderer=使用渲染器包围盒, Collider=使用碰撞器包围盒, Offset=使用固定偏移")]
    private TargetCenterMode m_TargetCenterMode = TargetCenterMode.Auto;

    [SerializeField]
    [Tooltip("目标高度偏移（当使用 Offset 模式或作为备用值）")]
    private float m_TargetHeightOffset = 1f;

    [SerializeField]
    [Tooltip("追踪目标可视化颜色")]
    private Color m_HomingTargetGizmoColor = Color.cyan;

    [Header("自动发射配置")]
    [SerializeField]
    [Tooltip("自动发射间隔（秒）")]
    private float m_AutoFireInterval = 0.5f;

    [SerializeField]
    [Tooltip("启动时是否自动发射")]
    private bool m_AutoFireOnStart = false;

    [Header("调试配置")]
    [SerializeField]
    [Tooltip("是否显示调试UI")]
    private bool m_ShowDebugUI = true;

    [SerializeField]
    [Tooltip("是否绘制目标点")]
    private bool m_DrawTargetGizmos = true;

    [SerializeField]
    [Tooltip("模拟的发射者阵营")]
    private int m_OwnerCamp = 1;

    #endregion

    #region 私有字段

    /// <summary>所有可用的投射物预制体列表</summary>
    private List<GameObject> m_ProjectilePrefabs = new List<GameObject>();

    /// <summary>当前选中的投射物索引</summary>
    private int m_CurrentProjectileIndex = 0;

    private bool m_IsAutoFiring = false;
    private float m_AutoFireTimer = 0f;
    private Vector3 m_LastTargetPosition;
    private int m_TotalFiredCount = 0;
    private int m_TotalHitCount = 0;
    private ChessProjectile m_LastProjectile;

    /// <summary>是否已加载预制体列表</summary>
    private bool m_IsProjectilesLoaded = false;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        DebugEx.LogModule("ProjectileTestController", "投射物测试控制器已启动");
        LoadProjectilePrefabs();

        // 自动检测追踪目标是否为 ChessEntity
        if (m_HomingTarget != null && m_HomingTargetEntity == null)
        {
            m_HomingTargetEntity = m_HomingTarget.GetComponent<ChessEntity>();
            if (m_HomingTargetEntity != null)
            {
                DebugEx.LogModule("ProjectileTestController",
                    $"自动检测到追踪目标为 ChessEntity: {m_HomingTargetEntity.Config?.Name}");
            }
        }
    }

    private void Start()
    {
        // 验证配置
        if (m_ProjectilePrefabs.Count == 0)
        {
            DebugEx.ErrorModule("ProjectileTestController",
                $"未找到任何投射物预制体！请检查文件夹路径: {m_ProjectileFolderPath}");
            enabled = false;
            return;
        }

        // 设置初始投射物
        if (m_CurrentProjectilePrefab == null && m_ProjectilePrefabs.Count > 0)
        {
            m_CurrentProjectilePrefab = m_ProjectilePrefabs[0];
            m_CurrentProjectileIndex = 0;
        }

        m_IsAutoFiring = m_AutoFireOnStart;

        DebugEx.Success("ProjectileTestController",
            $"已加载 {m_ProjectilePrefabs.Count} 个投射物预制体");
        DebugEx.LogModule("ProjectileTestController", "按 H 键显示/隐藏快捷键说明");

        if (m_IsAutoFiring)
        {
            DebugEx.Success("ProjectileTestController", "自动发射模式已启动");
        }
    }

    private void Update()
    {
        // 快捷键已移至 Tools > Clash of Gods > Test Manager 窗口管理
        // HandleInput();

        // 自动发射
        if (m_IsAutoFiring)
        {
            m_AutoFireTimer += Time.deltaTime;
            if (m_AutoFireTimer >= m_AutoFireInterval)
            {
                FireProjectile();
                m_AutoFireTimer = 0f;
            }
        }
    }

    private void OnGUI()
    {
        if (!m_ShowDebugUI) return;
        DrawDebugUI();
    }

    private void OnDrawGizmos()
    {
        if (!m_DrawTargetGizmos) return;

        // 绘制最后一次的目标点
        if (m_LastTargetPosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_LastTargetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, m_LastTargetPosition);
        }

        // 绘制追踪目标
        if (m_EnableHoming && m_HomingTarget != null)
        {
            // 获取目标中心位置
            Vector3 targetCenter = GetTargetCenterPosition(m_HomingTarget);

            // 绘制目标中心点
            Gizmos.color = m_HomingTargetGizmoColor;
            Gizmos.DrawWireSphere(targetCenter, 0.5f);
            Gizmos.DrawLine(transform.position, targetCenter);

            // 绘制目标指示器（十字）
            Gizmos.DrawLine(targetCenter + Vector3.up * 0.5f, targetCenter + Vector3.up * 1.5f);
            Gizmos.DrawLine(targetCenter + Vector3.left * 0.3f, targetCenter + Vector3.right * 0.3f);
            Gizmos.DrawLine(targetCenter + Vector3.forward * 0.3f, targetCenter + Vector3.back * 0.3f);

            // 绘制从目标底部到中心的连线（显示偏移）
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(m_HomingTarget.position, targetCenter);
            Gizmos.DrawWireSphere(m_HomingTarget.position, 0.2f);
        }

        // 绘制发射范围扇形（仅在非追踪模式下）
        if (!m_EnableHoming)
        {
            Gizmos.color = Color.yellow;
            Vector3 forward = transform.forward;
            Vector3 leftBound = Quaternion.Euler(0, -m_RandomAngleRange / 2, 0) * forward;
            Vector3 rightBound = Quaternion.Euler(0, m_RandomAngleRange / 2, 0) * forward;

            Gizmos.DrawRay(transform.position, leftBound * m_MaxTargetDistance);
            Gizmos.DrawRay(transform.position, rightBound * m_MaxTargetDistance);
        }
    }

    #endregion

    #region 预制体加载

    /// <summary>
    /// 加载文件夹中的所有投射物预制体
    /// </summary>
    private void LoadProjectilePrefabs()
    {
        m_ProjectilePrefabs.Clear();

#if UNITY_EDITOR
        // 编辑器模式：使用 AssetDatabase 扫描
        // 确保路径以 Assets 开头
        string searchPath = m_ProjectileFolderPath;
        if (!searchPath.StartsWith("Assets/") && !searchPath.StartsWith("Assets\\"))
        {
            searchPath = "Assets/" + searchPath;
        }

        // 检查文件夹是否存在
        if (!AssetDatabase.IsValidFolder(searchPath))
        {
            DebugEx.ErrorModule("ProjectileTestController",
                $"文件夹不存在: {searchPath}");
            m_IsProjectilesLoaded = true;
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });

        DebugEx.LogModule("ProjectileTestController",
            $"开始扫描文件夹: {searchPath}，找到 {guids.Length} 个预制体");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
            {
                // 检查是否包含 ChessProjectile 组件（可选）
                ChessProjectile projectileComponent = prefab.GetComponent<ChessProjectile>();

                m_ProjectilePrefabs.Add(prefab);

                string componentInfo = projectileComponent != null ? "✓" : "✗";
                DebugEx.LogModule("ProjectileTestController",
                    $"加载预制体: {prefab.name} [ChessProjectile: {componentInfo}]");
            }
        }
#else
    // 运行时模式：需要预制体在 Resources 文件夹中
    DebugEx.WarningModule("ProjectileTestController", 
        "运行时模式下，预制体必须放在 Resources 文件夹中才能加载");
    
    // 尝试从 Resources 加载（需要用户手动将预制体放入 Resources）
    string resourcePath = m_ProjectileFolderPath.Replace("Assets/", "").Replace("Resources/", "");
    GameObject[] prefabs = Resources.LoadAll<GameObject>(resourcePath);
    
    DebugEx.LogModule("ProjectileTestController", 
        $"从 Resources/{resourcePath} 加载了 {prefabs.Length} 个预制体");

    foreach (GameObject prefab in prefabs)
    {
        m_ProjectilePrefabs.Add(prefab);
    }
#endif

        m_IsProjectilesLoaded = true;

        if (m_ProjectilePrefabs.Count > 0)
        {
            DebugEx.Success("ProjectileTestController",
                $"成功加载 {m_ProjectilePrefabs.Count} 个投射物预制体");
        }
        else
        {
            DebugEx.WarningModule("ProjectileTestController",
                "未找到任何投射物预制体，请检查文件夹路径配置");
        }
    }

    #endregion

    // 键盘输入已移至 GameTestWindow，此方法已删除

    #region 投射物切换

    /// <summary>
    /// 切换投射物
    /// </summary>
    /// <param name="direction">方向（-1=上一个，1=下一个）</param>
    public void SwitchProjectile(int direction)
    {
        if (m_ProjectilePrefabs.Count == 0) return;

        m_CurrentProjectileIndex += direction;

        // 循环索引
        if (m_CurrentProjectileIndex < 0)
        {
            m_CurrentProjectileIndex = m_ProjectilePrefabs.Count - 1;
        }
        else if (m_CurrentProjectileIndex >= m_ProjectilePrefabs.Count)
        {
            m_CurrentProjectileIndex = 0;
        }

        m_CurrentProjectilePrefab = m_ProjectilePrefabs[m_CurrentProjectileIndex];

        DebugEx.Success("ProjectileTestController",
            $"切换投射物: [{m_CurrentProjectileIndex + 1}/{m_ProjectilePrefabs.Count}] {m_CurrentProjectilePrefab.name}");

        // 重置统计
        ResetStatistics();
    }

    /// <summary>
    /// 切换追踪模式
    /// </summary>
    public void ToggleHoming()
    {
        m_EnableHoming = !m_EnableHoming;

        string status = m_EnableHoming ? "开启" : "关闭";
        string targetInfo = "";

        if (m_EnableHoming)
        {
            if (m_HomingTarget != null)
            {
                targetInfo = $"，目标: {m_HomingTarget.name}";
            }
            else
            {
                DebugEx.WarningModule("ProjectileTestController",
                    "追踪模式已开启，但未设置追踪目标！");
            }
        }

        DebugEx.LogModule("ProjectileTestController",
            $"追踪模式: {status}{targetInfo}");
    }

    /// <summary>
    /// 设置当前投射物（通过索引）
    /// </summary>
    public void SetProjectileByIndex(int index)
    {
        if (index < 0 || index >= m_ProjectilePrefabs.Count) return;

        m_CurrentProjectileIndex = index;
        m_CurrentProjectilePrefab = m_ProjectilePrefabs[index];

        DebugEx.Success("ProjectileTestController",
            $"选择投射物: [{index + 1}/{m_ProjectilePrefabs.Count}] {m_CurrentProjectilePrefab.name}");

        // 重置统计
        ResetStatistics();
    }

    #endregion

    #region 投射物发射

    /// <summary>
    /// 发射投射物
    /// </summary>
    public void FireProjectile()
    {
        if (m_CurrentProjectilePrefab == null)
        {
            return;
        }

        // 计算发射位置
        Vector3 spawnPosition = transform.position + Vector3.up * m_SpawnHeightOffset;

        // 确定目标位置和目标实体
        Vector3 targetPosition;
        ChessEntity targetEntity = null;

        if (m_EnableHoming && m_HomingTarget != null)
        {
            // 追踪模式 - 使用目标中心位置
            targetPosition = GetTargetCenterPosition(m_HomingTarget);
            targetEntity = m_HomingTargetEntity; // 可能为 null
        }
        else
        {
            // 随机目标模式
            targetPosition = GenerateRandomTargetPosition();
            targetEntity = null;
        }

        m_LastTargetPosition = targetPosition;

        // 计算发射方向
        Vector3 direction = (targetPosition - spawnPosition).normalized;

        // 实例化投射物
        GameObject projectileObj = Instantiate(
            m_CurrentProjectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(direction)
        );

        // 获取或添加 ChessProjectile 组件
        ChessProjectile projectile = projectileObj.GetComponent<ChessProjectile>();
        if (projectile == null)
        {
            projectile = projectileObj.AddComponent<ChessProjectile>();
        }

        // 初始化投射物 
        projectile.Initialize(
            m_OwnerCamp,
            targetEntity,  // 如果是追踪模式且目标是 ChessEntity，传入目标
            targetPosition,
            direction,     // ⭐ 传入计算好的发射方向
            m_ProjectileSpeed,
            m_MaxHitCount,
            OnProjectileHit
        );

        projectile.SetDestroyCallback(OnProjectileDestroyed);

        m_LastProjectile = projectile;
        m_TotalFiredCount++;
    }

    /// <summary>
    /// 生成随机目标位置
    /// </summary>
    private Vector3 GenerateRandomTargetPosition()
    {
        // 随机距离
        float distance = Random.Range(m_MinTargetDistance, m_MaxTargetDistance);

        // 随机角度（相对于当前朝向）
        float randomAngle = Random.Range(-m_RandomAngleRange / 2, m_RandomAngleRange / 2);

        // 计算方向
        Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * transform.forward;

        // 计算目标位置（保持在地面高度）
        Vector3 targetPosition = transform.position + direction * distance;
        targetPosition.y = transform.position.y;

        return targetPosition;
    }

    /// <summary>
    /// 获取目标的中心位置
    /// </summary>
    /// <param name="target">目标 Transform</param>
    /// <returns>目标中心位置</returns>
    private Vector3 GetTargetCenterPosition(Transform target)
    {
        if (target == null)
        {
            DebugEx.WarningModule("ProjectileTestController", "目标为空，返回零向量");
            return Vector3.zero;
        }

        Vector3 centerPosition = target.position;
        string method = "Transform.position";

        switch (m_TargetCenterMode)
        {
            case TargetCenterMode.Auto:
                // 自动检测：优先 Renderer，其次 Collider，最后 Offset
                Renderer renderer = target.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    centerPosition = renderer.bounds.center;
                    method = "Renderer.bounds.center";
                    break;
                }

                Collider collider = target.GetComponentInChildren<Collider>();
                if (collider != null)
                {
                    centerPosition = collider.bounds.center;
                    method = "Collider.bounds.center";
                    break;
                }

                // 使用固定偏移作为备用
                centerPosition = target.position + Vector3.up * m_TargetHeightOffset;
                method = $"Offset ({m_TargetHeightOffset}m)";
                break;

            case TargetCenterMode.Renderer:
                renderer = target.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    centerPosition = renderer.bounds.center;
                    method = "Renderer.bounds.center";
                }
                else
                {
                    DebugEx.WarningModule("ProjectileTestController",
                        $"目标 {target.name} 没有 Renderer 组件，使用固定偏移");
                    centerPosition = target.position + Vector3.up * m_TargetHeightOffset;
                    method = $"Offset (fallback)";
                }
                break;

            case TargetCenterMode.Collider:
                collider = target.GetComponentInChildren<Collider>();
                if (collider != null)
                {
                    centerPosition = collider.bounds.center;
                    method = "Collider.bounds.center";
                }
                else
                {
                    DebugEx.WarningModule("ProjectileTestController",
                        $"目标 {target.name} 没有 Collider 组件，使用固定偏移");
                    centerPosition = target.position + Vector3.up * m_TargetHeightOffset;
                    method = $"Offset (fallback)";
                }
                break;

            case TargetCenterMode.Offset:
                centerPosition = target.position + Vector3.up * m_TargetHeightOffset;
                method = $"Offset ({m_TargetHeightOffset}m)";
                break;
        }

        DebugEx.LogModule("ProjectileTestController",
            $"目标中心位置: {centerPosition:F2} (方法: {method})");

        return centerPosition;
    }

    #endregion

    #region 回调处理

    /// <summary>
    /// 投射物命中回调
    /// </summary>
    private void OnProjectileHit(ChessEntity target)
    {
        m_TotalHitCount++;

        DebugEx.Success("ProjectileTestController",
            $"投射物命中目标: {target.Config?.Name ?? "未知"} (总命中: {m_TotalHitCount})");
    }

    /// <summary>
    /// 投射物销毁回调
    /// </summary>
    private void OnProjectileDestroyed()
    {
        DebugEx.LogModule("ProjectileTestController", "投射物已销毁");
        m_LastProjectile = null;
    }

    #endregion

    #region 调试功能

    /// <summary>
    /// 重置统计数据
    /// </summary>
    public void ResetStats()
    {
        ResetStatistics();
    }

    private void ResetStatistics()
    {
        m_TotalFiredCount = 0;
        m_TotalHitCount = 0;
        m_LastTargetPosition = Vector3.zero;

        DebugEx.LogModule("ProjectileTestController", "统计数据已重置");
    }

    /// <summary>
    /// 绘制调试UI
    /// </summary>
    private void DrawDebugUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 450, 600));
        GUILayout.BeginVertical("box");

        // 标题
        GUILayout.Label("<b><size=16>投射物批量测试控制器</size></b>", GetTitleStyle());
        GUILayout.Space(10);

        // 当前投射物信息
        GUILayout.Label("<b>当前投射物</b>", GetHeaderStyle());
        if (m_CurrentProjectilePrefab != null)
        {
            GUILayout.Label($"名称: {m_CurrentProjectilePrefab.name}");
            GUILayout.Label($"索引: {m_CurrentProjectileIndex + 1} / {m_ProjectilePrefabs.Count}");
        }
        else
        {
            GUILayout.Label("未选择投射物");
        }
        GUILayout.Space(10);

        // 配置信息
        GUILayout.Label("<b>配置信息</b>", GetHeaderStyle());
        GUILayout.Label($"发射速度: {m_ProjectileSpeed} m/s");
        GUILayout.Label($"目标距离: {m_MinTargetDistance:F1} - {m_MaxTargetDistance:F1} m");
        GUILayout.Label($"角度范围: ±{m_RandomAngleRange / 2:F0}°");
        GUILayout.Label($"最大命中数: {m_MaxHitCount}");
        GUILayout.Space(10);

        // 统计信息
        GUILayout.Label("<b>统计信息</b>", GetHeaderStyle());
        GUILayout.Label($"发射总数: {m_TotalFiredCount}");
        GUILayout.Label($"命中总数: {m_TotalHitCount}");

        float hitRate = m_TotalFiredCount > 0 ? (float)m_TotalHitCount / m_TotalFiredCount * 100f : 0f;
        GUILayout.Label($"命中率: {hitRate:F1}%");
        GUILayout.Space(10);

        // 状态
        GUILayout.Label("<b>当前状态</b>", GetHeaderStyle());
        GUILayout.Label($"自动发射: {(m_IsAutoFiring ? "<color=green>开启</color>" : "<color=red>关闭</color>")}");
        GUILayout.Label($"追踪模式: {(m_EnableHoming ? "<color=cyan>开启</color>" : "<color=red>关闭</color>")}");

        if (m_EnableHoming)
        {
            if (m_HomingTarget != null)
            {
                string targetName = m_HomingTarget.name;
                string entityInfo = m_HomingTargetEntity != null ? $" ({m_HomingTargetEntity.Config?.Name})" : "";
                GUILayout.Label($"追踪目标: <color=cyan>{targetName}{entityInfo}</color>");

                // 显示目标中心计算方式
                GUILayout.Label($"中心计算: {m_TargetCenterMode}");

                // 显示目标中心位置
                Vector3 centerPos = GetTargetCenterPosition(m_HomingTarget);
                GUILayout.Label($"目标中心: {centerPos:F2}");
            }
            else
            {
                GUILayout.Label("<color=red>追踪目标: 未设置</color>");
            }
        }

        if (m_IsAutoFiring)
        {
            float progress = m_AutoFireTimer / m_AutoFireInterval;
            GUILayout.Label($"下次发射: {(m_AutoFireInterval - m_AutoFireTimer):F2}s");
        }
        GUILayout.Space(10);

        // 快捷键说明
        GUILayout.Label("<b>快捷键</b>", GetHeaderStyle());
        GUILayout.Label("Space - 发射单发投射物");
        GUILayout.Label("A - 切换自动发射");
        GUILayout.Label("T - 切换追踪模式");
        GUILayout.Label("R - 重置统计数据");
        GUILayout.Label("← → - 切换投射物");
        GUILayout.Label("H - 显示/隐藏此面板");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private GUIStyle GetTitleStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }

    private GUIStyle GetHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.fontStyle = FontStyle.Bold;
        return style;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 手动发射投射物（可通过代码调用）
    /// </summary>
    public void ManualFire()
    {
        FireProjectile();
    }

    /// <summary>
    /// 设置自动发射状态
    /// </summary>
    public void SetAutoFire(bool enabled)
    {
        m_IsAutoFiring = enabled;
        m_AutoFireTimer = 0f;
    }

    /// <summary>
    /// 获取所有投射物预制体列表
    /// </summary>
    public List<GameObject> GetProjectilePrefabs()
    {
        return m_ProjectilePrefabs;
    }

    #endregion

#if UNITY_EDITOR

    #region 自定义 Inspector

    [CustomEditor(typeof(ProjectileTestController), true)]
    public class ProjectileTestControllerInspector : Editor
    {
        private Vector2 m_ScrollPosition;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            ProjectileTestController controller = (ProjectileTestController)target;

            // 刷新预制体列表按钮
            if (GUILayout.Button("刷新投射物列表", GUILayout.Height(25)))
            {
                controller.LoadProjectilePrefabs();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("投射物列表", EditorStyles.boldLabel);

            List<GameObject> prefabs = controller.GetProjectilePrefabs();

            if (prefabs == null || prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到投射物预制体，请检查文件夹路径配置", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"共 {prefabs.Count} 个投射物", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            // 滚动视图
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.MaxHeight(400));

            // 绘制按钮网格（每行2个）
            int columns = 2;
            for (int i = 0; i < prefabs.Count; i += columns)
            {
                EditorGUILayout.BeginHorizontal();

                for (int j = 0; j < columns && (i + j) < prefabs.Count; j++)
                {
                    int index = i + j;
                    GameObject prefab = prefabs[index];

                    if (prefab == null) continue;

                    // 按钮样式（当前选中的高亮显示）
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.alignment = TextAnchor.MiddleCenter;
                    buttonStyle.wordWrap = true;

                    if (index == controller.m_CurrentProjectileIndex)
                    {
                        buttonStyle.normal.textColor = Color.green;
                        buttonStyle.fontStyle = FontStyle.Bold;
                    }

                    if (GUILayout.Button(prefab.name, buttonStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                    {
                        controller.SetProjectileByIndex(index);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 追踪测试
            EditorGUILayout.LabelField("追踪测试", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(controller.m_EnableHoming ? "关闭追踪" : "开启追踪", GUILayout.Height(30)))
            {
                controller.ToggleHoming();
            }

            // 显示追踪目标状态
            if (controller.m_HomingTarget != null)
            {
                string targetInfo = controller.m_HomingTarget.name;
                if (controller.m_HomingTargetEntity != null)
                {
                    targetInfo += $" (ChessEntity)";
                }
                EditorGUILayout.LabelField($"目标: {targetInfo}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("未设置追踪目标", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 测试按钮
            EditorGUILayout.LabelField("测试操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("发射单发", GUILayout.Height(30)))
            {
                controller.ManualFire();
            }
            if (GUILayout.Button(controller.m_IsAutoFiring ? "停止自动发射" : "开始自动发射", GUILayout.Height(30)))
            {
                controller.SetAutoFire(!controller.m_IsAutoFiring);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("上一个", GUILayout.Height(30)))
            {
                controller.SwitchProjectile(-1);
            }
            if (GUILayout.Button("下一个", GUILayout.Height(30)))
            {
                controller.SwitchProjectile(1);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("重置统计", GUILayout.Height(25)))
            {
                controller.ResetStatistics();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    #endregion

#endif
}
