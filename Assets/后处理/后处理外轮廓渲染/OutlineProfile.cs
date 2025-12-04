using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class OutlineProfile
{
    public string profileName = "Outline Profile";
    public LayerMask targetLayer;
    public Color outlineColor = Color.green;
    
    [Header("轮廓参数")]
    [Range(1, 10)]
    public int outlineWidth = 4;
    [Range(1, 10)]
    public int iterations = 3;
    
    [Header("距离剔除")]
    public bool useDistanceCulling = true;
    public float maxDistance = 50f;
    
    [Header("性能优化")]
    public bool useTextureCache = true;
    public float cacheRefreshRate = 0.1f;
    
    [Header("运行时状态")]
    public bool enabled = true;
    
    [HideInInspector]
    public float lastCacheTime = 0f;
    
    // 根据使用场景选择合适的类型
    // 如果用于传统后处理（OnRenderImage），使用 RenderTexture
    [HideInInspector]
    public RenderTexture cachedTexture;
    
    // 如果用于 URP ScriptableRenderPass，使用 RTHandle
    [HideInInspector]
    public RTHandle cachedTextureHandle;
    
    [HideInInspector]
    public bool needsUpdate = true;
}
