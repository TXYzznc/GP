/// <summary>
/// 敌人动画类型枚举
/// 对应 Animator 中的 AnimType 参数值
/// </summary>
public enum EnemyAnimationType
{
    Idle = 0,       // 待机
    Walk = 1,       // 行走/巡逻
    Alert = 2,      // 警戒
    Run = 3,        // 追击/奔跑
    Attack = 4,     // 攻击（预留）
    Death = 5,      // 死亡
    Rest = 6,       // 休息
}
