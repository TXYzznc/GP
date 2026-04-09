using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 悬浮提示框 - 用于显示技能描述等悬浮提示信息
/// </summary>
public partial class FloatingBoxTip : UIFormBase
{
    private RectTransform m_RectTransform;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        m_RectTransform = GetComponent<RectTransform>();
        
        Log.Info($"FloatingBoxTip OnInit: GameObject={gameObject.name}, RectTransform={m_RectTransform != null}");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        Log.Info($"FloatingBoxTip OnOpen: GameObject active={gameObject.activeSelf}, position={transform.position}");
    }

    /// <summary>
    /// 设置提示框数据
    /// </summary>
    /// <param name="text">显示的文本</param>
    public void SetData(string text)
    {
        Log.Info($"FloatingBoxTip SetData: text={text}, varText={varText != null}");
        
        if (varText != null)
        {
            varText.text = text;
            Log.Info($"varText.text 已设置: {varText.text}");
        }
        else
        {
            Log.Error("varText 为 null！请检查 Unity Inspector 中是否正确赋值");
        }
    }

    /// <summary>
    /// 设置提示框位置（屏幕坐标）
    /// </summary>
    /// <param name="screenPosition">屏幕坐标位置</param>
    public void SetPosition(Vector2 screenPosition)
    {
        Log.Info($"FloatingBoxTip SetPosition: screenPosition={screenPosition}");
        
        if (m_RectTransform != null)
        {
            // 转换屏幕坐标到UI坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_RectTransform.parent as RectTransform,
                screenPosition,
                GF.UICamera,
                out Vector2 localPoint
            );

            m_RectTransform.anchoredPosition = localPoint;
            Log.Info($"anchoredPosition 已设置: {m_RectTransform.anchoredPosition}");
        }
        else
        {
            Log.Error("m_RectTransform 为 null！");
        }
    }

    /// <summary>
    /// 设置提示框位置（相对于目标RectTransform）
    /// </summary>
    /// <param name="targetRect">目标RectTransform</param>
    /// <param name="offset">偏移量</param>
    public void SetPositionRelativeTo(RectTransform targetRect, Vector2 offset)
    {
        Log.Info($"FloatingBoxTip SetPositionRelativeTo: targetRect={targetRect != null}, offset={offset}");
        
        if (m_RectTransform != null && targetRect != null)
        {
            // 获取目标的屏幕坐标
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GF.UICamera, corners[1]); // 左上角

            Log.Info($"目标屏幕坐标: {screenPos}");

            // 应用偏移
            screenPos += offset;

            SetPosition(screenPos);
        }
        else
        {
            Log.Error($"参数无效: m_RectTransform={m_RectTransform != null}, targetRect={targetRect != null}");
        }
    }
}