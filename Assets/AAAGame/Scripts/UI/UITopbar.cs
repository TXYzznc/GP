using GameFramework.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class UITopbar : UIFormBase
{
    public const string P_EnableBG = "EnableBG";
    public const string P_EnableSettingBtn = "EnableSettingBtn";
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);

        varBg.enabled = Params.Get<VarBoolean>(P_EnableBG, true);
        varBtnMenu.gameObject.SetActive(Params.Get<VarBoolean>(P_EnableSettingBtn, true));


        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        varTxtCoin.text = playerDm.Coins.ToString();
        varTxtEnergy.text = playerDm.GetData(PlayerDataType.Energy).ToString();
        varTxtGem.text = playerDm.GetData(PlayerDataType.Diamond).ToString();

        PlayOpenAnimation();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        base.OnClose(isShutdown, userData);
    }

    public override void OnClickClose()
    {
        Interactable = false;
        DOTween.Kill(gameObject);
        var rt = GetComponent<RectTransform>();
        var cg = GetComponent<CanvasGroup>();
        UIAnimationHelper.SlideOut(rt, cg, UIAnimationHelper.SlideDirection.FromTop, 80f, 0.25f)
            .OnComplete(() => GF.UI.Close(this.UIForm));
    }

    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        Interactable = false;
        var rt = GetComponent<RectTransform>();
        var cg = GetComponent<CanvasGroup>();
        UIAnimationHelper.SlideIn(rt, cg, UIAnimationHelper.SlideDirection.FromTop, 80f, 0.3f)
            .OnComplete(() => Interactable = true);
    }
    private void OnPlayerDataChanged(object sender, GameEventArgs e)
    {
        var args = e as PlayerDataChangedEventArgs;
        switch (args.DataType)
        {
            case PlayerDataType.Coins:
                varTxtCoin.text = args.Value.ToString();
                break;
            case PlayerDataType.Diamond:
                varTxtGem.text = args.Value.ToString();
                break;
            case PlayerDataType.Energy:
                varTxtEnergy.text = args.Value.ToString();
                break;
        }
    }

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varBtnMenu)
        {
            GF.UI.OpenUIForm(UIViews.SettingDialog);
        }
        else if (btSelf == varBtnCoin)
        {
            GF.UI.ShowToast("�ӽ��");
        }
        else if (btSelf == varBtnGem)
        {
            GF.UI.ShowToast("����ʯ");
        }
        else if (btSelf == varBtnEnergy)
        {
            GF.UI.ShowToast("������");
        }
    }
}
