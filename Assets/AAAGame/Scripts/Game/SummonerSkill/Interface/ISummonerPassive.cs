/// <summary>
/// 召唤师被动技能接口
/// </summary>
public interface ISummonerPassive
{
    /// <summary>被动 ID（对应 SummonerSkillTable.Id）</summary>
    int PassiveId { get; }

    /// <summary>初始化，传入运行时上下文与配置表行</summary>
    void Init(SummonerSkillContext ctx, SummonerSkillTable config);

    /// <summary>每帧驱动（条件检测、状态切换）</summary>
    void Tick(float dt);

    /// <summary>战斗结束时调用，移除所有已施加的 Buff，清理引用</summary>
    void Dispose();
}
