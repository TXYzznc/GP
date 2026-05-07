/// <summary>
/// 恶魂被动：虚无之躯 (ID=41)
/// 物理伤害减免30%，虚无之物难以被伤害
/// 通过 StatModBuff(ID=10) 实现
/// </summary>
public class EvilSoulPassive : IChessPassive
{
    #region 接口实现

    public int PassiveId => m_Config?.Id ?? 0;

    #endregion

    #region 私有字段

    private ChessContext m_Ctx;
    private SummonChessSkillTable m_Config;

    /// <summary>是否已应用虚无之躯Buff</summary>
    private bool m_BuffApplied;

    #endregion

    #region 公共方法

    public void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;

        // 初始化时应用虚无之躯Buff
        ApplyPassiveBuff();

        DebugEx.Log("EvilSoulPassive", "虚无之躯被动初始化完成");
    }

    public void Tick(float dt) { }

    public void Dispose()
    {
        // 移除虚无之躯Buff
        if (m_BuffApplied && m_Ctx?.BuffManager != null)
        {
            m_Ctx.BuffManager.RemoveBuff(10); // 虚无之躯Buff ID=10
            m_BuffApplied = false;
        }
    }

    #endregion

    #region 私有方法

    private void ApplyPassiveBuff()
    {
        if (m_Ctx?.BuffManager == null)
            return;

        m_Ctx.BuffManager.AddBuff(10, m_Ctx.Owner, m_Ctx.Attribute); // 虚无之躯Buff ID=10
        m_BuffApplied = true;

        DebugEx.Log("EvilSoulPassive", "虚无之躯被动生效，物理伤害减免30%");
    }

    #endregion
}
