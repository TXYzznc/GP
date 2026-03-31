using UnityEngine;
using GameFramework;

/// <summary>
/// 物品稀有度颜色映射。
/// 从 ColorTable 配置表读取颜色数据。
/// </summary>
public static class RarityColorHelper
{
    // 默认格子底色（无物品时）
    public static readonly Color DefaultBg = new Color(0.15f, 0.15f, 0.15f, 1f);

    // 缓存的颜色数据（避免频繁查表）
    private static readonly System.Collections.Generic.Dictionary<int, Color> s_ColorCache = 
        new System.Collections.Generic.Dictionary<int, Color>();

    /// <summary>
    /// 根据稀有度值获取对应颜色
    /// 从 ColorTable 读取，缓存结果以提高性能
    /// </summary>
    public static Color GetColor(int rarity)
    {
        // 检查缓存
        if (s_ColorCache.TryGetValue(rarity, out var cachedColor))
        {
            return cachedColor;
        }

        // 从配置表读取颜色
        var colorTable = GF.DataTable.GetDataTable<ColorTable>();
        if (colorTable == null)
        {
            DebugEx.Warning("RarityColorHelper", "ColorTable 未加载");
            return DefaultBg;
        }

        var colorRow = colorTable.GetDataRow(rarity);
        if (colorRow == null)
        {
            DebugEx.Warning("RarityColorHelper", $"ColorTable 中不存在 ID={rarity} 的颜色配置");
            return DefaultBg;
        }

        // 解析十六进制色值
        Color color = HexToColor(colorRow.ColorHex);
        
        // 缓存结果
        s_ColorCache[rarity] = color;
        
        DebugEx.Log("RarityColorHelper", $"加载颜色: ID={rarity}, Name={colorRow.ColorName}, Hex={colorRow.ColorHex}");
        
        return color;
    }

    /// <summary>
    /// 将十六进制色值转换为 Unity Color
    /// 支持格式：#RRGGBB 或 RRGGBB
    /// </summary>
    private static Color HexToColor(string hex)
    {
        // 移除 # 符号
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        // 验证长度
        if (hex.Length != 6)
        {
            DebugEx.Error("RarityColorHelper", $"无效的十六进制色值: {hex}");
            return Color.white;
        }

        // 解析 RGB 值
        if (!int.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int r) ||
            !int.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out int g) ||
            !int.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
        {
            DebugEx.Error("RarityColorHelper", $"十六进制色值解析失败: {hex}");
            return Color.white;
        }

        // 转换为 0-1 范围的浮点数
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    /// <summary>
    /// 清空颜色缓存（在重新加载配置表时调用）
    /// </summary>
    public static void ClearCache()
    {
        s_ColorCache.Clear();
        DebugEx.Log("RarityColorHelper", "颜色缓存已清空");
    }
}
