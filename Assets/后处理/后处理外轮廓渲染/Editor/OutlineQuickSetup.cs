using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// 轮廓系统快速设置工具
/// </summary>
public class OutlineQuickSetup : EditorWindow
{
    [MenuItem("Tools/轮廓系统/快速设置场景")]
    public static void QuickSetup()
    {
        // 检查是否已有 OutlineManager
        OutlineManager existingManager = Object.FindObjectOfType<OutlineManager>();
        
        if (existingManager != null)
        {
            bool recreate = EditorUtility.DisplayDialog(
                "已存在 OutlineManager",
                $"场景中已经存在 OutlineManager（{existingManager.gameObject.name}）。\n\n是否要重新配置？",
                "重新配置",
                "取消");
            
            if (!recreate) return;
            
            Selection.activeGameObject = existingManager.gameObject;
            EditorGUIUtility.PingObject(existingManager.gameObject);
            return;
        }
        
        // 创建 OutlineManager
        GameObject managerObj = new GameObject("OutlineManager");
        OutlineManager manager = managerObj.AddComponent<OutlineManager>();
        
        // 添加测试脚本
        managerObj.AddComponent<OutlineConfigTest>();
        
        // 选中并高亮
        Selection.activeGameObject = managerObj;
        EditorGUIUtility.PingObject(managerObj);
        
        // 标记场景为已修改
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        Debug.Log("<color=green>✓</color> OutlineManager 已创建！请运行游戏测试轮廓效果。");
        
        EditorUtility.DisplayDialog(
            "设置完成",
            "OutlineManager 已成功创建！\n\n" +
            "下一步：\n" +
            "1. 将需要轮廓的物体设置到 Enemy/Ally/Interactive Layer\n" +
            "2. 点击 Play 按钮测试\n" +
            "3. 使用快捷键 1/2/3 切换轮廓",
            "确定");
    }
    
    [MenuItem("Tools/轮廓系统/检查场景配置")]
    public static void CheckSceneSetup()
    {
        string report = "=== 轮廓系统场景配置检查 ===\n\n";
        bool allGood = true;
        
        // 检查 OutlineManager
        OutlineManager manager = Object.FindObjectOfType<OutlineManager>();
        if (manager != null)
        {
            report += "✓ OutlineManager: 已找到\n";
            report += $"  - 位置: {manager.gameObject.name}\n";
            report += $"  - Profile 数量: {manager.outlineProfiles.Count}\n";
        }
        else
        {
            report += "✗ OutlineManager: 未找到\n";
            report += "  → 请使用 'Tools → 轮廓系统 → 快速设置场景' 创建\n";
            allGood = false;
        }
        
        report += "\n";
        
        // 检查 Layer
        string[] requiredLayers = { "Enemy", "Ally", "Interactive" };
        foreach (string layerName in requiredLayers)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex != -1)
            {
                report += $"✓ Layer '{layerName}': 已创建 (Index: {layerIndex})\n";
            }
            else
            {
                report += $"✗ Layer '{layerName}': 未创建\n";
                allGood = false;
            }
        }
        
        report += "\n";
        
        // 检查场景物体
        if (manager != null)
        {
            foreach (var profile in manager.outlineProfiles)
            {
                int layerIndex = LayerMask.NameToLayer(profile.profileName);
                if (layerIndex == -1) continue;
                
                Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
                int count = 0;
                
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.layer == layerIndex)
                    {
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    report += $"✓ '{profile.profileName}' Layer: 找到 {count} 个物体\n";
                }
                else
                {
                    report += $"⚠ '{profile.profileName}' Layer: 没有物体\n";
                }
            }
        }
        
        report += "\n";
        
        if (allGood)
        {
            report += "=== 配置检查通过！===\n";
            report += "可以运行游戏测试轮廓效果。";
        }
        else
        {
            report += "=== 发现配置问题 ===\n";
            report += "请根据上述提示修复问题。";
        }
        
        Debug.Log(report);
        
        EditorUtility.DisplayDialog(
            "场景配置检查",
            report,
            "确定");
    }
    
    [MenuItem("Tools/轮廓系统/创建测试物体")]
    public static void CreateTestObjects()
    {
        // 检查 Layer 是否存在
        if (LayerMask.NameToLayer("Enemy") == -1 ||
            LayerMask.NameToLayer("Ally") == -1 ||
            LayerMask.NameToLayer("Interactive") == -1)
        {
            EditorUtility.DisplayDialog(
                "错误",
                "请先创建 Enemy、Ally、Interactive Layer！\n\n" +
                "路径: Edit → Project Settings → Tags and Layers",
                "确定");
            return;
        }
        
        // 创建测试物体
        GameObject testParent = new GameObject("OutlineTestObjects");
        
        // Enemy 测试物体
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "TestEnemy";
        enemy.transform.parent = testParent.transform;
        enemy.transform.position = new Vector3(-2, 0, 0);
        enemy.layer = LayerMask.NameToLayer("Enemy");
        
        // Ally 测试物体
        GameObject ally = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ally.name = "TestAlly";
        ally.transform.parent = testParent.transform;
        ally.transform.position = new Vector3(0, 0, 0);
        ally.layer = LayerMask.NameToLayer("Ally");
        
        // Interactive 测试物体
        GameObject interactive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        interactive.name = "TestInteractive";
        interactive.transform.parent = testParent.transform;
        interactive.transform.position = new Vector3(2, 0, 0);
        interactive.layer = LayerMask.NameToLayer("Interactive");
        
        // 选中父物体
        Selection.activeGameObject = testParent;
        EditorGUIUtility.PingObject(testParent);
        
        // 标记场景为已修改
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        Debug.Log("<color=green>✓</color> 测试物体已创建！");
        
        EditorUtility.DisplayDialog(
            "测试物体已创建",
            "已创建 3 个测试物体：\n" +
            "- TestEnemy (红色轮廓)\n" +
            "- TestAlly (绿色轮廓)\n" +
            "- TestInteractive (黄色轮廓)\n\n" +
            "点击 Play 按钮查看效果！",
            "确定");
    }
}
