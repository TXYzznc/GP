using GameFramework.Event;

/// <summary>
/// 进入战斗状态事件
/// </summary>
public sealed class CombatEnterEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(CombatEnterEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
