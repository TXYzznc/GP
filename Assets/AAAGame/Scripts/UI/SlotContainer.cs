using UnityEngine;

/// <summary>
/// 格子容器接口
/// 定义一个容器应该如何操作其内部的格子
/// </summary>
public interface ISlotContainer
{
    /// <summary>容器类型</summary>
    SlotContainerType ContainerType { get; }

    /// <summary>获取指定索引的格子</summary>
    InventorySlot GetSlot(int slotIndex);

    /// <summary>
    /// 尝试从该容器移动物品到目标容器
    /// </summary>
    bool TryMoveToContainer(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex);

    /// <summary>
    /// 容器内格子的交互规则
    /// 定义该容器内的格子可以与哪些容器的格子交互
    /// </summary>
    bool CanInteractWith(SlotContainerType otherContainerType);
}

/// <summary>
/// 格子容器基类
/// 提供通用的容器功能和规则管理
/// </summary>
public abstract class SlotContainerBase : MonoBehaviour, ISlotContainer
{
    public abstract SlotContainerType ContainerType { get; }
    public abstract InventorySlot GetSlot(int slotIndex);
    public abstract bool CanInteractWith(SlotContainerType otherContainerType);

    public bool TryMoveToContainer(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex)
    {
        if (targetContainer == null)
        {
            DebugEx.Error("SlotContainer", $"[{ContainerType}] 目标容器为 null");
            return false;
        }

        if (!CanInteractWith(targetContainer.ContainerType))
        {
            DebugEx.Warning("SlotContainer",
                $"[{ContainerType}] 不允许与 [{targetContainer.ContainerType}] 交互（单向检查失败）");
            return false;
        }

        if (!targetContainer.CanInteractWith(this.ContainerType))
        {
            DebugEx.Warning("SlotContainer",
                $"[{targetContainer.ContainerType}] 不允许与 [{ContainerType}] 交互（双向检查失败）");
            return false;
        }

        var fromSlot = GetSlot(fromSlotIndex);
        if (fromSlot == null || fromSlot.IsEmpty)
        {
            DebugEx.Warning("SlotContainer", $"[{ContainerType}] 源格子 {fromSlotIndex} 为空或不存在");
            return false;
        }

        var targetSlot = targetContainer.GetSlot(targetSlotIndex);
        if (targetSlot == null)
        {
            DebugEx.Warning("SlotContainer",
                $"[{targetContainer.ContainerType}] 目标格子 {targetSlotIndex} 不存在");
            return false;
        }

        return ExecuteMove(fromSlotIndex, targetContainer, targetSlotIndex);
    }

    protected abstract bool ExecuteMove(int fromSlotIndex, ISlotContainer targetContainer, int targetSlotIndex);

    protected bool IsTargetContainerAllowingMove(ISlotContainer targetContainer)
    {
        return targetContainer is SlotContainerBase baseContainer &&
               baseContainer.CanInteractWith(this.ContainerType);
    }
}
