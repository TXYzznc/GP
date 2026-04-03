using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Buff 测试工具的使用示例
/// 展示各种常见的测试场景和用法
/// 注意：这只是示例代码，可以根据需要修改和扩展
/// </summary>
public class BuffTestExample : MonoBehaviour
{
    #region 测试方法

    /// <summary>
    /// 示例 1：快速应用单个 Buff
    /// </summary>
    public void Example1_ApplySingleBuff()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用攻击力增加 Buff
        BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

        // 验证
        DebugEx.LogModule("BuffTestExample", "✓ 示例 1 完成：已应用单个 Buff");
    }

    /// <summary>
    /// 示例 2：批量应用多个 Buff
    /// </summary>
    public void Example2_ApplyMultipleBuffs()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用多个 Buff：攻击+25% + 出血 + 速度+5%
        int[] buffIds = { 10101, 10102, 10106 };
        BuffTestTool.Instance.ApplyBuffs(buffIds, target);

        DebugEx.LogModule("BuffTestExample", $"✓ 示例 2 完成：已应用 {buffIds.Length} 个 Buff");
    }

    /// <summary>
    /// 示例 3：应用预设组合
    /// </summary>
    public void Example3_ApplyPreset()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用预设：伤害组合（攻击+ + 出血）
        BuffPresetManager.Instance.ApplyPreset("伤害组合", target);

        DebugEx.LogModule("BuffTestExample", "✓ 示例 3 完成：已应用预设 '伤害组合'");
    }

    /// <summary>
    /// 示例 4：验证 Buff 是否成功应用
    /// </summary>
    public void Example4_VerifyBuff()
    {
        var target = GetTestTarget();
        if (target == null) return;

        BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

        // 验证
        var result = BuffEffectVerifier.Instance.VerifyBuffApplied(10101, target);
        if (result.IsApplied)
        {
            DebugEx.LogModule("BuffTestExample", $"✓ {result.BuffName} 已成功应用，堆叠数: {result.StackCount}");
        }
        else
        {
            DebugEx.ErrorModule("BuffTestExample", $"✗ Buff 应用失败: {result.Message}");
        }
    }

    /// <summary>
    /// 示例 5：查看目标的所有 Buff
    /// </summary>
    public void Example5_ListAllBuffs()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 先应用一些 Buff
        BuffPresetManager.Instance.ApplyPreset("伤害组合", target);

        // 获取并显示所有 Buff
        var buffDetails = BuffEffectVerifier.Instance.GetBuffDetails(target);
        var report = new System.Text.StringBuilder();
        report.AppendLine($"目标 {target.name} 的所有 Buff：");
        foreach (var buff in buffDetails)
        {
            report.AppendLine($"  • {buff.Name} (ID={buff.BuffId}, 堆叠={buff.StackCount})");
        }

        DebugEx.LogModule("BuffTestExample", report.ToString());
    }

    /// <summary>
    /// 示例 6：查看目标属性变化
    /// </summary>
    public void Example6_CheckAttributes()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用攻击 Buff
        BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

        // 获取属性
        var attr = BuffEffectVerifier.Instance.GetTargetAttributes(target);
        var report = new System.Text.StringBuilder();
        report.AppendLine($"目标 {target.name} 的属性：");
        report.AppendLine($"  HP: {attr.HP:F0}/{attr.MaxHP:F0}");
        report.AppendLine($"  MP: {attr.MP:F0}/{attr.MaxMP:F0}");
        report.AppendLine($"  攻击力: {attr.AtkDamage:F0}");
        report.AppendLine($"  防御力: {attr.PhysDef:F0}");
        report.AppendLine($"  速度: {attr.Speed:F0}");

        DebugEx.LogModule("BuffTestExample", report.ToString());
    }

    /// <summary>
    /// 示例 7：检查控制状态
    /// </summary>
    public void Example7_CheckControlStates()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用控制 Buff：眩晕 + 冰冻
        BuffPresetManager.Instance.ApplyPreset("控制组合", target);

        // 检查控制状态
        var controls = BuffEffectVerifier.Instance.GetControlStates(target);
        if (controls.Count > 0)
        {
            DebugEx.LogModule("BuffTestExample", $"✓ 目标处于以下控制状态: {string.Join(", ", controls)}");
        }
        else
        {
            DebugEx.LogModule("BuffTestExample", "✓ 目标没有控制状态");
        }
    }

    /// <summary>
    /// 示例 8：测试 Buff 叠加
    /// </summary>
    public void Example8_TestBuffStacking()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用相同的 Buff 3 次
        for (int i = 0; i < 3; i++)
        {
            BuffTestTool.Instance.ApplyBuffToTarget(10101, target);
        }

        // 验证堆叠
        var buff = BuffTestTool.Instance.GetBuff(10101, target);
        if (buff != null)
        {
            DebugEx.LogModule("BuffTestExample", $"✓ Buff 堆叠成功，当前堆叠数: {buff.StackCount}");
        }
    }

    /// <summary>
    /// 示例 9：移除指定 Buff
    /// </summary>
    public void Example9_RemoveBuff()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用 Buff
        BuffTestTool.Instance.ApplyBuffToTarget(10101, target);
        BuffTestTool.Instance.ApplyBuffToTarget(10102, target);

        // 移除一个 Buff
        bool removed = BuffTestTool.Instance.RemoveBuffFromTarget(10101, target);
        if (removed)
        {
            DebugEx.LogModule("BuffTestExample", "✓ 已移除 Buff (ID=10101)");
        }

        // 验证剩余 Buff
        var buffs = BuffTestTool.Instance.GetTargetBuffs(target);
        DebugEx.LogModule("BuffTestExample", $"✓ 目标剩余 {buffs.Count} 个 Buff");
    }

    /// <summary>
    /// 示例 10：清空所有 Buff 并生成报告
    /// </summary>
    public void Example10_ClearAndReport()
    {
        var target = GetTestTarget();
        if (target == null) return;

        // 应用多个 Buff
        BuffPresetManager.Instance.ApplyPreset("伤害组合", target);
        BuffPresetManager.Instance.ApplyPreset("防守组合", target);

        // 生成报告（清空前）
        var reportBefore = BuffEffectVerifier.Instance.GenerateTestReport(target);
        DebugEx.LogModule("BuffTestExample", "【清空前的报告】\n" + reportBefore);

        // 清空所有 Buff
        BuffTestTool.Instance.ClearAllBuffs(target);

        // 生成报告（清空后）
        var reportAfter = BuffEffectVerifier.Instance.GenerateTestReport(target);
        DebugEx.LogModule("BuffTestExample", "【清空后的报告】\n" + reportAfter);
    }

    /// <summary>
    /// 示例 11：自动化测试所有 Buff
    /// </summary>
    public void Example11_TestAllBuffs()
    {
        var target = GetTestTarget();
        if (target == null) return;

        var allBuffs = BuffTestTool.Instance.GetAllAvailableBuffs();
        int successCount = 0;
        int failCount = 0;

        foreach (var buff in allBuffs)
        {
            BuffTestTool.Instance.ApplyBuffToTarget(buff.BuffId, target);
            var result = BuffEffectVerifier.Instance.VerifyBuffApplied(buff.BuffId, target);

            if (result.IsApplied)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            BuffTestTool.Instance.RemoveBuffFromTarget(buff.BuffId, target);
        }

        DebugEx.LogModule("BuffTestExample",
            $"✓ 自动化测试完成: 成功={successCount}, 失败={failCount}, 总计={allBuffs.Count}");
    }

    /// <summary>
    /// 示例 12：创建自定义预设
    /// </summary>
    public void Example12_CreateCustomPreset()
    {
        // 创建自定义预设
        int[] myBuffs = { 10101, 10105, 10301 };
        BuffPresetManager.Instance.AddPreset("超级组合", myBuffs);

        var target = GetTestTarget();
        if (target == null) return;

        // 应用自定义预设
        BuffPresetManager.Instance.ApplyPreset("超级组合", target);

        DebugEx.LogModule("BuffTestExample", "✓ 已创建并应用自定义预设 '超级组合'");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取测试目标（从场景中找第一个棋子）
    /// </summary>
    private GameObject GetTestTarget()
    {
        var entity = FindObjectOfType<ChessEntity>();
        if (entity == null)
        {
            DebugEx.ErrorModule("BuffTestExample", "场景中没有找到任何棋子实体");
            return null;
        }

        return entity.gameObject;
    }

    /// <summary>
    /// 运行所有示例
    /// </summary>
    public void RunAllExamples()
    {
        DebugEx.LogModule("BuffTestExample", "=== 开始运行所有示例 ===");

        Example1_ApplySingleBuff();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example2_ApplyMultipleBuffs();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example3_ApplyPreset();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example4_VerifyBuff();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example5_ListAllBuffs();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example6_CheckAttributes();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example7_CheckControlStates();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example8_TestBuffStacking();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example9_RemoveBuff();
        BuffTestTool.Instance.ClearAllBuffs(GetTestTarget());

        Example10_ClearAndReport();
        Example11_TestAllBuffs();
        Example12_CreateCustomPreset();

        DebugEx.LogModule("BuffTestExample", "=== 所有示例运行完成 ===");
    }

    #endregion

    #region Unity 生命周期

    private void Update()
    {
        // 按 E 键运行所有示例（用于快速测试）
        if (Input.GetKeyDown(KeyCode.E))
        {
            RunAllExamples();
        }

        // 按 1-9 快速运行对应的示例
        if (Input.GetKeyDown(KeyCode.Alpha1)) Example1_ApplySingleBuff();
        if (Input.GetKeyDown(KeyCode.Alpha2)) Example2_ApplyMultipleBuffs();
        if (Input.GetKeyDown(KeyCode.Alpha3)) Example3_ApplyPreset();
        if (Input.GetKeyDown(KeyCode.Alpha4)) Example4_VerifyBuff();
        if (Input.GetKeyDown(KeyCode.Alpha5)) Example5_ListAllBuffs();
        if (Input.GetKeyDown(KeyCode.Alpha6)) Example6_CheckAttributes();
        if (Input.GetKeyDown(KeyCode.Alpha7)) Example7_CheckControlStates();
        if (Input.GetKeyDown(KeyCode.Alpha8)) Example8_TestBuffStacking();
        if (Input.GetKeyDown(KeyCode.Alpha9)) Example9_RemoveBuff();
    }

    #endregion
}
