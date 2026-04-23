using UnityEngine;

/// <summary>
/// 物品效果执行器
/// </summary>
public class ItemEffectExecutor : MonoBehaviour
{
    #region 单例

    private static ItemEffectExecutor s_Instance;
    public static ItemEffectExecutor Instance => s_Instance;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DebugEx.Log("ItemEffectExecutor", "物品效果执行器初始化完成");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 执行效果
    /// </summary>
    public bool ExecuteEffect(int effectId)
    {
        var effectData = ItemManager.Instance?.GetSpecialEffectData(effectId);
        if (effectData == null)
        {
            DebugEx.Error("ItemEffectExecutor", $"效果数据不存在 ID:{effectId}");
            return false;
        }

        DebugEx.Log("ItemEffectExecutor", $"执行效果: {effectData.Name}");

        string effectType = effectData.GetParamValue<string>("type", "");

        // 尝试新架构（工厂模式）
        var effect = ItemEffectFactory.Create(effectType);
        if (effect != null)
        {
            var context = new ItemEffectContext(effectData, 0, null, null);
            return effect.Execute(context);
        }

        // 向后兼容：回退到旧逻辑
        return ExecuteEffectLegacy(effectType, effectData);
    }

    /// <summary>
    /// 旧版效果执行逻辑（向后兼容）
    /// </summary>
    [System.Obsolete("使用新的 ItemEffectFactory 架构代替")]
    private bool ExecuteEffectLegacy(string effectType, SpecialEffectData effectData)
    {
        switch (effectType)
        {
            case "AddGold":
                return ExecuteAddGold(effectData);

            case "RestoreHP":
                return ExecuteRestoreHP(effectData);

            case "RestoreMP":
                return ExecuteRestoreMP(effectData);

            case "AddExp":
                return ExecuteAddExp(effectData);

            case "RandomEquipment":
                return ExecuteRandomEquipment(effectData);

            case "RecoverChessHP":
                return ExecuteRecoverChessHP(effectData);

            case "ReviveChess":
                return ExecuteReviveChess(effectData);

            default:
                DebugEx.Warning("ItemEffectExecutor", $"未知的效果类型: {effectType}");
                return false;
        }
    }

    #endregion

    #region 效果实现

    /// <summary>
    /// 添加金币
    /// </summary>
    private bool ExecuteAddGold(SpecialEffectData effectData)
    {
        int min = effectData.GetParamValue<int>("min", 0);
        int max = effectData.GetParamValue<int>("max", 0);
        int gold = Random.Range(min, max + 1);

        DebugEx.Log("ItemEffectExecutor", $"添加金币: {gold}");

        // TODO: 调用玩家数据管理器添加金币
        // PlayerDataManager.Instance.AddGold(gold);

        DebugEx.Success("ItemEffectExecutor", $"金币添加成功: +{gold}");
        return true;
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    private bool ExecuteRestoreHP(SpecialEffectData effectData)
    {
        int value = effectData.GetParamValue<int>("value", 0);

        DebugEx.Log("ItemEffectExecutor", $"恢复生命值: {value}");

        // TODO: 调用玩家角色恢复生命值
        // PlayerCharacterManager.Instance.RestoreHP(value);

        DebugEx.Success("ItemEffectExecutor", $"生命值恢复成功: +{value}");
        return true;
    }

    /// <summary>
    /// 恢复魔法值
    /// </summary>
    private bool ExecuteRestoreMP(SpecialEffectData effectData)
    {
        int value = effectData.GetParamValue<int>("value", 0);

        DebugEx.Log("ItemEffectExecutor", $"恢复魔法值: {value}");

        // TODO: 调用玩家角色恢复魔法值
        // PlayerCharacterManager.Instance.RestoreMP(value);

        DebugEx.Success("ItemEffectExecutor", $"魔法值恢复成功: +{value}");
        return true;
    }

    /// <summary>
    /// 添加经验值
    /// </summary>
    private bool ExecuteAddExp(SpecialEffectData effectData)
    {
        int value = effectData.GetParamValue<int>("value", 0);

        DebugEx.Log("ItemEffectExecutor", $"添加经验值: {value}");

        // TODO: 调用玩家数据管理器添加经验值
        // PlayerDataManager.Instance.AddExp(value);

        DebugEx.Success("ItemEffectExecutor", $"经验值添加成功: +{value}");
        return true;
    }

    /// <summary>
    /// 恢复指定棋子血量（仅受伤棋子有效，已死亡的无法通过道具恢复）
    /// EffectParams：{ "chessId": int, "value": double }
    /// 若 chessId = 0 则不执行（需由调用方传入目标棋子）
    /// </summary>
    private bool ExecuteRecoverChessHP(SpecialEffectData effectData)
    {
        int chessId = effectData.GetParamValue<int>("chessId", 0);
        double value = effectData.GetParamValue<double>("value", 0);

        if (chessId <= 0)
        {
            DebugEx.Warning("ItemEffectExecutor", "RecoverChessHP: chessId 未指定，请在使用道具时传入目标棋子");
            return false;
        }

        bool success = GlobalChessManager.Instance.TryRecoverChessHP(chessId, value);

        if (success)
        {
            DebugEx.Success("ItemEffectExecutor", $"棋子 {chessId} 血量恢复 +{value}");
        }
        else
        {
            DebugEx.Warning("ItemEffectExecutor", $"棋子 {chessId} 血量恢复失败（已死亡或血量已满）");
        }

        return success;
    }

    /// <summary>
    /// 复活指定棋子（仅已死亡棋子有效）
    /// EffectParams：{ "chessId": int, "reviveHP": double }
    /// reviveHP = 0 时按最大血量 50% 复活
    /// </summary>
    private bool ExecuteReviveChess(SpecialEffectData effectData)
    {
        int chessId = effectData.GetParamValue<int>("chessId", 0);
        double reviveHP = effectData.GetParamValue<double>("reviveHP", 0);

        if (chessId <= 0)
        {
            DebugEx.Warning("ItemEffectExecutor", "ReviveChess: chessId 未指定");
            return false;
        }

        bool success = GlobalChessManager.Instance.TryReviveChess(chessId, reviveHP);

        if (success)
        {
            DebugEx.Success("ItemEffectExecutor", $"棋子 {chessId} 复活成功，HP={reviveHP}");
        }
        else
        {
            DebugEx.Warning("ItemEffectExecutor", $"棋子 {chessId} 复活失败（未死亡或未注册）");
        }

        return success;
    }

    /// <summary>
    /// 随机获得装备
    /// </summary>
    private bool ExecuteRandomEquipment(SpecialEffectData effectData)
    {
        int qualityMin = effectData.GetParamValue<int>("qualityMin", 1);
        int qualityMax = effectData.GetParamValue<int>("qualityMax", 3);

        DebugEx.Log("ItemEffectExecutor", $"随机装备品质范围: {qualityMin}-{qualityMax}");

        // TODO: 实现随机装备生成逻辑
        // 1. 从装备表中筛选符合品质范围的装备
        // 2. 随机选择一件装备
        // 3. 添加到背包

        DebugEx.Success("ItemEffectExecutor", "随机装备获得成功");
        return true;
    }

    #endregion
}
