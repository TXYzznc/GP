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

        // 加载背景图（通过配置ID）
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

        // ⚠️ 不需要手动解绑按钮事件！框架会自动处理
    }

    #endregion

    #region 按钮点击事件处理（重写基类方法）

    /// <summary>
    /// 按钮点击事件统一处理入口
    /// 框架会自动调用此方法，传入被点击的按钮
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

    #region 具体按钮逻辑

    /// <summary>
    /// 新游戏按钮点击
    /// </summary>
    private void OnNewGameButtonClick()
    {
        Log.Info("点击了新游戏按钮");

        GF.UI.OpenUIForm(UIViews.NewGameUI);

    }

    /// <summary>
    /// 继续游戏按钮点击
    /// </summary>
    private void OnContinueButtonClick()
    {
        Log.Info("点击了继续游戏按钮");

        if (HasSaveData())
        {
            // 加载最新的存档
            LoadLatestSave();
        }
        else
        {
            GF.UI.ShowToast(GF.Localization.GetString("NoSaveData"), UIExtension.ToastStyle.Red);
        }
    }

    /// <summary>
    /// 加载存档按钮点击
    /// </summary>
    private void OnLoadSaveButtonClick()
    {
        Log.Info("点击了加载存档按钮");

        // 打开存档列表界面
        // GF.UI.OpenUIForm(UIViews.SaveLoadUI);

        GF.UI.ShowToast(GF.Localization.GetString("OpenSaveList"));
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
        Log.Info("点击了存档上云按钮");
        
        // 打开云存档弹窗（作为子界面）
        OpenSubUIForm(UIViews.CloudArchiveUI);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查存档数据
    /// </summary>
    private void CheckSaveData()
    {
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
    }

    /// <summary>
    /// 检查是否有存档
    /// </summary>
    private bool HasSaveData()
    {
        // TODO: 实现实际的存档检查逻辑
        // return GF.Setting.HasSetting("LatestSave");
        return false; // 临时返回 false
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    private void StartNewGame()
    {
        Log.Info("开始新游戏!!!!");

        // 清除旧存档
        // GF.Setting.RemoveSetting("LatestSave");

        // 加载游戏场景
        // GF.Scene.LoadScene("GameScene");

        // 或者打开角色选择界面
        // GF.UI.OpenUIForm(UIViews.CharacterSelectUI);

        GF.UI.ShowToast("开始新游戏!!!!!!", UIExtension.ToastStyle.Green);
    }

    /// <summary>
    /// 加载最新存档
    /// </summary>
    private void LoadLatestSave()
    {
        Log.Info("加载最新存档");

        // TODO: 实现加载存档逻辑
        // var saveData = GF.Setting.GetString("LatestSave");
        // LoadGameFromSave(saveData);

        GF.UI.ShowToast("加载存档中...", UIExtension.ToastStyle.Blue);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void QuitGame()
    {
        Log.Info("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
