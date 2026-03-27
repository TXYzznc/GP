using GameFramework.Event;

/// <summary>
/// 战斗准备完成事件（玩家点击准备按钮或倒计时结束时触发）
/// </summary>
public sealed class CombatPreparationReadyEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(CombatPreparationReadyEventArgs).GetHashCode();

    public override int Id => EventId;

    public override void Clear()
    {
        // 无需清理
    }
}
