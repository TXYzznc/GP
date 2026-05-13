/// <summary>
/// 数据表助手 - SummonChessTable 按等级读取扩展
/// 根据棋子等级（1-3）从数组中提取对应数据
/// </summary>
public static class SummonChessTableHelper
{
    /// <summary>
    /// 根据等级从数组中获取数据
    /// rank: 1-3（超出范围使用 array[0]）
    /// </summary>
    public static T GetByRank<T>(T[] array, int rank) where T : struct
    {
        if (array == null || array.Length == 0)
            return default;

        if (rank <= 0 || rank > array.Length)
            return array[0];

        return array[rank - 1];
    }

    /// <summary>获取特定等级的预制体ID</summary>
    public static int GetPrefabId(this SummonChessTable row, int rank)
        => GetByRank(row.PrefabId, rank);

    /// <summary>获取特定等级的图标ID</summary>
    public static int GetIconId(this SummonChessTable row, int rank)
        => GetByRank(row.IconId, rank);

    /// <summary>获取特定等级的最大生命值</summary>
    public static double GetMaxHp(this SummonChessTable row, int rank)
        => GetByRank(row.MaxHp, rank);

    /// <summary>获取特定等级的最大法力值</summary>
    public static double GetMaxMp(this SummonChessTable row, int rank)
        => GetByRank(row.MaxMp, rank);

    /// <summary>获取特定等级的初始法力值</summary>
    public static double GetInitialMp(this SummonChessTable row, int rank)
        => GetByRank(row.InitialMp, rank);

    /// <summary>获取特定等级的攻击力</summary>
    public static double GetAtkDamage(this SummonChessTable row, int rank)
        => GetByRank(row.AtkDamage, rank);

    /// <summary>获取特定等级的攻击速度</summary>
    public static double GetAtkSpeed(this SummonChessTable row, int rank)
        => GetByRank(row.AtkSpeed, rank);

    /// <summary>获取特定等级的攻击距离</summary>
    public static double GetAtkRange(this SummonChessTable row, int rank)
        => GetByRank(row.AtkRange, rank);

    /// <summary>获取特定等级的护甲</summary>
    public static double GetArmor(this SummonChessTable row, int rank)
        => GetByRank(row.Armor, rank);

    /// <summary>获取特定等级的魔抗</summary>
    public static double GetMagicResist(this SummonChessTable row, int rank)
        => GetByRank(row.MagicResist, rank);

    /// <summary>获取特定等级的暴击率</summary>
    public static double GetCritRate(this SummonChessTable row, int rank)
        => GetByRank(row.CritRate, rank);

    /// <summary>获取特定等级的暴击伤害倍率</summary>
    public static double GetCritDamage(this SummonChessTable row, int rank)
        => GetByRank(row.CritDamage, rank);

    /// <summary>获取特定等级的法术强度</summary>
    public static double GetSpellPower(this SummonChessTable row, int rank)
        => GetByRank(row.SpellPower, rank);

    /// <summary>获取被动技能ID数组</summary>
    public static int[] GetPassiveIds(this SummonChessTable row)
        => row.PassiveIds ?? System.Array.Empty<int>();

    /// <summary>获取特定等级的普攻技能ID</summary>
    public static int GetNormalAtkId(this SummonChessTable row, int rank)
        => GetByRank(row.NormalAtkId, rank);

    /// <summary>获取特定等级的技能1 ID</summary>
    public static int GetSkill1Id(this SummonChessTable row, int rank)
        => GetByRank(row.Skill1Id, rank);

    /// <summary>获取特定等级的技能2 ID（可选的第二小技能）</summary>
    public static int GetSkill2Id(this SummonChessTable row, int rank)
        => GetByRank(row.Skill2Id, rank);

    /// <summary>获取特定等级的大招 ID</summary>
    public static int GetUltimateId(this SummonChessTable row, int rank)
        => GetByRank(row.UltimateId, rank);

    /// <summary>获取特定等级的故事文本</summary>
    public static string GetStoryText(this SummonChessTable row, int rank)
    {
        if (row.StoryText == null || row.StoryText.Length == 0)
            return string.Empty;
        if (rank <= 0 || rank > row.StoryText.Length)
            return row.StoryText[0] ?? string.Empty;
        return row.StoryText[rank - 1] ?? string.Empty;
    }

    /// <summary>获取特定等级的描述（兼容旧方法，推荐使用 GetStoryText）</summary>
    public static string GetDescription(this SummonChessTable row, int rank)
        => GetStoryText(row, rank);
}
