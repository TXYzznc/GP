using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity开发集成工具箱 - 主窗口
/// </summary>
public class ToolHubWindow : EditorWindow
{
    private const string SettingsAssetPath = "Assets/Editor/ToolHubSettings.asset";

    private ToolHubSettings settings;
    private readonly Dictionary<string, IToolHubPanel> panelCache = new();
    private IToolHubPanel currentPanel;
    private string currentTypeName;
    private Vector2 scrollPosition;
    
    // 样式
    private GUIStyle headerStyle;
    private GUIStyle tabStyle;
    private GUIStyle selectedTabStyle;
    private bool stylesInitialized;

    [MenuItem("工具/Unity开发工具箱 %#T")] // Ctrl+Shift+T
    public static void Open()
    {
        var win = GetWindow<ToolHubWindow>("unity开发工具箱");
        win.minSize = new Vector2(600, 400);
        win.Show();
    }

    private void OnEnable()
    {
        settings = LoadOrCreateSettings();
        stylesInitialized = false;
        
        // 恢复当前面板
        if (settings.tools.Count > 0 && settings.selectedIndex >= 0 && settings.selectedIndex < settings.tools.Count)
        {
            var entry = settings.tools[settings.selectedIndex];
            SwitchToPanel(entry.typeName);
        }
    }

    private void OnDisable()
    {
        currentPanel?.OnDisable();
    }

    private void OnDestroy()
    {
        foreach (var panel in panelCache.Values)
        {
            panel.OnDestroy();
        }
        panelCache.Clear();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        
        tabStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fixedHeight = 28,
            fontSize = 12
        };
        
        selectedTabStyle = new GUIStyle(tabStyle);
        selectedTabStyle.normal = selectedTabStyle.active;
        
        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();
        
        if (settings == null)
        {
            DrawErrorState();
            return;
        }

        DrawTopBar();
        
        if (settings.tools.Count == 0)
        {
            DrawEmptyState();
            return;
        }

        DrawTabs();
        DrawCurrentTool();
    }

    private void DrawErrorState()
    {
        EditorGUILayout.HelpBox("ToolHubSettings 配置丢失", MessageType.Error);
        if (GUILayout.Button("重新创建配置"))
            settings = LoadOrCreateSettings(true);
    }

    private void DrawEmptyState()
    {
        EditorGUILayout.Space(50);
        
        using (new EditorGUILayout.VerticalScope())
        {
            GUILayout.FlexibleSpace();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(400)))
                {
                    EditorGUILayout.LabelField("🧰 欢迎使用 Tool Hub", headerStyle);
                    EditorGUILayout.Space(20);
                    
                    EditorGUILayout.HelpBox(
                        "Tool Hub 是一个集成开发工具箱，可以将多个编辑器工具整合到一个窗口中。\n\n" +
                        "点击下方按钮添加工具，或使用顶部的 '+ 添加' 按钮。",
                        MessageType.Info
                    );
                    
                    EditorGUILayout.Space(20);
                    
                    if (GUILayout.Button("➕ 添加第一个工具", GUILayout.Height(40)))
                    {
                        ShowAddMenu();
                    }
                }
                
                GUILayout.FlexibleSpace();
            }
            
            GUILayout.FlexibleSpace();
        }
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            // 添加按钮
            using (new EditorGUI.DisabledScope(settings.locked))
            {
                if (GUILayout.Button("➕ 添加", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    ShowAddMenu();

                if (settings.tools.Count > 0 && GUILayout.Button("➖ 移除", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    RemoveCurrentTool();
            }

            GUILayout.FlexibleSpace();
            
            // 工具数量显示
            if (settings.tools.Count > 0)
            {
                GUILayout.Label($"工具: {settings.tools.Count}", EditorStyles.miniLabel);
                GUILayout.Space(10);
            }

            // 锁定按钮
            var lockIcon = settings.locked ? "解锁" : "锁定";
            if (GUILayout.Button(lockIcon, EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                settings.locked = !settings.locked;
                SaveSettings();
            }
            
            // 设置按钮
            if (GUILayout.Button("设置", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                ShowSettingsMenu();
            }
        }
    }

    private void DrawTabs()
    {
        if (settings.tools.Count == 0) return;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition, 
                GUIStyle.none, 
                GUIStyle.none, 
                GUILayout.Height(32)
            );
            
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < settings.tools.Count; i++)
                {
                    var entry = settings.tools[i];
                    var isSelected = i == settings.selectedIndex;
                    var displayName = string.IsNullOrWhiteSpace(entry.displayName) ? "(未命名)" : entry.displayName;
                    
                    var style = isSelected ? selectedTabStyle : tabStyle;
                    
                    if (GUILayout.Button(displayName, style, GUILayout.MinWidth(80)))
                    {
                        if (!settings.locked && i != settings.selectedIndex)
                        {
                            settings.selectedIndex = i;
                            SwitchToPanel(entry.typeName);
                            SaveSettings();
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        // 标签页重命名
        if (settings.selectedIndex >= 0 && settings.selectedIndex < settings.tools.Count)
        {
            var entry = settings.tools[settings.selectedIndex];
            
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(settings.locked))
                {
                    EditorGUILayout.LabelField("标签名:", GUILayout.Width(50));
                    EditorGUI.BeginChangeCheck();
                    var newName = EditorGUILayout.TextField(entry.displayName, GUILayout.Width(150));
                    if (EditorGUI.EndChangeCheck())
                    {
                        entry.displayName = newName;
                        SaveSettings();
                    }
                }
                
                GUILayout.FlexibleSpace();
                
                // 显示工具描述
                if (!string.IsNullOrEmpty(entry.description))
                {
                    EditorGUILayout.LabelField(entry.description, EditorStyles.miniLabel);
                }
            }
        }
        
        EditorGUILayout.Space(5);
    }

    private void DrawCurrentTool()
    {
        if (settings.tools.Count == 0) return;
        if (settings.selectedIndex < 0 || settings.selectedIndex >= settings.tools.Count) return;

        var entry = settings.tools[settings.selectedIndex];

        if (currentPanel == null || currentTypeName != entry.typeName)
        {
            SwitchToPanel(entry.typeName);
        }

        if (currentPanel == null)
        {
            EditorGUILayout.HelpBox($"无法加载工具:\n{entry.typeName}\n\n可能是类已被删除或重命名。", MessageType.Error);
            
            if (GUILayout.Button("移除此工具"))
            {
                RemoveCurrentTool();
            }
            return;
        }

        // 绘制工具界面
        try
        {
            currentPanel.OnGUI();
        }
        catch (Exception e)
        {
            EditorGUILayout.HelpBox($"工具绘制出错:\n{e.Message}", MessageType.Error);
            Debug.LogException(e);
        }
    }

    private void SwitchToPanel(string typeName)
    {
        // 禁用当前面板
        currentPanel?.OnDisable();
        
        // 获取或创建新面板
        currentPanel = GetOrCreatePanel(typeName);
        currentTypeName = typeName;
        
        // 启用新面板
        currentPanel?.OnEnable();
    }

    private void ShowAddMenu()
    {
        var menu = new GenericMenu();
        var types = FindAllToolPanelTypes().OrderBy(t => GetPriority(t)).ToList();

        if (types.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("没有可用的工具"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("如何创建工具..."), false, ShowHelpDialog);
        }
        else
        {
            foreach (var t in types)
            {
                var typeName = t.AssemblyQualifiedName;
                var menuName = GetMenuName(t);
                var description = GetDescription(t);

                bool alreadyAdded = settings.tools.Any(x => x.typeName == typeName);
                
                if (alreadyAdded)
                {
                    menu.AddDisabledItem(new GUIContent(menuName + " ✓"));
                }
                else
                {
                    menu.AddItem(new GUIContent(menuName), false, () => AddTool(t, typeName, description));
                }
            }
        }

        menu.ShowAsContext();
    }

    private void AddTool(Type t, string typeName, string description)
    {
        settings.tools.Add(new ToolHubSettings.ToolEntry
        {
            typeName = typeName,
            displayName = t.Name.Replace("Panel", "").Replace("Tool", ""),
            description = description
        });
        settings.selectedIndex = settings.tools.Count - 1;
        SwitchToPanel(typeName);
        SaveSettings();
    }

    private void RemoveCurrentTool()
    {
        if (settings.tools.Count == 0) return;

        int idx = Mathf.Clamp(settings.selectedIndex, 0, settings.tools.Count - 1);
        var entry = settings.tools[idx];

        if (!EditorUtility.DisplayDialog("移除工具",
                $"确定要从工具箱移除 '{entry.displayName}' 吗？\n\n（不会删除脚本文件）",
                "移除", "取消"))
            return;

        // 清理面板
        if (panelCache.TryGetValue(entry.typeName, out var panel))
        {
            panel.OnDisable();
            panel.OnDestroy();
            panelCache.Remove(entry.typeName);
        }

        if (currentTypeName == entry.typeName)
        {
            currentPanel = null;
            currentTypeName = null;
        }

        settings.tools.RemoveAt(idx);
        settings.selectedIndex = Mathf.Clamp(settings.selectedIndex, 0, Mathf.Max(0, settings.tools.Count - 1));
        
        // 切换到新的当前面板
        if (settings.tools.Count > 0)
        {
            SwitchToPanel(settings.tools[settings.selectedIndex].typeName);
        }
        
        SaveSettings();
    }

    private void ShowSettingsMenu()
    {
        var menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("刷新工具列表"), false, () => Repaint());
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("清空所有工具"), false, () =>
        {
            if (EditorUtility.DisplayDialog("清空确认", "确定要移除所有工具吗？", "确定", "取消"))
            {
                foreach (var panel in panelCache.Values)
                {
                    panel.OnDisable();
                    panel.OnDestroy();
                }
                panelCache.Clear();
                currentPanel = null;
                currentTypeName = null;
                settings.tools.Clear();
                settings.selectedIndex = 0;
                SaveSettings();
            }
        });
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("打开配置文件"), false, () =>
        {
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        });
        
        menu.ShowAsContext();
    }

    private void ShowHelpDialog()
    {
        EditorUtility.DisplayDialog("如何创建工具",
            "要创建可集成到 Tool Hub 的工具，需要：\n\n" +
            "1. 创建一个类实现 IToolHubPanel 接口\n" +
            "2. 添加 [ToolHubItem(\"菜单名称\")] 特性\n\n" +
            "示例：\n" +
            "[ToolHubItem(\"我的工具/示例\")]\n" +
            "public class MyToolPanel : IToolHubPanel\n" +
            "{\n" +
            "    public void OnEnable() { }\n" +
            "    public void OnDisable() { }\n" +
            "    public void OnGUI() { /* 绘制界面 */ }\n" +
            "    public void OnDestroy() { }\n" +
            "}",
            "知道了");
    }

    private IToolHubPanel GetOrCreatePanel(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        if (panelCache.TryGetValue(typeName, out var cached)) return cached;

        var type = Type.GetType(typeName);
        if (type == null) return null;
        if (!typeof(IToolHubPanel).IsAssignableFrom(type)) return null;

        try
        {
            var obj = Activator.CreateInstance(type) as IToolHubPanel;
            panelCache[typeName] = obj;
            return obj;
        }
        catch (Exception e)
        {
            Debug.LogError($"创建工具面板失败: {typeName}\n{e}");
            return null;
        }
    }

    private static IEnumerable<Type> FindAllToolPanelTypes()
    {
        return TypeCache.GetTypesDerivedFrom<IToolHubPanel>()
            .Where(t => !t.IsAbstract && !t.IsInterface && 
                   t.GetCustomAttributes(typeof(ToolHubItemAttribute), false).Length > 0);
    }

    private static string GetMenuName(Type t)
    {
        var attr = t.GetCustomAttributes(typeof(ToolHubItemAttribute), false).FirstOrDefault() as ToolHubItemAttribute;
        return attr?.MenuName ?? t.Name;
    }

    private static string GetDescription(Type t)
    {
        var attr = t.GetCustomAttributes(typeof(ToolHubItemAttribute), false).FirstOrDefault() as ToolHubItemAttribute;
        return attr?.Description ?? "";
    }

    private static int GetPriority(Type t)
    {
        var attr = t.GetCustomAttributes(typeof(ToolHubItemAttribute), false).FirstOrDefault() as ToolHubItemAttribute;
        return attr?.Priority ?? 100;
    }

    private ToolHubSettings LoadOrCreateSettings(bool forceRecreate = false)
    {
        if (forceRecreate)
        {
            AssetDatabase.DeleteAsset(SettingsAssetPath);
            AssetDatabase.SaveAssets();
        }

        var asset = AssetDatabase.LoadAssetAtPath<ToolHubSettings>(SettingsAssetPath);
        if (asset != null) return asset;

        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");

        asset = CreateInstance<ToolHubSettings>();
        AssetDatabase.CreateAsset(asset, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private void SaveSettings()
    {
        if (settings == null) return;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}
