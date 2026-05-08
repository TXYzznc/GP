using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成配置解析工具
/// 解析加权字符串格式：101:30,102:50,103:20 → {(101, 30), (102, 50), (103, 20)}
/// </summary>
public static class SpawnConfigParser
{
    /// <summary>
    /// 解析加权配置字符串为字典
    /// 格式: "101:30,102:50,103:20" → {101: 30, 102: 50, 103: 20}
    /// </summary>
    public static Dictionary<int, int> ParseWeightedConfig(string configStr)
    {
        var result = new Dictionary<int, int>();

        if (string.IsNullOrWhiteSpace(configStr))
            return result;

        var pairs = configStr.Split(',');
        foreach (var pair in pairs)
        {
            var parts = pair.Trim().Split(':');
            if (parts.Length != 2)
            {
                DebugEx.Warning("SpawnConfigParser", $"无效的权重配置格式: {pair}");
                continue;
            }

            if (!int.TryParse(parts[0].Trim(), out int id) ||
                !int.TryParse(parts[1].Trim(), out int weight))
            {
                DebugEx.Warning("SpawnConfigParser", $"权重解析失败: {pair}");
                continue;
            }

            if (weight <= 0)
            {
                DebugEx.Warning("SpawnConfigParser", $"权重必须大于0: {pair}");
                continue;
            }

            result[id] = weight;
        }

        return result;
    }

    /// <summary>
    /// 根据权重从ID列表中随机选择一个
    /// </summary>
    public static int PickWeightedRandom(Dictionary<int, int> weightedDict)
    {
        if (weightedDict.Count == 0)
            return 0;

        if (weightedDict.Count == 1)
        {
            foreach (var kvp in weightedDict)
                return kvp.Key;
        }

        // 计算总权重
        int totalWeight = 0;
        foreach (var weight in weightedDict.Values)
            totalWeight += weight;

        if (totalWeight <= 0)
            return 0;

        // 随机选择
        int randomValue = Random.Range(0, totalWeight);
        int accumulated = 0;

        foreach (var kvp in weightedDict)
        {
            accumulated += kvp.Value;
            if (randomValue < accumulated)
                return kvp.Key;
        }

        // 兜底：返回最后一个
        foreach (var kvp in weightedDict)
            return kvp.Key;

        return 0;
    }

    /// <summary>
    /// 从数组获取随机数（包含min，包含max）
    /// </summary>
    public static int RandomFromRange(int[] range)
    {
        if (range == null || range.Length < 2)
            return 0;

        int min = Mathf.Min(range[0], range[1]);
        int max = Mathf.Max(range[0], range[1]);

        return Random.Range(min, max + 1);
    }

    /// <summary>
    /// 从数组获取随机浮点数（包含min，包含max）
    /// </summary>
    public static float RandomFromRangeFloat(int[] range)
    {
        if (range == null || range.Length < 2)
            return 0f;

        float min = Mathf.Min(range[0], range[1]);
        float max = Mathf.Max(range[0], range[1]);

        return Random.Range(min, max);
    }
}
