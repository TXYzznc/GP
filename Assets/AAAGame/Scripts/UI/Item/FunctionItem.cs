using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

public partial class FunctionItem : UIItemBase
{
    private Action m_OnClick;

    /// <summary>
    /// 设置功能项数据（异步加载图标后再显示）
    /// </summary>
    public async UniTask SetDataAsync(string title, int iconId, Action onClick)
    {
        // 先隐藏对象，等图标加载完成后再显示
        gameObject.SetActive(false);

        // 设置标题
        if (varTitle != null)
        {
            varTitle.text = title;
        }

        // 异步加载按钮图标（等待加载完成）
        await LoadButtonIconAsync(iconId);

        // 设置点击回调
        m_OnClick = onClick;

        // 绑定按钮事件
        if (varFunctionItem != null)
        {
            varFunctionItem.onClick.RemoveAllListeners();
            varFunctionItem.onClick.AddListener(OnButtonClicked);
        }

        // 图标加载完成，显示对象
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 异步加载按钮图标
    /// </summary>
    private async UniTask LoadButtonIconAsync(int iconId)
    {
        DebugEx.Log("FunctionItem", $"[LoadButtonIconAsync] 开始加载图标: iconId={iconId}");

        if (varFunctionItem == null)
        {
            DebugEx.Error("FunctionItem", "[LoadButtonIconAsync] varFunctionItem 按钮组件未设置");
            return;
        }

        // 获取按钮的 Image 组件
        Image buttonImage = varFunctionItem.GetComponent<Image>();
        if (buttonImage == null)
        {
            DebugEx.Error("FunctionItem", "[LoadButtonIconAsync] 按钮上未找到 Image 组件");
            return;
        }

        DebugEx.Log(
            "FunctionItem",
            $"[LoadButtonIconAsync] 找到 Image 组件，开始加载资源: iconId={iconId}"
        );

        // 使用 ResourceExtension 加载图标到按钮的 Image
        await ResourceExtension.LoadSpriteAsync(iconId, buttonImage);

        DebugEx.Success("FunctionItem", $"[LoadButtonIconAsync] 按钮图标加载完成: iconId={iconId}");
    }

    private void OnButtonClicked()
    {
        m_OnClick?.Invoke();
    }
}
