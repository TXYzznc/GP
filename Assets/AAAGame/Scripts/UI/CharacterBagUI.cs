using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using GameExtension;
using GameFramework.DataTable;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 角色管理界面
/// 三区域布局：左侧棋子/宝物列表、中间立绘/模型展示、右侧四标签页
/// </summary>
public partial class CharacterBagUI : UIFormBase
{
    #region 字段

    private List<ChessItemUI_Small> m_ChessItemPool = new();
    private List<InventorySlotUI> m_TreasureSlotPool = new();
    private int m_CurrentSelectedChessId = -1;
    private bool m_IsShowingChessList = true; // true=棋子列表，false=宝物仓库
    private int m_CurrentTabIndex = 0; // 0=State, 1=Treasure, 2=LevelUp, 3=Story
    private int m_CurrentLevelStage = 0; // 当前选中的阶段（用于高亮）
    private bool m_IsShowingPortrait = false; // true=立绘，false=模型

    private IDataTable<SummonChessTable> m_DtSummonChess;
    private IDataTable<SummonChessSkillTable> m_DtSkill;

    // 模型显示相关 - 使用 UIModelViewer 组件
    private UIModelViewer m_ModelViewer = null;

    #endregion

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        DebugEx.Log(nameof(CharacterBagUI), "[OnOpen] 开始打开UI");

        // 获取数据表
        m_DtSummonChess = GF.DataTable.GetDataTable<SummonChessTable>();
        m_DtSkill = GF.DataTable.GetDataTable<SummonChessSkillTable>();

        // 重置默认显示状态
        m_IsShowingChessList = true; // 左侧默认显示角色栏
        m_CurrentTabIndex = 0; // 右侧默认显示StateUI
        m_CurrentLevelStage = 0; // 默认显示一阶数据
        m_CurrentSelectedChessId = -1; // 重置选中角色（等待列表加载后自动选择第一个）
        m_IsShowingPortrait = true; // 默认显示海报（立绘）

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[OnOpen] 初始状态: IsShowingChessList={m_IsShowingChessList}, TabIndex={m_CurrentTabIndex}"
        );

        // 初始化模型查看器
        InitializeModelViewer();

        // 注册事件
        RegisterEvents();

        // 初始化UI显示状态
        InitializeUIAppearance();

        // 初始化 UI
        LoadChessListAsync().Forget();

        // 请求解锁鼠标（通过引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseUnlock();

        DebugEx.Success(nameof(CharacterBagUI), "[OnOpen] UI打开完成");
    }

    /// <summary>
    /// 初始化模型查看器（参考 NewGameUI）
    /// </summary>
    private void InitializeModelViewer()
    {
        DebugEx.Log(nameof(CharacterBagUI), "[InitializeModelViewer] 开始初始化模型查看器");

        if (varOccupationImage == null)
        {
            DebugEx.Error(
                nameof(CharacterBagUI),
                "[InitializeModelViewer] varOccupationImage 为 null"
            );
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
                // 删除 Image 组件，添加 RawImage
                Destroy(image);
                rawImage = varOccupationImage.gameObject.AddComponent<RawImage>();
                DebugEx.Log(
                    nameof(CharacterBagUI),
                    "[InitializeModelViewer] 已将 Image 组件替换为 RawImage 组件"
                );
            }
            else
            {
                rawImage = varOccupationImage.gameObject.AddComponent<RawImage>();
            }
        }

        // 获取或初始化 UIModelViewer
        m_ModelViewer = varOccupationImage.GetComponent<UIModelViewer>();
        if (m_ModelViewer == null)
        {
            m_ModelViewer = varOccupationImage.gameObject.AddComponent<UIModelViewer>();
        }

        m_ModelViewer.Initialize(rawImage);

        DebugEx.Success(nameof(CharacterBagUI), "[InitializeModelViewer] UIModelViewer 初始化完成");
    }

    /// <summary>
    /// 初始化UI外观显示（默认状态）
    /// </summary>
    private void InitializeUIAppearance()
    {
        DebugEx.Log(nameof(CharacterBagUI), "[InitializeUIAppearance] 开始初始化UI外观");

        // 左侧默认显示角色栏（不显示宝物仓库）
        if (varChessContent != null)
        {
            varChessContent.gameObject.SetActive(true);
            DebugEx.Log(nameof(CharacterBagUI), "[InitializeUIAppearance] 显示角色栏");
        }
        if (varTreasureContent != null)
        {
            varTreasureContent.gameObject.SetActive(false);
            DebugEx.Log(nameof(CharacterBagUI), "[InitializeUIAppearance] 隐藏宝物仓库");
        }

        // 中间默认显示海报（立绘）
        if (varNormalImage != null)
        {
            varNormalImage.gameObject.SetActive(m_IsShowingPortrait);
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[InitializeUIAppearance] 立绘状态: active={varNormalImage.gameObject.activeSelf}"
            );
        }
        if (varOccupationImage != null)
        {
            varOccupationImage.gameObject.SetActive(!m_IsShowingPortrait);
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[InitializeUIAppearance] 模型状态: active={varOccupationImage.gameObject.activeSelf}"
            );
        }

        // 更新切换按钮文本
        UpdateSwitchButtonText();

        // 右侧默认显示StateUI（标签页0）
        if (varStateUI != null)
            varStateUI.gameObject.SetActive(true);
        if (varTreasureUI != null)
            varTreasureUI.gameObject.SetActive(false);
        if (varLevelUpUI != null)
            varLevelUpUI.gameObject.SetActive(false);
        if (varStoryUI != null)
            varStoryUI.gameObject.SetActive(false);

        DebugEx.Success(nameof(CharacterBagUI), "[InitializeUIAppearance] UI外观初始化完成");
    }

    private void RegisterEvents()
    {
        DebugEx.Log(nameof(CharacterBagUI), "[RegisterEvents] 开始注册事件");

        // 关闭按钮
        if (varCloseBtn != null)
            varCloseBtn.onClick.AddListener(() => GF.UI.CloseUIForm(this.UIForm));

        // 左侧列表切换
        if (varTreasureSwitchBtn != null)
        {
            varTreasureSwitchBtn.onClick.AddListener(OnTreasureSwitchBtnClicked);
            DebugEx.Log(nameof(CharacterBagUI), "[RegisterEvents] 注册宝物切换按钮事件");
        }

        // 立绘/模型切换
        if (varSwitchBtn != null)
        {
            varSwitchBtn.onClick.AddListener(OnSwitchBtnClicked);
            DebugEx.Log(nameof(CharacterBagUI), "[RegisterEvents] 注册立绘/模型切换按钮事件");
        }

        // 右侧标签页按钮
        if (varStateBtn != null)
            varStateBtn.onClick.AddListener(() => OnTabButtonClicked(0));
        if (varTreasureBtn != null)
            varTreasureBtn.onClick.AddListener(() => OnTabButtonClicked(1));
        if (varLevelUpBtn != null)
            varLevelUpBtn.onClick.AddListener(() => OnTabButtonClicked(2));
        if (varStoryBtn != null)
            varStoryBtn.onClick.AddListener(() => OnTabButtonClicked(3));

        // 技能按钮
        if (varPassiveSkill != null)
            varPassiveSkill.onClick.AddListener(() => OnSkillButtonClicked(SkillType.Passive));
        if (varNormalAtk != null)
            varNormalAtk.onClick.AddListener(() => OnSkillButtonClicked(SkillType.NormalAtk));
        if (varSkill_1 != null)
            varSkill_1.onClick.AddListener(() => OnSkillButtonClicked(SkillType.Skill1));
        if (varSkill_2 != null)
            varSkill_2.onClick.AddListener(() => OnSkillButtonClicked(SkillType.Skill2));
        if (varUltimateSkill != null)
            varUltimateSkill.onClick.AddListener(() => OnSkillButtonClicked(SkillType.Ultimate));

        // 阶段按钮
        if (varLevel1Arr != null && varLevel1Arr.Length > 0)
        {
            for (int i = 0; i < varLevel1Arr.Length; i++)
            {
                int index = i;
                varLevel1Arr[i].onClick.AddListener(() => OnLevelButtonClicked(index));
            }
        }

        DebugEx.Success(nameof(CharacterBagUI), "[RegisterEvents] 事件注册完成");
    }

    private async UniTask LoadChessListAsync()
    {
        try
        {
            if (varChessContent == null)
                return;

            // 清空现有的棋子卡
            foreach (var item in m_ChessItemPool)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            m_ChessItemPool.Clear();

            // 获取玩家拥有的棋子列表
            PlayerSaveData saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
            if (
                saveData == null
                || saveData.OwnedUnitCardIds == null
                || saveData.OwnedUnitCardIds.Count == 0
            )
            {
                DebugEx.Warning(nameof(CharacterBagUI), "玩家没有拥有任何棋子");
                return;
            }

            List<int> playerChessList = new(saveData.OwnedUnitCardIds);

            // 获取棋子卡模板
            if (varChessItemUI_Small == null)
            {
                DebugEx.Error(nameof(CharacterBagUI), "缺少 ChessItemUI_Small 模板");
                return;
            }

            // 创建棋子卡
            foreach (int chessId in playerChessList)
            {
                SummonChessTable chessRow = m_DtSummonChess.GetDataRow(chessId);
                if (chessRow == null)
                    continue;

                // 获取配置
                var chessConfig = CreateChessConfig(chessRow);

                // 实例化棋子卡
                GameObject cardObj = Instantiate(varChessItemUI_Small, varChessContent.transform);
                if (cardObj.TryGetComponent<ChessItemUI_Small>(out var cardScript))
                {
                    cardScript.InitChess(chessId, chessConfig);
                    cardScript.OnChessSelected += OnChessSelected;
                    m_ChessItemPool.Add(cardScript);

                    // 添加点击事件
                    if (cardObj.TryGetComponent<Button>(out var cardBtn))
                    {
                        cardBtn.onClick.AddListener(() => cardScript.OnCardSelected());
                    }
                }
            }

            // 选择第一个棋子
            if (m_ChessItemPool.Count > 0)
            {
                OnChessSelected(m_ChessItemPool[0].GetChessId());
            }

            DebugEx.Success(
                nameof(CharacterBagUI),
                $"棋子列表加载完成，共 {m_ChessItemPool.Count} 个"
            );
        }
        catch (System.Exception ex)
        {
            DebugEx.Error(nameof(CharacterBagUI), $"加载棋子列表失败：{ex.Message}");
        }

        await UniTask.CompletedTask;
    }

    private SummonChessConfig CreateChessConfig(SummonChessTable row)
    {
        return new SummonChessConfig
        {
            Id = row.Id,
            Name = row.Name,
            Quality = row.Quality,
            PopCost = row.PopCost,
            StoryText = row.StoryText,
            Races = row.Races,
            Classes = row.Classes,
            PrefabId = row.PrefabId,
            IconId = row.IconId,
            MaxHp = row.MaxHp,
            MaxMp = row.MaxMp,
            InitialMp = row.InitialMp,
            AtkDamage = row.AtkDamage,
            AtkSpeed = row.AtkSpeed,
            AtkRange = row.AtkRange,
            Armor = row.Armor,
            MagicResist = row.MagicResist,
            MoveSpeed = row.MoveSpeed,
            CritRate = row.CritRate,
            CritDamage = row.CritDamage,
            SpellPower = row.SpellPower,
            Shield = row.Shield,
            CooldownReduce = row.CooldownReduce,
            PassiveIds = row.PassiveIds,
            NormalAtkId = row.NormalAtkId,
            Skill1Id = row.Skill1Id,
            Skill2Id = row.Skill2Id,
            UltimateId = row.UltimateId,
            AIType = row.AIType,
        };
    }

    private void OnChessSelected(int chessId)
    {
        m_CurrentSelectedChessId = chessId;

        // 更新所有卡片的高亮状态
        foreach (var item in m_ChessItemPool)
        {
            item.SetHighlight(item.GetChessId() == chessId);
        }

        // 重置当前选中的阶段为 0
        m_CurrentLevelStage = 0;

        // 更新右侧所有标签页
        UpdateAllTabs();

        // 更新阶段按钮高亮
        UpdateLevelButtonHighlight();

        // ⭐ 切换角色时，默认选中第一个技能（Passive）
        SelectDefaultSkill();

        // ⭐ 更新中间显示（刷新立绘/模型）
        UpdateMiddleDisplay();

        DebugEx.Log(nameof(CharacterBagUI), $"选中棋子 {chessId}");
    }

    /// <summary>
    /// 选中默认技能（Passive - 第一个）并显示其描述
    /// </summary>
    private void SelectDefaultSkill()
    {
        if (m_CurrentSelectedChessId <= 0)
            return;

        SummonChessTable chessRow = m_DtSummonChess.GetDataRow(m_CurrentSelectedChessId);
        if (chessRow == null)
            return;

        // 获取Passive技能ID（第一个技能）
        int passiveSkillId =
            chessRow.PassiveIds != null && chessRow.PassiveIds.Length > 0
                ? chessRow.PassiveIds[0]
                : 0;

        if (passiveSkillId <= 0)
            return;

        SummonChessSkillTable skillRow = m_DtSkill.GetDataRow(passiveSkillId);
        if (skillRow == null)
            return;

        // 更新技能描述显示
        if (varSkillEffectText != null)
            varSkillEffectText.text = skillRow.EffectText;

        if (varSkillDescText != null)
            varSkillDescText.text = skillRow.DescText ?? skillRow.EffectText;

        DebugEx.Log(nameof(CharacterBagUI), $"默认选中技能 {passiveSkillId}: {skillRow.Name}");
    }

    /// <summary>
    /// 更新中间显示内容（刷新立绘/模型及宝物槽）
    /// </summary>
    private void UpdateMiddleDisplay()
    {
        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[UpdateMiddleDisplay] 开始更新中间显示, 当前选中棋子: {m_CurrentSelectedChessId}, 显示模式: {(m_IsShowingPortrait ? "立绘" : "模型")}"
        );

        if (m_CurrentSelectedChessId <= 0)
        {
            DebugEx.Warning(nameof(CharacterBagUI), "[UpdateMiddleDisplay] 未选中棋子");
            return;
        }

        SummonChessTable chessRow = m_DtSummonChess.GetDataRow(m_CurrentSelectedChessId);
        if (chessRow == null)
        {
            DebugEx.Error(
                nameof(CharacterBagUI),
                $"[UpdateMiddleDisplay] 棋子配置未找到: {m_CurrentSelectedChessId}"
            );
            return;
        }

        if (m_IsShowingPortrait && varNormalImage != null)
        {
            // 加载立绘（海报）
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[UpdateMiddleDisplay] 开始加载立绘: chessId={m_CurrentSelectedChessId}, posterId={chessRow.ChessPosterId}"
            );
            _ = ResourceExtension.LoadSpriteAsync(chessRow.ChessPosterId, varNormalImage);
        }
        else if (!m_IsShowingPortrait && m_ModelViewer != null)
        {
            // 加载3D模型（使用 UIModelViewer）
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[UpdateMiddleDisplay] 开始加载3D模型: chessId={m_CurrentSelectedChessId}, prefabId={chessRow.PrefabId}"
            );
            LoadChessModelAsync(chessRow).Forget();
        }

        // 刷新宝物装备槽显示
        UpdateTreasureSlots();

        DebugEx.Success(nameof(CharacterBagUI), "[UpdateMiddleDisplay] 中间显示更新完成");
    }

    /// <summary>
    /// 异步加载棋子3D模型
    /// </summary>
    private async UniTaskVoid LoadChessModelAsync(SummonChessTable chessRow)
    {
        if (chessRow == null || m_ModelViewer == null)
        {
            DebugEx.Error(
                nameof(CharacterBagUI),
                "[LoadChessModelAsync] chessRow 或 m_ModelViewer 为 null"
            );
            return;
        }

        int modelConfigId =
            chessRow.PrefabId != null && chessRow.PrefabId.Length > 0 ? chessRow.PrefabId[0] : 0;

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[LoadChessModelAsync] 开始异步加载模型: prefabId={modelConfigId}"
        );

        // 使用 UIModelViewer 异步加载模型
        await m_ModelViewer.SetModelAsync(modelConfigId);

        if (m_ModelViewer.HasModel())
        {
            // 设置模型旋转为 180 度，让模型面向玩家
            m_ModelViewer.SetModelRotation(180f);

            // 确保播放 Idle 待机动画
            m_ModelViewer.PlayIdleAnimation();

            DebugEx.Success(
                nameof(CharacterBagUI),
                $"[LoadChessModelAsync] 棋子模型加载成功: {chessRow.Name}，旋转角度设置为 (0, 180, 0)"
            );
        }
        else
        {
            DebugEx.Error(
                nameof(CharacterBagUI),
                $"[LoadChessModelAsync] 棋子模型加载失败: {chessRow.Name}"
            );
        }
    }

    private void UpdateAllTabs()
    {
        if (m_CurrentSelectedChessId <= 0)
            return;

        SummonChessTable chessRow = m_DtSummonChess.GetDataRow(m_CurrentSelectedChessId);
        if (chessRow == null)
            return;

        UpdateStateTab(chessRow);
        UpdateTreasureTab();
        UpdateLevelUpTab(chessRow, 0); // 默认显示第一阶段（一阶）数据
        UpdateStoryTab(chessRow, 0); // 默认显示第一阶段的故事
    }

    private void UpdateStateTab(SummonChessTable chessRow)
    {
        if (varNameText != null)
            varNameText.text = chessRow.Name;

        // 根据 Skill2Id 决定是否显示 Skill_2 按钮
        if (varSkill_2 != null)
        {
            bool hasSkill2 =
                chessRow.Skill2Id != null
                && chessRow.Skill2Id.Length > 0
                && chessRow.Skill2Id[0] != 0;
            varSkill_2.gameObject.SetActive(hasSkill2);
        }
    }

    private void UpdateTreasureTab()
    {
        if (m_CurrentSelectedChessId <= 0)
            return;

        // 获取该棋子装备的宝物列表
        var treasureManager = PlayerAccountDataManager.Instance;
        List<TreasureInstanceData> equippedTreasures = treasureManager.GetChessEquipments(
            m_CurrentSelectedChessId
        );

        StringBuilder baseAttributesText = new();
        StringBuilder treasureNamesText = new();

        if (equippedTreasures != null && equippedTreasures.Count > 0)
        {
            IDataTable<TreasureTable> dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();

            foreach (var treasure in equippedTreasures)
            {
                TreasureTable treasureRow = dtTreasure.GetDataRow(treasure.TreasureId);
                if (treasureRow == null)
                    continue;

                // 收集宝物名称
                treasureNamesText.Append(treasureRow.Name).Append("\n");

                // 收集词条信息
                if (treasure.Affixes != null && treasure.Affixes.Count > 0)
                {
                    foreach (var affix in treasure.Affixes)
                    {
                        string valueStr =
                            affix.ValueType == ValueType.Percent
                                ? $"{affix.ValueMin}%"
                                : affix.ValueMin.ToString();
                        baseAttributesText.Append($"• {affix.Name}: +{valueStr}\n");
                    }
                }
                else
                {
                    baseAttributesText.Append("无词条\n");
                }
            }
        }

        // 更新显示
        if (varBaseEffect != null)
        {
            varBaseEffect.text =
                treasureNamesText.Length > 0
                    ? treasureNamesText.ToString().TrimEnd()
                    : "未装备宝物";
        }

        if (varSpecialEffect != null)
        {
            varSpecialEffect.text =
                baseAttributesText.Length > 0
                    ? baseAttributesText.ToString().TrimEnd()
                    : "未装备宝物";
        }
    }

    private void UpdateLevelUpTab(SummonChessTable chessRow, int stage)
    {
        // stage: 0=一阶, 1=二阶, 2=三A阶, 3=三B阶
        if (stage < 0 || stage >= 4)
            stage = 0;

        if (varLevelUp_Base != null)
        {
            string baseInfo =
                $"HP: {chessRow.MaxHp[stage]}\n攻击: {chessRow.AtkDamage[stage]}\n防御: {chessRow.Armor[stage]}\n魔抗: {chessRow.MagicResist[stage]}";
            varLevelUp_Base.text = baseInfo;
        }
    }

    private void UpdateStoryTab(SummonChessTable chessRow, int stage)
    {
        if (varStoryText != null && chessRow.StoryText != null && chessRow.StoryText.Length > 0)
        {
            if (stage >= 0 && stage < chessRow.StoryText.Length)
            {
                varStoryText.text = chessRow.StoryText[stage];
            }
        }
    }

    private void OnTreasureSwitchBtnClicked()
    {
        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[OnTreasureSwitchBtnClicked] 点击切换按钮, 当前状态: IsShowingChessList={m_IsShowingChessList}"
        );

        m_IsShowingChessList = !m_IsShowingChessList;

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[OnTreasureSwitchBtnClicked] 切换后状态: IsShowingChessList={m_IsShowingChessList}"
        );

        if (varChessContent != null)
        {
            varChessContent.gameObject.SetActive(m_IsShowingChessList);
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[OnTreasureSwitchBtnClicked] 角色栏状态: active={varChessContent.gameObject.activeSelf}"
            );
        }

        if (varTreasureContent != null)
        {
            varTreasureContent.gameObject.SetActive(!m_IsShowingChessList);
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[OnTreasureSwitchBtnClicked] 宝物仓库状态: active={varTreasureContent.gameObject.activeSelf}"
            );
        }

        if (!m_IsShowingChessList)
        {
            DebugEx.Log(nameof(CharacterBagUI), "[OnTreasureSwitchBtnClicked] 开始加载宝物仓库");
            LoadTreasureRepositoryAsync().Forget();
        }

        DebugEx.Success(
            nameof(CharacterBagUI),
            $"[OnTreasureSwitchBtnClicked] 切换完成: {(m_IsShowingChessList ? "棋子列表" : "宝物仓库")}"
        );
    }

    private async UniTask LoadTreasureRepositoryAsync()
    {
        try
        {
            if (varTreasureContent == null)
                return;

            // 清空现有槽位
            foreach (var item in m_TreasureSlotPool)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            m_TreasureSlotPool.Clear();

            // 获取槽位预制体
            if (varInventorySlotUI == null)
            {
                DebugEx.Error(nameof(CharacterBagUI), "缺少 InventorySlotUI 模板");
                return;
            }

            // 获取玩家的宝物数据（背包 + 仓库）
            var treasureManager = PlayerAccountDataManager.Instance;
            var allTreasures = new List<TreasureInstanceData>();
            allTreasures.AddRange(treasureManager.GetInventoryTreasures());
            allTreasures.AddRange(treasureManager.GetWarehouseTreasures());

            // 创建宝物槽位（根据实际宝物数量和固定容量 50）
            int slotCount = Mathf.Max(50, allTreasures.Count); // 至少 50 个槽位

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObj = Instantiate(varInventorySlotUI, varTreasureContent.transform);
                if (slotObj.TryGetComponent<InventorySlotUI>(out var slotScript))
                {
                    m_TreasureSlotPool.Add(slotScript);

                    // 如果有宝物要显示在这个槽位
                    if (i < allTreasures.Count)
                    {
                        var treasure = allTreasures[i];
                        BindTreasureToSlot(slotScript, treasure);
                    }
                }
            }

            DebugEx.Success(
                nameof(CharacterBagUI),
                $"宝物仓库加载完成，共 {m_TreasureSlotPool.Count} 个槽位，{allTreasures.Count} 个宝物"
            );
        }
        catch (System.Exception ex)
        {
            DebugEx.Error(nameof(CharacterBagUI), $"加载宝物仓库失败：{ex.Message}");
        }

        await UniTask.CompletedTask;
    }

    private void BindTreasureToSlot(InventorySlotUI slotScript, TreasureInstanceData treasure)
    {
        if (slotScript == null || treasure == null)
            return;

        // 获取宝物配置
        IDataTable<TreasureTable> dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();
        TreasureTable treasureRow = dtTreasure.GetDataRow(treasure.TreasureId);

        if (treasureRow == null)
            return;

        // ⭐ 调用InventorySlotUI的接口来加载TreasureItemUI
        slotScript.LoadTreasureItemUI(treasure.TreasureId);

        // 显示装备状态：锁定和棋子名称
        if (treasure.EquippedChessId != 0)
        {
            slotScript.SetLockVisible(true);

            SummonChessTable chessRow = m_DtSummonChess.GetDataRow(treasure.EquippedChessId);
            string chessName = chessRow != null ? chessRow.Name : $"棋子{treasure.EquippedChessId}";
            slotScript.SetLockText($"{chessName}\n已装备");
        }
        else
        {
            slotScript.SetLockVisible(false);

            // ⭐ 修复问题三：只保留拖拽装备功能，移除点击装备
            // 为未装备的宝物添加拖拽处理
            AddTreasureDragHandler(slotScript, treasure.InstanceId, true);
        }
    }

    private void AddTreasureDragHandler(
        InventorySlotUI slotScript,
        int treasureInstanceId,
        bool isFromInventory
    )
    {
        var slotRect = slotScript.GetComponent<RectTransform>();
        if (slotRect == null)
            return;

        // 检查是否已有处理器
        if (slotRect.GetComponent<TreasureDragHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureDragHandler>();
        handler.Initialize(treasureInstanceId, isFromInventory, slotRect);
    }

    private void OnSwitchBtnClicked()
    {
        DebugEx.Log(nameof(CharacterBagUI), "[OnSwitchBtnClicked] 点击立绘/模型切换按钮");

        // 切换立绘/模型显示
        if (varNormalImage != null && varOccupationImage != null)
        {
            m_IsShowingPortrait = !m_IsShowingPortrait;

            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[OnSwitchBtnClicked] 切换到: {(m_IsShowingPortrait ? "海报" : "模型")}"
            );

            varNormalImage.gameObject.SetActive(m_IsShowingPortrait);
            varOccupationImage.gameObject.SetActive(!m_IsShowingPortrait);

            // 更新按钮文本
            UpdateSwitchButtonText();

            DebugEx.Success(
                nameof(CharacterBagUI),
                $"[OnSwitchBtnClicked] 切换完成: 立绘={varNormalImage.gameObject.activeInHierarchy}, 模型={varOccupationImage.gameObject.activeInHierarchy}"
            );

            // ⭐ 切换后重新加载对应的显示内容
            UpdateMiddleDisplay();
        }
        else
        {
            DebugEx.Error(
                nameof(CharacterBagUI),
                "[OnSwitchBtnClicked] varNormalImage 或 varOccupationImage 为空"
            );
        }
    }

    /// <summary>
    /// 更新切换按钮的文本（显示海报时文本为"模型"，显示模型时文本为"海报"）
    /// </summary>
    private void UpdateSwitchButtonText()
    {
        if (varSwitchBtn == null)
            return;

        // 获取按钮的子对象中的 TextMeshProUGUI 组件
        var buttonText = varSwitchBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (buttonText != null)
        {
            // 显示海报时，按钮文本为"模型"；显示模型时，按钮文本为"海报"
            buttonText.text = m_IsShowingPortrait ? "模型" : "海报";
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[UpdateSwitchButtonText] 按钮文本更新为: {buttonText.text}"
            );
        }
    }

    private void OnTabButtonClicked(int tabIndex)
    {
        m_CurrentTabIndex = tabIndex;

        // 隐藏所有标签页
        if (varStateUI != null)
            varStateUI.gameObject.SetActive(tabIndex == 0);
        if (varTreasureUI != null)
            varTreasureUI.gameObject.SetActive(tabIndex == 1);
        if (varLevelUpUI != null)
            varLevelUpUI.gameObject.SetActive(tabIndex == 2);
        if (varStoryUI != null)
            varStoryUI.gameObject.SetActive(tabIndex == 3);

        DebugEx.Log(nameof(CharacterBagUI), $"切换到标签页 {tabIndex}");
    }

    private void OnSkillButtonClicked(SkillType skillType)
    {
        if (m_CurrentSelectedChessId <= 0)
            return;

        SummonChessTable chessRow = m_DtSummonChess.GetDataRow(m_CurrentSelectedChessId);
        if (chessRow == null)
            return;

        int skillId = GetSkillIdByType(chessRow, skillType);
        if (skillId <= 0)
            return;

        SummonChessSkillTable skillRow = m_DtSkill.GetDataRow(skillId);
        if (skillRow == null)
            return;

        // 更新技能信息显示
        if (varSkillEffectText != null)
            varSkillEffectText.text = skillRow.EffectText;

        if (varSkillDescText != null)
            varSkillDescText.text = skillRow.DescText ?? skillRow.EffectText;

        DebugEx.Log(nameof(CharacterBagUI), $"显示技能 {skillId}: {skillRow.Name}");
    }

    private int GetSkillIdByType(SummonChessTable chessRow, SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Passive => chessRow.PassiveIds?[0] ?? 0,
            SkillType.NormalAtk => chessRow.NormalAtkId?[0] ?? 0,
            SkillType.Skill1 => chessRow.Skill1Id?[0] ?? 0,
            SkillType.Skill2 => chessRow.Skill2Id?[0] ?? 0,
            SkillType.Ultimate => chessRow.UltimateId?[0] ?? 0,
            _ => 0,
        };
    }

    private void OnLevelButtonClicked(int levelIndex)
    {
        if (m_CurrentSelectedChessId <= 0)
            return;

        SummonChessTable chessRow = m_DtSummonChess.GetDataRow(m_CurrentSelectedChessId);
        if (chessRow == null)
            return;

        int stageIndex = levelIndex % 4; // 0=一阶, 1=二阶, 2=三A阶, 3=三B阶
        m_CurrentLevelStage = stageIndex;

        UpdateLevelUpTab(chessRow, stageIndex);
        UpdateLevelButtonHighlight();

        DebugEx.Log(nameof(CharacterBagUI), $"显示阶级 {levelIndex} 的数据");
    }

    private void UpdateLevelButtonHighlight()
    {
        if (varLevel1Arr == null || varLevel1Arr.Length == 0)
            return;

        // 更新所有阶段按钮的高亮状态
        for (int i = 0; i < varLevel1Arr.Length; i++)
        {
            if (varLevel1Arr[i] != null && varLevel1Arr[i].TryGetComponent<Image>(out var btnImage))
            {
                // 选中的按钮为完全透明度，未选中的为 0.6 透明度
                btnImage.color =
                    i == m_CurrentLevelStage
                        ? new Color(1f, 1f, 1f, 1f)
                        : new Color(1f, 1f, 1f, 0.6f);
            }
        }
    }

    private void UpdateTreasureSlots()
    {
        if (m_CurrentSelectedChessId <= 0 || varTreasureSlot1Arr == null)
            return;

        var treasureManager = PlayerAccountDataManager.Instance;
        List<TreasureInstanceData> equippedTreasures = treasureManager.GetChessEquipments(
            m_CurrentSelectedChessId
        );
        IDataTable<TreasureTable> dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();

        for (int i = 0; i < varTreasureSlot1Arr.Length; i++)
        {
            RectTransform slotRect = varTreasureSlot1Arr[i];
            if (slotRect == null)
                continue;

            TreasureInstanceData treasure =
                i < equippedTreasures.Count ? equippedTreasures[i] : null;

            // ⭐ 修复问题一：TreasureSlot容器始终显示（无论是否有宝物）
            slotRect.gameObject.SetActive(true);

            if (treasure != null)
            {
                TreasureTable treasureRow = dtTreasure.GetDataRow(treasure.TreasureId);
                if (treasureRow != null)
                {
                    TreasureItemUI treasureItemUI =
                        slotRect.GetComponentInChildren<TreasureItemUI>();
                    if (treasureItemUI == null)
                    {
                        treasureItemUI = slotRect.gameObject.AddComponent<TreasureItemUI>();
                    }

                    treasureItemUI.InitTreasure(treasure.TreasureId);
                    treasureItemUI.gameObject.SetActive(true);

                    // 为已装备的宝物添加右键卸装处理
                    AddTreasureSlotRightClickHandler(slotRect, treasure.InstanceId);

                    // 为已装备的宝物添加拖拽处理（可拖拽卸装）
                    AddTreasureDragHandler(slotRect, treasure.InstanceId, false);

                    // 添加标签用于拖拽检测
                    slotRect.gameObject.tag = "TreasureSlot";
                }
            }
            else
            {
                // 没有宝物时，隐藏TreasureItemUI（但保留容器）
                TreasureItemUI treasureItemUI = slotRect.GetComponentInChildren<TreasureItemUI>();
                if (treasureItemUI != null)
                {
                    treasureItemUI.gameObject.SetActive(false);
                }
            }
        }

        DebugEx.Log(nameof(CharacterBagUI), $"刷新棋子 {m_CurrentSelectedChessId} 的宝物槽位");
    }

    private void AddTreasureDragHandler(
        RectTransform slotRect,
        int treasureInstanceId,
        bool isFromInventory
    )
    {
        if (slotRect == null)
            return;

        // 检查是否已有处理器
        if (slotRect.GetComponent<TreasureDragHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureDragHandler>();
        handler.Initialize(treasureInstanceId, isFromInventory, slotRect);
    }

    private void AddTreasureSlotRightClickHandler(RectTransform slotRect, int treasureInstanceId)
    {
        if (slotRect.GetComponent<TreasureSlotRightClickHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureSlotRightClickHandler>();
        handler.Initialize(treasureInstanceId, OnTreasureSlotRightClicked);
    }

    private void OnTreasureSlotRightClicked(int treasureInstanceId)
    {
        var treasureManager = PlayerAccountDataManager.Instance;
        treasureManager.UnequipTreasure(treasureInstanceId);
        treasureManager.SaveCurrentSave();

        DebugEx.Log(nameof(CharacterBagUI), $"从宝物槽卸装宝物 {treasureInstanceId}");

        UpdateTreasureSlots();
        UpdateTreasureTab();
        LoadTreasureRepositoryAsync().Forget();
    }

    public int GetCurrentSelectedChessId() => m_CurrentSelectedChessId;

    public void RefreshTreasureUI()
    {
        UpdateTreasureSlots();
        UpdateTreasureTab();
        LoadTreasureRepositoryAsync().Forget();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DebugEx.Log(nameof(CharacterBagUI), "[OnClose] 开始关闭UI");

        // ⭐ 移除所有按钮监听器（修复事件重复注册问题）
        UnregisterEvents();

        // ⭐ 清理模型查看器（包括 RenderTexture、Camera 和 ModelRoot）
        if (m_ModelViewer != null)
        {
            m_ModelViewer.CleanupRenderTexture();
        }

        // 清理棋子卡片池
        foreach (var item in m_ChessItemPool)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        m_ChessItemPool.Clear();

        // 清理宝物槽位池
        foreach (var item in m_TreasureSlotPool)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        m_TreasureSlotPool.Clear();

        // 请求锁定鼠标（通过引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseLock();

        DebugEx.Success(nameof(CharacterBagUI), "[OnClose] UI关闭完成");

        base.OnClose(isShutdown, userData);
    }

    private void UnregisterEvents()
    {
        DebugEx.Log(nameof(CharacterBagUI), "[UnregisterEvents] 开始移除事件监听器");

        if (varCloseBtn != null)
            varCloseBtn.onClick.RemoveAllListeners();
        if (varTreasureSwitchBtn != null)
            varTreasureSwitchBtn.onClick.RemoveAllListeners();
        if (varSwitchBtn != null)
            varSwitchBtn.onClick.RemoveAllListeners();
        if (varStateBtn != null)
            varStateBtn.onClick.RemoveAllListeners();
        if (varTreasureBtn != null)
            varTreasureBtn.onClick.RemoveAllListeners();
        if (varLevelUpBtn != null)
            varLevelUpBtn.onClick.RemoveAllListeners();
        if (varStoryBtn != null)
            varStoryBtn.onClick.RemoveAllListeners();
        if (varPassiveSkill != null)
            varPassiveSkill.onClick.RemoveAllListeners();
        if (varNormalAtk != null)
            varNormalAtk.onClick.RemoveAllListeners();
        if (varSkill_1 != null)
            varSkill_1.onClick.RemoveAllListeners();
        if (varSkill_2 != null)
            varSkill_2.onClick.RemoveAllListeners();
        if (varUltimateSkill != null)
            varUltimateSkill.onClick.RemoveAllListeners();

        if (varLevel1Arr != null)
        {
            foreach (var btn in varLevel1Arr)
            {
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
            }
        }

        DebugEx.Success(nameof(CharacterBagUI), "[UnregisterEvents] 事件监听器移除完成");
    }

    #region 辅助类型

    private enum SkillType
    {
        Passive,
        NormalAtk,
        Skill1,
        Skill2,
        Ultimate,
    }

    #endregion
}
