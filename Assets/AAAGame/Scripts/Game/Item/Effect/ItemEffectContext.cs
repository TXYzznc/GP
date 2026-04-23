using UnityEngine;

/// <summary>
/// 物品效果执行上下文
/// </summary>
public class ItemEffectContext
{
    public SpecialEffectData EffectData { get; }
    public int ItemId { get; }
    public GameObject User { get; }
    public GameObject Target { get; }

    public ItemEffectContext(SpecialEffectData effectData, int itemId, GameObject user = null, GameObject target = null)
    {
        EffectData = effectData;
        ItemId = itemId;
        User = user;
        Target = target;
    }

    /// <summary>
    /// 从效果参数中获取指定类型的值
    /// </summary>
    public T GetParam<T>(string key, T defaultValue = default)
    {
        if (EffectData == null)
            return defaultValue;

        return EffectData.GetParamValue(key, defaultValue);
    }

    /// <summary>
    /// 获取玩家账号数据
    /// </summary>
    public PlayerAccountData GetPlayerData()
    {
        return PlayerAccountDataManager.Instance?.CurrentAccountData;
    }
}
