/// <summary>
/// 召唤师主动技能接口
/// </summary>
public interface ISummonerSkill
{
    /// <summary>技能 ID（对应 SummonerSkillTable.Id）</summary>
    int SkillId { get; }

    /// <summary>初始化，传入运行时上下文与配置表行</summary>
    void Init(SummonerSkillContext ctx, SummonerSkillTable config);

    /// <summary>每帧更新（冷却倒计时等）</summary>
    void Tick(float dt);

    /// <summary>尝试释放技能；内部判断 CanCast，成功执行并返回 true，否则返回 false</summary>
    bool TryCast();

    /// <summary>是否可以释放（冷却、灵力、特殊条件）</summary>
    bool CanCast();

    /// <summary>剩余冷却时间（秒），0 表示可释放</summary>
    float GetCooldownRemaining();
}
