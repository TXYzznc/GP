/// <summary>
/// 召唤师被动技能抽象基类
/// 内置 m_IsActive 状态标记，仅在状态变化时触发 OnActivate/OnDeactivate
/// </summary>
public abstract class SummonerPassiveBase : ISummonerPassive
{
    protected SummonerSkillContext m_Ctx;
    protected SummonerSkillTable m_Config;

    /// <summary>当前激活状态（子类可读，基类管理）</summary>
    protected bool m_IsActive;

    public int PassiveId => m_Config?.Id ?? 0;

    public virtual void Init(SummonerSkillContext ctx, SummonerSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;
        m_IsActive = false;
    }

    public void Tick(float dt)
    {
        OnTick(dt);
    }

    public void Dispose()
    {
        OnDispose();
        m_IsActive = false;
    }

    /// <summary>子类实现：每帧逻辑（条件检测、Buff 管理）</summary>
    protected abstract void OnTick(float dt);

    /// <summary>子类实现：销毁时清理所有已施加的 Buff</summary>
    protected abstract void OnDispose();
}
