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
            DebugEx.Error(nameof(CombatTriggerManager), "敌人实体为空");
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
                DebugEx.Log(
                    nameof(CombatTriggerManager),
                    $"偷袭触发: {enemy.Config.Name}, 可选效果数={m_CurrentContext.AvailableDebuffs.Count}"
                );
                break;

            case CombatTriggerType.Encounter:
                // 遭遇战 = 玩家先手，获取候选Buff池，由UI让玩家三选一
                m_CurrentContext.AvailableBuffIds = GetPlayerInitiativeBuffPool();
                CombatTriggerEvents.FirePlayerInitiativeTriggered(m_CurrentContext.AvailableBuffIds);
                DebugEx.Log(
                    nameof(CombatTriggerManager),
                    $"遭遇战触发: {enemy.Config.Name}, 候选先手效果数={m_CurrentContext.AvailableBuffIds.Count}"
                );
                break;

            case CombatTriggerType.EnemyInitiated:
                m_CurrentContext.InitiativeBuffId = GetRandomInitiativeBuff();
                // 延迟应用：存储到SelectedEffectId，由CombatState在棋子就绪后统一应用
                m_CurrentContext.SelectedEffectId = m_CurrentContext.InitiativeBuffId;
                CombatTriggerEvents.FireEnemyInitiativeTriggered(m_CurrentContext.InitiativeBuffId);
                DebugEx.Log(
                    nameof(CombatTriggerManager),
                    $"敌方先手触发: {enemy.Config.Name}, 敌人先手效果={m_CurrentContext.InitiativeBuffId} (延迟到棋子就绪后应用)"
                );
                break;

            default:
                DebugEx.Log(nameof(CombatTriggerManager), $"普通触发: {enemy.Config.Name}");
                break;
        }

        // 输出战斗触发信息
        string triggerModeName = GetTriggerModeName(triggerType);
        DebugEx.Success(nameof(CombatTriggerManager), $"进入战斗 - 敌人: {enemy.Config.Name} ({triggerModeName})");

        // 注意：不在此处调用 EnemyEntityManager，由调用方负责进入战斗状态
        // 避免 CombatTriggerManager ↔ EnemyEntityManager 循环调用
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
    /// 获取偷袭效果池（Category=3）
    /// </summary>
    private List<int> GetSneakDebuffPool()
    {
        var effectIds = GetCombatEffectPoolByCategory(3);

        // 随机打乱顺序（Fisher-Yates洗牌）
        for (int i = effectIds.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = effectIds[i];
            effectIds[i] = effectIds[randomIndex];
            effectIds[randomIndex] = temp;
        }

        DebugEx.Log(
            nameof(CombatTriggerManager),
            $"获取偷袭效果池: {effectIds.Count}个 - [{string.Join(", ", effectIds)}]"
        );

        return effectIds;
    }

    /// <summary>
    /// 应用先手效果到玩家方（全体应用）
    /// 从SpecialEffectTable中获取效果配置，并应用其包含的所有Buff
    /// </summary>
    public void ApplyInitiativeEffectToPlayer(int effectId)
    {
        if (effectId <= 0)
        {
            return;
        }

        var context = GameEffectContext.CreateMultiTarget(EffectSource.CombatPrep, new System.Collections.Generic.List<UnityEngine.GameObject>());
        GameEffectService.Instance.Execute(effectId, context);

        DebugEx.Log(nameof(CombatTriggerManager), $"应用先手效果到玩家方: EffectId={effectId}");
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

        var context = GameEffectContext.CreateMultiTarget(EffectSource.CombatPrep, new System.Collections.Generic.List<UnityEngine.GameObject>(), null);
        GameEffectService.Instance.Execute(effectId, context);

        // 注：敌方先手效果提示UI由 CombatPreparationState.ShowEnemyInitiativeBuffIfNeeded() 处理

        DebugEx.Log(nameof(CombatTriggerManager), $"应用先手效果到敌人方: EffectId={effectId}, 敌人={enemy.Config.Name}");
    }

    /// <summary>
    /// 获取玩家先手效果池（Category=1，遭遇战三选一）
    /// </summary>
    private List<int> GetPlayerInitiativeBuffPool()
    {
        var effectIds = GetCombatEffectPoolByCategory(1);

        // 随机打乱顺序（Fisher-Yates洗牌）
        for (int i = effectIds.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = effectIds[i];
            effectIds[i] = effectIds[randomIndex];
            effectIds[randomIndex] = temp;
        }

        DebugEx.Log(
            nameof(CombatTriggerManager),
            $"获取玩家先手效果池: {effectIds.Count}个 - [{string.Join(", ", effectIds)}]"
        );

        return effectIds;
    }

    /// <summary>
    /// 获取随机先手效果（按权重）
    /// </summary>
    private int GetRandomInitiativeBuff()
    {
        bool isEnemyInitiative = (
            m_CurrentContext != null
            && m_CurrentContext.TriggerType == CombatTriggerType.EnemyInitiated
        );

        int category = isEnemyInitiative ? 2 : 1;
        int effectId = GetWeightedRandomEffect(category);

        if (effectId <= 0)
        {
            string effectType = isEnemyInitiative ? "敌人先手" : "玩家先手";
            DebugEx.Warning(nameof(CombatTriggerManager), $"未找到合适的{effectType}效果");
            return 0;
        }

        string type = isEnemyInitiative ? "敌人先手" : "玩家先手";
        DebugEx.Log(nameof(CombatTriggerManager), $"随机选择{type}效果: {effectId}");
        return effectId;
    }

    /// <summary>
    /// 从 CombatEffectTable 中按类别获取效果池（返回 SpecialEffectId 列表）
    /// </summary>
    private List<int> GetCombatEffectPoolByCategory(int category)
    {
        var result = new List<int>();

        var table = GF.DataTable.GetDataTable<CombatEffectTable>();
        if (table == null)
        {
            DebugEx.Warning(nameof(CombatTriggerManager), "CombatEffectTable未加载");
            return result;
        }

        var allRows = table.GetAllDataRows();
        foreach (var row in allRows)
        {
            if (row.Category == category)
            {
                result.Add(row.SpecialEffectId);
            }
        }

        return result;
    }

    /// <summary>
    /// 按权重随机选择一个效果（返回 SpecialEffectId）
    /// </summary>
    private int GetWeightedRandomEffect(int category)
    {
        var table = GF.DataTable.GetDataTable<CombatEffectTable>();
        if (table == null)
        {
            DebugEx.Warning(nameof(CombatTriggerManager), "CombatEffectTable未加载");
            return 0;
        }

        var candidates = new List<(int effectId, int weight)>();
        int totalWeight = 0;

        var allRows = table.GetAllDataRows();
        foreach (var row in allRows)
        {
            if (row.Category == category && row.Weight > 0)
            {
                candidates.Add((row.SpecialEffectId, row.Weight));
                totalWeight += row.Weight;
            }
        }

        if (candidates.Count == 0 || totalWeight <= 0)
            return 0;

        int roll = Random.Range(0, totalWeight);
        int accumulated = 0;
        foreach (var (effectId, weight) in candidates)
        {
            accumulated += weight;
            if (roll < accumulated)
                return effectId;
        }

        return candidates[candidates.Count - 1].effectId;
    }

    /// <summary>
    /// 根据 SpecialEffectId 获取对应的 CombatEffectTable 行（用于获取 IconId 等）
    /// </summary>
    public CombatEffectTable GetCombatEffectRow(int specialEffectId)
    {
        var table = GF.DataTable.GetDataTable<CombatEffectTable>();
        if (table == null) return null;

        var allRows = table.GetAllDataRows();
        foreach (var row in allRows)
        {
            if (row.SpecialEffectId == specialEffectId)
                return row;
        }
        return null;
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
