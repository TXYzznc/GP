using GameFramework.Event;

/// <summary>
/// 进入局内状态事件
/// </summary>
public sealed class InGameEnterEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(InGameEnterEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
