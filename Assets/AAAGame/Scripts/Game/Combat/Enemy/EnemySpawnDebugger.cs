using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 敌人生成调试工具
/// 用于在编辑器中测试和调整敌人生成的效果和算法
/// 在整个NavMesh上随机生成敌人，满足间距约束
/// </summary>
public class EnemySpawnDebugger : MonoBehaviour
{
    #region 序列化字段

    [Header("敌人配置")]
    [SerializeField]
    [Tooltip("敌人预制体引用 (EnemyEntity)")]
    private EnemyEntity m_EnemyPrefab;

    [SerializeField]
    [Tooltip("敌人生成数量")]
    private int m_EnemyCount = 3;

    [Header("生成约束")]
    [SerializeField]
    [Tooltip("敌人之间的最小间隔距离")]
    private float m_MinSpacing = 1.5f;

    [SerializeField]
    [Tooltip("安全区域检查半径（生成点周围半径内必须都是Walkable的NavMesh）")]
    private float m_SafetyRadius = 0.5f;

    [SerializeField]
    [Tooltip("安全区域采样密度（每个轴向的采样点数，越多检查越严格，推荐3-5）")]
    private int m_GridSampleDensity = 3;

    [SerializeField]
    [Tooltip("单个位置寻位最大重试次数")]
    private int m_MaxRetries = 50;

    [Header("调试选项")]
    [SerializeField]
    [Tooltip("是否显示生成位置的Gizmos")]
    private bool m_ShowGizmos = true;

    [SerializeField]
    [Tooltip("生成位置的Gizmos球体半径")]
    private float m_GizmosRadius = 0.3f;

    #endregion

    #region 私有字段

    /// <summary>已生成的敌人列表</summary>
    private readonly List<EnemyEntity> m_SpawnedEnemies = new();

    /// <summary>生成统计数据</summary>
    private readonly SpawnStatistics m_Statistics = new();

    /// <summary>缓存的NavMesh三角剖分</summary>
    private NavMeshTriangulation m_CachedTriangulation;

    /// <summary>预计算的采样点偏移（用于安全区检查）</summary>
    private Vector3[] m_CachedSampleOffsets;

    /// <summary>最小间距的平方值（用于快速距离检查）</summary>
    private float m_MinSpacingSqr;

    #endregion

    /// <summary>生成统计数据结构</summary>
    private class SpawnStatistics
    {
        public int TotalAttempts = 0;
        public int SuccessfulAttempts = 0;
        public int NavMeshFailures = 0;
        public int SpacingFailures = 0;
        public int SafetyFailures = 0;
        public int InstantiateFailures = 0;

        public void Reset()
        {
            TotalAttempts = 0;
            SuccessfulAttempts = 0;
            NavMeshFailures = 0;
            SpacingFailures = 0;
            SafetyFailures = 0;
            InstantiateFailures = 0;
        }

        public string GetReport()
        {
            return $"\n" +
                $"  ├─ 总尝试次数: {TotalAttempts}\n" +
                $"  ├─ 成功位置: {SuccessfulAttempts}\n" +
                $"  ├─ NavMesh 采样失败: {NavMeshFailures}\n" +
                $"  ├─ 间距冲突: {SpacingFailures}\n" +
                $"  ├─ 安全区域检查失败: {SafetyFailures}\n" +
                $"  └─ 实例化异常: {InstantiateFailures}";
        }
    }

    #region 编辑器方法

#if UNITY_EDITOR

    /// <summary>
    /// 在编辑器中显示自定义GUI
    /// </summary>
    public void OnInspectorGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("敌人生成调试工具", EditorStyles.boldLabel);

        // 配置检查
        if (m_EnemyPrefab == null)
        {
            EditorGUILayout.HelpBox("请先指定敌人预制体 (EnemyEntity)", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("生成参数", EditorStyles.boldLabel);
        m_EnemyCount = EditorGUILayout.IntSlider("敌人数量", m_EnemyCount, 1, 100);
        m_MinSpacing = EditorGUILayout.FloatField("敌人间距", Mathf.Max(0.1f, m_MinSpacing));
        m_SafetyRadius = EditorGUILayout.FloatField("安全检查半径", Mathf.Max(0.1f, m_SafetyRadius));
        m_GridSampleDensity = EditorGUILayout.IntSlider("采样密度（网格）", Mathf.Max(1, m_GridSampleDensity), 1, 5);
        m_MaxRetries = EditorGUILayout.IntSlider("重试次数", m_MaxRetries, 10, 200000);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Gizmos显示", EditorStyles.boldLabel);
        m_ShowGizmos = EditorGUILayout.Toggle("显示生成位置", m_ShowGizmos);
        if (m_ShowGizmos)
        {
            m_GizmosRadius = EditorGUILayout.FloatField("Gizmos球体半径", Mathf.Max(0.1f, m_GizmosRadius));
        }

        EditorGUILayout.Space(10);

        // 生成/清除按钮
        EditorGUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("生成敌人", GUILayout.Height(35)))
            {
                SpawnEnemies();
            }
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("清除敌人", GUILayout.Height(35)))
            {
                ClearEnemies();
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        // 显示已生成数量
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox($"已生成敌人数量: {m_SpawnedEnemies.Count}", MessageType.Info);
    }

#endif

    #endregion

    #region 公共方法

    /// <summary>
    /// 生成敌人
    /// </summary>
    public void SpawnEnemies()
    {
        if (m_EnemyPrefab == null)
        {
            DebugEx.Error("EnemySpawnDebugger", "敌人预制体未指定");
            return;
        }

        ClearEnemies();
        m_Statistics.Reset();

        DebugEx.Log("EnemySpawnDebugger", $"========== 敌人生成开始 ==========\n" +
            $"  ├─ 目标数量: {m_EnemyCount}\n" +
            $"  ├─ 最小间距: {m_MinSpacing:F2}\n" +
            $"  ├─ 安全检查半径: {m_SafetyRadius:F2}\n" +
            $"  ├─ 采样密度: {m_GridSampleDensity}\n" +
            $"  └─ 最大重试次数: {m_MaxRetries}");

        // 生成位置列表（在整个NavMesh上随机）
        List<Vector3> positions = GenerateSpawnPositions();

        for (int i = 0; i < positions.Count; i++)
        {
            SpawnSingleEnemy(positions[i], i);
        }

        string resultMessage = $"========== 敌人生成完成 ==========\n" +
            $"  ├─ 成功数量: {m_SpawnedEnemies.Count}/{m_EnemyCount}\n" +
            $"{m_Statistics.GetReport()}";

        if (m_SpawnedEnemies.Count == m_EnemyCount)
        {
            DebugEx.Success("EnemySpawnDebugger", resultMessage);
        }
        else if (m_SpawnedEnemies.Count > 0)
        {
            DebugEx.Warning("EnemySpawnDebugger", resultMessage);
        }
        else
        {
            DebugEx.Error("EnemySpawnDebugger", resultMessage);
        }
    }

    /// <summary>
    /// 清除所有已生成的敌人
    /// </summary>
    public void ClearEnemies()
    {
        for (int i = m_SpawnedEnemies.Count - 1; i >= 0; i--)
        {
            EnemyEntity enemy = m_SpawnedEnemies[i];
            if (enemy != null)
            {
                DestroyImmediate(enemy.gameObject);
            }
        }
        m_SpawnedEnemies.Clear();
        DebugEx.Log("EnemySpawnDebugger", "已清除所有敌人");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 生成指定数量的有效生成位置（在整个NavMesh上随机选择）
    /// 考虑最小间距约束
    /// </summary>
    private List<Vector3> GenerateSpawnPositions()
    {
        // 初始化缓存
        m_CachedTriangulation = NavMesh.CalculateTriangulation();
        if (m_CachedTriangulation.indices.Length == 0)
        {
            DebugEx.Error("EnemySpawnDebugger", "NavMesh 为空或未烘烤");
            return new();
        }

        m_MinSpacingSqr = m_MinSpacing * m_MinSpacing;
        PrecomputeSampleOffsets();

        List<Vector3> positions = new();
        int maxGlobalAttempts = m_EnemyCount * m_MaxRetries;

        while (positions.Count < m_EnemyCount && m_Statistics.TotalAttempts < maxGlobalAttempts)
        {
            m_Statistics.TotalAttempts++;

            // 在整个NavMesh上随机找一个点
            if (!RandomNavMeshPoint(out Vector3 randomPos))
            {
                m_Statistics.NavMeshFailures++;
                DebugEx.Error("EnemySpawnDebugger", $"尝试 #{m_Statistics.TotalAttempts}: NavMesh 采样失败");
                break;
            }

            // 检查是否与已生成的位置冲突
            if (!IsValidSpawnPosition(randomPos, positions))
            {
                continue;
            }

            positions.Add(randomPos);
            m_Statistics.SuccessfulAttempts++;
            DebugEx.Success("EnemySpawnDebugger",
                $"✓ 找到有效位置 #{positions.Count} (尝试次数: {m_Statistics.TotalAttempts})" +
                $"\n  └─ 坐标: ({randomPos.x:F2}, {randomPos.y:F2}, {randomPos.z:F2})");
        }

        if (positions.Count < m_EnemyCount)
        {
            string suggestion = m_Statistics.SpacingFailures > m_Statistics.SafetyFailures * 2
                ? "✗ 间距冲突过多 → 减少敌人数量或增加 m_MinSpacing 容差"
                : m_Statistics.SafetyFailures > 0
                ? "✗ 安全区域检查失败 → 增加 m_SafetyRadius 或降低 m_GridSampleDensity"
                : "✗ 无法找到足够位置 → 增加 m_MaxRetries 或检查 NavMesh 完整性";

            DebugEx.Warning("EnemySpawnDebugger",
                $"========== 位置生成未完成 ==========\n" +
                $"  ├─ 成功: {positions.Count}/{m_EnemyCount}\n" +
                $"  ├─ 总尝试: {m_Statistics.TotalAttempts}/{maxGlobalAttempts}\n" +
                $"  ├─ NavMesh 失败: {m_Statistics.NavMeshFailures}\n" +
                $"  ├─ 间距冲突: {m_Statistics.SpacingFailures}\n" +
                $"  ├─ 安全区检查失败: {m_Statistics.SafetyFailures}\n" +
                $"  └─ 建议: {suggestion}");
        }
        else
        {
            DebugEx.Log("EnemySpawnDebugger",
                $"========== 位置生成成功 ==========\n" +
                $"  ├─ 成功位置: {positions.Count}\n" +
                $"  ├─ 总尝试: {m_Statistics.TotalAttempts}\n" +
                $"  ├─ NavMesh 失败: {m_Statistics.NavMeshFailures}\n" +
                $"  ├─ 间距冲突: {m_Statistics.SpacingFailures}\n" +
                $"  └─ 安全区检查失败: {m_Statistics.SafetyFailures}");
        }

        return positions;
    }

    /// <summary>
    /// 预计算采样点偏移（避免运行时分配）
    /// </summary>
    private void PrecomputeSampleOffsets()
    {
        float step = m_SafetyRadius * 2f / (m_GridSampleDensity + 1);
        int sampleCount = (2 * m_GridSampleDensity + 1) * (2 * m_GridSampleDensity + 1);
        m_CachedSampleOffsets = new Vector3[sampleCount];

        int idx = 0;
        for (int x = -m_GridSampleDensity; x <= m_GridSampleDensity; x++)
        {
            for (int z = -m_GridSampleDensity; z <= m_GridSampleDensity; z++)
            {
                m_CachedSampleOffsets[idx++] = new Vector3(x * step, 0, z * step);
            }
        }
    }

    /// <summary>
    /// 在NavMesh上随机找一个点（使用缓存的三角剖分）
    /// </summary>
    private bool RandomNavMeshPoint(out Vector3 result)
    {
        result = Vector3.zero;

        if (m_CachedTriangulation.indices.Length == 0)
            return false;

        // 随机选择一个三角形
        int triangleIndex = Random.Range(0, m_CachedTriangulation.indices.Length / 3);
        int vertIndex = triangleIndex * 3;

        Vector3 v0 = m_CachedTriangulation.vertices[m_CachedTriangulation.indices[vertIndex]];
        Vector3 v1 = m_CachedTriangulation.vertices[m_CachedTriangulation.indices[vertIndex + 1]];
        Vector3 v2 = m_CachedTriangulation.vertices[m_CachedTriangulation.indices[vertIndex + 2]];

        // 在三角形内随机生成点（使用重心坐标）
        float r1 = Random.value;
        float r2 = Random.value;

        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        result = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
        return true;
    }

    /// <summary>
    /// 检查位置是否有效
    /// 条件1: 满足最小间距
    /// 条件2: 周围安全区域内都是Walkable的NavMesh
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 pos, List<Vector3> existingPositions)
    {
        // 条件1: 检查与已有位置的距离（用平方距离避免sqrt）
        foreach (var existingPos in existingPositions)
        {
            if ((pos - existingPos).sqrMagnitude < m_MinSpacingSqr)
            {
                m_Statistics.SpacingFailures++;
                return false;
            }
        }

        // 条件2: 检查周围安全区域是否都在Walkable的NavMesh上
        if (!IsAreaSafe(pos))
        {
            m_Statistics.SafetyFailures++;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查生成点周围的安全区域是否都在Walkable的NavMesh上
    /// 使用预计算的采样点偏移避免每次重新计算
    /// </summary>
    private bool IsAreaSafe(Vector3 centerPos)
    {
        foreach (var offset in m_CachedSampleOffsets)
        {
            Vector3 samplePos = centerPos + offset;
            if (!NavMesh.SamplePosition(samplePos, out _, 0f, NavMesh.AllAreas))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 生成单个敌人
    /// </summary>
    private void SpawnSingleEnemy(Vector3 spawnPos, int index)
    {
        try
        {
            // 实例化敌人
            EnemyEntity enemy = Instantiate(m_EnemyPrefab);
            enemy.name = $"Enemy_{index}";

            // 计算底部偏移并对齐平面
            float bottomOffset = EntityPositionHelper.CalculateBottomOffset(enemy.gameObject);
            spawnPos.y += bottomOffset;

            enemy.transform.position = spawnPos;
            m_SpawnedEnemies.Add(enemy);

            DebugEx.Success("EnemySpawnDebugger",
                $"✓ 敌人 #{index} 实例化成功" +
                $"\n  ├─ 位置: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})" +
                $"\n  └─ 底部偏移: {bottomOffset:F2}");
        }
        catch (System.Exception ex)
        {
            m_Statistics.InstantiateFailures++;
            DebugEx.Error("EnemySpawnDebugger",
                $"❌ 敌人 #{index} 实例化失败" +
                $"\n  ├─ 原因: {ex.GetType().Name}" +
                $"\n  └─ 详情: {ex.Message}");
        }
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (!m_ShowGizmos)
            return;

        // 绘制已生成敌人的底部位置
        Gizmos.color = Color.red;
        foreach (var enemy in m_SpawnedEnemies)
        {
            if (enemy != null)
            {
                Vector3 bottomPos = EntityPositionHelper.GetBottomPosition(enemy.gameObject);
                Gizmos.DrawWireSphere(bottomPos, m_GizmosRadius);

                // 绘制间距范围
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // 半透明橙色
                DrawGizmosCircle(bottomPos, m_MinSpacing, 16);
                Gizmos.color = Color.red;
            }
        }
    }

    /// <summary>
    /// 绘制水平圆形（用于显示敌人间距范围）
    /// </summary>
    private void DrawGizmosCircle(Vector3 center, float radius, int segments)
    {
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            points[i] = center + new Vector3(x, 0, z);
        }

        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }

    #endregion
}

#if UNITY_EDITOR

/// <summary>
/// 敌人生成调试工具的编辑器扩展
/// 在 Inspector 中显示自定义的生成/清除按钮
/// </summary>
[CustomEditor(typeof(EnemySpawnDebugger))]
public class EnemySpawnDebuggerEditor : Editor
{
    private EnemySpawnDebugger m_Debugger;

    private void OnEnable()
    {
        m_Debugger = target as EnemySpawnDebugger;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (m_Debugger != null)
        {
            m_Debugger.OnInspectorGUI();
        }
    }
}

#endif
