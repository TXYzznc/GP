using GameFramework.Event;

/// <summary>
/// 进入探索状态事件
/// </summary>
public sealed class ExplorationEnterEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ExplorationEnterEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
