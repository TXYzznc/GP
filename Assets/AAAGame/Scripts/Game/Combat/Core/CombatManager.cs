using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 战斗管理器 - 负责加载和战斗逻辑
/// 注意：这是一个占位符，后续会添加战斗逻辑
/// </summary>
public class CombatManager : SingletonBase<CombatManager>
{
    #region 单例已由基类提供

    // 使用 SingletonBase<CombatManager> 提供的 Instance 属性

    #endregion

    #region 字段

    private bool m_IsInCombat = false;

    #endregion

    #region 属性

    /// <summary>
    /// 是否在战斗中
    /// </summary>
    public bool IsInCombat => m_IsInCombat;

    #endregion

    #region Unity 生命周期

    #endregion

    #region 公共方法

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartCombat()
    {
        if (m_IsInCombat)
        {
            DebugEx.Warning(this.GetType().Name, "已经在战斗中");
            return;
        }

        m_IsInCombat = true;

        // ⭐ 构建敌人信息缓存（AI重构新增）
        if (CombatEntityTracker.Instance != null)
        {
            CombatEntityTracker.Instance.BuildEnemyCache();
        }

        // ⭐ 召唤师战斗组件已在 CombatPreparationState 中初始化完成，这里仅验证状态
        var playerCharacter = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerCharacter != null)
        {
            var chessEntity = playerCharacter.GetComponent<ChessEntity>();
            if (chessEntity != null && chessEntity.Config != null)
            {
                DebugEx.Log(this.GetType().Name,
                    $"✅ 召唤师棋子已准备就绪，ChessId={chessEntity.ChessId}，HP={SummonerRuntimeDataManager.Instance?.MaxHP}");
            }
            else
            {
                DebugEx.Warning(this.GetType().Name, "召唤师棋子未完成初始化");
            }
        }

        // TODO: 初始化战斗数据
        // TODO: 生成战斗单位
        // TODO: 开始战斗

        // 初始化召唤师技能系统
        InitializeSummonerSkillSystem();
    }

    /// <summary>
    /// 初始化召唤师技能系统（战斗开始时调用）
    /// </summary>
    private void InitializeSummonerSkillSystem()
    {
        var playerCharacter = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerCharacter == null)
            return;

        var summonerConfig = PlayerAccountDataManager.Instance?.GetCurrentSummonerConfig();
        if (summonerConfig == null)
        {
            DebugEx.Warning(this.GetType().Name, "未找到召唤师配置，跳过技能系统初始化");
            return;
        }

        // 构建技能上下文
        var ctx = new SummonerSkillContext
        {
            RuntimeData = SummonerRuntimeDataManager.Instance,
            EntityTracker = CombatEntityTracker.Instance,
            // GetAllies 不含召唤师自身，单独拿其 BuffManager
            SummonerBuffManager = playerCharacter.GetComponent<BuffManager>(),
        };

        // 获取或创建 SummonerSkillManager
        var skillManager = playerCharacter.GetComponent<SummonerSkillManager>();
        skillManager ??= playerCharacter.AddComponent<SummonerSkillManager>();

        skillManager.SetContext(ctx);

        // 合并被动技能 ID 和主动技能 ID
        var allSkillIds = new List<int>();
        if (summonerConfig.PassiveSkillIds != null)
            allSkillIds.AddRange(summonerConfig.PassiveSkillIds);
        if (summonerConfig.ActiveSkillIds != null)
            allSkillIds.AddRange(summonerConfig.ActiveSkillIds);

        skillManager.UpdateSkillsFromData(allSkillIds);
        skillManager.SetActive(true);
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    /// <param name="isVictory">是否胜利</param>
    public void EndCombat(bool isVictory)
    {
        if (!m_IsInCombat)
        {
            DebugEx.Warning(this.GetType().Name, "当前不在战斗中");
            return;
        }

        m_IsInCombat = false;
        DebugEx.Success(this.GetType().Name, $"战斗结束 - {(isVictory ? "胜利 ✅" : "失败 ❌")}");

        // 0. 停用召唤师技能系统（Dispose 所有被动 Buff）
        var playerCharacterForSkill = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerCharacterForSkill != null)
        {
            var skillManager = playerCharacterForSkill.GetComponent<SummonerSkillManager>();
            skillManager?.SetActive(false);
        }

        // 0.1 战斗结束前先回写棋子血量、清除所有 Buff（在销毁实体之前）
        BattleChessManager.Instance.OnBattleEnd();

        // 0.2 销毁所有飞行中的投射物（防止战斗结束后投射物仍命中目标）
        DestroyAllActiveProjectiles();

        // 1. 销毁场上所有棋子 GameObject
        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.DestroyAllChess();
        }

        // 2. 清理棋子库存状态（重置所有状态）
        ChessDeploymentTracker.Instance.OnBattleEnd();

        // 3. 清理敌人信息缓存 + 注销召唤师 + 移除战斗组件
        if (CombatEntityTracker.Instance != null)
        {
            CombatEntityTracker.Instance.UnregisterSummoner();
            CombatEntityTracker.Instance.ClearEnemyCache();
        }

        var playerCharacterEnd = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerCharacterEnd != null)
        {
            // 恢复玩家 Layer
            playerCharacterEnd.layer = (int)LayerHelper.Layer.Player;

            var summonerProxy = playerCharacterEnd.GetComponent<SummonerCombatProxy>();
            var attribute = playerCharacterEnd.GetComponent<ChessAttribute>();
            if (summonerProxy != null && attribute != null)
                summonerProxy.UnbindAttribute(attribute);

            // 移除战斗期间动态添加的组件
            var chessEntity = playerCharacterEnd.GetComponent<ChessEntity>();
            if (chessEntity != null) Destroy(chessEntity);
            if (attribute != null) Destroy(attribute);
            var buffManager = playerCharacterEnd.GetComponent<BuffManager>();
            if (buffManager != null) Destroy(buffManager);
        }

        // 4. 清理战斗管理器状态
        ChessSelectionManager.Instance.Cleanup();
        ChessPlacementManager.Instance.Cleanup();

        // 触发战斗结束事件
        CombatEndEventArgs eventArgs = CombatEndEventArgs.Create(isVictory);
        GF.Event.Fire(this, eventArgs);

        // TODO: 计算奖励
    }

    /// <summary>
    /// 销毁场景中所有飞行中的投射物
    /// </summary>
    private void DestroyAllActiveProjectiles()
    {
        var projectiles = FindObjectsOfType<ChessProjectile>();
        if (projectiles.Length > 0)
        {
            foreach (var p in projectiles)
            {
                if (p != null)
                    Destroy(p.gameObject);
            }
        }
    }

    #endregion
}
