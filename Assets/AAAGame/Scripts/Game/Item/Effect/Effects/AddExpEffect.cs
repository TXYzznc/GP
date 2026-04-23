/// <summary>
/// 添加经验值效果
/// </summary>
public class AddExpEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int value = context.GetParam<int>("value", 0);

        var playerData = context.GetPlayerData();
        if (playerData == null)
        {
            LogError("AddExpEffect", "未加载存档");
            return false;
        }

        playerData.Exp += value;
        PlayerAccountDataManager.Instance.SaveCurrentSave();

        LogSuccess("AddExpEffect", $"经验值添加成功: +{value}");
        GF.UI.ShowToast($"获得经验值：+{value}", ToastStyle.Green);

        return true;
    }
}
