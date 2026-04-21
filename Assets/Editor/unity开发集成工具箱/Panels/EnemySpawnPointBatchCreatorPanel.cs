#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// 敌人生成点批量创建面板 - 在工具箱中快速创建多个 EnemySpawnPoint
/// </summary>
[ToolHubItem("场景工具/敌人生成点批量创建器", "智能批量生成 EnemySpawnPoint，支持 NavMesh 约束和密度控制", 25)]
public class EnemySpawnPointBatchCreatorPanel : IToolHubPanel
{
    private int m_Count = 5;
    private float m_MinDistance = 5f;
    private float m_Radius = 1f;
    private float m_NavSampleRadius = 2f;
    private int m_MaxAttempts = 100000;
    private float m_MinDistanceFromEdge = 1f; // 距离 NavMesh 边界的最小距离

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("生成配置", EditorStyles.boldLabel);
        m_Count = EditorGUILayout.IntSlider("数量", m_Count, 1, 100);
        m_MinDistance = EditorGUILayout.FloatField("最小间距", m_MinDistance);
        m_MinDistanceFromEdge = EditorGUILayout.FloatField("离边界最小距离", m_MinDistanceFromEdge);
        m_Radius = EditorGUILayout.FloatField("Radius（随机偏移）", m_Radius);
        m_NavSampleRadius = EditorGUILayout.FloatField("NavSampleRadius", m_NavSampleRadius);
        m_MaxAttempts = EditorGUILayout.IntField("最大尝试次数", m_MaxAttempts);

        EditorGUILayout.Space(15);

        if (GUILayout.Button("在全场景 NavMesh 中生成", GUILayout.Height(40)))
        {
            GenerateSpawnPoints();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "工具说明：\n" +
            "• 自动扫描场景中的所有 NavMesh 区域\n" +
            "• 智能避开 NavMesh 边界，生成在合理位置\n" +
            "• 遵守最小间距约束（密度控制）\n" +
            "• 生成的 SpawnPoint 会贴在 NavMesh 上\n\n" +
            "提示：最小间距越大，生成数量可能会少于设置值。",
            MessageType.Info);
    }

    public void OnDestroy()
    {
    }

    public string GetHelpText()
    {
        return "批量生成 EnemySpawnPoint，支持 NavMesh 约束和密度控制。" +
               "可快速为场景布置多个敌人生成点，适合探索场景搭建。";
    }

    private void GenerateSpawnPoints()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("错误", "请在编辑器中打开一个场景", "确定");
            return;
        }

        // 智能生成位置（全场景 NavMesh 扫描）
        var selectedPoints = GeneratePointsOnNavMesh();
        if (selectedPoints.Count == 0)
        {
            EditorUtility.DisplayDialog("失败", "无法在场景 NavMesh 中找到合适的位置", "确定");
            return;
        }

        // 创建 SpawnPoint GameObjects
        var parentGO = new GameObject("EnemySpawnPoints");
        Undo.RegisterCreatedObjectUndo(parentGO, "Batch Create SpawnPoints");

        foreach (var pos in selectedPoints)
        {
            CreateSpawnPoint(parentGO.transform, pos);
        }

        EditorUtility.DisplayDialog("成功", $"已生成 {selectedPoints.Count} 个 SpawnPoint", "确定");
        Debug.Log($"已批量生成 {selectedPoints.Count} 个 EnemySpawnPoint（设置数量: {m_Count}）");
    }

    private List<Vector3> GeneratePointsOnNavMesh()
    {
        var selectedPoints = new List<Vector3>();
        var attempts = 0;

        // 尝试在 NavMesh 上随机生成合适的位置
        while (selectedPoints.Count < m_Count && attempts < m_MaxAttempts)
        {
            // 在一个大范围内随机采样（假设场景在 ±1000 范围内）
            var randomPos = new Vector3(
                Random.Range(-1000f, 1000f),
                Random.Range(-100f, 500f),
                Random.Range(-1000f, 1000f)
            );

            // 采样 NavMesh
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 100f, NavMesh.AllAreas))
            {
                var candidatePos = hit.position;

                // 检查与已有点的距离
                bool tooClose = false;
                foreach (var existingPoint in selectedPoints)
                {
                    if (Vector3.Distance(candidatePos, existingPoint) < m_MinDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                // 检查是否远离 NavMesh 边界（避免生成在边上）
                if (!tooClose && IsAwayFromNavMeshEdge(candidatePos, m_MinDistanceFromEdge))
                {
                    selectedPoints.Add(candidatePos);
                }
            }

            attempts++;
        }

        return selectedPoints;
    }

    private bool IsAwayFromNavMeshEdge(Vector3 pos, float minDistance)
    {
        // 检查到最近 NavMesh 边界的距离
        if (NavMesh.FindClosestEdge(pos, out NavMeshHit hit, NavMesh.AllAreas))
        {
            return hit.distance >= minDistance;
        }

        return false;
    }


    private void CreateSpawnPoint(Transform parent, Vector3 position)
    {
        var go = new GameObject($"EnemySpawnPoint_{parent.childCount}");
        go.transform.SetParent(parent);
        go.transform.position = position;

        var spawnPoint = go.AddComponent<SpawnPoint>();

        // 使用反射设置私有字段
        var typeField = typeof(SpawnPoint).GetField("m_Type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeField != null)
            typeField.SetValue(spawnPoint, SpawnPointType.Enemy);

        var radiusField = typeof(SpawnPoint).GetField("m_Radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (radiusField != null)
            radiusField.SetValue(spawnPoint, m_Radius);

        var navSampleField = typeof(SpawnPoint).GetField("m_NavSampleRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (navSampleField != null)
            navSampleField.SetValue(spawnPoint, m_NavSampleRadius);

        Undo.RegisterCreatedObjectUndo(go, "Create SpawnPoint");
    }
}

#endif
