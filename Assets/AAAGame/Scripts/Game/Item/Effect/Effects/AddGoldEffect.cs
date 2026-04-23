using UnityEngine;

/// <summary>
/// 添加金币效果
/// </summary>
public class AddGoldEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int min = context.GetParam<int>("min", 0);
        int max = context.GetParam<int>("max", 0);
        int gold = Random.Range(min, max + 1);

        var saveData = context.GetPlayerData();
        if (saveData == null)
        {
            LogError("AddGoldEffect", "未加载存档");
            return false;
        }

        saveData.Gold += gold;
        PlayerAccountDataManager.Instance.SaveCurrentSave();

        LogSuccess("AddGoldEffect", $"金币添加成功: +{gold}");
        GF.UI.ShowToast($"获得金币：+{gold}", UIExtension.ToastStyle.Green);

        return true;
    }
}
