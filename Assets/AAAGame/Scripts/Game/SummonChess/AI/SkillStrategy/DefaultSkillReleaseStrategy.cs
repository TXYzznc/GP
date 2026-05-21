using UnityEngine;

/// <summary>
/// 默认技能释放策略
/// 基于欲望值（Desire Value）的动态优先级：
/// DesireValue = Strength × min(TimeSinceLastCast / MaxWaitSeconds, 1.5)
/// 选择欲望值最高的可释放技能
/// </summary>
public class DefaultSkillReleaseStrategy : ISkillReleaseStrategy
{
    protected ChessContext m_Context;

    /// <summary>
    /// 欲望值上限倍数（防止数值无限增长）
    /// 默认1.5表示最大欲望值 = Strength × 1.5
    /// 可在子类中覆盖
    /// </summary>
    protected virtual float DesireValueCap => 1.5f;

    #region 接口实现

    public virtual void Init(ChessContext context)
    {
        m_Context = context;

        DebugEx.Log(
            "DefaultSkillReleaseStrategy",
            $"{context.Entity.Config.Name} 使用默认技能释放策略"
        );
    }

    public virtual bool ShouldUseSkill1()
    {
        // 检查技能1是否存在
        if (m_Context.Entity.Skill1 == null)
            return false;

        // 直接使用 CanCast() 检查所有条件
        // CanCast() 会检查：激活状态、冷却时间、法力值等
        return m_Context.Entity.Skill1.CanCast();
    }

    public virtual bool ShouldUseSkill2()
    {
        // 检查大招是否存在
        if (m_Context.Entity.Skill2 == null)
            return false;

        // 直接使用 CanCast() 检查所有条件
        return m_Context.Entity.Skill2.CanCast();
    }

    public virtual int GetPrioritySkill()
    {
        float currentTime = Time.time;

        float skill1Desire =
            (m_Context.Entity.Skill1?.CanCast() == true)
                ? m_Context.Entity.Skill1.GetDesireValue(currentTime)
                : 0f;
        float skill1_2Desire =
            (m_Context.Entity.Skill1_2?.CanCast() == true)
                ? m_Context.Entity.Skill1_2.GetDesireValue(currentTime)
                : 0f;
        float skill2Desire =
            (m_Context.Entity.Skill2?.CanCast() == true)
                ? m_Context.Entity.Skill2.GetDesireValue(currentTime)
                : 0f;

        DebugEx.Log(
            "DefaultSkillReleaseStrategy",
            $"{m_Context.Entity.Config.Name} 欲望值对比: 技能1={skill1Desire:F2}, 技能二={skill1_2Desire:F2}, 大招={skill2Desire:F2}"
        );

        // 选择欲望值最高的技能（大招 > 技能二 > 技能一）
        if (skill2Desire > skill1Desire && skill2Desire > skill1_2Desire && skill2Desire > 0)
        {
            DebugEx.Log(
                "DefaultSkillReleaseStrategy",
                $"{m_Context.Entity.Config.Name} 决策: 释放大招"
            );
            return 2;
        }

        if (skill1_2Desire > skill1Desire && skill1_2Desire > 0)
        {
            DebugEx.Log(
                "DefaultSkillReleaseStrategy",
                $"{m_Context.Entity.Config.Name} 决策: 释放技能二"
            );
            return 3;
        }

        if (skill1Desire > 0)
        {
            DebugEx.Log(
                "DefaultSkillReleaseStrategy",
                $"{m_Context.Entity.Config.Name} 决策: 释放技能1"
            );
            return 1;
        }

        return 0;
    }

    public virtual ChessEntity SelectSkillTarget(int skillIndex)
    {
        // 默认策略：所有技能都使用普攻目标
        // 子类可以重写来实现不同的目标选择策略
        if (m_Context == null || m_Context.Entity == null)
            return null;

        var aiBase = m_Context.Entity.AI as ChessAIBase;
        return aiBase != null ? aiBase.CurrentTarget : null;
    }

    #endregion

    #region 辅助方法（供子类使用）

    /// <summary>
    /// 获取当前血量百分比
    /// </summary>
    protected double GetHpPercent()
    {
        return m_Context.Entity.Attribute.CurrentHp / m_Context.Entity.Attribute.MaxHp;
    }

    /// <summary>
    /// 获取当前法力值百分比
    /// </summary>
    protected double GetMpPercent()
    {
        return m_Context.Entity.Attribute.CurrentMp / m_Context.Entity.Attribute.MaxMp;
    }

    /// <summary>
    /// 检查目标是否有效
    /// </summary>
    protected bool IsTargetValid(ChessEntity target)
    {
        return target != null && !target.Attribute.IsDead;
    }

    #endregion
}
