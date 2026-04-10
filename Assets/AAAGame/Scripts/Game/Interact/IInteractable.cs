using UnityEngine;

/// <summary>
/// 可交互对象接口
/// 所有需要与玩家交互的对象实现此接口
/// </summary>
public interface IInteractable
{
    /// <summary>交互提示文本（如"打开宝箱"、"对话"）</summary>
    string InteractionTip { get; }

    /// <summary>优先级（数值越大越优先，默认0）</summary>
    int Priority { get; }

    /// <summary>交互点位置（用于距离计算和UI定位）</summary>
    Transform InteractionPoint { get; }

    /// <summary>交互动画索引（-1表示不播放交互动画）</summary>
    int InteractAnimIndex { get; }

    /// <summary>是否可以交互（条件判断，如宝箱是否已开启）</summary>
    bool CanInteract(GameObject player);

    /// <summary>执行交互逻辑</summary>
    void OnInteract(GameObject player);
}
