using System;
using UnityEngine;
using UnityEngine.UI;
using GameExtension;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using DG.Tweening;

public partial class CurrencyItem : UIItemBase
{
    private int m_IconId = 0;
    private int m_CurrentCount = 0;

    public int IconId => m_IconId;

    /// <summary>
    /// 设置货币数据
    /// </summary>
    public void SetData(int iconId, int count)
    {
        m_IconId = iconId;
        m_CurrentCount = count;

        // 设置货币数量
        if (varCurrencyText != null)
        {
            varCurrencyText.text = count.ToString();
        }

        // 加载货币图标
        if (varCurrencyIcon != null && iconId > 0)
        {
            ResourceExtension.LoadSpriteAsync(
                iconId,
                varCurrencyIcon,
                (error) =>
                {
                    DebugEx.ErrorModule("CurrencyItem", $"货币图标加载失败 - ConfigId: {iconId}, Error: {error}");
                },
                1f,
                null
            );
            DebugEx.LogModule("CurrencyItem", $"货币图标加载成功 - ConfigId: {iconId}");
        }
    }

    /// <summary>
    /// 更新货币数量（带跳动动效）
    /// </summary>
    public async UniTask UpdateCountAsync(int newCount)
    {
        if (varCurrencyText == null)
            return;

        int oldCount = m_CurrentCount;
        m_CurrentCount = newCount;

        // 用数字跳动效果显示数值变化
        int displayValue = oldCount;
        var tween = DOTween.To(
            () => displayValue,
            value =>
            {
                displayValue = value;
                varCurrencyText.text = displayValue.ToString();
            },
            newCount,
            0.3f
        );

        // 添加缩放效果
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            DOTween.Kill(rectTransform, true);
            rectTransform.localScale = Vector3.one;
            rectTransform.DOScale(1.15f, 0.1f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    rectTransform.DOScale(1f, 0.1f).SetEase(Ease.InQuad);
                });
        }

        await tween.AsyncWaitForCompletion();
        DebugEx.LogModule("CurrencyItem", $"货币更新完成: {oldCount} → {newCount}");
    }
}
