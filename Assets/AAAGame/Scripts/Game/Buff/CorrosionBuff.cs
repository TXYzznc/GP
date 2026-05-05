using UnityEngine;

/// <summary>
/// 腐蚀 Buff (ID=101)
/// 每层降低目标15%护甲和魔抗，最多叠加2层，持续6s
/// 由邪灵的"污染侵蚀"技能施加
/// </summary>
public class CorrosionBuff : BuffBase
{
    #region 私有字段

    /// <summary>已降低的护甲值（用于退出时还原）</summary>
    private double m_ArmorReduced;

    /// <summary>已降低的魔抗值（用于退出时还原）</summary>
    private double m_MagicResistReduced;

    /// <summary>是否已应用属性修改</summary>
    private bool m_IsApplied;

    #endregion

    #region 公共方法

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyReduction();
        DebugEx.Log(
            "CorrosionBuff",
            $"腐蚀Buff生效，当前层数: {StackCount}，"
                + $"护甲降低: {m_ArmorReduced:F1}，魔抗降低: {m_MagicResistReduced:F1}"
        );
    }

    public override void OnExit()
    {
        RestoreReduction();
        base.OnExit();
        DebugEx.Log("CorrosionBuff", "腐蚀Buff结束，属性已还原");
    }

    public override bool OnStack()
    {
        // 叠层：先还原旧值，再按新层数重新计算
        RestoreReduction();
        bool result = base.OnStack(); // 层数+1，刷新持续时间
        ApplyReduction();

        DebugEx.Log(
            "CorrosionBuff",
            $"腐蚀Buff叠层，当前层数: {StackCount}，"
                + $"护甲降低: {m_ArmorReduced:F1}，魔抗降低: {m_MagicResistReduced:F1}"
        );
        return result;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 应用护甲/魔抗降低（按层数计算）
    /// </summary>
    private void ApplyReduction()
    {
        if (m_IsApplied || Ctx?.OwnerAttribute == null)
            return;

        // 每层降低15%护甲和魔抗
        double armorReducePerStack = Ctx.OwnerAttribute.Armor * 0.15;
        double magicResistReducePerStack = Ctx.OwnerAttribute.MagicResist * 0.15;

        m_ArmorReduced = armorReducePerStack * StackCount;
        m_MagicResistReduced = magicResistReducePerStack * StackCount;

        Ctx.OwnerAttribute.ModifyArmor(-m_ArmorReduced);
        Ctx.OwnerAttribute.ModifyMagicResist(-m_MagicResistReduced);

        m_IsApplied = true;
    }

    /// <summary>
    /// 还原护甲/魔抗
    /// </summary>
    private void RestoreReduction()
    {
        if (!m_IsApplied || Ctx?.OwnerAttribute == null)
            return;

        Ctx.OwnerAttribute.ModifyArmor(m_ArmorReduced);
        Ctx.OwnerAttribute.ModifyMagicResist(m_MagicResistReduced);

        m_ArmorReduced = 0;
        m_MagicResistReduced = 0;
        m_IsApplied = false;
    }

    #endregion
}
