using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 悬浮提示框 - 鼠标悬停时显示信息
/// </summary>
public partial class FloatingBoxTip : UIFormBase
{
    private RectTransform m_RectTransform;
    private Canvas m_ParentCanvas;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_RectTransform = GetComponent<RectTransform>();
        // 取根 Canvas（持有 worldCamera）
        m_ParentCanvas = FindRootCanvas(this);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 禁用射线拦截，防止提示框遮挡底层UI导致 PointerEnter/Exit 反复触发
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
        }
    }

    // Tips Canvas 为 Camera 模式时返回其 worldCamera，Overlay 模式返回 null
    private Camera GetCanvasCamera()
    {
        if (m_ParentCanvas == null) return null;
        return m_ParentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? m_ParentCanvas.worldCamera
            : null;
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
    /// 设置提示框位置（屏幕坐标 → 父节点本地坐标）
    /// Screen Space - Camera 模式下必须传入 Canvas 的 worldCamera
    /// </summary>
    public void SetPosition(Vector2 screenPosition)
    {
        if (m_RectTransform == null) return;

        var parentRect = m_RectTransform.parent as RectTransform;
        if (parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            GetCanvasCamera(),
            out Vector2 localPoint
        );
        m_RectTransform.anchoredPosition = localPoint;
    }

    /// <summary>
    /// 设置提示框位置（相对于目标 RectTransform，显示在其正上方 offset 处）
    /// </summary>
    public void SetPositionRelativeTo(RectTransform targetRect, Vector2 offset)
    {
        if (m_RectTransform == null || targetRect == null) return;

        Canvas rootCanvas = FindRootCanvas(targetRect);
        Camera targetCamera = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(targetCamera, topCenter);

        // Buff图标屏幕坐标（中心）
        Vector3 iconCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 iconScreenPos = RectTransformUtility.WorldToScreenPoint(targetCamera, iconCenter);

        DebugEx.LogModule("FloatingBoxTip",
            $"Buff图标屏幕坐标(中心)={iconScreenPos} | 顶部中心={screenPos} | offset={offset} | " +
            $"Canvas={rootCanvas?.name ?? "null"} renderMode={rootCanvas?.renderMode} camera={targetCamera?.name ?? "null"}");

        screenPos.y += offset.y;
        SetPosition(screenPos);

        // 输出提示框最终的屏幕坐标
        var screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), m_RectTransform.position);
        DebugEx.LogModule("FloatingBoxTip", $"提示框最终屏幕坐标={screenPoint} | anchoredPos={m_RectTransform.anchoredPosition}");
    }

    private static Canvas FindRootCanvas(Component target)
    {
        Canvas found = null;
        var t = target.transform;
        while (t != null)
        {
            var c = t.GetComponent<Canvas>();
            if (c != null) found = c;
            t = t.parent;
        }
        return found;
    }
}
