using GameFramework.Event;
using GameFramework;

/// <summary>
/// 战斗结束事件
/// </summary>
public sealed class CombatEndEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(CombatEndEventArgs).GetHashCode();

    public override int Id => EventId;

    /// <summary>
    /// 是否胜利
    /// </summary>
    public bool IsVictory { get; private set; }

    /// <summary>
    /// 创建战斗结束事件
    /// </summary>
    public static CombatEndEventArgs Create(bool isVictory)//失败或胜利时调用
    {
        var e = ReferencePool.Acquire<CombatEndEventArgs>();
        e.IsVictory = isVictory;
        return e;
    }

    public override void Clear()
    {
        IsVictory = false;
    }
}
