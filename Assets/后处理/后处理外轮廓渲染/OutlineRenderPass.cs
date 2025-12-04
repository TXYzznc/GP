using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class OutlineRenderPass : ScriptableRenderPass
{
    private OutlineManager outlineManager;
    private Material drawOccupiedMaterial;
    private Material outlineDetectionMaterial;
    private Material compositeMaterial;
    private Material addMaterial;  // 用于累加轮廓的材质

    private RTHandle tempColorTexture;
    private RTHandle occupiedTexture;
    private RTHandle outlineTexture;
    private RTHandle accumulatedOutlineTexture;  // 累积所有轮廓的纹理

    private FilteringSettings filteringSettings;
    private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    private ProfilingSampler profilingSampler;

    public OutlineRenderPass(Material drawOccupied, Material outlineDetection, Material composite)
    {
        this.drawOccupiedMaterial = drawOccupied;
        this.outlineDetectionMaterial = outlineDetection;
        this.compositeMaterial = composite;
        
        // 创建加法混合材质
        Shader addShader = Shader.Find("Outline/OutlineAdd");
        if (addShader != null)
        {
            this.addMaterial = new Material(addShader);
        }

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        shaderTagIdList.Add(new ShaderTagId("LightweightForward"));
        shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));

        profilingSampler = new ProfilingSampler("OutlineRenderPass");
    }

    public void SetTarget(ScriptableRenderer renderer)
    {
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (outlineManager == null)
        {
            outlineManager = OutlineManager.Instance;
        }

        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        if (outlineManager != null)
        {
            descriptor.width = Mathf.RoundToInt(descriptor.width * outlineManager.resolutionScale);
            descriptor.height = Mathf.RoundToInt(descriptor.height * outlineManager.resolutionScale);
        }

        RenderingUtils.ReAllocateIfNeeded(ref tempColorTexture, descriptor, name: "_TempColorTexture");
        RenderingUtils.ReAllocateIfNeeded(ref occupiedTexture, descriptor, name: "_OccupiedTexture");
        RenderingUtils.ReAllocateIfNeeded(ref outlineTexture, descriptor, name: "_OutlineTexture");
        RenderingUtils.ReAllocateIfNeeded(ref accumulatedOutlineTexture, descriptor, name: "_AccumulatedOutlineTexture");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (drawOccupiedMaterial == null || outlineDetectionMaterial == null || compositeMaterial == null)
        {
            return;
        }

        if (outlineManager == null)
        {
            return;
        }

        List<OutlineProfile> activeProfiles = outlineManager.GetActiveProfiles();
        if (activeProfiles == null || activeProfiles.Count == 0)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get();

        RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        Camera camera = renderingData.cameraData.camera;

        using (new ProfilingScope(cmd, profilingSampler))
        {
            // 复制原始场景到临时纹理
            cmd.Blit(cameraColorTarget, tempColorTexture);
            
            // 清空累积轮廓纹理
            cmd.SetRenderTarget(accumulatedOutlineTexture);
            cmd.ClearRenderTarget(false, true, Color.clear);
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 渲染每个 Profile 的轮廓并累积
            foreach (var profile in activeProfiles)
            {
                RenderOutlineForProfile(cmd, context, ref renderingData, profile, camera);
            }

            // 最后一次性混合所有轮廓到场景
            compositeMaterial.SetTexture("_OutlineTex", accumulatedOutlineTexture);
            RenderTexture finalRT = RenderTexture.GetTemporary(
                tempColorTexture.rt.width,
                tempColorTexture.rt.height,
                0,
                tempColorTexture.rt.format);
            
            cmd.Blit(tempColorTexture, finalRT, compositeMaterial, 0);
            cmd.Blit(finalRT, cameraColorTarget);
            
            RenderTexture.ReleaseTemporary(finalRT);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private void RenderOutlineForProfile(
        CommandBuffer cmd,
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        OutlineProfile profile,
        Camera camera)
    {
        List<Renderer> visibleRenderers = outlineManager.GetVisibleRenderers(profile, camera);
        if (visibleRenderers.Count == 0)
        {
            return;
        }

        // 1. 渲染物体占据区域
        RenderOccupiedArea(cmd, context, ref renderingData, profile.targetLayer);

        // 2. 检测轮廓边缘
        outlineDetectionMaterial.SetInt("_Width", profile.outlineWidth);
        outlineDetectionMaterial.SetInt("_Iterations", profile.iterations);
        outlineDetectionMaterial.SetColor("_Color", profile.outlineColor);
        cmd.Blit(occupiedTexture, outlineTexture, outlineDetectionMaterial, 0);

        // 执行命令以确保 outlineTexture 已经生成
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 3. 累积轮廓到 accumulatedOutlineTexture（使用加法混合）
        if (addMaterial != null)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(
                accumulatedOutlineTexture.rt.width,
                accumulatedOutlineTexture.rt.height,
                0,
                accumulatedOutlineTexture.rt.format);
            
            // 使用加法 Shader 将当前轮廓累加到已有轮廓上
            addMaterial.SetTexture("_MainTex", accumulatedOutlineTexture);
            addMaterial.SetTexture("_AddTex", outlineTexture);
            cmd.Blit(outlineTexture, tempRT, addMaterial, 0);
            cmd.Blit(tempRT, accumulatedOutlineTexture);
            
            // 执行累加命令
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            RenderTexture.ReleaseTemporary(tempRT);
        }
    }

    private void RenderOccupiedArea(
        CommandBuffer cmd,
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        LayerMask targetLayer)
    {
        // 设置渲染目标
        cmd.SetRenderTarget(
            occupiedTexture,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store,
            occupiedTexture,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.DontCare);
        
        cmd.ClearRenderTarget(true, true, Color.black);
        
        // 执行渲染目标设置
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 设置绘制设置
        var drawingSettings = CreateDrawingSettings(
            shaderTagIdList,
            ref renderingData,
            SortingCriteria.CommonOpaque);

        drawingSettings.overrideMaterial = drawOccupiedMaterial;
        drawingSettings.overrideMaterialPassIndex = 0;

        // 设置过滤
        var filterSettings = filteringSettings;
        filterSettings.layerMask = targetLayer;

        // 渲染物体到 occupiedTexture
        context.DrawRenderers(
            renderingData.cullResults,
            ref drawingSettings,
            ref filterSettings);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }

    public void Dispose()
    {
        tempColorTexture?.Release();
        occupiedTexture?.Release();
        outlineTexture?.Release();
        accumulatedOutlineTexture?.Release();
        
        if (addMaterial != null)
        {
            Object.DestroyImmediate(addMaterial);
        }
    }
}
