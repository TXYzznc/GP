using System.Collections.Generic;

/// <summary>
/// 宝箱界面的数据载体，打开 TreasureBoxUI 时作为 userData 传入
/// </summary>
public class TreasureBoxUIData
{
    /// <summary>宝箱中的物品列表</summary>
    public List<ItemStack> Items { get; }

    /// <summary>宝箱标题（可选）</summary>
    public string Title { get; }

    public TreasureBoxUIData(List<ItemStack> items, string title = "宝箱")
    {
        Items = items ?? new List<ItemStack>();
        Title = title;
    }
}
