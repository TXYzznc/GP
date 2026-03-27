/// <summary>
/// 棋子状态枚举
/// 用于管理棋子的行为状态
/// </summary>
public enum ChessState
{
    /// <summary>待机状态</summary>
    Idle = 0,

    /// <summary>移动状态</summary>
    Moving = 1,

    /// <summary>攻击状态</summary>
    Attacking = 2,

    /// <summary>施法状态</summary>
    Casting = 3,

    /// <summary>死亡状态</summary>
    Dead = 4,

    /// <summary>持续施法状态（引导技能中）</summary>
    Channeling = 5,

    /// <summary>控制受限状态（眩晕、沉默、禁锢等）</summary>
    Stunned = 6
}
