/// <summary>
/// 后羿技能释放策略
/// 技能1（神力）：自我技能，返回 null
/// 大招（分裂箭）：锁定综合状态最好的敌人
/// </summary>
public class HouyiSkillReleaseStrategy : DefaultSkillReleaseStrategy
{
    public override ChessEntity SelectSkillTarget(int skillIndex)
    {
        if (m_Context == null || m_Context.Entity == null)
            return null;

        switch (skillIndex)
        {
            case 1:
                // 技能1：自我技能，无需目标
                return null;

            case 2:
                // 大招：锁定综合状态最好的敌人（HP%+MP%）
                return ChessTargetFinder.FindBestStateEnemy(m_Context.Entity);

            default:
                return null;
        }
    }
}
