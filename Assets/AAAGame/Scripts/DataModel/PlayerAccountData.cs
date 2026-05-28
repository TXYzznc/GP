using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家存档数据，代表一个存档
/// </summary>
[Serializable]
public class PlayerSaveData
{
    // ========== 存档标识 ==========
    /// <summary>
    /// 存档ID，唯一标识，使用GUID
    /// </summary>
    public string SaveId;

    /// <summary>
    /// 存档名称（玩家自定义）
    /// </summary>
    public string SaveName;

    /// <summary>
    /// 创建时间（时间戳）
    /// </summary>
    public double CreateTime;

    /// <summary>
    /// 最后游玩时间（时间戳）
    /// </summary>
    public double LastPlayTime;

    // ========== 等级与经验 ==========
    /// <summary>
    /// 全局等级
    /// </summary>
    public int GlobalLevel;

    /// <summary>
    /// 当前经验
    /// </summary>
    public int CurrentExp;

    // ========== 召唤师系统 ==========
    /// <summary>
    /// 当前召唤师ID
    /// </summary>
    public int CurrentSummonerId;

    /// <summary>
    /// 已解锁召唤师信息（JSON字符串，存储List<int>）
    /// </summary>
    public string UnlockedSummonerInfo;

    /// <summary>
    /// 召唤师阶段（当前召唤师的阶段ID）
    /// </summary>
    public int SummonerPhases;

    // ========== 卡牌收集 ==========
    /// <summary>
    /// 拥有的单位卡ID列表
    /// </summary>
    public List<int> OwnedUnitCardIds;

    /// <summary>
    /// 拥有的策略卡ID列表
    /// </summary>
    public List<int> OwnedStrategyCardIds;

    // ========== 科技树 ==========
    /// <summary>
    /// 已解锁的科技ID列表
    /// </summary>
    public List<int> UnlockedTechIds;

    /// <summary>
    /// 玩家已解锁的技能
    /// </summary>
    public List<int> PlayerSkillIds;

    // ========== 资源 ==========
    /// <summary>
    /// 金币
    /// </summary>
    public int Gold;

    /// <summary>
    /// 起源石
    /// </summary>
    public int OriginStone;

    /// <summary>
    /// 灵石（战斗资源，用于棋子进阶）
    /// </summary>
    public int SpiritStone;

    // ========== 背包 ==========
    /// <summary>
    /// 背包物品（JSON字符串，存储List<InventoryItemSaveData>）
    /// 注意：这里存储的是简化的存档数据，运行时使用 InventoryManager 管理完整物品实例
    /// </summary>
    public string InventoryItems;

    /// <summary>
    /// 背包容量
    /// </summary>
    public int InventoryCapacity;

    // ========== 卡组 ==========
    /// <summary>
    /// 卡组数据（JSON字符串，存储List<DeckData>）
    /// </summary>
    public string SavedDecks;

    /// <summary>
    /// 当前卡组索引
    /// </summary>
    public List<int> CurrentDeckIndex;

    // ========== 任务与成就 ==========
    /// <summary>
    /// 已完成的任务ID列表
    /// </summary>
    public List<int> CompletedQuestIds;

    // ========== 游戏进度 ==========
    /// <summary>
    /// 是否完成新手教程
    /// </summary>
    public bool HasCompletedTutorial;

    /// <summary>
    /// 当前所在场景ID（用于断线重连）
    /// </summary>
    public int CurrentSceneId;

    // ========== 图鉴解锁记录 ==========
    /// <summary>
    /// 已发现的物品ID列表（消耗品、任务道具、装备、宝物统一记录ItemTable的ID）
    /// </summary>
    public List<int> DiscoveredItemIds;

    /// <summary>
    /// 已发现的敌人ID列表（EnemyEntityTable的ID）
    /// </summary>
    public List<int> DiscoveredEnemyIds;

    // ========== 设置与统计 ==========

    /// <summary>
    /// 设置数据（JSON字符串，存储PlayerSetting）
    /// </summary>
    public string Settings;

    /// <summary>
    /// 统计数据（JSON字符串，存储PlayerStatistics）
    /// </summary>
    public string Statistics;

    // ========== 局内快照（进入局内时保存，未正常结算时用于回档）==========
    /// <summary>
    /// 进入局内时的账号数据快照（JSON），非空说明当前有未结算的局内进度
    /// </summary>
    public string InGameSnapshot;

    // ========== 宝物系统 ==========
    /// <summary>
    /// 所有宝物实例数据（运行时使用，不直接序列化）
    /// </summary>
    [System.NonSerialized]
    public List<TreasureInstanceData> Treasures;

    /// <summary>
    /// 棋子的宝物槽（运行时使用，不直接序列化）：Key=ChessId, Value=[InstanceId, InstanceId, InstanceId]（最多3个）
    /// </summary>
    [System.NonSerialized]
    public System.Collections.Generic.Dictionary<int, List<int>> ChessTreasureSlots;

    /// <summary>
    /// 宝物数据 JSON 字符串（用于实际存储）
    /// </summary>
    public string TreasureData = "";

    /// <summary>
    /// 宝物槽数据 JSON 字符串（用于实际存储）
    /// </summary>
    public string ChessTreasureSlotsData = "";

    // ========== 运行时缓存（NonSerialized，不参与存档序列化）==========

    [System.NonSerialized] private List<InventoryItemSaveData> m_InventoryItemsCache;
    [System.NonSerialized] private List<DeckData> m_SavedDecksCache;
    [System.NonSerialized] private List<int> m_UnlockedSummonerIdsCache;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public PlayerSaveData()
    {
        OwnedUnitCardIds = new List<int>();
        OwnedStrategyCardIds = new List<int>();
        UnlockedTechIds = new List<int>();
        CurrentDeckIndex = new List<int>();
        CompletedQuestIds = new List<int>();
        DiscoveredItemIds = new List<int>();
        DiscoveredEnemyIds = new List<int>();
        Treasures = new List<TreasureInstanceData>();
        ChessTreasureSlots = new System.Collections.Generic.Dictionary<int, List<int>>();
    }

    #region 辅助方法

    /// <summary>
    /// 获取已解锁召唤师列表
    /// </summary>
    public List<int> GetUnlockedSummonerIds()
    {
        if (m_UnlockedSummonerIdsCache != null)
            return m_UnlockedSummonerIdsCache;

        if (string.IsNullOrEmpty(UnlockedSummonerInfo))
        {
            m_UnlockedSummonerIdsCache = new List<int>();
            return m_UnlockedSummonerIdsCache;
        }

        try
        {
            m_UnlockedSummonerIdsCache = JsonUtility.FromJson<ListWrapper<int>>(UnlockedSummonerInfo).Items;
        }
        catch
        {
            m_UnlockedSummonerIdsCache = new List<int>();
        }
        return m_UnlockedSummonerIdsCache;
    }

    /// <summary>
    /// 设置已解锁召唤师列表
    /// </summary>
    public void SetUnlockedSummonerIds(List<int> ids)
    {
        m_UnlockedSummonerIdsCache = ids;
        UnlockedSummonerInfo = JsonUtility.ToJson(new ListWrapper<int> { Items = ids });
    }

    /// <summary>
    /// 获取背包物品列表（简化的存档数据）
    /// 注意：这里返回的是存档数据，不是完整的物品实例
    /// 运行时应该使用 InventoryManager 来管理物品
    /// </summary>
    public List<InventoryItemSaveData> GetInventoryItems()
    {
        if (m_InventoryItemsCache != null)
            return m_InventoryItemsCache;

        if (string.IsNullOrEmpty(InventoryItems))
        {
            m_InventoryItemsCache = new List<InventoryItemSaveData>();
            return m_InventoryItemsCache;
        }

        try
        {
            m_InventoryItemsCache = JsonUtility.FromJson<ListWrapper<InventoryItemSaveData>>(InventoryItems).Items;
        }
        catch
        {
            m_InventoryItemsCache = new List<InventoryItemSaveData>();
        }
        return m_InventoryItemsCache;
    }

    /// <summary>
    /// 设置背包物品列表（简化的存档数据）
    /// 注意：这里设置的是存档数据，不是完整的物品实例
    /// 应该从 InventoryManager 获取数据后转换为存档格式
    /// </summary>
    public void SetInventoryItems(List<InventoryItemSaveData> items)
    {
        m_InventoryItemsCache = items;
        InventoryItems = JsonUtility.ToJson(
            new ListWrapper<InventoryItemSaveData> { Items = items }
        );
    }

    /// <summary>
    /// 获取卡组列表
    /// </summary>
    public List<DeckData> GetSavedDecks()
    {
        if (m_SavedDecksCache != null)
            return m_SavedDecksCache;

        if (string.IsNullOrEmpty(SavedDecks))
        {
            m_SavedDecksCache = new List<DeckData>();
            return m_SavedDecksCache;
        }

        try
        {
            m_SavedDecksCache = JsonUtility.FromJson<ListWrapper<DeckData>>(SavedDecks).Items;
        }
        catch
        {
            m_SavedDecksCache = new List<DeckData>();
        }
        return m_SavedDecksCache;
    }

    /// <summary>
    /// 设置卡组列表
    /// </summary>
    public void SetSavedDecks(List<DeckData> decks)
    {
        m_SavedDecksCache = decks;
        SavedDecks = JsonUtility.ToJson(new ListWrapper<DeckData> { Items = decks });
    }

    /// <summary>
    /// 获取玩家设置
    /// </summary>
    public PlayerSetting GetSettings()
    {
        if (string.IsNullOrEmpty(Settings))
        {
            return new PlayerSetting();
        }

        try
        {
            return JsonUtility.FromJson<PlayerSetting>(Settings);
        }
        catch
        {
            return new PlayerSetting();
        }
    }

    /// <summary>
    /// 设置玩家设置
    /// </summary>
    public void SetSettings(PlayerSetting settings)
    {
        Settings = JsonUtility.ToJson(settings);
    }

    /// <summary>
    /// 获取统计数据
    /// </summary>
    public PlayerStatistics GetStatistics()
    {
        if (string.IsNullOrEmpty(Statistics))
        {
            return new PlayerStatistics();
        }

        try
        {
            return JsonUtility.FromJson<PlayerStatistics>(Statistics);
        }
        catch
        {
            return new PlayerStatistics();
        }
    }

    /// <summary>
    /// 设置统计数据
    /// </summary>
    public void SetStatistics(PlayerStatistics statistics)
    {
        Statistics = JsonUtility.ToJson(statistics);
    }

    #endregion
}

/// <summary>
/// 玩家账号信息，代表一个账号下的所有存档
/// </summary>
[Serializable]
public class PlayerAccountInfo
{
    /// <summary>
    /// 账号ID，由固定的登录系统分配
    /// </summary>
    public string AccountId;

    /// <summary>
    /// 存档ID列表（栈结构，最近使用的在最前面）
    /// </summary>
    public List<string> SaveIdStack;

    /// <summary>
    /// 账号创建时间
    /// </summary>
    public double CreateTime;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public double LastLoginTime;

    public PlayerAccountInfo()
    {
        SaveIdStack = new List<string>();
    }
}

/// <summary>
/// 存档摘要信息（用于UI显示）
/// </summary>
[Serializable]
public class SaveBriefInfo
{
    public string SaveId;
    public string SaveName;
    public int GlobalLevel;
    public double LastPlayTime;
    public double CreateTime;
}

// ========== 辅助类 ==========

/// <summary>
/// 列表包装器（用于 JsonUtility 序列化）
/// </summary>
[Serializable]
public class ListWrapper<T>
{
    public List<T> Items = new List<T>();
}

/// <summary>
/// 背包物品存档数据（简化版，仅用于存档）
/// 注意：这不是完整的物品数据，完整的物品系统在 AAAGame.Item 命名空间中
/// </summary>
[Serializable]
public class InventoryItemSaveData
{
    /// <summary>
    /// 物品ID
    /// </summary>
    public int ItemId;

    /// <summary>
    /// 物品数量
    /// </summary>
    public int Count;

    /// <summary>
    /// 获得时间（Unix时间戳）
    /// </summary>
    public long ObtainTime;

    /// <summary>
    /// 物品唯一ID（用于区分同ID的不同实例，如宝物的不同词条）
    /// </summary>
    public int UniqueId;

    /// <summary>
    /// 额外数据（JSON格式，用于存储宝物词条等特殊数据）
    /// 例如：宝物的随机词条、装备的强化等级等
    /// </summary>
    public string ExtraData;
}

[Serializable]
public class DeckData
{
    public string DeckName;
    public List<int> UnitCardIds;
    public List<int> StrategyCardIds;

    public DeckData()
    {
        UnitCardIds = new List<int>();
        StrategyCardIds = new List<int>();
    }
}

[Serializable]
public class PlayerStatistics
{
    public int TotalPlayTime;
    public int TotalBattles;
    public int TotalVictories;
    public int TotalDefeats;
    public int TotalBossKills;
    public int TotalPurifications;
}

[Serializable]
public class PlayerSetting
{
    public float MusicVolume = 0.8f;
    public float SoundVolume = 1.0f;
    public bool VibrateEnabled = true;
    public int LanguageId = 1;
    public int GraphicsQuality = 2;
}

/// <summary>
/// 进入局内前的账号数据快照，用于异常退出时回滚
/// </summary>
[Serializable]
public class PlayerSaveSnapshot
{
    public int GlobalLevel;
    public int CurrentExp;
    public int Gold;
    public int OriginStone;
    public int SpiritStone;
    public string InventoryItems;
    public int InventoryCapacity;
    public List<int> CompletedQuestIds;
    // 宝物数据（与 PlayerSaveData 中的字段一一对应）
    public string TreasureData;
    public string ChessTreasureSlotsData;
}

/// <summary>
/// 宝物实例数据（每个宝物都独一无二）
/// </summary>
[Serializable]
public class TreasureInstanceData
{
    public int InstanceId;              // 物品实例的唯一 ID
    public int TreasureId;              // 宝物配置表 ID
    public int EnhanceLevel;            // 强化等级
    public List<AffixData> Affixes;     // 词条效果列表
    public TreasureLocation Location;   // 背包/仓库（物理位置）
    public int EquippedChessId;         // 装备给哪个棋子（0=未装备）

    public TreasureInstanceData()
    {
        Affixes = new List<AffixData>();
        EquippedChessId = 0;
        EnhanceLevel = 0;
        Location = TreasureLocation.Inventory;
    }
}

/// <summary>
/// 宝物位置枚举
/// </summary>
public enum TreasureLocation
{
    Inventory = 0,  // 在背包中
    Warehouse = 1   // 在仓库中
}

/// <summary>
/// 用于 JSON 序列化的宝物列表包装类
/// </summary>
[Serializable]
public class SerializedTreasureList
{
    public List<TreasureInstanceData> Items = new();
}

/// <summary>
/// 用于 JSON 序列化的宝物槽包装类（将 Dictionary 转换为列表）
/// </summary>
[Serializable]
public class SerializedChessTreasureSlot
{
    public int ChessId;
    public List<int> InstanceIds = new();
}

[Serializable]
public class SerializedChessTreasureSlots
{
    public List<SerializedChessTreasureSlot> Slots = new();
}
