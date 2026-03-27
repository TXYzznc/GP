using System;

/// <summary>
/// 消耗品
/// </summary>
[Serializable]
public class ConsumableItem : ItemBase
{
    #region 构造函数

    public ConsumableItem(int itemId, ItemData itemData)
        : base(itemId, itemData)
    {
        DebugEx.Log("ConsumableItem", $"创建消耗品: {Name}");
    }

    #endregion

    #region 重写方法

    protected override bool OnUse()
    {
        if (ItemData.UseEffectId <= 0)
        {
            DebugEx.Warning("ConsumableItem", $"消耗品没有配置使用效果: {Name}");
            return false;
        }

        DebugEx.Log("ConsumableItem", $"执行消耗品效果: {Name}, EffectId:{ItemData.UseEffectId}");

        // 通过效果执行器执行效果
        var effectExecutor = ItemEffectExecutor.Instance;
        if (effectExecutor != null)
        {
            bool success = effectExecutor.ExecuteEffect(ItemData.UseEffectId);
            if (success)
            {
                DebugEx.Success("ConsumableItem", $"消耗品使用成功: {Name}");
            }
            else
            {
                DebugEx.Error("ConsumableItem", $"消耗品使用失败: {Name}");
            }
            return success;
        }

        DebugEx.Error("ConsumableItem", "ItemEffectExecutor 未初始化");
        return false;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();
        string stackInfo = CanStack ? $"\n可堆叠 (最大:{MaxStackCount})" : "\n不可堆叠";
        return baseInfo + stackInfo;
    }

    #endregion
}
