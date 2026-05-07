/// <summary>
/// 黑暗杨戬技能释放策略
/// 技能1（天威圣戟·黑暗）：范围清盾技能，在自身周围释放
/// 大招（堕落劈山）：范围大招，在自身周围释放
/// 两个技能都是范围技能，不需要选择特定目标
/// </summary>
public class DarkYangyuanSkillReleaseStrategy : DefaultSkillReleaseStrategy
{
    public override ChessEntity SelectSkillTarget(int skillIndex)
    {
        // 黑暗杨戬的技能都是范围技能（在自身周围释放）
        // 返回 null 表示不需要选择特定目标，AI 会在自身位置释放
        // 技能1 和大招都是这样的机制
        return null;
    }
}
