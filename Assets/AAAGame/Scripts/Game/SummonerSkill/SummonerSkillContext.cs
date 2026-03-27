/// <summary>
/// 召唤师技能运行时上下文
/// 包含技能运行所需的所有外部依赖引用
/// </summary>
public class SummonerSkillContext
{
    /// <summary>召唤师运行时数据（HP / 灵力访问与修改）</summary>
    public SummonerRuntimeDataManager RuntimeData;

    /// <summary>战斗实体追踪器（获取全体友方/敌方棋子）</summary>
    public CombatEntityTracker EntityTracker;
}
