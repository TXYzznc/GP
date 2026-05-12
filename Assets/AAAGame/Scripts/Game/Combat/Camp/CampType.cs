/// <summary>
/// 阵营类型枚举
/// 定义游戏中所有可能的阵营
/// </summary>
public enum CampType
{
    /// <summary>玩家阵营（由玩家控制的单位）</summary>
    Player = 0,

    /// <summary>敌人阵营（AI控制的敌对单位）</summary>
    Enemy = 1,

    /// <summary>中立阵营（不参与战斗）</summary>
    Neutral = 2,

    /// <summary>动态战场阵营起始值（100起，每个战场占用2个：100/101, 102/103...）</summary>
    DynamicBattleStart = 100,
}
