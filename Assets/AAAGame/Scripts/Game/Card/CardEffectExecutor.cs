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

            // 尝试使用特殊脚本（有复杂逻辑的卡牌）
            if (m_EffectTypeMap.TryGetValue(cardData.CardId, out var effectType))
            {
                effectInstance = Activator.CreateInstance(effectType) as ICardEffect;
                if (effectInstance == null)
                {
                    DebugEx.ErrorModule("CardEffectExecutor", $"无法创建效果实例: {effectType.Name}");
                    return;
                }

                effectInstance.Init(cardData);
            }
            else
            {
                // 使用通用框架
                var targetSelector = GetTargetSelector(cardData.CTargetType);
                if (targetSelector == null)
                {
                    DebugEx.ErrorModule("CardEffectExecutor", $"未找到目标类型选择器: {cardData.CTargetType}");
                    return;
                }

                var appliers = GetEffectAppliers(cardData);
                effectInstance = CreateGenericEffect(cardData, targetSelector, appliers);
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
    /// 根据卡牌目标类型获取对应的选择器
    /// </summary>
    private ICardTargetSelector GetTargetSelector(CardTargetType targetType)
    {
        return targetType switch
        {
            CardTargetType.Self => new SelfSelector(),
            CardTargetType.AllAllyExcludeSummoner => new AllAllyExcludeSummonerSelector(),
            CardTargetType.AllAlly => new AllAlliesSelector(),
            CardTargetType.AllEnemy => new AllEnemiesSelector(),
            CardTargetType.SingleAlly => new ClosestAllySelector(),
            CardTargetType.AreaAlly => new AlliesInRadiusSelector(),
            CardTargetType.AreaEnemy => new EnemiesInRadiusSelector(),
            CardTargetType.SingleEnemy => new ClosestEnemySelector(),
            _ => null
        };
    }

    /// <summary>
    /// 根据卡牌配置智能选择效果应用器
    /// </summary>
    private ICardEffectApplier[] GetEffectAppliers(CardData cardData)
    {
        var appliers = new List<ICardEffectApplier>();
        var casterChess = GetCasterChess();

        // 判断是否需要伤害应用器
        if (cardData.TableRow.DamageType > 0)
        {
            if (cardData.TableRow.DamageCoeff > 0)
            {
                appliers.Add(new DamageWithCoefficientApplier(casterChess));
            }
            else if (cardData.TableRow.BaseDamage > 0)
            {
                appliers.Add(new DamageApplier());
            }
        }

        // 判断是否需要治疗应用器
        if (cardData.GetParam("healAmount", 0f) > 0)
        {
            appliers.Add(new HealApplier());
        }

        // 判断是否需要命中 Buff 应用器
        if (cardData.HitBuffIds.Length > 0)
        {
            appliers.Add(new BuffApplier(casterChess));
        }

        // 判断是否需要立即 Buff 应用器
        if (cardData.InstantBuffIds.Length > 0)
        {
            appliers.Add(new InstantBuffApplier(casterChess));
        }

        return appliers.ToArray();
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
