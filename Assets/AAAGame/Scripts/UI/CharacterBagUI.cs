using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using GameExtension;
using Cysharp.Threading.Tasks;
using GameFramework.DataTable;

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

    private IDataTable<SummonChessTable> m_DtSummonChess;
    private IDataTable<SummonChessSkillTable> m_DtSkill;

    #endregion

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 获取数据表
        m_DtSummonChess = GF.DataTable.GetDataTable<SummonChessTable>();
        m_DtSkill = GF.DataTable.GetDataTable<SummonChessSkillTable>();

        // 注册事件
        RegisterEvents();

        // 初始化 UI
        LoadChessListAsync().Forget();
    }

    private void RegisterEvents()
    {
        // 关闭按钮
        if (varCloseBtn != null)
            varCloseBtn.onClick.AddListener(() => GF.UI.CloseUIForm(this.UIForm));

        // 左侧列表切换
        if (varTreasureSwitchBtn != null)
            varTreasureSwitchBtn.onClick.AddListener(OnTreasureSwitchBtnClicked);

        // 立绘/模型切换
        if (varSwitchBtn != null)
            varSwitchBtn.onClick.AddListener(OnSwitchBtnClicked);

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
            if (saveData == null || saveData.OwnedUnitCardIds == null || saveData.OwnedUnitCardIds.Count == 0)
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

            DebugEx.Success(nameof(CharacterBagUI), $"棋子列表加载完成，共 {m_ChessItemPool.Count} 个");
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

        DebugEx.Log(nameof(CharacterBagUI), $"选中棋子 {chessId}");
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
        UpdateLevelUpTab(chessRow, 1);
        UpdateStoryTab(chessRow, 1);
    }

    private void UpdateStateTab(SummonChessTable chessRow)
    {
        if (varNameText != null)
            varNameText.text = chessRow.Name;

        // 显示一阶的属性
        if (varStateText != null)
        {
            string stateInfo = $"HP: {chessRow.MaxHp[0]}\n攻击: {chessRow.AtkDamage[0]}\n防御: {chessRow.Armor[0]}\n魔抗: {chessRow.MagicResist[0]}";
            varStateText.text = stateInfo;
        }

        // 根据 Skill2Id 决定是否显示 Skill_2 按钮
        if (varSkill_2 != null)
        {
            bool hasSkill2 = chessRow.Skill2Id != null && chessRow.Skill2Id.Length > 0 && chessRow.Skill2Id[0] != 0;
            varSkill_2.gameObject.SetActive(hasSkill2);
        }
    }

    private void UpdateTreasureTab()
    {
        if (m_CurrentSelectedChessId <= 0)
            return;

        // 获取该棋子装备的宝物列表
        var treasureManager = PlayerAccountDataManager.Instance;
        List<TreasureInstanceData> equippedTreasures = treasureManager.GetChessEquipments(m_CurrentSelectedChessId);

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
                        string valueStr = affix.ValueType == ValueType.Percent
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
            varBaseEffect.text = treasureNamesText.Length > 0
                ? treasureNamesText.ToString().TrimEnd()
                : "未装备宝物";
        }

        if (varSpecialEffect != null)
        {
            varSpecialEffect.text = baseAttributesText.Length > 0
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
            string baseInfo = $"HP: {chessRow.MaxHp[stage]}\n攻击: {chessRow.AtkDamage[stage]}\n防御: {chessRow.Armor[stage]}\n魔抗: {chessRow.MagicResist[stage]}";
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
        m_IsShowingChessList = !m_IsShowingChessList;

        if (varChessContent != null)
            varChessContent.gameObject.SetActive(m_IsShowingChessList);

        if (varTreasureContent != null)
            varTreasureContent.gameObject.SetActive(!m_IsShowingChessList);

        if (!m_IsShowingChessList)
        {
            LoadTreasureRepositoryAsync().Forget();
        }

        DebugEx.Log(nameof(CharacterBagUI), $"切换到 {(m_IsShowingChessList ? "棋子列表" : "宝物仓库")}");
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

            DebugEx.Success(nameof(CharacterBagUI), $"宝物仓库加载完成，共 {m_TreasureSlotPool.Count} 个槽位，{allTreasures.Count} 个宝物");
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

        // 在槽位中显示宝物
        GameObject treasureItemContainer = slotScript.GetTreasureItemUIContainer();
        if (treasureItemContainer != null)
        {
            TreasureItemUI treasureItem = treasureItemContainer.GetComponent<TreasureItemUI>();
            if (treasureItem != null)
            {
                treasureItem.InitTreasure(treasure.TreasureId);
            }
        }

        // 为所有宝物添加详情查看点击处理
        AddTreasureDetailClickHandler(slotScript, treasure.InstanceId);

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

            // 为未装备的宝物添加点击处理（快速装备）
            AddTreasureEquipClickHandler(slotScript, treasure.InstanceId);

            // 为未装备的宝物添加拖拽处理
            AddTreasureDragHandler(slotScript, treasure.InstanceId, true);
        }
    }

    private void AddTreasureDragHandler(InventorySlotUI slotScript, int treasureInstanceId, bool isFromInventory)
    {
        var slotRect = slotScript.GetComponent<RectTransform>();
        if (slotRect == null) return;

        // 检查是否已有处理器
        if (slotRect.GetComponent<TreasureDragHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureDragHandler>();
        handler.Initialize(treasureInstanceId, isFromInventory, slotRect);
    }

    private void OnSwitchBtnClicked()
    {
        // 切换立绘/模型显示
        if (varNormalImage != null && varOccupationImage != null)
        {
            bool isShowingPortrait = varNormalImage.gameObject.activeInHierarchy;
            varNormalImage.gameObject.SetActive(!isShowingPortrait);
            varOccupationImage.gameObject.SetActive(isShowingPortrait);

            DebugEx.Log(nameof(CharacterBagUI), $"切换到 {(!isShowingPortrait ? "立绘" : "模型")}");
        }
    }

    private void OnTabButtonClicked(int tabIndex)
    {
        m_CurrentTabIndex = tabIndex;

        // 隐藏所有标签页
        if (varStateUI != null) varStateUI.gameObject.SetActive(tabIndex == 0);
        if (varTreasureUI != null) varTreasureUI.gameObject.SetActive(tabIndex == 1);
        if (varLevelUpUI != null) varLevelUpUI.gameObject.SetActive(tabIndex == 2);
        if (varStoryUI != null) varStoryUI.gameObject.SetActive(tabIndex == 3);

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
            _ => 0
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
                btnImage.color = i == m_CurrentLevelStage
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
        List<TreasureInstanceData> equippedTreasures = treasureManager.GetChessEquipments(m_CurrentSelectedChessId);
        IDataTable<TreasureTable> dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();

        for (int i = 0; i < varTreasureSlot1Arr.Length; i++)
        {
            RectTransform slotRect = varTreasureSlot1Arr[i];
            if (slotRect == null) continue;

            TreasureInstanceData treasure = i < equippedTreasures.Count ? equippedTreasures[i] : null;

            if (treasure != null)
            {
                TreasureTable treasureRow = dtTreasure.GetDataRow(treasure.TreasureId);
                if (treasureRow != null)
                {
                    TreasureItemUI treasureItemUI = slotRect.GetComponentInChildren<TreasureItemUI>();
                    if (treasureItemUI == null)
                    {
                        treasureItemUI = slotRect.gameObject.AddComponent<TreasureItemUI>();
                    }

                    treasureItemUI.InitTreasure(treasure.TreasureId);
                    slotRect.gameObject.SetActive(true);

                    // 为已装备的宝物添加详情查看点击处理
                    AddTreasureDetailClickHandler(slotRect, treasure.InstanceId);

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
                slotRect.gameObject.SetActive(false);
            }
        }

        DebugEx.Log(nameof(CharacterBagUI), $"刷新棋子 {m_CurrentSelectedChessId} 的宝物槽位");
    }

    private void AddTreasureDragHandler(RectTransform slotRect, int treasureInstanceId, bool isFromInventory)
    {
        if (slotRect == null) return;

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

    private void AddTreasureEquipClickHandler(InventorySlotUI slotScript, int treasureInstanceId)
    {
        var slotRect = slotScript.GetComponent<RectTransform>();
        if (slotRect == null) return;

        if (slotRect.GetComponent<TreasureEquipClickHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureEquipClickHandler>();
        handler.Initialize(treasureInstanceId, OnTreasureEquipClicked);
    }

    private void AddTreasureDetailClickHandler(InventorySlotUI slotScript, int treasureInstanceId)
    {
        var slotRect = slotScript.GetComponent<RectTransform>();
        if (slotRect == null) return;

        if (slotRect.GetComponent<TreasureDetailClickHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureDetailClickHandler>();
        handler.Initialize(treasureInstanceId, slotRect);
    }

    private void AddTreasureDetailClickHandler(RectTransform slotRect, int treasureInstanceId)
    {
        if (slotRect == null) return;

        if (slotRect.GetComponent<TreasureDetailClickHandler>() != null)
            return;

        var handler = slotRect.gameObject.AddComponent<TreasureDetailClickHandler>();
        handler.Initialize(treasureInstanceId, slotRect);
    }

    public int GetCurrentSelectedChessId() => m_CurrentSelectedChessId;

    public void RefreshTreasureUI()
    {
        UpdateTreasureSlots();
        UpdateTreasureTab();
        LoadTreasureRepositoryAsync().Forget();
    }

    private void OnTreasureEquipClicked(int treasureInstanceId)
    {
        if (m_CurrentSelectedChessId <= 0)
        {
            DebugEx.Warning(nameof(CharacterBagUI), "未选中棋子，无法装备宝物");
            return;
        }

        var treasureManager = PlayerAccountDataManager.Instance;
        treasureManager.EquipTreasure(treasureInstanceId, m_CurrentSelectedChessId);
        treasureManager.SaveCurrentSave();

        DebugEx.Log(nameof(CharacterBagUI), $"装备宝物 {treasureInstanceId} 到棋子 {m_CurrentSelectedChessId}");

        UpdateTreasureSlots();
        UpdateTreasureTab();
        LoadTreasureRepositoryAsync().Forget();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

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
