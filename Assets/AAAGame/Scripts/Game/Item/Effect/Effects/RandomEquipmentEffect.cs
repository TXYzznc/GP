/// <summary>
/// 随机获得装备效果
/// </summary>
public class RandomEquipmentEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int qualityMin = context.GetParam<int>("qualityMin", 1);
        int qualityMax = context.GetParam<int>("qualityMax", 3);

        LogSuccess("RandomEquipmentEffect", $"随机装备品质范围: {qualityMin}-{qualityMax}");
        GF.UI.ShowToast("获得随机装备", ToastStyle.Green);

        // TODO: 实现随机装备生成逻辑
        // 1. 从装备表中筛选符合品质范围的装备
        // 2. 随机选择一件装备
        // 3. 添加到背包

        return true;
    }
}
