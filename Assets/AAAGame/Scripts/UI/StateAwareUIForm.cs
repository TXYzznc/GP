using UnityGameFramework.Runtime;
using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 状态感知 UI 表单 - 自动监听游戏状态事件
/// 子类需要实现事件订阅和取消订阅逻辑
/// </summary>
public abstract class StateAwareUIForm : UIFormBase
{
    #region 生命周期

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 统一初始化逻辑，先隐藏直到状态事件就绪
        HideUI();

        // 订阅状态事件
        SubscribeEvents();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 取消监听状态事件，防止内存泄漏
        UnsubscribeEvents();

        base.OnClose(isShutdown, userData);
    }

    #endregion

    #region 抽象方法

    /// <summary>
    /// 订阅状态事件
    /// 子类需要实现此方法，监听需要的状态事件
    /// </summary>
    protected abstract void SubscribeEvents();

    /// <summary>
    /// 取消订阅状态事件
    /// 子类需要实现此方法，取消所有订阅的状态事件
    /// </summary>
    protected abstract void UnsubscribeEvents();

    #endregion

    #region 保护方法

    /// <summary>
    /// 显示 UI（使用事件系统使用）
    /// </summary>
    protected void ShowUI()
    {
        // 优先使用 CanvasGroup 来显示，而不是 SetActive
        // 这样可以保留 GameObject 的 UI 层级和属性状态
        if (TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Log.Info($"{GetType().Name}: 使用 CanvasGroup 显示 UI");
        }
        else
        {
            // 如果没有 CanvasGroup，退而求其次使用 SetActive（应该为 UIFormBase 自动处理）
            Log.Warning($"{GetType().Name}: 未找到 CanvasGroup，使用 SetActive 显示");
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏 UI（使用事件系统使用）
    /// </summary>
    protected void HideUI()
    {
        // 优先使用 CanvasGroup 来隐藏，而不是 SetActive
        // 这样可以保留 GameObject 的 UI 层级和属性状态
        if (TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Log.Info($"{GetType().Name}: 使用 CanvasGroup 隐藏 UI");
        }
        else
        {
            // 如果没有 CanvasGroup，退而求其次使用 SetActive（应该为 UIFormBase 自动处理）
            Log.Warning($"{GetType().Name}: 未找到 CanvasGroup，使用 SetActive 隐藏");
            gameObject.SetActive(false);
        }
    }

    #endregion

}
