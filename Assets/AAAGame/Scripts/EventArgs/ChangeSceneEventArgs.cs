using GameFramework;
using GameFramework.Event;

/// <summary>
/// 场景切换事件参数
/// </summary>
public class ChangeSceneEventArgs : GameEventArgs
{
    /// <summary>
    /// 事件ID
    /// </summary>
    public static readonly int EventId = typeof(ChangeSceneEventArgs).GetHashCode();

    /// <summary>
    /// 获取事件ID
    /// </summary>
    public override int Id => EventId;

    /// <summary>
    /// 目标场景名称
    /// </summary>
    public string SceneName { get; private set; }

    /// <summary>
    /// 创建场景切换事件参数
    /// </summary>
    public static ChangeSceneEventArgs Create(string sceneName)
    {
        var args = ReferencePool.Acquire<ChangeSceneEventArgs>();
        args.SceneName = sceneName;
        return args;
    }

    /// <summary>
    /// 清理事件参数
    /// </summary>
    public override void Clear()
    {
        SceneName = null;
    }
}
