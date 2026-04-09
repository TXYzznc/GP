using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameOverUIForm : UIFormBase
{
    public const string P_IsWin = "IsWin";
    
    private bool isWin;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        isWin = Params.Get<VarBoolean>(P_IsWin);
        varTitleTxt.text = isWin ? GF.Localization.GetString("Victory") : GF.Localization.GetString("Failed");

        PlayOpenAnimation();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        base.OnClose(isShutdown, userData);
    }

    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        Interactable = false;
        var cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        // 标题初始缩放
        var titleRT = varTitleTxt.GetComponent<RectTransform>();
        titleRT.localScale = Vector3.one * 1.4f;

        // 按钮初始位置
        var btnRT = varBackBtn != null ? varBackBtn.GetComponent<RectTransform>() : null;
        Vector2 btnOrigPos = btnRT != null ? btnRT.anchoredPosition : Vector2.zero;
        if (btnRT != null)
            btnRT.anchoredPosition = btnOrigPos + new Vector2(0, -40f);

        DOTween.Sequence().SetUpdate(true)
            // 背景淡入
            .Append(cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuart))
            // 标题缩放弹入
            .Append(titleRT.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutQuart))
            // 按钮上滑淡入
            .AppendCallback(() =>
            {
                if (btnRT != null)
                    btnRT.DOAnchorPos(btnOrigPos, 0.3f).SetEase(Ease.OutQuart).SetUpdate(true);
            })
            .OnComplete(() => Interactable = true);
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if(btSelf == varBackBtn)
        {
            //(GF.Procedure.CurrentProcedure as GameOverProcedure).BackHome();
        }
    }
}
