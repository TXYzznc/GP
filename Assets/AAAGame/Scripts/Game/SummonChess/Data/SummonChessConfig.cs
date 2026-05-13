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
    public string[] StoryText; // 背景故事（按等级 1-3）
    #endregion

    #region 种族与职业

    public int[] Races; // 种族ID数组
    public int[] Classes; // 职业ID数组
    #endregion

    #region 资源ID（按等级）

    public int[] PrefabId; // 预制体资源ID（按等级 1-3）
    public int[] IconId; // 图标资源ID（按等级 1-3）
    #endregion

    #region 基础数值（按等级）

    public double[] MaxHp; // 最大生命值（按等级 1-3）
    public double[] MaxMp; // 最大法力值（按等级 1-3）
    public double[] InitialMp; // 初始法力值（按等级 1-3）
    public double[] AtkDamage; // 攻击力（按等级 1-3）
    public double[] AtkSpeed; // 攻击速度（按等级 1-3）
    public double[] AtkRange; // 攻击范围（按等级 1-3）
    public double[] Armor; // 护甲（按等级 1-3）
    public double[] MagicResist; // 魔抗（按等级 1-3）
    public double MoveSpeed; // 移动速度（不变等级）
    public double[] CritRate; // 暴击率（按等级 1-3）
    public double[] CritDamage; // 暴击伤害倍率（按等级 1-3）
    public double[] SpellPower; // 法术强度（按等级 1-3）
    public double Shield; // 初始护盾值（不变等级）
    public double CooldownReduce; // 冷却缩减百分比（不变等级）
    #endregion

    #region 技能与AI（按等级）

    public int[] PassiveIds; // 被动技能ID数组（按等级 1-3）
    public int[] NormalAtkId; // 普攻效果配置ID（按等级 1-3）
    public int[] Skill1Id; // 技能一ID（按等级 1-3）
    public int[] Skill2Id; // 技能二ID（可选的第二小技能，按等级 1-3）
    public int[] UltimateId; // 大招ID（按等级 1-3）
    public int AIType; // AI类型（1=近战，2=远程）
    public int AttackHitType; // 普攻命中检测类型（0=瞬间，1=近战，2=投射物，3=AOE，4=射线）
    public int ProjectilePrefabId; // 投射物预制体ID（AttackHitType=2时使用）
    #endregion

    #region 自定义数据

    public string CustomData; // 自定义数据（JSON格式，用于特殊机制如多阶段）
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

        // 验证数值
        if (MaxHp == null || MaxHp.Length == 0 || MaxHp[0] <= 0)
        {
            errorMsg = $"Invalid MaxHp: {MaxHp} for Id: {Id}";
            return false;
        }

        if (MaxMp == null || MaxMp.Length == 0 || MaxMp[0] < 0)
        {
            errorMsg = $"Invalid MaxMp for Id: {Id}";
            return false;
        }

        if (InitialMp == null || InitialMp.Length == 0 || InitialMp[0] < 0 || InitialMp[0] > MaxMp[0])
        {
            errorMsg = $"Invalid InitialMp for Id: {Id}";
            return false;
        }

        if (AtkDamage == null || AtkDamage.Length == 0 || AtkDamage[0] < 0)
        {
            errorMsg = $"Invalid AtkDamage for Id: {Id}";
            return false;
        }

        if (AtkSpeed == null || AtkSpeed.Length == 0 || AtkSpeed[0] <= 0)
        {
            errorMsg = $"Invalid AtkSpeed for Id: {Id}";
            return false;
        }

        if (AtkRange == null || AtkRange.Length == 0 || AtkRange[0] <= 0)
        {
            errorMsg = $"Invalid AtkRange for Id: {Id}";
            return false;
        }

        if (MoveSpeed < 0)
        {
            errorMsg = $"Invalid MoveSpeed: {MoveSpeed} for Id: {Id}";
            return false;
        }

        if (CritRate == null || CritRate.Length == 0 || CritRate[0] < 0 || CritRate[0] > 1)
        {
            errorMsg = $"Invalid CritRate for Id: {Id}";
            return false;
        }

        if (CritDamage == null || CritDamage.Length == 0 || CritDamage[0] < 1)
        {
            errorMsg = $"Invalid CritDamage for Id: {Id}";
            return false;
        }

        if (SpellPower == null || SpellPower.Length == 0 || SpellPower[0] < 0)
        {
            errorMsg = $"Invalid SpellPower for Id: {Id}";
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
