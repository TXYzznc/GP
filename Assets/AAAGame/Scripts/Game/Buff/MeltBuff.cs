using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 融化 Buff (ID=3)
/// 冰霜遇到灼烧时触发
/// 造成 30%法强 × 当前灼烧层数 的真实伤害
/// 融化消耗当前所有灼烧层数
/// </summary>
public class MeltBuff : BuffBase
{
    #region 常量

    /// <summary>法强系数</summary>
    private const double SPELL_POWER_RATIO = 0.3;

    #endregion

    #region 公共方法

    public override void OnEnter()
    {
        base.OnEnter();

        if (Ctx?.OwnerAttribute == null || Ctx?.OwnerBuffManager == null) return;

        // 获取灼烧Buff
        var burnBuff = Ctx.OwnerBuffManager.GetBuff(1) as BurnBuff;
        if (burnBuff == null || burnBuff.StackCount <= 0)
        {
            DebugEx.LogModule("MeltBuff", "目标没有灼烧层数，融化无效");
            IsFinished = true;
            return;
        }

        int burnStacks = burnBuff.StackCount;

        // 计算融化伤害：30%法强 × 灼烧层数
        // 法强来自施法者（嫦娥）
        double casterSpellPower = Ctx.CasterAttribute != null ? Ctx.CasterAttribute.SpellPower : 0;
        double meltDamage = casterSpellPower * SPELL_POWER_RATIO * burnStacks;

        DebugEx.LogModule("MeltBuff", $"融化触发: 法强={casterSpellPower:F1} × {SPELL_POWER_RATIO} × {burnStacks}层 = {meltDamage:F1}真实伤害");

        // 造成真实伤害
        if (meltDamage > 0)
        {
            Ctx.OwnerAttribute.TakeDamage(meltDamage, false, true);
        }

        // 消耗灼烧层数
        burnBuff.ReduceStacks(burnStacks);
        DebugEx.LogModule("MeltBuff", $"消耗灼烧{burnStacks}层，剩余{burnBuff.StackCount}层");

        // 融化是即时效果，立即结束
        IsFinished = true;
    }

    #endregion
}
