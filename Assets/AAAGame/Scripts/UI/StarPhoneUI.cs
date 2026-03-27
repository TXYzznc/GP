using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class StarPhoneUI : StateAwareUIForm
{
    #region 事件订阅

    protected override void SubscribeEvents()
    {
        Log.Info("StarPhoneUI: 订阅局内和探索状态事件");
        // 订阅局内事件（进入 → 显示）
        GF.Event.Subscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Subscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 订阅探索事件（探索 → 战斗）
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("StarPhoneUI: 取消订阅局内和探索状态事件");
        // 取消订阅局内事件
        GF.Event.Unsubscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Unsubscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 取消订阅探索事件
        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    #endregion

    #region 事件处理

    private void OnInGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到局内进入事件 → 显示UI");
        ShowUI();
        RefreshStarPhone();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到局内离开事件 → 保持显示（星盘始终可见）");
        // 注释：星盘在局内外都保持显示，不再隐藏
        // HideUI();
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到探索进入事件 → 显示UI");
        ShowUI();
        RefreshStarPhone();
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到探索离开事件 → 保持显示（星盘始终可见）");
        // 注释：星盘在局内外都保持显示，不再隐藏
        // HideUI();
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新星盘信息
    /// </summary>
    private void RefreshStarPhone()
    {
        // 设置标题
        if (varTitle != null)
        {
            varTitle.text = "星盘";
        }

        // 绑定按钮事件
        if (varStarPhone != null)
        {
            varStarPhone.onClick.RemoveAllListeners();
            varStarPhone.onClick.AddListener(OnStarPhoneClicked);
        }

        Log.Info("StarPhoneUI: 星盘信息已刷新");
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 星盘按钮点击回调
    /// </summary>
    private void OnStarPhoneClicked()
    {
        DebugEx.Log("StarPhoneUI", "点击了星盘按钮");

        // ⚠️ 临时功能：点击星盘打开背包UI
        // 注意：这是临时实现，后续可能会改为打开星盘详细界面或其他功能
        // TODO: 后续需要根据实际需求修改此处逻辑
        GF.UI.OpenUIForm(UIViews.InventoryUI);
        DebugEx.Success("StarPhoneUI", "已打开背包UI（临时功能）");
    }

    #endregion
}
