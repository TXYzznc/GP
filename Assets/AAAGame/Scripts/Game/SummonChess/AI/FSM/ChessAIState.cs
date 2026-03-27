/// <summary>
/// 棋子AI状态枚举
/// 定义AI的所有可能状态
/// </summary>
public enum ChessAIState
{
    /// <summary>召唤状态 - 棋子刚被召唤出来，播放召唤动画</summary>
    Summoning = 0,
    
    /// <summary>待机状态 - 无目标或等待决策，是核心决策状态</summary>
    Idle = 1,
    
    /// <summary>移动状态 - 向目标移动中</summary>
    Moving = 2,
    
    /// <summary>普攻状态 - 执行普通攻击</summary>
    Attacking = 3,
    
    /// <summary>使用技能状态 - 释放技能中</summary>
    UsingSkill = 4,
    
    /// <summary>死亡状态 - 棋子已死亡，终态</summary>
    Dead = 5
}
