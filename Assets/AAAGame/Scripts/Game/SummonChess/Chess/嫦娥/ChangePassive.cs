using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 嫦娥被动：月之守护 (ID=21)
/// 1. 敌人攻击嫦娥时，攻击者受到30%减速和15%减攻（Buff ID=6）
/// 2. 嫦娥在夜晚法强+40
/// 3. 嫦娥的攻击附带冰霜效果，远程攻击效果，普通攻击回复法力值
/// </summary>
public class ChangePassive : IChessPassive
{
    #region 接口实现

    public int PassiveId => m_Config?.Id ?? 0;  // ⭐ 第 13 行：SkillId 改为 Id

    #endregion

    #region 常量

    /// <summary>夜晚法强加成</summary>
    private const double NIGHT_SPELL_POWER_BONUS = 40;

    #endregion

    #region 私有字段

    private ChessContext m_Ctx;
    private SummonChessSkillTable m_Config;  // ⭐ 第 25 行：修改类型
    private bool m_IsNight = false; // TODO: 对接昼夜系统
    private bool m_NightBonusApplied;
    private bool m_EventRegistered;

    #endregion

    #region 公共方法

    public void Init(ChessContext ctx, SummonChessSkillTable config)  // ⭐ 第 34 行：修改参数类型
    {
        m_Ctx = ctx;
        m_Config = config;

        // 注册受伤事件，受伤时自动施加减益
        if (m_Ctx?.Attribute != null)
        {
            m_Ctx.Attribute.OnDamageTaken += OnDamageTaken;
            m_EventRegistered = true;
        }

        // 检查夜晚加成
        CheckNightBonus();

        DebugEx.LogModule("ChangePassive", "月之守护被动初始化完成");
    }

    public void Tick(float dt)
    {
        // 检测昼夜（后续对接昼夜系统后可优化为事件驱动）
        // bool currentIsNight = DayNightSystem.IsNight;
        // if (currentIsNight != m_IsNight)
        // {
        //     m_IsNight = currentIsNight;
        //     CheckNightBonus();
        // }
    }

    public void Dispose()
    {
        // 取消事件监听
        if (m_EventRegistered && m_Ctx?.Attribute != null)
        {
            m_Ctx.Attribute.OnDamageTaken -= OnDamageTaken;
            m_EventRegistered = false;
        }

        // 移除夜晚法强加成
        if (m_NightBonusApplied && m_Ctx?.Attribute != null)
        {
            m_Ctx.Attribute.ModifySpellPower(-NIGHT_SPELL_POWER_BONUS);
            m_NightBonusApplied = false;
        }
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 受伤时触发，对攻击者施加减速减攻 (Buff ID=6)
    /// </summary>
    private void OnDamageTaken(double damage, bool isMagic)
    {
        if (damage <= 0) return;

        // 理想情况：受伤事件应该携带攻击者信息
        // 注意：当前可能 OnDamageTaken 事件不携带攻击者信息
        // 需要通过 SummonChessManager 获取最近的攻击者，或扩展事件参数
        // 暂时使用简实现，通过 SummonChessManager 查询最近的敌方单位

        // TODO: 优化 - 扩展 TakeDamage 方法传入攻击者引用
        // 目前先记录日志，待战斗系统完善后补充
        DebugEx.LogModule("ChangePassive", "嫦娥受到攻击，应该对攻击者施加减益(Buff ID=6)");
    }

    /// <summary>
    /// 对指定攻击者施加减速减攻
    /// 供外部调用，由战斗系统传入攻击者引用时
    /// </summary>
    public void ApplyFrostToAttacker(ChessEntity attacker)
    {
        if (attacker == null || attacker.BuffManager == null) return;
        if (m_Ctx?.Attribute == null) return;

        attacker.BuffManager.AddBuff(6, m_Ctx.Owner, m_Ctx.Attribute); // 冰霜减益 ID=6
        DebugEx.LogModule("ChangePassive", $"月之守护对攻击者 {attacker.Config?.Name} 施加减益");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检查并应用夜晚法强加成
    /// </summary>
    private void CheckNightBonus()
    {
        if (m_Ctx?.Attribute == null) return;

        if (m_IsNight && !m_NightBonusApplied)
        {
            m_Ctx.Attribute.ModifySpellPower(NIGHT_SPELL_POWER_BONUS);
            m_NightBonusApplied = true;
            DebugEx.LogModule("ChangePassive", "夜晚，法强+40");
        }
        else if (!m_IsNight && m_NightBonusApplied)
        {
            m_Ctx.Attribute.ModifySpellPower(-NIGHT_SPELL_POWER_BONUS);
            m_NightBonusApplied = false;
            DebugEx.LogModule("ChangePassive", "白天，法强加成移除");
        }
    }

    #endregion
}
