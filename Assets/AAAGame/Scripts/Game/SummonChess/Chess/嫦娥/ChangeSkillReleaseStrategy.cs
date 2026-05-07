/// <summary>
/// 嫦娥技能释放策略
/// 技能1（传送）：当前普攻目标
/// 大招（冰冻）：选择半径为（施法范围+4）的范围内，敌人最密集的位置
/// </summary>
public class ChangeSkillReleaseStrategy : DefaultSkillReleaseStrategy
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
                // 大招：敌人最密集的位置
                // 返回 null，让技能自己处理位置选择
                // （技能会调用 ChessTargetFinder.FindMostDensePosition()）
                return null;

            default:
                return null;
        }
    }
}
