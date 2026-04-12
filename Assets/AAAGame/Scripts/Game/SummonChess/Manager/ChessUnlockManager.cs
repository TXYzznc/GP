using System;
using System.Collections.Generic;

/// <summary>
/// 棋子解锁管理器 - 管理玩家已解锁的棋子列表
/// 注意：棋子数据直接存储在 PlayerSaveData.OwnedUnitCardIds 中
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

    private ChessUnlockManager() { }

    #endregion

    #region 私有字段

    /// <summary>
    /// 当前存档数据引用
    /// </summary>
    private PlayerSaveData m_CurrentSaveData;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化管理器（在加载存档时调用）
    /// </summary>
    public void Initialize(PlayerSaveData saveData)
    {
        m_CurrentSaveData = saveData;
        DebugEx.LogModule("ChessUnlockManager", "初始化完成");
    }

    #endregion

    #region 解锁操作

    /// <summary>
    /// 解锁棋子（自动去重）
    /// </summary>
    /// <param name="chessId">召唤棋子ID</param>
    /// <returns>true=新解锁，false=已解锁</returns>
    public bool UnlockChess(int chessId)
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
        {
            DebugEx.ErrorModule("ChessUnlockManager", "SaveData 未初始化");
            return false;
        }

        if (m_CurrentSaveData.OwnedUnitCardIds.Contains(chessId))
        {
            DebugEx.LogModule("ChessUnlockManager", $"棋子已解锁: chessId={chessId}");
            return false;
        }

        m_CurrentSaveData.OwnedUnitCardIds.Add(chessId);
        OnChessUnlocked?.Invoke(chessId);

        DebugEx.LogModule("ChessUnlockManager", $"棋子已解锁: chessId={chessId}");
        return true;
    }

    /// <summary>
    /// 检查棋子是否已解锁
    /// </summary>
    public bool IsChessUnlocked(int chessId)
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
            return false;

        return m_CurrentSaveData.OwnedUnitCardIds.Contains(chessId);
    }

    /// <summary>
    /// 获取所有已解锁的棋子ID
    /// </summary>
    public IReadOnlyCollection<int> GetUnlockedChess()
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
            return new List<int>();

        return m_CurrentSaveData.OwnedUnitCardIds.AsReadOnly();
    }

    /// <summary>
    /// 获取已解锁棋子数量
    /// </summary>
    public int GetUnlockedCount()
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
            return 0;

        return m_CurrentSaveData.OwnedUnitCardIds.Count;
    }

    /// <summary>
    /// 获取指定品质的已解锁棋子数量
    /// </summary>
    public int GetUnlockedCountByQuality(int quality)
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
            return 0;

        int count = 0;
        foreach (var chessId in m_CurrentSaveData.OwnedUnitCardIds)
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
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
            return 0;

        int count = 0;
        foreach (var chessId in m_CurrentSaveData.OwnedUnitCardIds)
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

    #region 存档操作

    /// <summary>
    /// 清空（战斗结束或新存档）
    /// </summary>
    public void Clear()
    {
        if (m_CurrentSaveData != null && m_CurrentSaveData.OwnedUnitCardIds != null)
        {
            m_CurrentSaveData.OwnedUnitCardIds.Clear();
        }
        DebugEx.LogModule("ChessUnlockManager", "已清空棋子列表");
    }

    /// <summary>
    /// 初始化新存档（解锁初始棋子）
    /// </summary>
    /// <param name="initialChessIds">初始解锁的棋子ID列表</param>
    public void InitializeNewSave(List<int> initialChessIds)
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OwnedUnitCardIds == null)
        {
            DebugEx.ErrorModule("ChessUnlockManager", "SaveData 未初始化");
            return;
        }

        Clear();

        if (initialChessIds != null)
        {
            foreach (var chessId in initialChessIds)
            {
                UnlockChess(chessId);
            }

            DebugEx.LogModule(
                "ChessUnlockManager",
                $"新存档初始化: 解锁 {initialChessIds.Count} 个棋子"
            );
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
        int count = m_CurrentSaveData?.OwnedUnitCardIds?.Count ?? 0;
        return $"[ChessUnlockManager] UnlockedCount={count}";
    }

    /// <summary>
    /// 打印所有已解锁的棋子
    /// </summary>
    public void PrintUnlockedChess()
    {
        if (m_CurrentSaveData?.OwnedUnitCardIds == null)
        {
            DebugEx.LogModule("ChessUnlockManager", "未初始化");
            return;
        }

        DebugEx.LogModule(
            "ChessUnlockManager",
            $"已解锁棋子 ({m_CurrentSaveData.OwnedUnitCardIds.Count}):"
        );
        foreach (var chessId in m_CurrentSaveData.OwnedUnitCardIds)
        {
            if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
            {
                DebugEx.LogModule(
                    "ChessUnlockManager",
                    $"  - ID={chessId}, Name={config.Name}, Quality={config.Quality}, StarLevel={config.StarLevel}"
                );
            }
            else
            {
                DebugEx.LogModule("ChessUnlockManager", $"  - ID={chessId} (配置未找到)");
            }
        }
    }

    #endregion
}
