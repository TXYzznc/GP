using UnityEngine;

/// <summary>
/// 后羿技能一：神力 (ID=13)
/// 自身攻击附带灼烧伤害，攻击力+25%
/// 持续8秒
/// </summary>
public class HouyiSkill1 : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 3; // 主动技能

    #endregion

    #region 私有字段

    private bool m_IsActive;

    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        m_IsActive = false;

        DebugEx.LogModule("HouyiSkill1", "烈焰箭技能初始化完成");
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        // ⭐ 修改：Buff 的持续时间由 BuffTable 的 Duration 控制，无需手动计时
    }

    public override bool CanCast()
    {
        if (m_IsActive) return false; // 已激活时不可重复释放
        return base.CanCast();
    }

    public override bool TryCast()
    {
        if (!base.TryCast()) return false;

        // 激活技能效果
        ActivateSkill();

        return true;
    }

    /// <summary>
    /// 执行技能完整流程
    /// 神力技能是自身增益，只需播放特效
    /// </summary>
    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.ErrorModule("HouyiSkill1", "ExecuteSkill: caster 为 null");
            return;
        }

        // 播放技能释放特效
        PlaySkillEffect(caster);

        DebugEx.LogModule("HouyiSkill1", "神力技能执行完成（自身增益，无需命中检测）");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 激活技能效果
    /// ⭐ 修改：Buff 的持续时间由 BuffTable 的 Duration 控制，无需手动管理
    /// </summary>
    private void ActivateSkill()
    {
        m_IsActive = true;

        // 添加烈焰箭状态Buff（攻击力+25%，攻击附带灼烧）
        // Buff 会根据 BuffTable 中的 Duration (8秒) 自动移除，无需手动控制
        if (m_Ctx?.BuffManager != null)
        {
            m_Ctx.BuffManager.AddBuff(4, m_Ctx.Owner, m_Ctx.Attribute); // 烈焰箭Buff ID=4
        }

        DebugEx.LogModule("HouyiSkill1",
            $"烈焰箭激活! 攻击力+25%，攻击附带灼烧");
    }

    #endregion
}
