using UnityEngine;

/// <summary>
/// 邪灵被动：黑暗侵染 (ID=31)
/// 每次普攻命中后，有30%概率对目标施加"恐惧"Buff（降低20%攻击力，持续2s）
/// </summary>
public class EvilSpiritPassive : IChessPassive
{
    #region 接口实现

    public int PassiveId => m_Config?.Id ?? 0;

    #endregion

    #region 常量

    private const float FEAR_TRIGGER_CHANCE = 0.3f;

    #endregion

    #region 私有字段

    private ChessContext m_Ctx;
    private SummonChessSkillTable m_Config;
    private bool m_EventRegistered;

    #endregion

    #region 公共方法

    public void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        m_Ctx = ctx;
        m_Config = config;

        // 订阅"造成伤害"事件，在命中时触发恐惧
        if (m_Ctx?.Attribute != null)
        {
            m_Ctx.Attribute.OnDamageDealt += OnDamageDealt;
            m_EventRegistered = true;
        }

        DebugEx.Log("EvilSpiritPassive", "黑暗侵染被动初始化完成");
    }

    public void Tick(float dt) { }

    public void Dispose()
    {
        if (m_EventRegistered && m_Ctx?.Attribute != null)
        {
            m_Ctx.Attribute.OnDamageDealt -= OnDamageDealt;
            m_EventRegistered = false;
        }
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 造成伤害时，30%概率对目标施加恐惧
    /// </summary>
    private void OnDamageDealt(double damage, ChessAttribute targetAttr)
    {
        if (damage <= 0 || targetAttr == null)
            return;

        if (Random.value > FEAR_TRIGGER_CHANCE)
            return;

        // 获取目标的 BuffManager
        var targetEntity = targetAttr.GetComponent<ChessEntity>();
        if (targetEntity?.BuffManager == null)
            return;

        targetEntity.BuffManager.AddBuff(9, m_Ctx.Owner, m_Ctx.Attribute); // 恐惧 ID=9
        DebugEx.Log(
            "EvilSpiritPassive",
            $"黑暗侵染触发！对 {targetEntity.Config?.Name} 施加恐惧效果"
        );
    }

    #endregion
}
