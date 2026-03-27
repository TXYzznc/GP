using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 后羿被动：日月长弓 (ID=11)
/// 白天伤害+15%，射程+30
/// 通过属性修正 StatModBuff(ID=5) 实现
/// </summary>
public class HouyiPassive : IChessPassive
{
    #region 接口实现

    public int PassiveId => m_Config?.Id ?? 0;  // ⭐ SkillId 改为 Id

    #endregion

    #region 私有字段

    private ChessContext m_Ctx;
    private SummonChessSkillTable m_Config;  // ⭐ 修改类型
    private bool m_IsDay = true; // TODO: 对接昼夜系统

    /// <summary>是否已应用白天增益</summary>
    private bool m_DayBuffApplied;

    #endregion

    #region 公共方法

    public void Init(ChessContext ctx, SummonChessSkillTable config)  // ⭐ 修改参数类型
    {
        m_Ctx = ctx;
        m_Config = config;

        // 初始化时根据昼夜状态应用被动
        CheckAndApplyDayBuff();

        DebugEx.LogModule("HouyiPassive", "日月长弓被动初始化完成");
    }

    public void Tick(float dt)
    {
        // 每帧检测昼夜变化（后续对接昼夜系统后可优化为事件驱动）
        // bool currentIsDay = DayNightSystem.IsDay; // TODO
        // if (currentIsDay != m_IsDay)
        // {
        //     m_IsDay = currentIsDay;
        //     CheckAndApplyDayBuff();
        // }
    }

    public void Dispose()
    {
        // 移除白天增益Buff
        if (m_DayBuffApplied && m_Ctx?.BuffManager != null)
        {
            m_Ctx.BuffManager.RemoveBuff(5); // 日月长弓Buff ID=5
            m_DayBuffApplied = false;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检查并应用白天增益
    /// </summary>
    private void CheckAndApplyDayBuff()
    {
        if (m_Ctx?.BuffManager == null) return;

        if (m_IsDay && !m_DayBuffApplied)
        {
            // 白天：添加日月长弓Buff（伤害+15%，射程+30）
            m_Ctx.BuffManager.AddBuff(5, m_Ctx.Owner, m_Ctx.Attribute);
            m_DayBuffApplied = true;
            DebugEx.LogModule("HouyiPassive", "白天，日月长弓增益生效");
        }
        else if (!m_IsDay && m_DayBuffApplied)
        {
            // 夜晚：移除日月长弓Buff
            m_Ctx.BuffManager.RemoveBuff(5);
            m_DayBuffApplied = false;
            DebugEx.LogModule("HouyiPassive", "夜晚，日月长弓增益移除");
        }
    }

    #endregion
}
