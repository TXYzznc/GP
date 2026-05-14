using GameFramework.UI;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 结算UI表单
/// 显示当前结算统计数据（经验、金币、起源石等）以及本局获取的物品列表
/// 只支持玩家手动点击关闭按钮关闭
/// </summary>
public partial class SettlementUIForm : StateAwareUIForm
{
    #region 字段

    // UI 不会自动关闭，只能由玩家手动点击关闭按钮

    #endregion

    #region 生命周期

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        DebugEx.Log("SettlementUIForm", "结算UI已打开");

        // 绑定关闭按钮事件
        if (varCloseButton != null)
        {
            varCloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // 请求解锁鼠标（使用引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseUnlock();

        // 填充结算数据
        PopulateSettlementData();

        // 填充本局获取物品列表
        PopulateSessionAcquiredItemsAsync().Forget();

        // 显示UI
        ShowUI();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DebugEx.Log("SettlementUIForm", "结算UI正在关闭");

        // 解绑事件
        if (varCloseButton != null)
        {
            varCloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        // 清理物品列表UI
        ClearSessionAcquiredItems();

        // 清理浮窗提示（防止点击奖励时显示的提示框残留）
        GF.UI.CloseAllFloatingTips();

        // 请求锁定鼠标
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseLock();

        // 通知 SettlementManager 已关闭
        SettlementManager.Instance.NotifyUIClosedByUser();

        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // UI 不会自动关闭，等待玩家手动点击关闭按钮
    }

    protected override void SubscribeEvents()
    {
        // 结算UI不需要订阅特定的游戏状态事件
        // 它在 SettlementManager 的指导下显示
    }

    protected override void UnsubscribeEvents()
    {
        // 结算UI不需要取消订阅任何事件
    }

    #endregion

    #region UI 逻辑

    /// <summary>填充结算数据到UI</summary>
    private void PopulateSettlementData()
    {
        var settlementData = SettlementManager.Instance.GetCurrentSettlementData();
        if (settlementData == null)
        {
            DebugEx.Warning("SettlementUIForm", "无结算数据可显示");
            return;
        }

        // 设置标题：胜利显示"成功撤离"，失败显示"修生养息"
        if (varTitle != null)
        {
            varTitle.text = settlementData.IsDefeatScenario() ? "修生养息" : "成功撤离";
        }

        // 设置经验文本
        if (varExperienceText != null)
        {
            varExperienceText.text = $"经验: +{settlementData.GetTotalExperience()}";
        }

        // 设置资源收益文本（显示本局总资源）
        if (varCurrencyText != null)
        {
            varCurrencyText.text = $"资源收益: +{settlementData.GetTotalResourceGain()}";
        }

        DebugEx.Log("SettlementUIForm", "结算数据填充完成");
    }

    /// <summary>填充本局获取物品列表（异步加载图标）</summary>
    private async UniTaskVoid PopulateSessionAcquiredItemsAsync()
    {
        if (varContent == null)
        {
            DebugEx.Warning("SettlementUIForm", "物品列表内容容器未绑定");
            return;
        }

        var inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            DebugEx.Warning("SettlementUIForm", "背包管理器未初始化");
            return;
        }

        // 获取本局物品（排除装备和虚拟物品）
        var sessionItems = inventory.GetSessionAcquiredItems();
        if (sessionItems.Count == 0)
        {
            DebugEx.Log("SettlementUIForm", "本局无物品获取");
            return;
        }

        DebugEx.Log("SettlementUIForm", $"开始显示 {sessionItems.Count} 件物品");

        // 为每个物品创建 AwardItemUI 实例
        foreach (var (itemId, quantity) in sessionItems)
        {
            if (varAwardItemUI == null)
            {
                DebugEx.Warning("SettlementUIForm", "AwardItemUI 预制体未绑定");
                break;
            }

            // 实例化 AwardItemUI
            var itemObj = Instantiate(varAwardItemUI, varContent.transform);
            var awardItemUI = itemObj.GetComponent<AwardItemUI>();

            if (awardItemUI != null)
            {
                awardItemUI.SetData(itemId);
                DebugEx.Log("SettlementUIForm", $"已显示物品: ItemId={itemId}, Quantity={quantity}");
            }

            // 短暂延迟以显示逐个加入的动画效果
            await UniTask.Delay(50);
        }

        DebugEx.Log("SettlementUIForm", "物品列表填充完成");
    }

    /// <summary>清理物品列表UI</summary>
    private void ClearSessionAcquiredItems()
    {
        if (varContent == null)
            return;

        foreach (Transform child in varContent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>关闭按钮点击事件</summary>
    private void OnCloseButtonClicked()
    {
        DebugEx.Log("SettlementUIForm", "用户点击关闭按钮");

        // ⭐ 在离开局内前，将背包中的所有宝物保存到存档
        var playerDataManager = PlayerAccountDataManager.Instance;
        if (playerDataManager != null)
        {
            playerDataManager.SaveTreasuresFromInventory();
            DebugEx.Log("SettlementUIForm", "已保存背包中的宝物到存档");
        }

        CloseWithAnimation();
    }

    #endregion
}
