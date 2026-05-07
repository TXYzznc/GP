/// <summary>
/// 恶魂技能释放策略
/// 技能1（暗影斩）：当前普攻目标
/// 大招（暗影冲击）：当前普攻目标方向（大招是向前方范围内造成冲击）
/// </summary>
public class EvilSoulSkillReleaseStrategy : DefaultSkillReleaseStrategy
{
    public override ChessEntity SelectSkillTarget(int skillIndex)
    {
        if (m_Context == null || m_Context.Entity == null)
            return null;

        switch (skillIndex)
        {
            case 1:
                // 技能1：当前普攻目标
                var aiBase = m_Context.Entity.AI as ChessAIBase;
                return aiBase != null ? aiBase.CurrentTarget : null;

            case 2:
                // 大招：当前普攻目标（用来确定方向）
                // 恶魂大招是向前方范围内造成冲击，方向是当前普攻的目标方向
                aiBase = m_Context.Entity.AI as ChessAIBase;
                return aiBase != null ? aiBase.CurrentTarget : null;

            default:
                return null;
        }
    }
}
