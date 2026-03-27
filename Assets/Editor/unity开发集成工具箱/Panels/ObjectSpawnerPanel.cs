#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 批量对象生成器面板 - 可集成到工具箱，也可独立使用
/// </summary>
[ToolHubItem("场景工具/批量对象生成器", "快速批量生成测试对象到场景中", 30)]
public class ObjectSpawnerPanel : IToolHubPanel
{
    #region 配置数据
    [System.Serializable]
    private class SpawnConfig
    {
        public GameObject Prefab;
        public int Count = 10;
        public bool Enabled = true;
        public string LayerName = "Default";
    }

    private enum SpawnShapeType { Sphere, Box, Circle, Grid }

    private List<SpawnConfig> spawnConfigs = new List<SpawnConfig>();
    private Vector2 scrollPos;
    
    // 生成参数
    private Transform spawnCenter;
    private Vector3 customCenter = Vector3.zero;
    private bool useCustomCenter = false;
    private SpawnShapeType spawnShape = SpawnShapeType.Sphere;
    
    // 范围参数
    private float sphereRadius = 50f;
    private Vector3 boxSize = new Vector3(50f, 20f, 50f);
    private float circleRadius = 50f;
    private Vector3 gridSize = new Vector3(10, 1, 10);
    private float gridSpacing = 5f;
    
    // 随机参数
    private bool randomRotation = true;
    private bool randomScale = false;
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    private bool placeOnGround = true;
    private int groundLayer = 0;
    
    // 父级对象
    private Transform parentTransform;
    private bool createParentGroup = true;
    private string parentGroupName = "Generated Objects";
    
    // 生成记录
    private List<GameObject> spawnedObjects = new List<GameObject>();
    
    // 折叠状态
    private bool showCenter = true;
    private bool showShape = true;
    private bool showOptions = true;
    private bool showPrefabs = true;
    #endregion

    public void OnEnable()
    {
        if (spawnConfigs.Count == 0)
            spawnConfigs.Add(new SpawnConfig());
        
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public string GetHelpText() => "批量生成测试对象，支持多种分布形状";

    public void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawSpawnCenter();
        DrawSpawnShape();
        DrawSpawnOptions();
        DrawPrefabList();
        DrawActionButtons();
        DrawStatistics();
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawSpawnCenter()
    {
        showCenter = EditorGUILayout.BeginFoldoutHeaderGroup(showCenter, "📍 生成中心点");
        if (showCenter)
        {
            EditorGUILayout.BeginVertical("box");
            useCustomCenter = EditorGUILayout.Toggle("使用自定义中心", useCustomCenter);
            
            if (useCustomCenter)
                customCenter = EditorGUILayout.Vector3Field("中心位置", customCenter);
            else
            {
                spawnCenter = (Transform)EditorGUILayout.ObjectField("中心对象", spawnCenter, typeof(Transform), true);
                if (spawnCenter == null)
                    EditorGUILayout.HelpBox("未设置中心对象，将使用主摄像机位置", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(3);
    }

    private void DrawSpawnShape()
    {
        showShape = EditorGUILayout.BeginFoldoutHeaderGroup(showShape, "🔷 生成形状");
        if (showShape)
        {
            EditorGUILayout.BeginVertical("box");
            spawnShape = (SpawnShapeType)EditorGUILayout.EnumPopup("形状类型", spawnShape);
            
            EditorGUI.indentLevel++;
            switch (spawnShape)
            {
                case SpawnShapeType.Sphere:
                    sphereRadius = EditorGUILayout.Slider("球体半径", sphereRadius, 1f, 500f);
                    break;
                case SpawnShapeType.Box:
                    boxSize = EditorGUILayout.Vector3Field("盒体大小", boxSize);
                    break;
                case SpawnShapeType.Circle:
                    circleRadius = EditorGUILayout.Slider("圆形半径", circleRadius, 1f, 500f);
                    break;
                case SpawnShapeType.Grid:
                    gridSize = EditorGUILayout.Vector3IntField("网格大小", Vector3Int.RoundToInt(gridSize));
                    gridSpacing = EditorGUILayout.Slider("网格间距", gridSpacing, 1f, 20f);
                    EditorGUILayout.HelpBox($"将生成 {(int)(gridSize.x * gridSize.y * gridSize.z)} 个对象", MessageType.Info);
                    break;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(3);
    }

    private void DrawSpawnOptions()
    {
        showOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showOptions, "⚙️ 生成选项");
        if (showOptions)
        {
            EditorGUILayout.BeginVertical("box");
            randomRotation = EditorGUILayout.Toggle("随机旋转", randomRotation);
            
            randomScale = EditorGUILayout.Toggle("随机缩放", randomScale);
            if (randomScale)
            {
                EditorGUI.indentLevel++;
                scaleRange = EditorGUILayout.Vector2Field("缩放范围", scaleRange);
                EditorGUI.indentLevel--;
            }
            
            placeOnGround = EditorGUILayout.Toggle("放置在地面", placeOnGround);
            if (placeOnGround)
            {
                EditorGUI.indentLevel++;
                groundLayer = EditorGUILayout.LayerField("地面Layer", groundLayer);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(3);
            createParentGroup = EditorGUILayout.Toggle("创建父级分组", createParentGroup);
            if (createParentGroup)
            {
                EditorGUI.indentLevel++;
                parentGroupName = EditorGUILayout.TextField("分组名称", parentGroupName);
                EditorGUI.indentLevel--;
            }
            else
            {
                parentTransform = (Transform)EditorGUILayout.ObjectField("指定父级", parentTransform, typeof(Transform), true);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(3);
    }

    private void DrawPrefabList()
    {
        showPrefabs = EditorGUILayout.BeginFoldoutHeaderGroup(showPrefabs, "📦 预制体列表");
        if (showPrefabs)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已添加: {spawnConfigs.Count}", EditorStyles.miniLabel);
            if (GUILayout.Button("+", GUILayout.Width(25)))
                spawnConfigs.Add(new SpawnConfig());
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            
            for (int i = 0; i < spawnConfigs.Count; i++)
                DrawSpawnConfigItem(i);
            
            DrawDropArea();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(3);
    }

    private void DrawSpawnConfigItem(int index)
    {
        SpawnConfig config = spawnConfigs[index];
        
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = config.Enabled ? new Color(0.7f, 1f, 0.7f, 0.3f) : new Color(1f, 0.7f, 0.7f, 0.3f);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.BeginHorizontal();
        config.Enabled = EditorGUILayout.Toggle(config.Enabled, GUILayout.Width(20));
        
        EditorGUI.BeginDisabledGroup(!config.Enabled);
        config.Prefab = (GameObject)EditorGUILayout.ObjectField(config.Prefab, typeof(GameObject), true);
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("×", GUILayout.Width(22)))
        {
            spawnConfigs.RemoveAt(index);
            return;
        }
        EditorGUILayout.EndHorizontal();
        
        if (config.Enabled && config.Prefab != null)
        {
            EditorGUILayout.BeginHorizontal();
            config.Count = EditorGUILayout.IntSlider("数量", config.Count, 1, 500);
            
            int layerIndex = LayerMask.NameToLayer(config.LayerName);
            if (layerIndex < 0) layerIndex = 0;
            layerIndex = EditorGUILayout.LayerField(layerIndex, GUILayout.Width(80));
            config.LayerName = LayerMask.LayerToName(layerIndex);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawDropArea()
    {
        EditorGUILayout.Space(3);
        Rect dropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "🎯 拖放预制体到此处", EditorStyles.helpBox);
        
        Event evt = Event.current;
        if (dropArea.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go)
                    {
                        spawnConfigs.Add(new SpawnConfig
                        {
                            Prefab = go,
                            Count = 10,
                            Enabled = true,
                            LayerName = LayerMask.LayerToName(go.layer)
                        });
                    }
                }
                evt.Use();
            }
        }
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical("box");
        
        int totalCount = GetTotalSpawnCount();
        EditorGUILayout.LabelField($"📊 总计将生成: {totalCount} 个对象", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🚀 开始生成", GUILayout.Height(35)))
            SpawnObjects();
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(3);
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("🧹 清理生成的对象"))
            CleanupSpawnedObjects();
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ 清空配置"))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有配置吗？", "确定", "取消"))
            {
                spawnConfigs.Clear();
                spawnConfigs.Add(new SpawnConfig());
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawStatistics()
    {
        if (spawnedObjects.Count == 0) return;
        
        spawnedObjects.RemoveAll(obj => obj == null);
        if (spawnedObjects.Count == 0) return;
        
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"✅ 已生成: {spawnedObjects.Count} 个对象", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选中全部"))
            Selection.objects = spawnedObjects.ToArray();
        if (GUILayout.Button("定位第一个") && spawnedObjects[0] != null)
        {
            Selection.activeGameObject = spawnedObjects[0];
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    #region 生成逻辑
    private void SpawnObjects()
    {
        int validCount = 0;
        foreach (var config in spawnConfigs)
            if (config.Enabled && config.Prefab != null) validCount++;

        if (validCount == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有启用的有效预制体配置！", "确定");
            return;
        }

        Vector3 center = GetSpawnCenter();
        Transform parent = GetOrCreateParent();
        spawnedObjects.Clear();

        if (parent != null)
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "Spawn Objects");

        int totalSpawned = spawnShape == SpawnShapeType.Grid ? SpawnGrid(center, parent) : SpawnRandom(center, parent);

        EditorUtility.DisplayDialog("完成", $"成功生成 {totalSpawned} 个对象！", "确定");
        SceneView.RepaintAll();
    }

    private int SpawnRandom(Vector3 center, Transform parent)
    {
        int totalSpawned = 0;
        foreach (var config in spawnConfigs)
        {
            if (!config.Enabled || config.Prefab == null) continue;

            for (int i = 0; i < config.Count; i++)
            {
                Vector3 position = GetRandomPosition(center);
                Quaternion rotation = randomRotation ? Random.rotation : config.Prefab.transform.rotation;

                GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab);
                spawned ??= Object.Instantiate(config.Prefab);

                spawned.transform.position = position;
                spawned.transform.rotation = rotation;
                if (parent != null) spawned.transform.SetParent(parent);

                int layerIndex = LayerMask.NameToLayer(config.LayerName);
                if (layerIndex >= 0) spawned.layer = layerIndex;

                if (randomScale)
                {
                    float scale = Random.Range(scaleRange.x, scaleRange.y);
                    spawned.transform.localScale *= scale;
                }

                if (placeOnGround) PlaceOnGround(spawned.transform);

                spawnedObjects.Add(spawned);
                Undo.RegisterCreatedObjectUndo(spawned, "Spawn Object");
                totalSpawned++;
            }
        }
        return totalSpawned;
    }

    private int SpawnGrid(Vector3 center, Transform parent)
    {
        SpawnConfig config = null;
        foreach (var cfg in spawnConfigs)
        {
            if (cfg.Enabled && cfg.Prefab != null) { config = cfg; break; }
        }
        if (config == null) return 0;

        int totalSpawned = 0;
        Vector3 startPos = center - new Vector3(
            (gridSize.x - 1) * gridSpacing * 0.5f,
            (gridSize.y - 1) * gridSpacing * 0.5f,
            (gridSize.z - 1) * gridSpacing * 0.5f
        );

        for (int x = 0; x < (int)gridSize.x; x++)
        for (int y = 0; y < (int)gridSize.y; y++)
        for (int z = 0; z < (int)gridSize.z; z++)
        {
            Vector3 position = startPos + new Vector3(x * gridSpacing, y * gridSpacing, z * gridSpacing);

            GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab);
            spawned ??= Object.Instantiate(config.Prefab);

            spawned.transform.position = position;
            spawned.transform.rotation = randomRotation ? Random.rotation : config.Prefab.transform.rotation;
            if (parent != null) spawned.transform.SetParent(parent);

            int layerIndex = LayerMask.NameToLayer(config.LayerName);
            if (layerIndex >= 0) spawned.layer = layerIndex;

            if (randomScale)
            {
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                spawned.transform.localScale *= scale;
            }

            if (placeOnGround) PlaceOnGround(spawned.transform);

            spawnedObjects.Add(spawned);
            Undo.RegisterCreatedObjectUndo(spawned, "Spawn Grid Object");
            totalSpawned++;
        }
        return totalSpawned;
    }

    private Vector3 GetRandomPosition(Vector3 center)
    {
        return spawnShape switch
        {
            SpawnShapeType.Sphere => center + Random.insideUnitSphere * sphereRadius,
            SpawnShapeType.Box => center + new Vector3(
                Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f),
                Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f),
                Random.Range(-boxSize.z * 0.5f, boxSize.z * 0.5f)),
            SpawnShapeType.Circle => center + new Vector3(Random.insideUnitCircle.x, 0, Random.insideUnitCircle.y) * circleRadius,
            _ => center
        };
    }

    private void PlaceOnGround(Transform trans)
    {
        if (Physics.Raycast(trans.position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, 1 << groundLayer))
            trans.position = hit.point;
    }

    private Vector3 GetSpawnCenter()
    {
        if (useCustomCenter) return customCenter;
        if (spawnCenter != null) return spawnCenter.position;
        if (Camera.main != null) return Camera.main.transform.position;
        return Vector3.zero;
    }

    private Transform GetOrCreateParent()
    {
        if (!createParentGroup) return parentTransform;
        return new GameObject(parentGroupName).transform;
    }

    private void CleanupSpawnedObjects()
    {
        if (spawnedObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有需要清理的对象", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认", $"确定要删除 {spawnedObjects.Count} 个生成的对象吗？", "确定", "取消"))
            return;

        int removed = 0;
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
                removed++;
            }
        }
        spawnedObjects.Clear();
        EditorUtility.DisplayDialog("完成", $"已删除 {removed} 个对象", "确定");
        SceneView.RepaintAll();
    }

    private int GetTotalSpawnCount()
    {
        if (spawnShape == SpawnShapeType.Grid)
            return (int)(gridSize.x * gridSize.y * gridSize.z);

        int total = 0;
        foreach (var config in spawnConfigs)
            if (config.Enabled && config.Prefab != null)
                total += config.Count;
        return total;
    }
    #endregion

    #region Scene视图可视化
    private void OnSceneGUI(SceneView sceneView)
    {
        Vector3 center = GetSpawnCenter();
        Handles.color = Color.yellow;
        
        switch (spawnShape)
        {
            case SpawnShapeType.Sphere:
                Handles.DrawWireDisc(center, Vector3.up, sphereRadius);
                Handles.DrawWireDisc(center, Vector3.right, sphereRadius);
                Handles.DrawWireDisc(center, Vector3.forward, sphereRadius);
                break;
            case SpawnShapeType.Box:
                Handles.DrawWireCube(center, boxSize);
                break;
            case SpawnShapeType.Circle:
                Handles.DrawWireDisc(center, Vector3.up, circleRadius);
                break;
            case SpawnShapeType.Grid:
                Vector3 gridStart = center - new Vector3((gridSize.x - 1) * gridSpacing * 0.5f, 0, (gridSize.z - 1) * gridSpacing * 0.5f);
                for (int x = 0; x <= (int)gridSize.x; x++)
                {
                    Vector3 start = gridStart + new Vector3(x * gridSpacing, 0, 0);
                    Handles.DrawLine(start, start + new Vector3(0, 0, (gridSize.z - 1) * gridSpacing));
                }
                for (int z = 0; z <= (int)gridSize.z; z++)
                {
                    Vector3 start = gridStart + new Vector3(0, 0, z * gridSpacing);
                    Handles.DrawLine(start, start + new Vector3((gridSize.x - 1) * gridSpacing, 0, 0));
                }
                break;
        }
        
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, center, Quaternion.identity, 1f, EventType.Repaint);
    }
    #endregion
}
#endif
