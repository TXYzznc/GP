using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 外轮廓描边渲染特性
/// 用于在 URP 中实现多层轮廓效果
/// </summary>
public class OutlineRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader drawOccupiedShader;
        public Shader outlineDetectionShader;
        public Shader compositeShader;
    }

    public Settings settings = new Settings();
    private OutlineRenderPass renderPass;
    private Material drawOccupiedMaterial;
    private Material outlineDetectionMaterial;
    private Material compositeMaterial;

    public override void Create()
    {
        // 创建材质 - 使用 new Material 替代 CoreUtils
        if (settings.drawOccupiedShader != null)
        {
            drawOccupiedMaterial = new Material(settings.drawOccupiedShader);
            drawOccupiedMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        else
        {
            Debug.LogWarning("[OutlineRenderFeature] Draw Occupied Shader 未分配！");
        }
        
        if (settings.outlineDetectionShader != null)
        {
            outlineDetectionMaterial = new Material(settings.outlineDetectionShader);
            outlineDetectionMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        else
        {
            Debug.LogWarning("[OutlineRenderFeature] Outline Detection Shader 未分配！");
        }
        
        if (settings.compositeShader != null)
        {
            compositeMaterial = new Material(settings.compositeShader);
            compositeMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        else
        {
            Debug.LogWarning("[OutlineRenderFeature] Composite Shader 未分配！");
        }

        if (drawOccupiedMaterial != null && 
            outlineDetectionMaterial != null && 
            compositeMaterial != null)
        {
            renderPass = new OutlineRenderPass(
                drawOccupiedMaterial, 
                outlineDetectionMaterial, 
                compositeMaterial);
            
            Debug.Log("<color=green>[OutlineRenderFeature]</color> 初始化成功！");
        }
        else
        {
            Debug.LogError("[OutlineRenderFeature] 初始化失败！请检查 Shader 配置。");
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderPass == null) return;
        
        // 只在游戏相机渲染
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // 使用新的 API
            renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            renderPass.SetTarget(renderer);
            renderer.EnqueuePass(renderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        // 清理材质
        if (drawOccupiedMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(drawOccupiedMaterial);
            else
                Object.DestroyImmediate(drawOccupiedMaterial);
        }
        
        if (outlineDetectionMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(outlineDetectionMaterial);
            else
                Object.DestroyImmediate(outlineDetectionMaterial);
        }
        
        if (compositeMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(compositeMaterial);
            else
                Object.DestroyImmediate(compositeMaterial);
        }
    }
}