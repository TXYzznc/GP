using GameFramework.Event;

/// <summary>
/// 进入局外状态事件
/// </summary>
public sealed class OutOfGameEnterEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(OutOfGameEnterEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
