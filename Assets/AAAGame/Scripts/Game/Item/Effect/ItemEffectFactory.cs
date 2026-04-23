using System;
using System.Collections.Generic;

/// <summary>
/// 物品效果工厂
/// </summary>
public static class ItemEffectFactory
{
    private static readonly Dictionary<string, Func<IItemEffect>> s_Creators = new();

    /// <summary>
    /// 注册所有效果（在游戏启动时调用）
    /// </summary>
    public static void RegisterAll()
    {
        s_Creators.Clear();

        // 基础效果
        Register("AddGold", () => new AddGoldEffect());
        Register("RestoreHP", () => new RestoreHPEffect());
        Register("RestoreMP", () => new RestoreMPEffect());
        Register("AddExp", () => new AddExpEffect());
        Register("RecoverChessHP", () => new RecoverChessHPEffect());
        Register("ReviveChess", () => new ReviveChessEffect());
        Register("RandomEquipment", () => new RandomEquipmentEffect());

        // 卡牌解锁
        Register("UnlockCard", () => new UnlockCardEffect());

        DebugEx.LogModule("ItemEffectFactory", $"注册了 {s_Creators.Count} 个物品效果");
    }

    public static void Register(string effectType, Func<IItemEffect> creator)
    {
        if (creator == null)
        {
            DebugEx.ErrorModule("ItemEffectFactory", $"Register creator is null, type={effectType}");
            return;
        }

        if (s_Creators.ContainsKey(effectType))
        {
            DebugEx.WarningModule("ItemEffectFactory", $"效果类型 {effectType} 已经注册过了，将被覆盖");
        }

        s_Creators[effectType] = creator;
    }

    /// <summary>
    /// 创建效果实例
    /// </summary>
    public static IItemEffect Create(string effectType)
    {
        if (s_Creators.TryGetValue(effectType, out var creator))
        {
            return creator();
        }

        DebugEx.WarningModule("ItemEffectFactory", $"未找到效果类型: {effectType}");
        return null;
    }
}
