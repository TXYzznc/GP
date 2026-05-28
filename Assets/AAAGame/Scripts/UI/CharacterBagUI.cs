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

        // ⭐ 更新 Title 和图标（默认显示角色列表）
        UpdateTitleAndIcons();

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

        // ⭐ 更新标签按钮图标（默认选中 StateBtn）
        UpdateTabButtonIcons();

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

        // ⭐ 更新属性显示（使用当前阶段的数值）
        int stage = m_CurrentLevelStage; // 0=一阶, 1=二阶, 2=三A阶, 3=三B阶

        if (varHPText != null)
            varHPText.text =
                chessRow.MaxHp != null && stage < chessRow.MaxHp.Length
                    ? chessRow.MaxHp[stage].ToString()
                    : "0";

        if (varMpText != null)
            varMpText.text =
                chessRow.MaxMp != null && stage < chessRow.MaxMp.Length
                    ? chessRow.MaxMp[stage].ToString()
                    : "0";

        if (varAttackText != null)
            varAttackText.text =
                chessRow.AtkDamage != null && stage < chessRow.AtkDamage.Length
                    ? chessRow.AtkDamage[stage].ToString()
                    : "0";

        if (varMagicalAttackText != null)
            varMagicalAttackText.text =
                chessRow.SpellPower != null && stage < chessRow.SpellPower.Length
                    ? chessRow.SpellPower[stage].ToString()
                    : "0";

        if (varArmorText != null)
            varArmorText.text =
                chessRow.Armor != null && stage < chessRow.Armor.Length
                    ? chessRow.Armor[stage].ToString()
                    : "0";

        if (varSpelResistanceText != null)
            varSpelResistanceText.text =
                chessRow.MagicResist != null && stage < chessRow.MagicResist.Length
                    ? chessRow.MagicResist[stage].ToString()
                    : "0";

        if (varAttackSpeedText != null)
            varAttackSpeedText.text =
                chessRow.AtkSpeed != null && stage < chessRow.AtkSpeed.Length
                    ? chessRow.AtkSpeed[stage].ToString("F2")
                    : "0.00";

        if (varMoveSpeedText != null)
            varMoveSpeedText.text = chessRow.MoveSpeed.ToString("F2");

        if (varCriticalChanceText != null)
            varCriticalChanceText.text =
                chessRow.CritRate != null && stage < chessRow.CritRate.Length
                    ? $"{(chessRow.CritRate[stage] * 100):F1}%"
                    : "0%";

        if (varCriticalDamageText != null)
            varCriticalDamageText.text =
                chessRow.CritDamage != null && stage < chessRow.CritDamage.Length
                    ? $"{(chessRow.CritDamage[stage] * 100):F1}%"
                    : "0%";

        // ⭐ 加载技能图标
        LoadSkillIcons(chessRow);

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

        var treasureManager = PlayerAccountDataManager.Instance;
        List<TreasureInstanceData> equippedTreasures = treasureManager.GetChessEquipments(
            m_CurrentSelectedChessId
        );

        // 累加所有宝物的基础属性
        var totalBaseAttrs = new Dictionary<AttributeType, float>();
        // 收集所有特殊效果
        var specialEffectLines = new StringBuilder();

        if (equippedTreasures != null && equippedTreasures.Count > 0)
        {
            foreach (var treasure in equippedTreasures)
            {
                // 1. 基础属性：从 ItemManager 获取 TreasureData.BaseAttributes 累加
                var itemMgr = ItemManager.Instance;
                TreasureData treasureData = itemMgr != null ? itemMgr.GetTreasureData(treasure.TreasureId) : null;
                if (treasureData != null && treasureData.BaseAttributes != null)
                {
                    foreach (var kv in treasureData.BaseAttributes)
                    {
                        if (totalBaseAttrs.ContainsKey(kv.Key))
                            totalBaseAttrs[kv.Key] += kv.Value;
                        else
                            totalBaseAttrs[kv.Key] = kv.Value;
                    }
                }

                // 2. 特殊效果：从 ItemManager 获取 SpecialEffectData
                if (treasure.TreasureId > 0 && treasureData != null && treasureData.SpecialEffectId > 0)
                {
                    SpecialEffectData effectData = itemMgr != null ? itemMgr.GetSpecialEffectData(treasureData.SpecialEffectId) : null;
                    if (effectData != null && !string.IsNullOrEmpty(effectData.Description))
                    {
                        specialEffectLines.Append($"• {effectData.Name}: {effectData.Description}\n");
                    }
                }
            }
        }

        // 更新 BaseEffect：显示累加后的基础属性
        if (varBaseEffect != null)
        {
            if (totalBaseAttrs.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var kv in totalBaseAttrs)
                {
                    string attrName = GetAttributeTypeName(kv.Key);
                    string valueStr = IsPercentAttribute(kv.Key)
                        ? $"{kv.Value * 100:F1}%"
                        : kv.Value.ToString("F0");
                    sb.Append($"• {attrName}: +{valueStr}\n");
                }
                varBaseEffect.text = sb.ToString().TrimEnd();
            }
            else
            {
                varBaseEffect.text = "未装备宝物";
            }
        }

        // 更新 SpecialEffect：显示所有特殊效果
        if (varSpecialEffect != null)
        {
            varSpecialEffect.text = specialEffectLines.Length > 0
                ? specialEffectLines.ToString().TrimEnd()
                : "无特殊效果";
        }
    }

    private static string GetAttributeTypeName(AttributeType type)
    {
        return type switch
        {
            AttributeType.MaxHP         => "生命值",
            AttributeType.Attack        => "攻击力",
            AttributeType.MaxMP         => "法力值",
            AttributeType.AttackSpeed   => "攻击速度",
            AttributeType.CritRate      => "暴击率",
            AttributeType.CritDamage    => "暴击伤害",
            AttributeType.Defense       => "护甲",
            AttributeType.MagicResist   => "魔法抗性",
            AttributeType.SpellPower    => "法术强度",
            AttributeType.MoveSpeed     => "移动速度",
            AttributeType.CooldownReduce => "冷却缩减",
            _                           => type.ToString(),
        };
    }

    private static bool IsPercentAttribute(AttributeType type)
    {
        return type is AttributeType.CritRate or AttributeType.CritDamage or AttributeType.CooldownReduce;
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

        // 同步 ScrollRect 的 content 到当前显示的列表
        if (varLeftScroll != null)
        {
            if (m_IsShowingChessList)
                varLeftScroll.content = varChessContent != null ? varChessContent.GetComponent<RectTransform>() : null;
            else
                varLeftScroll.content = varTreasureContent != null ? varTreasureContent.GetComponent<RectTransform>() : null;
            varLeftScroll.verticalNormalizedPosition = 1f; // 回到顶部
        }

        // ⭐ 更新 Title 和图标
        UpdateTitleAndIcons();

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
        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[BindTreasureToSlot] 开始绑定宝物: slotScript={(slotScript != null ? "有效" : "null")}, treasure={(treasure != null ? $"ID={treasure.TreasureId}" : "null")}"
        );

        if (slotScript == null || treasure == null)
        {
            DebugEx.Warning(
                nameof(CharacterBagUI),
                "[BindTreasureToSlot] slotScript 或 treasure 为 null，跳过"
            );
            return;
        }

        // 获取宝物配置
        IDataTable<TreasureTable> dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();
        TreasureTable treasureRow = dtTreasure.GetDataRow(treasure.TreasureId);

        if (treasureRow == null)
        {
            DebugEx.Error(
                nameof(CharacterBagUI),
                $"[BindTreasureToSlot] 配置表中找不到宝物: TreasureId={treasure.TreasureId}"
            );
            return;
        }

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[BindTreasureToSlot] 开始加载TreasureItemUI: TreasureId={treasure.TreasureId}, Name={treasureRow.Name}"
        );

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

        // ⭐ 更新标签按钮图标
        UpdateTabButtonIcons();

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

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[UpdateTreasureSlots] 开始刷新宝物槽位: chessId={m_CurrentSelectedChessId}, 已装备宝物数={equippedTreasures.Count}"
        );

        for (int i = 0; i < varTreasureSlot1Arr.Length; i++)
        {
            RectTransform slotRect = varTreasureSlot1Arr[i];
            if (slotRect == null)
                continue;

            TreasureInstanceData treasure =
                i < equippedTreasures.Count ? equippedTreasures[i] : null;

            // 槽位始终显示（无论是否有宝物）
            slotRect.gameObject.SetActive(true);

            // ⭐ 清理旧的交互组件（避免重复添加）
            CleanupSlotInteractionHandlers(slotRect);

            if (treasure != null && treasure.TreasureId > 0)
            {
                // 有宝物：加载或更新 TreasureItemUI
                DebugEx.Log(
                    nameof(CharacterBagUI),
                    $"[UpdateTreasureSlots] 槽位 {i} 加载宝物: treasureId={treasure.TreasureId}, instanceId={treasure.InstanceId}"
                );

                LoadTreasureItemUIToSlot(slotRect, treasure.TreasureId);

                // ⭐ 添加右键点击处理器（卸下宝物）
                AddTreasureSlotRightClickHandler(slotRect, treasure.InstanceId);

                // ⭐ 添加交互处理器（左键点击/悬浮显示提示框）
                AddTreasureSlotInteractionHandler(slotRect, treasure.TreasureId);

                // 添加 TreasureSlotDropHandler 组件用于拖拽检测
                if (slotRect.GetComponent<TreasureSlotDropHandler>() == null)
                {
                    slotRect.gameObject.AddComponent<TreasureSlotDropHandler>();
                }
            }
            else
            {
                // 没有宝物时，隐藏TreasureItemUI（但保留容器）
                TreasureItemUI treasureItemUI = slotRect.GetComponentInChildren<TreasureItemUI>(
                    true
                );
                if (treasureItemUI != null)
                {
                    treasureItemUI.gameObject.SetActive(false);
                    DebugEx.Log(
                        nameof(CharacterBagUI),
                        $"[UpdateTreasureSlots] 槽位 {i} 隐藏宝物UI"
                    );
                }

                // 添加 TreasureSlotDropHandler 组件用于拖拽检测（空槽位也可以接收拖拽）
                if (slotRect.GetComponent<TreasureSlotDropHandler>() == null)
                {
                    slotRect.gameObject.AddComponent<TreasureSlotDropHandler>();
                }
            }
        }

        DebugEx.Success(
            nameof(CharacterBagUI),
            $"[UpdateTreasureSlots] 刷新完成: chessId={m_CurrentSelectedChessId}"
        );
    }

    /// <summary>
    /// 加载 TreasureItemUI 到宝物槽位
    /// </summary>
    private void LoadTreasureItemUIToSlot(RectTransform slotRect, int treasureId)
    {
        if (slotRect == null || treasureId <= 0)
            return;

        // 检查是否已经有 TreasureItemUI
        TreasureItemUI existingItemUI = slotRect.GetComponentInChildren<TreasureItemUI>(true);

        if (existingItemUI != null)
        {
            // 复用现有的 TreasureItemUI
            existingItemUI.InitTreasure(treasureId);
            existingItemUI.gameObject.SetActive(true);
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[LoadTreasureItemUIToSlot] 复用现有 TreasureItemUI: treasureId={treasureId}"
            );
        }
        else
        {
            // 需要实例化新的 TreasureItemUI
            // 使用 varTreasureItemUI 字段引用的预制体
            if (varTreasureItemUI == null)
            {
                DebugEx.Error(
                    nameof(CharacterBagUI),
                    "[LoadTreasureItemUIToSlot] varTreasureItemUI 预制体引用为空，请在 Unity Editor 中配置"
                );
                return;
            }

            // 实例化预制体
            GameObject treasureItemObj = Instantiate(varTreasureItemUI, slotRect);
            treasureItemObj.name = "TreasureItemUI";

            // 设置大小和位置（铺满整个槽位）
            RectTransform itemRect = treasureItemObj.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchorMin = Vector2.zero;
                itemRect.anchorMax = Vector2.one;
                itemRect.offsetMin = Vector2.zero;
                itemRect.offsetMax = Vector2.zero;
            }

            // 初始化宝物数据
            TreasureItemUI treasureItemUI = treasureItemObj.GetComponent<TreasureItemUI>();
            if (treasureItemUI != null)
            {
                treasureItemUI.InitTreasure(treasureId);
                treasureItemUI.gameObject.SetActive(true);
                DebugEx.Success(
                    nameof(CharacterBagUI),
                    $"[LoadTreasureItemUIToSlot] 实例化新 TreasureItemUI: treasureId={treasureId}"
                );
            }
            else
            {
                DebugEx.Error(
                    nameof(CharacterBagUI),
                    "[LoadTreasureItemUIToSlot] TreasureItemUI 预制体缺少 TreasureItemUI 组件"
                );
                Destroy(treasureItemObj);
            }
        }
    }

    /// <summary>
    /// 清理槽位上的旧交互组件（避免重复添加）
    /// </summary>
    private void CleanupSlotInteractionHandlers(RectTransform slotRect)
    {
        if (slotRect == null)
            return;

        // 移除旧的右键点击处理器
        var oldRightClickHandler = slotRect.GetComponent<TreasureSlotRightClickHandler>();
        if (oldRightClickHandler != null)
        {
            Destroy(oldRightClickHandler);
        }

        // 移除旧的交互处理器
        var oldInteractionHandler = slotRect.GetComponent<TreasureSlotInteractionHandler>();
        if (oldInteractionHandler != null)
        {
            Destroy(oldInteractionHandler);
        }

        // ⭐ 移除旧的拖拽处理器（TreasureSlot 不支持拖拽发起）
        var oldDragHandler = slotRect.GetComponent<TreasureDragHandler>();
        if (oldDragHandler != null)
        {
            Destroy(oldDragHandler);
        }
    }

    /// <summary>
    /// 添加右键点击处理器（卸下宝物）
    /// </summary>
    private void AddTreasureSlotRightClickHandler(RectTransform slotRect, int treasureInstanceId)
    {
        if (slotRect == null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureSlotRightClickHandler>();
        handler.Initialize(treasureInstanceId, OnTreasureSlotRightClicked);

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[AddTreasureSlotRightClickHandler] 添加右键点击处理器: instanceId={treasureInstanceId}"
        );
    }

    /// <summary>
    /// 添加交互处理器（左键点击/悬浮显示提示框）
    /// </summary>
    private void AddTreasureSlotInteractionHandler(RectTransform slotRect, int treasureId)
    {
        if (slotRect == null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureSlotInteractionHandler>();
        handler.Initialize(treasureId);

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[AddTreasureSlotInteractionHandler] 添加交互处理器: treasureId={treasureId}"
        );
    }

    /// <summary>
    /// 为宝物仓库的格子添加拖拽处理器（仅用于宝物仓库，不用于TreasureSlot）
    /// </summary>
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

    #region UI更新辅助方法

    /// <summary>
    /// 更新 Title 文本和图标显示
    /// </summary>
    private void UpdateTitleAndIcons()
    {
        if (m_IsShowingChessList)
        {
            // 显示角色列表
            if (varTitle != null)
                varTitle.text = "角色列表";

            // 显示宝物图标（提示可以切换到宝物列表）
            if (varTreasureIcon != null)
                varTreasureIcon.gameObject.SetActive(true);

            // 隐藏角色图标
            if (varChessIcon != null)
                varChessIcon.gameObject.SetActive(false);

            DebugEx.Log(nameof(CharacterBagUI), "[UpdateTitleAndIcons] 显示角色列表，显示宝物图标");
        }
        else
        {
            // 显示宝物列表
            if (varTitle != null)
                varTitle.text = "宝物列表";

            // 隐藏宝物图标
            if (varTreasureIcon != null)
                varTreasureIcon.gameObject.SetActive(false);

            // 显示角色图标（提示可以切换回角色列表）
            if (varChessIcon != null)
                varChessIcon.gameObject.SetActive(true);

            DebugEx.Log(nameof(CharacterBagUI), "[UpdateTitleAndIcons] 显示宝物列表，显示角色图标");
        }
    }

    /// <summary>
    /// 更新标签按钮图标（选中/未选中状态）
    /// </summary>
    private void UpdateTabButtonIcons()
    {
        // 资源 ID
        const int SELECTED_ICON_ID = 1011; // 选中状态图标
        const int UNSELECTED_ICON_ID = 1010; // 未选中状态图标

        // 更新 StateBtn
        if (varStateBtn != null)
        {
            int iconId = m_CurrentTabIndex == 0 ? SELECTED_ICON_ID : UNSELECTED_ICON_ID;
            LoadButtonIcon(varStateBtn, iconId);
        }

        // 更新 TreasureBtn
        if (varTreasureBtn != null)
        {
            int iconId = m_CurrentTabIndex == 1 ? SELECTED_ICON_ID : UNSELECTED_ICON_ID;
            LoadButtonIcon(varTreasureBtn, iconId);
        }

        // 更新 LevelUpBtn
        if (varLevelUpBtn != null)
        {
            int iconId = m_CurrentTabIndex == 2 ? SELECTED_ICON_ID : UNSELECTED_ICON_ID;
            LoadButtonIcon(varLevelUpBtn, iconId);
        }

        // 更新 StoryBtn
        if (varStoryBtn != null)
        {
            int iconId = m_CurrentTabIndex == 3 ? SELECTED_ICON_ID : UNSELECTED_ICON_ID;
            LoadButtonIcon(varStoryBtn, iconId);
        }

        DebugEx.Log(
            nameof(CharacterBagUI),
            $"[UpdateTabButtonIcons] 更新标签按钮图标，当前选中: {m_CurrentTabIndex}"
        );
    }

    /// <summary>
    /// 加载按钮图标
    /// </summary>
    private void LoadButtonIcon(Button button, int iconId)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            _ = ResourceExtension.LoadSpriteAsync(iconId, image);
        }
    }

    /// <summary>
    /// 加载技能图标
    /// </summary>
    private void LoadSkillIcons(SummonChessTable chessRow)
    {
        if (chessRow == null)
            return;

        // 加载被动技能图标
        if (
            varPassiveSkill != null
            && chessRow.PassiveIds != null
            && chessRow.PassiveIds.Length > 0
        )
        {
            int passiveSkillId = chessRow.PassiveIds[0];
            LoadSkillIcon(varPassiveSkill, passiveSkillId);
        }

        // 加载普通攻击图标
        if (varNormalAtk != null && chessRow.NormalAtkId != null && chessRow.NormalAtkId.Length > 0)
        {
            int normalAtkId = chessRow.NormalAtkId[0];
            LoadSkillIcon(varNormalAtk, normalAtkId);
        }

        // 加载技能1图标
        if (varSkill_1 != null && chessRow.Skill1Id != null && chessRow.Skill1Id.Length > 0)
        {
            int skill1Id = chessRow.Skill1Id[0];
            LoadSkillIcon(varSkill_1, skill1Id);
        }

        // 加载技能2图标
        if (varSkill_2 != null && chessRow.Skill2Id != null && chessRow.Skill2Id.Length > 0)
        {
            int skill2Id = chessRow.Skill2Id[0];
            LoadSkillIcon(varSkill_2, skill2Id);
        }

        // 加载大招图标
        if (
            varUltimateSkill != null
            && chessRow.UltimateId != null
            && chessRow.UltimateId.Length > 0
        )
        {
            int ultimateId = chessRow.UltimateId[0];
            LoadSkillIcon(varUltimateSkill, ultimateId);
        }

        DebugEx.Log(nameof(CharacterBagUI), $"[LoadSkillIcons] 加载技能图标: {chessRow.Name}");
    }

    /// <summary>
    /// 加载单个技能图标
    /// </summary>
    private void LoadSkillIcon(Button skillButton, int skillId)
    {
        if (skillButton == null || skillId <= 0)
            return;

        // 从 SummonChessSkillTable 获取技能数据
        var skillRow = m_DtSkill?.GetDataRow(skillId);
        if (skillRow == null)
        {
            DebugEx.Warning(
                nameof(CharacterBagUI),
                $"[LoadSkillIcon] 未找到技能数据: skillId={skillId}"
            );
            return;
        }

        // 获取技能图标 ID（IconId 是 int 类型，不是数组）
        int iconId = skillRow.IconId;

        if (iconId <= 0)
        {
            DebugEx.Warning(
                nameof(CharacterBagUI),
                $"[LoadSkillIcon] 技能图标ID无效: skillId={skillId}, iconId={iconId}"
            );
            return;
        }

        // 加载图标到按钮的 Image 组件
        var image = skillButton.GetComponent<Image>();
        if (image != null)
        {
            _ = ResourceExtension.LoadSpriteAsync(iconId, image);
            DebugEx.Log(
                nameof(CharacterBagUI),
                $"[LoadSkillIcon] 加载技能图标: skillId={skillId}, iconId={iconId}"
            );
        }
    }

    #endregion

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
