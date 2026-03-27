using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;

/// <summary>
/// 静态 UI 组件 - 用于管理全局静态 UI 元素
/// </summary>
public class StaticUIComponent : GameFrameworkComponent
{
    [Header("Waiting View:")]
    [SerializeField] GameObject waitingView = null;

    private void Start()
    {
        // 初始化等待视图
        if (waitingView != null)
        {
            waitingView.SetActive(false);
        }
        
        // 同步 Canvas 设置（如果需要）
        UpdateCanvasScaler();
    }

    /// <summary>
    /// 同步 Canvas Scaler 设置与主 UI Canvas
    /// </summary>
    public void UpdateCanvasScaler()
    {
        var uiRootCanvas = GFBuiltin.RootCanvas;
        if (uiRootCanvas == null) return;

        var canvasRoot = this.GetComponent<Canvas>();
        if (canvasRoot != null)
        {
            canvasRoot.worldCamera = uiRootCanvas.worldCamera;
            canvasRoot.planeDistance = uiRootCanvas.planeDistance;
            canvasRoot.sortingLayerID = uiRootCanvas.sortingLayerID;
            canvasRoot.sortingOrder = uiRootCanvas.sortingOrder;
        }

        var canvasScaler = this.GetComponent<CanvasScaler>();
        var uiRootScaler = uiRootCanvas.GetComponent<CanvasScaler>();
        
        if (canvasScaler != null && uiRootScaler != null)
        {
            canvasScaler.uiScaleMode = uiRootScaler.uiScaleMode;
            canvasScaler.screenMatchMode = uiRootScaler.screenMatchMode;
            canvasScaler.matchWidthOrHeight = uiRootScaler.matchWidthOrHeight;
            canvasScaler.referencePixelsPerUnit = uiRootScaler.referencePixelsPerUnit;
            canvasScaler.referenceResolution = uiRootScaler.referenceResolution;
        }
    }

    /// <summary>
    /// 显示等待视图
    /// </summary>
    public void ShowWaiting()
    {
        if (waitingView != null)
        {
            waitingView.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏等待视图
    /// </summary>
    public void HideWaiting()
    {
        if (waitingView != null)
        {
            waitingView.SetActive(false);
        }
    }
}
