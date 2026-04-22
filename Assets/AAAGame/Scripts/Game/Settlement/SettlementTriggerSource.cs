/// <summary>
/// 结算触发源枚举
/// </summary>
public enum SettlementTriggerSource
{
    /// <summary>通过传送门主动传送回基地</summary>
    Teleport = 0,

    /// <summary>玩家完全死亡</summary>
    Death = 1,

    /// <summary>其他触发源</summary>
    Other = 2,
}
