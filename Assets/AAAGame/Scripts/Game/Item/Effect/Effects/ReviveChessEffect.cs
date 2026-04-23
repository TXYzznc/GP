/// <summary>
/// 复活棋子效果
/// </summary>
public class ReviveChessEffect : ItemEffectBase
{
    public override bool Execute(ItemEffectContext context)
    {
        int chessId = context.GetParam<int>("chessId", 0);
        double reviveHP = context.GetParam<double>("reviveHP", 0);

        if (chessId <= 0)
        {
            LogWarning("ReviveChessEffect", "chessId 未指定");
            return false;
        }

        bool success = GlobalChessManager.Instance.TryReviveChess(chessId, reviveHP);

        if (success)
        {
            LogSuccess("ReviveChessEffect", $"棋子 {chessId} 复活成功，HP={reviveHP}");
            GF.UI.ShowToast($"棋子复活成功", UIExtension.ToastStyle.Green);
        }
        else
        {
            LogWarning("ReviveChessEffect", $"棋子 {chessId} 复活失败（未死亡或未注册）");
            GF.UI.ShowToast("复活失败", UIExtension.ToastStyle.Yellow);
        }

        return success;
    }
}
