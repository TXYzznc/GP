using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OutlineManager : MonoBehaviour
{
    [Header("轮廓配置")]
    public List<OutlineProfile> outlineProfiles = new List<OutlineProfile>();

    [Header("性能设置")]
    [Range(0.25f, 1f)]
    public float resolutionScale = 1f;
    
    [Range(1, 10)]
    public int maxProfilesPerFrame = 3;
    
    public bool useDeferredRendering = false;

    [Header("调试")]
    public bool showDebugInfo = false;

    private Dictionary<string, List<Renderer>> cachedRenderers = new Dictionary<string, List<Renderer>>();
    private float lastUpdateTime = 0f;
    private int currentProfileIndex = 0;

    private static OutlineManager instance;
    public static OutlineManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OutlineManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        InitializeDefaultProfiles();
    }

    private void InitializeDefaultProfiles()
    {
        if (outlineProfiles.Count == 0)
        {
            outlineProfiles.Add(new OutlineProfile
            {
                profileName = "Enemy",
                targetLayer = LayerMask.GetMask("Enemy"),
                outlineColor = new Color(1f, 0f, 0f, 0.8f),
                outlineWidth = 4,
                iterations = 3,
                maxDistance = 50f,
                useDistanceCulling = true,
                useTextureCache = true,
                cacheRefreshRate = 0.1f,
                enabled = true
            });

            outlineProfiles.Add(new OutlineProfile
            {
                profileName = "Ally",
                targetLayer = LayerMask.GetMask("Ally"),
                outlineColor = new Color(0f, 1f, 0f, 0.8f),
                outlineWidth = 4,
                iterations = 3,
                maxDistance = 50f,
                useDistanceCulling = true,
                useTextureCache = true,
                cacheRefreshRate = 0.1f,
                enabled = true
            });

            outlineProfiles.Add(new OutlineProfile
            {
                profileName = "Interactive",
                targetLayer = LayerMask.GetMask("Interactive"),
                outlineColor = new Color(1f, 1f, 0f, 0.8f),
                outlineWidth = 3,
                iterations = 2,
                maxDistance = 30f,
                useDistanceCulling = true,
                useTextureCache = true,
                cacheRefreshRate = 0.15f,
                enabled = true
            });
        }
    }

    public List<Renderer> GetVisibleRenderers(OutlineProfile profile, Camera camera)
    {
        if (!cachedRenderers.ContainsKey(profile.profileName) || 
            Time.time - lastUpdateTime > 1f)
        {
            UpdateRendererCache(profile);
        }

        if (!cachedRenderers.ContainsKey(profile.profileName))
        {
            return new List<Renderer>();
        }

        List<Renderer> visibleRenderers = new List<Renderer>();
        Vector3 cameraPos = camera.transform.position;

        foreach (var renderer in cachedRenderers[profile.profileName])
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            // 距离剔除
            if (profile.useDistanceCulling)
            {
                float distance = Vector3.Distance(cameraPos, renderer.bounds.center);
                if (distance > profile.maxDistance)
                    continue;
            }

            // 视锥剔除
            if (!GeometryUtility.TestPlanesAABB(
                GeometryUtility.CalculateFrustumPlanes(camera), 
                renderer.bounds))
            {
                continue;
            }

            visibleRenderers.Add(renderer);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[OutlineManager] Profile '{profile.profileName}' 可见物体: {visibleRenderers.Count}");
        }

        return visibleRenderers;
    }

    private void UpdateRendererCache(OutlineProfile profile)
    {
        List<Renderer> renderers = new List<Renderer>();
        
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in allRenderers)
        {
            if (((1 << renderer.gameObject.layer) & profile.targetLayer) != 0)
            {
                renderers.Add(renderer);
            }
        }

        cachedRenderers[profile.profileName] = renderers;
        lastUpdateTime = Time.time;

        if (showDebugInfo)
        {
            Debug.Log($"[OutlineManager] 缓存了 {renderers.Count} 个 '{profile.profileName}' 渲染器");
        }
    }

    public List<OutlineProfile> GetActiveProfiles()
    {
        if (useDeferredRendering)
        {
            return GetDeferredProfiles();
        }
        return outlineProfiles.Where(p => p.enabled).ToList();
    }

    private List<OutlineProfile> GetDeferredProfiles()
    {
        List<OutlineProfile> activeProfiles = new List<OutlineProfile>();
        int profilesAdded = 0;
        int totalProfiles = outlineProfiles.Count(p => p.enabled);

        if (totalProfiles == 0) return activeProfiles;

        for (int i = 0; i < maxProfilesPerFrame && profilesAdded < totalProfiles; i++)
        {
            currentProfileIndex = currentProfileIndex % outlineProfiles.Count;
            var profile = outlineProfiles[currentProfileIndex];
            
            if (profile.enabled)
            {
                activeProfiles.Add(profile);
                profilesAdded++;
            }
            
            currentProfileIndex++;
        }

        return activeProfiles;
    }

    #region 公共API

    public void InvalidateCache(string profileName)
    {
        var profile = outlineProfiles.Find(p => p.profileName == profileName);
        if (profile != null)
        {
            profile.needsUpdate = true;
        }
    }

    public void InvalidateAllCaches()
    {
        foreach (var profile in outlineProfiles)
        {
            profile.needsUpdate = true;
        }
    }

    public void SetProfileEnabled(string profileName, bool enabled)
    {
        var profile = outlineProfiles.Find(p => p.profileName == profileName);
        if (profile != null)
        {
            profile.enabled = enabled;
        }
    }

    public void SetProfileColor(string profileName, Color color)
    {
        var profile = outlineProfiles.Find(p => p.profileName == profileName);
        if (profile != null)
        {
            profile.outlineColor = color;
            profile.needsUpdate = true;
        }
    }

    #endregion

    private void OnDestroy()
    {
        foreach (var profile in outlineProfiles)
        {
            if (profile.cachedTexture != null)
            {
                profile.cachedTexture.Release();
                profile.cachedTexture = null;
            }
        }
        cachedRenderers.Clear();
    }
}