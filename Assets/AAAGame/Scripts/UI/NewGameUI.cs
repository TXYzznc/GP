using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using UnityEngine.Events;

/// <summary>
/// 新游戏UI - 角色创建流程
/// </summary>
public partial class NewGameUI : UIFormBase
{
    #region 流程状态枚举

    /// <summary>
    /// 创建角色流程状态
    /// </summary>
    private enum CreateCharacterState
    {
        InputName,      // 流程一：输入名字
        ShowStory,      // 流程二：显示游戏背景故事
        CreateCharacter // 流程三：创建角色
    }

    #endregion

    #region 私有字段

    private CreateCharacterState currentState;
    private string playerName = "";
    private TypewriterEffect storyTypewriter;

    // 配置：故事文本的多语言Key
    private const string STORY_TEXT_KEY = "Story_Welcome";

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        Log.Info("NewGameUI 初始化");

        // 初始隐藏提示按钮
        SetActive(varTips2?.gameObject, false);

        // 调试：检查 varName 是否为 null
        if (varName == null)
        {
            Log.Error("varName 为 null！请检查 Unity Inspector 中是否正确赋值");
        }
        else
        {
            Log.Info("varName 已正确赋值");

            // 监听输入框的变化
            varName.onValueChanged.AddListener(OnNameInputChanged);
            varName.onEndEdit.AddListener(OnNameInputEnd);
        }

        // 获取或添加打字机组件
        if (varStoryText != null)
        {
            storyTypewriter = varStoryText.GetComponent<TypewriterEffect>();
            if (storyTypewriter == null)
            {
                storyTypewriter = varStoryText.gameObject.AddComponent<TypewriterEffect>();
            }

            // 配置打字机效果
            storyTypewriter.SetTypeSpeed(0.05f);
            storyTypewriter.SetLocalizationKey(STORY_TEXT_KEY);

            // 监听打字机完成事件
            storyTypewriter.onTypingComplete.AddListener(OnStoryTypingComplete);
            storyTypewriter.onFadeOutComplete.AddListener(OnStoryFadeOutComplete);

            Log.Info($"打字机事件监听器已添加，监听器数量: {storyTypewriter.onTypingComplete.GetPersistentEventCount()}");
        }
        else
        {
            Log.Error("varStoryText 为 null！");
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        Log.Info("NewGameUI 已打开");

        // 开始流程一：输入名字
        StartFlow(CreateCharacterState.InputName);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        Log.Info("NewGameUI 已关闭");
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 监听空格键（流程一和流程二）
        if (currentState == CreateCharacterState.InputName || currentState == CreateCharacterState.ShowStory)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnTips2ButtonClick();
            }
        }
    }

    #endregion

    #region 按钮点击事件

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        if (btSelf == varContinueBtn)
        {
            OnContinueButtonClick();
        }
        else if (btSelf == varNextOccupation)
        {
            OnNextOccupationClick();
        }
        else if (btSelf == varLastOccupation)
        {
            OnLastOccupationClick();
        }
        else if (btSelf == varTips2)
        {
            OnTips2ButtonClick();
        }
    }

    /// <summary>
    /// 提示按钮点击（进入下一流程）
    /// </summary>
    private void OnTips2ButtonClick()
    {
        Log.Info($"Tips2 按钮被点击，当前流程: {currentState}");

        if (currentState == CreateCharacterState.InputName)
        {
            // 流程一：验证并进入流程二
            OnNameInputComplete();
        }
        else if (currentState == CreateCharacterState.ShowStory)
        {
            // 流程二：跳过打字机动画，直接进入流程三
            if (storyTypewriter != null && storyTypewriter.IsTyping())
            {
                // 如果正在打字，立即完成
                storyTypewriter.Complete();
            }
            else
            {
                // 如果已经打字完成，直接进入流程三
                StartFlow(CreateCharacterState.CreateCharacter);
            }
        }
    }

    /// <summary>
    /// 继续按钮点击（流程三完成）
    /// </summary>
    private void OnContinueButtonClick()
    {
        Log.Info("点击继续按钮，角色创建完成");
        OnCharacterCreationComplete();
    }

    /// <summary>
    /// 下一个职业按钮（暂未实现）
    /// </summary>
    private void OnNextOccupationClick()
    {
        Log.Info("切换到下一个职业");
        // TODO: 实现职业切换逻辑
    }

    /// <summary>
    /// 上一个职业按钮（暂未实现）
    /// </summary>
    private void OnLastOccupationClick()
    {
        Log.Info("切换到上一个职业");
        // TODO: 实现职业切换逻辑
    }

    #endregion

    #region 流程控制

    /// <summary>
    /// 开始指定流程
    /// </summary>
    private void StartFlow(CreateCharacterState state)
    {
        currentState = state;

        // 隐藏所有流程的UI容器
        HideAllFlows();

        // 显示当前流程的UI容器和初始化
        switch (state)
        {
            case CreateCharacterState.InputName:
                ShowNameInputFlow();
                break;
            case CreateCharacterState.ShowStory:
                ShowStoryFlow();
                break;
            case CreateCharacterState.CreateCharacter:
                ShowCharacterCreationFlow();
                break;
        }

        Log.Info($"进入流程: {state}");
    }

    /// <summary>
    /// 隐藏所有流程的UI容器
    /// </summary>
    private void HideAllFlows()
    {
        SetActive(varFirst, false);   // 流程一容器
        SetActive(varSecond, false);  // 流程二容器
        SetActive(varThird, false);   // 流程三容器
        SetActive(varTips2?.gameObject, false); // 隐藏提示按钮
    }

    /// <summary>
    /// 安全设置GameObject激活状态
    /// </summary>
    private void SetActive(GameObject go, bool active)
    {
        if (go != null)
        {
            go.SetActive(active);
        }
    }

    #endregion

    #region 流程一：输入名字

    /// <summary>
    /// 显示名字输入流程
    /// </summary>
    private void ShowNameInputFlow()
    {
        // 显示流程一的UI容器
        SetActive(varFirst, true);

        // 清空输入框并聚焦
        if (varName != null)
        {
            varName.text = "";
            varName.ActivateInputField();

            // 设置输入框的占位符文本（使用多语言）
            var placeholder = varName.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.text = GF.Localization.GetString("UI_InputName");
            }
        }

        // 初始隐藏提示按钮（等待输入内容后显示）
        SetActive(varTips2?.gameObject, false);

        // 更新提示按钮文本
        UpdateTips2ButtonText("UI_PressSpace");

        Log.Info("流程一：名字输入界面已显示");
    }

    /// <summary>
    /// 输入框内容变化回调
    /// </summary>
    private void OnNameInputChanged(string input)
    {
        Log.Info($"输入框内容变化: '{input}', Trim后: '{input.Trim()}', 长度: {input.Trim().Length}");

        // 当输入框有内容时，显示提示按钮
        if (!string.IsNullOrEmpty(input.Trim()))
        {
            Log.Info("输入框有内容，显示 Tips2 按钮");
            SetActive(varTips2?.gameObject, true);
        }
        else
        {
            Log.Info("输入框为空，隐藏 Tips2 按钮");
            SetActive(varTips2?.gameObject, false);
        }
    }

    /// <summary>
    /// 输入框结束编辑回调
    /// </summary>
    private void OnNameInputEnd(string input)
    {
        playerName = input.Trim();
        Log.Info($"输入框结束编辑，名字: {playerName}");
    }

    /// <summary>
    /// 名字输入完成（按下空格或点击按钮）
    /// </summary>
    private void OnNameInputComplete()
    {
        // 获取当前输入的名字
        if (varName != null)
        {
            playerName = varName.text.Trim();
        }

        // 验证名字是否有效
        if (string.IsNullOrEmpty(playerName))
        {
            string errorMsg = GF.Localization.GetString("UI_InputName");
            GF.UI.ShowToast(errorMsg, UIExtension.ToastStyle.Red);
            return;
        }

        Log.Info($"名字输入完成: {playerName}");

        // 进入流程二：显示故事
        StartFlow(CreateCharacterState.ShowStory);
    }

    #endregion

    #region 流程二：显示游戏背景故事

    /// <summary>
    /// 显示故事流程
    /// </summary>
    private void ShowStoryFlow()
    {
        // 显示流程二的UI容器
        SetActive(varSecond, true);

        // 初始隐藏提示按钮（等待打字完成后显示）
        SetActive(varTips2?.gameObject, false);

        // 更新提示按钮文本
        UpdateTips2ButtonText("UI_Continue");

        // 使用打字机组件播放（会自动从多语言系统获取文本）
        if (storyTypewriter != null)
        {
            storyTypewriter.Play();
        }
    }

    /// <summary>
    /// 故事打字完成回调
    /// </summary>
    private void OnStoryTypingComplete()
    {
        Log.Info("故事文本打字完成");

        // 打字完成后，显示提示按钮
        SetActive(varTips2?.gameObject, true);
    }

    /// <summary>
    /// 故事淡出完成回调
    /// </summary>
    private void OnStoryFadeOutComplete()
    {
        Log.Info("故事文本淡出完成");

        // 进入流程三：创建角色
        StartFlow(CreateCharacterState.CreateCharacter);
    }

    #endregion

    #region 流程三：创建角色

    /// <summary>
    /// 显示角色创建流程
    /// </summary>
    private void ShowCharacterCreationFlow()
    {
        // 显示流程三的UI容器
        SetActive(varThird, true);

        // 流程三不需要提示按钮
        SetActive(varTips2?.gameObject, false);

        // TODO: 初始化职业数据
        // LoadOccupationData(0);
    }

    /// <summary>
    /// 角色创建完成
    /// </summary>
    private void OnCharacterCreationComplete()
    {
        Log.Info($"=== 角色创建完成 ===");
        Log.Info($"角色名字: {playerName}");
        Log.Info($"职业: {varOccupation?.text ?? "未选择"}");
        Log.Info($"===================");

        // TODO: 保存角色数据
        // SaveCharacterData();

        // 显示欢迎提示（使用多语言，支持参数格式化）
        string welcomeMsg = string.Format(GF.Localization.GetString("UI_Welcome"), playerName);
        GF.UI.ShowToast(welcomeMsg, UIExtension.ToastStyle.Green);

        // 关闭当前界面（使用正确的方法）
        GF.UI.CloseUIForm(this.UIForm);

        // 或者使用带动画的关闭方式
        // CloseWithAnimation();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 更新提示按钮的文本
    /// </summary>
    private void UpdateTips2ButtonText(string localizationKey)
    {
        if (varTips2 != null)
        {
            var buttonText = varTips2.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = GF.Localization.GetString(localizationKey);
            }
        }
    }

    #endregion
}
