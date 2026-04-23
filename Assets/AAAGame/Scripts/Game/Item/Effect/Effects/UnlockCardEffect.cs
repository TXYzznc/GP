/// <summary>
/// 卡牌解锁效果
/// </summary>
public class UnlockCardEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int cardId = context.GetParam<int>("cardId", 0);
        if (cardId <= 0)
        {
            LogWarning("UnlockCardEffect", "卡牌ID未指定");
            GF.UI.ShowToast("卡牌解锁失败：配置错误", ToastStyle.Red);
            return false;
        }

        var playerData = context.GetPlayerData();
        if (playerData == null)
        {
            LogError("UnlockCardEffect", "未加载存档");
            GF.UI.ShowToast("卡牌解锁失败：数据异常", ToastStyle.Red);
            return false;
        }

        if (playerData.OwnedStrategyCardIds.Contains(cardId))
        {
            LogWarning("UnlockCardEffect", $"卡牌 {cardId} 已解锁");
            GF.UI.ShowToast("该卡牌已解锁", ToastStyle.Yellow);
            return false;
        }

        playerData.OwnedStrategyCardIds.Add(cardId);
        PlayerAccountDataManager.Instance.SaveCurrentSave();

        var cardData = GF.DataTable.GetDataTable<StrategyCardTable>()?.GetDataRow(cardId);
        string cardName = cardData?.Name ?? $"卡牌{cardId}";
        LogSuccess("UnlockCardEffect", $"解锁卡牌: {cardName}");
        GF.UI.ShowToast($"解锁卡牌：{cardName}", ToastStyle.Green);

        return true;
    }
}
