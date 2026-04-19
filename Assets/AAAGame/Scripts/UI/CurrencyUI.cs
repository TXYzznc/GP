using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class CurrencyUI : StateAwareUIForm
{
    #region 字段

    private List<CurrencyItem> m_CurrencyItems = new List<CurrencyItem>();

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        Log.Info("CurrencyUI: 订阅局内、局外和战斗状态事件");
        // 订阅局外事件（进入基地 → 显示）
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);

        // 订阅局内事件（进入 → 显示）
        GF.Event.Subscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Subscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 订阅战斗事件（探索 → 战斗）
        GF.Event.Subscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("CurrencyUI: 取消订阅局内、局外和战斗状态事件");
        // 取消订阅局外事件
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);

        // 取消订阅局内事件
        GF.Event.Unsubscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Unsubscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 取消订阅战斗事件
        GF.Event.Unsubscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    #endregion

    #region 事件处理

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到局外进入事件 → 显示UI");
        ShowUI();
        RefreshCurrency();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到局外离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnInGameEnter(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到局内进入事件 → 显示UI");
        ShowUI();
        RefreshCurrency();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到局内离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnCombatEnter(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到战斗进入事件 → 隐藏UI");
        HideUI();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到战斗离开事件 → 显示UI");
        ShowUI();
        RefreshCurrency();
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到探索进入事件 → 显示UI");
        ShowUI();
        RefreshCurrency();
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        Log.Info("CurrencyUI: 收到探索离开事件 → 隐藏UI");
        HideUI();
    }
    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新货币显示
    /// </summary>
    public void RefreshCurrency()
    {
        // 清理已生成的货币项
        ClearCurrencyItems();

        // 获取当前存档数据
        var saveData = PlayerAccountDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
        {
            Log.Warning("CurrencyUI: 当前没有存档数据");
            return;
        }

        // 创建两种货币（直接使用 ResourceConfigTable 中的图标ID）
        CreateCurrencyItem(1101, saveData.Gold); // 金币图标 ID=1101
        CreateCurrencyItem(1102, saveData.OriginStone); // 起源石图标 ID=1102

        Log.Info("CurrencyUI: 货币信息已刷新");
    }

    /// <summary>
    /// 创建货币项
    /// </summary>
    /// <param name="iconId">ResourceConfigTable 中的图标资源ID</param>
    /// <param name="count">货币数量</param>
    private void CreateCurrencyItem(int iconId, int count)
    {
        if (varCurrencyItem == null || varCurrencyPanel == null)
        {
            Log.Warning("CurrencyUI: 货币项模板或面板未配置");
            return;
        }

        // 实例化货币项
        GameObject itemObj = Instantiate(varCurrencyItem, varCurrencyPanel.transform);
        itemObj.SetActive(true);

        // 获取 CurrencyItem 组件
        CurrencyItem currencyItem = itemObj.GetComponent<CurrencyItem>();
        if (currencyItem != null)
        {
            // 直接使用 ResourceConfigTable 的图标ID
            currencyItem.SetData(iconId, count);
            m_CurrencyItems.Add(currencyItem);

            Log.Info($"CurrencyUI: 创建货币项成功 - IconId={iconId}, Count={count}");
        }
        else
        {
            Log.Error("CurrencyUI: 货币项上未找到 CurrencyItem 组件");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// 清理货币项
    /// </summary>
    private void ClearCurrencyItems()
    {
        foreach (var item in m_CurrencyItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        m_CurrencyItems.Clear();
    }

    #endregion

    #region 生命周期

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearCurrencyItems();
        base.OnClose(isShutdown, userData);
    }

    #endregion
}
