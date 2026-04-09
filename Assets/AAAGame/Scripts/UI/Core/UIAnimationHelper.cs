using DG.Tweening;
using UnityEngine;

/// <summary>
/// UI 动画工具类，提供统一的动画模板
/// 所有动画使用 transform + CanvasGroup.alpha（GPU 加速）
/// 入场缓动：Ease.OutQuart；退场缓动：Ease.InQuart
/// </summary>
public static class UIAnimationHelper
{
    public enum SlideDirection { FromTop, FromBottom, FromLeft, FromRight }

    // ────────────────────────────────
    //  基础动画
    // ────────────────────────────────

    /// <summary>alpha 0 → 1</summary>
    public static Tween FadeIn(CanvasGroup cg, float duration = 0.3f)
    {
        cg.alpha = 0f;
        return cg.DOFade(1f, duration).SetEase(Ease.OutQuart).SetUpdate(true);
    }

    /// <summary>alpha 1 → 0</summary>
    public static Tween FadeOut(CanvasGroup cg, float duration = 0.25f)
    {
        return cg.DOFade(0f, duration).SetEase(Ease.InQuart).SetUpdate(true);
    }

    /// <summary>从指定方向偏移位置滑入，同时淡入</summary>
    public static Sequence SlideIn(RectTransform rt, CanvasGroup cg, SlideDirection direction, float offset = 100f, float duration = 0.35f)
    {
        var startPos = GetOffsetPosition(rt, direction, offset);
        var endPos = Vector2.zero;

        rt.anchoredPosition = startPos;
        if (cg != null) cg.alpha = 0f;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(rt.DOAnchorPos(endPos, duration).SetEase(Ease.OutQuart));
        if (cg != null)
            seq.Join(cg.DOFade(1f, duration * 0.8f).SetEase(Ease.OutQuart));
        return seq;
    }

    /// <summary>向指定方向滑出，同时淡出</summary>
    public static Sequence SlideOut(RectTransform rt, CanvasGroup cg, SlideDirection direction, float offset = 100f, float duration = 0.25f)
    {
        var endPos = GetOffsetPosition(rt, direction, offset);

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(rt.DOAnchorPos(endPos, duration).SetEase(Ease.InQuart));
        if (cg != null)
            seq.Join(cg.DOFade(0f, duration).SetEase(Ease.InQuart));
        return seq;
    }

    /// <summary>缩放弹出：scale 0.85→1 + alpha 0→1</summary>
    public static Sequence PopIn(RectTransform rt, CanvasGroup cg, float duration = 0.3f)
    {
        rt.localScale = Vector3.one * 0.85f;
        if (cg != null) cg.alpha = 0f;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(rt.DOScale(Vector3.one, duration).SetEase(Ease.OutQuart));
        if (cg != null)
            seq.Join(cg.DOFade(1f, duration * 0.8f).SetEase(Ease.OutQuart));
        return seq;
    }

    /// <summary>缩放收回：scale 1→0.85 + alpha 1→0</summary>
    public static Sequence PopOut(RectTransform rt, CanvasGroup cg, float duration = 0.2f)
    {
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(rt.DOScale(Vector3.one * 0.85f, duration).SetEase(Ease.InQuart));
        if (cg != null)
            seq.Join(cg.DOFade(0f, duration).SetEase(Ease.InQuart));
        return seq;
    }

    /// <summary>子元素依次淡入+上滑入场（stagger）</summary>
    public static Sequence StaggerChildren(Transform parent, float staggerDelay = 0.06f, float duration = 0.25f)
    {
        var seq = DOTween.Sequence().SetUpdate(true);
        float delay = 0f;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            var childRT = child as RectTransform;
            var childCG = child.GetComponent<CanvasGroup>();

            if (childRT != null)
            {
                var originalPos = childRT.anchoredPosition;
                var startPos = originalPos + new Vector2(0, -20f);
                childRT.anchoredPosition = startPos;

                float capturedDelay = delay;
                seq.InsertCallback(capturedDelay, () =>
                {
                    childRT.DOAnchorPos(originalPos, duration).SetEase(Ease.OutQuart).SetUpdate(true);
                    if (childCG != null)
                    {
                        childCG.alpha = 0f;
                        childCG.DOFade(1f, duration).SetEase(Ease.OutQuart).SetUpdate(true);
                    }
                });
                delay += staggerDelay;
            }
        }
        return seq;
    }

    /// <summary>清理对象上所有 DOTween 动画（true = 立即完成到终态）</summary>
    public static void Kill(Component target, bool complete = false)
    {
        DOTween.Kill(target.gameObject, complete);
    }

    // ────────────────────────────────
    //  内部工具
    // ────────────────────────────────

    private static Vector2 GetOffsetPosition(RectTransform rt, SlideDirection direction, float offset)
    {
        return direction switch
        {
            SlideDirection.FromTop => new Vector2(0, offset),
            SlideDirection.FromBottom => new Vector2(0, -offset),
            SlideDirection.FromLeft => new Vector2(-offset, 0),
            SlideDirection.FromRight => new Vector2(offset, 0),
            _ => Vector2.zero
        };
    }
}
