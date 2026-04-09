/// <summary>
/// 策略卡目标类型（对应 CardTable.TargetType）
/// </summary>
public enum CardTargetType
{
    /// <summary>自身（释放者/召唤师）</summary>
    Self = 1,

    /// <summary>全体友方（不含召唤师）</summary>
    AllAllyExcludeSummoner = 2,

    /// <summary>全体友方（含召唤师）</summary>
    AllAlly = 3,

    /// <summary>敌方全体</summary>
    AllEnemy = 4,

    /// <summary>单体友方（就近判定友方单位）</summary>
    SingleAlly = 5,

    /// <summary>范围内友方（AreaRadius 范围内的友方单位）</summary>
    AreaAlly = 6,

    /// <summary>范围内敌方（AreaRadius 范围内的敌方单位）</summary>
    AreaEnemy = 7,

    /// <summary>单体敌方（就近判定敌方单位）</summary>
    SingleEnemy = 8,
}
