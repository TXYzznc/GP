using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Reflection;

/// <summary>
/// 轮廓渲染特性辅助工具
/// 用于快速添加和配置 OutlineRenderFeature
/// </summary>
public class OutlineRenderFeatureHelper : EditorWindow
{
    private UniversalRendererData rendererData;
    private Shader drawOccupiedShader;
    private Shader outlineDetectionShader;
    private Shader compositeShader;
    
    [MenuItem("Tools/轮廓系统/配置 Render Feature")]
    public static void ShowWindow()
    {
        var window = GetWindow<OutlineRenderFeatureHelper>("轮廓 Render Feature 配置");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    void OnEnable()
    {
        // 自动查找 Shader
        drawOccupiedShader = Shader.Find("Outline/DrawOccupied");
        outlineDetectionShader = Shader.Find("Outline/OutlineDetection");
        compositeShader = Shader.Find("Outline/OutlineComposite");
        
        // 尝试自动查找 Renderer Data
        AutoFindRendererData();
    }
    
    void OnGUI()
    {
        GUILayout.Label("轮廓渲染特性配置工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "此工具帮助你快速添加和配置 Outline Render Feature。\n" +
            "如果在 'Add Renderer Feature' 菜单中找不到 Outline Render Feature，可以使用此工具。",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Renderer Data 选择
        EditorGUILayout.LabelField("步骤 1: 选择 URP Renderer Asset", EditorStyles.boldLabel);
        rendererData = (UniversalRendererData)EditorGUILayout.ObjectField(
            "Renderer Data", 
            rendererData, 
            typeof(UniversalRendererData), 
            false);
        
        if (GUILayout.Button("自动查找 Renderer Data"))
        {
            AutoFindRendererData();
        }
        
        EditorGUILayout.Space();
        
        // Shader 配置
        EditorGUILayout.LabelField("步骤 2: 配置 Shader（自动查找）", EditorStyles.boldLabel);
        
        GUI.enabled = false;
        drawOccupiedShader = (Shader)EditorGUILayout.ObjectField(
            "Draw Occupied", 
            drawOccupiedShader, 
            typeof(Shader), 
            false);
        
        outlineDetectionShader = (Shader)EditorGUILayout.ObjectField(
            "Outline Detection", 
            outlineDetectionShader, 
            typeof(Shader), 
            false);
        
        compositeShader = (Shader)EditorGUILayout.ObjectField(
            "Composite", 
            compositeShader, 
            typeof(Shader), 
            false);
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 添加按钮
        EditorGUILayout.LabelField("步骤 3: 添加 Render Feature", EditorStyles.boldLabel);
        
        GUI.enabled = rendererData != null && 
                      drawOccupiedShader != null && 
                      outlineDetectionShader != null && 
                      compositeShader != null;
        
        if (GUILayout.Button("添加 Outline Render Feature", GUILayout.Height(30)))
        {
            AddOutlineRenderFeature();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 状态显示
        EditorGUILayout.LabelField("状态检查:", EditorStyles.boldLabel);
        
        string status = "";
        status += rendererData != null ? "✓ Renderer Data 已选择\n" : "✗ 请选择 Renderer Data\n";
        status += drawOccupiedShader != null ? "✓ Draw Occupied Shader 已找到\n" : "✗ Draw Occupied Shader 未找到\n";
        status += outlineDetectionShader != null ? "✓ Outline Detection Shader 已找到\n" : "✗ Outline Detection Shader 未找到\n";
        status += compositeShader != null ? "✓ Composite Shader 已找到\n" : "✗ Composite Shader 未找到\n";
        
        EditorGUILayout.HelpBox(status, MessageType.None);
    }
    
    private void AutoFindRendererData()
    {
        // 查找所有 UniversalRendererData
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            
            Debug.Log($"自动找到 Renderer Data: {path}");
        }
        else
        {
            Debug.LogWarning("未找到 UniversalRendererData！请确保项目使用 URP。");
        }
    }
    
    private void AddOutlineRenderFeature()
    {
        if (rendererData == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择 Renderer Data！", "确定");
            return;
        }
        
        // 检查是否已存在
        var existingFeatures = GetRendererFeatures(rendererData);
        foreach (var feature in existingFeatures)
        {
            if (feature is OutlineRenderFeature)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "已存在",
                    "Renderer Data 中已经存在 Outline Render Feature。\n是否要重新配置？",
                    "是",
                    "取消");
                
                if (!overwrite) return;
                break;
            }
        }
        
        // 创建新的 OutlineRenderFeature
        var outlineFeature = ScriptableObject.CreateInstance<OutlineRenderFeature>();
        outlineFeature.name = "Outline Render Feature";
        
        // 使用反射设置 Shader
        var settingsField = typeof(OutlineRenderFeature).GetField("settings");
        if (settingsField != null)
        {
            var settings = settingsField.GetValue(outlineFeature);
            var settingsType = settings.GetType();
            
            settingsType.GetField("drawOccupiedShader").SetValue(settings, drawOccupiedShader);
            settingsType.GetField("outlineDetectionShader").SetValue(settings, outlineDetectionShader);
            settingsType.GetField("compositeShader").SetValue(settings, compositeShader);
            
            settingsField.SetValue(outlineFeature, settings);
        }
        
        // 添加到 Renderer Data
        AddFeatureToRenderer(rendererData, outlineFeature);
        
        // 保存并刷新
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog(
            "成功",
            "Outline Render Feature 已成功添加到 Renderer Data！\n\n" +
            "你可以在 Renderer Data 的 Inspector 中查看和调整配置。",
            "确定");
        
        // 选中 Renderer Data 以便查看
        Selection.activeObject = rendererData;
        EditorGUIUtility.PingObject(rendererData);
        
        Debug.Log($"<color=green>✓</color> Outline Render Feature 已添加到: {AssetDatabase.GetAssetPath(rendererData)}");
    }
    
    private System.Collections.Generic.List<ScriptableRendererFeature> GetRendererFeatures(UniversalRendererData renderer)
    {
        var features = new System.Collections.Generic.List<ScriptableRendererFeature>();
        
        var property = typeof(UniversalRendererData).GetProperty(
            "rendererFeatures",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (property != null)
        {
            var featureList = property.GetValue(renderer) as System.Collections.Generic.List<ScriptableRendererFeature>;
            if (featureList != null)
            {
                features.AddRange(featureList);
            }
        }
        
        return features;
    }
    
    private void AddFeatureToRenderer(UniversalRendererData renderer, ScriptableRendererFeature feature)
    {
        var property = typeof(UniversalRendererData).GetProperty(
            "rendererFeatures",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (property != null)
        {
            var featureList = property.GetValue(renderer) as System.Collections.Generic.List<ScriptableRendererFeature>;
            if (featureList != null)
            {
                featureList.Add(feature);
                property.SetValue(renderer, featureList);
                
                // 添加为子资源
                AssetDatabase.AddObjectToAsset(feature, renderer);
            }
        }
    }
}
