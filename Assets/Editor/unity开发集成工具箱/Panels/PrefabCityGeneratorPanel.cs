using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FCG; // 引用 Fantastic City Generator 命名空间

/// <summary>
/// Fantastic City Generator 集成面板
/// </summary>
[ToolHubItem("场景工具/Fantastic City Generator", "快速生成城市街道、建筑和交通系统", 50)]
public class PrefabCityGeneratorPanel : IToolHubPanel
{
    #region 核心变量
    private CityGenerator cityGenerator;
    private const string PREFAB_PATH = "Assets/Tool_Plugins/Fantastic City Generator/Generate.prefab";
    private string basePath = "Assets/Tool_Plugins/Fantastic City Generator/Buildings/Prefabs/";

    // 生成选项
    private bool generateLightmapUVs = false;
    private bool withDowntownArea = true;
    private float downTownSize = 100;
    private bool withSatteliteCity = false;
    private bool borderFlat = false;

    // 交通选项
    private int trafficLightHand = 0;
    private string[] selStrings = { "右手驾驶", "左手驾驶" };
    private bool japanTrafficLight = false;

    // UI 状态
    private bool showAssetInspector = false;
    private bool showVisualPreview = true;
    private Vector2 mainScrollPos;
    private Vector2 assetScrollPos;
    private Vector2 folderSettingsScrollPos;

    // 折叠状态
    private bool f_BB, f_BC, f_BK, f_BR, f_DC, f_EB, f_EC, f_MB, f_SB, f_BBS, f_BCS;

    // 启用状态
    private bool use_BB = true, use_BC = true, use_BK = true, use_BR = true, use_DC = true,
                 use_EB = true, use_EC = true, use_MB = true, use_SB = true, use_BBS = true, use_BCS = true;

    // Update 循环控制
    private int enableUpdate = 0;
    #endregion

    public void OnEnable()
    {
        // 注册 Update 回调以处理 HideLadders 逻辑
        EditorApplication.update += OnEditorUpdate;
        LoadAssets();
    }

    public void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    public void OnDestroy()
    {
        // 清理逻辑
    }

    public string GetHelpText()
    {
        return "城市生成器功能说明：\n" +
               "1. 首先点击 [生成街道] 创建路网。\n" +
               "2. 街道生成后，点击 [生成建筑] 填充城市。\n" +
               "3. 最后点击 [添加交通系统] 生成车辆。\n" +
               "注意：请确保插件资源位于 'Assets/Tool_Plugins/Fantastic City Generator' 目录下。";
    }

    #region 逻辑更新 (原 Update)
    private void OnEditorUpdate()
    {
        if (enableUpdate == 0) return;
        enableUpdate++;
        if (enableUpdate <= 5) HideLadders();
        if (enableUpdate >= 5) enableUpdate = 0;
    }

    private void HideLadders()
    {
        RaycastHit hit;
        GameObject[] tempArray = GameObject.FindObjectsOfType(typeof(GameObject))
            .Select(g => g as GameObject).Where(g => g.name == "RayCast-HideLadder").ToArray();
        
        foreach (GameObject ray in tempArray)
        {
            if (Physics.Raycast(ray.transform.position, ray.transform.forward, out hit, 1.5f))
                if (ray.transform.childCount > 0) ray.transform.GetChild(0).gameObject.SetActive(false);
            else
                if (ray.transform.childCount > 0) ray.transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    #endregion

    public void OnGUI()
    {
        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

        DrawHeader();

        if (cityGenerator == null)
        {
            EditorGUILayout.HelpBox("未找到 CityGenerator 预制体。\n请确保文件路径为: " + PREFAB_PATH, MessageType.Error);
            if (GUILayout.Button("尝试重新加载")) LoadAssets(true);
        }
        else
        {
            DrawStreetGeneration();
            GUILayout.Space(10);
            DrawBuildingGeneration();
            GUILayout.Space(10);
            DrawAssetControl();
            GUILayout.Space(10);
            DrawTrafficSystem();
            GUILayout.Space(10);
            DrawMeshCombine();
        }

        EditorGUILayout.EndScrollView();
    }

    #region GUI 模块
    private void DrawHeader()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🏙️ 城市生成器控制面板", EditorStyles.boldLabel);
        GUILayout.Space(5);
    }

    private void DrawStreetGeneration()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("1. 街道生成 (Street Generation)", EditorStyles.miniLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("小型城市")) GenerateCity(1, borderFlat);
        if (GUILayout.Button("中型城市")) GenerateCity(2, borderFlat);
        if (GUILayout.Button("大型城市")) GenerateCity(3, borderFlat);
        if (GUILayout.Button("超大型城市")) GenerateCity(4, borderFlat);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        withSatteliteCity = EditorGUILayout.ToggleLeft("包含卫星城", withSatteliteCity, GUILayout.Width(120));
        if (!withSatteliteCity)
        {
            borderFlat = EditorGUILayout.ToggleLeft("边界平坦", borderFlat, GUILayout.Width(120));
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("清除街道")) cityGenerator.ClearCity();
        GUILayout.EndVertical();
    }

    private void DrawBuildingGeneration()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("2. 建筑生成 (Building Generation)", EditorStyles.miniLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("生成建筑"))
        {
            if (!GameObject.Find("Marcador")) 
            { 
                EditorUtility.DisplayDialog("提示", "请先生成街道！", "确定"); 
                return; 
            }
            LoadAssets(true);
            cityGenerator.GenerateAllBuildings(withDowntownArea, downTownSize);
            enableUpdate = 1; // 触发 HideLadders
        }
        if (GUILayout.Button("清除建筑"))
        {
            if (GameObject.Find("Marcador")) cityGenerator.DestroyBuildings();
        }
        GUILayout.EndHorizontal();

        withDowntownArea = EditorGUILayout.ToggleLeft("包含市中心区域", withDowntownArea);
        if (withDowntownArea)
        {
            downTownSize = EditorGUILayout.Slider("市中心范围", downTownSize, 50, 200);
        }
        GUILayout.EndVertical();
    }

    private void DrawAssetControl()
    {
        GUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        showAssetInspector = EditorGUILayout.Foldout(showAssetInspector, "资源配置与预览 (Asset Control)", true, EditorStyles.foldoutHeader);
        EditorGUILayout.EndHorizontal();

        if (showAssetInspector)
        {
            GUILayout.Space(5);
            DrawFolderConfigUI();
            
            EditorGUILayout.HelpBox("取消勾选分类可禁止生成该类建筑。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用更改并刷新", GUILayout.Width(160))) LoadAssets(true);
            showVisualPreview = EditorGUILayout.ToggleLeft("显示缩略图", showVisualPreview);
            EditorGUILayout.EndHorizontal();

            if (cityGenerator != null)
            {
                assetScrollPos = EditorGUILayout.BeginScrollView(assetScrollPos, GUILayout.MinHeight(300), GUILayout.MaxHeight(500));

                DrawAssetCategory("BB - 郊区街道建筑", ref f_BB, ref use_BB, cityGenerator.BB);
                DrawAssetCategory("BC - 市中心建筑", ref f_BC, ref use_BC, cityGenerator.BC);
                DrawAssetCategory("BK - 街区中心建筑", ref f_BK, ref use_BK, cityGenerator.BK);
                DrawAssetCategory("BR - 郊区住宅", ref f_BR, ref use_BR, cityGenerator.BR);
                DrawAssetCategory("DC - 双面拐角建筑", ref f_DC, ref use_DC, cityGenerator.DC);
                DrawAssetCategory("EB - 郊区拐角", ref f_EB, ref use_EB, cityGenerator.EB);
                DrawAssetCategory("EC - 市中心拐角", ref f_EC, ref use_EC, cityGenerator.EC);
                DrawAssetCategory("MB - 大型建筑(MB)", ref f_MB, ref use_MB, cityGenerator.MB);
                DrawAssetCategory("SB - 超大建筑块(SB)", ref f_SB, ref use_SB, cityGenerator.SB);
                DrawAssetCategory("BBS - 郊区斜坡建筑", ref f_BBS, ref use_BBS, cityGenerator.BBS);
                DrawAssetCategory("BCS - 市中心斜坡建筑", ref f_BCS, ref use_BCS, cityGenerator.BCS);

                EditorGUILayout.EndScrollView();
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawTrafficSystem()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("3. 交通系统 (Traffic System)", EditorStyles.miniLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("添加交通系统")) AddVehicles(trafficLightHand);
        if (GUILayout.Button("移除交通系统"))
        {
            TrafficSystem ts = Object.FindObjectOfType<TrafficSystem>();
            if (ts) Object.DestroyImmediate(ts.gameObject);
            GameObject cc = GameObject.Find("CarContainer");
            if (cc) Object.DestroyImmediate(cc);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("驾驶方向设置:");
        int rh = trafficLightHand;
        trafficLightHand = GUILayout.SelectionGrid(trafficLightHand, selStrings, 2);

        bool japanTL = japanTrafficLight;
        if (trafficLightHand != 0) // 左手驾驶
        {
            japanTrafficLight = EditorGUILayout.ToggleLeft("日本交通灯（蓝色）", japanTrafficLight);
        }

        if (rh != trafficLightHand || japanTL != japanTrafficLight)
        {
            if (GameObject.Find("CarContainer"))
                AddVehicles((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);
            else
                InverseCarDirection((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);
        }
        GUILayout.EndVertical();
    }

    private void DrawMeshCombine()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("4. 优化 (Optimization)", EditorStyles.miniLabel);
        if (GUILayout.Button("合并建筑网格 (Mesh Combine)")) DoMeshCombine();
        generateLightmapUVs = EditorGUILayout.Toggle("生成光照贴图UV", generateLightmapUVs);
        GUILayout.EndVertical();
    }
    #endregion

    #region 内部逻辑方法 (移植自 FCityGenerator.cs)

    private void LoadAssets(bool force = false)
    {
        cityGenerator = AssetDatabase.LoadAssetAtPath<CityGenerator>(PREFAB_PATH);

        if (cityGenerator == null) return;

        if (cityGenerator.folderSettings == null || cityGenerator.folderSettings.Count == 0)
            ScanAndSyncFolders();

        Dictionary<BuildingType, List<GameObject>> assetPool = new Dictionary<BuildingType, List<GameObject>>();
        foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            assetPool[type] = new List<GameObject>();

        foreach (var setting in cityGenerator.folderSettings)
        {
            if (!setting.isSelected || setting.category == BuildingType.忽略) continue;

            string folderPath = basePath + setting.folderName;
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*.prefab");
                foreach (var s in files)
                {
                    GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(s);
                    if (g != null) assetPool[setting.category].Add(g);
                }
            }
        }

        cityGenerator.BB = assetPool[BuildingType.郊区街道建筑].ToArray();
        cityGenerator.BC = assetPool[BuildingType.市中心建筑].ToArray();
        cityGenerator.BR = assetPool[BuildingType.郊区住宅].ToArray();
        cityGenerator.BK = assetPool[BuildingType.街区中心建筑].ToArray();
        cityGenerator.DC = assetPool[BuildingType.双面拐角建筑].ToArray();
        cityGenerator.EB = assetPool[BuildingType.郊区拐角].ToArray();
        cityGenerator.EC = assetPool[BuildingType.市中心拐角].ToArray();
        cityGenerator.MB = assetPool[BuildingType.大型建筑].ToArray();
        cityGenerator.SB = assetPool[BuildingType.超大建筑块].ToArray();
        cityGenerator.BBS = assetPool[BuildingType.郊区斜坡建筑].ToArray();
        cityGenerator.BCS = assetPool[BuildingType.市中心斜坡建筑].ToArray();

        // 开关控制逻辑
        if (!use_BB) cityGenerator.BB = new GameObject[0];
        if (!use_BC) cityGenerator.BC = new GameObject[0];
        if (!use_BR) cityGenerator.BR = new GameObject[0];
        if (!use_BK) cityGenerator.BK = new GameObject[0];
        if (!use_DC) cityGenerator.DC = new GameObject[0];
        if (!use_EB) cityGenerator.EB = new GameObject[0];
        if (!use_EC) cityGenerator.EC = new GameObject[0];
        if (!use_MB) cityGenerator.MB = new GameObject[0];
        if (!use_SB) cityGenerator.SB = new GameObject[0];
        if (!use_BBS) cityGenerator.BBS = new GameObject[0];
        if (!use_BCS) cityGenerator.BCS = new GameObject[0];
    }

    private void GenerateCity(int size, bool borderFlat)
    {
        LoadAssets();
        if (cityGenerator != null)
        {
            cityGenerator.GenerateCity(size, withSatteliteCity, borderFlat);
            
            TrafficSystem ts = Object.FindObjectOfType<TrafficSystem>();
            if (ts)
            {
                InverseCarDirection((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);
                ts.UpdateAllWayPoints();
            }
            GameObject carContainer = GameObject.Find("CarContainer");
            if (carContainer) Object.DestroyImmediate(carContainer);
        }
    }

    private void AddVehicles(int right_Hand = 0)
    {
        TrafficSystem trafficSystem = Object.FindObjectOfType<TrafficSystem>();
        if (!trafficSystem)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tool_Plugins/Fantastic City Generator/Traffic System/Traffic System.prefab");
            if (prefab)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                trafficSystem = instance.GetComponent<TrafficSystem>();
            }
        }
        
        if (!trafficSystem)
        {
            Debug.LogError("未找到 Traffic System.prefab");
            return;
        }
        
        trafficSystem.name = "Traffic System";
        GameObject carContainer = GameObject.Find("CarContainer");
        if (carContainer) Object.DestroyImmediate(carContainer);
        
        trafficSystem.LoadCars(right_Hand);
    }

    private void InverseCarDirection(int trafficHand)
    {
        TrafficSystem trafficSystem = Object.FindObjectOfType<TrafficSystem>();
        if (!trafficSystem)
        {
            // 尝试加载资源以获取脚本引用（虽然这里不能直接调用非静态方法，但保持原逻辑意图）
            // 在编辑器模式下，如果没有实例，我们无法直接调用 Direction 切换，除非我们实例化。
            // 这里遵循原逻辑：如果场景中没有，报错返回。
            return; 
        }
        
        if (trafficSystem)
        {
            trafficSystem.DeffineDirection(trafficHand);
            if (GameObject.Find("CarContainer"))
                AddVehicles((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);
        }
    }

    private void ScanAndSyncFolders()
    {
        if (cityGenerator == null || !Directory.Exists(basePath)) return;

        string[] directories = Directory.GetDirectories(basePath);
        List<string> currentFolderNames = new List<string>();

        foreach (var dir in directories)
            currentFolderNames.Add(new DirectoryInfo(dir).Name);

        if (cityGenerator.folderSettings == null) cityGenerator.folderSettings = new List<FolderSetting>();

        // 移除不存在的
        for (int i = cityGenerator.folderSettings.Count - 1; i >= 0; i--)
        {
            if (!currentFolderNames.Contains(cityGenerator.folderSettings[i].folderName))
                cityGenerator.folderSettings.RemoveAt(i);
        }

        // 添加新的
        foreach (string folderName in currentFolderNames)
        {
            if (!cityGenerator.folderSettings.Exists(x => x.folderName == folderName))
            {
                BuildingType autoType = BuildingType.忽略;
                foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
                {
                    if (type.ToString().StartsWith(folderName + "_") || type.ToString() == folderName)
                    {
                        autoType = type;
                        break;
                    }
                }

                cityGenerator.folderSettings.Add(new FolderSetting
                {
                    folderName = folderName,
                    category = autoType,
                    isSelected = true
                });
            }
        }
        EditorUtility.SetDirty(cityGenerator);
    }

    // --- UI 绘制辅助 ---

    private void DrawAssetCategory(string label, ref bool foldout, ref bool useToggle, GameObject[] assets)
    {
        int count = (assets != null) ? assets.Length : 0;
        string fullLabel = $"{label} [{count}]";

        EditorGUILayout.BeginHorizontal();
        bool newToggle = EditorGUILayout.Toggle(useToggle, GUILayout.Width(20));
        if (newToggle != useToggle)
        {
            useToggle = newToggle;
            LoadAssets(true);
        }

        if (!useToggle || count == 0) GUI.color = Color.gray;
        foldout = EditorGUILayout.Foldout(foldout, fullLabel, true);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        if (foldout)
        {
            EditorGUI.indentLevel++;
            if (!useToggle)
            {
                EditorGUILayout.HelpBox("该分类已禁用。", MessageType.Warning);
            }
            else if (count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                if (showVisualPreview)
                {
                    float width = EditorGUIUtility.currentViewWidth - 60; // 调整宽度适应
                    float itemSize = 80f;
                    float spacing = 5f;
                    int columns = Mathf.FloorToInt(width / (itemSize + spacing));
                    if (columns < 1) columns = 1;
                    int rows = Mathf.CeilToInt((float)count / columns);

                    for (int r = 0; r < rows; r++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int c = 0; c < columns; c++)
                        {
                            int index = r * columns + c;
                            if (index < count)
                            {
                                GameObject prefab = assets[index];
                                if (prefab != null) DrawSinglePreview(prefab, itemSize);
                            }
                            else GUILayout.Space(itemSize + spacing);
                        }
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(spacing);
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (assets[i] != null)
                            EditorGUILayout.ObjectField($"({i}) {assets[i].name}", assets[i], typeof(GameObject), false);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("列表为空，请检查配置。", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawSinglePreview(GameObject prefab, float size)
    {
        Texture2D preview = AssetPreview.GetAssetPreview(prefab);
        
        GUILayout.BeginVertical(GUILayout.Width(size), GUILayout.Height(size + 20));
        if (GUILayout.Button(preview, GUILayout.Width(size), GUILayout.Height(size)))
        {
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }
        string name = prefab.name;
        if (name.Length > 10) name = name.Substring(0, 8) + "..";
        GUILayout.Label(new GUIContent(name, prefab.name), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(size));
        GUILayout.EndVertical();
    }

    private void DrawFolderConfigUI()
    {
        if (GUILayout.Button("扫描文件夹 (刷新配置)")) ScanAndSyncFolders();

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("启用", GUILayout.Width(30));
        GUILayout.Label("文件夹", GUILayout.Width(150));
        GUILayout.Label("映射类型", GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        folderSettingsScrollPos = EditorGUILayout.BeginScrollView(folderSettingsScrollPos, GUILayout.Height(150));

        if (cityGenerator != null && cityGenerator.folderSettings != null)
        {
            for (int i = 0; i < cityGenerator.folderSettings.Count; i++)
            {
                var setting = cityGenerator.folderSettings[i];
                EditorGUILayout.BeginHorizontal();
                bool newSelect = EditorGUILayout.Toggle(setting.isSelected, GUILayout.Width(30));
                if (newSelect != setting.isSelected)
                {
                    setting.isSelected = newSelect;
                    EditorUtility.SetDirty(cityGenerator);
                }
                GUILayout.Label(setting.folderName, GUILayout.Width(150));
                BuildingType newType = (BuildingType)EditorGUILayout.EnumPopup(setting.category, GUILayout.Width(150));
                if (newType != setting.category)
                {
                    setting.category = newType;
                    EditorUtility.SetDirty(cityGenerator);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DoMeshCombine()
    {
        if (!GameObject.Find("Marcador")) return;
        if (!EditorUtility.DisplayDialog("合并网格", "合并建筑网格将移除 LOD 组件，操作不可逆（除非撤销）。\n\n是否继续？", "是", "否")) return;

        // 这里仅调用 CityGenerator 中类似的合并逻辑，
        // 由于原代码逻辑主要在 Window 类中实现，这里我们将其重构进来。
        // 注意：原 CombineMeshes 逻辑较长，如有需要在 CityGenerator.cs 或 MeshCombineUtility 中引用。
        // 为简化，这里直接调用原 FCityGenerator 中定义的逻辑的移植版：
        
        CombineLogic();
        EditorUtility.ClearProgressBar();
    }

    // 移植 Mesh Combine 逻辑
    private void CombineLogic()
    {
        GameObject[] my_Modules = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == "Marcador").ToArray();
        int tt = my_Modules.Length;
        for (int i = 0; i < tt; i++)
        {
            var module = my_Modules[i];
            GameObject newBlock = new GameObject("_block");
            newBlock.transform.SetPositionAndRotation(module.transform.position, module.transform.rotation);
            newBlock.transform.parent = module.transform.parent;

            foreach (Transform child in module.transform)
            {
                var filters = child.GetComponentsInChildren<MeshFilter>();
                // 移除 LOD
                foreach (var f in filters)
                    if (f.gameObject.name.Contains("_LOD")) Object.DestroyImmediate(f.gameObject);
                
                // 重新获取并分组
                filters = child.GetComponentsInChildren<MeshFilter>();
                foreach (var f in filters)
                {
                    if (f.gameObject.name.Contains("(Clone)")) f.gameObject.transform.parent = newBlock.transform;
                }
            }
            if (module) Object.DestroyImmediate(module);
        }

        GameObject[] blocks = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == "_block").ToArray();
        tt = blocks.Length;
        for (int i = 0; i < tt; i++)
        {
            EditorUtility.DisplayProgressBar("正在合并网格", $"处理中 {i}/{tt}", (float)i / tt);
            var module = blocks[i];
            GameObject newObj = new GameObject("合并后的建筑");
            newObj.transform.parent = module.transform.parent;
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            
            // 调用 FCG 的合并工具
            CombineMeshes(module, newObj);
        }
    }

    private void CombineMeshes(GameObject sourceObj, GameObject targetParent)
    {
        // 简单移植 FCityGenerator.CombineMeshes 的核心部分
        // 注意：这部分逻辑依赖 Mesh_CombineUtility.cs
        
        Component[] cloths = sourceObj.GetComponentsInChildren(typeof(Cloth));
        foreach (Cloth c in cloths) c.transform.parent = targetParent.transform;

        // 复制碰撞体
        foreach (var col in sourceObj.GetComponentsInChildren<BoxCollider>())
            CopyCollider(col, targetParent);
        foreach (var col in sourceObj.GetComponentsInChildren<MeshCollider>())
            CopyCollider(col, targetParent);

        // 调用合并
        FCCombine2(sourceObj, targetParent);
        
        if (sourceObj) Object.DestroyImmediate(sourceObj);
    }

    private void CopyCollider(Collider col, GameObject parent)
    {
        GameObject go = new GameObject(col.GetType().Name);
        go.transform.SetPositionAndRotation(col.transform.position, col.transform.rotation);
        go.transform.localScale = col.transform.lossyScale;  
        // 注意：lossyScale 近似，因为父级缩放可能影响
        go.transform.parent = parent.transform;
        
        UnityEditorInternal.ComponentUtility.CopyComponent(col);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
    }

    private void FCCombine2(GameObject objs, GameObject newParent)
    {
        // 核心合并逻辑，类似原脚本 Combine2
        MeshFilter[] filters = objs.GetComponentsInChildren<MeshFilter>();
        Matrix4x4 myTransform = objs.transform.worldToLocalMatrix;
        System.Collections.Hashtable materialToMesh = new System.Collections.Hashtable();

        foreach (MeshFilter filter in filters)
        {
            Renderer curRenderer = filter.GetComponent<Renderer>();
            if (curRenderer != null && curRenderer.enabled && filter.sharedMesh != null)
            {
                Mesh_CombineUtility.MeshInstance instance = new Mesh_CombineUtility.MeshInstance();
                instance.mesh = filter.sharedMesh;
                instance.transform = myTransform * filter.transform.localToWorldMatrix;
                
                Material[] materials = curRenderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    instance.subMeshIndex = System.Math.Min(m, instance.mesh.subMeshCount - 1);
                    System.Collections.ArrayList objects = (System.Collections.ArrayList)materialToMesh[materials[m]];
                    if (objects != null) objects.Add(instance);
                    else
                    {
                        objects = new System.Collections.ArrayList();
                        objects.Add(instance);
                        materialToMesh.Add(materials[m], objects);
                    }
                }
            }
        }

        foreach (System.Collections.DictionaryEntry de in materialToMesh)
        {
            System.Collections.ArrayList elements = (System.Collections.ArrayList)de.Value;
            Mesh_CombineUtility.MeshInstance[] instances = (Mesh_CombineUtility.MeshInstance[])elements.ToArray(typeof(Mesh_CombineUtility.MeshInstance));
            GameObject go = new GameObject("Mesh");
            go.transform.parent = newParent.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            
            go.AddComponent<MeshFilter>().sharedMesh = Mesh_CombineUtility.Combine(instances, false);
            go.AddComponent<MeshRenderer>().material = (Material)de.Key;
            
            if (generateLightmapUVs) Unwrapping.GenerateSecondaryUVSet(go.GetComponent<MeshFilter>().sharedMesh);
        }
    }

    #endregion
}