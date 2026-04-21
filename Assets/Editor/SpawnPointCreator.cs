#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class SpawnPointCreator
{
    [MenuItem("GameObject/SpawnPoint/Create Enemy Spawn", false, 10)]
    private static void CreateEnemySpawnPoint()
    {
        CreateSpawnPoint(SpawnPointType.Enemy, "EnemySpawnPoint");
    }

    [MenuItem("GameObject/SpawnPoint/Create TreasureBox Spawn", false, 10)]
    private static void CreateChestSpawnPoint()
    {
        CreateSpawnPoint(SpawnPointType.TreasureBox, "ChestSpawnPoint");
    }

    private static void CreateSpawnPoint(SpawnPointType type, string baseName)
    {
        // 获取当前选中对象的位置（如果有的话）
        Transform selectedTransform = Selection.activeTransform;
        Vector3 position = selectedTransform != null ? selectedTransform.position : Vector3.zero;

        // 创建新的 GameObject
        GameObject spawnPointGO = new GameObject(baseName);
        spawnPointGO.transform.position = position;

        // 添加 SpawnPoint 组件
        SpawnPoint spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();

        // 反射设置私有字段
        var typeField = typeof(SpawnPoint).GetField("m_Type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeField != null)
            typeField.SetValue(spawnPoint, type);

        var radiusField = typeof(SpawnPoint).GetField("m_Radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (radiusField != null)
            radiusField.SetValue(spawnPoint, 1f);

        var navSampleField = typeof(SpawnPoint).GetField("m_NavSampleRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (navSampleField != null)
            navSampleField.SetValue(spawnPoint, 5f);

        // 自动将其放在选中对象的子级（如果有的话）
        if (selectedTransform != null)
            spawnPointGO.transform.SetParent(selectedTransform);

        // 选中新创建的对象
        Selection.activeGameObject = spawnPointGO;
        EditorGUIUtility.PingObject(spawnPointGO);

        Debug.Log($"已创建 {type} 生成点：{baseName}");
    }
}

#endif
