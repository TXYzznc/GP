/// <summary>
/// 默认技能释放策略
/// 技能1：满足条件（冷却结束 + 法力足够）立即释放
/// 大招：满足条件（冷却结束 + 法力足够）立即释放
/// 优先级：大招 > 技能1
/// </summary>
public class DefaultSkillReleaseStrategy : ISkillReleaseStrategy
{
    protected ChessContext m_Context;

    #region 接口实现

    public virtual void Init(ChessContext context)
    {
        m_Context = context;
        
        DebugEx.LogModule("DefaultSkillReleaseStrategy",
            $"{context.Entity.Config.Name} 使用默认技能释放策略");
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
        // 默认优先级：大招 > 技能1
        if (ShouldUseSkill2())
        {
            DebugEx.LogModule("DefaultSkillReleaseStrategy",
                $"{m_Context.Entity.Config.Name} 决策: 使用大招");
            return 2;
        }

        if (ShouldUseSkill1())
        {
            DebugEx.LogModule("DefaultSkillReleaseStrategy",
                $"{m_Context.Entity.Config.Name} 决策: 使用技能1");
            return 1;
        }

        return 0; // 无技能可用
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
