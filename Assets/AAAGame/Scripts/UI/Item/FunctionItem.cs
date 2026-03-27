using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

public partial class FunctionItem : UIItemBase
{
    private Action m_OnClick;

    /// <summary>
    /// 设置功能项数据
    /// </summary>
    public void SetData(string title, Action onClick)
    {
        // 设置标题
        if (varTitle != null)
        {
            varTitle.text = title;
        }

        // 设置点击回调
        m_OnClick = onClick;

        // 绑定按钮事件
        if (varFunctionItem != null)
        {
            varFunctionItem.onClick.RemoveAllListeners();
            varFunctionItem.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        m_OnClick?.Invoke();
    }
}
