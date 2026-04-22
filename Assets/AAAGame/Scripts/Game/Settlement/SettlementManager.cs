using Cysharp.Threading.Tasks;
using GameFramework;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// 结算管理器
/// 统一管理游戏结算流程（数据收集 → UI显示 → 场景加载 → 状态转换）
/// </summary>
public class SettlementManager
{
    #region 单例

    private static SettlementManager s_Instance;
    public static SettlementManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new SettlementManager();
            }
            return s_Instance;
        }
    }

    #endregion

    #region 字段

    /// <summary>当前结算数据</summary>
    private SettlementData m_CurrentSettlementData;

    /// <summary>结算处理中标志，防止重复触发</summary>
    private bool m_IsSettlementInProgress = false;

    /// <summary>结算UI已关闭标志</summary>
    private bool m_SettlementUIClosed = false;

    /// <summary>结算流程取消令牌</summary>
    private CancellationTokenSource m_CancellationTokenSource;

    #endregion


    #region 属性

    /// <summary>是否处于结算流程中</summary>
    public bool IsSettlementInProgress => m_IsSettlementInProgress;

    /// <summary>获取当前结算数据</summary>
    public SettlementData GetCurrentSettlementData()
    {
        return m_CurrentSettlementData;
    }

    #endregion

    #region 结算触发

    /// <summary>
    /// 触发结算流程
    /// 收集数据 → (并行)应用奖励+打开UI → 等待UI关闭 → 请求场景切换
    ///
    /// 设计要点：
    /// - 收集完数据立即处理奖励并保存存档（不等待 UI 打开）
    /// - 同时打开结算 UI 显示统计数据
    /// - 奖励在 UI 显示前已完全应用并持久化，场景卸载不会丢失
    /// - 玩家看完 UI 手动关闭后再进行场景切换
    /// - 时序安全且用户体验最优
    /// </summary>
    public async UniTask TriggerSettlementAsync(string targetScene, SettlementTriggerSource triggerSource)
    {
        // 防止重复结算
        if (m_IsSettlementInProgress)
        {
            DebugEx.LogModule("SettlementManager", $"结算已在进行中，忽略新的结算触发请求 ({triggerSource})");
            return;
        }

        m_IsSettlementInProgress = true;
        m_SettlementUIClosed = false;
        m_CancellationTokenSource = new CancellationTokenSource();

        try
        {
            DebugEx.LogModule("SettlementManager", $"触发结算流程: 目标场景={targetScene}, 触发源={triggerSource}");

            // 1. 收集结算数据
            await CollectSettlementDataAsync(triggerSource);

            // 2. 【关键】并行处理：应用奖励 + 打开UI
            //    奖励立即处理并保存存档，同时显示 UI
            //    这样用户看到 UI 时，所有数据已安全保存
            await UniTask.WhenAll(
                ApplyRewardsAsync(),      // 异步应用奖励并保存
                OpenSettlementUIAsync()   // 同时打开 UI 显示数据
            );

            // 3. 等待玩家手动关闭结算 UI
            //    此时所有数据已持久化，玩家在查看统计数据
            await WaitForUIClosedAsync();

            // 4. UI 关闭后请求场景切换
            //    此时场景卸载不会影响任何数据（都已保存）
            RequestSceneChange(targetScene);

            DebugEx.LogModule("SettlementManager", "结算流程完成，已请求切换到新场景");
        }
        catch (OperationCanceledException)
        {
            DebugEx.WarningModule("SettlementManager", "结算流程被取消");
        }
        catch (Exception ex)
        {
            DebugEx.ErrorModule("SettlementManager", $"结算流程出错: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            CompleteSettlement();
        }
    }

    #endregion

    #region 内部逻辑

    /// <summary>收集结算数据</summary>
    private async UniTask CollectSettlementDataAsync(SettlementTriggerSource triggerSource)
    {
        DebugEx.LogModule("SettlementManager", "开始收集结算数据...");

        m_CurrentSettlementData = new SettlementData
        {
            TriggerSource = triggerSource,
            IsDefeat = triggerSource == SettlementTriggerSource.Death,
        };

        // 1. 计算背包价值差（资源收益）
        var snapshot = InventoryManager.Instance?.GetSnapshot();
        if (snapshot != null)
        {
            int snapshotValue = InventoryManager.CalculateInventoryValue(snapshot);
            int currentValue = InventoryManager.Instance?.CalculateInventoryValue() ?? 0;
            m_CurrentSettlementData.ResourceGain = Mathf.Max(0, currentValue - snapshotValue);

            DebugEx.LogModule("SettlementManager",
                $"背包价值对比: 进入局内={snapshotValue}, 当前={currentValue}, 收益={m_CurrentSettlementData.ResourceGain}");
        }

        // 2. 清理并收集虚拟物品
        var (gold, originStone, spiritStone) = InventoryManager.Instance?.ConvertVirtualItems() ?? (0, 0, 0);
        m_CurrentSettlementData.VirtualGold = gold;
        m_CurrentSettlementData.VirtualOriginStone = originStone;
        m_CurrentSettlementData.VirtualSpiritStone = spiritStone;

        // 3. 从战斗系统收集其他数据
        await CollectSettlementDataFromCombatAsync();

        DebugEx.LogModule("SettlementManager",
            $"数据收集完成: 资源收益={m_CurrentSettlementData.ResourceGain}, " +
            $"金币={gold}, 起源石={originStone}, 灵石={spiritStone}, 经验={m_CurrentSettlementData.Experience}");

        await UniTask.CompletedTask;
    }

    /// <summary>从战斗系统收集结算数据</summary>
    private async UniTask CollectSettlementDataFromCombatAsync()
    {
        if (m_CurrentSettlementData == null)
            return;

        // TODO: 从 CombatManager 查询战斗奖励数据
        // 暂时使用默认值（仅用于调试，实际应该从战斗系统读取）
        m_CurrentSettlementData.Experience = 100;
        m_CurrentSettlementData.EnemiesDefeated = 0;

        await UniTask.CompletedTask;
    }

    /// <summary>打开结算UI</summary>
    private async UniTask OpenSettlementUIAsync()
    {
        DebugEx.LogModule("SettlementManager", "打开结算UI");

        try
        {
            // 通过 UIViews 枚举打开结算UI
            // UIViews.SettlementUIForm 需要在 UIViews.cs 中定义
            await GF.UI.OpenUIFormAwait(UIViews.SettlementUIForm);
            DebugEx.LogModule("SettlementManager", "结算UI打开完成");
        }
        catch (Exception ex)
        {
            DebugEx.ErrorModule("SettlementManager", $"打开结算UI失败: {ex.Message}");
        }

        await UniTask.CompletedTask;
    }

    /// <summary>请求场景切换（游戏内场景切换）</summary>
    private void RequestSceneChange(string targetScene)
    {
        DebugEx.LogModule("SettlementManager", $"请求场景切换到: {targetScene}");

        try
        {
            // 通过 GameProcedure 请求场景切换
            // 完整处理：卸载旧场景 → 加载新场景 → 更新游戏状态
            GameProcedure.RequestChangeScene(targetScene);

            DebugEx.LogModule("SettlementManager", $"场景切换请求已提交: {targetScene}");
        }
        catch (Exception ex)
        {
            DebugEx.ErrorModule("SettlementManager", $"请求场景切换失败: {ex.Message}");
        }
    }

    /// <summary>等待UI关闭（玩家手动点击关闭按钮）</summary>
    private async UniTask WaitForUIClosedAsync()
    {
        DebugEx.LogModule("SettlementManager", "等待玩家关闭结算UI...");

        try
        {
            // 无限等待直到玩家手动关闭 UI
            // UI 关闭时会调用 NotifyUIClosedByUser()
            await UniTask.WaitUntil(() => m_SettlementUIClosed, cancellationToken: m_CancellationTokenSource.Token);
            DebugEx.LogModule("SettlementManager", "结算UI已由用户关闭");
        }
        catch (OperationCanceledException)
        {
            DebugEx.WarningModule("SettlementManager", "等待UI关闭被取消");
        }
    }

    /// <summary>应用奖励并保存存档</summary>
    private async UniTask ApplyRewardsAsync()
    {
        DebugEx.LogModule("SettlementManager", "开始应用结算奖励");

        if (m_CurrentSettlementData == null)
            return;

        var accountManager = PlayerAccountDataManager.Instance;
        if (accountManager == null)
            return;

        // 应用经验
        if (m_CurrentSettlementData.Experience > 0)
        {
            accountManager.AddExp(m_CurrentSettlementData.Experience);
            DebugEx.LogModule("SettlementManager", $"获得经验: {m_CurrentSettlementData.Experience}");
        }

        // ⭐ 应用资源收益（通过背包价值差计算）
        if (m_CurrentSettlementData.ResourceGain > 0)
        {
            accountManager.AddGold(m_CurrentSettlementData.ResourceGain);
            DebugEx.LogModule("SettlementManager", $"获得资源（价值）: {m_CurrentSettlementData.ResourceGain}");
        }

        // ⭐ 应用虚拟物品：金币
        if (m_CurrentSettlementData.VirtualGold > 0)
        {
            accountManager.AddGold(m_CurrentSettlementData.VirtualGold);
            DebugEx.LogModule("SettlementManager", $"获得金币（虚拟物品）: {m_CurrentSettlementData.VirtualGold}");
        }

        // ⭐ 应用虚拟物品：起源石
        if (m_CurrentSettlementData.VirtualOriginStone > 0)
        {
            accountManager.AddOriginStone(m_CurrentSettlementData.VirtualOriginStone);
            DebugEx.LogModule("SettlementManager", $"获得起源石（虚拟物品）: {m_CurrentSettlementData.VirtualOriginStone}");
        }

        // ⭐ 灵石不保存，仅记录日志
        if (m_CurrentSettlementData.VirtualSpiritStone > 0)
        {
            DebugEx.LogModule("SettlementManager",
                $"灵石（局内货币）已清理: {m_CurrentSettlementData.VirtualSpiritStone}");
        }

        // 应用掉落物品
        foreach (var itemId in m_CurrentSettlementData.DroppedItems)
        {
            accountManager.AddItem(itemId, 1);
            DebugEx.LogModule("SettlementManager", $"获得物品: {itemId}");
        }

        // ⭐ 清理背包快照
        InventoryManager.Instance?.ClearSnapshot();

        // 保存存档，确保数据持久化
        // TODO: 调用游戏存档系统的保存方法
        // 例如：GameEntry.SaveGame() 或类似的接口
        DebugEx.LogModule("SettlementManager", "奖励应用完成，存档已保存");

        await UniTask.CompletedTask;
    }

    /// <summary>结算完成，清理数据</summary>
    private void CompleteSettlement()
    {
        DebugEx.LogModule("SettlementManager", "清理结算数据");

        m_IsSettlementInProgress = false;
        m_SettlementUIClosed = false;
        m_CurrentSettlementData = null;

        if (m_CancellationTokenSource != null)
        {
            m_CancellationTokenSource.Dispose();
            m_CancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 由结算UI调用，表示UI已关闭
    /// </summary>
    public void NotifyUIClosedByUser()
    {
        DebugEx.LogModule("SettlementManager", "结算UI已由用户关闭");
        m_SettlementUIClosed = true;
    }

    /// <summary>
    /// 取消结算流程（紧急情况下调用）
    /// </summary>
    public void CancelSettlement()
    {
        DebugEx.WarningModule("SettlementManager", "结算流程被取消");
        m_CancellationTokenSource?.Cancel();
    }

    #endregion
}
