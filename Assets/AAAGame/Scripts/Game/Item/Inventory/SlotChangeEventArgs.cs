using System;

/// <summary>
/// 容器类型枚举
/// </summary>
public enum SlotContainerType
{
    Inventory,   // 背包
    Warehouse,   // 仓库
    Equip,       // 装备栏
    FastBar,     // 快捷栏
    Chess,       // 棋子
    TreasureBox  // 宝箱
}

/// <summary>
/// 格子变化类型
/// </summary>
public enum SlotChangeType
{
    Add,     // 添加物品
    Remove,  // 移除物品
    Update,  // 数量更新（堆叠）
    Move,    // 移动（内部交换）
    Clear    // 清空格子
}

/// <summary>
/// 格子变化事件参数（统一的事件参数设计）
/// </summary>
public class SlotChangeEventArgs
{
    /// <summary>容器类型</summary>
    public SlotContainerType ContainerType { get; set; }

    /// <summary>格子索引</summary>
    public int SlotIndex { get; set; }

    /// <summary>物品ID（-1表示格子为空）</summary>
    public int ItemId { get; set; }

    /// <summary>变化前的数量</summary>
    public int OldCount { get; set; }

    /// <summary>变化后的数量</summary>
    public int NewCount { get; set; }

    /// <summary>变化类型</summary>
    public SlotChangeType ChangeType { get; set; }
}
