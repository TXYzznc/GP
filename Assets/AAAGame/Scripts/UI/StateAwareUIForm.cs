using UnityGameFramework.Runtime;
using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 状态感知 UI 基类 - 自动监听游戏状态事件
/// 子类需要实现事件订阅和取消订阅逻辑
/// </summary>
public abstract class StateAwareUIForm : UIFormBase
{
    #region 生命周期

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 统一初始隐藏，等待状态事件触发
        HideUI();

        // 订阅状态事件
        SubscribeEvents();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 取消订阅状态事件（防止内存泄漏）
        UnsubscribeEvents();

        base.OnClose(isShutdown, userData);
    }

    #endregion

    #region 抽象方法

    /// <summary>
    /// 订阅状态事件
    /// 子类需要实现此方法，订阅需要监听的状态事件
    /// </summary>
    protected abstract void SubscribeEvents();

    /// <summary>
    /// 取消订阅状态事件
    /// 子类需要实现此方法，取消订阅的状态事件
    /// </summary>
    protected abstract void UnsubscribeEvents();

    #endregion

    #region 辅助方法

    /// <summary>
    /// 显示 UI（供事件回调使用）
    /// </summary>
    protected void ShowUI()
    {
        // ? 使用 CanvasGroup 控制显示，而不是 SetActive
        // 这样可以保持 GameObject 激活，UI 生命周期正常运行
        if (TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Log.Info($"{GetType().Name}: 使用 CanvasGroup 显示 UI");
        }
        else
        {
            // 如果没有 CanvasGroup，回退到 SetActive（不应该发生，因为 UIFormBase 会自动添加）
            Log.Warning($"{GetType().Name}: 未找到 CanvasGroup，使用 SetActive 显示");
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏 UI（供事件回调使用）
    /// </summary>
    protected void HideUI()
    {
        // ? 使用 CanvasGroup 控制隐藏，而不是 SetActive
        // 这样可以保持 GameObject 激活，UI 生命周期正常运行
        if (TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Log.Info($"{GetType().Name}: 使用 CanvasGroup 隐藏 UI");
        }
        else
        {
            // 如果没有 CanvasGroup，回退到 SetActive（不应该发生，因为 UIFormBase 会自动添加）
            Log.Warning($"{GetType().Name}: 未找到 CanvasGroup，使用 SetActive 隐藏");
            gameObject.SetActive(false);
        }
    }

    #endregion

}
