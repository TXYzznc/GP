/// <summary>
/// 游戏主状态类型
/// </summary>
public enum GameStateType
{
    /// <summary>
    /// 主菜单状态
    /// </summary>
    Menu,

    /// <summary>
    /// 局外状态（基地、城镇等）
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
