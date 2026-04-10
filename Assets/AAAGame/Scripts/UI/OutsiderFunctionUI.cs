using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework.Event;
using System.Collections.Generic;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class OutsiderFunctionUI : StateAwareUIForm
{
    #region 字段

    private List<FunctionItem> m_FunctionItems = new List<FunctionItem>();

    // 功能按钮名称
    private readonly string[] m_FunctionNames = new string[]
    {
        "图鉴",
        "商店",
        "仓库",
        "出战预设",
        "挑战"
    };

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        Log.Info("OutsiderFunctionUI: 订阅局外状态事件");
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("OutsiderFunctionUI: 取消订阅局外状态事件");
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    #endregion

    #region 事件处理

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        Log.Info("OutsiderFunctionUI: 收到局外进入事件");
        ShowUI();
        RefreshFunctions();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        Log.Info("OutsiderFunctionUI: 收到局外离开事件");
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

        Log.Info("OutsiderFunctionUI: 功能按钮已刷新");
    }

    /// <summary>
    /// 创建功能项
    /// </summary>
    private void CreateFunctionItem(string functionName, int index)
    {
        if (varFunctionItem == null || varOutsiderFunctionPanel == null)
        {
            Log.Warning("OutsiderFunctionUI: 功能项模板或面板未设置");
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
            Log.Error("OutsiderFunctionUI: 功能项上未找到 FunctionItem 组件");
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
        Log.Info($"OutsiderFunctionUI: 点击了功能按钮 - {functionName}");

        // TODO: 根据功能名称打开对应的UI
        switch (functionName)
        {
            case "图鉴":
                GF.UI.OpenUIForm(UIViews.DictionariesUI);
                break;
            case "商店":
                // 打开商店UI
                break;
            case "仓库":
                // 打开仓库UI
                break;
            case "召唤师":
                // 打开召唤师UI
                break;
            case "挑战":
                // 打开挑战UI
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
