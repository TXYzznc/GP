/// <summary>
/// 恢复魔法值效果
/// </summary>
public class RestoreMPEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int value = context.GetParam<int>("value", 0);

        LogSuccess("RestoreMPEffect", $"魔法值恢复成功: +{value}");
        GF.UI.ShowToast($"魔法值恢复：+{value}", UIExtension.ToastStyle.Green);

        return true;
    }
}
