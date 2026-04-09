using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 开始菜单UI
/// </summary>
public partial class StartMenuUI : UIFormBase
{
    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 设置背景图和通过资源ID加载
        varImgBackground.SetSpriteById(ResourceIds.MENU_BACKGROUND);
        varName.SetSpriteById(ResourceIds.MENU_NAME);
        varNameEn.SetSpriteById(ResourceIds.MENU_NAME_EN);
        var存档上云.image.SetSpriteById(ResourceIds.MENU_YUN);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        Log.Info("StartMenuUI 已打开");

        // 检查是否有存档，决定是否启用"继续游戏"按钮
        CheckSaveData();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        // ?? 如果需要手动清理按钮事件，可能会自动清理
    }

    #endregion

    #region 按钮点击事件（可以重写这个方法）

    /// <summary>
    /// 按钮点击事件统一处理器
    /// 可能会自动调用此方法，传入被点击的按钮
    /// </summary>
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        // 根据不同的按钮执行不同的逻辑
        if (btSelf == varBtnNewGame)
        {
            OnNewGameButtonClick();
        }
        else if (btSelf == varBtnContinue)
        {
            OnContinueButtonClick();
        }
        else if (btSelf == varBtnLoadSave)
        {
            OnLoadSaveButtonClick();
        }
        else if (btSelf == varBtnSettings)
        {
            OnSettingsButtonClick();
        }
        else if (btSelf == varBtnQuit)
        {
            OnQuitButtonClick();
        }
        else if (btSelf == var存档上云)
        {
            OnCloudArchive();
        }
    }

    #endregion

    #region 各个按钮逻辑

    /// <summary>
    /// 新游戏按钮点击
    /// </summary>
    private void OnNewGameButtonClick()
    {
        Log.Info("点击了新游戏按钮");
        // 打开新游戏界面
        GF.UI.OpenUIForm(UIViews.NewGameUI);
        // 关闭当前菜单界面
        GF.UI.CloseUIForm(this.UIForm);
    }

    /// <summary>
    /// 继续游戏按钮点击
    /// </summary>
    private void OnContinueButtonClick()
    {
        Log.Info("点击了继续游戏按钮");

        if (HasSaveData())
        {
            // 自动加载最新的存档或最近存档
            LoadLatestSave();
        }
        else
        {
            GF.UI.ShowToast("没有存档数据", UIExtension.ToastStyle.Red);
        }
    }

    /// <summary>
    /// 加载存档按钮点击
    /// </summary>
    private void OnLoadSaveButtonClick()
    {
        Log.Info("点击了加载存档按钮");

        // 打开存档列表界面
        GF.UI.OpenUIForm(UIViews.LoadGameUI);
    }

    /// <summary>
    /// 设置按钮点击
    /// </summary>
    private void OnSettingsButtonClick()
    {
        Log.Info("点击了设置按钮");

        // 打开设置对话框
        GF.UI.OpenUIForm(UIViews.SettingDialog);
    }

    /// <summary>
    /// 退出按钮点击
    /// </summary>
    private void OnQuitButtonClick()
    {
        Log.Info("点击了退出按钮");

        QuitGame();
    }
    private void OnCloudArchive()
    {
        Log.Info("点击了存档管理按钮");

        // 打开存档管理界面（暂为子界面）
        OpenSubUIForm(UIViews.CloudArchiveUI);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查存档数据
    /// </summary>
    private void CheckSaveData()
    {
        // 设置账号ID
        PlayerAccountDataManager.Instance.SetCurrentAccountId("000001");

        bool hasSave = HasSaveData();

        // 如果没有存档，禁用"继续游戏"按钮
        if (varBtnContinue != null)
        {
            varBtnContinue.interactable = hasSave;
        }

        // 如果没有存档，禁用"加载存档"按钮
        if (varBtnLoadSave != null)
        {
            varBtnLoadSave.interactable = hasSave;
        }

        Log.Info($"存档检查完成：是否有存档: {hasSave}");
    }

    /// <summary>
    /// 检查是否有存档
    /// </summary>
    private bool HasSaveData()
    {
        var saveInfos = PlayerAccountDataManager.Instance.GetAllSaveBriefInfos();
        return saveInfos != null && saveInfos.Count > 0;
    }

    /// <summary>
    /// 加载最新存档
    /// </summary>
    private void LoadLatestSave()
    {
        Log.Info("自动加载最新的存档");

        // 使用 PlayerAccountDataManager 来自动加载最近
        bool success = PlayerAccountDataManager.Instance.AutoLoadLastSave();

        if (success)
        {
            var currentSave = PlayerAccountDataManager.Instance.CurrentSaveData;
            if (currentSave != null)
            {
                GF.UI.ShowToast($"加载存档成功: {currentSave.SaveName}", UIExtension.ToastStyle.Green);

                // 进入游戏
                EnterGame();
            }
        }
        else
        {
            GF.UI.ShowToast("加载存档失败", UIExtension.ToastStyle.Red);
        }
    }

    /// <summary>
    /// 进入游戏
    /// </summary>
    private void EnterGame()
    {
        // 关闭当前菜单界面
        GF.UI.CloseUIForm(this.UIForm);

        // 使用 GameFlowManager 统一管理游戏流程
        GameFlowManager.EnterGame();
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void QuitGame()
    {
        // 使用 GameFlowManager 统一管理游戏流程
        GameFlowManager.QuitGame();
    }

    #endregion
}
