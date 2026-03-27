using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子解锁管理器 - 管理玩家已解锁的棋子列表
/// </summary>
public class ChessUnlockManager
{
    #region 单例

    private static ChessUnlockManager s_Instance;
    public static ChessUnlockManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ChessUnlockManager();
            }
            return s_Instance;
        }
    }

    private ChessUnlockManager()
    {
        m_UnlockedChess = new HashSet<int>();
    }

    #endregion
    
    #region 私有字段

    private HashSet<int> m_UnlockedChess = new HashSet<int>();

    #endregion

    #region 解锁操作

    /// <summary>
    /// 解锁棋子（自动去重）
    /// </summary>
    /// <param name="chessId">召唤棋子ID</param>
    /// <returns>true=新解锁，false=已解锁</returns>
    public bool UnlockChess(int chessId)
    {
        if (m_UnlockedChess.Contains(chessId))
        {
            DebugEx.LogModule("ChessUnlockManager", $"Chess already unlocked: chessId={chessId}");
            return false;  // 已解锁
        }

        m_UnlockedChess.Add(chessId);
        OnChessUnlocked?.Invoke(chessId);

        DebugEx.LogModule("ChessUnlockManager", $"Chess unlocked: chessId={chessId}");
        return true;
    }

    /// <summary>
    /// 检查棋子是否已解锁
    /// </summary>
    public bool IsChessUnlocked(int chessId)
    {
        return m_UnlockedChess.Contains(chessId);
    }

    /// <summary>
    /// 获取所有已解锁的棋子ID
    /// </summary>
    public IReadOnlyCollection<int> GetUnlockedChess()
    {
        return m_UnlockedChess;
    }

    /// <summary>
    /// 获取已解锁棋子数量
    /// </summary>
    public int GetUnlockedCount()
    {
        return m_UnlockedChess.Count;
    }

    /// <summary>
    /// 获取指定品质的已解锁棋子数量
    /// </summary>
    public int GetUnlockedCountByQuality(int quality)
    {
        int count = 0;
        foreach (var chessId in m_UnlockedChess)
        {
            if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
            {
                if (config.Quality == quality)
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 获取指定星级的已解锁棋子数量
    /// </summary>
    public int GetUnlockedCountByStarLevel(int starLevel)
    {
        int count = 0;
        foreach (var chessId in m_UnlockedChess)
        {
            if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
            {
                if (config.StarLevel == starLevel)
                {
                    count++;
                }
            }
        }
        return count;
    }

    #endregion

    #region 存档序列化

    /// <summary>
    /// 序列化到存档数据
    /// </summary>
    public List<int> SerializeToSaveData()
    {
        return new List<int>(m_UnlockedChess);
    }

    /// <summary>
    /// 从存档数据反序列化
    /// </summary>
    public void DeserializeFromSaveData(List<int> unlockedChessIds)
    {
        m_UnlockedChess.Clear();

        if (unlockedChessIds != null)
        {
            foreach (var id in unlockedChessIds)
            {
                m_UnlockedChess.Add(id);
            }

            DebugEx.LogModule("ChessUnlockManager", $"DeserializeFromSaveData: loaded {m_UnlockedChess.Count} unlocked chess");
        }
        else
        {
            DebugEx.LogModule("ChessUnlockManager", "DeserializeFromSaveData: no data to load");
        }
    }

    #endregion

    #region 新存档初始化

    /// <summary>
    /// 清空（战斗结束或新存档）
    /// </summary>
    public void Clear()
    {
        m_UnlockedChess.Clear();
        DebugEx.LogModule("ChessUnlockManager", "Clear: unlocked chess list cleared");
    }

    /// <summary>
    /// 初始化新存档（解锁初始棋子）
    /// </summary>
    /// <param name="initialChessIds">初始解锁的棋子ID列表</param>
    public void InitializeNewSave(List<int> initialChessIds)
    {
        Clear();

        if (initialChessIds != null)
        {
            foreach (var chessId in initialChessIds)
            {
                UnlockChess(chessId);
            }

            DebugEx.LogModule("ChessUnlockManager", $"InitializeNewSave: unlocked {initialChessIds.Count} initial chess");
        }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 棋子解锁事件
    /// </summary>
    public event Action<int> OnChessUnlocked;

    #endregion

    #region 调试

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[ChessUnlockManager] UnlockedCount={m_UnlockedChess.Count}";
    }

    /// <summary>
    /// 打印所有已解锁的棋子
    /// </summary>
    public void PrintUnlockedChess()
    {
        DebugEx.LogModule("ChessUnlockManager", $"Unlocked Chess ({m_UnlockedChess.Count}):");
        foreach (var chessId in m_UnlockedChess)
        {
            if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
            {
                DebugEx.LogModule("ChessUnlockManager", $"  - ID={chessId}, Name={config.Name}, Quality={config.Quality}, StarLevel={config.StarLevel}");
            }
            else
            {
                DebugEx.LogModule("ChessUnlockManager", $"  - ID={chessId} (config not found)");
            }
        }
    }

    #endregion
}
