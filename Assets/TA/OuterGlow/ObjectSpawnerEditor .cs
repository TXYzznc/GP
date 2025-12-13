#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 批量对象生成器 - 用于快速生成测试对象
/// </summary>
public class ObjectSpawnerEditor : EditorWindow
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

    private List<SpawnConfig> _spawnConfigs = new List<SpawnConfig>();
    private Vector2 _scrollPos;
    
    // 生成参数
    private Transform _spawnCenter;
    private Vector3 _customCenter = Vector3.zero;
    private bool _useCustomCenter = false;
    
    private enum SpawnShapeType
    {
        Sphere,
        Box,
        Circle,
        Grid
    }
    
    private SpawnShapeType _spawnShape = SpawnShapeType.Sphere;
    
    // 范围参数
    private float _sphereRadius = 50f;
    private Vector3 _boxSize = new Vector3(50f, 20f, 50f);
    private float _circleRadius = 50f;
    private Vector3 _gridSize = new Vector3(10, 1, 10);
    private float _gridSpacing = 5f;
    
    // 随机参数
    private bool _randomRotation = true;
    private bool _randomScale = false;
    private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);
    private bool _placeOnGround = true;
    private LayerMask _groundLayer = 1; // Default layer
    
    // 父级对象
    private Transform _parentTransform;
    private bool _createParentGroup = true;
    private string _parentGroupName = "Generated Objects";
    
    // 生成记录
    private List<GameObject> _spawnedObjects = new List<GameObject>();
    
    // 样式
    private GUIStyle _headerStyle;
    private GUIStyle _boxStyle;
    private Color _enabledColor = new Color(0.7f, 1f, 0.7f, 0.3f);
    private Color _disabledColor = new Color(1f, 0.7f, 0.7f, 0.3f);
    #endregion

    #region 菜单入口
    [MenuItem("Tools/批量对象生成器")]
    public static void ShowWindow()
    {
        ObjectSpawnerEditor window = GetWindow<ObjectSpawnerEditor>("对象生成器");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    #endregion

    #region GUI绘制
    void OnEnable()
    {
        // 添加默认配置
        if (_spawnConfigs.Count == 0)
        {
            _spawnConfigs.Add(new SpawnConfig());
        }
    }

    void OnGUI()
    {
        InitStyles();
        
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        DrawHeader();
        DrawSpawnCenter();
        DrawSpawnShape();
        DrawSpawnOptions();
        DrawPrefabList();
        DrawActionButtons();
        DrawStatistics();
        
        EditorGUILayout.EndScrollView();
    }

    private void InitStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 批量对象生成器", _headerStyle);
        EditorGUILayout.HelpBox("拖入预制体或场景对象，设置数量和范围，一键生成测试场景", MessageType.Info);
        EditorGUILayout.Space(5);
    }

    private void DrawSpawnCenter()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        EditorGUILayout.LabelField("📍 生成中心点", EditorStyles.boldLabel);
        
        _useCustomCenter = EditorGUILayout.Toggle("使用自定义中心", _useCustomCenter);
        
        if (_useCustomCenter)
        {
            _customCenter = EditorGUILayout.Vector3Field("中心位置", _customCenter);
        }
        else
        {
            _spawnCenter = (Transform)EditorGUILayout.ObjectField(
                "中心对象", 
                _spawnCenter, 
                typeof(Transform), 
                true
            );
            
            if (_spawnCenter == null && Camera.main != null)
            {
                EditorGUILayout.HelpBox("未设置中心对象，将使用主摄像机位置", MessageType.Warning);
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawSpawnShape()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        EditorGUILayout.LabelField("🔷 生成形状", EditorStyles.boldLabel);
        
        _spawnShape = (SpawnShapeType)EditorGUILayout.EnumPopup("形状类型", _spawnShape);
        
        EditorGUI.indentLevel++;
        
        switch (_spawnShape)
        {
            case SpawnShapeType.Sphere:
                _sphereRadius = EditorGUILayout.Slider("球体半径", _sphereRadius, 1f, 500f);
                break;
                
            case SpawnShapeType.Box:
                _boxSize = EditorGUILayout.Vector3Field("盒体大小", _boxSize);
                break;
                
            case SpawnShapeType.Circle:
                _circleRadius = EditorGUILayout.Slider("圆形半径", _circleRadius, 1f, 500f);
                break;
                
            case SpawnShapeType.Grid:
                _gridSize = EditorGUILayout.Vector3IntField("网格大小 (X×Y×Z)", Vector3Int.RoundToInt(_gridSize));
                _gridSpacing = EditorGUILayout.Slider("网格间距", _gridSpacing, 1f, 20f);
                EditorGUILayout.HelpBox($"将生成 {(int)(_gridSize.x * _gridSize.y * _gridSize.z)} 个对象", MessageType.Info);
                break;
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawSpawnOptions()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        EditorGUILayout.LabelField("⚙️ 生成选项", EditorStyles.boldLabel);
        
        _randomRotation = EditorGUILayout.Toggle("随机旋转", _randomRotation);
        
        _randomScale = EditorGUILayout.Toggle("随机缩放", _randomScale);
        if (_randomScale)
        {
            EditorGUI.indentLevel++;
            _scaleRange = EditorGUILayout.Vector2Field("缩放范围 (Min/Max)", _scaleRange);
            EditorGUI.indentLevel--;
        }
        
        _placeOnGround = EditorGUILayout.Toggle("放置在地面", _placeOnGround);
        if (_placeOnGround)
        {
            EditorGUI.indentLevel++;
            _groundLayer = EditorGUILayout.LayerField("地面Layer", _groundLayer);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(5);
        
        _createParentGroup = EditorGUILayout.Toggle("创建父级分组", _createParentGroup);
        if (_createParentGroup)
        {
            EditorGUI.indentLevel++;
            _parentGroupName = EditorGUILayout.TextField("分组名称", _parentGroupName);
            EditorGUI.indentLevel--;
        }
        else
        {
            _parentTransform = (Transform)EditorGUILayout.ObjectField(
                "指定父级对象", 
                _parentTransform, 
                typeof(Transform), 
                true
            );
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawPrefabList()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📦 预制体列表", EditorStyles.boldLabel);
        
        if (GUILayout.Button("+", GUILayout.Width(30)))
        {
            _spawnConfigs.Add(new SpawnConfig());
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        for (int i = 0; i < _spawnConfigs.Count; i++)
        {
            DrawSpawnConfigItem(i);
        }
        
        // 拖放区域
        DrawDropArea();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawSpawnConfigItem(int index)
    {
        SpawnConfig config = _spawnConfigs[index];
        
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = config.Enabled ? _enabledColor : _disabledColor;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.BeginHorizontal();
        
        // 启用开关
        config.Enabled = EditorGUILayout.Toggle(config.Enabled, GUILayout.Width(20));
        
        // 预制体字段
        EditorGUI.BeginDisabledGroup(!config.Enabled);
        config.Prefab = (GameObject)EditorGUILayout.ObjectField(
            config.Prefab, 
            typeof(GameObject), 
            true
        );
        EditorGUI.EndDisabledGroup();
        
        // 删除按钮
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            _spawnConfigs.RemoveAt(index);
            return;
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (config.Enabled && config.Prefab != null)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            
            // 数量
            config.Count = EditorGUILayout.IntSlider("数量", config.Count, 1, 1000);
            
            // Layer设置
            int layerIndex = LayerMask.NameToLayer(config.LayerName);
            if (layerIndex < 0) layerIndex = 0;
            
            layerIndex = EditorGUILayout.LayerField(layerIndex, GUILayout.Width(100));
            config.LayerName = LayerMask.LayerToName(layerIndex);
            
            EditorGUILayout.EndHorizontal();
            
            // 显示预制体信息
            string info = $"类型: {GetObjectType(config.Prefab)} | Layer: {config.LayerName}";
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawDropArea()
    {
        EditorGUILayout.Space(5);
        
        Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖放预制体到此处", EditorStyles.helpBox);
        
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
                        SpawnConfig newConfig = new SpawnConfig
                        {
                            Prefab = go,
                            Count = 10,
                            Enabled = true,
                            LayerName = LayerMask.LayerToName(go.layer)
                        };
                        _spawnConfigs.Add(newConfig);
                    }
                }
                
                evt.Use();
            }
        }
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        
        int totalCount = GetTotalSpawnCount();
        
        EditorGUILayout.LabelField($"总计将生成: {totalCount} 个对象", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 生成按钮
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🚀 开始生成", GUILayout.Height(40)))
        {
            SpawnObjects();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        // 清理按钮
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("🧹 清理生成的对象"))
        {
            CleanupSpawnedObjects();
        }
        
        // 清空配置
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ 清空配置"))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有配置吗？", "确定", "取消"))
            {
                _spawnConfigs.Clear();
                _spawnConfigs.Add(new SpawnConfig());
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawStatistics()
    {
        if (_spawnedObjects.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("📊 统计信息", EditorStyles.boldLabel);
            
            // 清理空引用
            _spawnedObjects.RemoveAll(obj => obj == null);
            
            EditorGUILayout.LabelField($"已生成对象数: {_spawnedObjects.Count}");
            
            if (_spawnedObjects.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("选中所有生成的对象"))
                {
                    Selection.objects = _spawnedObjects.ToArray();
                }
                
                if (GUILayout.Button("定位到第一个"))
                {
                    if (_spawnedObjects[0] != null)
                    {
                        Selection.activeGameObject = _spawnedObjects[0];
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    #endregion

    #region 生成逻辑
    private void SpawnObjects()
    {
        if (_spawnConfigs.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有配置任何预制体！", "确定");
            return;
        }

        int validCount = 0;
        foreach (var config in _spawnConfigs)
        {
            if (config.Enabled && config.Prefab != null)
                validCount++;
        }

        if (validCount == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有启用的有效预制体配置！", "确定");
            return;
        }

        // 获取生成中心
        Vector3 center = GetSpawnCenter();

        // 创建父级对象
        Transform parent = GetOrCreateParent();

        // 清空之前的记录
        _spawnedObjects.Clear();

        // 记录Undo
        if (parent != null)
        {
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "Spawn Objects");
        }

        int totalSpawned = 0;

        // 对于网格模式，使用特殊逻辑
        if (_spawnShape == SpawnShapeType.Grid)
        {
            totalSpawned = SpawnGrid(center, parent);
        }
        else
        {
            // 其他模式按配置逐个生成
            foreach (var config in _spawnConfigs)
            {
                if (!config.Enabled || config.Prefab == null)
                    continue;

                for (int i = 0; i < config.Count; i++)
                {
                    Vector3 position = GetRandomPosition(center);
                    Quaternion rotation = _randomRotation ? Random.rotation : config.Prefab.transform.rotation;

                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab);
                    if (spawned == null)
                    {
                        spawned = Instantiate(config.Prefab);
                    }

                    spawned.transform.position = position;
                    spawned.transform.rotation = rotation;

                    if (parent != null)
                    {
                        spawned.transform.SetParent(parent);
                    }

                    // 设置Layer
                    int layerIndex = LayerMask.NameToLayer(config.LayerName);
                    if (layerIndex >= 0)
                    {
                        spawned.layer = layerIndex;
                    }

                    // 随机缩放
                    if (_randomScale)
                    {
                        float scale = Random.Range(_scaleRange.x, _scaleRange.y);
                        spawned.transform.localScale *= scale;
                    }

                    // 放置在地面
                    if (_placeOnGround)
                    {
                        PlaceOnGround(spawned.transform);
                    }

                    _spawnedObjects.Add(spawned);
                    Undo.RegisterCreatedObjectUndo(spawned, "Spawn Object");
                    
                    totalSpawned++;
                }
            }
        }

        EditorUtility.DisplayDialog("完成", $"成功生成 {totalSpawned} 个对象！", "确定");
        
        // 刷新场景视图
        SceneView.RepaintAll();
    }

    private int SpawnGrid(Vector3 center, Transform parent)
    {
        int totalSpawned = 0;
        
        // 只使用第一个启用的配置
        SpawnConfig config = null;
        foreach (var cfg in _spawnConfigs)
        {
            if (cfg.Enabled && cfg.Prefab != null)
            {
                config = cfg;
                break;
            }
        }

        if (config == null)
            return 0;

        Vector3 startPos = center - new Vector3(
            (_gridSize.x - 1) * _gridSpacing * 0.5f,
            (_gridSize.y - 1) * _gridSpacing * 0.5f,
            (_gridSize.z - 1) * _gridSpacing * 0.5f
        );

        for (int x = 0; x < (int)_gridSize.x; x++)
        {
            for (int y = 0; y < (int)_gridSize.y; y++)
            {
                for (int z = 0; z < (int)_gridSize.z; z++)
                {
                    Vector3 position = startPos + new Vector3(
                        x * _gridSpacing,
                        y * _gridSpacing,
                        z * _gridSpacing
                    );

                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab);
                    if (spawned == null)
                    {
                        spawned = Instantiate(config.Prefab);
                    }

                    spawned.transform.position = position;
                    spawned.transform.rotation = _randomRotation ? Random.rotation : config.Prefab.transform.rotation;

                    if (parent != null)
                    {
                        spawned.transform.SetParent(parent);
                    }

                    int layerIndex = LayerMask.NameToLayer(config.LayerName);
                    if (layerIndex >= 0)
                    {
                        spawned.layer = layerIndex;
                    }

                    if (_randomScale)
                    {
                        float scale = Random.Range(_scaleRange.x, _scaleRange.y);
                        spawned.transform.localScale *= scale;
                    }

                    if (_placeOnGround)
                    {
                        PlaceOnGround(spawned.transform);
                    }

                    _spawnedObjects.Add(spawned);
                    Undo.RegisterCreatedObjectUndo(spawned, "Spawn Grid Object");
                    
                    totalSpawned++;
                }
            }
        }

        return totalSpawned;
    }

    private Vector3 GetRandomPosition(Vector3 center)
    {
        switch (_spawnShape)
        {
            case SpawnShapeType.Sphere:
                return center + Random.insideUnitSphere * _sphereRadius;

            case SpawnShapeType.Box:
                return center + new Vector3(
                    Random.Range(-_boxSize.x * 0.5f, _boxSize.x * 0.5f),
                    Random.Range(-_boxSize.y * 0.5f, _boxSize.y * 0.5f),
                    Random.Range(-_boxSize.z * 0.5f, _boxSize.z * 0.5f)
                );

            case SpawnShapeType.Circle:
                Vector2 randomCircle = Random.insideUnitCircle * _circleRadius;
                return center + new Vector3(randomCircle.x, 0, randomCircle.y);

            default:
                return center;
        }
    }

    private void PlaceOnGround(Transform trans)
    {
        RaycastHit hit;
        if (Physics.Raycast(trans.position + Vector3.up * 100f, Vector3.down, out hit, 200f, 1 << _groundLayer))
        {
            trans.position = hit.point;
        }
    }

    private Vector3 GetSpawnCenter()
    {
        if (_useCustomCenter)
        {
            return _customCenter;
        }

        if (_spawnCenter != null)
        {
            return _spawnCenter.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return Vector3.zero;
    }

    private Transform GetOrCreateParent()
    {
        if (!_createParentGroup)
        {
            return _parentTransform;
        }

        GameObject parentObj = new GameObject(_parentGroupName);
        return parentObj.transform;
    }

    private void CleanupSpawnedObjects()
    {
        if (_spawnedObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有需要清理的对象", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认", $"确定要删除 {_spawnedObjects.Count} 个生成的对象吗？", "确定", "取消"))
        {
            return;
        }

        int removed = 0;
        foreach (var obj in _spawnedObjects)
        {
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
                removed++;
            }
        }

        _spawnedObjects.Clear();
        
        EditorUtility.DisplayDialog("完成", $"已删除 {removed} 个对象", "确定");
        
        SceneView.RepaintAll();
    }

    private int GetTotalSpawnCount()
    {
        if (_spawnShape == SpawnShapeType.Grid)
        {
            return (int)(_gridSize.x * _gridSize.y * _gridSize.z);
        }

        int total = 0;
        foreach (var config in _spawnConfigs)
        {
            if (config.Enabled && config.Prefab != null)
            {
                total += config.Count;
            }
        }
        return total;
    }

    private string GetObjectType(GameObject obj)
    {
        if (PrefabUtility.IsPartOfPrefabAsset(obj))
        {
            return "预制体";
        }
        else if (PrefabUtility.IsPartOfPrefabInstance(obj))
        {
            return "预制体实例";
        }
        else
        {
            return "场景对象";
        }
    }
    #endregion

    #region Scene视图可视化
    void OnSceneGUI(SceneView sceneView)
    {
        if (!this)
            return;

        Vector3 center = GetSpawnCenter();
        
        Handles.color = Color.yellow;
        
        switch (_spawnShape)
        {
            case SpawnShapeType.Sphere:
                Handles.DrawWireDisc(center, Vector3.up, _sphereRadius);
                Handles.DrawWireDisc(center, Vector3.right, _sphereRadius);
                Handles.DrawWireDisc(center, Vector3.forward, _sphereRadius);
                break;

            case SpawnShapeType.Box:
                Handles.DrawWireCube(center, _boxSize);
                break;

            case SpawnShapeType.Circle:
                Handles.DrawWireDisc(center, Vector3.up, _circleRadius);
                break;

            case SpawnShapeType.Grid:
                Vector3 gridStart = center - new Vector3(
                    (_gridSize.x - 1) * _gridSpacing * 0.5f,
                    0,
                    (_gridSize.z - 1) * _gridSpacing * 0.5f
                );
                
                // 绘制网格
                for (int x = 0; x <= (int)_gridSize.x; x++)
                {
                    Vector3 start = gridStart + new Vector3(x * _gridSpacing, 0, 0);
                    Vector3 end = start + new Vector3(0, 0, (_gridSize.z - 1) * _gridSpacing);
                    Handles.DrawLine(start, end);
                }
                
                for (int z = 0; z <= (int)_gridSize.z; z++)
                {
                    Vector3 start = gridStart + new Vector3(0, 0, z * _gridSpacing);
                    Vector3 end = start + new Vector3((_gridSize.x - 1) * _gridSpacing, 0, 0);
                    Handles.DrawLine(start, end);
                }
                break;
        }
        
        // 绘制中心点
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, center, Quaternion.identity, 1f, EventType.Repaint);
    }

    void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    #endregion
}
#endif