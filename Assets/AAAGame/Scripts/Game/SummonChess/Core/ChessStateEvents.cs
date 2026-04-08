using System;

/// <summary>
/// 棋子状态事件定义
/// 提供全局可订阅的棋子状态变化事件
///
/// 使用方式：
///   // 订阅
///   ChessStateEvents.OnGlobalChessHPChanged += HandleHPChanged;
///   // 取消订阅
///   ChessStateEvents.OnGlobalChessHPChanged -= HandleHPChanged;
/// </summary>
public static class ChessStateEvents
{
    #region 全局状态事件（跨战斗持久化数据变化）

    /// <summary>
    /// 全局棋子状态变化（血量持久化更新时触发）
    /// 参数：chessId, newState
    /// 触发时机：战斗结束回写、道具恢复、基地恢复后
    /// </summary>
    public static event Action<int, GlobalChessState> OnGlobalChessStateChanged;

    /// <summary>
    /// 全局棋子血量变化
    /// 参数：chessId, oldHp, newHp
    /// </summary>
    public static event Action<int, double, double> OnGlobalChessHPChanged;

    /// <summary>
    /// 全体棋子血量恢复到满值（回到基地时触发）
    /// </summary>
    public static event Action OnAllChessHPRestored;

    #endregion

    #region 战斗状态事件（战斗内临时数据变化）

    /// <summary>
    /// 战斗棋子数据变化（HP 或 Buff 列表变化时触发）
    /// 参数：chessId
    /// </summary>
    public static event Action<int> OnBattleChessDataChanged;

    /// <summary>
    /// 棋子 Buff 被添加
    /// 参数：chessId, buffId
    /// </summary>
    public static event Action<int, int> OnBuffAdded;

    /// <summary>
    /// 棋子 Buff 被移除
    /// 参数：chessId, buffId
    /// </summary>
    public static event Action<int, int> OnBuffRemoved;

    /// <summary>
    /// 棋子装备变更（穿戴或卸下）
    /// 参数：chessId, slotIndex
    /// </summary>
    public static event Action<int, int> OnEquipmentChanged;

    #endregion

    #region 事件触发方法（供内部系统调用）

    internal static void FireGlobalChessStateChanged(int chessId, GlobalChessState state)
    {
        OnGlobalChessStateChanged?.Invoke(chessId, state);
    }

    internal static void FireGlobalChessHPChanged(int chessId, double oldHp, double newHp)
    {
        DebugEx.LogModule("ChessStateEvents", $"全局HP变化 [{chessId}]: {oldHp:F0} → {newHp:F0}");
        OnGlobalChessHPChanged?.Invoke(chessId, oldHp, newHp);
    }

    internal static void FireAllChessHPRestored()
    {
        DebugEx.LogModule("ChessStateEvents", "触发：全体棋子血量恢复");
        OnAllChessHPRestored?.Invoke();
    }

    internal static void FireBattleChessDataChanged(int chessId)
    {
        OnBattleChessDataChanged?.Invoke(chessId);
    }

    internal static void FireBuffAdded(int chessId, int buffId)
    {
        DebugEx.LogModule("ChessStateEvents", $"Buff 添加 [{chessId}]: BuffId={buffId}");
        OnBuffAdded?.Invoke(chessId, buffId);
    }

    internal static void FireBuffRemoved(int chessId, int buffId)
    {
        DebugEx.LogModule("ChessStateEvents", $"Buff 移除 [{chessId}]: BuffId={buffId}");
        OnBuffRemoved?.Invoke(chessId, buffId);
    }

    internal static void FireEquipmentChanged(int chessId, int slotIndex)
    {
        DebugEx.LogModule("ChessStateEvents", $"装备变更 [{chessId}]: SlotIndex={slotIndex}");
        OnEquipmentChanged?.Invoke(chessId, slotIndex);
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清除所有事件订阅（退出游戏或场景切换时调用）
    /// </summary>
    public static void ClearAll()
    {
        OnGlobalChessStateChanged = null;
        OnGlobalChessHPChanged = null;
        OnAllChessHPRestored = null;
        OnBattleChessDataChanged = null;
        OnBuffAdded = null;
        OnBuffRemoved = null;
        OnEquipmentChanged = null;

        DebugEx.LogModule("ChessStateEvents", "所有事件订阅已清除");
    }

    #endregion
}
