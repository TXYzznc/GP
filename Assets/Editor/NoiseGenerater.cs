using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 基于FastNoiseLite的增强版噪声生成器
/// </summary>
public class NoiseGenerater : EditorWindow
{
    #region 枚举定义

    /// <summary>
    /// FastNoiseLite支持的噪声类型
    /// </summary>
    public enum NoiseType
    {
        [Tooltip("OpenSimplex2噪声 - 高质量通用噪声")]
        OpenSimplex2,

        [Tooltip("OpenSimplex2S噪声 - 平滑变体")]
        OpenSimplex2S,

        [Tooltip("细胞噪声 - 蜂窝状图案")]
        Cellular,

        [Tooltip("柏林噪声 - 经典算法")]
        Perlin,

        [Tooltip("值噪声 - 简单快速")]
        Value,

        [Tooltip("立方值噪声 - 更平滑的值噪声")]
        ValueCubic
    }

    /// <summary>
    /// 分形类型
    /// </summary>
    public enum FractalType
    {
        [Tooltip("无分形 - 单层噪声")]
        None,

        [Tooltip("FBM分形 - 标准多层次细节")]
        FBm,

        [Tooltip("山脊分形 - 山脉效果")]
        Ridged,

        [Tooltip("乒乓分形 - 波浪起伏")]
        PingPong,

        [Tooltip("域扭曲渐进式")]
        DomainWarpProgressive,

        [Tooltip("域扭曲独立式")]
        DomainWarpIndependent
    }

    /// <summary>
    /// 细胞噪声返回类型
    /// </summary>
    public enum CellularReturnType
    {
        [Tooltip("距离 - 到最近点的距离")]
        Distance,

        [Tooltip("距离2 - 到第二近点的距离")]
        Distance2,

        [Tooltip("距离2添加 - 两个距离相加")]
        Distance2Add,

        [Tooltip("距离2减去 - 两个距离相减")]
        Distance2Sub,

        [Tooltip("距离2乘以 - 两个距离相乘")]
        Distance2Mul,

        [Tooltip("距离2除以 - 两个距离相除")]
        Distance2Div,

        [Tooltip("细胞值 - 细胞的随机值")]
        CellValue
    }

    /// <summary>
    /// 细胞距离函数
    /// </summary>
    public enum CellularDistanceFunction
    {
        [Tooltip("欧几里得距离 - 圆形细胞")]
        Euclidean,

        [Tooltip("欧几里得平方 - 性能更好")]
        EuclideanSq,

        [Tooltip("曼哈顿距离 - 方形细胞")]
        Manhattan,

        [Tooltip("混合距离 - 介于欧氏和曼哈顿之间")]
        Hybrid
    }

    /// <summary>
    /// 域扭曲类型
    /// </summary>
    public enum DomainWarpType
    {
        [Tooltip("OpenSimplex2扭曲")]
        OpenSimplex2,

        [Tooltip("OpenSimplex2缩减")]
        OpenSimplex2Reduced,

        [Tooltip("基础网格扭曲")]
        BasicGrid
    }

    #endregion

    #region 成员变量

    // FastNoiseLite实例
    private FastNoiseLite noise;

    // 基础参数
    private NoiseType noiseType = NoiseType.OpenSimplex2;
    private int seed = 1337;
    private float frequency = 0.01f;

    // 分形参数
    private FractalType fractalType = FractalType.None;
    private int octaves = 4;
    private float lacunarity = 2.0f;
    private float gain = 0.5f;
    private float weightedStrength = 0.0f;
    private float pingPongStrength = 2.0f;

    // 细胞噪声参数
    private CellularDistanceFunction cellularDistanceFunction = CellularDistanceFunction.Euclidean;
    private CellularReturnType cellularReturnType = CellularReturnType.Distance;
    private float cellularJitter = 1.0f;

    // 域扭曲参数
    private bool useDomainWarp = false;
    private DomainWarpType domainWarpType = DomainWarpType.OpenSimplex2;
    private float domainWarpAmp = 30.0f;

    // 纹理参数
    private int width = 512;
    private int height = 512;
    private Vector2 offset = Vector2.zero;

    // 后处理
    private bool invertNoise = false;
    private bool useContrast = false;
    private float contrastPower = 1.0f;
    private bool useColorGradient = false;
    private Gradient colorGradient = new Gradient();
    private bool normalizeOutput = true;

    // UI
    private Texture2D previewTexture;
    private Vector2 scrollPos;
    private bool autoRefresh = true;
    private string saveFolderName = "SaveImages";
    private string customFileName = "";  // 新增：自定义文件名

    // 预设
    private int selectedPreset = 0;
    private string[] presetNames = new string[]
    {
        "自定义",
        "经典地形",
        "细腻云层",
        "岩石表面",
        "水波纹",
        "木纹",
        "大理石",
        "细胞组织",
        "火焰",
        "山脉"
    };

    #endregion

    #region 窗口初始化

    [MenuItem("Tools/FastNoise噪声生成器")]
    static void Init()
    {
        NoiseGenerater window = GetWindow<NoiseGenerater>("FastNoise生成器");
        window.minSize = new Vector2(900, 700);
        window.Show();
    }

    void OnEnable()
    {
        InitializeNoise();
        colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
    }

    void InitializeNoise()
    {
        noise = new FastNoiseLite(seed);
        UpdateNoiseSettings();
    }

    #endregion

    #region GUI绘制

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawTitle();
        DrawPresetSection();
        DrawBasicParameters();
        DrawFractalParameters();

        if (noiseType == NoiseType.Cellular)
        {
            DrawCellularParameters();
        }

        DrawDomainWarpParameters();
        DrawPostProcessing();
        DrawControlButtons();
        DrawPreview();

        EditorGUILayout.EndScrollView();

        // 检测参数变化并自动刷新
        if (autoRefresh && GUI.changed)
        {
            UpdateNoiseSettings();
            GeneratePreview();
        }
    }

    void DrawTitle()
    {
        GUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("⚡ FastNoiseLite 高性能噪声生成器", titleStyle);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "FastNoiseLite是业界领先的开源噪声库，提供极致性能和丰富功能。\n" +
            "支持6种噪声类型、5种分形算法、域扭曲等高级特性。",
            MessageType.Info
        );
        GUILayout.Space(10);
    }

    void DrawPresetSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("📦 快速预设", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        selectedPreset = EditorGUILayout.Popup("预设模板", selectedPreset, presetNames);
        if (EditorGUI.EndChangeCheck() && selectedPreset > 0)
        {
            ApplyPreset(selectedPreset);
            selectedPreset = 0; // 重置为自定义
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    void DrawBasicParameters()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("⚙️ 基础参数", EditorStyles.boldLabel);

        noiseType = (NoiseType)EditorGUILayout.EnumPopup(
            new GUIContent("噪声类型", GetNoiseTypeDescription(noiseType)),
            noiseType
        );

        EditorGUILayout.HelpBox(GetNoiseTypeDescription(noiseType), MessageType.None);

        seed = EditorGUILayout.IntField(
            new GUIContent("随机种子", "改变种子可生成完全不同的噪声图案"),
            seed
        );

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎲 随机种子", GUILayout.Width(120)))
        {
            seed = Random.Range(0, 99999);
        }
        if (GUILayout.Button("📋 复制种子", GUILayout.Width(120)))
        {
            EditorGUIUtility.systemCopyBuffer = seed.ToString();
            Debug.Log($"已复制种子: {seed}");
        }
        EditorGUILayout.EndHorizontal();

        frequency = EditorGUILayout.Slider(
            new GUIContent("频率", "控制噪声的密集程度，值越大图案越密集"),
            frequency, 0.001f, 0.1f
        );

        EditorGUILayout.BeginHorizontal();
        width = EditorGUILayout.IntPopup("宽度", width,
            new string[] { "128", "256", "512", "1024", "2048" },
            new int[] { 128, 256, 512, 1024, 2048 }
        );
        height = EditorGUILayout.IntPopup("高度", height,
            new string[] { "128", "256", "512", "1024", "2048" },
            new int[] { 128, 256, 512, 1024, 2048 }
        );
        EditorGUILayout.EndHorizontal();

        offset = EditorGUILayout.Vector2Field(
            new GUIContent("偏移量", "在噪声空间中移动采样位置"),
            offset
        );

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    void DrawFractalParameters()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🌊 分形参数（多层次细节）", EditorStyles.boldLabel);

        fractalType = (FractalType)EditorGUILayout.EnumPopup(
            new GUIContent("分形类型", "叠加多层不同频率的噪声创建复杂效果"),
            fractalType
        );

        if (fractalType != FractalType.None)
        {
            EditorGUILayout.HelpBox(GetFractalDescription(fractalType), MessageType.None);

            octaves = EditorGUILayout.IntSlider(
                new GUIContent("八度数", "叠加的层数，越多细节越丰富但性能越低"),
                octaves, 1, 10
            );

            lacunarity = EditorGUILayout.Slider(
                new GUIContent("间隙度", "每层频率增长倍数，通常为2.0"),
                lacunarity, 1.0f, 4.0f
            );

            gain = EditorGUILayout.Slider(
                new GUIContent("增益/持续度", "每层振幅衰减系数，0.5表示每层减半"),
                gain, 0.0f, 1.0f
            );

            if (fractalType == FractalType.PingPong)
            {
                pingPongStrength = EditorGUILayout.Slider(
                    new GUIContent("乒乓强度", "控制乒乓效果的强度"),
                    pingPongStrength, 0.0f, 5.0f
                );
            }

            weightedStrength = EditorGUILayout.Slider(
                new GUIContent("权重强度", "基于前一层输出调整当前层权重"),
                weightedStrength, 0.0f, 1.0f
            );
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    void DrawCellularParameters()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🔷 细胞噪声参数", EditorStyles.boldLabel);

        cellularDistanceFunction = (CellularDistanceFunction)EditorGUILayout.EnumPopup(
            new GUIContent("距离函数", "决定细胞的形状特征"),
            cellularDistanceFunction
        );

        cellularReturnType = (CellularReturnType)EditorGUILayout.EnumPopup(
            new GUIContent("返回类型", "决定如何计算细胞值"),
            cellularReturnType
        );

        cellularJitter = EditorGUILayout.Slider(
            new GUIContent("抖动强度", "控制细胞点的随机偏移，0为规则网格，1为完全随机"),
            cellularJitter, 0.0f, 1.0f
        );

        EditorGUILayout.HelpBox(
            "💡 提示：\n" +
            "• Distance: 经典Worley噪声\n" +
            "• Distance2Sub: 产生清晰的细胞边界\n" +
            "• CellValue: 每个细胞不同颜色",
            MessageType.None
        );

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    void DrawDomainWarpParameters()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🌀 域扭曲（高级）", EditorStyles.boldLabel);

        useDomainWarp = EditorGUILayout.Toggle(
            new GUIContent("启用域扭曲", "使用噪声扭曲另一个噪声，创建有机形态"),
            useDomainWarp
        );

        if (useDomainWarp)
        {
            domainWarpType = (DomainWarpType)EditorGUILayout.EnumPopup(
                new GUIContent("扭曲类型", "选择扭曲算法"),
                domainWarpType
            );

            domainWarpAmp = EditorGUILayout.Slider(
                new GUIContent("扭曲强度", "扭曲的剧烈程度"),
                domainWarpAmp, 1.0f, 200.0f
            );

            EditorGUILayout.HelpBox(
                "域扭曲会显著改变噪声的视觉效果，适合创建:\n" +
                "• 流体纹理\n" +
                "• 有机形态\n" +
                "• 魔法效果",
                MessageType.Info
            );
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    void DrawPostProcessing()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🎨 后处理效果", EditorStyles.boldLabel);

        normalizeOutput = EditorGUILayout.Toggle(
            new GUIContent("归一化输出", "将输出值映射到0-1范围"),
            normalizeOutput
        );

        invertNoise = EditorGUILayout.Toggle(
            new GUIContent("反转噪声", "黑白颠倒"),
            invertNoise
        );

        useContrast = EditorGUILayout.Toggle(
            new GUIContent("启用对比度", "增强明暗对比"),
            useContrast
        );

        if (useContrast)
        {
            contrastPower = EditorGUILayout.Slider(
                new GUIContent("对比度强度", "1.0为原始值"),
                contrastPower, 0.1f, 5.0f
            );
        }

        useColorGradient = EditorGUILayout.Toggle(
            new GUIContent("颜色渐变", "将灰度映射到彩色渐变"),
            useColorGradient
        );

        if (useColorGradient)
        {
            colorGradient = EditorGUILayout.GradientField(
                new GUIContent("渐变色", "噪声值对应的颜色"),
                colorGradient
            );
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    void DrawControlButtons()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        autoRefresh = EditorGUILayout.Toggle("自动刷新", autoRefresh);

        if (!autoRefresh && GUILayout.Button("🔄 手动生成", GUILayout.Height(30)))
        {
            UpdateNoiseSettings();
            GeneratePreview();
        }

        if (GUILayout.Button("💾 保存PNG", GUILayout.Height(30), GUILayout.Width(120)))
        {
            if (previewTexture == null)
            {
                UpdateNoiseSettings();
                GeneratePreview();
            }
            SaveTexture();
        }
        EditorGUILayout.EndHorizontal();

        saveFolderName = EditorGUILayout.TextField("保存文件夹", saveFolderName);
        
        // 新增：文件名输入框（必填）
        EditorGUILayout.BeginHorizontal();
        customFileName = EditorGUILayout.TextField(
            new GUIContent("文件名 ⚠️", "必须填写，不需要加.png后缀"),
            customFileName
        );
        
        // 显示提示状态
        if (string.IsNullOrWhiteSpace(customFileName))
        {
            GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
            warningStyle.normal.textColor = Color.red;
            warningStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("必填！", warningStyle, GUILayout.Width(50));
        }
        else
        {
            GUIStyle okStyle = new GUIStyle(GUI.skin.label);
            okStyle.normal.textColor = Color.green;
            okStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("✓", okStyle, GUILayout.Width(50));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    void DrawPreview()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("👁️ 实时预览", EditorStyles.boldLabel);

        if (previewTexture == null)
        {
            GeneratePreview();
        }

        if (previewTexture != null)
        {
            // 固定正方形尺寸
            float squareSize = 512f;

            // 创建正方形容器
            Rect containerRect = GUILayoutUtility.GetRect(squareSize, squareSize);

            // 绘制深色背景
            EditorGUI.DrawRect(containerRect, new Color(0.18f, 0.18f, 0.18f, 1f));

            // 使用ScaleToFit模式，图片会保持宽高比居中显示在正方形内
            GUI.DrawTexture(containerRect, previewTexture, ScaleMode.ScaleToFit);

            // 绘制边框
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            Handles.DrawSolidRectangleWithOutline(containerRect, Color.clear, Handles.color);
            Handles.EndGUI();

            EditorGUILayout.LabelField(
                $"📐 纹理: {width}×{height} | 预览: {squareSize}×{squareSize} | 🎲 种子: {seed}",
                EditorStyles.miniLabel
            );
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region 噪声设置更新

    void UpdateNoiseSettings()
    {
        if (noise == null)
        {
            noise = new FastNoiseLite(seed);
        }
        else
        {
            noise.SetSeed(seed);
        }

        // 设置噪声类型
        switch (noiseType)
        {
            case NoiseType.OpenSimplex2:
                noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
                break;
            case NoiseType.OpenSimplex2S:
                noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
                break;
            case NoiseType.Cellular:
                noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
                break;
            case NoiseType.Perlin:
                noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
                break;
            case NoiseType.Value:
                noise.SetNoiseType(FastNoiseLite.NoiseType.Value);
                break;
            case NoiseType.ValueCubic:
                noise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
                break;
        }

        noise.SetFrequency(frequency);

        // 设置分形
        switch (fractalType)
        {
            case FractalType.None:
                noise.SetFractalType(FastNoiseLite.FractalType.None);
                break;
            case FractalType.FBm:
                noise.SetFractalType(FastNoiseLite.FractalType.FBm);
                break;
            case FractalType.Ridged:
                noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
                break;
            case FractalType.PingPong:
                noise.SetFractalType(FastNoiseLite.FractalType.PingPong);
                break;
            case FractalType.DomainWarpProgressive:
                noise.SetFractalType(FastNoiseLite.FractalType.DomainWarpProgressive);
                break;
            case FractalType.DomainWarpIndependent:
                noise.SetFractalType(FastNoiseLite.FractalType.DomainWarpIndependent);
                break;
        }

        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lacunarity);
        noise.SetFractalGain(gain);
        noise.SetFractalWeightedStrength(weightedStrength);
        noise.SetFractalPingPongStrength(pingPongStrength);

        // 细胞噪声设置
        if (noiseType == NoiseType.Cellular)
        {
            switch (cellularDistanceFunction)
            {
                case CellularDistanceFunction.Euclidean:
                    noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
                    break;
                case CellularDistanceFunction.EuclideanSq:
                    noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.EuclideanSq);
                    break;
                case CellularDistanceFunction.Manhattan:
                    noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
                    break;
                case CellularDistanceFunction.Hybrid:
                    noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Hybrid);
                    break;
            }

            switch (cellularReturnType)
            {
                case CellularReturnType.Distance:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
                    break;
                case CellularReturnType.Distance2:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2);
                    break;
                case CellularReturnType.Distance2Add:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Add);
                    break;
                case CellularReturnType.Distance2Sub:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Sub);
                    break;
                case CellularReturnType.Distance2Mul:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Mul);
                    break;
                case CellularReturnType.Distance2Div:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Div);
                    break;
                case CellularReturnType.CellValue:
                    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
                    break;
            }

            noise.SetCellularJitter(cellularJitter);
        }

        // 域扭曲设置
        if (useDomainWarp)
        {
            switch (domainWarpType)
            {
                case DomainWarpType.OpenSimplex2:
                    noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
                    break;
                case DomainWarpType.OpenSimplex2Reduced:
                    noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2Reduced);
                    break;
                case DomainWarpType.BasicGrid:
                    noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
                    break;
            }
            noise.SetDomainWarpAmp(domainWarpAmp);
        }
    }

    #endregion

    #region 纹理生成

    void GeneratePreview()
    {
        if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
        {
            previewTexture = new Texture2D(width, height);
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        float[,] noiseValues = new float[width, height];

        // 第一遍：生成噪声并找到最小最大值
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = x + offset.x;
                float yCoord = y + offset.y;

                // 使用FastNoiseLite生成噪声
                float noiseValue;
                if (useDomainWarp)
                {
                    // 域扭曲
                    noise.DomainWarp(ref xCoord, ref yCoord);
                    noiseValue = noise.GetNoise(xCoord, yCoord);
                }
                else
                {
                    noiseValue = noise.GetNoise(xCoord, yCoord);
                }

                noiseValues[x, y] = noiseValue;
                minValue = Mathf.Min(minValue, noiseValue);
                maxValue = Mathf.Max(maxValue, noiseValue);
            }
        }

        // 第二遍：归一化并应用后处理
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = noiseValues[x, y];

                // 归一化到0-1
                if (normalizeOutput && maxValue > minValue)
                {
                    value = (value - minValue) / (maxValue - minValue);
                }
                else
                {
                    value = (value + 1f) * 0.5f; // FastNoiseLite返回-1到1
                }

                // 后处理
                if (invertNoise)
                    value = 1f - value;

                if (useContrast)
                    value = Mathf.Pow(value, contrastPower);

                value = Mathf.Clamp01(value);

                // 颜色映射
                Color color;
                if (useColorGradient)
                {
                    color = colorGradient.Evaluate(value);
                }
                else
                {
                    color = new Color(value, value, value);
                }

                previewTexture.SetPixel(x, y, color);
            }
        }

        previewTexture.Apply();
    }

    #endregion

    #region 预设系统

    void ApplyPreset(int presetIndex)
    {
        switch (presetIndex)
        {
            case 1: // 经典地形
                noiseType = NoiseType.Perlin;
                fractalType = FractalType.FBm;
                octaves = 6;
                frequency = 0.005f;
                lacunarity = 2.0f;
                gain = 0.5f;
                useDomainWarp = false;
                break;

            case 2: // 细腻云层
                noiseType = NoiseType.OpenSimplex2;
                fractalType = FractalType.FBm;
                octaves = 5;
                frequency = 0.008f;
                lacunarity = 2.5f;
                gain = 0.6f;
                useDomainWarp = true;
                domainWarpAmp = 50f;
                break;

            case 3: // 岩石表面
                noiseType = NoiseType.ValueCubic;
                fractalType = FractalType.Ridged;
                octaves = 6;
                frequency = 0.015f;
                lacunarity = 2.2f;
                gain = 0.45f;
                useContrast = true;
                contrastPower = 1.5f;
                break;

            case 4: // 水波纹
                noiseType = NoiseType.Cellular;
                cellularReturnType = CellularReturnType.Distance2Sub;
                cellularDistanceFunction = CellularDistanceFunction.Euclidean;
                frequency = 0.02f;
                cellularJitter = 0.8f;
                break;

            case 5: // 木纹
                noiseType = NoiseType.Perlin;
                fractalType = FractalType.PingPong;
                octaves = 4;
                frequency = 0.01f;
                pingPongStrength = 3.0f;
                useDomainWarp = true;
                domainWarpAmp = 100f;
                break;

            case 6: // 大理石
                noiseType = NoiseType.OpenSimplex2S;
                fractalType = FractalType.Ridged;
                octaves = 5;
                frequency = 0.012f;
                useDomainWarp = true;
                domainWarpAmp = 80f;
                useContrast = true;
                contrastPower = 2.0f;
                break;

            case 7: // 细胞组织
                noiseType = NoiseType.Cellular;
                cellularReturnType = CellularReturnType.CellValue;
                cellularDistanceFunction = CellularDistanceFunction.Hybrid;
                frequency = 0.025f;
                cellularJitter = 1.0f;
                fractalType = FractalType.FBm;
                octaves = 3;
                break;

            case 8: // 火焰
                noiseType = NoiseType.OpenSimplex2;
                fractalType = FractalType.Ridged;
                octaves = 6;
                frequency = 0.02f;
                lacunarity = 3.0f;
                gain = 0.6f;
                useDomainWarp = true;
                domainWarpType = DomainWarpType.OpenSimplex2;
                domainWarpAmp = 150f;
                break;

            case 9: // 山脉
                noiseType = NoiseType.Perlin;
                fractalType = FractalType.Ridged;
                octaves = 7;
                frequency = 0.004f;
                lacunarity = 2.5f;
                gain = 0.4f;
                useContrast = true;
                contrastPower = 1.8f;
                break;
        }

        UpdateNoiseSettings();
        GeneratePreview();
    }

    #endregion

    #region 描述文本

    string GetNoiseTypeDescription(NoiseType type)
    {
        switch (type)
        {
            case NoiseType.OpenSimplex2:
                return "【OpenSimplex2】现代高质量噪声，性能优秀，无方向性失真\n适合：地形、云层、通用纹理";
            case NoiseType.OpenSimplex2S:
                return "【OpenSimplex2S】更平滑的OpenSimplex2变体\n适合：需要极致平滑的场景";
            case NoiseType.Cellular:
                return "【细胞噪声】基于Worley噪声，产生细胞状图案\n适合：岩石、细胞、水纹、裂缝";
            case NoiseType.Perlin:
                return "【柏林噪声】经典算法，广泛应用\n适合：传统地形、云雾、大理石";
            case NoiseType.Value:
                return "【值噪声】简单快速的基础噪声\n适合：快速原型、简单纹理";
            case NoiseType.ValueCubic:
                return "【立方值噪声】更平滑的值噪声\n适合：需要平滑过渡的场景";
            default:
                return "";
        }
    }

    string GetFractalDescription(FractalType type)
    {
        switch (type)
        {
            case FractalType.FBm:
                return "标准的分形布朗运动，叠加多层噪声创建自然细节";
            case FractalType.Ridged:
                return "产生尖锐的山脊效果，适合山脉地形";
            case FractalType.PingPong:
                return "创建波浪起伏效果，适合水面、木纹";
            case FractalType.DomainWarpProgressive:
                return "渐进式域扭曲，每层叠加扭曲效果";
            case FractalType.DomainWarpIndependent:
                return "独立域扭曲，每层独立计算";
            default:
                return "";
        }
    }

    #endregion

    #region 文件保存

    void SaveTexture()
    {
        // 检查文件名是否为空
        if (string.IsNullOrWhiteSpace(customFileName))
        {
            EditorUtility.DisplayDialog(
                "❌ 保存失败", 
                "请先输入文件名！\n\n文件名不能为空。", 
                "确定"
            );
            return;
        }

        // 清理文件名（移除非法字符）
        string sanitizedFileName = SanitizeFileName(customFileName);
        
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            EditorUtility.DisplayDialog(
                "❌ 保存失败", 
                "文件名包含非法字符，请重新输入！", 
                "确定"
            );
            return;
        }

        byte[] bytes = previewTexture.EncodeToPNG();
        string dirPath = Application.dataPath + "/" + saveFolderName + "/";

        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        // 使用用户输入的文件名
        string fileName = $"{dirPath}{sanitizedFileName}.png";
        
        // 检查文件是否已存在
        if (File.Exists(fileName))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "⚠️ 文件已存在",
                $"文件 '{sanitizedFileName}.png' 已存在！\n\n是否覆盖？",
                "覆盖",
                "取消"
            );
            
            if (!overwrite)
            {
                return;
            }
        }

        // 保存文件
        File.WriteAllBytes(fileName, bytes);
        Debug.Log($"✅ 噪声图已保存: {fileName}");
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "✅ 保存成功", 
            $"文件已保存到:\n{fileName}\n\n文件名: {sanitizedFileName}.png\n尺寸: {width}×{height}", 
            "确定"
        );
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    string SanitizeFileName(string fileName)
    {
        // 移除文件名中的非法字符
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitized = fileName;
        
        foreach (char c in invalidChars)
        {
            sanitized = sanitized.Replace(c.ToString(), "");
        }
        
        // 移除.png后缀（如果用户手动添加了）
        if (sanitized.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
        {
            sanitized = sanitized.Substring(0, sanitized.Length - 4);
        }
        
        return sanitized.Trim();
    }

    #endregion
}