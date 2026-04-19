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
/// 物品品质
/// </summary>
public enum ItemQuality
{
    Common = 1, // 普通（白色）
    Uncommon = 2, // 优秀（绿色）
    Rare = 3, // 稀有（蓝色）
    Epic = 4, // 史诗（紫色）
    Legendary = 5, // 传说（橙色）
}

#endregion

#region 效果类型枚举

/// <summary>
/// 特殊效果类型
/// </summary>
public enum SpecialEffectType
{
    ConsumableEffect = 1, // 消耗品效果
    EquipmentEffect = 2, // 装备特效
    TreasureEffect = 3, // 宝物特效
    SynergyEffect = 4, // 羁绊效果
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
/// 属性类型（对应棋子属性）
/// </summary>
public enum AttributeType
{
    All = 0, // 全属性
    Attack = 1, // 攻击力
    MaxHP = 2, // 生命值
    CritRate = 3, // 暴击率
    AttackSpeed = 4, // 攻击速度
    MoveSpeed = 5, // 移动速度
    Defense = 6, // 防御力
    MagicPower = 7, // 魔法强度
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
/// 羁绊类型
/// </summary>
public enum SynergyType
{
    Chess = 1, // 棋子羁绊
    Treasure = 2, // 宝物羁绊
}

#endregion
