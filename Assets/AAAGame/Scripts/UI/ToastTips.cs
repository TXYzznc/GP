using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ToastTips : UIFormBase
{
    public const string P_Duration = "Duration";
    public const string P_Text = "Text";
    public const string P_Style = "Style";

    float m_Duration;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_Duration = Params.Get<VarFloat>(P_Duration);
        varContentText.text = Params.Get<VarString>(P_Text);
        var style = Params.Get<VarUInt32>(P_Style);
        SetToastStyle(style);
        PlayOpenAnimation();
    }

    // 入场动画完成后触发倒计时
    protected override void OnOpenAnimationComplete()
    {
        // 不调用 base（不走 DOTweenSequence 路径），由 PlayOpenAnimation 的 OnComplete 控制
    }
    void SetToastStyle(uint style)
    {
        style = (uint)Mathf.Clamp(style, 0, (uint)UIExtension.ToastStyle.White);
        for (int i = 0; i < varToastMessageArr.Length; i++)
        {
            varToastMessageArr[i].SetActive(i == style);
        }
    }
    private void PlayOpenAnimation()
    {
        DOTween.Kill(gameObject);
        Interactable = false;
        var rt = GetComponent<RectTransform>();
        var cg = GetComponent<CanvasGroup>();
        var orig = rt.anchoredPosition;
        rt.anchoredPosition = orig + new Vector2(0, 50f);
        rt.localScale = Vector3.one * 0.95f;
        cg.alpha = 0f;
        DOTween.Sequence().SetUpdate(true)
            .Join(rt.DOAnchorPos(orig, 0.3f).SetEase(Ease.OutQuart))
            .Join(rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuart))
            .Join(cg.DOFade(1f, 0.25f).SetEase(Ease.OutQuart))
            .OnComplete(() =>
            {
                Interactable = true;
                ScheduleStart();
            });
    }

    private void ScheduleStart()
    {
        UniTask.Delay(TimeSpan.FromSeconds(m_Duration), true).ContinueWith(() =>
        {
            // 退场动画后关闭
            DOTween.Kill(gameObject);
            var rt = GetComponent<RectTransform>();
            var cg = GetComponent<CanvasGroup>();
            if (rt == null || cg == null) { GF.UI.Close(this.UIForm); return; }
            DOTween.Sequence().SetUpdate(true)
                .Join(rt.DOAnchorPos(rt.anchoredPosition + new Vector2(0, 50f), 0.25f).SetEase(Ease.InQuart))
                .Join(cg.DOFade(0f, 0.25f).SetEase(Ease.InQuart))
                .OnComplete(() => GF.UI.Close(this.UIForm));
        }).Forget();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(gameObject, true);
        base.OnClose(isShutdown, userData);
    }
}
