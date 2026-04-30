using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// 日志配置编辑器工具面板 - 集成到开发工具箱
/// 支持按脚本控制日志输出、批量操作、配置保存加载
/// </summary>
[ToolHubItem("调试工具/日志配置管理器", "按脚本名称控制日志输出，支持批量操作和配置保存", 30)]
public class LogConfigPanel : IToolHubPanel
{
    private Vector2 scrollPosition = Vector2.zero;
    private string searchFilter = "";
    private Dictionary<string, bool> scriptLogStates = new Dictionary<string, bool>();
    private bool isDirty = false;
    private GUIStyle headerStyle;
    private GUIStyle toggleStyle;
    private List<string> filteredScripts = new List<string>();
    private bool stylesInitialized = false;

    public void OnEnable()
    {
        // 初始化时从 DebugEx 获取当前状态
        RefreshScriptStates();
    }

    public void OnDisable()
    {
        // 保存任何未保存的更改
        if (isDirty)
        {
            SaveConfig();
        }
    }

    public void OnGUI()
    {
        if (!stylesInitialized)
        {
            InitializeStyles();
            stylesInitialized = true;
        }

        DrawHeader();
        DrawToolbar();
        DrawSearchBar();
        DrawScriptList();
        DrawFooter();
    }

    public void OnDestroy()
    {
        // 清理资源
    }

    public string GetHelpText()
    {
        return "按脚本名称控制日志输出。点击复选框启用/禁用某个脚本的日志，使用批量操作快速管理多个脚本。";
    }

    #region 样式初始化

    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            padding = new RectOffset(5, 5, 5, 5)
        };

        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            padding = new RectOffset(5, 5, 3, 3)
        };
    }

    #endregion

    #region UI 绘制

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("日志配置管理器", headerStyle);
        EditorGUILayout.LabelField("按脚本名称精细控制日志输出", EditorStyles.helpBox);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("全启用", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SetAllScriptsEnabled(true);
        }

        if (GUILayout.Button("全禁用", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SetAllScriptsEnabled(false);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("恢复默认", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            ResetToDefault();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("扫描DebugEx脚本", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ScanDebugExScripts();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            RefreshScriptStates();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
        string newFilter = EditorGUILayout.TextField(searchFilter);
        if (newFilter != searchFilter)
        {
            searchFilter = newFilter;
            UpdateFilteredScripts();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
    }

    private void DrawScriptList()
    {
        EditorGUILayout.LabelField($"脚本列表 ({filteredScripts.Count}/{scriptLogStates.Count})", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        if (filteredScripts.Count == 0 && scriptLogStates.Count > 0)
        {
            EditorGUILayout.HelpBox($"没有找到匹配 \"{searchFilter}\" 的脚本", MessageType.Info);
        }
        else if (scriptLogStates.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无已注册的脚本。运行游戏后日志调用会自动注册脚本。", MessageType.Info);
        }

        // 按脚本名称排序显示
        foreach (var scriptName in filteredScripts)
        {
            DrawScriptToggle(scriptName);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawScriptToggle(string scriptName)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool currentState = scriptLogStates[scriptName];
        bool newState = EditorGUILayout.Toggle(currentState, toggleStyle, GUILayout.Width(20));

        EditorGUILayout.LabelField(scriptName, GUILayout.ExpandWidth(true));

        if (newState != currentState)
        {
            scriptLogStates[scriptName] = newState;
            isDirty = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFooter()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        // 状态指示
        string statusText = isDirty ? "★ 有未保存的更改" : "✓ 已保存";
        GUI.color = isDirty ? Color.yellow : Color.green;
        EditorGUILayout.LabelField(statusText, EditorStyles.helpBox, GUILayout.Width(150));
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();

        // 保存按钮
        GUI.color = isDirty ? Color.green : Color.gray;
        if (GUILayout.Button("保存配置", GUILayout.Width(80), GUILayout.Height(25)))
        {
            SaveConfig();
        }
        GUI.color = Color.white;

        // 加载按钮
        if (GUILayout.Button("加载配置", GUILayout.Width(80), GUILayout.Height(25)))
        {
            LoadConfig();
        }

        EditorGUILayout.EndHorizontal();

        // 快速操作
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("导出为JSON", GUILayout.Width(100)))
        {
            ExportToJSON();
        }

        if (GUILayout.Button("清除配置", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清除所有日志配置吗？", "确定", "取消"))
            {
                ClearConfig();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 功能方法

    /// <summary>
    /// 扫描项目中使用了 DebugEx 的脚本
    /// </summary>
    private void ScanDebugExScripts()
    {
        var debugExScripts = GetDebugExScripts();

        if (debugExScripts.Count == 0)
        {
            EditorUtility.DisplayDialog("扫描结果", "未找到使用 DebugEx 的脚本", "确定");
            return;
        }

        // 添加新发现的脚本到字典
        foreach (var scriptName in debugExScripts)
        {
            if (!scriptLogStates.ContainsKey(scriptName))
            {
                scriptLogStates[scriptName] = true; // 默认启用新发现的脚本
            }
        }

        UpdateFilteredScripts();
        isDirty = true;
        ApplyChangesToDebugEx();
        EditorUtility.DisplayDialog("扫描完成", $"发现 {debugExScripts.Count} 个使用 DebugEx 的脚本\n已添加到配置列表", "确定");
    }

    /// <summary>
    /// 获取项目中使用了 DebugEx 的脚本类名
    /// 扫描 Assets 文件夹中所有 .cs 文件，检查是否包含 DebugEx 调用
    /// </summary>
    private List<string> GetDebugExScripts()
    {
        var scriptNames = new HashSet<string>(); // 用 HashSet 避免重复

        // 查找所有 .cs 文件
        string[] guids = AssetDatabase.FindAssets("t:Script");

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // 只处理 C# 脚本文件
            if (!assetPath.EndsWith(".cs"))
                continue;

            // 读取文件内容检查是否包含 DebugEx 调用
            try
            {
                string fileContent = System.IO.File.ReadAllText(assetPath);
                if (!fileContent.Contains("DebugEx."))
                    continue;
            }
            catch
            {
                continue;
            }

            // 获取脚本文件
            var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (scriptAsset == null)
                continue;

            // 获取脚本中定义的所有类型
            System.Type[] types = scriptAsset.GetClass() != null
                ? new[] { scriptAsset.GetClass() }
                : System.Type.EmptyTypes;

            foreach (var type in types)
            {
                if (type != null && !string.IsNullOrEmpty(type.Name))
                {
                    scriptNames.Add(type.Name);
                }
            }
        }

        return new List<string>(scriptNames);
    }

    private void RefreshScriptStates()
    {
        scriptLogStates = DebugEx.GetAllScriptLogStates();
        UpdateFilteredScripts();
    }

    private void UpdateFilteredScripts()
    {
        filteredScripts.Clear();

        if (string.IsNullOrEmpty(searchFilter))
        {
            filteredScripts.AddRange(scriptLogStates.Keys.OrderBy(x => x));
        }
        else
        {
            string filter = searchFilter.ToLower();
            filteredScripts.AddRange(
                scriptLogStates.Keys
                    .Where(x => x.ToLower().Contains(filter))
                    .OrderBy(x => x)
            );
        }
    }

    private void SetAllScriptsEnabled(bool enabled)
    {
        foreach (var key in scriptLogStates.Keys.ToList())
        {
            scriptLogStates[key] = enabled;
        }
        isDirty = true;
        ApplyChangesToDebugEx();
    }

    private void ResetToDefault()
    {
        if (EditorUtility.DisplayDialog("确认", "确定要恢复所有脚本的默认配置吗？", "确定", "取消"))
        {
            DebugEx.ClearScriptLogConfig();
            RefreshScriptStates();
            isDirty = false;
            EditorUtility.DisplayDialog("提示", "已恢复默认配置", "确定");
        }
    }

    private void ApplyChangesToDebugEx()
    {
        DebugEx.SetAllScriptLogEnabled(new Dictionary<string, bool>(scriptLogStates));
    }

    private void SaveConfig()
    {
        ApplyChangesToDebugEx();
        bool success = LogConfigManager.SaveConfigToFile(scriptLogStates);
        isDirty = false;

        if (success)
        {
            EditorUtility.DisplayDialog("保存成功", "日志配置已保存", "确定");
        }
    }

    private void LoadConfig()
    {
        var loadedStates = LogConfigManager.LoadConfigFromFile();
        if (loadedStates.Count > 0)
        {
            scriptLogStates = loadedStates;
            ApplyChangesToDebugEx();
            UpdateFilteredScripts();
            isDirty = false;
            EditorUtility.DisplayDialog("加载成功", $"已加载 {loadedStates.Count} 个脚本的配置", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("加载失败", "没有找到保存的配置文件", "确定");
        }
    }

    private void ClearConfig()
    {
        LogConfigManager.DeleteConfigFile();
        DebugEx.ClearScriptLogConfig();
        RefreshScriptStates();
        isDirty = false;
        EditorUtility.DisplayDialog("清除成功", "配置已清除", "确定");
    }

    private void ExportToJSON()
    {
        string json = LogConfig.FromDictionary(scriptLogStates).ToJson();
        string path = EditorUtility.SaveFilePanel("导出日志配置", "", "log_config.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            EditorUtility.DisplayDialog("导出成功", $"配置已导出到:\n{path}", "确定");
        }
    }

    #endregion
}
