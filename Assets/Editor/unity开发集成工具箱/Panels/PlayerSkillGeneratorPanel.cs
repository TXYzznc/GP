using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 玩家技能脚本生成器面板
/// </summary>
[ToolHubItem("通用工具/自定义脚本生成器", "一键生成脚本和对应配置", 60)]
public class PlayerSkillGeneratorPanel : IToolHubPanel
{
    private const string PrefKeySkillPath = "SkillGen.SkillPath";
    private const string PrefKeyParamPath = "SkillGen.ParamPath";

    private string skillName = "";

    // 默认路径（相对 Assets）
    private string skillScriptFolder = @"Assets\AAAGame\Scripts\Game\Player\PlayerSkill\Skills";
    private string paramScriptFolder = @"Assets\AAAGame\Scripts\Game\Player\PlayerSkill\SkillsSO";

    public void OnEnable()
    {
        skillScriptFolder = EditorPrefs.GetString(PrefKeySkillPath, skillScriptFolder);
        paramScriptFolder = EditorPrefs.GetString(PrefKeyParamPath, paramScriptFolder);
    }

    public void OnDisable()
    {
        EditorPrefs.SetString(PrefKeySkillPath, skillScriptFolder);
        EditorPrefs.SetString(PrefKeyParamPath, paramScriptFolder);
    }

    public void OnDestroy() { }

    public string GetHelpText() => "说明：\n1. 输入技能名称（如 Dash）。\n2. 自动生成 Skill 逻辑类和 ParamSO 配置类。\n3. 类名会自动转换为 PascalCase 格式。";

    public void OnGUI()
    {
        EditorGUILayout.LabelField("配置生成器", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            skillName = EditorGUILayout.TextField("技能名称 (Skill Name)", skillName);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("输出路径 (Output Paths):", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                skillScriptFolder = EditorGUILayout.TextField("逻辑脚本文件夹", skillScriptFolder);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var selected = EditorUtility.OpenFolderPanel("选择技能脚本文件夹", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selected))
                        skillScriptFolder = ToAssetsRelativePath(selected);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                paramScriptFolder = EditorGUILayout.TextField("数据类(SO)文件夹", paramScriptFolder);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var selected = EditorUtility.OpenFolderPanel("选择 SO 脚本文件夹", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selected))
                        paramScriptFolder = ToAssetsRelativePath(selected);
                }
            }
        }

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(skillName)))
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 执行生成", GUILayout.Height(40)))
            {
                Generate();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void Generate()
    {
        string baseName = ToPascalIdentifier(skillName);
        if (string.IsNullOrEmpty(baseName))
        {
            EditorUtility.DisplayDialog("错误", "技能名无法转换为合法类名，请换一个名称。", "OK");
            return;
        }

        string skillClassName = baseName + "Skill";
        string paramClassName = baseName + "ParamSO";

        string skillFolderAbs = ToAbsolutePath(skillScriptFolder);
        string paramFolderAbs = ToAbsolutePath(paramScriptFolder);

        try
        {
            Directory.CreateDirectory(skillFolderAbs);
            Directory.CreateDirectory(paramFolderAbs);

            string skillFileAbs = Path.Combine(skillFolderAbs, $"{skillClassName}.cs");
            string paramFileAbs = Path.Combine(paramFolderAbs, $"{paramClassName}.cs");

            if (!ConfirmOverwriteIfExists(skillFileAbs) || !ConfirmOverwriteIfExists(paramFileAbs))
                return;

            string skillCode = BuildSkillTemplate(skillClassName, paramClassName);
            string paramCode = BuildParamTemplate(paramClassName, baseName);

            File.WriteAllText(skillFileAbs, skillCode, new UTF8Encoding(true));
            File.WriteAllText(paramFileAbs, paramCode, new UTF8Encoding(true));

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"已生成：\n- {skillClassName}.cs\n- {paramClassName}.cs", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("生成失败", e.Message, "OK");
        }
    }

    private static bool ConfirmOverwriteIfExists(string fileAbs)
    {
        if (!File.Exists(fileAbs)) return true;
        return EditorUtility.DisplayDialog("文件已存在", $"文件 {Path.GetFileName(fileAbs)} 已存在，是否覆盖？", "覆盖", "取消");
    }

    private static string BuildSkillTemplate(string skillClassName, string paramClassName)
    {
        return $@"using UnityEngine;

public class {skillClassName} : IPlayerSkill
{{
    public int SkillId => common.Id;

    private PlayerSkillContext ctx;
    private SkillCommonConfig common;
    private float cdRemain;

    private {paramClassName} param;

    public void Init(PlayerSkillContext ctx, SkillCommonConfig common, SkillParamSO _param)
    {{
        this.ctx = ctx;
        this.common = common;

        param = _param as {paramClassName};
        if (param == null)
            Debug.LogError($""{skillClassName} missing {paramClassName} for skillId={{common.Id}}"");
    }}

    public void Tick(float dt)
    {{
        if (cdRemain > 0f) cdRemain -= dt;
    }}

    public bool TryCast()
    {{
        if (cdRemain > 0f) return false;

        cdRemain = common.Cooldown;
        return true;
    }}
}}
";
    }

    private static string BuildParamTemplate(string paramClassName, string menuSkillName)
    {
        return $@"using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName=""Skills/Params/{menuSkillName}"")]
public class {paramClassName} : SkillParamSO
{{
    
}}
";
    }

    private static string ToAssetsRelativePath(string folderAbs)
    {
        folderAbs = folderAbs.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (!folderAbs.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)) return "Assets";
        return "Assets" + folderAbs.Substring(dataPath.Length);
    }

    private static string ToAbsolutePath(string assetsRelative)
    {
        string rel = assetsRelative.Replace('\\', '/');
        if (!rel.StartsWith("Assets", StringComparison.OrdinalIgnoreCase)) throw new Exception($"路径必须以 Assets/ 开头: {assetsRelative}");
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        return Path.Combine(projectRoot, rel);
    }

    private static string ToPascalIdentifier(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var sb = new StringBuilder();
        bool newWord = true;
        foreach (char c in raw.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(newWord ? char.ToUpperInvariant(c) : c);
                newWord = false;
            }
            else newWord = true;
        }
        string s = sb.ToString();
        if (s.Length > 0 && char.IsDigit(s[0])) s = "_" + s;
        return s;
    }
}