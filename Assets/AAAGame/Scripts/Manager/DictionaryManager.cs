using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 图鉴分类
/// </summary>
public enum DictionaryCategory
{
    Chess = 0, // 棋子
    StrategyCard, // 策略卡
    Enemy, // 敌人
    Equipment, // 装备
    Treasure, // 宝物
    Consumable, // 消耗品
    QuestItem, // 任务道具
}

/// <summary>
/// 图鉴条目数据（用于UI显示的通用结构）
/// </summary>
public struct DictionaryEntryData
{
    public int Id;
    public string Name;
    public string Description;
    public int IconId;
    public int Quality; // 品质/稀有度
    public bool IsUnlocked;
    public string SubText; // 副标题信息
    public DictionaryCategory Category;
}

/// <summary>
/// 图鉴管理器 - 统一管理所有分类的图鉴解锁状态
/// 从各DataTable读取全量数据，从PlayerSaveData读取解锁记录
/// </summary>
public class DictionaryManager
{
    #region 单例

    private static DictionaryManager s_Instance;
    public static DictionaryManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new DictionaryManager();
            }
            return s_Instance;
        }
    }

    private DictionaryManager() { }

    #endregion

    #region 字段

    private PlayerSaveData m_SaveData;

    // 缓存各分类的全量ID列表
    private Dictionary<DictionaryCategory, List<int>> m_AllIdsCache = new();

    #endregion

    #region 事件

    /// <summary>新条目被发现时触发</summary>
    public event Action<DictionaryCategory, int> OnEntryDiscovered;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化，加载存档时调用
    /// </summary>
    public void Initialize(PlayerSaveData saveData)
    {
        m_SaveData = saveData;
        m_AllIdsCache.Clear();

        // 确保存档中的列表不为null
        if (m_SaveData.DiscoveredItemIds == null)
            m_SaveData.DiscoveredItemIds = new List<int>();
        if (m_SaveData.DiscoveredEnemyIds == null)
            m_SaveData.DiscoveredEnemyIds = new List<int>();

        DebugEx.LogModule("DictionaryManager", "初始化完成");
    }

    /// <summary>
    /// 清空（新存档时）
    /// </summary>
    public void Clear()
    {
        m_SaveData = null;
        m_AllIdsCache.Clear();
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 判断某条目是否已解锁
    /// </summary>
    public bool IsUnlocked(DictionaryCategory category, int id)
    {
        if (m_SaveData == null)
            return false;

        switch (category)
        {
            case DictionaryCategory.Chess:
                return m_SaveData.OwnedUnitCardIds != null
                    && m_SaveData.OwnedUnitCardIds.Contains(id);

            case DictionaryCategory.StrategyCard:
                return m_SaveData.OwnedStrategyCardIds != null
                    && m_SaveData.OwnedStrategyCardIds.Contains(id);

            case DictionaryCategory.Enemy:
                return m_SaveData.DiscoveredEnemyIds != null
                    && m_SaveData.DiscoveredEnemyIds.Contains(id);

            case DictionaryCategory.Equipment:
            case DictionaryCategory.Treasure:
            case DictionaryCategory.Consumable:
            case DictionaryCategory.QuestItem:
                return m_SaveData.DiscoveredItemIds != null
                    && m_SaveData.DiscoveredItemIds.Contains(id);

            default:
                return false;
        }
    }

    /// <summary>
    /// 获取该分类的所有ID（从DataTable读取，有缓存）
    /// </summary>
    public List<int> GetAllIds(DictionaryCategory category)
    {
        if (m_AllIdsCache.TryGetValue(category, out var cached))
            return cached;

        var ids = LoadAllIdsFromTable(category);
        m_AllIdsCache[category] = ids;
        return ids;
    }

    /// <summary>
    /// 获取该分类已解锁数量
    /// </summary>
    public int GetUnlockedCount(DictionaryCategory category)
    {
        var allIds = GetAllIds(category);
        int count = 0;
        foreach (var id in allIds)
        {
            if (IsUnlocked(category, id))
                count++;
        }
        return count;
    }

    /// <summary>
    /// 获取该分类总数量
    /// </summary>
    public int GetTotalCount(DictionaryCategory category)
    {
        return GetAllIds(category).Count;
    }

    /// <summary>
    /// 获取所有分类的已解锁/总数
    /// </summary>
    public void GetTotalProgress(out int unlocked, out int total)
    {
        unlocked = 0;
        total = 0;
        foreach (DictionaryCategory cat in Enum.GetValues(typeof(DictionaryCategory)))
        {
            unlocked += GetUnlockedCount(cat);
            total += GetTotalCount(cat);
        }
    }

    /// <summary>
    /// 获取某条目的显示数据
    /// </summary>
    public DictionaryEntryData GetEntryData(DictionaryCategory category, int id)
    {
        var entry = new DictionaryEntryData
        {
            Id = id,
            Category = category,
            IsUnlocked = IsUnlocked(category, id),
        };

        switch (category)
        {
            case DictionaryCategory.Chess:
                FillChessEntry(ref entry, id);
                break;
            case DictionaryCategory.StrategyCard:
                FillCardEntry(ref entry, id);
                break;
            case DictionaryCategory.Enemy:
                FillEnemyEntry(ref entry, id);
                break;
            default:
                FillItemEntry(ref entry, id);
                break;
        }

        return entry;
    }

    #endregion

    #region 发现/解锁

    /// <summary>
    /// 发现新条目（获得物品、击败敌人时调用）
    /// </summary>
    public bool Discover(DictionaryCategory category, int id)
    {
        if (m_SaveData == null)
            return false;
        if (IsUnlocked(category, id))
            return false; // 已解锁

        switch (category)
        {
            case DictionaryCategory.Enemy:
                m_SaveData.DiscoveredEnemyIds.Add(id);
                break;

            case DictionaryCategory.Equipment:
            case DictionaryCategory.Treasure:
            case DictionaryCategory.Consumable:
            case DictionaryCategory.QuestItem:
                m_SaveData.DiscoveredItemIds.Add(id);
                break;

            // Chess 和 StrategyCard 由各自的管理器负责，这里不处理
            default:
                return false;
        }

        OnEntryDiscovered?.Invoke(category, id);
        DebugEx.LogModule("DictionaryManager", $"发现新条目: {category} id={id}");
        return true;
    }

    #endregion

    #region 私有方法 - 从DataTable加载

    private List<int> LoadAllIdsFromTable(DictionaryCategory category)
    {
        var ids = new List<int>();

        switch (category)
        {
            case DictionaryCategory.Chess:
            {
                var table = GF.DataTable.GetDataTable<SummonChessTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                        ids.Add(row.Id);
                }
                break;
            }
            case DictionaryCategory.StrategyCard:
            {
                var table = GF.DataTable.GetDataTable<CardTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                        ids.Add(row.Id);
                }
                break;
            }
            case DictionaryCategory.Enemy:
            {
                var table = GF.DataTable.GetDataTable<EnemyEntityTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                        ids.Add(row.Id);
                }
                break;
            }
            case DictionaryCategory.Equipment:
            {
                var table = GF.DataTable.GetDataTable<ItemTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                    {
                        if (row.Type == (int)ItemType.Equipment)
                            ids.Add(row.Id);
                    }
                }
                break;
            }
            case DictionaryCategory.Treasure:
            {
                var table = GF.DataTable.GetDataTable<ItemTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                    {
                        if (row.Type == (int)ItemType.Treasure)
                            ids.Add(row.Id);
                    }
                }
                break;
            }
            case DictionaryCategory.Consumable:
            {
                var table = GF.DataTable.GetDataTable<ItemTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                    {
                        if (row.Type == (int)ItemType.Consumable)
                            ids.Add(row.Id);
                    }
                }
                break;
            }
            case DictionaryCategory.QuestItem:
            {
                var table = GF.DataTable.GetDataTable<ItemTable>();
                if (table != null)
                {
                    foreach (var row in table.GetAllDataRows())
                    {
                        if (row.Type == (int)ItemType.Quest)
                            ids.Add(row.Id);
                    }
                }
                break;
            }
        }

        return ids;
    }

    private void FillChessEntry(ref DictionaryEntryData entry, int id)
    {
        var table = GF.DataTable.GetDataTable<SummonChessTable>();
        var row = table?.GetDataRow(id);
        if (row == null)
            return;

        entry.Name = row.Name;
        entry.Description = row.Description;
        entry.IconId = row.IconId;
        entry.Quality = row.Quality;
        entry.SubText = $"★{row.StarLevel}";
    }

    private void FillCardEntry(ref DictionaryEntryData entry, int id)
    {
        var table = GF.DataTable.GetDataTable<CardTable>();
        var row = table?.GetDataRow(id);
        if (row == null)
            return;

        entry.Name = row.Name;
        entry.Description = row.Desc;
        entry.IconId = row.IconId;
        entry.Quality = row.Rarity;
        entry.SubText = $"灵力 {row.SpiritCost}";
    }

    private void FillEnemyEntry(ref DictionaryEntryData entry, int id)
    {
        var table = GF.DataTable.GetDataTable<EnemyEntityTable>();
        var row = table?.GetDataRow(id);
        if (row == null)
            return;

        entry.Name = row.Name;
        entry.Description = row.Description;
        entry.IconId = row.IconId;
        entry.Quality = row.Difficulty;
        entry.SubText = $"难度 {new string('★', row.Difficulty)}";
    }

    private void FillItemEntry(ref DictionaryEntryData entry, int id)
    {
        var table = GF.DataTable.GetDataTable<ItemTable>();
        var row = table?.GetDataRow(id);
        if (row == null)
            return;

        entry.Name = row.Name;
        entry.Description = row.Description;
        entry.IconId = row.IconId > 0 ? row.IconId : row.DetailIconId;
        entry.Quality = row.Quality;
        entry.SubText = "";
    }

    #endregion
}
