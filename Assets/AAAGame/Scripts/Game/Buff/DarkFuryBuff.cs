using UnityEngine;

/// <summary>
/// 黑暗狂暴 Buff (ID=8)
/// 攻速+30%，持续5s
/// 由邪灵大招"黑暗爆发"触发
/// </summary>
public class DarkFuryBuff : BuffBase
{
    #region 私有字段

    private double m_AtkSpeedAdded;
    private bool m_IsApplied;

    #endregion

    #region 公共方法

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyFury();
        DebugEx.Log("DarkFuryBuff", $"黑暗狂暴生效，攻速提升: {m_AtkSpeedAdded:F3}");
    }

    public override void OnExit()
    {
        RestoreFury();
        base.OnExit();
        DebugEx.Log("DarkFuryBuff", "黑暗狂暴结束，攻速已还原");
    }

    public override bool OnStack()
    {
        // 不叠层，只刷新持续时间
        if (Config.Duration > 0)
            DurationRemain = (float)Config.Duration;
        return false;
    }

    #endregion

    #region 私有方法

    private void ApplyFury()
    {
        if (m_IsApplied || Ctx?.OwnerAttribute == null)
            return;

        m_AtkSpeedAdded = Ctx.OwnerAttribute.AtkSpeed * 0.3;
        Ctx.OwnerAttribute.ModifyAtkSpeed(m_AtkSpeedAdded);
        m_IsApplied = true;
    }

    private void RestoreFury()
    {
        if (!m_IsApplied || Ctx?.OwnerAttribute == null)
            return;

        Ctx.OwnerAttribute.ModifyAtkSpeed(-m_AtkSpeedAdded);
        m_AtkSpeedAdded = 0;
        m_IsApplied = false;
    }

    #endregion
}
