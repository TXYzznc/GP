using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 嫦娥被动：九天玄冰 (ID=21)
/// 1. 敌人攻击嫦娥时，攻击者受到减速减攻（Buff ID=6）
/// 2. 嫦娥在夜晚法强+40（通过 Buff ID=28 实现）
/// 3. 嫦娥的攻击附带冰霜效果，远程攻击效果，普通攻击回复法力值
/// </summary>
public class ChangePassive : IChessPassive
{
    #region 接口实现

    public int PassiveId => m_Config?.Id ?? 0;

    #endregion

    #region 私有字段

    private ChessContext m_Ctx;
    private SummonChessSkillTable m_Config;
    private bool m_IsNight = false; // TODO: 对接昼夜系统

    /// <summary>是否已应用夜晚法强加成Buff</summary>
    private bool m_NightBuffApplied;

    #endregion

    #region 公共方法

    public void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;

        // 检查并应用夜晚法强加成
        CheckAndApplyNightBonus();

        DebugEx.Log(nameof(ChangePassive), "九天玄冰被动初始化完成");
    }

    public void Tick(float dt)
    {
        // TODO: 对接昼夜系统后启用动态切换
        // bool currentIsNight = DayNightSystem.IsNight;
        // if (currentIsNight != m_IsNight)
        // {
        //     m_IsNight = currentIsNight;
        //     CheckAndApplyNightBonus();
        // }
    }

    public void Dispose()
    {
        // 移除夜晚法强加成Buff
        if (m_NightBuffApplied && m_Ctx?.BuffManager != null)
        {
            m_Ctx.BuffManager.RemoveBuff(28); // 九天玄冰·夜晚加成 ID=28
            m_NightBuffApplied = false;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检查并应用夜晚法强加成
    /// </summary>
    private void CheckAndApplyNightBonus()
    {
        if (m_Ctx?.BuffManager == null)
            return;

        if (m_IsNight && !m_NightBuffApplied)
        {
            m_Ctx.BuffManager.AddBuff(28, m_Ctx.Owner, m_Ctx.Attribute); // 九天玄冰·夜晚加成 ID=28
            m_NightBuffApplied = true;
            DebugEx.Log(nameof(ChangePassive), "夜晚，法强+40");
        }
        else if (!m_IsNight && m_NightBuffApplied)
        {
            m_Ctx.BuffManager.RemoveBuff(28);
            m_NightBuffApplied = false;
            DebugEx.Log(nameof(ChangePassive), "白天，法强加成移除");
        }
    }

    #endregion
}
