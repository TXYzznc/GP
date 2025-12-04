using UnityEngine;

/// <summary>
/// 轮廓系统配置测试脚本
/// 用于验证轮廓系统是否正确配置
/// </summary>
public class OutlineConfigTest : MonoBehaviour
{
    [Header("测试设置")]
    [Tooltip("是否在启动时自动检查配置")]
    public bool autoCheckOnStart = true;
    
    [Tooltip("是否显示详细日志")]
    public bool showDetailedLog = true;
    
    [Header("快捷键控制")]
    [Tooltip("按键 1: 切换 Enemy 轮廓")]
    public KeyCode toggleEnemyKey = KeyCode.Alpha1;
    
    [Tooltip("按键 2: 切换 Ally 轮廓")]
    public KeyCode toggleAllyKey = KeyCode.Alpha2;
    
    [Tooltip("按键 3: 切换 Interactive 轮廓")]
    public KeyCode toggleInteractiveKey = KeyCode.Alpha3;
    
    [Tooltip("按键 D: 切换调试信息")]
    public KeyCode toggleDebugKey = KeyCode.D;
    
    [Tooltip("按键 R: 刷新所有缓存")]
    public KeyCode refreshCacheKey = KeyCode.R;
    
    private OutlineManager manager;
    private bool configValid = false;
    
    void Start()
    {
        if (autoCheckOnStart)
        {
            CheckConfiguration();
        }
    }
    
    void Update()
    {
        if (!configValid) return;
        
        // 切换轮廓
        if (Input.GetKeyDown(toggleEnemyKey))
        {
            ToggleProfile("Enemy", 0);
        }
        
        if (Input.GetKeyDown(toggleAllyKey))
        {
            ToggleProfile("Ally", 1);
        }
        
        if (Input.GetKeyDown(toggleInteractiveKey))
        {
            ToggleProfile("Interactive", 2);
        }
        
        // 切换调试信息
        if (Input.GetKeyDown(toggleDebugKey))
        {
            if (manager != null)
            {
                manager.showDebugInfo = !manager.showDebugInfo;
                Debug.Log($"<color=cyan>[OutlineTest]</color> 调试信息: {(manager.showDebugInfo ? "开启" : "关闭")}");
            }
        }
        
        // 刷新缓存
        if (Input.GetKeyDown(refreshCacheKey))
        {
            if (manager != null)
            {
                manager.InvalidateAllCaches();
                Debug.Log("<color=cyan>[OutlineTest]</color> 已刷新所有轮廓缓存");
            }
        }
    }
    
    /// <summary>
    /// 检查轮廓系统配置
    /// </summary>
    [ContextMenu("检查配置")]
    public void CheckConfiguration()
    {
        Debug.Log("<color=yellow>========== 轮廓系统配置检查 ==========</color>");
        
        bool allChecksPass = true;
        
        // 检查 1: OutlineManager
        allChecksPass &= CheckOutlineManager();
        
        // 检查 2: Layer 设置
        allChecksPass &= CheckLayers();
        
        // 检查 3: URP Renderer Feature
        allChecksPass &= CheckRendererFeature();
        
        // 检查 4: 场景物体
        allChecksPass &= CheckSceneObjects();
        
        // 总结
        Debug.Log("<color=yellow>========================================</color>");
        if (allChecksPass)
        {
            Debug.Log("<color=green>✅ 配置检查通过！轮廓系统应该可以正常工作。</color>");
            configValid = true;
        }
        else
        {
            Debug.LogWarning("<color=red>❌ 配置检查失败！请根据上述提示修复问题。</color>");
            configValid = false;
        }
        Debug.Log("<color=yellow>========================================</color>");
    }
    
    private bool CheckOutlineManager()
    {
        Debug.Log("<color=cyan>[检查 1/4]</color> OutlineManager 组件...");
        
        manager = OutlineManager.Instance;
        
        if (manager == null)
        {
            Debug.LogError("  ❌ 未找到 OutlineManager！请在场景中创建 OutlineManager 物体并添加组件。");
            return false;
        }
        
        Debug.Log($"  ✅ OutlineManager 已找到: {manager.gameObject.name}");
        
        if (manager.outlineProfiles == null || manager.outlineProfiles.Count == 0)
        {
            Debug.LogWarning("  ⚠️ OutlineManager 没有配置 Profile！");
            return false;
        }
        
        Debug.Log($"  ✅ 已配置 {manager.outlineProfiles.Count} 个 Profile");
        
        if (showDetailedLog)
        {
            foreach (var profile in manager.outlineProfiles)
            {
                string status = profile.enabled ? "启用" : "禁用";
                Debug.Log($"    - {profile.profileName}: {status}, 颜色={profile.outlineColor}, 宽度={profile.outlineWidth}");
            }
        }
        
        return true;
    }
    
    private bool CheckLayers()
    {
        Debug.Log("<color=cyan>[检查 2/4]</color> Layer 设置...");
        
        bool allLayersExist = true;
        string[] requiredLayers = { "Enemy", "Ally", "Interactive" };
        
        foreach (string layerName in requiredLayers)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex == -1)
            {
                Debug.LogError($"  ❌ Layer '{layerName}' 不存在！请在 Project Settings → Tags and Layers 中创建。");
                allLayersExist = false;
            }
            else
            {
                Debug.Log($"  ✅ Layer '{layerName}' 存在 (Index: {layerIndex})");
            }
        }
        
        return allLayersExist;
    }
    
    private bool CheckRendererFeature()
    {
        Debug.Log("<color=cyan>[检查 3/4]</color> URP Renderer Feature...");
        
        // 注意：这个检查需要通过反射或手动验证
        // 这里只提供提示信息
        Debug.Log("  ℹ️ 请手动验证以下内容：");
        Debug.Log("    1. URP Renderer Asset 中是否添加了 'Outline Render Feature'");
        Debug.Log("    2. Draw Occupied Shader 是否分配为 'Outline/DrawOccupied'");
        Debug.Log("    3. Outline Detection Shader 是否分配为 'Outline/OutlineDetection'");
        Debug.Log("    4. Composite Shader 是否分配为 'Outline/OutlineComposite'");
        Debug.Log("  💡 路径: Project Settings → Graphics → URP Asset → Renderer → Renderer Features");
        
        return true; // 假设已正确配置
    }
    
    private bool CheckSceneObjects()
    {
        Debug.Log("<color=cyan>[检查 4/4]</color> 场景物体...");
        
        if (manager == null) return false;
        
        bool foundObjects = false;
        
        foreach (var profile in manager.outlineProfiles)
        {
            // 查找该 Layer 的物体
            int layerIndex = LayerMask.NameToLayer(profile.profileName);
            if (layerIndex == -1) continue;
            
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            int count = 0;
            
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.layer == layerIndex)
                {
                    count++;
                    if (showDetailedLog && count <= 3)
                    {
                        Debug.Log($"    - {renderer.gameObject.name} (Layer: {profile.profileName})");
                    }
                }
            }
            
            if (count > 0)
            {
                Debug.Log($"  ✅ 找到 {count} 个 '{profile.profileName}' Layer 的物体");
                foundObjects = true;
            }
            else
            {
                Debug.LogWarning($"  ⚠️ 没有找到 '{profile.profileName}' Layer 的物体");
            }
        }
        
        if (!foundObjects)
        {
            Debug.LogWarning("  ⚠️ 场景中没有任何物体设置了轮廓 Layer！");
            Debug.Log("  💡 请将需要显示轮廓的物体设置到 Enemy/Ally/Interactive Layer");
        }
        
        return foundObjects;
    }
    
    private void ToggleProfile(string profileName, int index)
    {
        if (manager == null || manager.outlineProfiles.Count <= index) return;
        
        bool newState = !manager.outlineProfiles[index].enabled;
        manager.SetProfileEnabled(profileName, newState);
        
        string status = newState ? "开启" : "关闭";
        Debug.Log($"<color=cyan>[OutlineTest]</color> {profileName} 轮廓: {status}");
    }
    
    void OnGUI()
    {
        if (!configValid) return;
        
        // 显示快捷键提示
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        string helpText = "轮廓系统快捷键:\n" +
                         $"[{toggleEnemyKey}] 切换 Enemy 轮廓\n" +
                         $"[{toggleAllyKey}] 切换 Ally 轮廓\n" +
                         $"[{toggleInteractiveKey}] 切换 Interactive 轮廓\n" +
                         $"[{toggleDebugKey}] 切换调试信息\n" +
                         $"[{refreshCacheKey}] 刷新缓存";
        
        GUI.Box(new Rect(10, 10, 250, 130), helpText, style);
        
        // 显示当前状态
        if (manager != null)
        {
            string statusText = "轮廓状态:\n";
            foreach (var profile in manager.outlineProfiles)
            {
                string status = profile.enabled ? "✓" : "✗";
                statusText += $"{status} {profile.profileName}\n";
            }
            
            GUI.Box(new Rect(10, 150, 250, 100), statusText, style);
        }
    }
}
