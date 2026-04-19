/// <summary>
/// 虚拟物品（金币、灵石等资源）
/// 特点：可以显示，但不能使用、不能装备、不能拖拽进背包
/// </summary>
public class VirtualItem : ItemBase
{
    #region 构造函数

    public VirtualItem(int itemId, ItemData itemData)
        : base(itemId, itemData)
    {
        DebugEx.Log("VirtualItem", $"创建虚拟物品: {Name} (ID:{itemId})");
    }

    #endregion

    #region 属性重写

    /// <summary>
    /// 虚拟物品不能使用
    /// </summary>
    public override bool CanUse => false;

    /// <summary>
    /// 虚拟物品不能装备
    /// </summary>
    public override bool CanEquip => false;

    #endregion

    #region 方法实现

    /// <summary>
    /// 虚拟物品不能使用
    /// </summary>
    protected override bool OnUse()
    {
        DebugEx.Warning("VirtualItem", $"虚拟物品不能使用: {Name}");
        return false;
    }

    #endregion
}
