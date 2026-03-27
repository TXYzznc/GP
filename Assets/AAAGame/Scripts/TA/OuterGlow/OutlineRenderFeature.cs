using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

/// <summary>
/// 优化版外轮廓渲染特性（无合批版本）2
/// 保留：RT降采样、视锥剔除、距离LOD
/// </summary>
public class OutlineRenderFeature : ScriptableRendererFeature
{
    private static OutlineRenderFeature _instance;
    public static OutlineRenderFeature Instance => _instance;

    [Header("=== 着色器配置 ===")]
    [Tooltip("描边着色器，用于绘制物体轮廓")]
    public Shader OutlineShader;
    [Tooltip("渲染层级")]
    [SerializeField] private LayerMask m_LayerMask = -1;

    [Tooltip("模糊着色器，用于生成描边模糊效果")]
    public Shader BlurShader;

    [Header("=== 默认外观 ===")]
    [Tooltip("默认描边颜色")]
    public Color DefaultColor = Color.yellow;

    [Tooltip("默认描边宽度（像素）")]
    public float DefaultBlurSize = 20f;

    [Header("=== 渲染参数 ===")]
    [Tooltip("裁剪尺寸，控制描边边缘的锐利程度")]
    public float ClipSize = 0.5f;

    [Tooltip("描边强度，控制最终描边的可见度")]
    public float Intensity = 1f;

    [Tooltip("指数幂次，控制描边的衰减曲线")]
    public float ExpPower = 1f;

    [Header("=== 性能优化 ===")]
    [Tooltip("渲染纹理降采样倍数 (1=全分辨率, 2=半分辨率, 4=四分之一分辨率)\n降低分辨率可提升性能，但描边会略微模糊")]
    [Range(1, 4)]
    public int RTDownsample = 2;

    [Tooltip("启用视锥体剔除\n不在相机视野内的物体将不会渲染描边")]
    public bool EnableFrustumCulling = true;

    [Tooltip("启用距离LOD\n超过指定距离的物体将不会渲染描边")]
    public bool EnableDistanceLOD = true;

    [Tooltip("最大描边渲染距离（米）\n超过此距离的物体不会显示描边")]
    public float MaxOutlineDistance = 100f;

    private SimpleOutlineRenderPass _outlineRenderPass;
    private bool _isReady;

    public bool IsReady => _isReady;

    #region 数据管理

    public class OutlineData
    {
        public int Id;
        public List<Renderer> Renderers;
        public Color OutlineColor;
        public float OutlineSize;

        public OutlineData(int id, List<Renderer> renderers, Color color, float size)
        {
            Id = id;
            Renderers = renderers;
            OutlineColor = color;
            OutlineSize = size;
        }

        public void UpdateOutline(Color color, float size)
        {
            OutlineColor = color;
            OutlineSize = size;
        }
    }

    private List<OutlineData> _outlineDatas = new List<OutlineData>();
    public List<OutlineData> OutlineDatas => _outlineDatas;

    #endregion

    public override void Create()
    {
        _instance = this;

        if (_outlineRenderPass == null)
        {
            _outlineDatas.Clear();
            _outlineRenderPass = new SimpleOutlineRenderPass();
            _outlineRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            _isReady = false;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Application.isPlaying)
            return;

        if (_outlineRenderPass == null)
            return;

        // 检查当前相机的 Culling Mask 是否包含目标层
        if ((renderingData.cameraData.camera.cullingMask & m_LayerMask) == 0)
        {
            return;  // 如果不包含，跳过此 Pass
        }

        _outlineRenderPass.scriptableRenderer = renderer;
        renderer.EnqueuePass(_outlineRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        _outlineRenderPass?.Cleanup();
        base.Dispose(disposing);
    }

    #region 公共API

    private bool CheckId(List<Renderer> renderers, out OutlineData outlineData)
    {
        outlineData = null;

        if (_outlineDatas == null || _outlineDatas.Count == 0) return false;
        if (renderers == null || renderers.Count == 0) return false;

        // 找到第一个“仍然有效”的 renderer
        Renderer firstValid = null;
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
            {
                firstValid = renderers[i];
                break;
            }
        }

        if (firstValid == null) return false;

        // 用 renderer 自己的 instanceId（不要访问 transform，避免 destroyed 报错）
        int targetId = firstValid.GetInstanceID();

        for (int i = 0; i < _outlineDatas.Count; i++)
        {
            var data = _outlineDatas[i];
            if (data != null && targetId == data.Id)
            {
                outlineData = data;
                return true;
            }
        }

        return false;
    }

    public bool UpdateOutlines(List<Renderer> renderers, Color color, float outlineSize)
    {
        if (renderers.Count <= 0)
            return false;

        if (CheckId(renderers, out var outlineData))
        {
            outlineData.UpdateOutline(color, outlineSize);
            return true;
        }
        return false;
    }

    public void DrawOrUpdateOutlines(List<Renderer> renderers, Color color, float outlineSize)
    {
        if (renderers.Count <= 0)
            return;

        if (UpdateOutlines(renderers, color, outlineSize))
            return;

        var id = renderers[0].GetInstanceID();
        var data = new OutlineData(id, renderers, color, outlineSize);
        _outlineDatas.Add(data);
        _isReady = true;
    }

    public void RemoveDrawOutlines(List<Renderer> renderers)
    {
        if (renderers == null || renderers.Count == 0)
            return;

        // 全是 destroyed/null 就直接忽略
        bool anyValid = false;
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null) { anyValid = true; break; }
        }
        if (!anyValid)
            return;

        if (CheckId(renderers, out var outlineData))
        {
            _outlineDatas.Remove(outlineData);
        }
        _isReady = _outlineDatas.Count > 0;
    }

    public void StopDrawOutline()
    {
        _outlineDatas.Clear();
        _isReady = false;
    }

    #endregion
}

/// <summary>
/// 简化版渲染Pass - 保留视锥剔除、距离LOD、RT降采样
/// 每个OutlineData独立渲染（不合批）
/// </summary>
public class SimpleOutlineRenderPass : ScriptableRenderPass
{
    private static readonly ProfilingSampler _profiling = new ProfilingSampler("Simple Outline Pass");
    public ScriptableRenderer scriptableRenderer = null;

    // 每个对象独立的绘制器
    private Dictionary<int, OutlineDrawer> _drawers = new Dictionary<int, OutlineDrawer>();
    private List<int> _toRemove = new List<int>();

    // 视锥剔除
    private Plane[] _frustumPlanes = new Plane[6];
    private Camera _currentCamera;
    private Vector3 _cameraPosition;

    private bool IsReady(RenderingData renderingData)
    {
        if (!Application.isPlaying)
            return false;

        var feature = OutlineRenderFeature.Instance;
        if (feature == null || feature.OutlineShader == null || feature.BlurShader == null)
            return false;

        var cameraData = renderingData.cameraData;
        if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera)
            return false;

        return true;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (!IsReady(renderingData))
            return;

        _currentCamera = renderingData.cameraData.camera;
        _cameraPosition = _currentCamera.transform.position;

        var feature = OutlineRenderFeature.Instance;

        // 计算视锥平面
        if (feature.EnableFrustumCulling)
        {
            GeometryUtility.CalculateFrustumPlanes(_currentCamera, _frustumPlanes);
        }

        // 同步绘制器
        SyncDrawers(renderingData);
    }

    private void SyncDrawers(RenderingData renderingData)
    {
        var feature = OutlineRenderFeature.Instance;
        var datas = feature.OutlineDatas;

        // 标记需要移除的
        _toRemove.Clear();
        foreach (var kvp in _drawers)
        {
            bool found = false;
            foreach (var data in datas)
            {
                if (kvp.Key == data.Id)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                kvp.Value.Clear();
                _toRemove.Add(kvp.Key);
            }
        }

        foreach (var id in _toRemove)
        {
            _drawers.Remove(id);
        }

        // 创建或更新绘制器
        foreach (var data in datas)
        {
            if (!_drawers.TryGetValue(data.Id, out var drawer))
            {
                drawer = new OutlineDrawer(data.Id, data.Renderers, data.OutlineColor, data.OutlineSize);
                _drawers[data.Id] = drawer;
            }
            else
            {
                drawer.UpdateParameters(data.OutlineColor, data.OutlineSize);
            }

            // 初始化RT（带降采样）
            drawer.InitRT(renderingData, feature.RTDownsample);
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!IsReady(renderingData))
            return;

        var feature = OutlineRenderFeature.Instance;
        if (!feature.IsReady || _drawers.Count == 0)
            return;

        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, _profiling))
        {
            var cameraColorTarget = scriptableRenderer.cameraColorTargetHandle;

            foreach (var drawer in _drawers.Values)
            {
                // 视锥剔除和距离LOD检查
                if (!ShouldRender(drawer, feature))
                    continue;

                drawer.OnDrawOutline(cmd, renderingData, cameraColorTarget, feature);
            }
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    /// <summary>
    /// 检查是否应该渲染（视锥剔除 + 距离LOD）
    /// </summary>
    private bool ShouldRender(OutlineDrawer drawer, OutlineRenderFeature feature)
    {
        var renderers = drawer.Renderers;
        if (renderers == null || renderers.Count == 0)
            return false;

        // 检查是否有任何渲染器可见
        bool anyVisible = false;

        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            // 视锥剔除
            if (feature.EnableFrustumCulling)
            {
                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, renderer.bounds))
                    continue;
            }

            // 距离LOD
            if (feature.EnableDistanceLOD)
            {
                float distance = Vector3.Distance(_cameraPosition, renderer.bounds.center);
                if (distance > feature.MaxOutlineDistance)
                    continue;
            }

            anyVisible = true;
            break;
        }

        return anyVisible;
    }

    public void Cleanup()
    {
        foreach (var drawer in _drawers.Values)
        {
            drawer.Clear();
        }
        _drawers.Clear();
    }
}

/// <summary>
/// 单个对象的描边绘制器（带RT降采样优化）
/// </summary>
public class OutlineDrawer
{
    private int _id;
    private RenderTexture _srcRt;
    private RenderTexture _outlineRt;

    private const int BLUR_COUNT = 3;
    private RenderTexture[] _blurRts = new RenderTexture[BLUR_COUNT];
    private Material[] _blurMats = new Material[BLUR_COUNT];
    private Material _outlineMat;

    private List<Renderer> _renderers;
    private Color _color;
    private float _outlineSize;

    public int Id => _id;
    public List<Renderer> Renderers => _renderers;

    public OutlineDrawer(int id, List<Renderer> renderers, Color color, float outlineSize)
    {
        _id = id;
        _renderers = renderers;
        _color = color;
        _outlineSize = outlineSize;
    }

    public void UpdateParameters(Color color, float outlineSize)
    {
        _color = color;
        _outlineSize = outlineSize;
    }

    /// <summary>
    /// 初始化RT（带降采样）
    /// </summary>
    public void InitRT(RenderingData renderingData, int downsample)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        int fullWidth = descriptor.width;
        int fullHeight = descriptor.height;

        // 模糊RT使用降采样
        int blurWidth = fullWidth / Mathf.Clamp(downsample, 1, 4);
        int blurHeight = fullHeight / Mathf.Clamp(downsample, 1, 4);

        // 源RT - 全分辨率
        InitRenderTexture(fullWidth, fullHeight, ref _srcRt, "_outlineSrcRt", GraphicsFormat.R8G8_UNorm);

        // 模糊RT - 降采样
        for (int i = 0; i < BLUR_COUNT; i++)
        {
            InitRenderTexture(blurWidth, blurHeight, ref _blurRts[i], $"_outlineBlurRt{i}", GraphicsFormat.R8G8_UNorm);
        }

        // 输出RT - 全分辨率
        InitRenderTexture(fullWidth, fullHeight, ref _outlineRt, "_outlineOutlineRt", GraphicsFormat.R8G8B8A8_UNorm);
    }

    private void InitRenderTexture(int width, int height, ref RenderTexture rt, string name, GraphicsFormat format)
    {
        if (rt == null || !rt.IsCreated() || rt.width != width || rt.height != height)
        {
            if (rt != null)
                RenderTexture.ReleaseTemporary(rt);

            var desc = new RenderTextureDescriptor(width, height)
            {
                useMipMap = false,
                autoGenerateMips = false,
                depthBufferBits = 0,
                graphicsFormat = format,
                msaaSamples = 1,
                dimension = TextureDimension.Tex2D
            };

            rt = RenderTexture.GetTemporary(desc);
            rt.filterMode = FilterMode.Bilinear;
            rt.name = name;
        }
    }

    public void OnDrawOutline(CommandBuffer cmd, RenderingData renderingData, RTHandle cameraColorTarget, OutlineRenderFeature feature)
    {
        if (_renderers == null || _renderers.Count == 0)
            return;

        // Step 1: 绘制轮廓
        cmd.SetRenderTarget(_srcRt,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.DontCare);
        cmd.ClearRenderTarget(true, true, Color.clear);

        if (_outlineMat == null)
            _outlineMat = new Material(feature.OutlineShader);

        _outlineMat.SetFloat("_Clip", feature.ClipSize);

        foreach (var renderer in _renderers)
        {
            if (renderer != null && renderer.enabled)
                cmd.DrawRenderer(renderer, _outlineMat, 0, 0);
        }

        // Step 2: 模糊
        float blurStep = _outlineSize / BLUR_COUNT;
        for (int i = 0; i < BLUR_COUNT; i++)
        {
            if (_blurMats[i] == null)
                _blurMats[i] = new Material(feature.BlurShader);

            _blurMats[i].SetFloat("_BlurSize", blurStep * (i + 1));

            RenderTexture srcRt = (i == 0) ? _srcRt : _blurRts[i - 1];
            _blurMats[i].SetTexture("_MainTex", srcRt);
            cmd.Blit(srcRt, _blurRts[i], _blurMats[i]);
        }

        // Step 3: 生成轮廓
        _outlineMat.SetTexture("_BaseMap", _srcRt);
        _outlineMat.SetTexture("_BlurTex", _blurRts[BLUR_COUNT - 1]);
        cmd.Blit(_blurRts[BLUR_COUNT - 1], _outlineRt, _outlineMat, 1);

        // Step 4: 合成
        _outlineMat.SetColor("_OutlineColor", _color);
        _outlineMat.SetFloat("_Intensity", feature.Intensity);
        _outlineMat.SetFloat("_ExpPower", feature.ExpPower);
        _outlineMat.SetTexture("_OutlineTex", _outlineRt);
        cmd.Blit(_outlineRt, cameraColorTarget.nameID, _outlineMat, 2);
    }

    public void Clear()
    {
        if (_srcRt != null)
        {
            RenderTexture.ReleaseTemporary(_srcRt);
            _srcRt = null;
        }

        if (_outlineRt != null)
        {
            RenderTexture.ReleaseTemporary(_outlineRt);
            _outlineRt = null;
        }

        for (int i = 0; i < BLUR_COUNT; i++)
        {
            if (_blurRts[i] != null)
            {
                RenderTexture.ReleaseTemporary(_blurRts[i]);
                _blurRts[i] = null;
            }
        }

        if (_outlineMat != null)
        {
            Object.DestroyImmediate(_outlineMat);
            _outlineMat = null;
        }

        for (int i = 0; i < BLUR_COUNT; i++)
        {
            if (_blurMats[i] != null)
            {
                Object.DestroyImmediate(_blurMats[i]);
                _blurMats[i] = null;
            }
        }
    }
}
