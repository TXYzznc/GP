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
    /// 注：以下卡牌已迁移到通用框架，不需要注册：
    /// - 1001 (神圣庇护)、1002 (烈焰风暴)、1004 (战争号角)
    /// - 1005 (暗影突袭)、1007 (冰霜新星)、1008 (狂暴)、1009 (群体治疗)
    /// - 1010 (雷霆一击)、1011 (混乱诅咒)
    ///
    /// 保留的脚本（待实现或有复杂逻辑）：
    /// - 1003 (时间回溯) - 占位实现，后续会有真正效果
    /// - 1006 (生命汲取) - 联动逻辑复杂
    /// - 1012 (不屈意志) - 状态依赖
    /// </summary>
    private void InitializeEffectMap()
    {
        m_EffectTypeMap[1003] = typeof(TimeRewindCardEffect);     // 时间回溯（待实现真正效果）
        m_EffectTypeMap[1006] = typeof(LifeDrainCardEffect);      // 生命汲取（联动逻辑复杂）
        m_EffectTypeMap[1012] = typeof(ResurrectionCardEffect);   // 不屈意志（状态依赖）

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
            ICardEffect effectInstance = null;

            // 使用新框架的卡牌效果
            switch (cardData.CardId)
            {
                case 1001: // 神圣庇护
                    effectInstance = CreateGenericEffect(cardData,
                        new AllAlliesSelector(), new InstantBuffApplier());
                    break;
                case 1002: // 烈焰风暴
                    effectInstance = CreateGenericEffect(cardData,
                        new AllEnemiesSelector(), new DamageApplier(), new BuffApplier());
                    break;
                // case 1003 已移到 default 分支，使用旧脚本（待实现真正的效果）
                case 1004: // 战争号角
                    effectInstance = CreateGenericEffect(cardData,
                        new AllAlliesSelector(), new InstantBuffApplier());
                    break;
                case 1005: // 暗影突袭（需要获取施法者）
                    {
                        var casterChess = GetCasterChess();
                        effectInstance = CreateGenericEffect(cardData,
                            new ClosestEnemySelector(),
                            new DamageWithCoefficientApplier(casterChess),
                            new BuffApplier());
                    }
                    break;
                case 1007: // 冰霜新星
                    effectInstance = CreateGenericEffect(cardData,
                        new EnemiesInRadiusSelector(), new BuffApplier());
                    break;
                case 1008: // 狂暴
                    effectInstance = CreateGenericEffect(cardData,
                        new ClosestAllySelector(), new InstantBuffApplier());
                    break;
                case 1009: // 群体治疗
                    effectInstance = CreateGenericEffect(cardData,
                        new AllAlliesSelector(), new HealApplier());
                    break;
                case 1010: // 雷霆一击
                    effectInstance = CreateGenericEffect(cardData,
                        new ClosestEnemySelector(), new DamageApplier());
                    break;
                case 1011: // 混乱诅咒
                    effectInstance = CreateGenericEffect(cardData,
                        new AllEnemiesSelector(), new BuffApplier());
                    break;
                default:
                    // 兼容旧脚本（LifeDrain、Resurrection 等特殊效果）
                    if (!m_EffectTypeMap.TryGetValue(cardData.CardId, out var effectType))
                    {
                        DebugEx.ErrorModule("CardEffectExecutor", $"未找到卡牌效果类型: ID={cardData.CardId}");
                        return;
                    }

                    effectInstance = Activator.CreateInstance(effectType) as ICardEffect;
                    if (effectInstance == null)
                    {
                        DebugEx.ErrorModule("CardEffectExecutor", $"无法创建效果实例: {effectType.Name}");
                        return;
                    }

                    effectInstance.Init(cardData);
                    break;
            }

            if (effectInstance == null)
            {
                DebugEx.ErrorModule("CardEffectExecutor", $"无法创建效果实例: ID={cardData.CardId}");
                return;
            }

            effectInstance.Execute(targetPosition);
            DebugEx.LogModule("CardEffectExecutor", $"执行卡牌效果: {cardData.CardId}");
        }
        catch (Exception ex)
        {
            DebugEx.ErrorModule("CardEffectExecutor", $"执行卡牌效果异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 创建通用效果实例
    /// </summary>
    private ICardEffect CreateGenericEffect(CardData cardData, ICardTargetSelector selector, params ICardEffectApplier[] appliers)
    {
        var effect = new GenericCardEffect();
        effect.Init(cardData, selector, appliers);
        return effect;
    }

    /// <summary>
    /// 获取施法者棋子（召唤师）
    /// 召唤师的 ChessEntity 组件挂载在玩家角色 GameObject 上
    /// </summary>
    private ChessEntity GetCasterChess()
    {
        var playerCharacterManager = PlayerCharacterManager.Instance;
        if (playerCharacterManager == null)
            return null;

        var playerCharacter = playerCharacterManager.CurrentPlayerCharacter;
        if (playerCharacter == null)
            return null;

        return playerCharacter.GetComponent<ChessEntity>();
    }

    #endregion
}
