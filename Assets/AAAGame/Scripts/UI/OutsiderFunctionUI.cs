using System.Collections.Generic;
using DG.Tweening;
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
    private readonly string[] m_FunctionNames = new string[] { "角色", "图鉴", "仓库", "出战预设" };

    // 防抖机制：记录正在打开的UI（防止重复点击）
    private UIViews? m_OpeningUI = null;

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        DebugEx.Log("OutsiderFunctionUI", "订阅局外状态事件");
        GF.Event.Subscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Subscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    protected override void UnsubscribeEvents()
    {
        DebugEx.Log("OutsiderFunctionUI", "取消订阅局外状态事件");
        GF.Event.Unsubscribe(OutOfGameEnterEventArgs.EventId, OnOutOfGameEnter);
        GF.Event.Unsubscribe(OutOfGameLeaveEventArgs.EventId, OnOutOfGameLeave);
    }

    #endregion

    #region 事件处理

    private void OnOutOfGameEnter(object sender, GameEventArgs e)
    {
        DebugEx.Log("OutsiderFunctionUI", "收到局外进入事件");
        ShowUI();
        RefreshFunctions();
    }

    private void OnOutOfGameLeave(object sender, GameEventArgs e)
    {
        DebugEx.Log("OutsiderFunctionUI", "收到局外离开事件");
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

        DebugEx.Log("OutsiderFunctionUI", "功能按钮已刷新");
    }

    /// <summary>
    /// 创建功能项
    /// </summary>
    private void CreateFunctionItem(string functionName, int index)
    {
        if (varFunctionItem == null || varOutsiderFunctionPanel == null)
        {
            DebugEx.Warning("OutsiderFunctionUI", "功能项模板或面板未设置");
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
            DebugEx.Error("OutsiderFunctionUI", "功能项上未找到 FunctionItem 组件");
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
        DebugEx.Log("OutsiderFunctionUI", $"点击了功能按钮 - {functionName}");

        switch (functionName)
        {
            case "角色":
                OpenUIFormSafe(UIViews.CharacterBagUI);
                break;
            case "图鉴":
                OpenUIFormSafe(UIViews.DictionariesUI);
                break;
            case "仓库":
                OpenUIFormSafe(UIViews.WarehouseUI);
                break;
            case "出战预设":
                OpenUIFormSafe(UIViews.BattlePresetUI);
                break;
        }
    }

    /// <summary>
    /// 安全打开UI（避免重复打开）
    /// </summary>
    private void OpenUIFormSafe(UIViews uiView)
    {
        DebugEx.Log("OutsiderFunctionUI", $"[OpenUIFormSafe] 开始检查 UI {uiView}");

        // 防抖检查：如果正在打开同一个UI，忽略
        if (m_OpeningUI == uiView)
        {
            DebugEx.Warning("OutsiderFunctionUI", $"UI {uiView} 正在打开中，忽略重复请求");
            return;
        }

        // 检查UI是否已经打开（防止重复打开导致serial id冲突）
        if (GF.UI.HasUIForm(uiView))
        {
            DebugEx.Warning("OutsiderFunctionUI", $"UI {uiView} 已经打开，忽略重复打开请求");
            return;
        }

        // 检查UI是否正在加载中
        if (GF.UI.IsLoadingUIForm(uiView))
        {
            DebugEx.Warning("OutsiderFunctionUI", $"UI {uiView} 正在加载中，忽略重复打开请求");
            return;
        }

        // 标记正在打开
        m_OpeningUI = uiView;
        DebugEx.Log("OutsiderFunctionUI", $"[OpenUIFormSafe] 开始打开UI {uiView}");

        // 打开UI
        int formId = GF.UI.OpenUIForm(uiView);
        DebugEx.Log(
            "OutsiderFunctionUI",
            $"[OpenUIFormSafe] UI {uiView} 已调用OpenUIForm，返回formId={formId}"
        );

        // ⭐ 延迟清除标记（等待UI完全打开）
        // ⚠️ 注意：这个延迟回调可能在UI关闭后才执行，导致访问已关闭的UIForm
        DOVirtual.DelayedCall(
            0.5f,
            () =>
            {
                DebugEx.Log(
                    "OutsiderFunctionUI",
                    $"[DOTween延迟回调] 0.5秒后执行，当前m_OpeningUI={m_OpeningUI}, 目标uiView={uiView}"
                );

                if (m_OpeningUI == uiView)
                {
                    m_OpeningUI = null;
                    DebugEx.Log(
                        "OutsiderFunctionUI",
                        $"[DOTween延迟回调] UI {uiView} 打开完成，清除防抖标记"
                    );
                }
                else
                {
                    DebugEx.Warning(
                        "OutsiderFunctionUI",
                        $"[DOTween延迟回调] m_OpeningUI已变化，不清除标记"
                    );
                }
            }
        );
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
