using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameUIForm : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RefreshCoinsText();

        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, false);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        this.OpenSubUIForm(UIViews.Topbar, 1, uiparms);

        PlayOpenAnimation();
    }

    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        Interactable = false;
        var cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuart).SetUpdate(true)
            .OnComplete(() => Interactable = true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        base.OnClose(isShutdown, userData);
    }
    private void RefreshCoinsText()
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        coinNumText.text = playerDm.Coins.ToString();
    }
}
