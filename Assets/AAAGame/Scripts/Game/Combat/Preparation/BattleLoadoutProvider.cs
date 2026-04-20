using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战前出战资源提供者
/// 从当前选中的预设获取棋子和策略卡
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
    /// 获取备战棋子ID列表（从当前选中的预设获取）
    /// </summary>
    public List<int> GetPreparedChessIds()
    {
        int presetIndex = BattlePresetManager.Instance.GetDefaultPresetIndex();
        DeckData deck = BattlePresetManager.Instance.GetPreset(presetIndex);

        if (deck != null && deck.UnitCardIds != null)
        {
            return new List<int>(deck.UnitCardIds);
        }
        return new List<int>();
    }

    /// <summary>
    /// 获取备战策略卡ID列表（从当前选中的预设获取）
    /// </summary>
    public List<int> GetPreparedStrategyCardIds()
    {
        int presetIndex = BattlePresetManager.Instance.GetDefaultPresetIndex();
        DeckData deck = BattlePresetManager.Instance.GetPreset(presetIndex);

        if (deck != null && deck.StrategyCardIds != null)
        {
            return new List<int>(deck.StrategyCardIds);
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
