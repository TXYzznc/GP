/// <summary>
/// 攻击命中检测类型
/// 定义不同的命中检测方式和目标
/// </summary>
public enum AttackHitType
{
    /// <summary>
    /// 瞬发命中（直接对锁定目标造成伤害）
    /// 适用于：自动战斗、点击攻击、单体技能
    /// </summary>
    Instant = 0,

    /// <summary>
    /// 近战碰撞（通过武器Collider检测）
    /// 适用于：近战武器挥砍、格斗战斗
    /// </summary>
    Melee = 1,

    /// <summary>
    /// 投射物（子弹/箭矢，通过碰撞检测）
    /// 适用于：弓箭射击、飞弹、远程攻击
    /// </summary>
    Projectile = 2,

    /// <summary>
    /// 范围检测（OverlapSphere）
    /// 适用于：AOE技能、爆炸、范围伤害
    /// </summary>
    AOE = 3,

    /// <summary>
    /// 射线检测（Raycast）
    /// 适用于：激光、火焰、穿透攻击
    /// </summary>
    Raycast = 4
}
