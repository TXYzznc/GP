using GameFramework.Event;
using GameFramework;

/// <summary>
/// 玩家升级事件
/// </summary>
public sealed class PlayerLevelUpEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(PlayerLevelUpEventArgs).GetHashCode();
    public override int Id => EventId;

    public int OldLevel { get; private set; }
    public int NewLevel { get; private set; }

    public static PlayerLevelUpEventArgs Create(int oldLevel, int newLevel)
    {
        var e = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        e.OldLevel = oldLevel;
        e.NewLevel = newLevel;
        return e;
    }

    public override void Clear()
    {
        OldLevel = 0;
        NewLevel = 0;
    }
}
