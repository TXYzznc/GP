using UnityEngine;

/// <summary>
/// 假人AI - 不执行任何行动
/// 用于测试受伤、稻草人等
/// AIType = 3
/// </summary>
public class DummyAI : IChessAI
{
    #region 字段
    private ChessContext m_Context;

    #endregion

    #region IChessAI 实现

    /// <summary>
    /// 初始化AI
    /// </summary>
    public void Init(ChessContext ctx)
    {
        m_Context = ctx;
        DebugEx.LogModule("DummyAI", $"初始化完成: {ctx.Config.Name} - 假人不会执行任何行动");
    }

    /// <summary>
    /// 每帧更新AI逻辑
    /// 假人不执行任何行动
    /// </summary>
    public void Tick(float dt)
    {
        // 假人不执行任何行动
    }

    /// <summary>
    /// 寻找攻击目标
    /// 假人不会攻击，始终返回null
    /// </summary>
    public ChessEntity FindTarget()
    {
        // 假人不攻击
        return null;
    }

    /// <summary>
    /// 执行移动
    /// 假人不移动
    /// </summary>
    public void Move(Vector3 targetPosition, float dt)
    {
        // 假人不移动
    }

    /// <summary>
    /// 执行攻击
    /// 假人不攻击
    /// </summary>
    public void Attack(ChessEntity target)
    {
        // 假人不攻击
    }

    /// <summary>
    /// 攻击完成回调（由 CombatController 调用）
    /// 假人不攻击，此方法为空实现
    /// </summary>
    public void OnAttackComplete()
    {
        // 假人不攻击，无需处理
    }

    /// <summary>
    /// 重置目标状态（用于手动移动完成后）
    /// 假人不攻击，此方法为空实现
    /// </summary>
    public void ResetTargetAfterPlayerMove()
    {
        // 假人不攻击，无需重置目标
    }

    #endregion
}
