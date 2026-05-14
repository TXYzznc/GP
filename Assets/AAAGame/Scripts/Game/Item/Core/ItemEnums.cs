using System;

#region 物品类型枚举

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Consumable = 1, // 消耗品
    Quest = 2, // 任务道具
    Treasure = 3, // 宝物
    Equipment = 4, // 装备
    Virtual = 5, // 虚拟物品（金币、灵石等资源）
}

/// <summary>
/// 物品稀有度
/// </summary>
public enum ItemRarity
{
    Common    = 1, // 普通（绿色）
    Uncommon  = 2, // 优良（蓝色）
    Rare      = 3, // 稀有（紫色）
    Epic      = 4, // 史诗（金色）
    Legendary = 5, // 传说（炫彩，UI 层需特殊处理渐变效果）
}

#endregion

#region 效果类型枚举

/// <summary>
/// 特殊效果执行类型
/// </summary>
public enum SpecialEffectType
{
    Instant = 1,  // 即时效果（消耗品：加金币、回血等，执行后无状态）
    Buff = 2,     // Buff 效果（通过 BuffManager 添加，有持续时间/图标/可驱散）
    Passive = 3,  // 被动效果（穿戴期间永久生效，直接改属性，无图标/不可驱散）
    Trigger = 4,  // 触发效果（预留：满足条件时触发）
}

/// <summary>
/// 效果来源类型（标识效果由谁提供）
/// </summary>
public enum EffectSourceType
{
    Equipment = 1,  // 装备
    Treasure = 2,   // 宝物
    Synergy = 3,    // 羁绊
    Combat = 4,     // 战斗（先手/偷袭）
    Consumable = 5, // 消耗品
}

/// <summary>
/// 词条类型
/// </summary>
public enum AffixType
{
    AttributeBonus = 1, // 属性加成
    SpecialEffect = 2, // 特殊效果
}

/// <summary>
/// 属性类型（对应棋子属性，与AffixTable.AttributeType列一一对应）
/// </summary>
public enum AttributeType
{
    MaxHP = 0, // 生命值
    Attack = 1, // 攻击力
    MaxMP = 2, // 法力值
    AttackSpeed = 3, // 攻击速度
    CritRate = 4, // 暴击率
    CritDamage = 5, // 暴击伤害
    Defense = 6, // 防御力（护甲）
    MagicResist = 7, // 魔法抗性
    SpellPower = 8, // 法术强度
    MoveSpeed = 9, // 移动速度
    CooldownReduce = 10, // 冷却缩减
}

/// <summary>
/// 数值类型
/// </summary>
public enum ValueType
{
    Fixed = 1, // 固定值
    Percent = 2, // 百分比
}

#endregion

#region 羁绊类型枚举

/// <summary>
/// 羁绊类型（由 SynergyTable.IsTreasureSynergy 字段决定）
/// </summary>
public enum SynergyType
{
    Chess = 1,    // 棋子羁绊（战斗界面显示）
    Treasure = 2  // 宝物羁绊（仅装备者，不在界面显示）
}

#endregion
