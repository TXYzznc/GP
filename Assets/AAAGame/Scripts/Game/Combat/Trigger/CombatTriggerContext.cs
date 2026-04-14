using System.Collections.Generic;

/// <summary>
/// 战斗触发上下文
/// 保存战斗触发时的所有相关信息
/// 用于战斗准备阶段应用先手效果
/// </summary>
public class CombatTriggerContext
{
    /// <summary>战斗触发类型</summary>
    public CombatTriggerType TriggerType { get; set; }

    /// <summary>触发战斗的敌人</summary>
    public EnemyEntity TriggerEnemy { get; set; }

    /// <summary>偷袭时可选的Debuff ID列表（三选一）</summary>
    public List<int> AvailableDebuffs { get; set; }

    /// <summary>遭遇战时可选的先手Buff ID列表（三选一）</summary>
    public List<int> AvailableBuffIds { get; set; }

    /// <summary>先手Buff ID（遭遇战/敌方先手）</summary>
    public int InitiativeBuffId { get; set; }

    /// <summary>玩家是否拥有先手</summary>
    public bool PlayerHasInitiative { get; set; }

    /// <summary>玩家选择的效果ID（偷袭Debuff或先手Buff，在准备阶段由玩家选择，战斗开始后应用）</summary>
    public int SelectedEffectId { get; set; }

    public CombatTriggerContext()
    {
        TriggerType = CombatTriggerType.Normal;
        TriggerEnemy = null;
        AvailableDebuffs = new List<int>();
        AvailableBuffIds = new List<int>();
        InitiativeBuffId = 0;
        PlayerHasInitiative = false;
        SelectedEffectId = 0;
    }
}
