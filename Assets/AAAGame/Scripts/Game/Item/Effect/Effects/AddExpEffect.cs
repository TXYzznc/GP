/// <summary>
/// 添加经验值效果
/// </summary>
public class AddExpEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int value = context.GetParam<int>("value", 0);

        if (PlayerAccountDataManager.Instance.CurrentSaveData == null)
        {
            LogError("AddExpEffect", "未加载存档");
            return false;
        }

        // 走 AddExp 以触发升级检查和经验倍率
        PlayerAccountDataManager.Instance.AddExp(value);

        LogSuccess("AddExpEffect", $"经验值添加成功: +{value}");
        GF.UI.ShowToast($"获得经验值：+{value}", UIExtension.ToastStyle.Green);

        return true;
    }
}
