/// <summary>
/// 战斗触发类型
/// 定义战斗如何被触发
/// </summary>
public enum CombatTriggerType
{
    /// <summary>普通触发 - 敌人追到玩家距离内触发</summary>
    Normal = 0,

    /// <summary>偷袭 - 玩家从背后接近未被发现的敌人触发</summary>
    SneakAttack = 1,

    /// <summary>遭遇战 - 玩家面对未被发现的敌人触发</summary>
    Encounter = 2,

    /// <summary>敌方先手 - 敌人先进入警觉并发起战斗</summary>
    EnemyInitiated = 3,
}
