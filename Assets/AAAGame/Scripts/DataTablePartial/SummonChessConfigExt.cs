/// <summary>
/// 数据扩展 - SummonChessConfig 按等级读取
/// </summary>
public static class SummonChessConfigHelper
{
    /// <summary>根据等级从配置中获取数据</summary>
    public static T GetByRank<T>(T[] array, int rank) where T : struct
    {
        if (array == null || array.Length == 0)
            return default;

        if (rank <= 0 || rank > array.Length)
            return array[0];

        return array[rank - 1];
    }

    /// <summary>获取特定等级的预制体ID</summary>
    public static int GetPrefabId(this SummonChessConfig config, int rank)
        => GetByRank(config.PrefabId, rank);

    /// <summary>获取特定等级的图标ID</summary>
    public static int GetIconId(this SummonChessConfig config, int rank)
        => GetByRank(config.IconId, rank);

    /// <summary>获取特定等级的最大生命值</summary>
    public static double GetMaxHp(this SummonChessConfig config, int rank)
        => GetByRank(config.MaxHp, rank);

    /// <summary>获取特定等级的最大法力值</summary>
    public static double GetMaxMp(this SummonChessConfig config, int rank)
        => GetByRank(config.MaxMp, rank);

    /// <summary>获取特定等级的初始法力值</summary>
    public static double GetInitialMp(this SummonChessConfig config, int rank)
        => GetByRank(config.InitialMp, rank);

    /// <summary>获取特定等级的攻击力</summary>
    public static double GetAtkDamage(this SummonChessConfig config, int rank)
        => GetByRank(config.AtkDamage, rank);

    /// <summary>获取特定等级的攻击速度</summary>
    public static double GetAtkSpeed(this SummonChessConfig config, int rank)
        => GetByRank(config.AtkSpeed, rank);

    /// <summary>获取特定等级的攻击距离</summary>
    public static double GetAtkRange(this SummonChessConfig config, int rank)
        => GetByRank(config.AtkRange, rank);

    /// <summary>获取特定等级的护甲</summary>
    public static double GetArmor(this SummonChessConfig config, int rank)
        => GetByRank(config.Armor, rank);

    /// <summary>获取特定等级的魔抗</summary>
    public static double GetMagicResist(this SummonChessConfig config, int rank)
        => GetByRank(config.MagicResist, rank);

    /// <summary>获取特定等级的暴击率</summary>
    public static double GetCritRate(this SummonChessConfig config, int rank)
        => GetByRank(config.CritRate, rank);

    /// <summary>获取特定等级的暴击伤害倍率</summary>
    public static double GetCritDamage(this SummonChessConfig config, int rank)
        => GetByRank(config.CritDamage, rank);

    /// <summary>获取特定等级的法术强度</summary>
    public static double GetSpellPower(this SummonChessConfig config, int rank)
        => GetByRank(config.SpellPower, rank);

    /// <summary>获取被动技能ID数组</summary>
    public static int[] GetPassiveIds(this SummonChessConfig config)
        => config.PassiveIds ?? System.Array.Empty<int>();

    /// <summary>获取特定等级的描述</summary>
    public static string GetDescription(this SummonChessConfig config, int rank)
    {
        if (config.Description == null || config.Description.Length == 0)
            return string.Empty;
        if (rank <= 0 || rank > config.Description.Length)
            return config.Description[0] ?? string.Empty;
        return config.Description[rank - 1] ?? string.Empty;
    }

    /// <summary>获取特定等级的普攻技能ID</summary>
    public static int GetNormalAtkId(this SummonChessConfig config, int rank)
        => GetByRank(config.NormalAtkId, rank);

    /// <summary>获取特定等级的技能1 ID</summary>
    public static int GetSkill1Id(this SummonChessConfig config, int rank)
        => GetByRank(config.Skill1Id, rank);

    /// <summary>获取特定等级的技能2 ID</summary>
    public static int GetSkill2Id(this SummonChessConfig config, int rank)
        => GetByRank(config.Skill2Id, rank);
}
