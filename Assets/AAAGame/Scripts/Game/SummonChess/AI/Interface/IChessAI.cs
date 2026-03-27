using UnityEngine;

/// <summary>
/// 棋子AI接口
/// 定义棋子的战斗行为
/// 参考 IPlayerSkill 的设计
/// </summary>
public interface IChessAI
{
    /// <summary>
    /// 初始化AI
    /// </summary>
    /// <param name="ctx">棋子上下文</param>
    void Init(ChessContext ctx);

    /// <summary>
    /// 每帧更新AI逻辑
    /// </summary>
    /// <param name="dt">帧间隔时间</param>
    void Tick(float dt);

    /// <summary>
    /// 寻找攻击目标
    /// </summary>
    /// <returns>目标棋子实体，如果没有目标则返回null</returns>
    ChessEntity FindTarget();

    /// <summary>
    /// 执行移动
    /// </summary>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="dt">帧间隔时间</param>
    void Move(Vector3 targetPosition, float dt);

    /// <summary>
    /// 执行攻击
    /// </summary>
    /// <param name="target">攻击目标</param>
    void Attack(ChessEntity target);
    
    /// <summary>
    /// 攻击完成回调（由 CombatController 调用）
    /// </summary>
    void OnAttackComplete();
    
    /// <summary>
    /// 重置目标状态（用于手动移动完成后）
    /// </summary>
    void ResetTargetAfterPlayerMove();
}
