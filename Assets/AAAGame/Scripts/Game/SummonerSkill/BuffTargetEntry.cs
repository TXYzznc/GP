/// <summary>
/// 技能配置中的 Buff+目标 配对，对应 TXT 中 "buffId:targetType" 格式
/// </summary>
public struct BuffTargetEntry
{
    public int BuffId;

    /// <summary>
    /// 1=召唤师自身  2=全体友方(不含召唤师)  3=全体友方(含召唤师)
    /// 4=全体敌方    5=命中目标(单体，由调用方传入)
    /// </summary>
    public int TargetType;

    /// <summary>
    /// 解析 "4003:2,4004:1" 格式的字符串，返回 BuffTargetEntry 数组。
    /// 值为 "0" 或空时返回空数组。
    /// </summary>
    public static BuffTargetEntry[] ParseArray(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "0")
            return System.Array.Empty<BuffTargetEntry>();

        string[] pairs = raw.Split(',');
        var result = new BuffTargetEntry[pairs.Length];
        for (int i = 0; i < pairs.Length; i++)
        {
            string[] parts = pairs[i].Split(':');
            result[i] = new BuffTargetEntry
            {
                BuffId     = int.Parse(parts[0].Trim()),
                TargetType = parts.Length > 1 ? int.Parse(parts[1].Trim()) : 3,
            };
        }
        return result;
    }
}
