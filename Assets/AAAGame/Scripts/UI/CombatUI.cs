using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework.Event;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class CombatUI : StateAwareUIForm
{
    #region 字段

    /// <summary>⭐ 新增：当前显示详情的棋子实体</summary>
    private ChessEntity m_CurrentDetailChess;

    /// <summary>装备槽列表（预创建固定数量）</summary>
    private List<InventorySlotUI> m_EquipSlots = new List<InventorySlotUI>();

    /// <summary>装备槽容器</summary>
    private InventorySlotContainerImpl m_EquipSlotContainer;

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        DebugEx.LogModule("CombatUI", "订阅战斗状态事件");
        GF.Event.Subscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        // 订阅运行时数据变化事件
        SubscribeRuntimeDataEvents();

        // 订阅棋子选中事件
        ChessSelectionManager.OnChessSelected += OnChessSelectedForDetail;
        ChessSelectionManager.OnChessDeselected += OnChessDeselectedForDetail;
    }

    protected override void UnsubscribeEvents()
    {
        DebugEx.LogModule("CombatUI", "取消订阅战斗状态事件");
        GF.Event.Unsubscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        // 取消订阅运行时数据变化事件
        UnsubscribeRuntimeDataEvents();

        // 取消订阅棋子选中事件
        ChessSelectionManager.OnChessSelected -= OnChessSelectedForDetail;
        ChessSelectionManager.OnChessDeselected -= OnChessDeselectedForDetail;
    }

    /// <summary>
    /// 订阅运行时数据变化事件
    /// </summary>
    private void SubscribeRuntimeDataEvents()
    {
        // 订阅召唤师HP/MP变化事件
        if (SummonerRuntimeDataManager.Instance != null)
        {
            SummonerRuntimeDataManager.Instance.OnHPChanged += OnSummonerHPChanged;
            SummonerRuntimeDataManager.Instance.OnMPChanged += OnSummonerMPChanged;
        }
    }

    /// <summary>
    /// 取消订阅运行时数据变化事件
    /// </summary>
    private void UnsubscribeRuntimeDataEvents()
    {
        // 取消订阅召唤师HP/MP变化事件
        if (SummonerRuntimeDataManager.Instance != null)
        {
            SummonerRuntimeDataManager.Instance.OnHPChanged -= OnSummonerHPChanged;
            SummonerRuntimeDataManager.Instance.OnMPChanged -= OnSummonerMPChanged;
        }
    }

    #endregion

    #region 事件处理

    private void OnCombatEnter(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("CombatUI", "收到战斗进入事件");

        // ⭐ 新增：创建卡牌预览管理器
        CreateCardPreviewManager();

        // 初始化 CardManager
        if (CardManager.Instance != null)
        {
            CardManager.Instance.InitializeForCombat();
            // 订阅卡牌移除事件，自动刷新卡槽
            CardManager.Instance.OnCardRemoved += OnCardRemoved;
            DebugEx.LogModule("CombatUI", "CardManager 已初始化");
        }

        // 订阅背包数据变化事件
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += OnInventoryChanged;

        // 初始化装备槽（仅首次进入战斗）
        if (m_EquipSlots.Count == 0)
            InitializeEquipSlots();

        ShowUI();
        RefreshCombatUI();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        DebugEx.LogModule("CombatUI", "收到战斗离开事件");

        // ⭐ 新增：销毁卡牌预览管理器
        if (CardPreviewDisplayShader.Instance != null)
        {
            Destroy(CardPreviewDisplayShader.Instance.gameObject);
            DebugEx.LogModule("CombatUI", "卡牌预览管理器已销毁");
        }

        // 清理 CardManager
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardRemoved -= OnCardRemoved;
            CardManager.Instance.Clear();
            DebugEx.LogModule("CombatUI", "CardManager 已清理");
        }

        // 取消订阅背包数据变化事件
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= OnInventoryChanged;

        // 清理 CardSlotContainer 的状态
        var container = GetCardSlotContainer();
        if (container != null)
        {
            container.ClearState();
        }

        HideUI();
    }

    /// <summary>
    /// 创建卡牌预览管理器（生成到 WorldCanvas 下）
    /// </summary>
    private void CreateCardPreviewManager()
    {
        // 检查是否已存在
        if (CardPreviewDisplayShader.Instance != null)
            return;

        // 查找 WorldCanvas
        var worldCanvas = FindObjectOfType<Canvas>();
        Transform parentTransform = transform.parent;

        // 尝试找到名字为 "WorldCanvas" 的 Canvas
        var allCanvas = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvas)
        {
            if (canvas.gameObject.name == "WorldCanvas")
            {
                parentTransform = canvas.transform;
                break;
            }
        }

        var go = new GameObject("CardPreviewDisplayShader");
        go.transform.SetParent(parentTransform);
        go.AddComponent<CardPreviewDisplayShader>();

        DebugEx.LogModule("CombatUI", "卡牌预览管理器已创建");
    }

    /// <summary>
    /// 卡牌移除事件回调
    /// </summary>
    private void OnCardRemoved(int cardId)
    {
        DebugEx.LogModule("CombatUI", $"卡牌已移除: ID={cardId}，更新卡槽");

        // 查找被移除的卡牌对应的 UI 并移除
        var container = GetCardSlotContainer();
        if (container == null)
            return;

        var cardSlots = varCardSlots.transform.GetComponentsInChildren<CardSlotItem>();
        foreach (var slot in cardSlots)
        {
            if (slot.GetCardData()?.CardId == cardId)
            {
                container.RemoveCard(slot);
                Destroy(slot.gameObject);
                break;
            }
        }
    }
    /// <summary>
    /// 获取详情UI
    /// </summary>
    public DetailInfoUI GetDetailInfoUI()
    {
        if (varDetailInfoUI != null)
        {
            return varDetailInfoUI.GetComponent<DetailInfoUI>();
        }
        return null;
    }

    /// <summary>
    /// 获取卡槽吸附区域（使用绿色区域作为吸附检测）
    /// </summary>
    public Image GetCardSlotAdsorptionArea()
    {
        return varGreenArea;
    }

    /// <summary>
    /// 获取无效区域预览（红色覆盖，用于碰撞检测）
    /// </summary>
    public Image GetInvalidAreaPreview()
    {
        return varRedArea;
    }

    /// <summary>
    /// 战斗阶段棋子被选中，显示详情
    /// ⭐ 修改：订阅棋子属性变化事件，实现动态更新
    /// </summary>
    private void OnChessSelectedForDetail(ChessEntity entity)
    {
        var detailUI = GetDetailInfoUI();
        if (detailUI == null) return;

        m_CurrentDetailChess = entity;

        // ⭐ 新增：订阅棋子属性变化事件
        if (entity.Attribute != null)
        {
            entity.Attribute.OnHpChanged += OnDetailChessHpChanged;
            entity.Attribute.OnMpChanged += OnDetailChessMpChanged;
            DebugEx.LogModule("CombatUI", $"已订阅棋子 {entity.Config?.Name} 的属性变化事件");
        }

        // 订阅Buff变化事件
        ChessStateEvents.OnBuffAdded += OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved += OnDetailChessBuffChanged;

        detailUI.SetChessUnitData(entity);
        detailUI.RefreshUI();
        detailUI.ShowWithAnimation();
        DebugEx.LogModule("CombatUI", $"显示棋子详情: {entity.Config?.Name}");
    }

    /// <summary>
    /// ⭐ 新增：棋子HP变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessHpChanged(double oldHp, double newHp)
    {
        if (m_CurrentDetailChess == null)
            return;

        var detailUI = GetDetailInfoUI();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            DebugEx.LogModule("CombatUI", $"DetailInfoUI已刷新（HP变化 {oldHp:F0} -> {newHp:F0}）");
        }
    }

    /// <summary>
    /// ⭐ 新增：棋子MP变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessMpChanged(double oldMp, double newMp)
    {
        if (m_CurrentDetailChess == null)
            return;

        var detailUI = GetDetailInfoUI();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            DebugEx.LogModule("CombatUI", $"DetailInfoUI已刷新（MP变化 {oldMp:F0} -> {newMp:F0}）");
        }
    }

    /// <summary>
    /// ⭐ 新增：棋子Buff变化时，动态更新DetailInfoUI
    /// </summary>
    private void OnDetailChessBuffChanged(int chessId, int buffId)
    {
        if (m_CurrentDetailChess == null)
            return;

        // 只更新当前显示的棋子
        if (m_CurrentDetailChess.Config.Id != chessId)
            return;

        var detailUI = GetDetailInfoUI();
        if (detailUI != null)
        {
            detailUI.RefreshUI();
            DebugEx.LogModule("CombatUI", $"DetailInfoUI已刷新（Buff变化 ID={buffId}）");
        }
    }

    /// <summary>
    /// 战斗阶段棋子取消选中，隐藏详情
    /// ⭐ 修改：取消订阅棋子属性变化事件
    /// </summary>
    private void OnChessDeselectedForDetail()
    {
        // ⭐ 新增：取消订阅属性变化事件
        if (m_CurrentDetailChess != null && m_CurrentDetailChess.Attribute != null)
        {
            m_CurrentDetailChess.Attribute.OnHpChanged -= OnDetailChessHpChanged;
            m_CurrentDetailChess.Attribute.OnMpChanged -= OnDetailChessMpChanged;
            DebugEx.LogModule("CombatUI", $"已取消订阅棋子 {m_CurrentDetailChess.Config?.Name} 的属性变化事件");
        }

        ChessStateEvents.OnBuffAdded -= OnDetailChessBuffChanged;
        ChessStateEvents.OnBuffRemoved -= OnDetailChessBuffChanged;

        m_CurrentDetailChess = null;

        if (varDetailInfoUI != null)
        {
            varDetailInfoUI.SetActive(false);
        }
    }

    /// <summary>
    /// 召唤师HP变化回调
    /// </summary>
    private void OnSummonerHPChanged(float oldValue, float newValue)
    {
        RefreshPlayerHP();
    }

    /// <summary>
    /// 召唤师MP变化回调
    /// </summary>
    private void OnSummonerMPChanged(float oldValue, float newValue)
    {
        RefreshPlayerMP();
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新战斗UI
    /// </summary>
    private void RefreshCombatUI()
    {
        RefreshEnemyInfo();
        RefreshPlayerStatus();
        RefreshEquipmentPanel();
        RefreshCardSlots();
        BindButtonEvents();

        DebugEx.LogModule("CombatUI", "战斗UI已刷新");
    }

    /// <summary>
    /// 刷新敌人信息
    /// </summary>
    private void RefreshEnemyInfo()
    {
        if (varEnemyTitle != null)
        {
            varEnemyTitle.SetActive(true);
        }

        if (varEnemyName != null)
        {
            varEnemyName.text = "稻草人";  // TODO: 从战斗数据获取
        }

        if (varEnemyNum != null)
        {
            varEnemyNum.text = "x3";  // TODO: 从战斗数据获取
        }

        if (varEnemyWave != null)
        {
            varEnemyWave.text = "1/3";  // TODO: 从战斗数据获取
        }
    }

    /// <summary>
    /// 刷新玩家状态
    /// </summary>
    private void RefreshPlayerStatus()
    {
        RefreshPlayerHP();
        RefreshPlayerMP();
    }

    /// <summary>
    /// 刷新玩家HP显示
    /// </summary>
    private void RefreshPlayerHP()
    {
        // 刷新HP滑条
        if (varHPSlider != null)
        {
            // 从召唤师运行时数据获取HP
            if (SummonerRuntimeDataManager.Instance != null && SummonerRuntimeDataManager.Instance.IsInitialized)
            {
                varHPSlider.value = SummonerRuntimeDataManager.Instance.HPPercent;
            }
            else
            {
                varHPSlider.value = 1.0f;  // 默认满血
            }
        }

        // 刷新HP文本
        if (varHpText != null)
        {
            if (SummonerRuntimeDataManager.Instance != null && SummonerRuntimeDataManager.Instance.IsInitialized)
            {
                int currentHP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.CurrentHP);
                int maxHP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.MaxHP);
                varHpText.text = $"{currentHP}/{maxHP}";
            }
            else
            {
                varHpText.text = "100/100";  // 默认值
            }
        }
    }

    /// <summary>
    /// 刷新玩家MP显示
    /// </summary>
    private void RefreshPlayerMP()
    {
        // 刷新MP滑条
        if (varMPSlider != null)
        {
            // 从召唤师运行时数据获取MP
            if (SummonerRuntimeDataManager.Instance != null && SummonerRuntimeDataManager.Instance.IsInitialized)
            {
                varMPSlider.value = SummonerRuntimeDataManager.Instance.MPPercent;
            }
            else
            {
                varMPSlider.value = 1.0f;  // 默认满MP
            }
        }

        // 刷新MP文本
        if (varMpText != null)
        {
            if (SummonerRuntimeDataManager.Instance != null && SummonerRuntimeDataManager.Instance.IsInitialized)
            {
                int currentMP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.CurrentMP);
                int maxMP = Mathf.RoundToInt(SummonerRuntimeDataManager.Instance.MaxMP);
                varMpText.text = $"{currentMP}/{maxMP}";
            }
            else
            {
                varMpText.text = "50/50";  // 默认值
            }
        }
    }

    /// <summary>
    /// 初始化装备槽（战斗进入时调用一次）
    /// 预创建 9 个装备槽，始终显示
    /// </summary>
    private void InitializeEquipSlots()
    {
        if (varEquipmentPanel == null || varInventorySlotUI == null)
            return;

        // 初始化容器（如果还未初始化）
        if (m_EquipSlotContainer == null)
        {
            m_EquipSlotContainer = GetComponent<InventorySlotContainerImpl>();
            if (m_EquipSlotContainer == null)
                m_EquipSlotContainer = gameObject.AddComponent<InventorySlotContainerImpl>();
        }

        const int equipSlotsCount = 9;
        for (int i = 0; i < equipSlotsCount; i++)
        {
            var go = Instantiate(varInventorySlotUI, varEquipmentPanel.transform);
            if (!go.TryGetComponent<InventorySlotUI>(out var slotUI))
                continue;

            slotUI.SetSlotIndex(i);
            slotUI.SetContainerType(SlotContainerType.Equip);
            slotUI.SetSlotContainer(m_EquipSlotContainer);
            slotUI.gameObject.SetActive(true);
            slotUI.gameObject.name = $"EquipSlot_{i}";
            m_EquipSlots.Add(slotUI);
        }

        DebugEx.LogModule("CombatUI", $"装备栏初始化完成，共 {m_EquipSlots.Count} 个装备槽");
    }

    /// <summary>
    /// 刷新装备面板：从背包显示装备到预创建的槽位
    /// </summary>
    private void RefreshEquipmentPanel()
    {
        if (m_EquipSlots.Count == 0)
            return;

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            DebugEx.WarningModule("CombatUI", "InventoryManager 未初始化");
            return;
        }

        var itemTable = GF.DataTable.GetDataTable<ItemTable>();
        var allSlots = inventoryManager.GetAllSlots();
        int displayIndex = 0;

        // 遍历背包所有槽位，过滤装备类型并填充到预创建的槽位
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot.IsEmpty)
                continue;

            var row = itemTable?.GetDataRow(slot.ItemStack.Item.ItemId);
            if (row == null || row.Type != (int)ItemType.Equipment)
                continue;

            if (displayIndex >= m_EquipSlots.Count)
                break;

            m_EquipSlots[displayIndex].SetData(slot.ItemStack);
            displayIndex++;
        }

        // 清空未填充的槽位
        for (int i = displayIndex; i < m_EquipSlots.Count; i++)
        {
            m_EquipSlots[i].SetData(null);
        }

        DebugEx.LogModule("CombatUI", $"装备面板已刷新，显示 {displayIndex} 个装备");
    }

    /// <summary>
    /// 背包数据变化回调
    /// </summary>
    private void OnInventoryChanged()
    {
        RefreshEquipmentPanel();
    }

    /// <summary>
    /// 刷新卡牌槽
    /// </summary>
    private void RefreshCardSlots()
    {
        if (varCardSlots == null || varCardSlotItem == null)
        {
            return;
        }

        var container = GetCardSlotContainer();
        if (container == null)
        {
            DebugEx.ErrorModule("CombatUI", "未找到 CardSlotContainer 组件");
            return;
        }

        // 清理所有卡牌槽
        for (int i = varCardSlots.transform.childCount - 1; i >= 0; i--)
        {
            var child = varCardSlots.transform.GetChild(i);
            if (child.gameObject != varCardSlotItem)
            {
                Destroy(child.gameObject);
            }
        }

        // 从 CardManager 获取卡牌列表并创建卡牌槽
        if (CardManager.Instance != null)
        {
            var cards = CardManager.Instance.GetAvailableCards();
            for (int i = 0; i < cards.Count; i++)
            {
                CreateCardSlot(cards[i], i, container).Forget();
            }

            DebugEx.LogModule("CombatUI", $"刷新卡牌槽完成，共 {cards.Count} 张卡牌");
        }
        else
        {
            DebugEx.WarningModule("CombatUI", "CardManager 未初始化");
        }
    }

    /// <summary>
    /// 创建卡牌槽
    /// </summary>
    private async UniTaskVoid CreateCardSlot(CardData cardData, int index, CardSlotContainer container)
    {
        GameObject slotGo = Instantiate(varCardSlotItem, varCardSlots.transform);
        slotGo.SetActive(true);
        slotGo.name = $"CardSlot_{index}";

        CardSlotItem slotItem = slotGo.GetComponent<CardSlotItem>();
        if (slotItem != null)
        {
            slotItem.SetData(cardData);
            // 通过容器添加卡牌，播放进场动画
            await container.AddCardAsync(slotItem);
        }
    }

    /// <summary>
    /// 获取卡牌容器
    /// </summary>
    private CardSlotContainer GetCardSlotContainer()
    {
        return varCardSlots?.GetComponent<CardSlotContainer>();
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 人口按钮
        if (varBtn_Population != null)
        {
            varBtn_Population.onClick.RemoveAllListeners();
            varBtn_Population.onClick.AddListener(OnPopulationButtonClicked);
        }

        // 刷新按钮
        if (varBtn_Refresh != null)
        {
            varBtn_Refresh.onClick.RemoveAllListeners();
            varBtn_Refresh.onClick.AddListener(OnRefreshButtonClicked);
        }

        // 召唤师技能按钮
        if (varBtn1Arr != null)
        {
            for (int i = 0; i < varBtn1Arr.Length; i++)
            {
                int index = i;  // 闭包捕获
                varBtn1Arr[i].onClick.RemoveAllListeners();
                varBtn1Arr[i].onClick.AddListener(() => OnSummonerSkillClicked(index));
            }
        }

        // 加载技能图标（异步）
        RefreshSummonerSkillButtonsAsync().Forget();

        // 设置消耗数值
        if (varConsumeNum_Population != null)
        {
            varConsumeNum_Population.text = "2";  // TODO: 从配置获取
        }

        if (varConsumeNum_Refresh != null)
        {
            varConsumeNum_Refresh.text = "1";  // TODO: 从配置获取
        }

        if (varComNumText != null)
        {
            varComNumText.text = "30";  // TODO: 从战斗数据获取
        }
    }

    /// <summary>
    /// 刷新人口显示
    /// </summary>
    private void RefreshPopulationDisplay()
    {
        if (CombatSessionData.Instance.IsInitialized)
        {
            // 更新人口文本（如果有对应UI元素显示人口）
            // TODO: 根据实际UI元素更新
            // varPopulationText.text = $"{CombatSessionData.Instance.UsedPopulation}/{CombatSessionData.Instance.CurrentMaxDomination}";

            // 更新金币文本
            if (varComNumText != null)
            {
                varComNumText.text = CombatSessionData.Instance.Gold.ToString();
            }
        }
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// 人口按钮点击回调
    /// </summary>
    private void OnPopulationButtonClicked()
    {
        DebugEx.LogModule("CombatUI", "点击了人口按钮");

        if (CombatSessionData.Instance.TryUpgradePopulation())
        {
            // 刷新UI显示
            RefreshPopulationDisplay();
            DebugEx.LogModule("CombatUI", "统治值升级成功");
        }
        else
        {
            DebugEx.WarningModule("CombatUI", "统治值升级失败（金币不足）");
            // TODO: 显示提示信息
        }
    }

    /// <summary>
    /// 刷新按钮点击回调
    /// </summary>
    private void OnRefreshButtonClicked()
    {
        DebugEx.LogModule("CombatUI", "点击了刷新按钮");
        // TODO: 刷新卡牌池
    }

    /// <summary>
    /// 召唤师技能按钮点击回调（slot = index+1，所有输入走 PlayerInputManager）
    /// </summary>
    private void OnSummonerSkillClicked(int index)
    {
        int slot = index + 1;
        DebugEx.LogModule("CombatUI", $"点击了召唤师技能按钮，slot={slot}");
        PlayerInputManager.Instance?.TriggerSummonerSkill(slot);
    }

    /// <summary>
    /// 刷新召唤师技能按钮图标与可用状态
    /// </summary>
    private async UniTaskVoid RefreshSummonerSkillButtonsAsync()
    {
        if (varBtn1Arr == null || varBtn1Arr.Length == 0)
            return;

        // 从玩家角色上取 SummonerSkillManager
        var playerCharacter = PlayerCharacterManager.Instance?.CurrentPlayerCharacter;
        var skillManager = playerCharacter != null
            ? playerCharacter.GetComponent<SummonerSkillManager>()
            : null;

        var skillTable = GF.DataTable.GetDataTable<SummonerSkillTable>();

        for (int i = 0; i < varBtn1Arr.Length; i++)
        {
            var btn = varBtn1Arr[i];
            if (btn == null) continue;

            // 有对应主动技能时才显示并加载图标
            bool hasSkill = skillManager != null && i < skillManager.Skills.Count;
            btn.gameObject.SetActive(hasSkill);

            if (!hasSkill || skillTable == null) continue;

            int skillId = skillManager.Skills[i].SkillId;
            var row = skillTable.GetDataRow(skillId);
            if (row == null) continue;

            // 加载技能图标到按钮的 Image
            var btnImage = btn.GetComponent<Image>();
            if (btnImage != null && row.IconId > 0)
            {
                await GameExtension.ResourceExtension.LoadSpriteAsync(row.IconId, btnImage, 1f, null);
            }
        }
    }

    #endregion

    #region 生命周期

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 更新召唤师MP恢复
        if (SummonerRuntimeDataManager.Instance != null && SummonerRuntimeDataManager.Instance.IsInitialized)
        {
            SummonerRuntimeDataManager.Instance.UpdateMPRegen(elapseSeconds);
        }
    }

    #endregion
}
