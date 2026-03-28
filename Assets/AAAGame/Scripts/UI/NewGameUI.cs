using System.Collections;
using System.Collections.Generic;
using GameExtension;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 新游戏UI - 角色创建界面
/// </summary>
public partial class NewGameUI : UIFormBase
{
    #region 创建角色流程状态枚举

    /// <summary>
    /// 创建角色流程状态
    /// </summary>
    private enum CreateCharacterState
    {
        InputName, // 第一步：输入名字
        ShowStory, // 第二步：显示游戏背景故事
        CreateCharacter, // 第三步：创建角色
    }

    #endregion

    #region 私有字段

    private CreateCharacterState currentState;
    private string playerName = "";
    private TypewriterEffect storyTypewriter;

    // 配置：故事文本的多语言Key
    private const string STORY_TEXT_KEY = "Story_Welcome";

    // 召唤师选择相关
    private List<SummonerTable> m_AvailableSummoners = new List<SummonerTable>();
    private int m_CurrentSummonerIndex = 0;

    // 模型显示相关 - 使用 UIModelViewer 组件
    private UIModelViewer m_ModelViewer = null;

    // 技能提示相关
    private int m_HoveredSkillIndex = -1;
    private int m_CurrentFloatingTipId = -1; // 当前显示的浮动提示框ID

    // 技能按钮数组（8个技能：2个固定 + 3个路线一 + 3个路线二）
    private Button[] m_SkillButtons = null;
    private List<int> m_CurrentSkillIds = new List<int>(); // 当前召唤师的技能ID列表
    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        DebugEx.Log("NewGameUI", "初始化开始");

        // 初始化隐藏提示按钮
        SetActive(varTips2?.gameObject, false);

        // 初始化技能按钮数组
        InitializeSkillButtons();

        // 测试：检查 varName 是否为 null
        if (varName == null)
        {
            DebugEx.Error("NewGameUI", "varName 为 null，请在 Unity Inspector 中是否正确赋值");
        }
        else
        {
            DebugEx.Log("NewGameUI", "varName 已正确赋值");

            // 监听输入文本变化
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

            DebugEx.Log(
                "NewGameUI",
                $"打字机事件监听器已添加，监听器数量: {storyTypewriter.onTypingComplete.GetPersistentEventCount()}"
            );
        }
        else
        {
            DebugEx.Error("NewGameUI", "varStoryText 为 null！");
        }

        // 初始化模型查看器
        InitializeModelViewer();
    }

    /// <summary>
    /// 初始化模型查看器
    /// </summary>
    private void InitializeModelViewer()
    {
        if (varOccupationImage == null)
        {
            Log.Error("varOccupationImage 为 null，无法初始化模型查看器");
            return;
        }

        // 获取或添加 RawImage 组件
        RawImage rawImage = varOccupationImage.GetComponent<RawImage>();
        if (rawImage == null)
        {
            // 如果是 Image 组件，需要替换为 RawImage
            Image image = varOccupationImage.GetComponent<Image>();
            if (image != null)
            {
                // 保存原始属性
                var rectTransform = image.rectTransform;

                // 删除 Image 组件，添加 RawImage
                Destroy(image);
                rawImage = varOccupationImage.AddComponent<RawImage>();

                Log.Info("已将 Image 组件替换为 RawImage 组件");
            }
            else
            {
                rawImage = varOccupationImage.AddComponent<RawImage>();
            }
        }

        // 获取或初始化 UIModelViewer
        m_ModelViewer = varOccupationImage.GetComponent<UIModelViewer>();
        if (m_ModelViewer == null)
        {
            m_ModelViewer = varOccupationImage.AddComponent<UIModelViewer>();
        }

        m_ModelViewer.Initialize(rawImage);

        // 设置双击事件
        m_ModelViewer.OnDoubleClick = OnModelDoubleClick;

        Log.Info("UIModelViewer 初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        Log.Info("NewGameUI 已打开");

        // 新的存档系统不需要存档槽，每次创建都是新的存档
        // 确保账号信息已加载
        PlayerAccountDataManager.Instance.SetCurrentAccountId("000001");

        // 加载可选召唤师列表
        LoadAvailableSummoners();

        // 开始第一步：输入名字
        StartFlow(CreateCharacterState.InputName);
    }

    /// <summary>
    /// 加载可选召唤师列表
    /// </summary>
    private void LoadAvailableSummoners()
    {
        m_AvailableSummoners.Clear();

        var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
        if (summonerTable == null)
        {
            Log.Error("SummonerTable 未加载");
            return;
        }

        // 获取所有阶段1的召唤师（初始可选）
        foreach (var summoner in summonerTable.GetAllDataRows())
        {
            if (summoner.Phase == 1)
            {
                m_AvailableSummoners.Add(summoner);
            }
        }

        Log.Info($"加载了 {m_AvailableSummoners.Count} 个可选召唤师");

        // 默认选择第一个
        m_CurrentSummonerIndex = 0;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理技能提示框
        HideSkillTooltip();

        // 清理模型查看器
        if (m_ModelViewer != null)
        {
            m_ModelViewer.ClearModel();
        }

        base.OnClose(isShutdown, userData);

        Log.Info("NewGameUI 已关闭");
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 监听空格键，跳过第一和第二阶段
        if (
            currentState == CreateCharacterState.InputName
            || currentState == CreateCharacterState.ShowStory
        )
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnTips2ButtonClick();
            }
        }

        // 在角色创建阶段，处理模型交互和技能悬停
        if (currentState == CreateCharacterState.CreateCharacter)
        {
            // 使用 UIModelViewer 处理模型交互
            if (m_ModelViewer != null)
            {
                m_ModelViewer.HandleInteraction();
            }
            HandleSkillHover();
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
    /// 提示按钮点击（进入下一阶段）
    /// </summary>
    private void OnTips2ButtonClick()
    {
        Log.Info($"Tips2 按钮点击，当前阶段: {currentState}");

        if (currentState == CreateCharacterState.InputName)
        {
            // 第一步：验证名字，进入第二阶段
            OnNameInputComplete();
        }
        else if (currentState == CreateCharacterState.ShowStory)
        {
            // 第二阶段：如果正在打字机播放，则跳过；否则进入第三阶段
            if (storyTypewriter != null && storyTypewriter.IsTyping())
            {
                // 如果正在打字，跳过完成
                storyTypewriter.Complete();
            }
            else
            {
                // 如果已经播放完成，直接进入第三阶段
                StartFlow(CreateCharacterState.CreateCharacter);
            }
        }
    }

    /// <summary>
    /// 继续按钮点击（角色创建完成）
    /// </summary>
    private void OnContinueButtonClick()
    {
        Log.Info("点击了继续按钮，角色创建完成");
        OnCharacterCreationComplete();
    }

    /// <summary>
    /// 下一个职业按钮
    /// </summary>
    private void OnNextOccupationClick()
    {
        if (m_AvailableSummoners.Count == 0)
            return;

        m_CurrentSummonerIndex++;
        if (m_CurrentSummonerIndex >= m_AvailableSummoners.Count)
        {
            m_CurrentSummonerIndex = 0;
        }

        UpdateSummonerDisplay();
        Log.Info($"切换到下一个职业: {GetCurrentSummoner()?.Name}");
    }

    /// <summary>
    /// 上一个职业按钮
    /// </summary>
    private void OnLastOccupationClick()
    {
        if (m_AvailableSummoners.Count == 0)
            return;

        m_CurrentSummonerIndex--;
        if (m_CurrentSummonerIndex < 0)
        {
            m_CurrentSummonerIndex = m_AvailableSummoners.Count - 1;
        }

        UpdateSummonerDisplay();
        Log.Info($"切换到上一个职业: {GetCurrentSummoner()?.Name}");
    }

    /// <summary>
    /// 获取当前选中的召唤师
    /// </summary>
    private SummonerTable GetCurrentSummoner()
    {
        if (
            m_AvailableSummoners.Count == 0
            || m_CurrentSummonerIndex < 0
            || m_CurrentSummonerIndex >= m_AvailableSummoners.Count
        )
        {
            return null;
        }
        return m_AvailableSummoners[m_CurrentSummonerIndex];
    }

    /// <summary>
    /// 更新召唤师显示
    /// </summary>
    private void UpdateSummonerDisplay()
    {
        var summoner = GetCurrentSummoner();
        if (summoner == null)
            return;

        // 更新职业名称
        if (varOccupation != null)
        {
            varOccupation.text = summoner.Name;
        }

        // 更新职业描述
        if (varDes != null)
        {
            varDes.text = summoner.Description;
        }

        // 更新技能显示
        UpdateSkillDisplay(summoner);

        // 加载并显示召唤师模型
        LoadSummonerModel(summoner);
    }

    /// <summary>
    /// 更新技能显示
    /// </summary>
    private void UpdateSkillDisplay(SummonerTable summoner)
    {
        // 获取技能配置表
        var skillTable = GF.DataTable.GetDataTable<SummonerSkillTable>();
        if (skillTable == null)
            return;

        // 使用ShowSkills字段获取所有要显示的技能
        m_CurrentSkillIds.Clear();
        if (summoner.ShowSkills != null && summoner.ShowSkills.Length > 0)
        {
            m_CurrentSkillIds.AddRange(summoner.ShowSkills);
            DebugEx.Log("NewGameUI", $"使用ShowSkills字段，总技能数: {m_CurrentSkillIds.Count}");
        }
        else
        {
            // 如果ShowSkills为空，则使用被动技能和主动技能ID作为备选
            if (summoner.PassiveSkillIds != null)
            {
                m_CurrentSkillIds.AddRange(summoner.PassiveSkillIds);
            }
            if (summoner.ActiveSkillIds != null)
            {
                m_CurrentSkillIds.AddRange(summoner.ActiveSkillIds);
            }
            DebugEx.Warning(
                "NewGameUI",
                $"ShowSkills为空，使用PassiveSkillIds和ActiveSkillIds，总技能数: {m_CurrentSkillIds.Count}"
            );
        }

        DebugEx.Log("NewGameUI", $"更新技能显示，总技能数: {m_CurrentSkillIds.Count}");

        // 更新技能名称文本显示
        if (varSkillNameArr != null)
        {
            for (int i = 0; i < varSkillNameArr.Length; i++)
            {
                if (i < m_CurrentSkillIds.Count)
                {
                    var skill = skillTable.GetDataRow(m_CurrentSkillIds[i]);
                    if (skill != null && varSkillNameArr[i] != null)
                    {
                        varSkillNameArr[i].text = skill.Name;
                        varSkillNameArr[i].gameObject.SetActive(true);
                    }
                }
                else if (varSkillNameArr[i] != null)
                {
                    varSkillNameArr[i].gameObject.SetActive(false);
                }
            }
        }

        // 更新技能图标显示
        if (m_SkillButtons != null)
        {
            for (int i = 0; i < m_SkillButtons.Length; i++)
            {
                if (m_SkillButtons[i] != null)
                {
                    if (i < m_CurrentSkillIds.Count)
                    {
                        var skill = skillTable.GetDataRow(m_CurrentSkillIds[i]);
                        if (skill != null)
                        {
                            // 加载技能图标到按钮
                            LoadSkillIcon(m_SkillButtons[i], skill.IconId, i);
                            m_SkillButtons[i].gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        m_SkillButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 加载技能图标到按钮
    /// </summary>
    private void LoadSkillIcon(Button skillButton, int iconId, int skillIndex)
    {
        if (skillButton == null)
        {
            DebugEx.Warning("NewGameUI", $"技能按钮为空: 按钮索引={skillIndex}");
            return;
        }

        var image = skillButton.GetComponent<Image>();
        if (image == null)
        {
            DebugEx.Error("NewGameUI", $"技能按钮 {skillIndex} 没有 Image 组件");
            return;
        }

        try
        {
            DebugEx.Log("NewGameUI", $"开始加载技能图标: ID={iconId}, 按钮索引={skillIndex}");

            // 使用 ResourceExtension 异步加载图标（fire-and-forget）
            _ = ResourceExtension.LoadSpriteAsync(iconId, image);

            DebugEx.Log("NewGameUI", $"技能图标加载请求已发送: ID={iconId}, 按钮索引={skillIndex}");
        }
        catch (System.Exception ex)
        {
            DebugEx.Error(
                "NewGameUI",
                $"技能图标加载异常: ID={iconId}, 按钮索引={skillIndex}, 错误={ex.Message}"
            );
        }
    }

    #endregion

    #region 流程控制

    /// <summary>
    /// 开始指定阶段
    /// </summary>
    private void StartFlow(CreateCharacterState state)
    {
        currentState = state;

        // 隐藏所有阶段的UI组件
        HideAllFlows();

        // 显示当前阶段的UI组件并初始化
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

        Log.Info($"进入阶段: {state}");
    }

    /// <summary>
    /// 隐藏所有阶段的UI组件
    /// </summary>
    private void HideAllFlows()
    {
        SetActive(varFirst, false); // 第一步界面
        SetActive(varSecond, false); // 第二阶段界面
        SetActive(varThird, false); // 第三步界面
        SetActive(varTips2?.gameObject, false); // 提示按钮
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

    #region 第一步：输入名字

    /// <summary>
    /// 显示输入名字界面
    /// </summary>
    private void ShowNameInputFlow()
    {
        // 显示第一步UI组件
        SetActive(varFirst, true);

        // 清空输入框并聚焦
        if (varName != null)
        {
            varName.text = "";
            varName.ActivateInputField();

            // 设置输入框占位符文本（使用多语言）
            var placeholder = varName.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.text = GF.Localization.GetString("UI_InputName");
            }
        }

        // 初始隐藏提示按钮，等待输入内容后显示
        SetActive(varTips2?.gameObject, false);

        // 更新提示按钮文本
        UpdateTips2ButtonText("UI_PressSpace");

        Log.Info("第一步：输入名字界面已显示");
    }

    /// <summary>
    /// 输入框内容变化回调
    /// </summary>
    private void OnNameInputChanged(string input)
    {
        Log.Info(
            $"输入框内容变化: '{input}', Trim后: '{input.Trim()}', 长度: {input.Trim().Length}"
        );

        // 如果输入了内容，显示提示按钮
        if (!string.IsNullOrEmpty(input.Trim()))
        {
            Log.Info("输入了内容，显示 Tips2 按钮");
            SetActive(varTips2?.gameObject, true);
        }
        else
        {
            Log.Info("输入为空，隐藏 Tips2 按钮");
            SetActive(varTips2?.gameObject, false);
        }
    }

    /// <summary>
    /// 输入框编辑回调
    /// </summary>
    private void OnNameInputEnd(string input)
    {
        playerName = input.Trim();
        Log.Info($"输入框编辑结束，名字: {playerName}");
    }

    /// <summary>
    /// 名字输入完成（点击空格或按钮）
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

        // 进入第二阶段：显示故事
        StartFlow(CreateCharacterState.ShowStory);
    }

    #endregion

    #region 第二阶段：显示游戏背景故事

    /// <summary>
    /// 显示故事界面
    /// </summary>
    private void ShowStoryFlow()
    {
        // 显示第二阶段UI组件
        SetActive(varSecond, true);

        // 初始隐藏提示按钮，等待打字完成后显示
        SetActive(varTips2?.gameObject, false);

        // 更新提示按钮文本
        UpdateTips2ButtonText("UI_Continue");

        // 使用打字机播放故事，自动从多语言系统获取文本
        if (storyTypewriter != null)
        {
            storyTypewriter.Play();
        }
    }

    /// <summary>
    /// 打字完成回调
    /// </summary>
    private void OnStoryTypingComplete()
    {
        Log.Info("故事文本打字完成");

        // 打字完成后显示提示按钮
        SetActive(varTips2?.gameObject, true);
    }

    /// <summary>
    /// 淡出完成回调
    /// </summary>
    private void OnStoryFadeOutComplete()
    {
        Log.Info("故事文本淡出完成");

        // 淡出完成后，进入角色创建
        StartFlow(CreateCharacterState.CreateCharacter);
    }

    #endregion

    #region 第三步：创建角色

    /// <summary>
    /// 显示角色创建界面
    /// </summary>
    private void ShowCharacterCreationFlow()
    {
        // 显示第三步UI组件
        SetActive(varThird, true);

        // 第三步不需要提示按钮
        SetActive(varTips2?.gameObject, false);

        // 初始化职业显示
        if (m_AvailableSummoners.Count > 0)
        {
            m_CurrentSummonerIndex = 0;
            UpdateSummonerDisplay();
        }
        else
        {
            Log.Warning("没有可选的召唤师");
        }
    }

    /// <summary>
    /// 角色创建完成
    /// </summary>
    private void OnCharacterCreationComplete()
    {
        var selectedSummoner = GetCurrentSummoner();
        if (selectedSummoner == null)
        {
            GF.UI.ShowToast("请选择一个职业", UIExtension.ToastStyle.Red);
            return;
        }

        Log.Info($"=== 角色创建完成 ===");
        Log.Info($"角色名字: {playerName}");
        Log.Info($"职业: {selectedSummoner.Name}");
        Log.Info($"===================");

        // 创建新存档（使用新的API）
        var saveData = PlayerAccountDataManager.Instance.CreateNewSave(
            playerName, // 存档名称使用玩家名字
            selectedSummoner.Id
        );

        if (saveData == null)
        {
            GF.UI.ShowToast("创建角色失败", UIExtension.ToastStyle.Red);
            return;
        }

        // 设置初始位置（从 PosTable 获取）
        var posTable = GF.DataTable.GetDataTable<PosTable>();
        if (posTable != null)
        {
            var initialPosData = posTable.GetDataRow(1); // ID=1 是初始出生点
            if (initialPosData != null)
            {
                saveData.PlayerPos = initialPosData.Position;
                Log.Info($"设置初始位置: {saveData.PlayerPos} - {initialPosData.Description}");
            }
            else
            {
                Log.Warning("找不到初始位置数据 (PosTable ID=1)，使用默认位置");
                saveData.PlayerPos = Vector3.zero;
            }
        }
        else
        {
            Log.Error("PosTable 未加载");
            saveData.PlayerPos = Vector3.zero;
        }

        // 保存存档（确保位置被保存）
        PlayerAccountDataManager.Instance.SaveCurrentSave();

        Log.Info($"玩家存档创建成功:");
        Log.Info($"  - 存档ID: {saveData.SaveId}");
        Log.Info($"  - 存档名称: {saveData.SaveName}");
        Log.Info($"  - 初始位置: {saveData.PlayerPos}");
        Log.Info($"  - 等级: {saveData.GlobalLevel}");
        Log.Info($"  - 金币: {saveData.Gold}");
        Log.Info($"  - 召唤师: {selectedSummoner.Name}");

        // 显示欢迎提示（使用多语言，支持参数格式化）
        string welcomeMsg = string.Format(GF.Localization.GetString("UI_Welcome"), playerName);
        GF.UI.ShowToast(welcomeMsg, UIExtension.ToastStyle.Green);

        // 关闭当前界面
        GF.UI.CloseUIForm(this.UIForm);

        // 进入游戏
        EnterGame();
    }

    /// <summary>
    /// 进入游戏
    /// </summary>
    private void EnterGame()
    {
        // 使用 GameFlowManager 统一管理游戏流程
        GameFlowManager.EnterGame();
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

    #region 模型显示与交互

    /// <summary>
    /// 加载并显示召唤师模型
    /// </summary>
    private async void LoadSummonerModel(SummonerTable summoner)
    {
        if (summoner == null || m_ModelViewer == null)
            return;

        // 获取模型预制体配置ID
        int modelConfigId = summoner.PrefabId;

        // 使用 UIModelViewer 异步加载模型
        await m_ModelViewer.SetModelAsync(modelConfigId);

        if (m_ModelViewer.HasModel())
        {
            // 设置模型旋转为 180 度，让模型面向玩家
            m_ModelViewer.SetModelRotation(180f);

            Log.Info($"召唤师模型加载成功: {summoner.Name}，旋转角度设置为 (0, 180, 0)");
        }
    }

    /// <summary>
    /// 模型双击事件
    /// </summary>
    private void OnModelDoubleClick()
    {
        var summoner = GetCurrentSummoner();
        if (summoner != null)
        {
            DebugEx.LogModule("NewGameUI", $"双击召唤师 {summoner.Name}，播放交互动画");

            // 播放交互动画
            if (m_ModelViewer != null)
            {
                m_ModelViewer.PlayInteractAnimation(0); // 使用索引0的交互动画
            }
        }
    }

    #endregion

    #region 技能按钮管理

    /// <summary>
    /// 初始化技能按钮数组
    /// </summary>
    private void InitializeSkillButtons()
    {
        // 创建技能按钮数组（8个技能）
        m_SkillButtons = new Button[]
        {
            varSkill1, // 固定技能1（被动）
            varSkill2, // 固定技能2（主动）
            varSkill_A3, // 路线一技能3
            varSkill_A4, // 路线一技能4
            varSkill_A5, // 路线一技能5
            varSkill_B3, // 路线二技能3
            varSkill_B4, // 路线二技能4
            varSkill_B5, // 路线二技能5
        };

        // 为每个技能按钮添加点击事件
        for (int i = 0; i < m_SkillButtons.Length; i++)
        {
            if (m_SkillButtons[i] != null)
            {
                int index = i; // 闭包捕获
                m_SkillButtons[i].onClick.AddListener(() => OnSkillButtonClick(index));
            }
        }

        DebugEx.Log("NewGameUI", "技能按钮数组初始化完成");
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnSkillButtonClick(int skillIndex)
    {
        DebugEx.Log("NewGameUI", $"点击技能按钮: {skillIndex}");
        ShowSkillTooltip(skillIndex);
    }

    #endregion

    #region 技能悬停提示

    /// <summary>
    /// 处理技能图标悬停
    /// </summary>
    private void HandleSkillHover()
    {
        if (m_SkillButtons == null)
            return;

        int newHoveredIndex = -1;

        for (int i = 0; i < m_SkillButtons.Length; i++)
        {
            if (m_SkillButtons[i] == null || !m_SkillButtons[i].gameObject.activeSelf)
                continue;

            var rectTransform = m_SkillButtons[i].GetComponent<RectTransform>();
            if (rectTransform != null && IsPointerOverRectTransform(rectTransform))
            {
                newHoveredIndex = i;
                break;
            }
        }

        // 检测状态变化
        if (newHoveredIndex != m_HoveredSkillIndex)
        {
            DebugEx.Log(
                "NewGameUI",
                $"技能悬停状态变化: {m_HoveredSkillIndex} -> {newHoveredIndex}"
            );

            if (m_HoveredSkillIndex >= 0)
            {
                HideSkillTooltip();
            }

            m_HoveredSkillIndex = newHoveredIndex;

            if (m_HoveredSkillIndex >= 0)
            {
                ShowSkillTooltip(m_HoveredSkillIndex);
            }
        }
    }

    /// <summary>
    /// 显示技能提示
    /// </summary>
    private void ShowSkillTooltip(int skillIndex)
    {
        DebugEx.Log("NewGameUI", $"显示技能提示: skillIndex={skillIndex}");

        if (skillIndex < 0 || skillIndex >= m_CurrentSkillIds.Count)
        {
            DebugEx.Warning("NewGameUI", $"技能索引越界: {skillIndex} / {m_CurrentSkillIds.Count}");
            return;
        }

        int skillId = m_CurrentSkillIds[skillIndex];
        var skillTable = GF.DataTable.GetDataTable<SummonerSkillTable>();
        if (skillTable == null)
        {
            DebugEx.Error("NewGameUI", "SummonerSkillTable 未加载");
            return;
        }

        var skill = skillTable.GetDataRow(skillId);
        if (skill != null && m_SkillButtons != null && skillIndex < m_SkillButtons.Length)
        {
            DebugEx.Log("NewGameUI", $"准备显示技能提示: {skill.Name}");

            // 构建提示文本
            string tooltipText = $"<b>{skill.Name}</b>\n{skill.Desc}";

            // 获取技能按钮的 RectTransform
            var skillButton = m_SkillButtons[skillIndex];
            if (skillButton == null)
            {
                DebugEx.Error("NewGameUI", $"技能按钮 {skillIndex} 为 null");
                return;
            }

            var rectTransform = skillButton.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                DebugEx.Error("NewGameUI", $"技能按钮 {skillIndex} 的 RectTransform 为 null");
                return;
            }

            // 回收旧的提示框
            if (m_CurrentFloatingTipId != -1)
            {
                DebugEx.Log("NewGameUI", $"回收旧的提示框: {m_CurrentFloatingTipId}");
                var oldForm = GF.UI.GetUIForm(m_CurrentFloatingTipId);
                if (oldForm != null)
                {
                    oldForm.OnPause();
                }
            }

            // 显示技能提示框（在技能按钮的右上方）
            m_CurrentFloatingTipId = GF.UI.ShowFloatingTipAt(
                tooltipText,
                rectTransform,
                new Vector2(10f, 0f)
            );

            DebugEx.Log(
                "NewGameUI",
                $"技能提示已显示: {skill.Name}, FormId={m_CurrentFloatingTipId}"
            );
        }
        else
        {
            DebugEx.Warning(
                "NewGameUI",
                $"技能数据无效: skill={skill}, m_SkillButtons={m_SkillButtons}, skillIndex={skillIndex}"
            );
        }
    }

    /// <summary>
    /// 隐藏技能提示
    /// </summary>
    private void HideSkillTooltip()
    {
        // 回收当前显示的浮动提示框（不销毁，只回收）
        if (m_CurrentFloatingTipId != -1)
        {
            DebugEx.Log("NewGameUI", $"隐藏技能提示框: {m_CurrentFloatingTipId}");
            var uiForm = GF.UI.GetUIForm(m_CurrentFloatingTipId);
            if (uiForm != null)
            {
                uiForm.OnPause();
            }
        }
    }

    private bool IsPointerOverRectTransform(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return false;

        Vector2 localPoint;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                Input.mousePosition,
                GF.UICamera,
                out localPoint
            ) && rectTransform.rect.Contains(localPoint);
    }

    #endregion
}
