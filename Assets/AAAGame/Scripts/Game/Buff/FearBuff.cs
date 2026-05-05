using UnityEngine;

/// <summary>
/// 恐惧 Buff (ID=9)
/// 降低目标20%攻击力，持续2s
/// 由邪灵被动"黑暗侵染"触发
/// </summary>
public class FearBuff : BuffBase
{
    #region 私有字段

    private double m_AtkDamageReduced;
    private bool m_IsApplied;

    #endregion

    #region 公共方法

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyFear();
        DebugEx.Log("FearBuff", $"恐惧Buff生效，攻击力降低: {m_AtkDamageReduced:F1}");
    }

    public override void OnExit()
    {
        RestoreFear();
        base.OnExit();
        DebugEx.Log("FearBuff", "恐惧Buff结束，攻击力已还原");
    }

    public override bool OnStack()
    {
        // 恐惧不叠层，只刷新持续时间
        if (Config.Duration > 0)
            DurationRemain = (float)Config.Duration;
        return false;
    }

    #endregion

    #region 私有方法

    private void ApplyFear()
    {
        if (m_IsApplied || Ctx?.OwnerAttribute == null)
            return;

        m_AtkDamageReduced = Ctx.OwnerAttribute.AtkDamage * 0.2;
        Ctx.OwnerAttribute.ModifyAtkDamage(-m_AtkDamageReduced);
        m_IsApplied = true;
    }

    private void RestoreFear()
    {
        if (!m_IsApplied || Ctx?.OwnerAttribute == null)
            return;

        Ctx.OwnerAttribute.ModifyAtkDamage(m_AtkDamageReduced);
        m_AtkDamageReduced = 0;
        m_IsApplied = false;
    }

    #endregion
}
