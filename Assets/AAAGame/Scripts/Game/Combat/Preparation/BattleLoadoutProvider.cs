using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战前出战资源提供者
/// 简化版：暂时将玩家已解锁的棋子和策略卡作为"备战可用"资源
/// 后续可扩展为完整的编战编辑系统
/// </summary>
public class BattleLoadoutProvider
{
    #region 单例

    private static BattleLoadoutProvider s_Instance;
    public static BattleLoadoutProvider Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new BattleLoadoutProvider();
            }
            return s_Instance;
        }
    }

    private BattleLoadoutProvider() { }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取备战棋子ID列表（暂时返回所有已解锁棋子）
    /// </summary>
    public List<int> GetPreparedChessIds()
    {
        if (ChessUnlockManager.Instance != null)
        {
            return new List<int>(ChessUnlockManager.Instance.GetUnlockedChess());
        }
        return new List<int>();
    }

    /// <summary>
    /// 获取备战策略卡ID列表（暂时返回玩家拥有的策略卡）
    /// </summary>
    public List<int> GetPreparedStrategyCardIds()
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData != null && saveData.OwnedStrategyCardIds != null)
        {
            return new List<int>(saveData.OwnedStrategyCardIds);
        }
        return new List<int>();
    }

    /// <summary>
    /// 获取备战棋子数量
    /// </summary>
    public int GetPreparedChessCount()
    {
        return GetPreparedChessIds().Count;
    }

    #endregion
}
