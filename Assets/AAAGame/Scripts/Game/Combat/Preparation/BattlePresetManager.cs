using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 出战预设管理器
/// 负责预设数据的 CRUD 和存档读写
/// </summary>
public class BattlePresetManager
{
    #region 单例

    private static BattlePresetManager s_Instance;
    public static BattlePresetManager Instance
    {
        get
        {
            if (s_Instance == null)
                s_Instance = new BattlePresetManager();
            return s_Instance;
        }
    }

    private BattlePresetManager() { }

    #endregion

    #region 常量

    /// <summary>最大预设数量</summary>
    public const int MAX_PRESET_COUNT = 5;

    /// <summary>每预设最大棋子数</summary>
    public const int MAX_CHESS_COUNT = 8;

    /// <summary>每预设最大策略卡数</summary>
    public const int MAX_STRATEGY_CARD_COUNT = 12;

    #endregion

    #region 事件

    /// <summary>预设数据变化事件</summary>
    public event Action OnPresetsChanged;

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取所有预设
    /// </summary>
    public List<DeckData> GetAllPresets()
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null)
            return new List<DeckData>();

        return saveData.GetSavedDecks();
    }

    /// <summary>
    /// 获取指定索引的预设（超出范围返回null）
    /// </summary>
    public DeckData GetPreset(int index)
    {
        var presets = GetAllPresets();
        if (index < 0 || index >= presets.Count)
            return null;
        return presets[index];
    }

    /// <summary>
    /// 获取当前默认预设索引（-1表示无默认）
    /// </summary>
    public int GetDefaultPresetIndex()
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData?.CurrentDeckIndex != null && saveData.CurrentDeckIndex.Count > 0)
            return saveData.CurrentDeckIndex[0];
        return -1;
    }

    /// <summary>
    /// 设置默认预设索引
    /// </summary>
    public void SetDefaultPresetIndex(int index)
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null) return;

        if (saveData.CurrentDeckIndex == null)
            saveData.CurrentDeckIndex = new List<int>();

        saveData.CurrentDeckIndex.Clear();
        saveData.CurrentDeckIndex.Add(index);
        SaveToFile();

        DebugEx.LogModule("BattlePresetManager", $"设置默认预设索引: {index}");
    }

    /// <summary>
    /// 创建新预设
    /// </summary>
    public int CreatePreset(string name)
    {
        var presets = GetAllPresets();
        if (presets.Count >= MAX_PRESET_COUNT)
        {
            DebugEx.WarningModule("BattlePresetManager", "预设数量已达上限");
            return -1;
        }

        var deck = new DeckData
        {
            DeckName = string.IsNullOrEmpty(name) ? $"预设{presets.Count + 1}" : name
        };

        presets.Add(deck);
        SavePresets(presets);

        DebugEx.LogModule("BattlePresetManager", $"创建新预设: {deck.DeckName}, index={presets.Count - 1}");
        return presets.Count - 1;
    }

    /// <summary>
    /// 保存预设（覆盖指定索引）
    /// </summary>
    public void SavePreset(int index, DeckData data)
    {
        var presets = GetAllPresets();
        if (index < 0 || index >= presets.Count)
        {
            DebugEx.WarningModule("BattlePresetManager", $"无效的预设索引: {index}");
            return;
        }

        presets[index] = data;
        SavePresets(presets);

        DebugEx.LogModule("BattlePresetManager", $"保存预设: index={index}, name={data.DeckName}");
    }

    /// <summary>
    /// 删除预设
    /// </summary>
    public void DeletePreset(int index)
    {
        var presets = GetAllPresets();
        if (index < 0 || index >= presets.Count)
            return;

        presets.RemoveAt(index);
        SavePresets(presets);

        // 修正默认预设索引
        int defaultIndex = GetDefaultPresetIndex();
        if (defaultIndex == index)
            SetDefaultPresetIndex(-1);
        else if (defaultIndex > index)
            SetDefaultPresetIndex(defaultIndex - 1);

        DebugEx.LogModule("BattlePresetManager", $"删除预设: index={index}");
    }

    /// <summary>
    /// 获取已解锁的棋子ID列表
    /// </summary>
    public List<int> GetAvailableChessIds()
    {
        if (ChessUnlockManager.Instance != null)
            return new List<int>(ChessUnlockManager.Instance.GetUnlockedChess());
        return new List<int>();
    }

    /// <summary>
    /// 获取已拥有的策略卡ID列表
    /// </summary>
    public List<int> GetAvailableCardIds()
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData?.OwnedStrategyCardIds != null)
            return new List<int>(saveData.OwnedStrategyCardIds);
        return new List<int>();
    }

    #endregion

    #region 私有方法

    private void SavePresets(List<DeckData> presets)
    {
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null) return;

        saveData.SetSavedDecks(presets);
        SaveToFile();
        OnPresetsChanged?.Invoke();
    }

    private void SaveToFile()
    {
        PlayerAccountDataManager.Instance.SaveCurrentSave();
    }

    #endregion
}
