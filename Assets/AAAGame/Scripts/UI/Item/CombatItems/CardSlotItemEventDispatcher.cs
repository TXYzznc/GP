using System;
using UnityEngine;

/// <summary>
/// 卡牌槽事件总线：解耦 CardSlotItem 和 CardSlotContainer
/// 职责：管理所有与卡牌交互相关的事件（拖拽、选中、销毁等）
/// </summary>
public static class CardSlotItemEventDispatcher
{
    #region 事件定义

    /// <summary>卡牌开始拖拽</summary>
    public static event Action<CardSlotItem, int> OnDragStarted;

    /// <summary>卡牌拖拽中，插入位置改变</summary>
    public static event Action<CardSlotItem, int> OnDragPositionChanged;

    /// <summary>卡牌拖拽结束</summary>
    public static event Action<CardSlotItem, Vector3, bool> OnDragEnded;  // item, worldPos, isValid

    /// <summary>卡牌选中状态改变</summary>
    public static event Action<CardSlotItem, bool> OnSelectionChanged;  // item, isSelected

    /// <summary>卡牌即将销毁（发出，让容器准备重排）</summary>
    public static event Action<CardSlotItem> OnAboutToDestroy;

    /// <summary>容器开始重排（通知卡牌禁用交互）</summary>
    public static event Action OnContainerRearrangeStarted;

    /// <summary>容器重排完成（通知卡牌启用交互）</summary>
    public static event Action OnContainerRearrangeEnded;

    /// <summary>拖拽上下文改变（其他卡需要知道当前谁在拖拽、插入位置在哪）</summary>
    public static event Action<CardSlotItem, int> OnDragContextChanged;  // dragCard, insertIndex

    #endregion

    #region 事件发射接口

    /// <summary>发出拖拽开始事件</summary>
    public static void RaiseDragStarted(CardSlotItem card, int startIndex)
    {
        OnDragStarted?.Invoke(card, startIndex);
        DebugEx.LogModule("CardSlotItemEventDispatcher", $"[拖拽开始] {card.GetCardData()?.Name ?? "Unknown"}");
    }

    /// <summary>发出拖拽位置改变事件</summary>
    public static void RaiseDragPositionChanged(CardSlotItem card, int newInsertIndex)
    {
        OnDragPositionChanged?.Invoke(card, newInsertIndex);
    }

    /// <summary>发出拖拽结束事件</summary>
    public static void RaiseDragEnded(CardSlotItem card, Vector3 worldPos, bool isValid)
    {
        OnDragEnded?.Invoke(card, worldPos, isValid);
        DebugEx.LogModule("CardSlotItemEventDispatcher",
            $"[拖拽结束] {card.GetCardData()?.Name ?? "Unknown"}, isValid={isValid}");
    }

    /// <summary>发出选中状态改变事件</summary>
    public static void RaiseSelectionChanged(CardSlotItem card, bool isSelected)
    {
        OnSelectionChanged?.Invoke(card, isSelected);
        DebugEx.LogModule("CardSlotItemEventDispatcher",
            $"[选中改变] {card.GetCardData()?.Name ?? "Unknown"}, selected={isSelected}");
    }

    /// <summary>发出即将销毁事件</summary>
    public static void RaiseAboutToDestroy(CardSlotItem card)
    {
        bool hasSubscribers = OnAboutToDestroy != null;
        DebugEx.LogModule("CardSlotItemEventDispatcher",
            $"[即将销毁] {card.GetCardData()?.Name ?? "Unknown"} | 有订阅者={hasSubscribers}");
        OnAboutToDestroy?.Invoke(card);
    }

    /// <summary>发出容器重排开始事件</summary>
    public static void RaiseContainerRearrangeStarted()
    {
        OnContainerRearrangeStarted?.Invoke();
    }

    /// <summary>发出容器重排完成事件</summary>
    public static void RaiseContainerRearrangeEnded()
    {
        OnContainerRearrangeEnded?.Invoke();
    }

    /// <summary>发出拖拽上下文改变事件</summary>
    public static void RaiseDragContextChanged(CardSlotItem dragCard, int insertIndex)
    {
        OnDragContextChanged?.Invoke(dragCard, insertIndex);
    }

    #endregion

    #region 清理

    /// <summary>清理所有事件监听（UI关闭时调用）</summary>
    public static void ClearAllListeners()
    {
        OnDragStarted = null;
        OnDragPositionChanged = null;
        OnDragEnded = null;
        OnSelectionChanged = null;
        OnAboutToDestroy = null;
        OnContainerRearrangeStarted = null;
        OnContainerRearrangeEnded = null;
        OnDragContextChanged = null;

        DebugEx.LogModule("CardSlotItemEventDispatcher", "所有事件监听已清理");
    }

    #endregion
}

/// <summary>
/// 卡牌槽项的状态枚举
/// </summary>
public enum CardSlotItemState
{
    Idle,          // 待命（未选中、未拖拽）
    Hovering,      // 鼠标悬停（仅用于视觉效果）
    Selected,      // 已选中
    Dragging,      // 拖拽中
    Destroying,    // 销毁中
    Destroyed,     // 已销毁（回收到池）
}

/// <summary>
/// 拖拽上下文：容器用来记录当前谁在拖拽
/// </summary>
public struct DragContext
{
    public CardSlotItem Card;
    public int StartIndex;
    public int CurrentInsertIndex;
    public bool IsActive => Card != null;

    public void Clear()
    {
        Card = null;
        StartIndex = -1;
        CurrentInsertIndex = -1;
    }
}

/// <summary>
/// 拖拽释放结果
/// </summary>
public struct DragReleaseResult
{
    public Vector3 WorldPosition;
    public bool IsInValidArea;      // 是否在有效的战场区域
    public bool IsInAdsorptionArea;  // 是否在吸附区（需要返回卡槽）
}
