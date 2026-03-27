using System;
using UnityEngine;
using UnityEngine.UI;
using GameExtension;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;

public partial class CurrencyItem : UIItemBase
{
    /// <summary>
    /// 设置货币数据
    /// </summary>
    public void SetData(int iconId, int count)
    {
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
}
