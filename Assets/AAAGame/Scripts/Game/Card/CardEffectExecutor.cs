using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌效果执行器（参考 Buff 的动态创建方式）
/// </summary>
public class CardEffectExecutor : MonoBehaviour
{
    #region 字段

    private Dictionary<int, Type> m_EffectTypeMap = new Dictionary<int, Type>();

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        InitializeEffectMap();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化效果类型映射
    /// </summary>
    private void InitializeEffectMap()
    {
        // 注册所有卡牌效果类型
        m_EffectTypeMap[1001] = typeof(HolyShieldCardEffect);
        m_EffectTypeMap[1002] = typeof(FlameStormCardEffect);
        m_EffectTypeMap[1003] = typeof(TimeRewindCardEffect);
        m_EffectTypeMap[1004] = typeof(WarCryCardEffect);
        m_EffectTypeMap[1005] = typeof(ShadowAssaultCardEffect);
        m_EffectTypeMap[1006] = typeof(LifeDrainCardEffect);
        m_EffectTypeMap[1007] = typeof(FrostNovaCardEffect);
        m_EffectTypeMap[1008] = typeof(BerserkCardEffect);
        m_EffectTypeMap[1009] = typeof(GroupHealCardEffect);
        m_EffectTypeMap[1010] = typeof(ThunderStrikeCardEffect);
        m_EffectTypeMap[1011] = typeof(ChaosCurseCardEffect);
        m_EffectTypeMap[1012] = typeof(ResurrectionCardEffect);

        DebugEx.LogModule("CardEffectExecutor", "卡牌效果类型映射已初始化");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 执行卡牌效果
    /// </summary>
    public void ExecuteCardEffect(CardData cardData, Vector3 targetPosition)
    {
        if (cardData == null)
        {
            DebugEx.ErrorModule("CardEffectExecutor", "卡牌数据为空");
            return;
        }

        try
        {
            // 根据 CardId 获取效果类型
            if (!m_EffectTypeMap.TryGetValue(cardData.CardId, out var effectType))
            {
                DebugEx.ErrorModule("CardEffectExecutor", $"未找到卡牌效果类型: ID={cardData.CardId}");
                return;
            }

            // 创建效果实例（参考 BuffFactory 的方式）
            var effectInstance = Activator.CreateInstance(effectType) as ICardEffect;
            if (effectInstance == null)
            {
                DebugEx.ErrorModule("CardEffectExecutor", $"无法创建效果实例: {effectType.Name}");
                return;
            }

            // 初始化并执行效果
            effectInstance.Init(cardData);
            effectInstance.Execute(targetPosition);

            DebugEx.LogModule("CardEffectExecutor", $"执行卡牌效果: {cardData.CardId}");
        }
        catch (Exception ex)
        {
            DebugEx.ErrorModule("CardEffectExecutor", $"执行卡牌效果异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion
}
