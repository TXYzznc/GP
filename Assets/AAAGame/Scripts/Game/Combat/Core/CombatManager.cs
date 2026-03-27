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

    private void OnEnable()
    {
        DebugEx.Log("CombatManager", "已启用");
    }

    private void OnDisable()
    {
        DebugEx.Log("CombatManager", "已禁用");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartCombat()
    {
        if (m_IsInCombat)
        {
            DebugEx.Warning("CombatManager", "已经在战斗中");
            return;
        }

        m_IsInCombat = true;
        DebugEx.Log("CombatManager", "战斗开始");

        // ⭐ 构建敌人信息缓存（AI重构新增）
        if (CombatEntityTracker.Instance != null)
        {
            CombatEntityTracker.Instance.BuildEnemyCache();
        }

        // 注册召唤师战斗代理 + 动态添加战斗组件 + 重置HP/MP
        var playerCharacter = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        if (playerCharacter != null)
        {
            var summonerProxy = playerCharacter.GetComponent<SummonerCombatProxy>();
            if (summonerProxy != null)
            {
                // 先重置HP/MP，确保后续 MaxHP 读取为本场战斗的满值
                SummonerRuntimeDataManager.Instance?.InitializeForBattle();

                // 从 SummonerTable.SummonChessId → SummonChessTable 读取召唤师战斗配置
                // 注意：召唤师行不走 ChessDataManager（Validate 不通过），直接从配置表行构造
                SummonChessConfig summonChessConfig = null;
                int summonChessId = 0;
                var summonerTableConfig = PlayerAccountDataManager.Instance?.GetCurrentSummonerConfig();
                if (summonerTableConfig != null)
                {
                    summonChessId = summonerTableConfig.SummonChessId;
                    var chessTableRow = GF.DataTable.GetDataTable<SummonChessTable>()?.GetDataRow(summonChessId);
                    if (chessTableRow != null)
                    {
                        // 直接构造，跳过 Validate（召唤师行的 AIType/StarLevel/MaxHp 等字段不适用棋子规则）
                        summonChessConfig = new SummonChessConfig
                        {
                            Id = chessTableRow.Id,
                            Name = chessTableRow.Name,
                            Quality = chessTableRow.Quality,
                            PopCost = 0,
                            Races = chessTableRow.Races ?? System.Array.Empty<int>(),
                            Classes = chessTableRow.Classes ?? System.Array.Empty<int>(),
                            StarLevel = 1,
                            NextStarId = 0,
                            PrefabId = chessTableRow.PrefabId,
                            IconId = chessTableRow.IconId,
                            // HP/MP 不从这里读，由 InitializeAsSummoner 用 SummonerRuntimeDataManager 覆盖
                            MaxHp = 1,
                            MaxMp = 0,
                            InitialMp = 0,
                            AtkDamage = 0,
                            AtkSpeed = 0.01,
                            AtkRange = chessTableRow.AtkRange,
                            Armor = chessTableRow.Armor,
                            MagicResist = chessTableRow.MagicResist,
                            MoveSpeed = chessTableRow.MoveSpeed,
                            CritRate = 0,
                            CritDamage = 1.5,
                            SpellPower = 0,
                            Shield = 0,
                            CooldownReduce = 0,
                            PassiveIds = System.Array.Empty<int>(),
                            NormalAtkId = 0,
                            Skill1Id = 0,
                            Skill2Id = 0,
                            AIType = 0,
                        };
                    }
                    else
                    {
                        DebugEx.WarningModule("CombatManager",
                            $"SummonChessTable 中未找到召唤师行 ID={summonChessId}，使用空配置");
                    }
                }

                // 动态添加战斗组件（如已存在则复用，保证幂等）
                var buffManager = playerCharacter.GetComponent<BuffManager>();
                if (buffManager == null)
                    buffManager = playerCharacter.AddComponent<BuffManager>();

                var attribute = playerCharacter.GetComponent<ChessAttribute>();
                if (attribute == null)
                    attribute = playerCharacter.AddComponent<ChessAttribute>();

                var chessEntity = playerCharacter.GetComponent<ChessEntity>();
                if (chessEntity == null)
                    chessEntity = playerCharacter.AddComponent<ChessEntity>();

                // InitializeAsSummoner：从 config 读防御属性，HP 来自 SummonerRuntimeDataManager.MaxHP
                chessEntity.InitializeAsSummoner(summonChessId, summonChessConfig, 0);

                // 绑定 HP 变化事件 → 同步到 SummonerRuntimeDataManager（CombatUI 订阅它刷新 varHPSlider）
                summonerProxy.ResetDeadState();
                summonerProxy.BindAttribute(attribute);

                // 战斗期间将玩家 Layer 改为 Chess，使投射物/武器碰撞检测能命中召唤师
                playerCharacter.layer = (int)LayerHelper.Layer.Chess;

                CombatEntityTracker.Instance?.RegisterSummoner(summonerProxy);
                DebugEx.LogModule("CombatManager",
                    $"召唤师战斗组件已就绪，ChessId={summonChessId}，HP={SummonerRuntimeDataManager.Instance?.MaxHP}");
            }
            else
            {
                DebugEx.WarningModule("CombatManager", "玩家角色上未找到 SummonerCombatProxy");
            }
        }

        // TODO: 初始化战斗数据
        // TODO: 生成战斗单位
        // TODO: 开始战斗
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    /// <param name="isVictory">是否胜利</param>
    public void EndCombat(bool isVictory)
    {
        if (!m_IsInCombat)
        {
            Log.Warning("CombatManager: 当前不在战斗中");
            return;
        }

        m_IsInCombat = false;
        Log.Info($"CombatManager: 战斗结束 - {(isVictory ? "胜利" : "失败")}");

        // 0. 战斗结束前先回写棋子血量、清除所有 Buff（在销毁实体之前）
        BattleChessManager.Instance.OnBattleEnd();

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

            DebugEx.LogModule("CombatManager", "召唤师战斗组件已移除");
        }

        // 4. 清理战斗管理器状态
        ChessSelectionManager.Instance.Cleanup();
        ChessPlacementManager.Instance.Cleanup();

        // 触发战斗结束事件
        CombatEndEventArgs eventArgs = CombatEndEventArgs.Create(isVictory);
        GF.Event.Fire(this, eventArgs);

        // TODO: 计算奖励
    }

    #endregion
}
