using GameFramework.Event;

/// <summary>
/// 离开局内状态事件
/// </summary>
public sealed class InGameLeaveEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(InGameLeaveEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
