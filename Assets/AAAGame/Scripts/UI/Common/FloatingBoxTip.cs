using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 悬浮提示框 - 鼠标悬停时显示信息
/// </summary>
public partial class FloatingBoxTip : UIFormBase
{
    private RectTransform m_RectTransform;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_RectTransform = GetComponent<RectTransform>();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 禁用射线拦截，防止提示框遮挡底层UI导致 PointerEnter/Exit 反复触发（飘动问题）
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 设置提示框内容
    /// </summary>
    public void SetData(string text)
    {
        if (varText != null)
        {
            varText.text = text;
        }
    }

    /// <summary>
    /// 设置提示框位置（屏幕坐标）
    /// </summary>
    public void SetPosition(Vector2 screenPosition)
    {
        if (m_RectTransform != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_RectTransform.parent as RectTransform,
                screenPosition,
                GF.UICamera,
                out Vector2 localPoint
            );
            m_RectTransform.anchoredPosition = localPoint;
        }
    }

    /// <summary>
    /// 设置提示框位置（相对于目标RectTransform）
    /// </summary>
    public void SetPositionRelativeTo(RectTransform targetRect, Vector2 offset)
    {
        if (m_RectTransform != null && targetRect != null)
        {
            // 获取目标左上角的屏幕坐标
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GF.UICamera, corners[1]);
            screenPos += offset;
            SetPosition(screenPos);
        }
    }
}
