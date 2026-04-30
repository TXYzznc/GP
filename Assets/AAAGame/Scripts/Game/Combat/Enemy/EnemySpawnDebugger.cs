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
    private List<EnemyEntity> m_SpawnedEnemies = new List<EnemyEntity>();

    #endregion

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
        m_MaxRetries = EditorGUILayout.IntSlider("重试次数", m_MaxRetries, 10, 2000);

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

        DebugEx.Log("EnemySpawnDebugger", $"开始在NavMesh上生成 {m_EnemyCount} 个敌人");

        // 生成位置列表（在整个NavMesh上随机）
        List<Vector3> positions = GenerateSpawnPositions();

        for (int i = 0; i < positions.Count; i++)
        {
            SpawnSingleEnemy(positions[i], i);
        }

        DebugEx.Success("EnemySpawnDebugger", $"敌人生成完成，成功生成={m_SpawnedEnemies.Count}/{m_EnemyCount}");
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
        List<Vector3> positions = new List<Vector3>();
        int attempts = 0;
        int maxGlobalAttempts = m_EnemyCount * m_MaxRetries;

        while (positions.Count < m_EnemyCount && attempts < maxGlobalAttempts)
        {
            // 在整个NavMesh上随机找一个点
            if (RandomNavMeshPoint(out Vector3 randomPos))
            {
                // 检查是否与已生成的位置冲突
                if (IsValidSpawnPosition(randomPos, positions))
                {
                    positions.Add(randomPos);
                    DebugEx.Log("EnemySpawnDebugger",
                        $"找到有效位置 #{positions.Count}: ({randomPos.x:F2}, {randomPos.y:F2}, {randomPos.z:F2})");
                }
            }
            else
            {
                DebugEx.Warning("EnemySpawnDebugger", "无法在NavMesh上找到随机点");
                break;
            }

            attempts++;
        }

        if (positions.Count < m_EnemyCount)
        {
            DebugEx.Warning("EnemySpawnDebugger",
                $"只找到 {positions.Count}/{m_EnemyCount} 个有效位置 (最大重试 {maxGlobalAttempts} 次)。" +
                $"建议：增加重试次数、减少敌人数量或增加敌人间距的容差");
        }

        return positions;
    }

    /// <summary>
    /// 在NavMesh上随机找一个点
    /// </summary>
    private bool RandomNavMeshPoint(out Vector3 result)
    {
        result = Vector3.zero;

        // 获取所有NavMesh面
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.indices.Length == 0)
        {
            return false;
        }

        // 随机选择一个三角形
        int triangleIndex = Random.Range(0, triangulation.indices.Length / 3);
        int vertIndex = triangleIndex * 3;

        Vector3 v0 = triangulation.vertices[triangulation.indices[vertIndex]];
        Vector3 v1 = triangulation.vertices[triangulation.indices[vertIndex + 1]];
        Vector3 v2 = triangulation.vertices[triangulation.indices[vertIndex + 2]];

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
        // 条件1: 检查与已有位置的距离
        foreach (var existingPos in existingPositions)
        {
            if (Vector3.Distance(pos, existingPos) < m_MinSpacing)
            {
                return false;
            }
        }

        // 条件2: 检查周围安全区域是否都在Walkable的NavMesh上
        if (!IsAreaSafe(pos))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查生成点周围的安全区域是否都在Walkable的NavMesh上
    /// 使用网格采样确保整个区域都是可行走的
    /// </summary>
    private bool IsAreaSafe(Vector3 centerPos)
    {
        // 网格采样：在正方形区域内按网格采样
        float step = m_SafetyRadius * 2f / (m_GridSampleDensity + 1);

        for (int x = -m_GridSampleDensity; x <= m_GridSampleDensity; x++)
        {
            for (int z = -m_GridSampleDensity; z <= m_GridSampleDensity; z++)
            {
                Vector3 samplePos = centerPos + new Vector3(x * step, 0, z * step);

                // 严格检查：搜索距离为0，点必须在NavMesh上
                if (!NavMesh.SamplePosition(samplePos, out _, 0f, NavMesh.AllAreas))
                {
                    return false;
                }
            }
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
                $"敌人 #{index} 生成成功 - 位置: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2}), 底部偏移: {bottomOffset:F2}");
        }
        catch (System.Exception ex)
        {
            DebugEx.Error("EnemySpawnDebugger",
                $"敌人 #{index} 生成失败: {ex.Message}");
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
