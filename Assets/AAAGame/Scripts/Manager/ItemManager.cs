using System.Collections.Generic;
using GameFramework.DataTable;
using UnityEngine;

/// <summary>
/// 物品管理器
/// </summary>
public class ItemManager : SingletonBase<ItemManager>
{
    #region 字段

    private Dictionary<int, ItemData> m_ItemDataDict; // 物品配置字典
    private Dictionary<int, SpecialEffectData> m_EffectDataDict; // 特殊效果配置字典
    private Dictionary<int, AffixData> m_AffixDataDict; // 词条配置字典
    private Dictionary<int, SynergyData> m_SynergyDataDict; // 羁绊配置字典
    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();

        DebugEx.LogModule("ItemManager", "物品管理器初始化开始");
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

        // 注意：配置表需要先通过 GameFramework 加载
        // 这里只是初始化字典，实际加载需要在配置表准备好后手动调用
        // 可以在游戏启动流程中调用 LoadAllTables() 方法
    }

    /// <summary>
    /// 加载所有配置表（需要在配置表加载完成后调用）
    /// </summary>
    public void LoadAllTables()
    {
        DebugEx.LogModule("ItemManager", "开始加载所有配置表");

        LoadItemTable();
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
        DebugEx.LogModule("ItemManager", "开始加载物品配置表");

        var table = GF.DataTable.GetDataTable<ItemTable>();
        if (table == null)
        {
            DebugEx.ErrorModule("ItemManager", "物品配置表未加载，请先加载配置表");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.WarningModule("ItemManager", "物品配置表为空");
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
    /// 加载特殊效果配置表
    /// </summary>
    public void LoadSpecialEffectTable()
    {
        DebugEx.LogModule("ItemManager", "开始加载特殊效果配置表");

        var table = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (table == null)
        {
            DebugEx.ErrorModule("ItemManager", "特殊效果配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.WarningModule("ItemManager", "特殊效果配置表为空");
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
        DebugEx.LogModule("ItemManager", "开始加载词条配置表");

        var table = GF.DataTable.GetDataTable<AffixTable>();
        if (table == null)
        {
            DebugEx.ErrorModule("ItemManager", "词条配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.WarningModule("ItemManager", "词条配置表为空");
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
        DebugEx.LogModule("ItemManager", "开始加载羁绊配置表");

        var table = GF.DataTable.GetDataTable<SynergyTable>();
        if (table == null)
        {
            DebugEx.ErrorModule("ItemManager", "羁绊配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.WarningModule("ItemManager", "羁绊配置表为空");
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

        DebugEx.WarningModule("ItemManager", $"物品配置不存在 ID:{itemId}");
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

        DebugEx.WarningModule("ItemManager", $"特殊效果配置不存在 ID:{effectId}");
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

        DebugEx.WarningModule("ItemManager", $"词条配置不存在 ID:{affixId}");
        return null;
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

        DebugEx.WarningModule("ItemManager", $"羁绊配置不存在 ID:{synergyId}");
        return null;
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
            DebugEx.ErrorModule("ItemManager", $"创建物品失败，配置不存在 ID:{itemId}");
            return null;
        }

        DebugEx.LogModule("ItemManager", $"创建物品: {itemData.Name} (ID:{itemId})");

        ItemBase item = null;

        switch (itemData.Type)
        {
            case ItemType.Consumable:
                item = new ConsumableItem(itemId, itemData);
                break;

            case ItemType.Quest:
                item = new QuestItem(itemId, itemData);
                break;

            case ItemType.Treasure:
                item = new TreasureItem(itemId, itemData);
                break;

            case ItemType.Equipment:
                item = new EquipmentItem(itemId, itemData);
                break;

            case ItemType.Virtual:
                item = new VirtualItem(itemId, itemData);
                break;

            default:
                DebugEx.ErrorModule("ItemManager", $"未知的物品类型: {itemData.Type}");
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
            Quality = (ItemQuality)row.Quality,
            Description = row.Description,
            IconId = row.IconId,
            DetailIconId = row.DetailIconId,
            CanStack = row.CanStack == 1,
            MaxStackCount = row.MaxStackCount,
            CanUse = row.CanUse == 1,
            UseEffectId = row.UseEffectId,
            CanEquip = row.CanEquip == 1,
            SpecialEffectId = row.SpecialEffectId,
            AffixPoolIds = row.GetAffixPoolIdList(),
            AffixMinCount = row.AffixMinCount,
            AffixMaxCount = row.AffixMaxCount,
            SynergyIds = row.GetSynergyIdList(),
            BaseAttributes = row.ParseBaseAttributes(),
            SellPrice = row.SellPrice,
        };

        return itemData;
    }

    /// <summary>
    /// 转换为特殊效果数据
    /// </summary>
    private SpecialEffectData ConvertToEffectData(SpecialEffectTable row)
    {
        // 构建JSON格式的效果参数
        var buffIds = row.BuffIds ?? new int[0];
        var selfBuffIds = row.SelfBuffIds ?? new int[0];
        string effectParams =
            $"{{\"BuffIds\":[{string.Join(",", buffIds)}],\"SelfBuffIds\":[{string.Join(",", selfBuffIds)}]}}";

        var effectData = new SpecialEffectData
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            EffectType = (SpecialEffectType)row.EffectType,
            EffectParams = effectParams,
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
                // 尝试将属性名转换为 AttributeType 枚举
                if (System.Enum.TryParse<AttributeType>(property.Name, out var attrType))
                {
                    float value = property.Value.ToObject<float>();
                    dict[attrType] = value;
                }
                else
                {
                    DebugEx.WarningModule("ItemManager", $"未知的属性类型: {property.Name}");
                }
            }
        }
        catch (System.Exception e)
        {
            DebugEx.ErrorModule("ItemManager", $"解析属性JSON失败: {json}, Error:{e.Message}");
        }

        return dict;
    }

    #endregion
}
