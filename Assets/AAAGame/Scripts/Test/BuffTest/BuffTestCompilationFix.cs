using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 编译检查辅助脚本
/// 用于验证所有编译错误是否已修复
/// </summary>
public class BuffTestCompilationFix : MonoBehaviour
{
    /// <summary>
    /// 验证 BuffEffectVerifier 能否正常创建
    /// </summary>
    public void VerifyBuffEffectVerifier()
    {
        var verifier = BuffEffectVerifier.Instance;
        if (verifier == null)
        {
            DebugEx.ErrorModule("CompilationFix", "BuffEffectVerifier 创建失败");
            return;
        }

        var target = FindObjectOfType<ChessEntity>();
        if (target == null)
        {
            DebugEx.WarningModule("CompilationFix", "场景中没有 ChessEntity，跳过验证");
            return;
        }

        // 测试获取属性信息
        var attrInfo = verifier.GetTargetAttributes(target.gameObject);
        DebugEx.LogModule("CompilationFix", $"✓ BuffEffectVerifier 正常工作");
        DebugEx.LogModule("CompilationFix", $"  目标: {attrInfo.Name}");
        DebugEx.LogModule("CompilationFix", $"  HP: {attrInfo.HP}/{attrInfo.MaxHP}");
        DebugEx.LogModule("CompilationFix", $"  MP: {attrInfo.MP}/{attrInfo.MaxMP}");
    }

    /// <summary>
    /// 验证 BuffTestTool 能否正常创建
    /// </summary>
    public void VerifyBuffTestTool()
    {
        var tool = BuffTestTool.Instance;
        if (tool == null)
        {
            DebugEx.ErrorModule("CompilationFix", "BuffTestTool 创建失败");
            return;
        }

        // 测试获取所有 Buff
        var buffs = tool.GetAllAvailableBuffs();
        DebugEx.LogModule("CompilationFix", $"✓ BuffTestTool 正常工作，共 {buffs.Count} 个可用 Buff");
    }

    /// <summary>
    /// 验证 BuffPresetManager 能否正常创建
    /// </summary>
    public void VerifyBuffPresetManager()
    {
        var manager = BuffPresetManager.Instance;
        if (manager == null)
        {
            DebugEx.ErrorModule("CompilationFix", "BuffPresetManager 创建失败");
            return;
        }

        var presets = manager.GetAllPresets();
        DebugEx.LogModule("CompilationFix", $"✓ BuffPresetManager 正常工作，共 {presets.Count} 个预设");
    }

    /// <summary>
    /// 运行所有验证
    /// </summary>
    public void RunAllVerifications()
    {
        DebugEx.LogModule("CompilationFix", "========== 开始编译验证 ==========");

        VerifyBuffTestTool();
        VerifyBuffPresetManager();
        VerifyBuffEffectVerifier();

        DebugEx.LogModule("CompilationFix", "========== 编译验证完成 ==========");
    }
}
