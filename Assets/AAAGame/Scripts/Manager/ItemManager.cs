using System.Collections.Generic;
using GameFramework.DataTable;
using UnityEngine;

/// <summary>
/// 物品管理器
/// </summary>
public class ItemManager : SingletonBase<ItemManager>
{
    #region 字段

    private Dictionary<int, ItemData> m_ItemDataDict;
    private Dictionary<int, SpecialEffectData> m_EffectDataDict;
    private Dictionary<int, AffixData> m_AffixDataDict;
    private Dictionary<int, SynergyData> m_SynergyDataDict;
    // key = ItemTableId
    private Dictionary<int, ConsumableData> m_ConsumableDataDict;
    private Dictionary<int, TreasureData> m_TreasureDataDict;
    private Dictionary<int, EquipmentData> m_EquipmentDataDict;
    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();

        DebugEx.Log("ItemManager", "物品管理器初始化开始");
        InitializeData();
        DebugEx.Success("ItemManager", "物品管理器初始化完成");
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化数据
    /// </summary>
    private void InitializeData()
    {
        m_ItemDataDict = new Dictionary<int, ItemData>();
        m_EffectDataDict = new Dictionary<int, SpecialEffectData>();
        m_AffixDataDict = new Dictionary<int, AffixData>();
        m_SynergyDataDict = new Dictionary<int, SynergyData>();
        m_ConsumableDataDict = new Dictionary<int, ConsumableData>();
        m_TreasureDataDict = new Dictionary<int, TreasureData>();
        m_EquipmentDataDict = new Dictionary<int, EquipmentData>();

        // 注意：配置表需要先通过 GameFramework 加载
        // 这里只是初始化字典，实际加载需要在配置表准备好后手动调用
        // 可以在游戏启动流程中调用 LoadAllTables() 方法
    }

    /// <summary>
    /// 加载所有配置表（需要在配置表加载完成后调用）
    /// </summary>
    public void LoadAllTables()
    {
        DebugEx.Log("ItemManager", "开始加载所有配置表");

        LoadItemTable();
        LoadConsumableTable();
        LoadTreasureTable();
        LoadEquipmentTable();
        LoadSpecialEffectTable();
        LoadAffixTable();
        LoadSynergyTable();

        DebugEx.Success("ItemManager", "所有配置表加载完成");
    }

    /// <summary>
    /// 加载物品配置表
    /// </summary>
    public void LoadItemTable()
    {
        DebugEx.Log("ItemManager", "开始加载物品配置表");

        var table = GF.DataTable.GetDataTable<ItemTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "物品配置表未加载，请先加载配置表");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "物品配置表为空");
            return;
        }

        m_ItemDataDict.Clear();
        foreach (var row in allRows)
        {
            var itemData = ConvertToItemData(row);
            if (itemData != null)
            {
                m_ItemDataDict[itemData.Id] = itemData;
            }
        }

        DebugEx.Success("ItemManager", $"物品配置表加载完成，共 {m_ItemDataDict.Count} 条数据");
    }

    /// <summary>
    /// 加载消耗品配置表
    /// </summary>
    public void LoadConsumableTable()
    {
        DebugEx.Log("ItemManager", "开始加载消耗品配置表");

        var table = GF.DataTable.GetDataTable<ConsumableTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "消耗品配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "消耗品配置表为空");
            return;
        }

        m_ConsumableDataDict.Clear();
        foreach (var row in allRows)
        {
            var data = new ConsumableData
            {
                Id = row.Id,
                ItemTableId = row.ItemTableId,
                CanUse = row.CanUse,
                UseEffectId = row.UseEffectId,
            };
            m_ConsumableDataDict[row.ItemTableId] = data;
        }

        DebugEx.Success("ItemManager", $"消耗品配置表加载完成，共 {m_ConsumableDataDict.Count} 条数据");
    }

    /// <summary>
    /// 加载宝物配置表
    /// </summary>
    public void LoadTreasureTable()
    {
        DebugEx.Log("ItemManager", "开始加载宝物配置表");

        var table = GF.DataTable.GetDataTable<TreasureTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "宝物配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "宝物配置表为空");
            return;
        }

        m_TreasureDataDict.Clear();
        foreach (var row in allRows)
        {
            var data = new TreasureData
            {
                Id = row.Id,
                ItemTableId = row.ItemTableId,
                SpecialEffectId = row.SpecialEffectId,
                SynergyIds = new List<int>(row.SynergyIds ?? new int[0]),
                BaseAttributes = ParseAttributes(row.BaseAttributes),
            };
            m_TreasureDataDict[row.ItemTableId] = data;
        }

        DebugEx.Success("ItemManager", $"宝物配置表加载完成，共 {m_TreasureDataDict.Count} 条数据");
    }

    /// <summary>
    /// 加载装备配置表
    /// </summary>
    public void LoadEquipmentTable()
    {
        DebugEx.Log("ItemManager", "开始加载装备配置表");

        var table = GF.DataTable.GetDataTable<EquipmentTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "装备配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "装备配置表为空");
            return;
        }

        m_EquipmentDataDict.Clear();
        foreach (var row in allRows)
        {
            var data = new EquipmentData
            {
                Id = row.Id,
                ItemTableId = row.ItemTableId,
                SpecialEffectId = row.SpecialEffectId,
                BaseAttributes = ParseAttributes(row.BaseAttributes),
            };
            m_EquipmentDataDict[row.ItemTableId] = data;
        }

        DebugEx.Success("ItemManager", $"装备配置表加载完成，共 {m_EquipmentDataDict.Count} 条数据");
    }

    /// <summary>
    /// 加载特殊效果配置表
    /// </summary>
    public void LoadSpecialEffectTable()
    {
        DebugEx.Log("ItemManager", "开始加载特殊效果配置表");

        var table = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "特殊效果配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "特殊效果配置表为空");
            return;
        }

        m_EffectDataDict.Clear();
        foreach (var row in allRows)
        {
            var effectData = ConvertToEffectData(row);
            if (effectData != null)
            {
                m_EffectDataDict[effectData.Id] = effectData;
            }
        }

        DebugEx.Success(
            "ItemManager",
            $"特殊效果配置表加载完成，共 {m_EffectDataDict.Count} 条数据"
        );
    }

    /// <summary>
    /// 加载词条配置表
    /// </summary>
    public void LoadAffixTable()
    {
        DebugEx.Log("ItemManager", "开始加载词条配置表");

        var table = GF.DataTable.GetDataTable<AffixTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "词条配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "词条配置表为空");
            return;
        }

        m_AffixDataDict.Clear();
        foreach (var row in allRows)
        {
            var affixData = ConvertToAffixData(row);
            if (affixData != null)
            {
                m_AffixDataDict[affixData.Id] = affixData;
            }
        }

        DebugEx.Success("ItemManager", $"词条配置表加载完成，共 {m_AffixDataDict.Count} 条数据");
    }

    /// <summary>
    /// 加载羁绊配置表
    /// </summary>
    public void LoadSynergyTable()
    {
        DebugEx.Log("ItemManager", "开始加载羁绊配置表");

        var table = GF.DataTable.GetDataTable<SynergyTable>();
        if (table == null)
        {
            DebugEx.Error("ItemManager", "羁绊配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Warning("ItemManager", "羁绊配置表为空");
            return;
        }

        m_SynergyDataDict.Clear();
        foreach (var row in allRows)
        {
            var synergyData = ConvertToSynergyData(row);
            if (synergyData != null)
            {
                m_SynergyDataDict[synergyData.Id] = synergyData;
            }
        }

        DebugEx.Success("ItemManager", $"羁绊配置表加载完成，共 {m_SynergyDataDict.Count} 条数据");
    }

    #endregion

    #region 公共方法 - 数据获取

    /// <summary>
    /// 获取物品配置数据
    /// </summary>
    public ItemData GetItemData(int itemId)
    {
        if (m_ItemDataDict.TryGetValue(itemId, out var data))
        {
            return data;
        }

        DebugEx.Warning("ItemManager", $"物品配置不存在 ID:{itemId}");
        return null;
    }

    /// <summary>
    /// 获取特殊效果配置数据
    /// </summary>
    public SpecialEffectData GetSpecialEffectData(int effectId)
    {
        if (m_EffectDataDict.TryGetValue(effectId, out var data))
        {
            return data;
        }

        DebugEx.Warning("ItemManager", $"特殊效果配置不存在 ID:{effectId}");
        return null;
    }

    /// <summary>
    /// 获取词条配置数据
    /// </summary>
    public AffixData GetAffixData(int affixId)
    {
        if (m_AffixDataDict.TryGetValue(affixId, out var data))
        {
            return data;
        }

        DebugEx.Warning("ItemManager", $"词条配置不存在 ID:{affixId}");
        return null;
    }

    /// <summary>
    /// 获取所有词条配置数据
    /// </summary>
    public List<AffixData> GetAllAffixData()
    {
        return new List<AffixData>(m_AffixDataDict.Values);
    }

    /// <summary>
    /// 从宝物物品创建持久化数据（用于保存到存档）
    /// </summary>
    public TreasureInstanceData CreateTreasureInstanceData(TreasureItem treasureItem, int instanceId)
    {
        if (treasureItem == null)
        {
            DebugEx.Error("ItemManager", $"CreateTreasureInstanceData: treasureItem is null");
            return null;
        }

        var instanceData = new TreasureInstanceData
        {
            InstanceId = instanceId,
            TreasureId = treasureItem.ItemId,
            EnhanceLevel = 0,
            Location = TreasureLocation.Inventory,
            EquippedChessId = 0,
            Affixes = treasureItem.GetAffixDataForPersistence()
        };

        DebugEx.Log("ItemManager", $"创建宝物实例: {treasureItem.Name} (InstanceId:{instanceId}, 词条数:{instanceData.Affixes.Count})");
        return instanceData;
    }

    /// <summary>
    /// 获取羁绊配置数据
    /// </summary>
    public SynergyData GetSynergyData(int synergyId)
    {
        if (m_SynergyDataDict.TryGetValue(synergyId, out var data))
        {
            return data;
        }

        DebugEx.Warning("ItemManager", $"羁绊配置不存在 ID:{synergyId}");
        return null;
    }

    /// <summary>
    /// 获取消耗品专有数据（key = ItemTableId）
    /// </summary>
    public ConsumableData GetConsumableData(int itemTableId)
    {
        m_ConsumableDataDict.TryGetValue(itemTableId, out var data);
        return data;
    }

    /// <summary>
    /// 获取宝物专有数据（key = ItemTableId）
    /// </summary>
    public TreasureData GetTreasureData(int itemTableId)
    {
        m_TreasureDataDict.TryGetValue(itemTableId, out var data);
        return data;
    }

    /// <summary>
    /// 获取装备专有数据（key = ItemTableId）
    /// </summary>
    public EquipmentData GetEquipmentData(int itemTableId)
    {
        m_EquipmentDataDict.TryGetValue(itemTableId, out var data);
        return data;
    }

    #endregion

    #region 公共方法 - 物品创建

    /// <summary>
    /// 创建物品实例
    /// </summary>
    public ItemBase CreateItem(int itemId)
    {
        var itemData = GetItemData(itemId);
        if (itemData == null)
        {
            DebugEx.Error("ItemManager", $"创建物品失败，配置不存在 ID:{itemId}");
            return null;
        }

        DebugEx.Log("ItemManager", $"创建物品: {itemData.Name} (ID:{itemId})");

        ItemBase item = null;

        switch (itemData.Type)
        {
            case ItemType.Consumable:
                item = new ConsumableItem(itemId, itemData, GetConsumableData(itemId));
                break;

            case ItemType.Quest:
                item = new QuestItem(itemId, itemData);
                break;

            case ItemType.Treasure:
                var treasure = new TreasureItem(itemId, itemData, GetTreasureData(itemId));
                treasure.SetAffixes(AffixGenerator.Generate(itemData.Rarity));
                item = treasure;
                break;

            case ItemType.Equipment:
                item = new EquipmentItem(itemId, itemData, GetEquipmentData(itemId));
                break;

            case ItemType.Virtual:
                item = new VirtualItem(itemId, itemData);
                break;

            default:
                DebugEx.Error("ItemManager", $"未知的物品类型: {itemData.Type}");
                break;
        }

        if (item != null)
        {
            DebugEx.Success("ItemManager", $"物品创建成功: {itemData.Name}");

            // 自动解锁图鉴
            UnlockDictionaryEntry(itemId, itemData.Type);
        }

        return item;
    }

    /// <summary>
    /// 解锁图鉴条目
    /// </summary>
    private void UnlockDictionaryEntry(int itemId, ItemType itemType)
    {
        DictionaryCategory category;

        switch (itemType)
        {
            case ItemType.Equipment:
                category = DictionaryCategory.Equipment;
                break;
            case ItemType.Treasure:
                category = DictionaryCategory.Treasure;
                break;
            case ItemType.Consumable:
                category = DictionaryCategory.Consumable;
                break;
            case ItemType.Quest:
                category = DictionaryCategory.QuestItem;
                break;
            default:
                return; // 不支持的类型
        }

        bool isNew = DictionaryManager.Instance.Discover(category, itemId);
        if (isNew)
        {
            DebugEx.Success("ItemManager", $"图鉴解锁: {itemType} ID:{itemId}");
        }
    }

    #endregion

    #region 私有方法 - 数据转换

    /// <summary>
    /// 转换为物品数据
    /// </summary>
    private ItemData ConvertToItemData(ItemTable row)
    {
        var itemData = new ItemData
        {
            Id = row.Id,
            Name = row.Name,
            Type = (ItemType)row.Type,
            Rarity = (ItemRarity)row.Rarity,
            IsOnlyInGame = row.IsOnlyInGame,
            Description = row.Description,
            IconId = row.IconId,
            CanStack = row.CanStack,
            MaxStackCount = row.MaxStackCount,
            SellPrice = row.SellPrice,
            Value = row.Value,
            Weight = row.Weight,
        };

        return itemData;
    }

    /// <summary>
    /// 转换为特殊效果数据
    /// </summary>
    private SpecialEffectData ConvertToEffectData(SpecialEffectTable row)
    {
        var effectData = new SpecialEffectData
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            EffectType = (SpecialEffectType)row.EffectType,
            EffectParams = row.EffectParams ?? "",
        };

        return effectData;
    }

    /// <summary>
    /// 转换为词条数据
    /// </summary>
    private AffixData ConvertToAffixData(AffixTable row)
    {
        var affixData = new AffixData
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            AffixType = (AffixType)row.AffixType,
            AttributeType = (AttributeType)row.AttributeType,
            ValueType = (ValueType)row.ValueType,
            ValueMin = row.ValueMin,
            ValueMax = row.ValueMax,
            Weight = row.Weight,
        };

        return affixData;
    }

    /// <summary>
    /// 转换为羁绊数据
    /// </summary>
    private SynergyData ConvertToSynergyData(SynergyTable row)
    {
        var synergyData = new SynergyData
        {
            Id = row.Id,
            Name = row.Name,
            Type = (SynergyType)row.Type,
            Description = row.Description,
            RequireCount = row.RequireCount,
            RequireIds = row.GetRequireIdList(),
            EffectId = row.EffectId,
        };

        return synergyData;
    }

    /// <summary>
    /// 解析整数列表（逗号分隔）
    /// </summary>
    private List<int> ParseIntList(string str)
    {
        var list = new List<int>();

        if (string.IsNullOrEmpty(str))
        {
            return list;
        }

        var parts = str.Split(',');
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int value))
            {
                list.Add(value);
            }
        }

        return list;
    }

    /// <summary>
    /// 解析属性字典（JSON格式）
    /// </summary>
    private Dictionary<AttributeType, float> ParseAttributes(string json)
    {
        var dict = new Dictionary<AttributeType, float>();

        if (string.IsNullOrEmpty(json) || json == "{}")
        {
            return dict;
        }

        try
        {
            // 使用 Newtonsoft.Json 解析
            var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);

            foreach (var property in jObject.Properties())
            {
                string name = property.Name;
                // 兼容旧配置表中的属性名
                if (name == "MagicPower") name = "SpellPower";

                if (System.Enum.TryParse<AttributeType>(name, out var attrType))
                {
                    float value = property.Value.ToObject<float>();
                    dict[attrType] = value;
                }
                else
                {
                    DebugEx.Warning("ItemManager", $"未知的属性类型: {property.Name}");
                }
            }
        }
        catch (System.Exception e)
        {
            DebugEx.Error("ItemManager", $"解析属性JSON失败: {json}, Error:{e.Message}");
        }

        return dict;
    }

    #endregion
}
