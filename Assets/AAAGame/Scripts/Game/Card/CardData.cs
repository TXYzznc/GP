using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 策略卡数据类
/// </summary>
public class CardData
{
    #region 字段

    /// <summary>卡牌ID</summary>
    public int CardId { get; private set; }

    /// <summary>配置表行引用</summary>
    public CardTable TableRow { get; private set; }

    /// <summary>是否已使用</summary>
    public bool IsUsed { get; set; }

    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }

    /// <summary>释放时施加的 Buff ID 列表（由 InstantBuffs 字段解析）</summary>
    public int[] InstantBuffIds { get; private set; }

    /// <summary>命中时施加的 Buff ID 列表（由 HitBuffs 字段解析）</summary>
    public int[] HitBuffIds { get; private set; }

    /// <summary>ParamsConfig 解析后的 JSON 对象（缓存）</summary>
    private JObject m_ParamsJson;

    #endregion

    #region 构造函数

    public CardData(int cardId, CardTable tableRow)
    {
        CardId = cardId;
        TableRow = tableRow;
        IsUsed = false;
        IsSelected = false;

        InstantBuffIds = ParseBuffIdList(tableRow?.InstantBuffs);
        HitBuffIds = ParseBuffIdList(tableRow?.HitBuffs);
        m_ParamsJson = ParseParamsConfig(tableRow?.ParamsConfig);
    }

    #endregion

    #region 便捷属性

    public string Name => TableRow?.Name ?? "";
    public string Desc => TableRow?.Desc ?? "";
    public string StoryText => TableRow?.StoryText ?? "";
    public int IconId => TableRow?.IconId ?? 0;
    public float SpiritCost => TableRow?.SpiritCost ?? 0;
    public CardTargetType CTargetType => (CardTargetType)(TableRow?.TargetType ?? 0);
    public float AreaRadius => TableRow?.AreaRadius ?? 0;

    #endregion

    #region ParamsConfig 读取

    /// <summary>
    /// 从 ParamsConfig 获取 float 参数
    /// </summary>
    public float GetParam(string key, float defaultValue = 0f)
    {
        if (m_ParamsJson == null || !m_ParamsJson.TryGetValue(key, out var token))
            return defaultValue;
        return token.ToObject<float>();
    }

    /// <summary>
    /// 从 ParamsConfig 获取 int 参数
    /// </summary>
    public int GetParam(string key, int defaultValue = 0)
    {
        if (m_ParamsJson == null || !m_ParamsJson.TryGetValue(key, out var token))
            return defaultValue;
        return token.ToObject<int>();
    }

    /// <summary>
    /// 从 ParamsConfig 获取 string 参数
    /// </summary>
    public string GetParam(string key, string defaultValue = "")
    {
        if (m_ParamsJson == null || !m_ParamsJson.TryGetValue(key, out var token))
            return defaultValue;
        return token.ToString();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 解析 Buff ID 列表，支持格式："5001" 或 "5001,5002,5003"
    /// </summary>
    private static int[] ParseBuffIdList(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "0")
            return Array.Empty<int>();

        string[] parts = raw.Split(',');
        int[] result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            result[i] = int.Parse(parts[i].Trim());
        }
        return result;
    }

    /// <summary>
    /// 解析 ParamsConfig JSON 字符串
    /// </summary>
    private static JObject ParseParamsConfig(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "0")
            return null;

        try
        {
            return JObject.Parse(raw);
        }
        catch (Exception e)
        {
            DebugEx.WarningModule("CardData", $"ParamsConfig 解析失败: {e.Message}, raw={raw}");
            return null;
        }
    }

    #endregion
}
