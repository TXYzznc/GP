using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 冰霜 Buff (ID=2)
/// 降低目标15%移速和5%攻速
/// 如果叠加灼烧状态时，触发融化效果
/// </summary>
public class FrostBuff : BuffBase
{
    #region 常量

    private const double MOVE_SPEED_REDUCE_RATIO = 0.15;
    private const double ATK_SPEED_REDUCE_RATIO = 0.05;

    #endregion

    #region 私有字段

    private double m_MoveSpeedReduced;
    private double m_AtkSpeedReduced;
    private bool m_IsSlowApplied;

    #endregion

    #region 公共方法

    public override void OnEnter()
    {
        base.OnEnter();
        ApplySlow();
        CheckAndTriggerMelt();
    }

    public override void OnExit()
    {
        RestoreSlow();
        base.OnExit();
    }

    public override bool OnStack()
    {
        // 冰霜不叠层，只刷新持续时间
        if (Config.Duration > 0)
        {
            DurationRemain = (float)Config.Duration;
        }

        // 重新计算减速，属性可能已变化
        RestoreSlow();
        ApplySlow();
        CheckAndTriggerMelt();

        return true;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 应用减速效果
    /// </summary>
    private void ApplySlow()
    {
        if (Ctx?.OwnerAttribute == null) return;

        m_MoveSpeedReduced = Ctx.OwnerAttribute.MoveSpeed * MOVE_SPEED_REDUCE_RATIO;
        m_AtkSpeedReduced = Ctx.OwnerAttribute.AtkSpeed * ATK_SPEED_REDUCE_RATIO;

        Ctx.OwnerAttribute.ModifyMoveSpeed(-m_MoveSpeedReduced);
        Ctx.OwnerAttribute.ModifyAtkSpeed(-m_AtkSpeedReduced);
        m_IsSlowApplied = true;

        DebugEx.LogModule("FrostBuff", $"冰霜生效: 移速-{m_MoveSpeedReduced:F1} 攻速-{m_AtkSpeedReduced:F3}");
    }

    /// <summary>
    /// 恢复减速效果
    /// </summary>
    private void RestoreSlow()
    {
        if (!m_IsSlowApplied || Ctx?.OwnerAttribute == null) return;

        Ctx.OwnerAttribute.ModifyMoveSpeed(m_MoveSpeedReduced);
        Ctx.OwnerAttribute.ModifyAtkSpeed(m_AtkSpeedReduced);
        m_IsSlowApplied = false;
        m_MoveSpeedReduced = 0;
        m_AtkSpeedReduced = 0;
    }

    /// <summary>
    /// 检查并触发融化效果
    /// </summary>
    private void CheckAndTriggerMelt()
    {
        if (Ctx?.OwnerBuffManager == null) return;

        // 检查目标是否有灼烧
        var burnBuff = Ctx.OwnerBuffManager.GetBuff(1) as BurnBuff;
        if (burnBuff != null && burnBuff.StackCount > 0)
        {
            DebugEx.LogModule("FrostBuff", $"检测到灼烧{burnBuff.StackCount}层，触发融化!");

            // 添加融化Buff（融化施加者属性，用于计算法强伤害）
            Ctx.OwnerBuffManager.AddBuff(3, Ctx.Caster, Ctx.CasterAttribute);
        }
    }

    #endregion
}
