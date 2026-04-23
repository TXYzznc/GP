/// <summary>
/// 添加经验值效果
/// </summary>
public class AddExpEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int value = context.GetParam<int>("value", 0);

        var saveData = context.GetPlayerData();
        if (saveData == null)
        {
            LogError("AddExpEffect", "未加载存档");
            return false;
        }

        saveData.CurrentExp += value;
        PlayerAccountDataManager.Instance.SaveCurrentSave();

        LogSuccess("AddExpEffect", $"经验值添加成功: +{value}");
        GF.UI.ShowToast($"获得经验值：+{value}", UIExtension.ToastStyle.Green);

        return true;
    }
}
