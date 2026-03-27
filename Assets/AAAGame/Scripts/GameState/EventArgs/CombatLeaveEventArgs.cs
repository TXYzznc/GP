using GameFramework.Event;

/// <summary>
/// 离开战斗状态事件
/// </summary>
public sealed class CombatLeaveEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(CombatLeaveEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
