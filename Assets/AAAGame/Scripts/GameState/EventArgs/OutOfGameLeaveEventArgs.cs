using GameFramework.Event;

/// <summary>
/// 离开局外状态事件
/// </summary>
public sealed class OutOfGameLeaveEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(OutOfGameLeaveEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
