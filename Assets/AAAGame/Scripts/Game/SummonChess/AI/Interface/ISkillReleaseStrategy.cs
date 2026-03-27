/// <summary>
/// 技能释放策略接口
/// 负责判断何时应该释放技能1或大招
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
}
