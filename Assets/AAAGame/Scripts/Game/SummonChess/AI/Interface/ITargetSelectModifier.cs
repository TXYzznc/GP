/// <summary>
/// AI 索敌修改器接口
/// 实现此接口的 Buff 可以干预 AI 的目标选择逻辑。
/// 在 ChessAIBase.FindTarget() 中依次调用所有注册的修改器。
/// </summary>
public interface ITargetSelectModifier
{
    /// <summary>
    /// 修改 AI 选出的目标。
    /// </summary>
    /// <param name="originalTarget">策略选出的原始目标（可能为 null）</param>
    /// <param name="context">棋子上下文，用于获取己方/敌方列表等</param>
    /// <returns>最终使用的目标；返回 null 则 AI 本轮不进行攻击</returns>
    ChessEntity ModifyTarget(ChessEntity originalTarget, ChessContext context);
}
