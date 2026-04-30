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
            DebugEx.Error(nameof(BuffTestCompilationFix), "BuffEffectVerifier 创建失败");
            return;
        }

        var target = FindObjectOfType<ChessEntity>();
        if (target == null)
        {
            DebugEx.Warning(nameof(BuffTestCompilationFix), "场景中没有 ChessEntity，跳过验证");
            return;
        }

        // 测试获取属性信息
        var attrInfo = verifier.GetTargetAttributes(target.gameObject);
        DebugEx.Log(nameof(BuffTestCompilationFix), $"✓ BuffEffectVerifier 正常工作");
        DebugEx.Log(nameof(BuffTestCompilationFix), $"  目标: {attrInfo.Name}");
        DebugEx.Log(nameof(BuffTestCompilationFix), $"  HP: {attrInfo.HP}/{attrInfo.MaxHP}");
        DebugEx.Log(nameof(BuffTestCompilationFix), $"  MP: {attrInfo.MP}/{attrInfo.MaxMP}");
    }

    /// <summary>
    /// 验证 BuffTestTool 能否正常创建
    /// </summary>
    public void VerifyBuffTestTool()
    {
        var tool = BuffTestTool.Instance;
        if (tool == null)
        {
            DebugEx.Error(nameof(BuffTestCompilationFix), "BuffTestTool 创建失败");
            return;
        }

        // 测试获取所有 Buff
        var buffs = tool.GetAllAvailableBuffs();
        DebugEx.Log(nameof(BuffTestCompilationFix), $"✓ BuffTestTool 正常工作，共 {buffs.Count} 个可用 Buff");
    }

    /// <summary>
    /// 验证 BuffPresetManager 能否正常创建
    /// </summary>
    public void VerifyBuffPresetManager()
    {
        var manager = BuffPresetManager.Instance;
        if (manager == null)
        {
            DebugEx.Error(nameof(BuffTestCompilationFix), "BuffPresetManager 创建失败");
            return;
        }

        var presets = manager.GetAllPresets();
        DebugEx.Log(nameof(BuffTestCompilationFix), $"✓ BuffPresetManager 正常工作，共 {presets.Count} 个预设");
    }

    /// <summary>
    /// 运行所有验证
    /// </summary>
    public void RunAllVerifications()
    {
        DebugEx.Log(nameof(BuffTestCompilationFix), "========== 开始编译验证 ==========");

        VerifyBuffTestTool();
        VerifyBuffPresetManager();
        VerifyBuffEffectVerifier();

        DebugEx.Log(nameof(BuffTestCompilationFix), "========== 编译验证完成 ==========");
    }
}
