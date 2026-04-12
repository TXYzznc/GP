using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class OutsiderFunctionUI : StateAwareUIForm
{
    #region 字段

    private List<FunctionItem> m_FunctionItems = new List<FunctionItem>();

    // 功能按钮名称（商店和挑战暂时隐藏）
    private readonly string[] m_FunctionNames = new string[] { "图鉴", "仓库", "出战预设" };

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        DebugEx.LogModule("OutsiderFunctionUI", "订阅局外状态事件");
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    protected override void UnsubscribeEvents()
    {
        DebugEx.LogModule("OutsiderFunctionUI", "取消订阅局外状态事件");
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    #endregion

    #region 事件处理

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("OutsiderFunctionUI", "收到局外进入事件");
        ShowUI();
        RefreshFunctions();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("OutsiderFunctionUI", "收到局外离开事件");
        HideUI();
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新功能按钮
    /// </summary>
    private void RefreshFunctions()
    {
        // 清除旧的功能项
        ClearFunctionItems();

        // 创建功能按钮
        for (int i = 0; i < m_FunctionNames.Length; i++)
        {
            CreateFunctionItem(m_FunctionNames[i], i);
        }

        DebugEx.LogModule("OutsiderFunctionUI", "功能按钮已刷新");
    }

    /// <summary>
    /// 创建功能项
    /// </summary>
    private void CreateFunctionItem(string functionName, int index)
    {
        if (varFunctionItem == null || varOutsiderFunctionPanel == null)
        {
            DebugEx.WarningModule("OutsiderFunctionUI", "功能项模板或面板未设置");
            return;
        }

        // 实例化功能项
        GameObject itemObj = Instantiate(varFunctionItem, varOutsiderFunctionPanel.transform);
        itemObj.SetActive(true);

        // 获取 FunctionItem 组件
        FunctionItem functionItem = itemObj.GetComponent<FunctionItem>();
        if (functionItem != null)
        {
            functionItem.SetData(functionName, () => OnFunctionClicked(functionName));
            m_FunctionItems.Add(functionItem);
        }
        else
        {
            DebugEx.ErrorModule("OutsiderFunctionUI", "功能项上未找到 FunctionItem 组件");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// 清除功能项
    /// </summary>
    private void ClearFunctionItems()
    {
        foreach (var item in m_FunctionItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        m_FunctionItems.Clear();
    }

    #endregion

    #region 功能按钮回调

    /// <summary>
    /// 功能按钮点击回调
    /// </summary>
    private void OnFunctionClicked(string functionName)
    {
        DebugEx.LogModule("OutsiderFunctionUI", $"点击了功能按钮 - {functionName}");

        switch (functionName)
        {
            case "图鉴":
                GF.UI.OpenUIForm(UIViews.DictionariesUI);
                break;
            case "仓库":
                GF.UI.OpenUIForm(UIViews.WarehouseUI);
                break;
            case "出战预设":
                GF.UI.OpenUIForm(UIViews.BattlePresetUI);
                break;
        }
    }

    #endregion

    #region 生命周期

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearFunctionItems();
        base.OnClose(isShutdown, userData);
    }

    #endregion
}
