using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 战斗触发管理器
/// 统一处理所有战斗触发方式（偷袭、遭遇战、敌方先手）
/// 管理战斗触发上下文和相关Buff/Debuff的分配
/// </summary>
public class CombatTriggerManager : SingletonBase<CombatTriggerManager>
{
    #region 私有字段

    /// <summary>当前战斗触发上下文</summary>
    private CombatTriggerContext m_CurrentContext;

    /// <summary>可配置的偷袭Debuff阈值</summary>
    private const float SNEAK_ATTACK_ALERT_THRESHOLD = 0.3f;

    /// <summary>可配置的遭遇战警觉度阈值</summary>
    private const float ENCOUNTER_ALERT_THRESHOLD = 0.5f;

    /// <summary>偷袭检测距离</summary>
    private const float SNEAK_ATTACK_DISTANCE = 3f;

    /// <summary>遭遇战检测距离</summary>
    private const float ENCOUNTER_DISTANCE = 5f;

    /// <summary>身后判定角度（度）</summary>
    private const float BEHIND_ANGLE_THRESHOLD = 60f;

    /// <summary>玩家面向角度阈值（度）</summary>
    private const float PLAYER_FACING_ANGLE_THRESHOLD = 45f;

    #endregion

    #region 属性

    /// <summary>当前战斗触发上下文</summary>
    public CombatTriggerContext CurrentContext => m_CurrentContext;

    #endregion

    #region Unity生命周期

    private void Awake()
    {
        base.Awake();
        DebugEx.LogModule("CombatTriggerManager", "初始化完成");
    }

    private void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 触发战斗
    /// </summary>
    public void TriggerCombat(EnemyEntity enemy, CombatTriggerType triggerType)
    {
        if (enemy == null)
        {
            DebugEx.ErrorModule("CombatTriggerManager", "敌人实体为空");
            return;
        }

        // 玩家主动触发战斗时，立即结束战后隐身效果
        var playerGo = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerGo != null)
            playerGo.GetComponent<PostCombatStealth>()?.Deactivate();

        // 创建战斗上下文
        m_CurrentContext = new CombatTriggerContext
        {
            TriggerType = triggerType,
            TriggerEnemy = enemy,
            PlayerHasInitiative = (triggerType != CombatTriggerType.EnemyInitiated),
        };

        // 根据触发类型分配效果
        switch (triggerType)
        {
            case CombatTriggerType.SneakAttack:
                m_CurrentContext.AvailableDebuffs = GetSneakDebuffPool();
                CombatTriggerEvents.FireSneakAttackTriggered(m_CurrentContext.AvailableDebuffs);
                DebugEx.LogModule(
                    "CombatTriggerManager",
                    $"偷袭触发: {enemy.Config.Name}, 可选效果数={m_CurrentContext.AvailableDebuffs.Count}"
                );
                break;

            case CombatTriggerType.Encounter:
                m_CurrentContext.InitiativeBuffId = GetRandomInitiativeBuff();
                ApplyInitiativeEffectToPlayer(m_CurrentContext.InitiativeBuffId);
                DebugEx.LogModule(
                    "CombatTriggerManager",
                    $"遭遇战触发: {enemy.Config.Name}, 先手效果={m_CurrentContext.InitiativeBuffId}"
                );
                break;

            case CombatTriggerType.EnemyInitiated:
                m_CurrentContext.InitiativeBuffId = GetRandomInitiativeBuff();
                ApplyInitiativeEffectToEnemy(m_CurrentContext.InitiativeBuffId, enemy);
                CombatTriggerEvents.FireEnemyInitiativeTriggered(m_CurrentContext.InitiativeBuffId);
                DebugEx.LogModule(
                    "CombatTriggerManager",
                    $"敌方先手触发: {enemy.Config.Name}, 敌人先手效果={m_CurrentContext.InitiativeBuffId}"
                );
                break;

            default:
                DebugEx.LogModule("CombatTriggerManager", $"普通触发: {enemy.Config.Name}");
                break;
        }

        // 输出战斗方式总结
        string triggerModeName = GetTriggerModeName(triggerType);
        DebugEx.LogModule(
            "CombatTriggerManager",
            $"<color=#FFD700>========== 进入战斗 ==========</color>"
        );
        DebugEx.LogModule(
            "CombatTriggerManager",
            $"<color=#FFD700>敌人: {enemy.Config.Name}</color>"
        );
        DebugEx.LogModule(
            "CombatTriggerManager",
            $"<color=#FFD700>战斗方式: {triggerModeName}</color>"
        );
        DebugEx.LogModule(
            "CombatTriggerManager",
            $"<color=#FFD700>============================</color>"
        );

        // 调用EnemyEntityManager触发战斗
        EnemyEntityManager.Instance.TriggerCombat(enemy);
    }

    /// <summary>
    /// 清除当前上下文
    /// </summary>
    public void ClearContext()
    {
        m_CurrentContext = null;
        CombatTriggerEvents.FireCombatContextCleared();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取战斗方式的名称
    /// </summary>
    private string GetTriggerModeName(CombatTriggerType triggerType)
    {
        return triggerType switch
        {
            CombatTriggerType.SneakAttack => "我方偷袭",
            CombatTriggerType.Encounter => "我方先手（遭遇战）",
            CombatTriggerType.EnemyInitiated => "敌方先手",
            CombatTriggerType.Normal => "普通战斗",
            _ => "未知战斗类型",
        };
    }

    /// <summary>
    /// 获取偷袭效果池
    /// 从SpecialEffectTable中筛选EffectCategory=3（玩家偷袭）的效果
    /// </summary>
    private List<int> GetSneakDebuffPool()
    {
        List<int> effectIds = new List<int>();

        var specialEffectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (specialEffectTable == null)
        {
            DebugEx.WarningModule("CombatTriggerManager", "SpecialEffectTable未加载");
            return effectIds;
        }

        // 遍历所有特殊效果配置，筛选出玩家偷袭效果
        var allEffects = specialEffectTable.GetAllDataRows();
        foreach (var effect in allEffects)
        {
            // 筛选条件：EffectCategory=3（玩家偷袭）
            if (effect.EffectCategory == 3)
            {
                effectIds.Add(effect.Id);
            }
        }

        // 随机打乱顺序（Fisher-Yates洗牌）
        if (effectIds.Count > 0)
        {
            for (int i = effectIds.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = effectIds[i];
                effectIds[i] = effectIds[randomIndex];
                effectIds[randomIndex] = temp;
            }
        }

        DebugEx.LogModule(
            "CombatTriggerManager",
            $"获取偷袭效果池: {effectIds.Count}个 - [{string.Join(", ", effectIds)}]"
        );

        return effectIds;
    }

    /// <summary>
    /// 应用先手效果到玩家方（全体应用）
    /// 从SpecialEffectTable中获取效果配置，并应用其包含的所有Buff
    /// </summary>
    private void ApplyInitiativeEffectToPlayer(int effectId)
    {
        if (effectId <= 0)
        {
            return;
        }

        var specialEffectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (specialEffectTable == null)
        {
            DebugEx.WarningModule("CombatTriggerManager", "SpecialEffectTable未加载");
            return;
        }

        var effect = specialEffectTable.GetDataRow(effectId);
        if (effect == null)
        {
            DebugEx.WarningModule("CombatTriggerManager", $"未找到先手效果: {effectId}");
            return;
        }

        // 解析Buff ID列表并应用到玩家方（全体）
        ApplyBuffsFromEffect(effect, null, true); // isPlayerSide=true

        DebugEx.LogModule(
            "CombatTriggerManager",
            $"应用先手效果到玩家方: EffectId={effectId}, 名称={effect.Name}, BuffIds={string.Join(",", effect.BuffIds ?? new int[0])}, SelfBuffIds={string.Join(",", effect.SelfBuffIds ?? new int[0])}"
        );
    }

    /// <summary>
    /// 应用先手效果到敌人方（全体应用）
    /// 从SpecialEffectTable中获取效果配置，并应用其包含的所有Buff
    /// </summary>
    private void ApplyInitiativeEffectToEnemy(int effectId, EnemyEntity enemy)
    {
        if (effectId <= 0 || enemy == null)
        {
            return;
        }

        var specialEffectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (specialEffectTable == null)
        {
            DebugEx.WarningModule("CombatTriggerManager", "SpecialEffectTable未加载");
            return;
        }

        var effect = specialEffectTable.GetDataRow(effectId);
        if (effect == null)
        {
            DebugEx.WarningModule("CombatTriggerManager", $"未找到先手效果: {effectId}");
            return;
        }

        // 应用Buff到敌人方（全体）
        ApplyBuffsFromEffect(effect, enemy, false); // isPlayerSide=false

        // 注：敌方先手效果提示UI由 CombatPreparationState.ShowEnemyInitiativeBuffIfNeeded() 处理
        // 无需在此重复显示

        DebugEx.LogModule(
            "CombatTriggerManager",
            $"应用先手效果到敌人方: EffectId={effectId}, 名称={effect.Name}, BuffIds={string.Join(",", effect.BuffIds ?? new int[0])}, SelfBuffIds={string.Join(",", effect.SelfBuffIds ?? new int[0])}, 敌人={enemy.Config.Name}"
        );
    }

    /// <summary>
    /// 从特殊效果中应用所有包含的Buff
    /// BuffIds: 应用到目标方（全体）
    /// SelfBuffIds: 应用到自身方（全体）
    /// </summary>
    private void ApplyBuffsFromEffect(
        SpecialEffectTable effect,
        EnemyEntity targetEnemy,
        bool isPlayerSide
    )
    {
        if (effect == null)
        {
            return;
        }

        // 应用给自身的Buff（SelfBuffIds）
        if (effect.SelfBuffIds != null && effect.SelfBuffIds.Length > 0)
        {
            foreach (int buffId in effect.SelfBuffIds)
            {
                if (buffId > 0)
                {
                    if (isPlayerSide)
                    {
                        // TODO: 获取玩家实体，应用Buff到玩家方（全体）
                        // GameObject playerEntity = GetPlayerEntity();
                        // BuffApplyHelper.ApplyBuff(buffId, playerEntity, true, null);
                        DebugEx.LogModule(
                            "CombatTriggerManager",
                            $"  应用Buff到玩家方(全体-SelfBuff): BuffId={buffId}"
                        );
                    }
                    else if (targetEnemy != null)
                    {
                        // 应用Buff到敌人方（全体）
                        BuffApplyHelper.ApplyBuff(buffId, targetEnemy.gameObject, true, null);
                        DebugEx.LogModule(
                            "CombatTriggerManager",
                            $"  应用Buff到敌人方(全体-SelfBuff): BuffId={buffId}, 敌人={targetEnemy.Config.Name}"
                        );
                    }
                }
            }
        }

        // 应用给目标的Buff（BuffIds）
        if (effect.BuffIds != null && effect.BuffIds.Length > 0)
        {
            foreach (int buffId in effect.BuffIds)
            {
                if (buffId > 0)
                {
                    if (isPlayerSide)
                    {
                        // 玩家先手的BuffIds通常应用到自己，但这里留作扩展
                        DebugEx.LogModule(
                            "CombatTriggerManager",
                            $"  应用Buff到玩家方(全体-TargetBuff): BuffId={buffId}"
                        );
                    }
                    else if (targetEnemy != null)
                    {
                        // 应用Buff到目标敌人方（全体）
                        BuffApplyHelper.ApplyBuff(buffId, targetEnemy.gameObject, true, null);
                        DebugEx.LogModule(
                            "CombatTriggerManager",
                            $"  应用Buff到敌人方(全体-TargetBuff): BuffId={buffId}, 敌人={targetEnemy.Config.Name}"
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取随机先手效果
    /// 从SpecialEffectTable中根据战斗触发类型筛选先手效果进行随机选择
    /// </summary>
    private int GetRandomInitiativeBuff()
    {
        List<int> initiativeEffects = new List<int>();

        var specialEffectTable = GF.DataTable.GetDataTable<SpecialEffectTable>();
        if (specialEffectTable == null)
        {
            DebugEx.WarningModule("CombatTriggerManager", "SpecialEffectTable未加载");
            return 0;
        }

        // 根据当前上下文判断效果类型
        // 如果是EnemyInitiated，获取敌人先手效果（EffectCategory=2）
        // 否则获取玩家先手效果（EffectCategory=1）
        bool isEnemyInitiative = (
            m_CurrentContext != null
            && m_CurrentContext.TriggerType == CombatTriggerType.EnemyInitiated
        );

        int targetCategory = isEnemyInitiative ? 2 : 1; // 1=玩家先手, 2=敌人先手

        // 遍历所有特殊效果配置，筛选出对应的先手效果
        var allEffects = specialEffectTable.GetAllDataRows();
        foreach (var effect in allEffects)
        {
            if (effect.EffectCategory == targetCategory)
            {
                initiativeEffects.Add(effect.Id);
            }
        }

        if (initiativeEffects.Count == 0)
        {
            DebugEx.WarningModule(
                "CombatTriggerManager",
                $"未找到合适的先手效果（Category={targetCategory}）"
            );
            return 0;
        }

        // 随机选择一个效果（可以根据Weight权重来选择，目前先用简单随机）
        int randomEffectId = initiativeEffects[Random.Range(0, initiativeEffects.Count)];

        DebugEx.LogModule(
            "CombatTriggerManager",
            $"随机选择先手效果: {randomEffectId} (候选池:{initiativeEffects.Count}个，Category={targetCategory})"
        );

        return randomEffectId;
    }

    #endregion

    #region 测试菜单

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Test/Combat/Test Sneak Debuff Pool")]
    private static void TestSneakDebuffPool()
    {
        var mgr = CombatTriggerManager.Instance;
        var pool = mgr.GetSneakDebuffPool();
        Debug.Log(
            $"<color=cyan>Sneak Debuff Pool: {string.Join(", ", pool)} ({pool.Count}个)</color>"
        );
    }

    [UnityEditor.MenuItem("Test/Combat/Test Initiative Buff - Player")]
    private static void TestInitiativeBuffPlayer()
    {
        var mgr = CombatTriggerManager.Instance;
        // 模拟遭遇战上下文
        mgr.TriggerCombat(null, CombatTriggerType.Encounter);
        int buff = mgr.GetRandomInitiativeBuff();
        Debug.Log($"<color=cyan>Random Initiative Buff (Player): {buff}</color>");
    }

    [UnityEditor.MenuItem("Test/Combat/Test Initiative Buff - Enemy")]
    private static void TestInitiativeBuffEnemy()
    {
        var mgr = CombatTriggerManager.Instance;
        // 模拟敌方先手上下文
        mgr.TriggerCombat(null, CombatTriggerType.EnemyInitiated);
        int buff = mgr.GetRandomInitiativeBuff();
        Debug.Log($"<color=cyan>Random Initiative Buff (Enemy): {buff}</color>");
    }
#endif

    #endregion
}
