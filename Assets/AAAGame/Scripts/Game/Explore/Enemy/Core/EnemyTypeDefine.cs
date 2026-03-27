/// <summary>
/// 敌人类型枚举
/// 对应 EnemyEntityTable.EnemyType 字段
/// </summary>
public enum EnemyType
{
    Normal = 0,     // 普通敌人
    Elite = 1,      // 精英敌人（会广播）
    Boss = 2,       // Boss敌人（可净化，掉落钥匙）
    Special = 3     // 特殊敌人（可净化）
}

/// <summary>
/// 敌人状态枚举
/// </summary>
public enum EnemyStatus
{
    Alive = 0,      // 存活
    Defeated = 1,   // 被击败（等待净化或消失）
    Purified = 2    // 已净化
}

/// <summary>
/// AI状态枚举（扩展）
/// </summary>
public enum EnemyAIState
{
    Idle,                   // 休息：原地待机
    Patrol,                 // 巡逻：在范围内移动
    Alert,                  // 警戒：发现玩家
    Chase,                  // 追击：追击玩家
    AlertedByBroadcast,     // 被广播警戒：收到广播后追击玩家（可被拉入群体战斗）
    Combat,                 // 战斗：战斗中
    Rest,                   // 深度休息：不会被玩家接近惊醒（头上显示睡眠条）
    Defeated                // 被击败：等待净化或消失
}
