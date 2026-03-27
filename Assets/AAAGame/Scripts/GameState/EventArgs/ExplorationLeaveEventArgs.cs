using GameFramework.Event;

/// <summary>
/// 离开探索状态事件
/// </summary>
public sealed class ExplorationLeaveEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ExplorationLeaveEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
