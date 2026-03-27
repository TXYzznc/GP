/// <summary>
/// 召唤师主动技能抽象基类
/// 处理冷却倒计时、灵力检查、TryCast 调用链
/// 子类只需实现 ExecuteSkill() 和可选的 CanCast() 扩展
/// </summary>
public abstract class SummonerSkillBase : ISummonerSkill
{
    protected SummonerSkillContext m_Ctx;
    protected SummonerSkillTable m_Config;
    private float m_Cooldown;

    public int SkillId => m_Config?.Id ?? 0;

    public virtual void Init(SummonerSkillContext ctx, SummonerSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;
        m_Cooldown = 0f;
    }

    public void Tick(float dt)
    {
        if (m_Cooldown > 0f)
            m_Cooldown -= dt;
    }

    public virtual bool CanCast()
    {
        if (m_Cooldown > 0f)
            return false;
        if (m_Config.SpiritCost > 0f && m_Ctx.RuntimeData.CurrentMP < m_Config.SpiritCost)
            return false;
        return true;
    }

    public bool TryCast()
    {
        if (!CanCast())
            return false;

        if (m_Config.SpiritCost > 0f)
            m_Ctx.RuntimeData.ConsumeMP(m_Config.SpiritCost);

        m_Cooldown = m_Config.Cooldown;
        ExecuteSkill();
        return true;
    }

    public float GetCooldownRemaining() => m_Cooldown > 0f ? m_Cooldown : 0f;

    /// <summary>子类实现：技能实际效果</summary>
    protected abstract void ExecuteSkill();
}
