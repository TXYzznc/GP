using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class PlayerSkillGeneratorWindow : EditorWindow
{
    private const string PrefKeySkillPath = "SkillGen.SkillPath";
    private const string PrefKeyParamPath = "SkillGen.ParamPath";

    private string skillName = "";

    // 默认路径（相对 Assets）
    private string skillScriptFolder = @"Assets\AAAGame\Scripts\Game\Player\PlayerSkill\Skills";
    private string paramScriptFolder = @"Assets\AAAGame\Scripts\Game\Player\PlayerSkill\SkillsSO";

    //[MenuItem("工具/Skill Generator")]
    public static void Open()
    {
        var win = GetWindow<PlayerSkillGeneratorWindow>("PlayerSkill Generator");
        win.minSize = new Vector2(640, 260);
        win.Show();
    }

    private void OnEnable()
    {
        skillScriptFolder = EditorPrefs.GetString(PrefKeySkillPath, skillScriptFolder);
        paramScriptFolder = EditorPrefs.GetString(PrefKeyParamPath, paramScriptFolder);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(PrefKeySkillPath, skillScriptFolder);
        EditorPrefs.SetString(PrefKeyParamPath, paramScriptFolder);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Generate Skill Script + SkillParamSO Script", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        skillName = EditorGUILayout.TextField("Skill Name (技能名)", skillName);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output Paths (相对 Assets):", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            skillScriptFolder = EditorGUILayout.TextField("Skill Scripts Folder", skillScriptFolder);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Skill Script Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                    skillScriptFolder = ToAssetsRelativePath(selected);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            paramScriptFolder = EditorGUILayout.TextField("ParamSO Scripts Folder", paramScriptFolder);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select ParamSO Script Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                    paramScriptFolder = ToAssetsRelativePath(selected);
            }
        }

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(skillName)))
        {
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
            {
                Generate();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "说明：\n" +
            "1) 输入技能名，会自动生成：技能名+Skill、技能名+ParamSO\n" +
            "2) 会自动把技能名转换为合法 C# 类名（去空格/符号，首字母大写）\n" +
            "3) 若文件已存在会提示是否覆盖",
            MessageType.Info);
    }

    private void Generate()
    {
        string baseName = ToPascalIdentifier(skillName);
        if (string.IsNullOrEmpty(baseName))
        {
            EditorUtility.DisplayDialog("Error", "技能名无法转换为合法类名，请换一个。", "OK");
            return;
        }

        string skillClassName = baseName + "Skill";
        string paramClassName = baseName + "ParamSO";

        // 生成路径
        string skillFolderAbs = ToAbsolutePath(skillScriptFolder);
        string paramFolderAbs = ToAbsolutePath(paramScriptFolder);

        Directory.CreateDirectory(skillFolderAbs);
        Directory.CreateDirectory(paramFolderAbs);

        string skillFileAbs = Path.Combine(skillFolderAbs, $"{skillClassName}.cs");
        string paramFileAbs = Path.Combine(paramFolderAbs, $"{paramClassName}.cs");

        // 写入前检查覆盖
        if (!ConfirmOverwriteIfExists(skillFileAbs) || !ConfirmOverwriteIfExists(paramFileAbs))
            return;

        // 生成内容
        string skillCode = BuildSkillTemplate(skillClassName, paramClassName);
        string paramCode = BuildParamTemplate(paramClassName, baseName);

        File.WriteAllText(skillFileAbs, skillCode, new UTF8Encoding(true));
        File.WriteAllText(paramFileAbs, paramCode, new UTF8Encoding(true));

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            $"已生成：\n- {skillScriptFolder}/{skillClassName}.cs\n- {paramScriptFolder}/{paramClassName}.cs",
            "OK");
    }

    private static bool ConfirmOverwriteIfExists(string fileAbs)
    {
        if (!File.Exists(fileAbs)) return true;

        return EditorUtility.DisplayDialog(
            "File Exists",
            $"文件已存在，是否覆盖？\n{fileAbs}",
            "Overwrite", "Cancel");
    }

    // ========== Templates ==========

    private static string BuildSkillTemplate(string skillClassName, string paramClassName)
    {
        // 按你给的模板生成（仅做占位符替换）
        // 注意：原模板有 “return false;;” 我这里修正成一个分号，避免风格问题
        return
$@"using UnityEngine;

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
        // 你给的模板里带了 System.Collections.Generic；虽然没用，但这里按原样保留
        return
$@"using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName=""Skills/Params/{menuSkillName}"")]
public class {paramClassName} : SkillParamSO
{{
    
}}
";
    }

    // ========== Path Helpers ==========

    private static string ToAssetsRelativePath(string folderAbs)
    {
        folderAbs = folderAbs.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');

        if (!folderAbs.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
        {
            // 不在 Assets 下，强制返回默认
            return "Assets";
        }

        return "Assets" + folderAbs.Substring(dataPath.Length);
    }

    private static string ToAbsolutePath(string assetsRelative)
    {
        // 允许用户填 \ 或 /
        string rel = assetsRelative.Replace('\\', '/');

        if (!rel.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Path must start with Assets/: {assetsRelative}");

        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        string abs = Path.Combine(projectRoot, rel);
        return abs;
    }

    // ========== Naming Helpers ==========

    /// <summary>
    /// 将用户输入转换为 PascalCase 的合法 C# 标识符
    /// 示例： "dash skill" -> "DashSkill"（这里仅返回基名 DashSkill 的前半段由你输入决定）
    /// </summary>
    private static string ToPascalIdentifier(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // 只保留字母数字，其他当作分隔符
        var sb = new StringBuilder();
        bool newWord = true;

        foreach (char c in raw.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(newWord ? char.ToUpperInvariant(c) : c);
                newWord = false;
            }
            else
            {
                newWord = true;
            }
        }

        string s = sb.ToString();

        // C# 标识符不能以数字开头
        if (s.Length > 0 && char.IsDigit(s[0]))
            s = "_" + s;

        return s;
    }
}