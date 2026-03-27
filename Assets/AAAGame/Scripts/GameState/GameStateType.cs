/// <summary>
/// 游戏主状态类型
/// </summary>
public enum GameStateType
{
    /// <summary>
    /// 局外状态（主菜单、角色选择等）
    /// </summary>
    OutOfGame,

    /// <summary>
    /// 局内状态（游戏进行中）
    /// </summary>
    InGame
}

/// <summary>
/// 局内子状态类型
/// </summary>
public enum InGameStateType
{
    /// <summary>
    /// 探索状态（玩家自由控制）
    /// </summary>
    Exploration,

    /// <summary>
    /// 战斗准备状态（选择棋子、查看Buff等）
    /// </summary>
    CombatPreparation,

    /// <summary>
    /// 战斗状态（自动战斗）
    /// </summary>
    Combat
}
