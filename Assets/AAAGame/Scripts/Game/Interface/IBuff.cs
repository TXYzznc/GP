using UnityEngine;

/// <summary>
/// Buff 接口，定义所有 Buff 的基础行为规范
/// </summary>
public interface IBuff
{
    /// <summary>
    /// Buff ID (对应 DataTable ID)
    /// </summary>
    int BuffId { get; }

    /// <summary>
    /// 当前层数
    /// </summary>
    int StackCount { get; }

    /// <summary>
    /// 是否已结束（需要在下一帧移除）
    /// </summary>
    bool IsFinished { get; }

    /// <summary>
    /// 初始化 Buff
    /// </summary>
    /// <param name="ctx">上下文</param>
    /// <param name="config">配置数据</param>
    void Init(BuffContext ctx, BuffTable config);

    /// <summary>
    /// Buff 生效时调用（首次添加）
    /// </summary>
    void OnEnter();

    /// <summary>
    /// 每帧更新
    /// </summary>
    /// <param name="dt">Delta Time</param>
    void OnUpdate(float dt);

    /// <summary>
    /// Buff 移除时调用
    /// </summary>
    void OnExit();

    /// <summary>
    /// 尝试添加相同 Buff 时调用（处理叠层逻辑）
    /// </summary>
    /// <returns>是否刷新了持续时间</returns>
    bool OnStack();
    
    /// <summary>
    /// 减少层数（用于融化消耗灼烧层数）
    /// </summary>
    /// <param name="count">要减少的层数</param>
    void ReduceStacks(int count);
}
