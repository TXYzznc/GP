using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局棋子管理器（单例）
/// 负责维护所有棋子在战斗间隙的持久化状态（血量等）
///
/// 使用场景：
/// - 战斗开始时：BattleChessManager 调用 GetChessState() 读取血量
/// - 战斗结束时：BattleChessManager 调用 UpdateChessHP() 回写血量
/// - 回到基地时：调用 RestoreAllChessHP() 全体满血恢复
/// - 使用道具时：调用 TryRecoverChessHP() 恢复受伤棋子血量
/// </summary>
public class GlobalChessManager
{
    #region 单例

    private static GlobalChessManager s_Instance;

    public static GlobalChessManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new GlobalChessManager();
            }
            return s_Instance;
        }
    }

    private GlobalChessManager()
    {
        m_ChessStates = new Dictionary<int, GlobalChessState>();
    }

    #endregion

    #region 私有字段

    /// <summary>棋子全局状态字典（ChessId → GlobalChessState）</summary>
    private readonly Dictionary<int, GlobalChessState> m_ChessStates;

    #endregion

    #region 事件

    /// <summary>棋子全局状态变化事件（chessId, oldState, newState）</summary>
    public event Action<int, GlobalChessState> OnGlobalChessStateChanged;

    /// <summary>棋子血量变化事件（chessId, oldHp, newHp）</summary>
    public event Action<int, double, double> OnChessHPChanged;

    /// <summary>全体血量恢复事件</summary>
    public event Action OnAllChessHPRestored;

    #endregion

    #region 棋子状态注册

    /// <summary>
    /// 注册棋子（首次加入阵容时调用）
    /// 若已存在则跳过，以保留之前的战斗状态
    /// </summary>
    public void RegisterChess(int chessId, double maxHp)
    {
        if (m_ChessStates.ContainsKey(chessId))
        {
            DebugEx.LogModule("GlobalChessManager", $"棋子 {chessId} 已注册，保留现有状态");
            return;
        }

        var state = new GlobalChessState(chessId, maxHp);
        m_ChessStates[chessId] = state;

        DebugEx.LogModule("GlobalChessManager", $"注册棋子 {chessId}，MaxHP={maxHp:F0}");
        OnGlobalChessStateChanged?.Invoke(chessId, state);
    }

    /// <summary>
    /// 强制重新注册棋子（如星级升级导致 MaxHP 变化时）
    /// 血量按比例保留
    /// </summary>
    public void ReregisterChess(int chessId, double newMaxHp)
    {
        double hpRatio = 1.0;

        if (m_ChessStates.TryGetValue(chessId, out var existing) && existing.MaxHp > 0)
        {
            hpRatio = existing.CurrentHp / existing.MaxHp;
        }

        var state = new GlobalChessState(chessId, newMaxHp)
        {
            CurrentHp = Math.Max(0, newMaxHp * hpRatio)
        };
        m_ChessStates[chessId] = state;

        DebugEx.LogModule("GlobalChessManager", $"重新注册棋子 {chessId}，MaxHP={newMaxHp:F0}，HP比例={hpRatio:P0}");
        OnGlobalChessStateChanged?.Invoke(chessId, state);
    }

    /// <summary>
    /// 注销棋子（从阵容移除时调用）
    /// </summary>
    public void UnregisterChess(int chessId)
    {
        if (m_ChessStates.Remove(chessId))
        {
            DebugEx.LogModule("GlobalChessManager", $"注销棋子 {chessId}");
        }
    }

    #endregion

    #region 状态查询

    /// <summary>
    /// 获取棋子全局状态
    /// </summary>
    public GlobalChessState GetChessState(int chessId)
    {
        if (m_ChessStates.TryGetValue(chessId, out var state))
        {
            return state;
        }

        DebugEx.WarningModule("GlobalChessManager", $"找不到棋子状态 ChessId={chessId}");
        return null;
    }

    /// <summary>
    /// 是否已注册该棋子
    /// </summary>
    public bool HasChess(int chessId)
    {
        return m_ChessStates.ContainsKey(chessId);
    }

    /// <summary>
    /// 获取所有已注册棋子ID
    /// </summary>
    public IEnumerable<int> GetAllChessIds()
    {
        return m_ChessStates.Keys;
    }

    /// <summary>
    /// 获取所有已注册棋子状态
    /// </summary>
    public IEnumerable<GlobalChessState> GetAllChessStates()
    {
        return m_ChessStates.Values;
    }

    #endregion

    #region 血量更新

    /// <summary>
    /// 更新棋子血量（战斗结束时由 BattleChessManager 调用）
    /// </summary>
    public void UpdateChessHP(int chessId, double newHP)
    {
        if (!m_ChessStates.TryGetValue(chessId, out var state))
        {
            DebugEx.WarningModule("GlobalChessManager", $"UpdateChessHP: 找不到棋子 {chessId}");
            return;
        }

        double oldHp = state.CurrentHp;
        state.CurrentHp = Math.Clamp(newHP, 0, state.MaxHp);

        DebugEx.LogModule(
            "GlobalChessManager",
            $"更新血量 ChessId={chessId}：{oldHp:F0} → {state.CurrentHp:F0}/{state.MaxHp:F0}"
        );

        OnChessHPChanged?.Invoke(chessId, oldHp, state.CurrentHp);
        OnGlobalChessStateChanged?.Invoke(chessId, state);
        ChessStateEvents.FireGlobalChessHPChanged(chessId, oldHp, state.CurrentHp);
        ChessStateEvents.FireGlobalChessStateChanged(chessId, state);
    }

    /// <summary>
    /// 恢复所有棋子血量到满值（离开战斗回到基地时调用）
    /// 包括已死亡的棋子
    /// </summary>
    public void RestoreAllChessHP()
    {
        int count = 0;

        foreach (var state in m_ChessStates.Values)
        {
            double oldHp = state.CurrentHp;
            state.CurrentHp = state.MaxHp;

            if (Math.Abs(oldHp - state.CurrentHp) > 0.01)
            {
                OnChessHPChanged?.Invoke(state.ChessId, oldHp, state.CurrentHp);
                count++;
            }
        }

        DebugEx.LogModule("GlobalChessManager", $"基地恢复：{count} 个棋子血量恢复到满值");
        OnAllChessHPRestored?.Invoke();
        ChessStateEvents.FireAllChessHPRestored();
    }

    /// <summary>
    /// 使用道具恢复棋子血量（仅受伤棋子可以恢复，已死亡的不行）
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="recoverAmount">恢复量</param>
    /// <returns>是否成功恢复</returns>
    public bool TryRecoverChessHP(int chessId, double recoverAmount)
    {
        if (!m_ChessStates.TryGetValue(chessId, out var state))
        {
            DebugEx.WarningModule("GlobalChessManager", $"TryRecoverChessHP: 找不到棋子 {chessId}");
            return false;
        }

        if (state.IsDead)
        {
            DebugEx.LogModule(
                "GlobalChessManager",
                $"棋子 {chessId} 已死亡，无法通过道具恢复血量（需回到基地自动恢复）"
            );
            return false;
        }

        if (state.IsFullHp)
        {
            DebugEx.LogModule("GlobalChessManager", $"棋子 {chessId} 血量已满，无需恢复");
            return false;
        }

        double oldHp = state.CurrentHp;
        state.CurrentHp = Math.Min(state.MaxHp, state.CurrentHp + recoverAmount);

        DebugEx.LogModule(
            "GlobalChessManager",
            $"道具恢复：ChessId={chessId} {oldHp:F0} → {state.CurrentHp:F0}/{state.MaxHp:F0}"
        );

        OnChessHPChanged?.Invoke(chessId, oldHp, state.CurrentHp);
        OnGlobalChessStateChanged?.Invoke(chessId, state);
        return true;
    }

    /// <summary>
    /// 复活棋子（特殊技能/道具使用，仅已死亡棋子可复活）
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="reviveHP">复活后的血量（传入0则按最大血量50%计算）</param>
    /// <returns>是否成功复活</returns>
    public bool TryReviveChess(int chessId, double reviveHP = 0)
    {
        if (!m_ChessStates.TryGetValue(chessId, out var state))
        {
            DebugEx.WarningModule("GlobalChessManager", $"TryReviveChess: 找不到棋子 {chessId}");
            return false;
        }

        if (!state.IsDead)
        {
            DebugEx.LogModule("GlobalChessManager", $"棋子 {chessId} 未死亡，无需复活");
            return false;
        }

        double oldHp = state.CurrentHp;
        double targetHp = reviveHP > 0 ? reviveHP : state.MaxHp * 0.5;
        state.CurrentHp = Math.Clamp(targetHp, 1, state.MaxHp);

        DebugEx.LogModule(
            "GlobalChessManager",
            $"复活棋子 {chessId}，HP恢复至 {state.CurrentHp:F0}/{state.MaxHp:F0}"
        );

        OnChessHPChanged?.Invoke(chessId, oldHp, state.CurrentHp);
        OnGlobalChessStateChanged?.Invoke(chessId, state);
        return true;
    }

    #endregion

    #region 局内等级/经验

    /// <summary>
    /// 重置所有棋子的局内等级和经验（从基地开始新游戏时调用）
    /// </summary>
    public void ResetAllChessLevelAndExp()
    {
        foreach (var state in m_ChessStates.Values)
        {
            state.Level = 1;
            state.Experience = 0;
        }

        DebugEx.LogModule("GlobalChessManager", "所有棋子局内等级和经验已重置");
    }

    /// <summary>
    /// 更新棋子经验值（局内升级时调用）
    /// </summary>
    public void UpdateChessLevelAndExp(int chessId, int level, int experience)
    {
        if (!m_ChessStates.TryGetValue(chessId, out var state))
        {
            DebugEx.WarningModule("GlobalChessManager", $"UpdateChessLevelAndExp: 找不到棋子 {chessId}");
            return;
        }

        state.Level = level;
        state.Experience = experience;

        DebugEx.LogModule("GlobalChessManager", $"棋子 {chessId} 等级更新：Lv{level} Exp={experience}");
        OnGlobalChessStateChanged?.Invoke(chessId, state);
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清空所有状态（退出游戏或重置时调用）
    /// </summary>
    public void Clear()
    {
        m_ChessStates.Clear();

        OnGlobalChessStateChanged = null;
        OnChessHPChanged = null;
        OnAllChessHPRestored = null;

        DebugEx.LogModule("GlobalChessManager", "所有全局棋子状态已清空");
    }

    #endregion

    #region 调试

    public void DebugPrintAll()
    {
        DebugEx.LogModule("GlobalChessManager", $"=== 全局棋子状态 ({m_ChessStates.Count} 个) ===");

        foreach (var state in m_ChessStates.Values)
        {
            DebugEx.LogModule("GlobalChessManager", state.ToString());
        }

        DebugEx.LogModule("GlobalChessManager", "================================");
    }

    public string GetDebugInfo()
    {
        return $"[GlobalChessManager] 已注册 {m_ChessStates.Count} 个棋子";
    }

    #endregion
}
