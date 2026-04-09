using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 灼烧 Buff (ID=1, 5002)
/// 最多叠层，每秒每层造成固定伤害
/// 达到5层/10层时，额外降低50点护甲
/// CustomData 格式：{"DamagePerStack":5}
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

    /// <summary>每层每秒伤害（从 CustomData 读取）</summary>
    private double m_DamagePerStack;

    /// <summary>是否已应用5层护甲削弱</summary>
    private bool m_ArmorReduced5;

    /// <summary>是否已应用10层护甲削弱</summary>
    private bool m_ArmorReduced10;

    #endregion

    #region 初始化

    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 从 CustomData 读取每层伤害
        m_DamagePerStack = 5; // 默认值
        if (!string.IsNullOrEmpty(config?.CustomData) && config.CustomData != "{}")
        {
            try
            {
                var json = JObject.Parse(config.CustomData);
                if (json.TryGetValue("DamagePerStack", out var token))
                {
                    m_DamagePerStack = token.ToObject<double>();
                }
            }
            catch { }
        }
    }

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

        double damage = m_DamagePerStack * StackCount;
        Ctx.OwnerAttribute.TakeDamage(damage, false, true, false, CombatVFXManager.DamageType.BurnDamage);

        DebugEx.LogModule("BurnBuff", $"灼烧伤害: {damage:F1} ({StackCount}层 × {m_DamagePerStack})");
    }

    public override void OnExit()
    {
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
        if (StackCount < Config.MaxStack)
        {
            StackCount++;
            OnStackCountChanged();
        }
        return true;
    }

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

    public override void ReduceStacks(int count)
    {
        if (count <= 0) return;

        int oldStacks = StackCount;
        bool wasAt5 = oldStacks >= 5;
        bool wasAt10 = oldStacks >= 10;

        StackCount = Mathf.Max(0, StackCount - count);

        bool nowAt5 = StackCount >= 5;
        bool nowAt10 = StackCount >= 10;

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

        if (StackCount <= 0)
        {
            IsFinished = true;
        }

        DebugEx.LogModule("BurnBuff", $"灼烧层数减少: {oldStacks} -> {StackCount}");
    }

    #endregion

    #region 私有方法

    private void CheckArmorReduce()
    {
        if (Ctx?.OwnerAttribute == null) return;

        if (StackCount >= 5 && !m_ArmorReduced5)
        {
            Ctx.OwnerAttribute.ModifyArmor(-ARMOR_REDUCE_AT_5);
            m_ArmorReduced5 = true;
            DebugEx.LogModule("BurnBuff", "灼烧达到5层，护甲-50");
        }

        if (StackCount >= 10 && !m_ArmorReduced10)
        {
            Ctx.OwnerAttribute.ModifyArmor(-ARMOR_REDUCE_AT_10);
            m_ArmorReduced10 = true;
            DebugEx.LogModule("BurnBuff", "灼烧达到10层，护甲再-50");
        }
    }

    #endregion
}
