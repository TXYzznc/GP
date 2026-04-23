/// <summary>
/// 恢复生命值效果
/// </summary>
public class RestoreHPEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int value = context.GetParam<int>("value", 0);

        LogSuccess("RestoreHPEffect", $"生命值恢复成功: +{value}");
        GF.UI.ShowToast($"生命值恢复：+{value}", ToastStyle.Green);

        return true;
    }
}
