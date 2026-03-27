using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// ItemTable 扩展类 - 提供辅助方法和属性解析
/// </summary>
public partial class ItemTable
{
    #region 辅助属性

    /// <summary>
    /// 获取词条池ID列表（转换为List）
    /// </summary>
    public List<int> GetAffixPoolIdList()
    {
        if (AffixPoolIds == null || AffixPoolIds.Length == 0)
        {
            return new List<int>();
        }
        return new List<int>(AffixPoolIds);
    }

    /// <summary>
    /// 获取羁绊ID列表（转换为List）
    /// </summary>
    public List<int> GetSynergyIdList()
    {
        if (SynergyIds == null || SynergyIds.Length == 0)
        {
            return new List<int>();
        }
        return new List<int>(SynergyIds);
    }

    /// <summary>
    /// 解析基础属性（JSON格式）
    /// 格式：{"Attack":"60","AttackSpeed":"10%","MaxHP":"200"}
    /// 支持固定数值（如 "60"）和百分比（如 "10%"，解析后存储为 0.1）
    /// </summary>
    public Dictionary<AttributeType, float> ParseBaseAttributes()
    {
        var result = new Dictionary<AttributeType, float>();

        if (string.IsNullOrEmpty(BaseAttributes) || BaseAttributes == "{}")
        {
            return result;
        }

        try
        {
            // 使用 Newtonsoft.Json 解析 JSON
            var jObject = Newtonsoft.Json.Linq.JObject.Parse(BaseAttributes);

            foreach (var property in jObject.Properties())
            {
                // 尝试将属性名转换为 AttributeType 枚举
                if (System.Enum.TryParse<AttributeType>(property.Name, out var attrType))
                {
                    string valueStr = property.Value.ToString();
                    float value;

                    // 检查是否为百分比
                    if (valueStr.EndsWith("%"))
                    {
                        // 去掉百分号，转换为小数（35% -> 0.35）
                        string numStr = valueStr.Substring(0, valueStr.Length - 1);
                        if (float.TryParse(numStr, out float percentage))
                        {
                            value = percentage / 100f;
                        }
                        else
                        {
                            DebugEx.WarningModule(
                                "ItemTable",
                                $"解析百分比失败 ID:{Id}, 属性:{property.Name}, 值:{valueStr}"
                            );
                            continue;
                        }
                    }
                    else
                    {
                        // 普通数值
                        if (!float.TryParse(valueStr, out value))
                        {
                            DebugEx.WarningModule(
                                "ItemTable",
                                $"解析数值失败 ID:{Id}, 属性:{property.Name}, 值:{valueStr}"
                            );
                            continue;
                        }
                    }

                    result[attrType] = value;
                }
                else
                {
                    DebugEx.WarningModule(
                        "ItemTable",
                        $"未知的属性类型 ID:{Id}, 属性名:{property.Name}"
                    );
                }
            }
        }
        catch (System.Exception e)
        {
            DebugEx.ErrorModule(
                "ItemTable",
                $"解析基础属性失败 ID:{Id}, BaseAttributes:{BaseAttributes}, Error:{e.Message}"
            );
        }

        return result;
    }

    #endregion
}
