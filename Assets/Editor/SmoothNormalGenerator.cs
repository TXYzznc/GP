using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SmoothNormalGeneratorWindow : EditorWindow
{
    private List<Object> targetAssets = new List<Object>();
    private List<GameObject> sceneObjects = new List<GameObject>();  // ✅ 新增：场景对象列表
    private Vector2 scrollPosition;
    private bool autoApplyToScene = true;
    private bool showSuccessDialog = true;
    
    private int processedCount = 0;
    private int totalCount = 0;
    private bool isProcessing = false;

    [MenuItem("Tools/平滑法线生成器")]
    static void ShowWindow()
    {
        var window = GetWindow<SmoothNormalGeneratorWindow>("平滑法线生成器");
        window.minSize = new Vector2(400, 350);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("平滑法线生成工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "拖拽方式：\n" +
            "• 从 Project 窗口拖拽：文件夹、模型文件、Prefab\n" +
            "• 从 Hierarchy 窗口拖拽：场景中的游戏对象\n" +
            "• 生成的Mesh会保存在原文件同目录下\n" +
            "• 自动替换场景中使用该Mesh的物体",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // ========== 拖拽区域 ==========
        DrawDropArea();

        EditorGUILayout.Space(10);

        // ========== 选项 ==========
        EditorGUILayout.LabelField("选项", EditorStyles.boldLabel);
        autoApplyToScene = EditorGUILayout.Toggle("自动应用到场景物体", autoApplyToScene);
        showSuccessDialog = EditorGUILayout.Toggle("显示完成提示", showSuccessDialog);

        EditorGUILayout.Space(10);

        // ========== 文件列表 ==========
        DrawAssetList();

        EditorGUILayout.Space(10);

        // ========== 操作按钮 ==========
        EditorGUI.BeginDisabledGroup((targetAssets.Count == 0 && sceneObjects.Count == 0) || isProcessing);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("清空列表", GUILayout.Height(30)))
        {
            targetAssets.Clear();
            sceneObjects.Clear();
        }
        
        if (GUILayout.Button("开始处理", GUILayout.Height(30)))
        {
            ProcessAllAssets();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.EndDisabledGroup();

        // ========== 进度显示 ==========
        if (isProcessing)
        {
            EditorGUILayout.Space(10);
            float progress = totalCount > 0 ? (float)processedCount / totalCount : 0;
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                progress,
                $"处理中... {processedCount}/{totalCount}"
            );
        }
    }

    // ========== 拖拽区域绘制 ==========
    void DrawDropArea()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
        
        // ✅ 更新提示文本
        GUI.Box(dropArea, "拖拽文件夹、模型文件或场景对象到这里\n支持从 Project 和 Hierarchy 拖拽", EditorStyles.helpBox);
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        // ✅ 区分场景对象和资产
                        if (draggedObject is GameObject go)
                        {
                            // 检查是否是场景对象
                            if (IsSceneObject(go))
                            {
                                AddSceneObject(go);
                            }
                            else
                            {
                                // Prefab 资产
                                AddAsset(draggedObject);
                            }
                        }
                        else
                        {
                            AddAsset(draggedObject);
                        }
                    }
                    
                    evt.Use();
                }
                break;
        }
    }

    // ========== ✅ 新增：检查是否是场景对象 ==========
    bool IsSceneObject(GameObject go)
    {
        // 场景对象没有资产路径
        return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go));
    }

    // ========== ✅ 新增：添加场景对象 ==========
    void AddSceneObject(GameObject go)
    {
        // 检查是否包含 Mesh
        MeshFilter mf = go.GetComponent<MeshFilter>();
        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
        
        if (mf != null || smr != null)
        {
            if (!sceneObjects.Contains(go))
            {
                sceneObjects.Add(go);
                Debug.Log($"✅ 添加场景对象: {go.name}");
            }
        }
        else
        {
            // 递归检查子对象
            MeshFilter[] childMeshFilters = go.GetComponentsInChildren<MeshFilter>();
            SkinnedMeshRenderer[] childSkinnedRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            if (childMeshFilters.Length > 0 || childSkinnedRenderers.Length > 0)
            {
                if (!sceneObjects.Contains(go))
                {
                    sceneObjects.Add(go);
                    Debug.Log($"✅ 添加场景对象（含子对象）: {go.name}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ {go.name} 不包含 Mesh 组件");
            }
        }
    }

    // ========== 添加资产 ==========
    void AddAsset(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        
        if (string.IsNullOrEmpty(path))
            return;

        // 如果是文件夹，递归添加所有Mesh
        if (AssetDatabase.IsValidFolder(path))
        {
            AddAssetsFromFolder(path);
        }
        else
        {
            // 检查是否包含Mesh
            if (ContainsMesh(obj))
            {
                if (!targetAssets.Contains(obj))
                {
                    targetAssets.Add(obj);
                }
            }
        }
    }

    // ========== 从文件夹添加 ==========
    void AddAssetsFromFolder(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model t:Mesh t:GameObject", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            
            if (asset != null && ContainsMesh(asset))
            {
                if (!targetAssets.Contains(asset))
                {
                    targetAssets.Add(asset);
                }
            }
        }
    }

    // ========== 检查是否包含Mesh ==========
    bool ContainsMesh(Object obj)
    {
        if (obj is Mesh)
            return true;

        if (obj is GameObject go)
        {
            return go.GetComponentInChildren<MeshFilter>() != null ||
                   go.GetComponentInChildren<SkinnedMeshRenderer>() != null;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return assets.Any(a => a is Mesh);
        }

        return false;
    }

    // ========== ✅ 新增：绘制资产列表（分类显示）==========
    void DrawAssetList()
    {
        int totalItems = targetAssets.Count + sceneObjects.Count;
        EditorGUILayout.LabelField($"待处理项目 ({totalItems})", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        
        // 显示资产文件
        if (targetAssets.Count > 0)
        {
            EditorGUILayout.LabelField("📁 资产文件:", EditorStyles.miniLabel);
            for (int i = targetAssets.Count - 1; i >= 0; i--)
            {
                if (targetAssets[i] == null)
                {
                    targetAssets.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(targetAssets[i], typeof(Object), false);
                if (GUILayout.Button("移除", GUILayout.Width(50)))
                {
                    targetAssets.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        // 显示场景对象
        if (sceneObjects.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎮 场景对象:", EditorStyles.miniLabel);
            for (int i = sceneObjects.Count - 1; i >= 0; i--)
            {
                if (sceneObjects[i] == null)
                {
                    sceneObjects.RemoveAt(i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(sceneObjects[i], typeof(GameObject), true);
                if (GUILayout.Button("移除", GUILayout.Width(50)))
                {
                    sceneObjects.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    // ========== 处理所有资产 ==========
    void ProcessAllAssets()
    {
        isProcessing = true;
        processedCount = 0;
        totalCount = 0;

        List<Mesh> processedMeshes = new List<Mesh>();
        Dictionary<Mesh, Mesh> meshMapping = new Dictionary<Mesh, Mesh>();

        // ✅ 收集所有Mesh（包括场景对象）
        List<Mesh> meshesToProcess = new List<Mesh>();
        
        // 从资产收集
        foreach (Object obj in targetAssets)
        {
            CollectMeshes(obj, meshesToProcess);
        }
        
        // ✅ 从场景对象收集
        foreach (GameObject go in sceneObjects)
        {
            CollectMeshesFromSceneObject(go, meshesToProcess);
        }

        totalCount = meshesToProcess.Count;

        if (totalCount == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到可处理的Mesh", "确定");
            isProcessing = false;
            return;
        }

        // 处理每个Mesh
        foreach (Mesh originalMesh in meshesToProcess)
        {
            try
            {
                Mesh newMesh = ProcessMesh(originalMesh);
                if (newMesh != null)
                {
                    meshMapping[originalMesh] = newMesh;
                    processedMeshes.Add(newMesh);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"处理 {originalMesh.name} 时出错: {e.Message}");
            }

            processedCount++;
            Repaint();
        }

        // 自动应用到场景
        if (autoApplyToScene && meshMapping.Count > 0)
        {
            ApplyToScene(meshMapping);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        isProcessing = false;

        if (showSuccessDialog)
        {
            EditorUtility.DisplayDialog(
                "完成",
                $"成功处理 {processedMeshes.Count} 个Mesh\n" +
                $"文件已保存在原模型同目录下",
                "确定"
            );
        }

        Debug.Log($"✅ 批处理完成！共处理 {processedMeshes.Count} 个Mesh");
    }

    // ========== 收集Mesh ==========
    void CollectMeshes(Object obj, List<Mesh> meshList)
    {
        if (obj is Mesh mesh)
        {
            if (!meshList.Contains(mesh))
                meshList.Add(mesh);
            return;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Mesh m && !meshList.Contains(m))
                {
                    meshList.Add(m);
                }
            }
        }
    }

    // ========== ✅ 新增：从场景对象收集Mesh ==========
    void CollectMeshesFromSceneObject(GameObject go, List<Mesh> meshList)
    {
        // 收集 MeshFilter
        MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh != null && !meshList.Contains(mf.sharedMesh))
            {
                meshList.Add(mf.sharedMesh);
            }
        }

        // 收集 SkinnedMeshRenderer
        SkinnedMeshRenderer[] skinnedRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in skinnedRenderers)
        {
            if (smr.sharedMesh != null && !meshList.Contains(smr.sharedMesh))
            {
                meshList.Add(smr.sharedMesh);
            }
        }
    }

    // ========== 处理单个Mesh ==========
    Mesh ProcessMesh(Mesh originalMesh)
    {
        // 创建副本
        Mesh newMesh = Object.Instantiate(originalMesh);
        newMesh.name = originalMesh.name + "_SmoothNormal";

        // 处理平滑法线
        BakeSmoothNormalToTangent(newMesh);

        // 获取保存路径
        string originalPath = AssetDatabase.GetAssetPath(originalMesh);
        if (string.IsNullOrEmpty(originalPath))
        {
            // ✅ 如果是场景中的Mesh，保存到默认路径
            originalPath = "Assets/GeneratedMeshes/";
            if (!Directory.Exists(originalPath))
            {
                Directory.CreateDirectory(originalPath);
            }
            originalPath = Path.Combine(originalPath, originalMesh.name + ".asset");
        }

        // 保存到原始Mesh同目录
        string directory = Path.GetDirectoryName(originalPath);
        string fileName = Path.GetFileNameWithoutExtension(originalPath) + "_SmoothNormal.asset";
        string newPath = Path.Combine(directory, fileName).Replace("\\", "/");

        // 检查是否已存在
        if (File.Exists(newPath))
        {
            if (!EditorUtility.DisplayDialog(
                "文件已存在",
                $"文件 {fileName} 已存在，是否覆盖？",
                "覆盖",
                "跳过"))
            {
                return null;
            }
            AssetDatabase.DeleteAsset(newPath);
        }

        // 创建资产
        AssetDatabase.CreateAsset(newMesh, newPath);
        Debug.Log($"✅ 已保存: {newPath}");

        return newMesh;
    }

    // ========== 应用到场景 ==========
    void ApplyToScene(Dictionary<Mesh, Mesh> meshMapping)
    {
        int replacedCount = 0;

        // 查找所有MeshFilter
        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (meshMapping.ContainsKey(mf.sharedMesh))
            {
                Undo.RecordObject(mf, "Replace Mesh");
                mf.sharedMesh = meshMapping[mf.sharedMesh];
                replacedCount++;
            }
        }

        // 查找所有SkinnedMeshRenderer
        SkinnedMeshRenderer[] skinnedRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in skinnedRenderers)
        {
            if (meshMapping.ContainsKey(smr.sharedMesh))
            {
                Undo.RecordObject(smr, "Replace Mesh");
                smr.sharedMesh = meshMapping[smr.sharedMesh];
                replacedCount++;
            }
        }

        if (replacedCount > 0)
        {
            Debug.Log($"✅ 已替换场景中 {replacedCount} 个物体的Mesh");
        }
    }

    // ========== 平滑法线处理 ==========
    static void BakeSmoothNormalToTangent(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = new Vector4[vertices.Length];

        var normalDict = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            if (!normalDict.ContainsKey(pos))
                normalDict[pos] = Vector3.zero;

            normalDict[pos] += normals[i];
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 smoothNormal = normalDict[vertices[i]].normalized;
            tangents[i] = new Vector4(smoothNormal.x, smoothNormal.y, smoothNormal.z, 0);
        }

        mesh.tangents = tangents;
        EditorUtility.SetDirty(mesh);
    }
}