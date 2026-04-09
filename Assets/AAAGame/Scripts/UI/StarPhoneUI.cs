using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class StarPhoneUI : StateAwareUIForm
{
    #region 事件订阅

    protected override void SubscribeEvents()
    {
        Log.Info("StarPhoneUI: 订阅局外、局内和探索状态事件");
        // 订阅局外事件（进入基地 → 显示）
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);

        // 订阅局内事件（进入 → 显示）
        GF.Event.Subscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Subscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 订阅探索事件（探索 → 战斗）
        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("StarPhoneUI: 取消订阅局外、局内和探索状态事件");
        // 取消订阅局外事件
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);

        // 取消订阅局内事件
        GF.Event.Unsubscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Unsubscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 取消订阅探索事件
        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    #endregion

    #region 事件处理

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到局外进入事件 → 显示UI");
        ShowUI();
        RefreshStarPhone();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到局外离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnInGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到局内进入事件 → 显示UI");
        ShowUI();
        RefreshStarPhone();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.Log("StarPhoneUI", "收到局内离开事件 → 隐藏UI");
        HideUI();
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

    #region 动画

    protected new void ShowUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.ShowUI(); return; }
        DOTween.Kill(gameObject);
        cg.alpha = 0f; cg.blocksRaycasts = true; cg.interactable = true;
        rt.localScale = Vector3.one * 0.8f;
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(1f, 0.25f).SetEase(Ease.OutQuart))
            .Join(rt.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutQuart));
    }

    protected new void HideUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.HideUI(); return; }
        DOTween.Kill(gameObject);
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(0f, 0.2f).SetEase(Ease.InQuart))
            .Join(rt.DOScale(Vector3.one * 0.8f, 0.2f).SetEase(Ease.InQuart))
            .OnComplete(() => { cg.interactable = false; cg.blocksRaycasts = false; });
    }

    #endregion
}
