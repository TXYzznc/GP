using System.Collections.Generic;

/// <summary>
/// 索敌策略接口
/// 定义如何从敌人列表中选择最优目标
/// 使用策略模式，支持不同的索敌逻辑
/// </summary>
public interface ITargetSearchStrategy
{
    /// <summary>
    /// 从敌人列表中选择最优目标
    /// </summary>
    /// <param name="self">自身棋子实体</param>
    /// <param name="enemies">敌人信息缓存列表</param>
    /// <returns>最优目标，如果没有合适目标则返回 null</returns>
    ChessEntity SelectBestTarget(ChessEntity self, List<EnemyInfoCache> enemies);
}
