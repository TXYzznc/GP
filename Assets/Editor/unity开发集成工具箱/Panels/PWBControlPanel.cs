#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PluginMaster;

[ToolHubItem("场景工具/PWB 控制台", "PWB集成控制中心，快速切换笔刷工具。", 5)]
public class PWBControlPanel : IToolHubPanel
{
    private Vector2 scrollPos;
    private bool initialized = false;

    // --- 自定义保存按键 ---
    private const string PREF_SAVE_KEY = "PWB_Apply_Key";
    public static KeyCode ApplyKey
    {
        get => (KeyCode)EditorPrefs.GetInt(PREF_SAVE_KEY, (int)KeyCode.Return);
        set => EditorPrefs.SetInt(PREF_SAVE_KEY, (int)value);
    }
    // -------------------------

    // 工具名称
    private static readonly string[] ToolNames = {
        "Pin (图钉)", "Brush (笔刷)", "Gravity (重力)",
        "Line (线条)", "Shape (形状)", "Tiling (平铺)",
        "Replacer (替换)", "Eraser (橡皮)", "Select (选择)",
        "Extrude (挤出)", "Mirror (镜像)", "Floor (地板)", "Wall (墙壁)"
    };

    // 工具对应的 Tooltip
    private static readonly string[] ToolTooltips = {
        "Pin Tool (快捷键: 1)\n在光标位置单个放置预制体，支持精确控制位置和旋转。",
        "Brush Tool (快捷键: 2)\n在表面上批量绘制预制体，支持随机化和密度控制。",
        "Gravity Tool (快捷键: 3)\n基于物理重力模拟将物体自然掉落在表面上。",
        "Line Tool (快捷键: 4)\n沿绘制的线条或贝塞尔曲线排列对象。",
        "Shape Tool (快捷键: 5)\n沿圆形或多边形路径排列对象。",
        "Tiling Tool (快捷键: 6)\n在矩形区域内网格化平铺对象。",
        "Replacer Tool (快捷键: 7)\n将场景中选定的对象替换为当前笔刷中的预制体。",
        "Eraser Tool (快捷键: 8)\n擦除/删除 PWB 生成的物体或场景中的其他物体。",
        "Selection Tool (快捷键: 9)\nPWB 专用选择工具，支持基于网格和顶点的变换操作。",
        "Extrude Tool (快捷键: X)\n将选定物体沿指定轴向进行挤出复制。",
        "Mirror Tool (快捷键: M)\n创建选定对象的镜像副本。",
        "Floor Tool (快捷键: F)\n模块化地板生成工具，用于快速铺设地砖。",
        "Wall Tool (快捷键: W)\n模块化墙壁生成工具，用于快速建立围墙。"
    };

    private static readonly ToolManager.PaintTool[] ToolEnums = {
        ToolManager.PaintTool.PIN, ToolManager.PaintTool.BRUSH, ToolManager.PaintTool.GRAVITY,
        ToolManager.PaintTool.LINE, ToolManager.PaintTool.SHAPE, ToolManager.PaintTool.TILING,
        ToolManager.PaintTool.REPLACER, ToolManager.PaintTool.ERASER, ToolManager.PaintTool.SELECTION,
        ToolManager.PaintTool.EXTRUDE, ToolManager.PaintTool.MIRROR, ToolManager.PaintTool.FLOOR, ToolManager.PaintTool.WALL
    };

    public void OnEnable()
    {
        InitializePWB();
        ToolManager.OnToolChange += OnPWBToolChanged;
    }

    public void OnDisable() => ToolManager.OnToolChange -= OnPWBToolChanged;
    public void OnDestroy() => ToolManager.OnToolChange -= OnPWBToolChanged;

    private void InitializePWB()
    {
        if (initialized) return;
        try
        {
            if (!PWBCore.staticDataWasInitialized) PWBCore.Initialize();
            if (PaletteManager.allPalettesCount == 0)
            {
                // 确保加载了数据
                PaletteManager.instance.LoadPaletteFiles(true);
                PaletteManager.InitializeSelectedPalette();
            }
            initialized = true;
        }
        catch { }
    }

    private void OnPWBToolChanged(ToolManager.PaintTool tool)
    {
        // 刷新所有相关窗口
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        foreach (var win in windows)
            if (win.GetType().Name == "ToolHubWindow" || win.GetType().Name.Contains("PWBControlWindow")) win.Repaint();
    }

    public void OnGUI()
    {
        InitializePWB();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        try
        {
            DrawHeaderStatus();
            EditorGUILayout.Space(5);

            // 设置区域
            DrawCustomSettings();
            EditorGUILayout.Space(5);

            // 核心功能菜单按钮
            DrawMenuWindowsButtons();

            EditorGUILayout.Space(5);
            // 工具网格
            DrawToolsGrid();

            EditorGUILayout.Space(5);
            // 开关
            DrawSettingsToggles();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"绘制错误: {e.Message}", MessageType.Error);
            if (GUILayout.Button("重试初始化")) { initialized = false; InitializePWB(); }
        }

        EditorGUILayout.EndScrollView();
    }

    // 绘制自定义设置 (含Tooltip)
    private void DrawCustomSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(new GUIContent("⌨️ 快捷键配置", "设置 PWB 工具的常用交互按键"), EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        KeyCode newKey = (KeyCode)EditorGUILayout.EnumPopup(
            new GUIContent("确认生成按键 (Apply):", "在 Line, Shape, Tiling 等工具中确认并应用生成的快捷键。"),
            ApplyKey);

        if (EditorGUI.EndChangeCheck())
        {
            ApplyKey = newKey;
        }
        EditorGUILayout.HelpBox($"当前: 按 [{ApplyKey}] 键确认生成 (Line/Shape/Tiling 等)", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    // 状态头 (含Tooltip)
    private void DrawHeaderStatus()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        string brushName = "未选择";
        bool hasBrush = false;

        // 获取当前笔刷Tooltip
        string brushTooltip = "当前没有选择任何笔刷。请在调色板中选择一个。";

        if (PaletteManager.selectedPalette != null)
        {
            var brush = PaletteManager.selectedBrush;
            if (brush != null)
            {
                brushName = brush.name;
                hasBrush = true;
                brushTooltip = $"当前选中的笔刷: {brushName}\nID: {brush.id}";
            }
        }

        var statusStyle = new GUIStyle(EditorStyles.label);
        statusStyle.normal.textColor = hasBrush ? Color.green : Color.yellow;

        GUILayout.Label(new GUIContent("●", hasBrush ? "笔刷就绪" : "无笔刷"), statusStyle, GUILayout.Width(15));
        GUILayout.Label(new GUIContent($"当前笔刷: {brushName}", brushTooltip), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        GUILayout.Label(new GUIContent($"正在使用: {ToolManager.tool}", "当前激活的 PWB 工具"), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    // 菜单按钮 (含Tooltip)
    private void DrawMenuWindowsButtons()
    {
        EditorGUILayout.LabelField("功能窗口菜单", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // 第一行
            DrawDoubleButton(
                "工具栏 (Toolbar)", "打开 PWB 的浮动小工具栏", PWBToolbar.ShowWindow,
                "调色板 (Palette)", "打开预制体调色板，管理和选择要生成的对象", PrefabPalette.ShowWindow,
                28);

            // 第二行
            DrawDoubleButton(
                "笔刷属性 (Brush Props)", "调整当前选中笔刷的参数（如随机旋转、缩放、位置偏移等）", BrushProperties.ShowWindow,
                "工具属性 (Tool Props)", "调整当前激活工具的具体参数（如 Line 的间距、Shape 的半径等）", ToolProperties.ShowWindow);

            // 第三行
            DrawDoubleButton(
                "项目列表 (Items)", "查看和管理当前场景中所有持久化的 PWB 项目（如已画出的 Line 或 Tiling 配置）", PWBItemsWindow.ShowWindow,
                "笔刷创建设置 (Creation)", "设置如何将预制体拖入调色板的默认规则及缩略图生成规则", BrushCreationSettingsWindow.ShowWindow);

            // 第四行
            DrawDoubleButton(
                "网格与吸附设置 (Snap)", "配置网格大小、原点、旋转及吸附规则", SnapSettingsWindow.ShowWindow,
                "偏好设置 (Preferences)", "PWB 的全局设置，包括快捷键绑定、UI颜色等", PWBPreferences.ShowWindow);

            // 第五行
            if (GUILayout.Button(new GUIContent("📄 文档 (Documentation)", "打开 PWB 官方 PDF 文档"), GUILayout.Height(24)))
                PWBCore.OpenDocFile();
        }
    }

    private void DrawDoubleButton(
        string name1, string tip1, System.Action action1,
        string name2, string tip2, System.Action action2,
        float height = 24)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent(name1, tip1), GUILayout.Height(height))) action1?.Invoke();
        if (GUILayout.Button(new GUIContent(name2, tip2), GUILayout.Height(height))) action2?.Invoke();
        EditorGUILayout.EndHorizontal();
    }

    // 工具网格 (含Tooltip)
    private void DrawToolsGrid()
    {
        EditorGUILayout.LabelField("工具切换", EditorStyles.boldLabel);
        int columns = 3;
        int rows = Mathf.CeilToInt((float)ToolNames.Length / columns);
        for (int i = 0; i < rows; i++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < columns; j++)
            {
                int index = i * columns + j;
                if (index < ToolNames.Length)
                    DrawToolToggle(ToolNames[index], ToolTooltips[index], ToolEnums[index]);
                else
                    GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);

        // 判断当前是否有工具处于激活状态
        bool isPainting = ToolManager.tool != ToolManager.PaintTool.NONE;

        // 如果没有工具激活，禁用按钮交互
        using (new EditorGUI.DisabledScope(!isPainting))
        {
            Color originalColor = GUI.backgroundColor;

            // 只有在绘制状态下才显示红色作为警示，否则保持默认灰色（DisabledScope 会自动使其变暗）
            if (isPainting)
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            }

            if (GUILayout.Button(new GUIContent("✋ 停止绘制 (Esc)", "取消当前工具状态，停止绘制并返回普通选择模式"), GUILayout.Height(30)))
            {
                ToolManager.DeselectTool();
            }

            GUI.backgroundColor = originalColor;
        }
    }

    private void DrawToolToggle(string name, string tooltip, ToolManager.PaintTool toolType)
    {
        bool isActive = ToolManager.tool == toolType;
        var oldColor = GUI.backgroundColor;
        if (isActive) GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);

        if (GUILayout.Button(new GUIContent(name, tooltip), EditorStyles.miniButton))
        {
            if (isActive) ToolManager.DeselectTool();
            else
            {
                // 如果需要笔刷但未选择，自动打开调色板
                bool needBrush = toolType != ToolManager.PaintTool.SELECTION && toolType != ToolManager.PaintTool.ERASER &&
                                 toolType != ToolManager.PaintTool.EXTRUDE && toolType != ToolManager.PaintTool.MIRROR;
                if (needBrush && PaletteManager.selectedPalette != null && PaletteManager.selectedBrush == null)
                    PrefabPalette.ShowWindow();

                ToolManager.tool = toolType;
                PWBToolbar.RepaintWindow();
                SceneView.RepaintAll();
            }
        }
        GUI.backgroundColor = oldColor;
    }

    // 通用开关 (含Tooltip)
    private void DrawSettingsToggles()
    {
        if (SnapManager.settings == null) return;
        EditorGUILayout.LabelField("通用开关", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUI.BeginChangeCheck();

        bool snap = EditorGUILayout.Toggle(
            new GUIContent("吸附 (Snap)", "全局启用/禁用网格吸附功能 (快捷键: Ctrl/Command)"),
            SnapManager.settings.snappingEnabled);

        bool grid = EditorGUILayout.Toggle(
            new GUIContent("网格 (Grid)", "在 Scene 视图中显示/隐藏辅助网格"),
            SnapManager.settings.visibleGrid);

        if (EditorGUI.EndChangeCheck())
        {
            SnapManager.settings.snappingEnabled = snap;
            SnapManager.settings.visibleGrid = grid;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndVertical();
    }
}
#endif