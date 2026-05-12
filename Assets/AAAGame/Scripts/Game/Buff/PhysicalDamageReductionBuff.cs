using Newtonsoft.Json.Linq;

/// <summary>
/// 物理伤害减免 Buff（虚无之躯）
/// 通过修改属性配置表的 PhysicalDamageReduce 实现减免
/// CustomData: {"PhysicalDamageReduce":"30%"}
/// </summary>
public class PhysicalDamageReductionBuff : BuffBase
{
    public override void Init(BuffContext ctx, BuffTable config)
    {
        base.Init(ctx, config);

        // 物理减免通过 StatModBuff 的方式实现
        // 需要在属性计算时处理 PhysicalDamageReduce 字段
    }
}
