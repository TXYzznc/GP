using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using UnityEngine.Events;

/// <summary>
/// 加载存档UI
/// </summary>
public partial class LoadGameUI : UIFormBase
{
    private List<SaveBriefInfo> m_SaveInfos = new List<SaveBriefInfo>();
    private string m_SelectedSaveId = null;

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Log.Info("LoadGameUI 初始化");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Log.Info("LoadGameUI 已打开");

        // 设置账号ID
        PlayerAccountDataManager.Instance.SetCurrentAccountId("000001");

        // 刷新存档列表
        RefreshSaveList();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        Log.Info("LoadGameUI 已关闭");

        // 清理
        m_SelectedSaveId = null;
    }

    #endregion

    #region 按钮点击事件

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        if (btSelf == varEnter)
        {
            OnEnterButtonClick();
        }
        else if (btSelf == varClose)
        {
            OnCloseButtonClick();
        }
    }

    /// <summary>
    /// 确认按钮点击
    /// </summary>
    private void OnEnterButtonClick()
    {
        if (string.IsNullOrEmpty(m_SelectedSaveId))
        {
            GF.UI.ShowToast("请先选择一个存档", UIExtension.ToastStyle.Red);
            return;
        }

        Log.Info($"确认加载存档: {m_SelectedSaveId}");

        // 加载选中的存档
        var saveData = PlayerAccountDataManager.Instance.LoadSave(m_SelectedSaveId);

        if (saveData != null)
        {
            GF.UI.ShowToast($"加载存档成功: {saveData.SaveName}", UIExtension.ToastStyle.Green);

            // 关闭当前界面
            GF.UI.CloseUIForm(this.UIForm);

            // 进入游戏
            EnterGame();
        }
        else
        {
            GF.UI.ShowToast("加载存档失败", UIExtension.ToastStyle.Red);
        }
    }

    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseButtonClick()
    {
        Log.Info("关闭存档列表界面");
        GF.UI.CloseUIForm(this.UIForm);
    }

    #endregion

    #region 存档列表管理

    /// <summary>
    /// 刷新存档列表
    /// </summary>
    private void RefreshSaveList()
    {
        // 获取所有存档信息（这里使用测试数据）
        m_SaveInfos = PlayerAccountDataManager.Instance.GetAllSaveBriefInfos();

        Log.Info($"找到了 {m_SaveInfos.Count} 个存档");

        // 生成存档项UI（使用 SpawnItem 方法）
        foreach (var saveInfo in m_SaveInfos)
        {
            var item = this.SpawnItem<UIItemObject>(varGameItem, varGameContent.content);
            (item.itemLogic as GameItem).Init(saveInfo, OnSaveSelected);
        }

        // 如果没有存档，显示提示
        if (m_SaveInfos.Count == 0)
        {
            Log.Warning("没有找到任何存档");
        }
    }

    /// <summary>
    /// 存档项选中回调
    /// </summary>
    private void OnSaveSelected(string saveId)
    {
        m_SelectedSaveId = saveId;
        Log.Info($"当前选中存档: {saveId}");

        // TODO: 可以在这里添加视觉反馈，高亮选中的存档项
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 进入游戏
    /// </summary>
    private void EnterGame()
    {
        // 使用 GameFlowManager 统一管理游戏流程
        GameFlowManager.EnterGame();
    }

    #endregion
}
