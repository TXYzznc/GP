using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tool_Plugins.MaterialConverter
{
    /// <summary>
    /// 材质批量转换器 - 转换时保留贴图/颜色/参数，无法迁移的属性输出警告
    /// </summary>
    public class MaterialConverterWindow : EditorWindow
    {
        // ── 预设 Shader 映射表 ──────────────────────────────────────────────
        private static readonly Dictionary<string, string[]> ShaderPresets = new()
        {
            ["Standard"] = new[] { "Standard", "Standard (Specular setup)" },
            ["URP"] = new[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Particles/Lit",
                "Universal Render Pipeline/Particles/Simple Lit",
                "Universal Render Pipeline/Particles/Unlit",
            },
            ["HDRP"] = new[]
            {
                "HDRP/Lit",
                "HDRP/Unlit",
                "HDRP/Eye",
                "HDRP/Hair",
                "HDRP/StackLit",
                "HDRP/LayeredLit",
            },
        };

        private static readonly string[] TargetPipelineOptions =
        {
            "Standard",
            "URP",
            "HDRP",
            "Custom",
        };

        private static readonly Dictionary<string, string> DefaultTargetShader = new()
        {
            ["Standard"] = "Standard",
            ["URP"] = "Universal Render Pipeline/Lit",
            ["HDRP"] = "HDRP/Lit",
        };

        // ── 跨管线属性别名映射（源属性名 → 目标属性名候选列表，按优先级排列）──
        // 当同名属性在目标 Shader 中不存在时，依次尝试别名
        private static readonly Dictionary<string, string[]> PropAliases = new()
        {
            // 基础颜色
            ["_Color"] = new[] { "_BaseColor" },
            ["_BaseColor"] = new[] { "_Color" },
            ["_AlbedoColor"] = new[] { "_BaseColor", "_Color" },
            ["_MainColor"] = new[] { "_BaseColor", "_Color" },
            ["_TintColor"] = new[] { "_BaseColor", "_Color" },
            ["_DiffuseColor"] = new[] { "_BaseColor", "_Color" },
            // 主贴图 / Albedo
            ["_MainTex"] = new[] { "_BaseMap", "_BaseColorMap" },
            ["_BaseMap"] = new[] { "_MainTex" },
            ["_BaseColorMap"] = new[] { "_MainTex", "_BaseMap" },
            ["_Albedo"] = new[] { "_BaseMap", "_MainTex" },
            ["_AlbedoMap"] = new[] { "_BaseMap", "_MainTex" },
            ["_AlbedoTexture"] = new[] { "_BaseMap", "_MainTex" },
            ["_DiffuseMap"] = new[] { "_BaseMap", "_MainTex" },
            ["_DiffuseTex"] = new[] { "_BaseMap", "_MainTex" },
            ["_ColorMap"] = new[] { "_BaseMap", "_MainTex" },
            // 自发光颜色
            ["_EmissionColor"] = new[] { "_EmissiveColor" },
            ["_EmissiveColor"] = new[] { "_EmissionColor" },
            // 自发光贴图
            ["_EmissionMap"] = new[] { "_EmissiveColorMap" },
            ["_EmissiveColorMap"] = new[] { "_EmissionMap" },
            // 法线贴图
            ["_BumpMap"] = new[] { "_NormalMap" },
            ["_NormalMap"] = new[] { "_BumpMap" },
            ["_NormalTex"] = new[] { "_BumpMap", "_NormalMap" },
            ["_NormalTexture"] = new[] { "_BumpMap", "_NormalMap" },
            // 法线强度
            ["_BumpScale"] = new[] { "_NormalScale", "_NormalMapDepth" },
            ["_NormalScale"] = new[] { "_BumpScale" },
            ["_NormalMapDepth"] = new[] { "_BumpScale", "_NormalScale" },
            // 遮挡贴图
            ["_OcclusionMap"] = new[] { "_MaskMap" },
            ["_AOMap"] = new[] { "_OcclusionMap", "_MaskMap" },
            ["_AmbientOcclusionMap"] = new[] { "_OcclusionMap", "_MaskMap" },
            // 金属度
            ["_Metallic"] = new[] { "_Metalness" },
            ["_Metalness"] = new[] { "_Metallic" },
            // 金属贴图
            ["_MetallicGlossMap"] = new[] { "_MaskMap" },
            ["_MetallicMap"] = new[] { "_MetallicGlossMap", "_MaskMap" },
            ["_MetallicTexture"] = new[] { "_MetallicGlossMap", "_MaskMap" },
            // 平滑度 / 粗糙度
            ["_Glossiness"] = new[] { "_Smoothness" },
            ["_GlossMapScale"] = new[] { "_Smoothness" },
            ["_Smoothness"] = new[] { "_Glossiness", "_GlossMapScale" },
            ["_Snoothness"] = new[] { "_Smoothness", "_Glossiness" }, // ASE 拼写错误
            ["_SmoothnessMap"] = new[] { "_MetallicGlossMap" },
            ["_RoughnessMap"] = new[] { "_MetallicGlossMap" },
            ["_Roughness"] = new[] { "_Smoothness" },
            // 高度贴图
            ["_ParallaxMap"] = new[] { "_HeightMap" },
            ["_HeightMap"] = new[] { "_ParallaxMap" },
            // 高度强度
            ["_Parallax"] = new[] { "_HeightAmplitude", "_ParalaxOffset" },
            ["_HeightAmplitude"] = new[] { "_Parallax" },
            ["_ParalaxOffset"] = new[] { "_Parallax", "_HeightAmplitude" }, // ASE 拼写错误
            // 透明度裁剪
            ["_Cutoff"] = new[] { "_AlphaClipThreshold" },
            ["_AlphaClipThreshold"] = new[] { "_Cutoff" },
        };

        // ── 状态 ────────────────────────────────────────────────────────────
        private DefaultAsset _targetFolder;
        private int _sourcePipelineIndex = 0;
        private int _targetPipelineIndex = 1;
        private string _customTargetShader = "";
        private bool _includeSubfolders = true;
        private Vector2 _previewScroll;
        private Vector2 _logScroll;

        private readonly List<Material> _previewMaterials = new();
        private bool _previewDirty = true;

        // 日志条目：(文字, 是否警告)
        private readonly List<(string text, bool isWarning)> _log = new();

        [MenuItem("Tools/Material Converter")]
        public static void Open()
        {
            var win = GetWindow<MaterialConverterWindow>("材质转换器");
            win.minSize = new Vector2(480, 580);
        }

        // 调试用：打印选中材质的所有 Shader 属性
        [MenuItem("Tools/Material Converter - Debug Selected Material")]
        public static void DebugSelectedMaterial()
        {
            var mat = Selection.activeObject as Material;
            if (mat == null)
            {
                Debug.LogWarning("[MaterialConverter] 请先在 Project 面板选中一个材质");
                return;
            }

            var shader = mat.shader;
            int count = ShaderUtil.GetPropertyCount(shader);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== {mat.name}  Shader: {shader.name}  ({count} props) ===");
            for (int i = 0; i < count; i++)
            {
                string name = ShaderUtil.GetPropertyName(shader, i);
                var type = ShaderUtil.GetPropertyType(shader, i);
                string val = type switch
                {
                    ShaderUtil.ShaderPropertyType.Color => mat.GetColor(name).ToString(),
                    ShaderUtil.ShaderPropertyType.Vector => mat.GetVector(name).ToString(),
                    ShaderUtil.ShaderPropertyType.Float => mat.GetFloat(name).ToString("F4"),
                    ShaderUtil.ShaderPropertyType.Range => mat.GetFloat(name).ToString("F4"),
                    ShaderUtil.ShaderPropertyType.TexEnv => mat.GetTexture(name)?.name ?? "(null)",
                    _ => "?",
                };
                sb.AppendLine($"  [{type, -8}]  {name, -30} = {val}");
            }
            Debug.Log(sb.ToString());
        }

        // ── GUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            GUILayout.Label("材质批量转换器", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawFolderField();
            EditorGUILayout.Space(6);
            DrawPipelineSelectors();
            EditorGUILayout.Space(6);
            DrawOptions();
            EditorGUILayout.Space(8);
            DrawPreviewSection();
            EditorGUILayout.Space(6);
            DrawActionButtons();
            EditorGUILayout.Space(6);
            DrawLog();
        }

        // ── 文件夹拖入区域 ───────────────────────────────────────────────────
        private void DrawFolderField()
        {
            GUILayout.Label("目标文件夹", EditorStyles.miniBoldLabel);
            var dropRect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            GUI.Box(
                dropRect,
                _targetFolder == null
                    ? "将文件夹拖到此处，或点击选择"
                    : AssetDatabase.GetAssetPath(_targetFolder),
                EditorStyles.helpBox
            );

            var evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    if (!AssetDatabase.IsValidFolder(path))
                        continue;
                    _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                    _previewDirty = true;
                    break;
                }
                evt.Use();
            }
            else if (evt.type == EventType.MouseDown)
            {
                string selected = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                        selected = "Assets" + selected[Application.dataPath.Length..];
                    _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(selected);
                    _previewDirty = true;
                }
                evt.Use();
            }
        }

        // ── 管线选择器 ───────────────────────────────────────────────────────
        private void DrawPipelineSelectors()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Width(position.width * 0.45f)
            );
            GUILayout.Label("待转换管线（来源）", EditorStyles.miniBoldLabel);
            int newSrc = GUILayout.SelectionGrid(
                _sourcePipelineIndex,
                new[] { "Standard", "URP", "HDRP", "Other" },
                1,
                EditorStyles.radioButton
            );
            if (newSrc != _sourcePipelineIndex)
            {
                _sourcePipelineIndex = newSrc;
                _previewDirty = true;
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.Label("→", GUILayout.Width(20));
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Width(position.width * 0.45f)
            );
            GUILayout.Label("目标管线", EditorStyles.miniBoldLabel);
            _targetPipelineIndex = GUILayout.SelectionGrid(
                _targetPipelineIndex,
                TargetPipelineOptions,
                1,
                EditorStyles.radioButton
            );
            if (_targetPipelineIndex == 3)
                _customTargetShader = EditorGUILayout.TextField("Shader 名称", _customTargetShader);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // ── 选项 ─────────────────────────────────────────────────────────────
        private void DrawOptions()
        {
            bool sub = EditorGUILayout.Toggle("包含子文件夹", _includeSubfolders);
            if (sub != _includeSubfolders)
            {
                _includeSubfolders = sub;
                _previewDirty = true;
            }
        }

        // ── 预览列表 ─────────────────────────────────────────────────────────
        private void DrawPreviewSection()
        {
            if (_previewDirty)
                RefreshPreview();

            GUILayout.Label(
                $"匹配到的材质（{_previewMaterials.Count} 个）",
                EditorStyles.miniBoldLabel
            );
            _previewScroll = EditorGUILayout.BeginScrollView(
                _previewScroll,
                EditorStyles.helpBox,
                GUILayout.Height(Mathf.Clamp(_previewMaterials.Count * 20 + 8, 60, 160))
            );

            if (_previewMaterials.Count == 0)
                GUILayout.Label("  — 无匹配材质 —", EditorStyles.centeredGreyMiniLabel);
            else
                foreach (var mat in _previewMaterials)
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);

            EditorGUILayout.EndScrollView();
        }

        // ── 操作按钮 ─────────────────────────────────────────────────────────
        private void DrawActionButtons()
        {
            string targetShaderName = GetTargetShaderName();
            bool canConvert =
                _targetFolder != null
                && _previewMaterials.Count > 0
                && !string.IsNullOrEmpty(targetShaderName)
                && Shader.Find(targetShaderName) != null;

            EditorGUI.BeginDisabledGroup(!canConvert);
            if (GUILayout.Button($"开始转换  →  {targetShaderName}", GUILayout.Height(32)))
                Convert(targetShaderName);
            EditorGUI.EndDisabledGroup();

            if (!canConvert && _targetFolder != null)
            {
                string hint = string.IsNullOrEmpty(targetShaderName)
                    ? "请输入目标 Shader 名称"
                    : $"找不到 Shader：{targetShaderName}";
                EditorGUILayout.HelpBox(hint, MessageType.Warning);
            }
        }

        // ── 日志区域 ─────────────────────────────────────────────────────────
        // 不能在静态字段初始化时访问 EditorStyles，延迟到 GUI 阶段
        private static GUIStyle _warnStyle;
        private static GUIStyle WarnStyle =>
            _warnStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.6f, 0f) },
            };

        private void DrawLog()
        {
            if (_log.Count == 0)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("转换日志", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("复制日志", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                var sb = new StringBuilder();
                foreach (var (text, _) in _log)
                    sb.AppendLine(text);
                GUIUtility.systemCopyBuffer = sb.ToString();
                Debug.Log("[MaterialConverter] 日志已复制到剪贴板");
            }
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(
                _logScroll,
                EditorStyles.helpBox,
                GUILayout.Height(120)
            );
            foreach (var (text, isWarning) in _log)
                GUILayout.Label(text, isWarning ? WarnStyle : EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        // ── 刷新预览 ─────────────────────────────────────────────────────────
        private void RefreshPreview()
        {
            _previewMaterials.Clear();
            _previewDirty = false;
            if (_targetFolder == null)
                return;

            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            string[] guids = AssetDatabase.FindAssets(
                "t:Material",
                _includeSubfolders ? new[] { folderPath } : null
            );

            bool isOther = _sourcePipelineIndex == 3; // Other

            // 预设管线的所有 Shader 名（用于 Other 模式排除）
            HashSet<string> allPresetShaders = null;
            string[] sourceShaders = null;
            if (isOther)
            {
                allPresetShaders = new HashSet<string>();
                foreach (var kv in ShaderPresets)
                foreach (var s in kv.Value)
                    allPresetShaders.Add(s);
            }
            else
            {
                sourceShaders = ShaderPresets[
                    new[] { "Standard", "URP", "HDRP" }[_sourcePipelineIndex]
                ];
            }

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (
                    !_includeSubfolders
                    && Path.GetDirectoryName(path)?.Replace('\\', '/') != folderPath
                )
                    continue;

                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null)
                    continue;

                if (isOther)
                {
                    // Other：不属于任何预设管线的 Shader
                    if (!allPresetShaders.Contains(mat.shader.name))
                        _previewMaterials.Add(mat);
                }
                else
                {
                    foreach (var s in sourceShaders)
                        if (mat.shader.name == s)
                        {
                            _previewMaterials.Add(mat);
                            break;
                        }
                }
            }
        }

        // ── 转换核心 ─────────────────────────────────────────────────────────
        private void Convert(string targetShaderName)
        {
            var targetShader = Shader.Find(targetShaderName);
            if (targetShader == null)
            {
                Debug.LogError($"[MaterialConverter] 找不到 Shader: {targetShaderName}");
                return;
            }

            _log.Clear();
            int success = 0,
                skip = 0,
                warnCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var mat in _previewMaterials)
                {
                    if (mat == null)
                    {
                        skip++;
                        continue;
                    }

                    // 1. 快照原有属性
                    var snapshot = SnapshotMaterial(mat);
                    string oldShaderName = mat.shader.name;

                    // 2. 切换 Shader（Unity 会重置所有属性）
                    mat.shader = targetShader;

                    // 3. 回写属性，收集警告
                    // Other 模式：自定义 Shader 的私有属性找不到目标属于正常，静默跳过未知属性
                    bool silentUnknown = _sourcePipelineIndex == 3;
                    var warnings = RestoreProperties(mat, snapshot, targetShader, silentUnknown);

                    EditorUtility.SetDirty(mat);
                    _log.Add(($"✓  {mat.name}  [{oldShaderName}  →  {targetShaderName}]", false));

                    foreach (var w in warnings)
                    {
                        _log.Add(
                            ($"   ⚠  {mat.name}: 属性 \"{w}\" 在目标 Shader 中不存在，已跳过", true)
                        );
                        Debug.LogWarning(
                            $"[MaterialConverter] {mat.name}: 属性 \"{w}\" 在目标 Shader 中不存在"
                        );
                        warnCount++;
                    }

                    success++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string summary =
                warnCount > 0
                    ? $"── 完成：{success} 个成功，{skip} 个跳过，{warnCount} 条属性警告 ──"
                    : $"── 完成：{success} 个成功，{skip} 个跳过 ──";
            _log.Add((summary, false));
            _previewDirty = true;
            Debug.Log($"[MaterialConverter] {summary}");
        }

        // ── 属性快照 ─────────────────────────────────────────────────────────
        private struct MatSnapshot
        {
            public Dictionary<string, Color> Colors;
            public Dictionary<string, Vector4> Vectors;
            public Dictionary<string, float> Floats;
            public Dictionary<string, int> Ints;
            public Dictionary<string, Texture> Textures;
            public Dictionary<string, Vector2> TextureOffsets;
            public Dictionary<string, Vector2> TextureScales;
            public RenderingMode RenderMode;
            public int RenderQueue;
        }

        private enum RenderingMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent,
        }

        private static MatSnapshot SnapshotMaterial(Material mat)
        {
            var snap = new MatSnapshot
            {
                Colors = new(),
                Vectors = new(),
                Floats = new(),
                Ints = new(),
                Textures = new(),
                TextureOffsets = new(),
                TextureScales = new(),
                RenderQueue = mat.renderQueue,
            };

            // 读取渲染模式（Standard 用 _Mode float 表示）
            if (mat.HasProperty("_Mode"))
                snap.RenderMode = (RenderingMode)(int)mat.GetFloat("_Mode");

            var shader = mat.shader;
            int count = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < count; i++)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);
                var propType = ShaderUtil.GetPropertyType(shader, i);

                switch (propType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        snap.Colors[propName] = mat.GetColor(propName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        snap.Vectors[propName] = mat.GetVector(propName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        snap.Floats[propName] = mat.GetFloat(propName);
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        snap.Textures[propName] = mat.GetTexture(propName);
                        snap.TextureOffsets[propName] = mat.GetTextureOffset(propName);
                        snap.TextureScales[propName] = mat.GetTextureScale(propName);
                        break;
                }
            }

            return snap;
        }

        // 这些属性在目标 Shader 中虽然同名存在，但只是兼容占位，
        // 必须强制走别名映射写入真正生效的属性
        private static readonly HashSet<string> ForceAliasProps = new()
        {
            "_Color", // Standard → URP: 必须写 _BaseColor 而非同名的兼容 _Color
            "_MainTex", // Standard → URP: 必须写 _BaseMap
        };

        // Standard 等 Shader 的内部控制属性，目标 Shader 没有时无需警告
        private static readonly HashSet<string> SilentSkipProps = new()
        {
            "_Mode",
            "_UVSec",
            "_SrcBlend",
            "_DstBlend",
            "_ZWrite",
            "_SmoothnessTextureChannel",
            "_SpecularHighlights",
            "_GlossyReflections",
            "_Surface",
            "_Blend",
            "_AlphaClip",
            "_ReceiveShadows",
            "_QueueOffset",
            "_QueueControl",
        };

        // ── 属性回写，返回无法迁移的属性名列表 ──────────────────────────────
        private static List<string> RestoreProperties(
            Material mat,
            MatSnapshot snap,
            Shader targetShader,
            bool silentUnknown = false
        )
        {
            var missing = new List<string>();

            // 构建目标 Shader 属性集合（名称 → 类型）
            int targetCount = ShaderUtil.GetPropertyCount(targetShader);
            var targetProps = new Dictionary<string, ShaderUtil.ShaderPropertyType>(targetCount);
            for (int i = 0; i < targetCount; i++)
                targetProps[ShaderUtil.GetPropertyName(targetShader, i)] =
                    ShaderUtil.GetPropertyType(targetShader, i);

            // Color 和 Vector 在底层都是 4 分量，互相兼容
            static bool IsColorOrVector(ShaderUtil.ShaderPropertyType t) =>
                t == ShaderUtil.ShaderPropertyType.Color
                || t == ShaderUtil.ShaderPropertyType.Vector;

            static bool IsFloatOrRange(ShaderUtil.ShaderPropertyType t) =>
                t == ShaderUtil.ShaderPropertyType.Float
                || t == ShaderUtil.ShaderPropertyType.Range;

            // 查找目标属性名：先同名，再别名；类型用 isCompatible 判断
            // ForceAliasProps 中的属性跳过同名匹配，直接走别名（避免写入兼容占位属性）
            string Resolve(string srcProp, Func<ShaderUtil.ShaderPropertyType, bool> isCompatible)
            {
                if (
                    !ForceAliasProps.Contains(srcProp)
                    && targetProps.TryGetValue(srcProp, out var t)
                    && isCompatible(t)
                )
                    return srcProp;
                if (PropAliases.TryGetValue(srcProp, out var aliases))
                    foreach (var alias in aliases)
                        if (targetProps.TryGetValue(alias, out var at) && isCompatible(at))
                            return alias;
                // ForceAlias 属性找不到别名时，回退到同名（总比丢失好）
                if (
                    ForceAliasProps.Contains(srcProp)
                    && targetProps.TryGetValue(srcProp, out var fallback)
                    && isCompatible(fallback)
                )
                    return srcProp;
                return null;
            }

            // 已处理的属性名（避免 Color/Vector 重复报告）
            var handled = new HashSet<string>();

            // 颜色（ShaderUtil 有时把 Color 报告为 Vector，统一用 Color/Vector 兼容匹配）
            foreach (var kv in snap.Colors)
            {
                handled.Add(kv.Key);
                var dst = Resolve(kv.Key, IsColorOrVector);
                if (dst != null)
                    mat.SetColor(dst, kv.Value);
                else if (
                    !SilentSkipProps.Contains(kv.Key)
                    && (!silentUnknown || PropAliases.ContainsKey(kv.Key))
                )
                    missing.Add(kv.Key);
            }

            // Vector（Color 已处理的跳过）
            foreach (var kv in snap.Vectors)
            {
                if (handled.Contains(kv.Key))
                    continue;
                handled.Add(kv.Key);
                var dst = Resolve(kv.Key, IsColorOrVector);
                // Vector4 可能是颜色，用 SetColor 写入以保留 HDR 信息
                if (dst != null)
                    mat.SetColor(dst, kv.Value);
                else if (
                    !SilentSkipProps.Contains(kv.Key)
                    && (!silentUnknown || PropAliases.ContainsKey(kv.Key))
                )
                    missing.Add(kv.Key);
            }

            // Float / Range
            foreach (var kv in snap.Floats)
            {
                var dst = Resolve(kv.Key, IsFloatOrRange);
                if (dst != null)
                    mat.SetFloat(dst, kv.Value);
                else if (
                    !SilentSkipProps.Contains(kv.Key)
                    && (!silentUnknown || PropAliases.ContainsKey(kv.Key))
                )
                    missing.Add(kv.Key);
            }

            // Int
            foreach (var kv in snap.Ints)
            {
                if (targetProps.ContainsKey(kv.Key))
                    mat.SetInt(kv.Key, kv.Value);
                else if (
                    !SilentSkipProps.Contains(kv.Key)
                    && (!silentUnknown || PropAliases.ContainsKey(kv.Key))
                )
                    missing.Add(kv.Key);
            }

            // 贴图
            foreach (var kv in snap.Textures)
            {
                var dst = Resolve(kv.Key, t => t == ShaderUtil.ShaderPropertyType.TexEnv);
                if (dst != null)
                {
                    mat.SetTexture(dst, kv.Value);
                    mat.SetTextureOffset(dst, snap.TextureOffsets[kv.Key]);
                    mat.SetTextureScale(dst, snap.TextureScales[kv.Key]);
                }
                else if (
                    !SilentSkipProps.Contains(kv.Key)
                    && (!silentUnknown || PropAliases.ContainsKey(kv.Key))
                )
                    missing.Add(kv.Key);
            }

            // 渲染队列
            mat.renderQueue = snap.RenderQueue;

            return missing;
        }

        private string GetTargetShaderName()
        {
            string key = TargetPipelineOptions[_targetPipelineIndex];
            if (key == "Custom")
                return _customTargetShader?.Trim();
            return DefaultTargetShader.TryGetValue(key, out var s) ? s : "";
        }
    }
}
