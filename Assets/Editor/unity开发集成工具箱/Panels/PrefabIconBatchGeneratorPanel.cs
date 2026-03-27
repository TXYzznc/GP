using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// 预制体图标批量生成器面板 - 可集成到工具箱，也可独立使用
/// </summary>
[ToolHubItem("资源工具/预制体图标生成器", "批量生成预制体预览图标", 30)]
public class PrefabIconBatchGeneratorPanel : IToolHubPanel
{
    // 输入设置
    /// <summary>预制体文件夹 - 包含需要生成图标的预制体</summary>
    private DefaultAsset prefabFolder;
    /// <summary>预制体列表 - 直接拖入的预制体对象</summary>
    private System.Collections.Generic.List<GameObject> prefabList = new System.Collections.Generic.List<GameObject>();
    /// <summary>预制体库（可选）- 如果指定，生成的图标会自动回写到库中</summary>
    // 预览设置
    /// <summary>预览预制体 - 用于实时预览截图效果</summary>
    private GameObject previewPrefab;
    /// <summary>预览纹理 - 存储预览图</summary>
    private Texture2D previewTexture;
    /// <summary>预制体列表滚动位置</summary>
    private Vector2 prefabListScrollPos;
    /// <summary>主界面滚动位置</summary>
    private Vector2 mainScrollPos;
    
    // 渲染设置
    /// <summary>临时Layer索引 - 用于隔离渲染对象</summary>
    private const int TEMP_LAYER = 31;

    // 输出设置
    private DefaultAsset outputFolder;
    private int size = 256;
    private bool transparentBackground = true;
    private Color backgroundColor = new Color(0, 0, 0, 0);

    // 相机设置
    private float yaw = 45f;
    private float pitch = 25f;
    private float padding = 1.25f;
    private bool orthographic = false;
    private float fieldOfView = 30f;

    // 光照设置
    private float lightIntensity = 1.2f;
    private Color lightColor = Color.white;
    private Color ambientColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    public void OnEnable() { }

    public void OnDisable()
    {
        if (previewTexture != null)
        {
            UnityEngine.Object.DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    public void OnDestroy()
    {
        if (previewTexture != null)
        {
            UnityEngine.Object.DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    public string GetHelpText()
    {
        return "批量生成预制体图标。支持自定义相机角度、光照和输出设置。";
    }

    public void OnGUI()
    {   
        // 开始主滚动视图 - 为底部按钮留出空间
        mainScrollPos = EditorGUILayout.BeginScrollView(
            mainScrollPos,
            GUILayout.Height(EditorGUIUtility.currentViewWidth > 0 ? 
                GUILayoutUtility.GetRect(0, 0).y : 0));

        EditorGUILayout.LabelField("输入设置", EditorStyles.boldLabel);

        // 第一行：预制体文件夹 和 预制体库
        EditorGUILayout.BeginHorizontal();
        prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("预制体文件夹", "选择包含预制体的文件夹（可选）"),
            prefabFolder, typeof(DefaultAsset), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("或", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);

        // 预制体列表区域 - 固定高度，左右布局
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("预制体列表", GUILayout.Width(100));
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            prefabList.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.Height(150));
        
        // 左侧：滚动列表区域
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        prefabListScrollPos = EditorGUILayout.BeginScrollView(
            prefabListScrollPos,
            GUILayout.Height(150));

        if (prefabList.Count > 0)
        {
            for (int i = 0; i < prefabList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                prefabList[i] = (GameObject)EditorGUILayout.ObjectField(
                    $"[{i}]",
                    prefabList[i],
                    typeof(GameObject),
                    false,
                    GUILayout.Width(200));
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    prefabList.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("列表为空", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 右侧：拖拽区域
        EditorGUILayout.BeginVertical(GUILayout.Width(150));
        DrawPrefabDropArea();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);
        
        // 第一行：输出文件夹 和 图标尺寸
        EditorGUILayout.BeginHorizontal();
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("输出文件夹", "必须在 Assets 目录下，用于保存生成的图标"),
            outputFolder, typeof(DefaultAsset), false);
        
        size = EditorGUILayout.IntField(
            new GUIContent("图标尺寸", "生成的图标大小（像素），范围 32-8192"),
            size,
            GUILayout.Width(200));
        size = Mathf.Clamp(size, 32, 8192);
        EditorGUILayout.EndHorizontal();

        // 第二行：透明背景 和 背景颜色
        EditorGUILayout.BeginHorizontal();
        transparentBackground = EditorGUILayout.Toggle(
            new GUIContent("透明背景", "启用后背景为透明，否则使用指定的背景颜色"),
            transparentBackground);
        
        using (new EditorGUI.DisabledScope(transparentBackground))
        {
            backgroundColor = EditorGUILayout.ColorField(
                new GUIContent("背景颜色", "非透明背景时使用的颜色"),
                backgroundColor);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("相机设置", EditorStyles.boldLabel);
        
        // 第一行：偏航角 和 俯仰角
        EditorGUILayout.BeginHorizontal();
        yaw = EditorGUILayout.Slider(
            new GUIContent("偏航角", "相机水平旋转角度，-180° 到 180°"),
            yaw, -180f, 180f);
        pitch = EditorGUILayout.Slider(
            new GUIContent("俯仰角", "相机垂直旋转角度，-89° 到 89°"),
            pitch, -89f, 89f);
        EditorGUILayout.EndHorizontal();

        // 第二行：边距系数 和 正交投影
        EditorGUILayout.BeginHorizontal();
        padding = EditorGUILayout.Slider(
            new GUIContent("边距系数", "控制预制体在图标中的大小，值越大预制体越小"),
            padding, 0.5f, 2.5f);
        orthographic = EditorGUILayout.Toggle(
            new GUIContent("正交投影", "启用正交投影，否则使用透视投影"),
            orthographic,
            GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        // 第三行：视野角度（仅透视投影时）
        using (new EditorGUI.DisabledScope(orthographic))
        {
            fieldOfView = EditorGUILayout.Slider(
                new GUIContent("视野角度 (FOV)", "透视投影时的相机视野角度"),
                fieldOfView, 10f, 80f);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("光照设置", EditorStyles.boldLabel);
        
        // 第一行：光照强度
        lightIntensity = EditorGUILayout.Slider(
            new GUIContent("光照强度", "主光源的强度，0-5"),
            lightIntensity, 0f, 5f);
        
        // 第二行：光照颜色 和 环境光颜色
        EditorGUILayout.BeginHorizontal();
        lightColor = EditorGUILayout.ColorField(
            new GUIContent("光照颜色", "主光源的颜色"),
            lightColor);
        ambientColor = EditorGUILayout.ColorField(
            new GUIContent("环境光颜色", "场景的环境光颜色"),
            ambientColor);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // 预览功能 - 固定大小的正方形区域
        EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GameObject newPreviewPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("预览预制体", "拖入预制体查看截图效果"),
            previewPrefab,
            typeof(GameObject),
            false);

        // 检测预览对象是否改变
        if (newPreviewPrefab != previewPrefab)
        {
            previewPrefab = newPreviewPrefab;
            UpdatePreview();
        }

        if (GUILayout.Button("刷新预览", GUILayout.Width(80)))
        {
            UpdatePreview();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 固定大小的正方形预览区域
        DrawFixedPreviewArea();

        EditorGUILayout.Space(12);

        // 结束主滚动视图
        EditorGUILayout.EndScrollView();

        // 固定在底部的按钮区域 - 不在滚动视图内
        EditorGUILayout.Space(4);
        
        using (new EditorGUI.DisabledScope(!CanRun(out string reason)))
        {
            if (GUILayout.Button("生成图标", GUILayout.Height(36)))
            {
                Generate();
            }
        }

        if (!CanRun(out string r))
            EditorGUILayout.HelpBox(r, MessageType.Warning);

        EditorGUILayout.HelpBox(
            "说明：\n" +
            "- 可以选择文件夹批量生成，或拖入预制体到拖拽区域\n" +
            "- 拖拽区域支持同时拖入多个预制体\n" +
            "- 输出目录请放在 Assets 下，才能自动导入为 Sprite\n" +
            "- 使用预览功能可以实时查看截图效果\n",
            MessageType.Info);
    }

    /// <summary>
    /// 验证是否可以运行生成操作
    /// </summary>
    private bool CanRun(out string reason)
    {
        reason = null;

        bool hasPrefabFolder = prefabFolder != null;
        bool hasPrefabList = prefabList != null && prefabList.Count > 0 && prefabList.Any(p => p != null);

        if (!hasPrefabFolder && !hasPrefabList)
        {
            reason = "请选择预制体文件夹或添加预制体到列表。";
            return false;
        }

        if (outputFolder == null)
        {
            reason = "请选择 Output Folder（建议 Assets 内）。";
            return false;
        }

        string outPath = AssetDatabase.GetAssetPath(outputFolder);
        if (string.IsNullOrEmpty(outPath) || !outPath.StartsWith("Assets"))
        {
            reason = "Output Folder 必须在 Assets 下（否则无法自动导入为 Sprite 并回写）。";
            return false;
        }

        if (hasPrefabFolder)
        {
            string inPath = AssetDatabase.GetAssetPath(prefabFolder);
            if (string.IsNullOrEmpty(inPath) || !AssetDatabase.IsValidFolder(inPath))
            {
                reason = "Prefab Folder 不是有效文件夹。";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 验证预制体是否有效
    /// </summary>
    private bool ValidatePrefab(GameObject prefab, out string errorMessage)
    {
        errorMessage = null;

        if (prefab == null)
        {
            errorMessage = "预制体为 null";
            return false;
        }

        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            errorMessage = $"预制体 '{prefab.name}' 没有任何 Renderer 组件";
            return false;
        }

        int disabledCount = 0;
        int noMaterialCount = 0;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!r.enabled) disabledCount++;
            if (r.sharedMaterial == null) noMaterialCount++;
        }

        if (disabledCount == renderers.Length)
        {
            errorMessage = $"预制体 '{prefab.name}' 的所有 Renderer 都被禁用";
            return false;
        }

        if (noMaterialCount == renderers.Length)
        {
            errorMessage = $"预制体 '{prefab.name}' 的所有 Renderer 都缺少材质";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 绘制预制体拖拽区域 - 支持同时拖入多个预制体（固定大小版本）
    /// </summary>
    private void DrawPrefabDropArea()
    {
        Event evt = Event.current;

        // 固定大小的拖拽区域
        Rect dropArea = GUILayoutUtility.GetRect(150, 150, GUILayout.Width(150), GUILayout.Height(150));

        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
        GUI.Box(dropArea, "", EditorStyles.helpBox);
        GUI.backgroundColor = originalColor;

        GUIStyle centeredStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
            wordWrap = true
        };

        string hintText = "🎯 拖拽预制体\n到这里\n\n支持同时\n拖入多个";

        GUI.Label(dropArea, hintText, centeredStyle);

        switch (evt.type)
        {
            case EventType.DragUpdated:
                if (dropArea.Contains(evt.mousePosition))
                {
                    bool hasValidPrefab = false;
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject)
                        {
                            hasValidPrefab = true;
                            break;
                        }
                    }

                    DragAndDrop.visualMode = hasValidPrefab
                        ? DragAndDropVisualMode.Copy
                        : DragAndDropVisualMode.Rejected;

                    evt.Use();
                }
                break;

            case EventType.DragPerform:
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.AcceptDrag();

                    int addedCount = 0;
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go)
                        {
                            if (!prefabList.Contains(go))
                            {
                                prefabList.Add(go);
                                addedCount++;
                            }
                        }
                    }

                    if (addedCount > 0)
                    {
                        Debug.Log($"[图标生成] 已添加 {addedCount} 个预制体到列表");
                    }

                    evt.Use();
                }
                break;
        }
    }

    /// <summary>
    /// 绘制固定大小的正方形预览区域
    /// </summary>
    private void DrawFixedPreviewArea()
    {
        const float previewSize = 256f;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical(GUILayout.Width(previewSize));

        Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.Width(previewSize), GUILayout.Height(previewSize));

        EditorGUI.DrawRect(new Rect(previewRect.x - 1, previewRect.y - 1, previewRect.width + 2, previewRect.height + 2), new Color(0.3f, 0.3f, 0.3f));

        if (previewTexture != null)
        {
            if (transparentBackground)
            {
                DrawCheckerboard(previewRect);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, backgroundColor);
            }

            GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(
                $"尺寸: {previewTexture.width}x{previewTexture.height}",
                EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            if (transparentBackground)
            {
                DrawCheckerboard(previewRect);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, backgroundColor);
            }

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            GUI.Label(previewRect, "拖入预制体到上方\n查看预览效果", hintStyle);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("无预览", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制棋盘格背景
    /// </summary>
    private void DrawCheckerboard(Rect rect)
    {
        int gridSize = 8;
        Color lightGray = new Color(0.8f, 0.8f, 0.8f);
        Color darkGray = new Color(0.6f, 0.6f, 0.6f);

        int cols = Mathf.CeilToInt(rect.width / gridSize);
        int rows = Mathf.CeilToInt(rect.height / gridSize);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Rect cellRect = new Rect(
                    rect.x + x * gridSize,
                    rect.y + y * gridSize,
                    gridSize,
                    gridSize
                );

                Color color = (x + y) % 2 == 0 ? lightGray : darkGray;
                EditorGUI.DrawRect(cellRect, color);
            }
        }
    }

    /// <summary>
    /// 更新预览图（使用真实相机渲染方式）
    /// </summary>
    private void UpdatePreview()
    {
        if (previewTexture != null)
        {
            UnityEngine.Object.DestroyImmediate(previewTexture);
            previewTexture = null;
        }

        if (previewPrefab == null)
        {
            return;
        }

        if (!ValidatePrefab(previewPrefab, out string error))
        {
            Debug.LogWarning($"[预览] {error}");
            EditorUtility.DisplayDialog("预览失败", error + "\n\n请检查预制体是否包含有效的 Renderer 组件。", "确定");
            return;
        }

        // 创建临时的PreviewRenderUtility来传递参数
        var preview = new PreviewRenderUtility();
        try
        {
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : backgroundColor;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 1000f;
            preview.camera.fieldOfView = fieldOfView;
            preview.ambientColor = ambientColor;

            preview.lights[0].intensity = lightIntensity;
            preview.lights[0].color = lightColor;
            preview.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0f);

            preview.lights[1].intensity = lightIntensity * 0.35f;
            preview.lights[1].color = lightColor;
            preview.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 0f);

            // 使用新的渲染方式
            previewTexture = RenderPrefabToTexture2D(preview, previewPrefab, 256, yaw, pitch, padding, orthographic);

            if (previewTexture == null)
            {
                Debug.LogError($"[预览] 渲染失败，返回的纹理为 null。预制体: {previewPrefab.name}");
                EditorUtility.DisplayDialog("预览失败",
                    $"无法渲染预制体 '{previewPrefab.name}'。\n\n可能的原因：\n" +
                    "1. 预制体没有有效的 Renderer 组件\n" +
                    "2. 所有 Renderer 都被禁用\n" +
                    "3. Renderer 缺少材质\n\n" +
                    "请检查 Console 中的详细警告信息。",
                    "确定");
            }
            else
            {
                Debug.Log($"[预览] 成功生成预览图: {previewPrefab.name} ({previewTexture.width}x{previewTexture.height})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[预览] 生成预览时发生错误: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("预览失败", $"生成预览时发生错误:\n{e.Message}", "确定");
        }
        finally
        {
            preview.Cleanup();
        }

        GUI.changed = true;
    }

    /// <summary>
    /// 执行图标生成操作
    /// </summary>
    private void Generate()
    {
        string outputFolderPath = AssetDatabase.GetAssetPath(outputFolder);

        var prefabPathSet = new System.Collections.Generic.HashSet<string>();
        var prefabsToProcess = new System.Collections.Generic.List<GameObject>();

        if (prefabFolder != null)
        {
            string prefabFolderPath = AssetDatabase.GetAssetPath(prefabFolder);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });

            foreach (var guid in guids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (prefabPathSet.Add(prefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        prefabsToProcess.Add(prefab);
                    }
                }
            }
        }

        if (prefabList != null && prefabList.Count > 0)
        {
            foreach (var prefab in prefabList)
            {
                if (prefab != null)
                {
                    string path = AssetDatabase.GetAssetPath(prefab);
                    if (!string.IsNullOrEmpty(path) && prefabPathSet.Add(path))
                    {
                        var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (loadedPrefab != null)
                        {
                            prefabsToProcess.Add(loadedPrefab);
                        }
                    }
                }
            }
        }

        if (prefabsToProcess.Count == 0)
        {
            EditorUtility.DisplayDialog("Prefab Icon Generator", "没有找到要处理的预制体。", "OK");
            return;
        }

        var validPrefabs = new System.Collections.Generic.List<GameObject>();
        var invalidPrefabs = new System.Collections.Generic.List<string>();

        foreach (var prefab in prefabsToProcess)
        {
            if (ValidatePrefab(prefab, out string error))
            {
                validPrefabs.Add(prefab);
            }
            else
            {
                invalidPrefabs.Add(error);
                Debug.LogWarning($"[图标生成] 跳过无效预制体: {error}", prefab);
            }
        }

        if (invalidPrefabs.Count > 0)
        {
            string message = $"发现 {invalidPrefabs.Count} 个无效预制体:\n\n" +
                            string.Join("\n", invalidPrefabs.Take(5));
            if (invalidPrefabs.Count > 5)
            {
                message += $"\n... 还有 {invalidPrefabs.Count - 5} 个";
            }
            message += "\n\n这些预制体将被跳过。是否继续处理有效的预制体？";

            if (!EditorUtility.DisplayDialog("发现无效预制体", message, "继续", "取消"))
            {
                return;
            }
        }

        if (validPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有找到任何有效的预制体可以处理。\n\n请检查预制体是否包含 Renderer 组件。", "确定");
            return;
        }

        Debug.Log($"[图标生成] 开始处理 {validPrefabs.Count} 个有效预制体（跳过 {invalidPrefabs.Count} 个无效预制体）");

        var preview = new PreviewRenderUtility();
        try
        {
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : backgroundColor;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 1000f;
            preview.camera.fieldOfView = fieldOfView;
            preview.camera.cullingMask = -1;
            preview.ambientColor = ambientColor;

            preview.lights[0].intensity = lightIntensity;
            preview.lights[0].color = lightColor;
            preview.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0f);

            preview.lights[1].intensity = lightIntensity * 0.35f;
            preview.lights[1].color = lightColor;
            preview.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 0f);

            int okCount = 0;

            for (int i = 0; i < validPrefabs.Count; i++)
            {
                var prefab = validPrefabs[i];

                if (EditorUtility.DisplayCancelableProgressBar(
                        "正在生成预制体图标",
                        $"{prefab.name} ({i + 1}/{validPrefabs.Count})",
                        (float)(i + 1) / validPrefabs.Count))
                {
                    Debug.Log($"[图标生成] 用户取消操作，已处理 {okCount} 个预制体");
                    break;
                }

                string pngAssetPath = Path.Combine(outputFolderPath, prefab.name + ".png").Replace("\\", "/");

                try
                {
                    Texture2D tex = RenderPrefabToTexture2D(preview, prefab, size, yaw, pitch, padding, orthographic);

                    if (tex != null)
                    {
                        byte[] bytes = tex.EncodeToPNG();
                        File.WriteAllBytes(pngAssetPath, bytes);
                        UnityEngine.Object.DestroyImmediate(tex);

                        AssetDatabase.ImportAsset(pngAssetPath, ImportAssetOptions.ForceUpdate);
                        SetupAsSprite(pngAssetPath, transparentBackground);

                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngAssetPath);

                        okCount++;
                        Debug.Log($"[图标生成] 成功生成: {prefab.name} -> {pngAssetPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[图标生成] 渲染失败: {prefab.name}（返回纹理为 null）", prefab);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[图标生成] 处理预制体 '{prefab.name}' 时发生错误: {e.Message}\n{e.StackTrace}", prefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string resultMessage = $"完成：成功生成 {okCount} 张图标";
            if (invalidPrefabs.Count > 0)
            {
                resultMessage += $"\n跳过 {invalidPrefabs.Count} 个无效预制体";
            }

            EditorUtility.DisplayDialog("Prefab Icon Generator", resultMessage, "OK");
            Debug.Log($"[图标生成] {resultMessage}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            preview.Cleanup();
        }
    }

    /// <summary>
    /// 将预制体渲染为Texture2D（使用真实相机和独立Layer，避免PreviewRenderUtility的兼容性问题）
    /// </summary>
    private static Texture2D RenderPrefabToTexture2D(
        PreviewRenderUtility preview,
        GameObject prefab,
        int size,
        float yaw,
        float pitch,
        float padding,
        bool orthographic)
    {
        GameObject instance = null;
        GameObject camGO = null;
        GameObject mainLightGO = null;
        GameObject fillLightGO = null;
        RenderTexture rt = null;

        try
        {
            // 1. 实例化预制体并设置到独立Layer
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // 递归设置所有子对象到临时Layer
            SetLayerRecursive(instance, TEMP_LAYER);

            // 2. 检查Renderer
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning($"[图标生成] 预制体 '{prefab.name}' 没有Renderer组件，将生成空白图标。", prefab);
                return null;
            }

            // 3. 计算包围盒
            Bounds bounds = CalculateBounds(instance);
            Vector3 center = bounds.center;

            // 4. 创建真实相机
            camGO = new GameObject("IconGenCamera");
            camGO.hideFlags = HideFlags.HideAndDontSave;
            var cam = camGO.AddComponent<Camera>();

            // 只渲染临时Layer，避免场景干扰
            cam.cullingMask = 1 << TEMP_LAYER;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = preview.camera.backgroundColor;
            cam.orthographic = orthographic;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;

            if (!orthographic)
            {
                cam.fieldOfView = preview.camera.fieldOfView;
            }

            // 5. 设置相机位置和旋转
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;

            if (orthographic)
            {
                float radius = bounds.extents.magnitude * padding;
                cam.orthographicSize = radius;
                cam.transform.position = center + dir * 10f;
            }
            else
            {
                float radius = bounds.extents.magnitude * padding;
                float fov = cam.fieldOfView * Mathf.Deg2Rad;
                float dist = radius / Mathf.Sin(fov * 0.5f);
                cam.transform.position = center + dir * dist;
            }

            cam.transform.LookAt(center);

            // 6. 创建主光源
            mainLightGO = new GameObject("IconGenMainLight");
            mainLightGO.hideFlags = HideFlags.HideAndDontSave;
            var mainLight = mainLightGO.AddComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.intensity = preview.lights[0].intensity;
            mainLight.color = preview.lights[0].color;
            mainLight.transform.rotation = preview.lights[0].transform.rotation;
            mainLight.cullingMask = 1 << TEMP_LAYER;

            // 7. 创建辅助光源
            fillLightGO = new GameObject("IconGenFillLight");
            fillLightGO.hideFlags = HideFlags.HideAndDontSave;
            var fillLight = fillLightGO.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = preview.lights[1].intensity;
            fillLight.color = preview.lights[1].color;
            fillLight.transform.rotation = preview.lights[1].transform.rotation;
            fillLight.cullingMask = 1 << TEMP_LAYER;

            // 8. 设置环境光
            Color originalAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = preview.ambientColor;

            // 9. 渲染到RenderTexture
            rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 8;
            cam.targetTexture = rt;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            cam.Render();

            // 10. 读取到Texture2D
            var tex2D = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            tex2D.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex2D.Apply();

            RenderTexture.active = prev;

            // 恢复环境光
            RenderSettings.ambientLight = originalAmbient;

            return tex2D;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[图标生成] 渲染预制体 '{prefab.name}' 时发生异常: {e.Message}\n{e.StackTrace}", prefab);
            return null;
        }
        finally
        {
            // 清理所有创建的对象
            if (rt != null)
            {
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }

            if (fillLightGO != null)
                UnityEngine.Object.DestroyImmediate(fillLightGO);

            if (mainLightGO != null)
                UnityEngine.Object.DestroyImmediate(mainLightGO);

            if (camGO != null)
                UnityEngine.Object.DestroyImmediate(camGO);

            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    /// <summary>
    /// 递归设置GameObject及其所有子对象的Layer
    /// </summary>
    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    /// <summary>
    /// 计算GameObject的包围盒
    /// </summary>
    private static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    /// <summary>
    /// 将纹理资源设置为Sprite类型
    /// </summary>
    private static void SetupAsSprite(string assetPath, bool hasAlpha)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = hasAlpha;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        importer.SaveAndReimport();
    }
}