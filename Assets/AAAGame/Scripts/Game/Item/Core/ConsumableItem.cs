using System;

/// <summary>
/// 消耗品
/// </summary>
[Serializable]
public class ConsumableItem : ItemBase
{
    #region 字段

    private ConsumableData m_ConsumableData;

    #endregion

    #region 属性

    public override bool CanUse => m_ConsumableData?.CanUse ?? false;

    #endregion

    #region 构造函数

    public ConsumableItem(int itemId, ItemData itemData, ConsumableData consumableData)
        : base(itemId, itemData)
    {
        m_ConsumableData = consumableData;
        DebugEx.Log(nameof(ConsumableItem), $"创建消耗品: {Name}");
    }

    #endregion

    #region 重写方法

    protected override bool OnUse()
    {
        int useEffectId = m_ConsumableData?.UseEffectId ?? 0;
        if (useEffectId <= 0)
        {
            DebugEx.Warning(nameof(ConsumableItem), $"消耗品没有配置使用效果: {Name}");
            return false;
        }

        DebugEx.Log(nameof(ConsumableItem), $"执行消耗品效果: {Name}, EffectId:{useEffectId}");

        // 通过 GameEffectService 执行效果
        var context = GameEffectContext.CreateSingleTarget(EffectSource.Item, null, null);
        bool success = GameEffectService.Instance.Execute(useEffectId, context);

        if (success)
            DebugEx.Success(nameof(ConsumableItem), $"消耗品使用成功: {Name}");
        else
            DebugEx.Error(nameof(ConsumableItem), $"消耗品使用失败: {Name}");
        return success;
    }

    public override string GetDetailInfo()
    {
        string baseInfo = base.GetDetailInfo();
        string stackInfo = CanStack ? $"\n可堆叠 (最大:{MaxStackCount})" : "\n不可堆叠";
        return baseInfo + stackInfo;
    }

    #endregion
}
