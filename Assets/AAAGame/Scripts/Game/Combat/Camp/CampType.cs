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
    
    // ========== 预留PVP扩展 ==========
    /// <summary>队伍1（PVP模式）</summary>
    Team1 = 10,
    
    /// <summary>队伍2（PVP模式）</summary>
    Team2 = 11,
    
    /// <summary>队伍3（PVP模式）</summary>
    Team3 = 12,
    
    /// <summary>队伍4（PVP模式）</summary>
    Team4 = 13,
}
