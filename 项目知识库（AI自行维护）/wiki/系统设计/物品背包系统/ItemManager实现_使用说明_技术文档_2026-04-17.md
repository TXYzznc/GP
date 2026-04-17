# ItemManager 完整实现示例

> **最后更新**: 2026-04-17
> **状态**: 已验证有效
> **核心类**: ItemManager

## 📋 目录

- [完整的数据转换方法](#完整的数据转换方法)
- [修改 LoadItemTable 方法](#修改-loaditemtable-方法)
- [同样修改其他加载方法](#同样修改其他加载方法)
- [注意事项](#注意事项)

---


将以下代码替换 `ItemManager.cs` 中的数据转换方法：

```csharp
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
        IconPath = row.IconPath,
        DetailIconPath = row.DetailIconPath,
        CanStack = row.CanStack == 1,
        MaxStackCount = row.MaxStackCount,
        CanUse = row.CanUse == 1,
        UseEffectId = row.UseEffectId,
        CanEquip = row.CanEquip == 1,
        EquipType = (EquipType)row.EquipType,
        SpecialEffectId = row.SpecialEffectId,
        SellPrice = row.SellPrice
    };
    
    // 解析词条池ID列表
    if (!string.IsNullOrEmpty(row.AffixPoolIds))
    {
        itemData.AffixPoolIds = ParseIntList(row.AffixPoolIds);
    }
    else
    {
        itemData.AffixPoolIds = new List<int>();
    }
    
    // 解析词条数量范围
    if (!string.IsNullOrEmpty(row.AffixCount))
    {
        var range = row.AffixCount.Split('-');
        if (range.Length == 2)
        {
            itemData.AffixMinCount = int.Parse(range[0]);
            itemData.AffixMaxCount = int.Parse(range[1]);
        }
    }
    
    // 解析羁绊ID列表
    if (!string.IsNullOrEmpty(row.SynergyIds))
    {
        itemData.SynergyIds = ParseIntList(row.SynergyIds);
    }
    else
    {
        itemData.SynergyIds = new List<int>();
    }
    
    // 解析基础属性（JSON格式）
    if (!string.IsNullOrEmpty(row.BaseAttributes) && row.BaseAttributes != "{}")
    {
        itemData.BaseAttributes = ParseAttributes(row.BaseAttributes);
    }
    else
    {
        itemData.BaseAttributes = new Dictionary<AttributeType, float>();
    }
    
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
        EffectParams = row.EffectParams
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
        Weight = row.Weight
    };
    
    // 解析数值范围
    if (!string.IsNullOrEmpty(row.ValueRange))
    {
        var range = row.ValueRange.Split('-');
        if (range.Length == 2)
        {
            affixData.ValueMin = float.Parse(range[0]);
            affixData.ValueMax = float.Parse(range[1]);
        }
    }
    
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
        EffectId = row.EffectId
    };
    
    // 解析需求ID列表
    if (!string.IsNullOrEmpty(row.RequireIds))
    {
        synergyData.RequireIds = ParseIntList(row.RequireIds);
    }
    else
    {
        synergyData.RequireIds = new List<int>();
    }
    
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
```

## 修改 LoadItemTable 方法

将原来的 `LoadItemTable` 方法修改为：

```csharp
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

    // 获取所有行数据
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
```

[↑ 返回目录](#目录)

---

## 同样修改其他加载方法

```csharp
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
```

[↑ 返回目录](#目录)

---

## 注意事项

1. 确保项目中已经引用了 `Newtonsoft.Json` 库（用于解析 JSON 格式的属性数据）
2. **API调用正确写法**：使用 `GF.DataTable.GetDataTable<T>()` 来获取配置表（v2026-04-17更新为实际项目API）
3. 所有数据加载方法都应该是 `public` 而非 `private`，因为需要在游戏启动流程中手动调用
4. 必须在数据加载前使用 `.Clear()` 清空字典，避免重复加载时数据累积
5. 所有的枚举转换都使用了强制类型转换，确保配置表中的数值与枚举定义一致
6. 使用 `DebugEx.LogModule()`、`DebugEx.ErrorModule()`、`DebugEx.WarningModule()` 来记录日志

[↑ 返回目录](#目录)
