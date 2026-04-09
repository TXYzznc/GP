using UnityEngine;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using System;
using DG.Tweening;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MenuUIForm : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnUserDataChanged);
        RefreshMoneyText();
        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, true);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        this.OpenSubUIForm(UIViews.Topbar, 1, uiparms);

        PlayOpenAnimation();
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnUserDataChanged);
        base.OnClose(isShutdown, userData);
    }

    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        Interactable = false;
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        cg.alpha = 0f;
        rt.anchoredPosition = new Vector2(0, -40f);
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(1f, 0.35f).SetEase(Ease.OutQuart))
            .Join(rt.DOAnchorPos(Vector2.zero, 0.35f).SetEase(Ease.OutQuart))
            .OnComplete(() => Interactable = true);
    }


    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SETTING":
                GF.UI.OpenUIForm(UIViews.SettingDialog);
                break;
        }
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as PlayerDataChangedEventArgs;
        switch (args.DataType)
        {
            case PlayerDataType.Coins:
                RefreshMoneyText();
                break;
            case PlayerDataType.LevelId:

                break;
        }
    }


    private void RefreshMoneyText()
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        SetMoneyText(playerDm.Coins);
    }
    private void SetMoneyText(int money)
    {
        moneyText.text = UtilityBuiltin.Valuer.ToCoins(money);
    }
}
