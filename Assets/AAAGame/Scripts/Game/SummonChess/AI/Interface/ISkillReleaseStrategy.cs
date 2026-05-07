/// <summary>
/// 技能释放策略接口
/// 职责：
/// 1. 判断何时应该释放技能1或大招
/// 2. 为技能选择目标（可能与普攻目标不同）
/// </summary>
public interface ISkillReleaseStrategy
{
    /// <summary>
    /// 初始化策略（传入棋子上下文）
    /// </summary>
    void Init(ChessContext context);

    /// <summary>
    /// 判断是否应该使用技能1
    /// </summary>
    /// <returns>true=应该使用技能1</returns>
    bool ShouldUseSkill1();

    /// <summary>
    /// 判断是否应该使用大招
    /// </summary>
    /// <returns>true=应该使用大招</returns>
    bool ShouldUseSkill2();

    /// <summary>
    /// 获取优先级最高的技能
    /// </summary>
    /// <returns>0=无技能, 1=技能1, 2=大招</returns>
    int GetPrioritySkill();

    /// <summary>
    /// 为技能选择目标
    /// 技能可以有不同的目标策略（与普攻目标可能不同）
    /// </summary>
    /// <param name="skillIndex">技能索引（1或2）</param>
    /// <returns>
    /// - ChessEntity：锁定的目标棋子
    /// - null：不需要锁定目标（如自我技能、全体技能，由技能自己处理）
    /// </returns>
    ChessEntity SelectSkillTarget(int skillIndex);
}
