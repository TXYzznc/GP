using UnityEngine;

/// <summary>
/// Buff 应用辅助类
/// 统一处理 Buff 应用逻辑，支持单体和全体应用
/// </summary>
public static class BuffApplyHelper
{
    /// <summary>
    /// 应用 Buff 到目标
    /// </summary>
    /// <param name="buffId">Buff ID</param>
    /// <param name="target">目标实体（GameObject 或其他）</param>
    /// <param name="isGroupTarget">是否为全体目标</param>
    /// <param name="caster">施法者（可选）</param>
    public static void ApplyBuff(int buffId, GameObject target, bool isGroupTarget, GameObject caster = null)
    {
        if (target == null)
        {
            DebugEx.WarningModule("BuffApplyHelper", "目标为空，无法应用 Buff");
            return;
        }

        if (isGroupTarget)
        {
            ApplyBuffToGroup(buffId, target, caster);
        }
        else
        {
            ApplyBuffToSingle(buffId, target, caster);
        }
    }

    /// <summary>
    /// 应用 Buff 到单个目标
    /// </summary>
    private static void ApplyBuffToSingle(int buffId, GameObject target, GameObject caster)
    {
        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            DebugEx.WarningModule("BuffApplyHelper", $"目标 {target.name} 没有 BuffManager 组件");
            return;
        }

        buffManager.AddBuff(buffId, caster);
        DebugEx.LogModule("BuffApplyHelper", $"应用 Buff {buffId} 到单体: {target.name}");
    }

    /// <summary>
    /// 应用 Buff 到全体目标（同阵营的所有单位）
    /// </summary>
    private static void ApplyBuffToGroup(int buffId, GameObject targetRepresentative, GameObject caster)
    {
        // TODO: 实现获取同阵营所有单位的逻辑
        // 当前实现：仅应用到代表目标本身
        // 后续需要与战斗系统集成，获取完整的队伍列表
        //
        // 可能的实现思路：
        // 1. 获取目标所属的阵营/队伍 ID
        // 2. 查询战斗管理器获取该阵营的所有参战单位
        // 3. 逐个应用 Buff

        var buffManager = targetRepresentative.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            DebugEx.WarningModule("BuffApplyHelper", $"目标 {targetRepresentative.name} 没有 BuffManager 组件");
            return;
        }

        buffManager.AddBuff(buffId, caster);
        DebugEx.LogModule("BuffApplyHelper", $"应用 Buff {buffId} 到全体（当前仅代表目标）: {targetRepresentative.name}");
    }
}
