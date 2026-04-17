using System;
using UnityEngine;

/// <summary>
/// 召唤棋子配置数据
/// 对应数据表 SummonChessTable 的一行数据
/// </summary>
[Serializable]
public class SummonChessConfig
{
    #region 基础信息

    public int Id; // 棋子ID
    public string Name; // 棋子名称
    public int Quality; // 品质（1-4：白、绿、蓝、紫）
    public int PopCost; // 人口消耗
    public string Description; // 描述
    #endregion

    #region 种族与职业

    public int[] Races; // 种族ID数组
    public int[] Classes; // 职业ID数组
    #endregion

    #region 星级系统

    public int StarLevel; // 星级（1-3）
    public int NextStarId; // 下一星级ID（0表示无法升星）
    #endregion

    #region 资源ID

    public int PrefabId; // 预制体资源ID
    public int IconId; // 图标资源ID
    #endregion

    #region 基础数值

    public double MaxHp; // 最大生命值
    public double MaxMp; // 最大法力值
    public double InitialMp; // 初始法力值
    public double AtkDamage; // 攻击力
    public double AtkSpeed; // 攻击速度
    public double AtkRange; // 攻击范围
    public double Armor; // 护甲
    public double MagicResist; // 魔抗
    public double MoveSpeed; // 移动速度
    public double CritRate; // 暴击率
    public double CritDamage; // 暴击伤害倍率
    public double SpellPower; // 法术强度
    public double Shield; // 初始护盾值
    public double CooldownReduce; // 冷却缩减百分比
    #endregion

    #region 技能与AI

    public int[] PassiveIds; // 被动技能ID数组
    public int NormalAtkId; // 普攻效果配置ID
    public int Skill1Id; // 技能一ID（0=无）
    public int Skill2Id; // 技能二/大招ID（0=无）
    public int AIType; // AI类型（1=近战，2=远程）
    public int AttackHitType; // 普攻命中检测类型（0=瞬间，1=近战，2=投射物，3=AOE，4=射线）
    public int ProjectilePrefabId; // 投射物预制体ID（AttackHitType=2时使用）
    #endregion

    #region 数据验证

    /// <summary>
    /// 验证配置数据的有效性
    /// </summary>
    public bool Validate(out string errorMsg)
    {
        errorMsg = string.Empty;

        // 验证ID
        if (Id <= 0)
        {
            errorMsg = $"Invalid Id: {Id}";
            return false;
        }

        // 验证名称
        if (string.IsNullOrEmpty(Name))
        {
            errorMsg = $"Invalid Name for Id: {Id}";
            return false;
        }

        // 验证品质
        if (Quality < 1 || Quality > 4)
        {
            errorMsg = $"Invalid Quality: {Quality} for Id: {Id}";
            return false;
        }

        // 验证星级
        if (StarLevel < 1 || StarLevel > 3)
        {
            errorMsg = $"Invalid StarLevel: {StarLevel} for Id: {Id}";
            return false;
        }

        // 验证数值
        if (MaxHp <= 0)
        {
            errorMsg = $"Invalid MaxHp: {MaxHp} for Id: {Id}";
            return false;
        }

        if (MaxMp < 0)
        {
            errorMsg = $"Invalid MaxMp: {MaxMp} for Id: {Id}";
            return false;
        }

        if (InitialMp < 0 || InitialMp > MaxMp)
        {
            errorMsg = $"Invalid InitialMp: {InitialMp} (MaxMp: {MaxMp}) for Id: {Id}";
            return false;
        }

        if (AtkDamage < 0)
        {
            errorMsg = $"Invalid AtkDamage: {AtkDamage} for Id: {Id}";
            return false;
        }

        if (AtkSpeed <= 0)
        {
            errorMsg = $"Invalid AtkSpeed: {AtkSpeed} for Id: {Id}";
            return false;
        }

        if (AtkRange <= 0)
        {
            errorMsg = $"Invalid AtkRange: {AtkRange} for Id: {Id}";
            return false;
        }

        if (MoveSpeed < 0)
        {
            errorMsg = $"Invalid MoveSpeed: {MoveSpeed} for Id: {Id}";
            return false;
        }

        if (CritRate < 0 || CritRate > 1)
        {
            errorMsg = $"Invalid CritRate: {CritRate} for Id: {Id}";
            return false;
        }

        if (CritDamage < 1)
        {
            errorMsg = $"Invalid CritDamage: {CritDamage} for Id: {Id} (should >= 1.0)";
            return false;
        }

        if (SpellPower < 0)
        {
            errorMsg = $"Invalid SpellPower: {SpellPower} for Id: {Id}";
            return false;
        }

        if (Shield < 0)
        {
            errorMsg = $"Invalid Shield: {Shield} for Id: {Id}";
            return false;
        }

        if (CooldownReduce < 0 || CooldownReduce > 1)
        {
            errorMsg = $"Invalid CooldownReduce: {CooldownReduce} for Id: {Id}";
            return false;
        }

        // 验证被动技能
        if (PassiveIds == null)
        {
            PassiveIds = Array.Empty<int>();
        }

        // 验证AI类型（0 = 无AI，如召唤师棋子占位）
        if (AIType < 0)
        {
            errorMsg = $"Invalid AIType: {AIType} for Id: {Id}";
            return false;
        }

        // 验证数组字段（允许为空或null）
        if (Races == null)
        {
            Races = Array.Empty<int>();
        }

        if (Classes == null)
        {
            Classes = Array.Empty<int>();
        }

        return true;
    }

    #endregion
}
