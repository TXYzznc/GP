using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 开始菜单 UI
/// </summary>
public partial class StartMenuUI : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        GF.Log("开始菜单打开");

        // 检查是否有存档，决定"继续"按钮是否可用
        varImgBackground.SetSpriteById(ResourceIds.BACKGROUND_MAIN);
        UpdateContinueButton();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理工作
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 更新"继续"按钮状态
    /// </summary>
    private void UpdateContinueButton()
    {
        // 检查是否有存档
        bool hasSaveData = CheckHasSaveData();

        if (varBtnContinue != null)
        {
            varBtnContinue.interactable = hasSaveData;
        }
    }

    /// <summary>
    /// 检查是否有存档数据
    /// </summary>
    private bool CheckHasSaveData()
    {
        // TODO: 实现存档检测逻辑
        // 例如：检查 PlayerPrefs 或文件系统
        // return GF.Setting.HasSetting("LastSaveTime");

        // 暂时返回 false
        return false;
    }

    #region 按钮点击事件

    /// <summary>
    /// 点击"新游戏"按钮
    /// </summary>
    private void OnClickNewGame()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.Log("点击：新游戏");

        // TODO: 开始新游戏
        // 1. 清除旧存档（可选）
        // 2. 初始化游戏数据
        // 3. 切换到游戏场景

        // 示例：显示确认对话框
        ShowConfirmDialog("开始新游戏", "确定要开始新游戏吗？", () =>
        {
            StartNewGame();
        });
    }

    /// <summary>
    /// 点击"继续"按钮
    /// </summary>
    private void OnClickContinue()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.Log("点击：继续游戏");

        // TODO: 加载最近的存档并继续游戏
        LoadLastSave();
    }

    /// <summary>
    /// 点击"读取存档"按钮
    /// </summary>
    private void OnClickLoadSave()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.Log("点击：读取存档");

        // TODO: 打开存档列表界面
        // GF.UI.OpenUIForm(UIViews.SaveLoadDialog);

        GF.UI.ShowToast("存档系统开发中...", UIExtension.ToastStyle.Blue);
    }

    /// <summary>
    /// 点击"设置"按钮
    /// </summary>
    private void OnClickSettings()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.Log("点击：设置");

        // 打开设置界面（如果你保留了 SettingDialog）
        // GF.UI.OpenUIForm(UIViews.SettingDialog);

        // 或者打开你自己的设置界面
        GF.UI.ShowToast("设置界面开发中...", UIExtension.ToastStyle.Blue);
    }

    /// <summary>
    /// 点击"退出"按钮
    /// </summary>
    private void OnClickQuit()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.Log("点击：退出游戏");

        // 显示确认对话框
        ShowConfirmDialog("退出游戏", "确定要退出游戏吗？", () =>
        {
            QuitGame();
        });
    }

    #endregion

    #region 游戏逻辑

    /// <summary>
    /// 开始新游戏
    /// </summary>
    private void StartNewGame()
    {
        GF.Log("开始新游戏");

        // TODO: 实现新游戏逻辑
        // 1. 清除旧数据
        ClearGameData();

        // 2. 初始化新游戏数据
        InitNewGameData();

        // 3. 切换到游戏场景
        // 方式 1: 直接切换流程（不换场景）
        // ChangeState<GamePlayProcedure>(procedureOwner);

        // 方式 2: 切换到游戏场景
        // procedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, "GamePlay");
        // ChangeState<ChangeSceneProcedure>(procedureOwner);

        GF.UI.ShowToast("新游戏开始！", UIExtension.ToastStyle.Green);
    }

    /// <summary>
    /// 加载最近的存档
    /// </summary>
    private void LoadLastSave()
    {
        GF.Log("加载最近存档");

        // TODO: 实现加载存档逻辑
        // 1. 从 Setting 或文件读取存档数据
        // 2. 恢复游戏状态
        // 3. 切换到游戏场景

        GF.UI.ShowToast("加载存档中...", UIExtension.ToastStyle.Blue);
    }

    /// <summary>
    /// 清除游戏数据
    /// </summary>
    private void ClearGameData()
    {
        // 清除 PlayerPrefs 或其他存储的数据
        // GF.Setting.RemoveSetting("PlayerLevel");
        // GF.Setting.RemoveSetting("PlayerExp");
        // GF.Setting.Save();
    }

    /// <summary>
    /// 初始化新游戏数据
    /// </summary>
    private void InitNewGameData()
    {
        // 初始化玩家数据
        // GF.Setting.SetInt("PlayerLevel", 1);
        // GF.Setting.SetInt("PlayerExp", 0);
        // GF.Setting.SetInt("PlayerGold", 0);
        // GF.Setting.Save();
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void QuitGame()
    {
        GF.Log("退出游戏");

        // 保存设置
        GF.Setting.Save();

        // 退出应用
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    private void ShowConfirmDialog(string title, string message, System.Action onConfirm)
    {
        // TODO: 使用框架的对话框系统
        // 如果你保留了 CommonDialog，可以这样用：
        // var dialogParams = UIParams.Create();
        // dialogParams.Set<VarString>("Title", title);
        // dialogParams.Set<VarString>("Message", message);
        // dialogParams.Set<VarAction>("OnConfirm", onConfirm);
        // GF.UI.OpenUIForm(UIViews.CommonDialog, dialogParams);

        // 临时方案：直接执行
        onConfirm?.Invoke();
    }

    #endregion
}
