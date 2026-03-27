#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 项目健康检查面板 - 可集成到工具箱，也可独立使用
/// </summary>
[ToolHubItem("诊断工具/项目体检", "全面的项目健康检查，扫描多种常见问题", 10)]
public class ProjectHealthCheckPanel : IToolHubPanel
{
    private Vector2 scrollPosition;

    private readonly List<ResultItem> missingScriptInOpenScenes = new List<ResultItem>();
    private readonly List<ResultItem> missingScriptInAssetsYaml = new List<ResultItem>();
    private readonly List<ResultItem> findFirstObjectByTypeUsages = new List<ResultItem>();
    private readonly List<ResultItem> customEditorCamera = new List<ResultItem>();
    private readonly List<ResultItem> runtimeScriptsUnderEditorFolder = new List<ResultItem>();

    private bool scanOpenScenes = true;
    private bool scanAssetsYaml = true;
    private bool scanScripts = true;

    private const string MissingScriptYamlToken = "m_Script: {fileID: 0";

    private class ResultItem
    {
        public string Title;
        public string Path;
        public string AssetPath;
        public UnityEngine.Object PingObject;
    }

    public void OnEnable()
    {
        // 初始化列表（已在声明时初始化）
    }

    public void OnDisable()
    {
        missingScriptInOpenScenes.Clear();
        missingScriptInAssetsYaml.Clear();
        findFirstObjectByTypeUsages.Clear();
        customEditorCamera.Clear();
        runtimeScriptsUnderEditorFolder.Clear();
    }

    public void OnDestroy()
    {
        missingScriptInOpenScenes.Clear();
        missingScriptInAssetsYaml.Clear();
        findFirstObjectByTypeUsages.Clear();
        customEditorCamera.Clear();
        runtimeScriptsUnderEditorFolder.Clear();
    }

    public string GetHelpText()
    {
        return "项目健康检查工具。扫描缺失脚本、API兼容性问题和代码组织问题。";
    }

    public void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Project Health Check（项目体检）", EditorStyles.boldLabel);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("扫描项", EditorStyles.boldLabel);
            scanOpenScenes = EditorGUILayout.ToggleLeft("扫描当前打开的场景（层级遍历，能给出对象路径）", scanOpenScenes);
            scanAssetsYaml = EditorGUILayout.ToggleLeft("扫描 Assets 中的 Scene/Prefab YAML（速度快，给出资源路径）", scanAssetsYaml);
            scanScripts = EditorGUILayout.ToggleLeft("扫描脚本潜在问题（API/自定义Editor/Editor文件夹）", scanScripts);
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("开始扫描", GUILayout.Height(32)))
        {
            Scan();
        }

        EditorGUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawSection(
            "① 当前打开场景里的 Missing Script（最推荐先修）",
            missingScriptInOpenScenes,
            "解决：选中对象 -> Inspector 删除 Missing (Mono Script) 组件；或还原丢失脚本/重新挂载。");

        DrawSection(
            "② 资源 YAML 中疑似 Missing Script（Scene/Prefab）",
            missingScriptInAssetsYaml,
            "解决：双击定位资源；打开场景/Prefab 后删除 Missing Script 组件。");

        DrawSection(
            "③ FindFirstObjectByType 使用点（Unity < 2022 可能编译失败）",
            findFirstObjectByTypeUsages,
            "解决：Unity 2021 及以下请改用 FindObjectOfType / Resources.FindObjectsOfTypeAll 等替代。");

        DrawSection(
            "④ 可能干扰 Camera 的自定义 Editor（CustomEditor(typeof(Camera)))",
            customEditorCamera,
            "解决：检查该脚本是否必要；确保放在 Editor 文件夹；避免与 URP/内置 CameraEditor 冲突。");

        DrawSection(
            "⑤ Editor 文件夹里疑似\"运行时脚本\"（继承 MonoBehaviour/ScriptableObject）",
            runtimeScriptsUnderEditorFolder,
            "解决：运行时代码不要放 Editor 文件夹；把它们移到普通 Scripts 目录。");

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope("box"))
        {
            EditorGUILayout.LabelField("当前 Unity 版本：", GUILayout.Width(110));
            EditorGUILayout.LabelField(Application.unityVersion);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("复制报告到剪贴板", GUILayout.Width(140)))
            {
                EditorGUIUtility.systemCopyBuffer = BuildTextReport();
                Debug.Log("Project Health Check: 报告已复制到剪贴板。");
            }
        }
    }

    private void Scan()
    {
        missingScriptInOpenScenes.Clear();
        missingScriptInAssetsYaml.Clear();
        findFirstObjectByTypeUsages.Clear();
        customEditorCamera.Clear();
        runtimeScriptsUnderEditorFolder.Clear();

        try
        {
            if (scanOpenScenes) ScanOpenScenesForMissingScripts();
            if (scanAssetsYaml) ScanAssetsYamlForMissingScripts();
            if (scanScripts) ScanScriptsForCommonIssues();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        Debug.Log(BuildTextReport());
    }

    private void ScanOpenScenesForMissingScripts()
    {
        int sceneCount = SceneManager.sceneCount;

        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                ScanGameObjectTreeForMissingScripts(scene.name, root.transform, missingScriptInOpenScenes);
            }
        }
    }

    private void ScanGameObjectTreeForMissingScripts(string sceneName, Transform t, List<ResultItem> results)
    {
        var comps = t.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                results.Add(new ResultItem
                {
                    Title = $"[Scene: {sceneName}] Missing Script",
                    Path = GetTransformPath(t),
                    AssetPath = null,
                    PingObject = t.gameObject
                });
            }
        }

        for (int c = 0; c < t.childCount; c++)
            ScanGameObjectTreeForMissingScripts(sceneName, t.GetChild(c), results);
    }

    private static string GetTransformPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private void ScanAssetsYamlForMissingScripts()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!File.Exists(path)) continue;
            if (LooksLikeMissingScriptYaml(path))
            {
                missingScriptInAssetsYaml.Add(new ResultItem
                {
                    Title = "Scene YAML contains m_Script fileID:0 (Missing Script)",
                    Path = null,
                    AssetPath = path,
                    PingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!File.Exists(path)) continue;
            if (LooksLikeMissingScriptYaml(path))
            {
                missingScriptInAssetsYaml.Add(new ResultItem
                {
                    Title = "Prefab YAML contains m_Script fileID:0 (Missing Script)",
                    Path = null,
                    AssetPath = path,
                    PingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }
        }
    }

    private bool LooksLikeMissingScriptYaml(string assetPath)
    {
        try
        {
            string text = File.ReadAllText(assetPath);
            return text.Contains(MissingScriptYamlToken);
        }
        catch
        {
            return false;
        }
    }

    private void ScanScriptsForCommonIssues()
    {
        string[] scriptGuids = AssetDatabase.FindAssets("t:Script");

        var rxFindFirst = new Regex(@"\bFindFirstObjectByType\s*<|\bFindFirstObjectByType\s*\(", RegexOptions.Compiled);
        var rxCustomEditorCamera = new Regex(@"\[CustomEditor\s*\(\s*typeof\s*\(\s*(UnityEngine\.)?Camera\s*\)\s*\)\s*\]", RegexOptions.Compiled);
        var rxRuntimeType = new Regex(@":\s*(MonoBehaviour|ScriptableObject)\b", RegexOptions.Compiled);

        bool unityMayNotSupportFindFirstObjectByType = IsUnityVersionLessThan2022(Application.unityVersion);

        foreach (string guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            string text;
            try { text = File.ReadAllText(path); }
            catch { continue; }

            if (unityMayNotSupportFindFirstObjectByType && rxFindFirst.IsMatch(text))
            {
                findFirstObjectByTypeUsages.Add(new ResultItem
                {
                    Title = "FindFirstObjectByType usage (Unity < 2022 risk)",
                    AssetPath = path,
                    PingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }

            if (rxCustomEditorCamera.IsMatch(text))
            {
                customEditorCamera.Add(new ResultItem
                {
                    Title = "CustomEditor(typeof(Camera)) detected",
                    AssetPath = path,
                    PingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }

            if (path.Replace("\\", "/").Contains("/Editor/") && rxRuntimeType.IsMatch(text))
            {
                runtimeScriptsUnderEditorFolder.Add(new ResultItem
                {
                    Title = "Runtime-type script under /Editor/ (MonoBehaviour/ScriptableObject)",
                    AssetPath = path,
                    PingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }
        }
    }

    private static bool IsUnityVersionLessThan2022(string unityVersion)
    {
        try
        {
            var m = Regex.Match(unityVersion, @"^(?<major>\d+)\.(?<minor>\d+)");
            if (!m.Success) return false;
            int major = int.Parse(m.Groups["major"].Value);
            return major < 2022;
        }
        catch
        {
            return false;
        }
    }

    private void DrawSection(string title, List<ResultItem> items, string hint)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(hint, MessageType.Info);

            if (items.Count == 0)
            {
                EditorGUILayout.LabelField("未发现问题。");
                return;
            }

            EditorGUILayout.LabelField($"发现 {items.Count} 条：");

            foreach (var it in items)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("定位", GUILayout.Width(48)))
                    {
                        if (it.PingObject != null)
                        {
                            EditorGUIUtility.PingObject(it.PingObject);
                            Selection.activeObject = it.PingObject;
                        }
                        else if (!string.IsNullOrEmpty(it.AssetPath))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(it.AssetPath);
                            if (obj != null)
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                            }
                        }
                    }

                    string line = it.Title;
                    if (!string.IsNullOrEmpty(it.AssetPath)) line += $" | {it.AssetPath}";
                    if (!string.IsNullOrEmpty(it.Path)) line += $" | {it.Path}";
                    EditorGUILayout.SelectableLabel(line, GUILayout.Height(16));
                }
            }
        }
    }

    private string BuildTextReport()
    {
        return
            "===== Project Health Check Report =====\n" +
            $"Unity: {Application.unityVersion}\n" +
            $"OpenScene Missing Scripts: {missingScriptInOpenScenes.Count}\n" +
            $"Assets YAML Missing Scripts: {missingScriptInAssetsYaml.Count}\n" +
            $"FindFirstObjectByType usages: {findFirstObjectByTypeUsages.Count}\n" +
            $"CustomEditor(Camera): {customEditorCamera.Count}\n" +
            $"Runtime scripts under Editor/: {runtimeScriptsUnderEditorFolder.Count}\n" +
            "======================================\n";
    }
}
#endif
