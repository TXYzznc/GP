using GameFramework.UI;
using UnityEngine;

/// <summary>
/// 结算UI表单
/// 显示当前结算统计数据（经验、金币、起源石等）
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

        DebugEx.LogModule("SettlementUIForm", "结算UI已打开");

        // 绑定关闭按钮事件
        if (varCloseButton != null)
        {
            varCloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // 填充结算数据
        PopulateSettlementData();

        // 显示UI
        ShowUI();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DebugEx.LogModule("SettlementUIForm", "结算UI正在关闭");

        // 解绑事件
        if (varCloseButton != null)
        {
            varCloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

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
            DebugEx.WarningModule("SettlementUIForm", "无结算数据可显示");
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

        DebugEx.LogModule("SettlementUIForm", "结算数据填充完成");
    }

    /// <summary>关闭按钮点击事件</summary>
    private void OnCloseButtonClicked()
    {
        DebugEx.LogModule("SettlementUIForm", "用户点击关闭按钮");
        CloseWithAnimation();
    }

    #endregion
}
