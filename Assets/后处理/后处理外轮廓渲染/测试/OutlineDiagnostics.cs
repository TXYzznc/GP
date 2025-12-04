using UnityEngine;
using System.Text;

/// <summary>
/// 轮廓系统诊断工具
/// 用于排查渲染问题
/// </summary>
public class OutlineDiagnostics : MonoBehaviour
{
    [Header("诊断设置")]
    public bool runDiagnosticsOnStart = true;
    public bool continuousDiagnostics = false;
    
    private OutlineManager manager;
    private float lastDiagTime = 0f;
    
    void Start()
    {
        if (runDiagnosticsOnStart)
        {
            RunDiagnostics();
        }
    }
    
    void Update()
    {
        if (continuousDiagnostics && Time.time - lastDiagTime > 2f)
        {
            RunDiagnostics();
            lastDiagTime = Time.time;
        }
    }
    
    [ContextMenu("运行诊断")]
    public void RunDiagnostics()
    {
        StringBuilder report = new StringBuilder();
        
        report.AppendLine("========== 轮廓系统诊断报告 ==========");
        report.AppendLine();
        
        // 1. 检查 OutlineManager
        manager = OutlineManager.Instance;
        if (manager == null)
        {
            report.AppendLine("❌ 致命错误: OutlineManager 不存在！");
            report.AppendLine("====================================");
            Debug.LogError(report.ToString());
            return;
        }
        
        report.AppendLine($"✓ OutlineManager 已找到: {manager.gameObject.name}");
        report.AppendLine();
        
        // 2. 检查 Profile 配置
        report.AppendLine("【Profile 配置】");
        report.AppendLine($"  总数: {manager.outlineProfiles.Count}");
        report.AppendLine();
        
        int enabledCount = 0;
        foreach (var profile in manager.outlineProfiles)
        {
            string status = profile.enabled ? "✓ 启用" : "✗ 禁用";
            report.AppendLine($"  [{status}] {profile.profileName}");
            report.AppendLine($"      颜色: RGBA({profile.outlineColor.r:F2}, {profile.outlineColor.g:F2}, {profile.outlineColor.b:F2}, {profile.outlineColor.a:F2})");
            report.AppendLine($"      宽度: {profile.outlineWidth}, 迭代: {profile.iterations}");
            report.AppendLine($"      Layer Mask: {profile.targetLayer.value}");
            report.AppendLine($"      纹理缓存: {(profile.useTextureCache ? "启用" : "禁用")}");
            
            if (profile.enabled) enabledCount++;
            
            // 检查 Layer 是否有物体
            int layerIndex = GetLayerIndex(profile.targetLayer);
            if (layerIndex != -1)
            {
                string layerName = LayerMask.LayerToName(layerIndex);
                Renderer[] renderers = FindObjectsOfType<Renderer>();
                int count = 0;
                foreach (var r in renderers)
                {
                    if (r.gameObject.layer == layerIndex)
                    {
                        count++;
                    }
                }
                report.AppendLine($"      Layer '{layerName}' (Index: {layerIndex}) 物体数: {count}");
                
                if (count == 0 && profile.enabled)
                {
                    report.AppendLine($"      ⚠️ 警告: 该 Layer 没有物体，不会显示轮廓！");
                }
            }
            else
            {
                report.AppendLine($"      ⚠️ 警告: Layer Mask 无效！");
            }
            
            report.AppendLine();
        }
        
        report.AppendLine($"启用的 Profile 数量: {enabledCount}/{manager.outlineProfiles.Count}");
        report.AppendLine();
        
        // 3. 检查 Shader
        report.AppendLine("【Shader 检查】");
        CheckShader(report, "Outline/DrawOccupied");
        CheckShader(report, "Outline/OutlineDetection");
        CheckShader(report, "Outline/OutlineComposite");
        report.AppendLine();
        
        // 4. 检查相机
        report.AppendLine("【相机检查】");
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            report.AppendLine($"  ✓ 主相机: {mainCam.gameObject.name}");
            report.AppendLine($"    渲染路径: {mainCam.actualRenderingPath}");
            report.AppendLine($"    渲染管线: {(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? "URP" : "Built-in")}");
        }
        else
        {
            report.AppendLine("  ⚠️ 未找到主相机！");
        }
        report.AppendLine();
        
        // 5. 性能设置
        report.AppendLine("【性能设置】");
        report.AppendLine($"  分辨率缩放: {manager.resolutionScale}");
        report.AppendLine($"  延迟渲染: {manager.useDeferredRendering}");
        report.AppendLine($"  每帧最大 Profile: {manager.maxProfilesPerFrame}");
        report.AppendLine($"  调试信息: {manager.showDebugInfo}");
        report.AppendLine();
        
        // 6. 场景统计
        report.AppendLine("【场景统计】");
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        report.AppendLine($"  场景总渲染器数: {allRenderers.Length}");
        
        // 统计各 Layer 的物体数
        report.AppendLine("  各 Layer 物体分布:");
        string[] layerNames = { "Enemy", "Ally", "Interactive" };
        foreach (string layerName in layerNames)
        {
            int layerIdx = LayerMask.NameToLayer(layerName);
            if (layerIdx != -1)
            {
                int count = 0;
                foreach (var r in allRenderers)
                {
                    if (r.gameObject.layer == layerIdx)
                    {
                        count++;
                    }
                }
                report.AppendLine($"    {layerName} (Layer {layerIdx}): {count} 个");
            }
            else
            {
                report.AppendLine($"    {layerName}: Layer 不存在！");
            }
        }
        report.AppendLine();
        
        // 7. 问题诊断
        report.AppendLine("【问题诊断】");
        bool hasIssues = false;
        
        if (enabledCount == 0)
        {
            report.AppendLine("  ⚠️ 所有 Profile 都被禁用，不会显示轮廓");
            hasIssues = true;
        }
        
        // 检查是否有启用的 Profile 但没有物体
        foreach (var profile in manager.outlineProfiles)
        {
            if (profile.enabled)
            {
                int layerIndex = GetLayerIndex(profile.targetLayer);
                if (layerIndex != -1)
                {
                    int count = 0;
                    foreach (var r in allRenderers)
                    {
                        if (r.gameObject.layer == layerIndex)
                        {
                            count++;
                        }
                    }
                    if (count == 0)
                    {
                        report.AppendLine($"  ⚠️ Profile '{profile.profileName}' 已启用但没有对应 Layer 的物体");
                        hasIssues = true;
                    }
                }
            }
        }
        
        // 检查 Shader
        if (!IsShaderValid("Outline/DrawOccupied") || 
            !IsShaderValid("Outline/OutlineDetection") || 
            !IsShaderValid("Outline/OutlineComposite"))
        {
            report.AppendLine("  ❌ 有 Shader 不可用，轮廓渲染会失败");
            report.AppendLine("     请检查 Console 中的 Shader 编译错误");
            hasIssues = true;
        }
        
        if (!hasIssues)
        {
            report.AppendLine("  ✓ 未发现明显问题");
        }
        
        report.AppendLine();
        report.AppendLine("====================================");
        
        // 根据是否有问题选择日志级别
        if (hasIssues)
        {
            Debug.LogWarning(report.ToString());
        }
        else
        {
            Debug.Log(report.ToString());
        }
    }
    
    private void CheckShader(StringBuilder report, string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader != null)
        {
            bool isSupported = shader.isSupported;
            string status = isSupported ? "✓" : "✗";
            string supportText = isSupported ? "支持" : "不支持";
            report.AppendLine($"  {status} {shaderName}: {supportText}");
            
            if (!isSupported)
            {
                report.AppendLine($"      ❌ Shader 不支持！请检查编译错误");
            }
        }
        else
        {
            report.AppendLine($"  ✗ {shaderName}: 未找到");
        }
    }
    
    private bool IsShaderValid(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        return shader != null && shader.isSupported;
    }
    
    private int GetLayerIndex(LayerMask layerMask)
    {
        int mask = layerMask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1;
    }
}
