using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 灼烧 Buff (ID=1)
/// 最多10层，每秒每层造成 EffectValue 点伤害
/// 达到5层/10层时，额外降低50点护甲
/// </summary>
public class BurnBuff : BuffBase
{
    #region 常量

    /// <summary>5层时降低的护甲</summary>
    private const double ARMOR_REDUCE_AT_5 = 50;

    /// <summary>10层时额外降低的护甲</summary>
    private const double ARMOR_REDUCE_AT_10 = 50;

    #endregion

    #region 私有字段

    /// <summary>是否已应用5层护甲削弱</summary>
    private bool m_ArmorReduced5;

    /// <summary>是否已应用10层护甲削弱</summary>
    private bool m_ArmorReduced10;

    #endregion

    #region 公共方法

    public override void OnEnter()
    {
        base.OnEnter();
        m_ArmorReduced5 = false;
        m_ArmorReduced10 = false;
        CheckArmorReduce();
    }

    protected override void OnTick()
    {
        if (Ctx?.OwnerAttribute == null) return;

        // 每秒每层造成 EffectValue 点伤害
        double damage = Config.EffectValue * StackCount;

        // 使用灼烧专属的飘字类型
        Ctx.OwnerAttribute.TakeDamage(damage, false, true, false, CombatVFXManager.DamageType.BurnDamage);

        DebugEx.LogModule("BurnBuff", $"灼烧伤害: {damage:F1} ({StackCount}层 × {Config.EffectValue})");
    }

    public override void OnExit()
    {
        // 恢复护甲削弱
        if (m_ArmorReduced10 && Ctx?.OwnerAttribute != null)
        {
            Ctx.OwnerAttribute.ModifyArmor(ARMOR_REDUCE_AT_10);
            m_ArmorReduced10 = false;
        }
        if (m_ArmorReduced5 && Ctx?.OwnerAttribute != null)
        {
            Ctx.OwnerAttribute.ModifyArmor(ARMOR_REDUCE_AT_5);
            m_ArmorReduced5 = false;
        }

        base.OnExit();
    }

    #endregion

    #region 叠层逻辑

    public override bool OnStack()
    {
        // 叠加不刷新持续时间（灼烧Buff设计只增加层数）
        if (StackCount < Config.MaxStack)
        {
            StackCount++;
            OnStackCountChanged();
        }
        return true;
    }

    /// <summary>
    /// 添加指定数量的层数（例如大招一次加2层）
    /// </summary>
    public void AddStacks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (StackCount >= Config.MaxStack) break;
            StackCount++;
        }
        OnStackCountChanged();
    }

    protected override void OnStackCountChanged()
    {
        CheckArmorReduce();
    }

    /// <summary>
    /// 减少层数（用于融化效果消耗）
    /// </summary>
    public override void ReduceStacks(int count)
    {
        if (count <= 0) return;

        int oldStacks = StackCount;

        // 先检查是否需要恢复护甲
        bool wasAt5 = oldStacks >= 5;
        bool wasAt10 = oldStacks >= 10;

        // 减少层数
        StackCount = Mathf.Max(0, StackCount - count);

        bool nowAt5 = StackCount >= 5;
        bool nowAt10 = StackCount >= 10;

        // 恢复护甲
        if (wasAt10 && !nowAt10 && m_ArmorReduced10)
        {
            Ctx?.OwnerAttribute?.ModifyArmor(ARMOR_REDUCE_AT_10);
            m_ArmorReduced10 = false;
        }
        if (wasAt5 && !nowAt5 && m_ArmorReduced5)
        {
            Ctx?.OwnerAttribute?.ModifyArmor(ARMOR_REDUCE_AT_5);
            m_ArmorReduced5 = false;
        }

        // 如果层数归零则结束
        if (StackCount <= 0)
        {
            IsFinished = true;
        }

        DebugEx.LogModule("BurnBuff", $"灼烧层数减少: {oldStacks} -> {StackCount}");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检查并应用护甲削弱
    /// </summary>
    private void CheckArmorReduce()
    {
        if (Ctx?.OwnerAttribute == null) return;

        // 5层降护甲
        if (StackCount >= 5 && !m_ArmorReduced5)
        {
            Ctx.OwnerAttribute.ModifyArmor(-ARMOR_REDUCE_AT_5);
            m_ArmorReduced5 = true;
            DebugEx.LogModule("BurnBuff", "灼烧达到5层，护甲-50");
        }

        // 10层再降护甲
        if (StackCount >= 10 && !m_ArmorReduced10)
        {
            Ctx.OwnerAttribute.ModifyArmor(-ARMOR_REDUCE_AT_10);
            m_ArmorReduced10 = true;
            DebugEx.LogModule("BurnBuff", "灼烧达到10层，护甲再-50");
        }
    }

    #endregion
}
