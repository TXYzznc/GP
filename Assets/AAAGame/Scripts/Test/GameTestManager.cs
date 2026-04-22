using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 统一测试管理器 - 管理游戏中所有可选的测试功能
/// 仅在编辑器和开发版本中有效
/// </summary>
public class GameTestManager : SingletonBase<GameTestManager>
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    #region 字段

    [Header("测试开关")]
    [Tooltip("是否启用战斗自动结束（进入战斗后3秒自动胜利结束）")]
    [SerializeField]
    private bool m_AutoEndCombat = false;

    // 性能优化：缓存单例引用
    private GameStateManager m_GameStateManager;

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();

        // 缓存单例引用，避免每帧访问
        m_GameStateManager = GameStateManager.Instance;

        DebugEx.LogModule("GameTestManager", "测试管理器初始化完成");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region 棋子系统测试

    /// <summary>
    /// 测试棋子系统初始化
    /// </summary>
    private void TestChessSystem()
    {
        Log.Info("========== 棋子系统测试开始 ==========");

        // 测试1：ChessDataManager
        if (ChessDataManager.Instance != null)
        {
            Log.Info("  ✓ ChessDataManager 初始化成功");
        }
        else
        {
            Log.Warning("  ✗ ChessDataManager 未初始化");
        }

        // 测试2：ChessFactory
        Log.Info($"  ✓ ChessFactory: {ChessFactory.GetDebugInfo()}");

        // 测试3：ChessUnlockManager
        if (ChessUnlockManager.Instance != null)
        {
            Log.Info("  ✓ ChessUnlockManager 初始化成功");

            // 测试解锁功能
            bool result1 = ChessUnlockManager.Instance.UnlockChess(1);
            Log.Info($"    解锁棋子1(首次): {(result1 ? "成功" : "已解锁")}");

            bool result2 = ChessUnlockManager.Instance.UnlockChess(1);
            Log.Info($"    重复解锁1(重复): {(result2 ? "成功" : "已解锁")} (应显示已解锁)");

            int count = ChessUnlockManager.Instance.GetUnlockedCount();
            Log.Info($"    当前已解锁数量: {count}");
        }
        else
        {
            Log.Warning("  ✗ ChessUnlockManager 未初始化");
        }

        Log.Info("========== 棋子系统测试结束 ==========");
    }

    /// <summary>
    /// 确保有测试用的已解锁棋子，如果解锁列表为空，自动解锁配置表中的所有棋子
    /// </summary>
    private void EnsureTestChessUnlocked()
    {
        if (ChessUnlockManager.Instance.GetUnlockedCount() > 0)
        {
            Log.Info($"[测试] 已有 {ChessUnlockManager.Instance.GetUnlockedCount()} 个已解锁棋子");
            return;
        }

        // 解锁配置表中的所有棋子
        var allIds = ChessDataManager.Instance.GetAllConfigIds();
        foreach (var id in allIds)
        {
            ChessUnlockManager.Instance.UnlockChess(id);
        }

        Log.Info($"[测试] 自动解锁了 {allIds.Count} 个棋子用于测试");
    }

    #endregion

    #region 运行时数据系统测试

    /// <summary>
    /// 测试运行时数据管理系统
    /// </summary>
    private void TestRuntimeDataSystem()
    {
        DebugEx.LogModule("GameTestManager", "========== 运行时数据管理系统测试开始 ==========");

        // 测试1：PlayerRuntimeDataManager
        if (PlayerRuntimeDataManager.Instance != null)
        {
            DebugEx.LogModule("GameTestManager", "✓ PlayerRuntimeDataManager 实例存在");

            if (PlayerRuntimeDataManager.Instance.IsInitialized)
            {
                DebugEx.LogModule("GameTestManager", $"✓ PlayerRuntimeDataManager 已初始化");
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 当前污染值: {PlayerRuntimeDataManager.Instance.CurrentCorruption:F1}/{PlayerRuntimeDataManager.Instance.MaxCorruption:F1}"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 污染值百分比: {PlayerRuntimeDataManager.Instance.CorruptionPercent:P1}"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 污染值增长速度: {PlayerRuntimeDataManager.Instance.CorruptionGrowthRate:F1}/秒"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 当前移速: {PlayerRuntimeDataManager.Instance.CurrentMoveSpeed:F1}"
                );

                // 测试污染值操作
                DebugEx.LogModule("GameTestManager", "  测试污染值操作...");
                float oldCorruption = PlayerRuntimeDataManager.Instance.CurrentCorruption;
                PlayerRuntimeDataManager.Instance.AddCorruption(10f);
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 增加10点污染值: {oldCorruption:F1} -> {PlayerRuntimeDataManager.Instance.CurrentCorruption:F1}"
                );

                // 测试战斗失败
                PlayerRuntimeDataManager.Instance.OnCombatDefeat();
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 战斗失败后污染值: {PlayerRuntimeDataManager.Instance.CurrentCorruption:F1}"
                );
            }
            else
            {
                DebugEx.WarningModule(
                    "GameTestManager",
                    "✗ PlayerRuntimeDataManager 未初始化（需要进入局内状态）"
                );
            }
        }
        else
        {
            DebugEx.ErrorModule("GameTestManager", "✗ PlayerRuntimeDataManager 实例不存在");
        }

        // 测试2：SummonerRuntimeDataManager
        if (SummonerRuntimeDataManager.Instance != null)
        {
            DebugEx.LogModule("GameTestManager", "✓ SummonerRuntimeDataManager 实例存在");

            if (SummonerRuntimeDataManager.Instance.IsInitialized)
            {
                DebugEx.LogModule("GameTestManager", $"✓ SummonerRuntimeDataManager 已初始化");
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 当前HP: {SummonerRuntimeDataManager.Instance.CurrentHP:F1}/{SummonerRuntimeDataManager.Instance.MaxHP:F1}"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - HP百分比: {SummonerRuntimeDataManager.Instance.HPPercent:P1}"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 当前MP: {SummonerRuntimeDataManager.Instance.CurrentMP:F1}/{SummonerRuntimeDataManager.Instance.MaxMP:F1}"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - MP百分比: {SummonerRuntimeDataManager.Instance.MPPercent:P1}"
                );
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - MP恢复速度: {SummonerRuntimeDataManager.Instance.MPRegen:F1}/秒"
                );

                // 测试HP/MP操作
                DebugEx.LogModule("GameTestManager", "  测试HP/MP操作...");
                SummonerRuntimeDataManager.Instance.ReduceHP(20f);
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 减少20点HP后: {SummonerRuntimeDataManager.Instance.CurrentHP:F1}/{SummonerRuntimeDataManager.Instance.MaxHP:F1}"
                );

                bool mpConsumed = SummonerRuntimeDataManager.Instance.ConsumeMP(10f);
                DebugEx.LogModule(
                    "GameTestManager",
                    $"  - 消耗10点MP: {(mpConsumed ? "成功" : "失败")} - 当前MP: {SummonerRuntimeDataManager.Instance.CurrentMP:F1}"
                );
            }
            else
            {
                DebugEx.WarningModule(
                    "GameTestManager",
                    "✗ SummonerRuntimeDataManager 未初始化（需要进入战斗状态）"
                );
            }
        }
        else
        {
            DebugEx.ErrorModule("GameTestManager", "✗ SummonerRuntimeDataManager 实例不存在");
        }

        // 测试3：数据表配置
        if (PlayerAccountDataManager.Instance != null)
        {
            var summonerConfig = PlayerAccountDataManager.Instance.GetCurrentSummonerConfig();
            if (summonerConfig != null)
            {
                DebugEx.LogModule("GameTestManager", "✓ 召唤师配置读取成功");
                DebugEx.LogModule("GameTestManager", $"  - 召唤师名称: {summonerConfig.Name}");
                DebugEx.LogModule("GameTestManager", $"  - 基础HP: {summonerConfig.BaseHP}");
                DebugEx.LogModule("GameTestManager", $"  - 基础MP: {summonerConfig.BaseMP}");
                DebugEx.LogModule("GameTestManager", $"  - MP恢复: {summonerConfig.MPRegen}");
                DebugEx.LogModule("GameTestManager", $"  - 移动速度: {summonerConfig.MoveSpeed}");
            }
            else
            {
                DebugEx.WarningModule(
                    "GameTestManager",
                    "✗ 召唤师配置读取失败（可能没有当前存档）"
                );
            }
        }

        DebugEx.LogModule("GameTestManager", "========== 运行时数据管理系统测试结束 ==========");
    }

    #endregion

    #region 战斗测试

    /// <summary>
    /// 模拟战斗自动结束（3秒后自动胜利结束）
    /// </summary>
    public void TestAutoEndCombat()
    {
        if (CombatManager.Instance == null || !CombatManager.Instance.IsInCombat)
        {
            Log.Warning("[测试] 当前不在战斗中，无法触发自动结束");
            return;
        }

        Log.Info("[测试] 3秒后自动结束战斗（胜利结束）");
        Invoke(nameof(DoAutoEndCombat), 3f);
    }

    private void DoAutoEndCombat()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.IsInCombat)
        {
            CombatManager.Instance.EndCombat(true);
        }
    }

    #endregion

    #region 结算系统测试

    /// <summary>
    /// 快速触发游戏结算 - 传送
    /// </summary>
    public void TestTriggerSettlementTeleport()
    {
        DebugEx.LogModule("GameTestManager", "[测试] 触发结算 - 传送方式");
        SettlementManager.Instance.TriggerSettlementAsync("BaseScene", SettlementTriggerSource.Teleport).Forget();
    }

    /// <summary>
    /// 快速触发游戏结算 - 污染过高死亡
    /// </summary>
    public void TestTriggerSettlementCorruptionDeath()
    {
        DebugEx.LogModule("GameTestManager", "[测试] 触发结算 - 污染过高死亡");
        SettlementManager.Instance.TriggerSettlementAsync("BaseScene", SettlementTriggerSource.Death).Forget();
    }

    #endregion

    #region 外部调用

    /// <summary>
    /// 在战斗开始时调用，根据配置自动结束
    /// </summary>
    public void OnCombatStarted()
    {
        if (m_AutoEndCombat)
        {
            TestAutoEndCombat();
        }
    }

    #endregion

#endif
}
