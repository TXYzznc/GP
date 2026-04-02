/// <summary>
/// 场景类型枚举
/// </summary>
public enum SceneType
{
    /// <summary>
    /// 未知场景
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// 基地场景（局外）
    /// </summary>
    Base = 1,
    
    /// <summary>
    /// 大世界场景（局内）
    /// </summary>
    World = 2,
    
    /// <summary>
    /// 新手引导场景（局内）
    /// </summary>
    Tutorial = 3,
    
    /// <summary>
    /// 特殊副本场景（局内）
    /// </summary>
    Dungeon = 4,

    /// <summary>
    /// 主菜单场景
    /// </summary>
    Menu = 5
}

/// <summary>
/// 场景进入条件类型
/// </summary>
public enum SceneConditionType
{
    /// <summary>
    /// 无条件（任何人都可以进入）
    /// </summary>
    None = 0,
    
    /// <summary>
    /// 需要完成引导
    /// </summary>
    CompleteTutorial = 1,
    
    /// <summary>
    /// 需要完成指定任务
    /// 参数：任务ID 或 多个任务ID（逗号分隔）
    /// </summary>
    CompleteQuest = 2,
    
    /// <summary>
    /// 需要达到指定等级
    /// 参数：等级要求
    /// </summary>
    ReachLevel = 3,
    
    /// <summary>
    /// 需要拥有指定物品
    /// 参数：物品ID 或 多个物品ID（逗号分隔）
    /// </summary>
    HasItem = 4,
    
    /// <summary>
    /// 需要完成指定科技
    /// 参数：科技ID 或 多个科技ID（逗号分隔）
    /// </summary>
    UnlockTech = 5,
    
    /// <summary>
    /// 自定义条件（通过代码检查）
    /// 参数：条件检查函数名
    /// </summary>
    Custom = 99
}
