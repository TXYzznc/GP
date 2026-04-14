using System;
using System.Collections.Generic;

/// <summary>
/// 战斗触发事件中心
/// 解耦 CombatTriggerManager 与 UI / State 层的通知依赖
///
/// 使用说明：
/// - 发布方（CombatTriggerManager）调用 Fire* 方法
/// - 订阅方（CombatPreparationState / CombatPreparationUI 等）订阅相应 Action
/// - LastValue 属性提供"最后一次触发值"，供因时序问题错过事件的订阅方补读
/// </summary>
public static class CombatTriggerEvents
{
    #region 事件

    /// <summary>敌方先手触发 — 参数为 effectId（SpecialEffectTable 行 ID）</summary>
    public static event Action<int> OnEnemyInitiativeTriggered;

    /// <summary>玩家偷袭触发 — 参数为可选 Debuff 效果 ID 列表</summary>
    public static event Action<List<int>> OnSneakAttackTriggered;

    /// <summary>玩家先手触发（遭遇战） — 参数为可选 Buff 效果 ID 列表</summary>
    public static event Action<List<int>> OnPlayerInitiativeTriggered;

    /// <summary>战斗上下文清除（战斗结束 / 脱战后调用）</summary>
    public static event Action OnCombatContextCleared;

    #endregion

    #region 最后一次触发值（Late-subscriber 补读）

    /// <summary>最近一次敌方先手效果 ID（0 表示未触发）</summary>
    public static int LastEnemyInitiativeEffectId { get; private set; }

    /// <summary>最近一次偷袭可选 Debuff 池</summary>
    public static List<int> LastSneakDebuffPool { get; private set; }

    /// <summary>最近一次玩家先手可选 Buff 池</summary>
    public static List<int> LastPlayerInitiativeBuffPool { get; private set; }

    #endregion

    #region 发布接口（由 CombatTriggerManager 调用）

    /// <summary>
    /// 触发敌方先手事件
    /// </summary>
    public static void FireEnemyInitiativeTriggered(int effectId)
    {
        LastEnemyInitiativeEffectId = effectId;
        OnEnemyInitiativeTriggered?.Invoke(effectId);
    }

    /// <summary>
    /// 触发玩家偷袭事件
    /// </summary>
    public static void FireSneakAttackTriggered(List<int> debuffPool)
    {
        LastSneakDebuffPool = debuffPool;
        OnSneakAttackTriggered?.Invoke(debuffPool);
    }

    /// <summary>
    /// 触发玩家先手事件（遭遇战三选一）
    /// </summary>
    public static void FirePlayerInitiativeTriggered(List<int> buffPool)
    {
        LastPlayerInitiativeBuffPool = buffPool;
        OnPlayerInitiativeTriggered?.Invoke(buffPool);
    }

    /// <summary>
    /// 触发战斗上下文清除事件
    /// </summary>
    public static void FireCombatContextCleared()
    {
        LastEnemyInitiativeEffectId = 0;
        LastSneakDebuffPool = null;
        LastPlayerInitiativeBuffPool = null;
        OnCombatContextCleared?.Invoke();
    }

    #endregion
}
