using System;

/// <summary>
/// 任务道具
/// </summary>
[Serializable]
public class QuestItem : ItemBase
{
    #region 构造函数

    public QuestItem(int itemId, ItemData itemData)
        : base(itemId, itemData)
    {
        DebugEx.Log("QuestItem", $"创建任务道具: {Name}");
    }

    #endregion

    #region 重写方法

    public override bool CanUse => false; // 任务道具不可使用

    public override bool CanStack => false; // 任务道具不可堆叠

    protected override bool OnUse()
    {
        DebugEx.Warning("QuestItem", $"任务道具不可使用: {Name}");
        return false;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();
        return baseInfo + "\n[任务道具]";
    }

    #endregion
}
