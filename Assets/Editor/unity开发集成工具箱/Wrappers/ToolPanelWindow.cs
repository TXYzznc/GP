#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// 工具面板独立窗口包装器 - 让工具面板可以作为独立窗口使用
/// </summary>
public class ToolPanelWindow<T> : EditorWindow where T : class, IToolHubPanel, new()
{
    private T panel;
    private Vector2 scrollPosition;

    protected virtual void OnEnable()
    {
        panel = new T();
        panel.OnEnable();
    }

    protected virtual void OnDisable()
    {
        panel?.OnDisable();
    }

    protected virtual void OnDestroy()
    {
        panel?.OnDestroy();
    }

    protected virtual void OnGUI()
    {
        if (panel == null)
        {
            panel = new T();
            panel.OnEnable();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        panel.OnGUI();
        EditorGUILayout.EndScrollView();
    }
}

/// <summary>
/// 噪声生成器独立窗口
/// </summary>
public class NoiseGeneratorWindow : ToolPanelWindow<NoiseGeneratorPanel>
{
    [MenuItem("工具/纹理工具/FastNoise 噪声生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<NoiseGeneratorWindow>("FastNoise 噪声生成器");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }
}

/// <summary>
/// 平滑法线生成器独立窗口
/// </summary>
public class SmoothNormalWindow : ToolPanelWindow<NormalProcessingPanel>
{
    [MenuItem("工具/模型工具/法线处理工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<SmoothNormalWindow>("法线处理工具");
        window.minSize = new Vector2(400, 450);
        window.Show();
    }
}

/// <summary>
/// 批量对象生成器独立窗口
/// </summary>
public class ObjectSpawnerWindow : ToolPanelWindow<ObjectSpawnerPanel>
{
    [MenuItem("工具/场景工具/批量对象生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<ObjectSpawnerWindow>("批量对象生成器");
        window.minSize = new Vector2(400, 550);
        window.Show();
    }
}

/// <summary>
/// Fantastic City Generator 独立窗口
/// </summary>
public class CityGeneratorWindow : ToolPanelWindow<PrefabCityGeneratorPanel>
{
    [MenuItem("工具/场景工具/城市生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<CityGeneratorWindow>("城市生成器");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
}

/// <summary>
/// 玩家技能生成器独立窗口
/// </summary>
public class PlayerSkillGeneratorWindowProxy : ToolPanelWindow<PlayerSkillGeneratorPanel>
{
    [MenuItem("工具/通用生成/自定义脚本生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerSkillGeneratorWindowProxy>("技能脚本生成器");
        window.minSize = new Vector2(640, 400);
        window.Show();
    }
}

/// <summary>
/// 场景文本翻译（百度）独立窗口
/// </summary>
public class SceneTextTranslatorWindow : ToolPanelWindow<SceneTextTranslatorPanel>
{
    [MenuItem("工具/本地化工具/场景文本翻译(Baidu)")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneTextTranslatorWindow>("场景文本翻译(Baidu)");
        window.minSize = new Vector2(520, 420);
        window.Show();
    }
}

/// <summary>
/// 场景引用查找器 独立窗口
/// </summary>
public class SceneReferenceFinderWindowProxy : ToolPanelWindow<SceneReferenceFinderPanel>
{
    [MenuItem("工具/场景工具/场景引用查找器")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneReferenceFinderWindowProxy>("场景引用查找器");
        window.minSize = new Vector2(520, 420);
        window.Show();
    }
}

/// <summary>
/// 编辑器截图工具 独立窗口（使用 Panel）
/// </summary>
public class EditorScreenshotToolWindowProxy : ToolPanelWindow<EditorScreenshotPanel>
{
    [MenuItem("工具/资源工具/Editor Screenshot Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<EditorScreenshotToolWindowProxy>("Screenshot Tool");
        window.minSize = new Vector2(360, 260);
        window.Show();
    }
}

/// <summary>
/// 缺失脚本GUID扫描 独立窗口（使用 Panel）
/// </summary>
public class FindMissingScriptWindowProxy : ToolPanelWindow<FindMissingScriptPanel>
{
    [MenuItem("工具/诊断工具/缺失脚本GUID扫描")]
    public static void ShowWindow()
    {
        var window = GetWindow<FindMissingScriptWindowProxy>("Missing Script GUID Scanner");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }
}

/// <summary>
/// 项目健康检查 独立窗口（使用 Panel）
/// </summary>
public class ProjectHealthCheckWindowProxy : ToolPanelWindow<ProjectHealthCheckPanel>
{
    [MenuItem("工具/诊断工具/打开体检窗口")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProjectHealthCheckWindowProxy>("Project Health Check");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }
}

/// <summary>
/// 预制体图标批量生成器 独立窗口（使用 Panel）
/// </summary>
public class PrefabIconBatchGeneratorWindowProxy : ToolPanelWindow<PrefabIconBatchGeneratorPanel>
{
    [MenuItem("工具/资源工具/预制体图标生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabIconBatchGeneratorWindowProxy>("Prefab Icon Generator");
        window.minSize = new Vector2(420, 420);
        window.Show();
    }
}

/// <summary>
/// Prefab World Builder 控制台独立窗口
/// </summary>
public class PWBControlWindow : ToolPanelWindow<PWBControlPanel>
{
    [MenuItem("工具/场景工具/PWB 控制台")]
    public static void ShowWindow()
    {
        var window = GetWindow<PWBControlWindow>("PWB 控制台");
        window.minSize = new Vector2(300, 450);
        window.Show();
    }
}

#endif
