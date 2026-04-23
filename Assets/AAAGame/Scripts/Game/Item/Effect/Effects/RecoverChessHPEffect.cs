/// <summary>
/// 恢复棋子血量效果
/// </summary>
public class RecoverChessHPEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int chessId = context.GetParam<int>("chessId", 0);
        double value = context.GetParam<double>("value", 0);

        if (chessId <= 0)
        {
            LogWarning("RecoverChessHPEffect", "chessId 未指定，请在使用道具时传入目标棋子");
            return false;
        }

        bool success = GlobalChessManager.Instance.TryRecoverChessHP(chessId, value);

        if (success)
        {
            LogSuccess("RecoverChessHPEffect", $"棋子 {chessId} 血量恢复 +{value}");
            GF.UI.ShowToast($"棋子血量恢复：+{value}", UIExtension.ToastStyle.Green);
        }
        else
        {
            LogWarning("RecoverChessHPEffect", $"棋子 {chessId} 血量恢复失败（已死亡或血量已满）");
            GF.UI.ShowToast("血量恢复失败", UIExtension.ToastStyle.Yellow);
        }

        return success;
    }
}
