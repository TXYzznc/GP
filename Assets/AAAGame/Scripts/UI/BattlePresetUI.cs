using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
/// <summary>
/// 出战预设界面
/// 在局外设置带入局内的棋子和策略卡组合
/// </summary>
public partial class BattlePresetUI : UIFormBase
{
    #region 字段

    /// <summary>当前编辑的预设索引（-1表示未选中）</summary>
    private int m_CurrentPresetIndex = -1;

    /// <summary>当前编辑中的预设数据（临时副本）</summary>
    private DeckData m_EditingPreset;

    /// <summary>已生成的预设槽位UI列表</summary>
    private List<GameObject> m_PresetSlotItems = new List<GameObject>();

    /// <summary>已选棋子区域的UI项列表</summary>
    private List<GameObject> m_SelectedChessItems = new List<GameObject>();

    /// <summary>可选棋子池的UI项列表</summary>
    private List<GameObject> m_PoolChessItems = new List<GameObject>();

    /// <summary>已选策略卡区域的UI项列表</summary>
    private List<GameObject> m_SelectedCardItems = new List<GameObject>();

    /// <summary>可选策略卡池的UI项列表</summary>
    private List<GameObject> m_PoolCardItems = new List<GameObject>();

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        DebugEx.LogModule("BattlePresetUI", "初始化");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        DebugEx.LogModule("BattlePresetUI", "已打开");

        // 绑定按钮事件
        BindButtons();

        // 刷新预设列表
        RefreshPresetList();

        // 默认选中第一个预设（如果有）
        var presets = BattlePresetManager.Instance.GetAllPresets();
        if (presets.Count > 0)
        {
            int defaultIndex = BattlePresetManager.Instance.GetDefaultPresetIndex();
            SelectPreset(defaultIndex >= 0 && defaultIndex < presets.Count ? defaultIndex : 0);
        }
        else
        {
            ClearEditArea();
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearAllItems();
        m_CurrentPresetIndex = -1;
        m_EditingPreset = null;

        base.OnClose(isShutdown, userData);
        DebugEx.LogModule("BattlePresetUI", "已关闭");
    }

    #endregion

    #region 按钮事件

    private void BindButtons()
    {
        if (varBtnBack != null)
        {
            varBtnBack.onClick.RemoveAllListeners();
            varBtnBack.onClick.AddListener(OnBackClicked);
        }

        if (varBtnSave != null)
        {
            varBtnSave.onClick.RemoveAllListeners();
            varBtnSave.onClick.AddListener(OnSaveClicked);
        }

        if (varBtnDelete != null)
        {
            varBtnDelete.onClick.RemoveAllListeners();
            varBtnDelete.onClick.AddListener(OnDeleteClicked);
        }

        if (varBtnSetDefault != null)
        {
            varBtnSetDefault.onClick.RemoveAllListeners();
            varBtnSetDefault.onClick.AddListener(OnSetDefaultClicked);
        }

        if (varBtnReset != null)
        {
            varBtnReset.onClick.RemoveAllListeners();
            varBtnReset.onClick.AddListener(OnResetClicked);
        }
    }

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
    }

    private void OnBackClicked()
    {
        GF.UI.CloseUIForm(this.UIForm);
    }

    private void OnSaveClicked()
    {
        if (m_CurrentPresetIndex < 0 || m_EditingPreset == null)
            return;

        // 更新名称
        if (varPresetNameInput != null)
            m_EditingPreset.DeckName = varPresetNameInput.text;

        BattlePresetManager.Instance.SavePreset(m_CurrentPresetIndex, m_EditingPreset);
        RefreshPresetList();

        DebugEx.LogModule("BattlePresetUI", $"保存预设: {m_EditingPreset.DeckName}");
    }

    private void OnDeleteClicked()
    {
        if (m_CurrentPresetIndex < 0)
            return;

        BattlePresetManager.Instance.DeletePreset(m_CurrentPresetIndex);
        m_CurrentPresetIndex = -1;
        m_EditingPreset = null;

        RefreshPresetList();
        ClearEditArea();

        // 自动选中第一个预设
        var presets = BattlePresetManager.Instance.GetAllPresets();
        if (presets.Count > 0)
            SelectPreset(0);
    }

    private void OnSetDefaultClicked()
    {
        if (m_CurrentPresetIndex < 0)
            return;

        BattlePresetManager.Instance.SetDefaultPresetIndex(m_CurrentPresetIndex);
        RefreshPresetList();
    }

    private void OnResetClicked()
    {
        if (m_CurrentPresetIndex < 0)
            return;

        // 重新加载当前预设数据
        SelectPreset(m_CurrentPresetIndex);
    }

    #endregion

    #region 预设列表

    /// <summary>
    /// 刷新预设列表（左侧面板）
    /// </summary>
    private void RefreshPresetList()
    {
        ClearPresetSlots();

        var presets = BattlePresetManager.Instance.GetAllPresets();
        int defaultIndex = BattlePresetManager.Instance.GetDefaultPresetIndex();

        // 创建已有预设槽位
        for (int i = 0; i < presets.Count; i++)
        {
            CreatePresetSlot(presets[i], i, i == defaultIndex, i == m_CurrentPresetIndex);
        }

        // 创建空槽位（点击创建新预设）
        for (int i = presets.Count; i < BattlePresetManager.MAX_PRESET_COUNT; i++)
        {
            CreateEmptyPresetSlot(i);
        }
    }

    private void CreatePresetSlot(DeckData data, int index, bool isDefault, bool isSelected)
    {
        if (varPresetSlotItem == null || varPresetSlotContainer == null)
            return;

        var go = Instantiate(varPresetSlotItem, varPresetSlotContainer.transform);
        go.SetActive(true);
        go.name = $"PresetSlot_{index}";

        // 设置名称
        var nameText = go.transform.Find("PresetName")?.GetComponent<TMP_Text>();
        if (nameText != null)
            nameText.text = data.DeckName;

        // 设置摘要
        var summaryText = go.transform.Find("PresetSummary")?.GetComponent<TMP_Text>();
        if (summaryText != null)
            summaryText.text = $"棋子 {data.UnitCardIds.Count} · 策略卡 {data.StrategyCardIds.Count}/{BattlePresetManager.MAX_CARD_COUNT}";

        // 默认标记
        var defaultBadge = go.transform.Find("DefaultBadge");
        if (defaultBadge != null)
            defaultBadge.gameObject.SetActive(isDefault);

        // 选中高亮
        var selectedBg = go.transform.Find("SelectedBg");
        if (selectedBg != null)
            selectedBg.gameObject.SetActive(isSelected);

        // 按钮事件
        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            int capturedIndex = index;
            btn.onClick.AddListener(() => SelectPreset(capturedIndex));
        }

        m_PresetSlotItems.Add(go);
    }

    private void CreateEmptyPresetSlot(int index)
    {
        if (varPresetSlotItem == null || varPresetSlotContainer == null)
            return;

        var go = Instantiate(varPresetSlotItem, varPresetSlotContainer.transform);
        go.SetActive(true);
        go.name = $"PresetSlot_Empty_{index}";

        // 清空名称，显示"+"
        var nameText = go.transform.Find("PresetName")?.GetComponent<TMP_Text>();
        if (nameText != null)
            nameText.text = "+";

        var summaryText = go.transform.Find("PresetSummary")?.GetComponent<TMP_Text>();
        if (summaryText != null)
            summaryText.text = "创建新预设";

        var defaultBadge = go.transform.Find("DefaultBadge");
        if (defaultBadge != null)
            defaultBadge.gameObject.SetActive(false);

        var selectedBg = go.transform.Find("SelectedBg");
        if (selectedBg != null)
            selectedBg.gameObject.SetActive(false);

        // 按钮事件 - 创建新预设
        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                int newIndex = BattlePresetManager.Instance.CreatePreset(null);
                if (newIndex >= 0)
                {
                    RefreshPresetList();
                    SelectPreset(newIndex);
                }
            });
        }

        m_PresetSlotItems.Add(go);
    }

    #endregion

    #region 选中预设 / 编辑区域

    /// <summary>
    /// 选中预设并加载到编辑区域
    /// </summary>
    private void SelectPreset(int index)
    {
        var preset = BattlePresetManager.Instance.GetPreset(index);
        if (preset == null)
            return;

        m_CurrentPresetIndex = index;

        // 深拷贝为编辑副本
        m_EditingPreset = new DeckData
        {
            DeckName = preset.DeckName,
            UnitCardIds = new List<int>(preset.UnitCardIds),
            StrategyCardIds = new List<int>(preset.StrategyCardIds)
        };

        // 刷新预设列表选中状态
        RefreshPresetList();

        // 刷新编辑区域
        RefreshEditArea();

        DebugEx.LogModule("BattlePresetUI", $"选中预设: index={index}, name={preset.DeckName}");
    }

    /// <summary>
    /// 刷新编辑区域所有内容
    /// </summary>
    private void RefreshEditArea()
    {
        if (m_EditingPreset == null)
            return;

        // 预设名称
        if (varPresetNameInput != null)
            varPresetNameInput.text = m_EditingPreset.DeckName;

        // 刷新棋子区域
        RefreshChessSection();

        // 刷新策略卡区域
        RefreshCardSection();

        // 更新按钮状态
        UpdateFooterButtons();
    }

    /// <summary>
    /// 清空编辑区域
    /// </summary>
    private void ClearEditArea()
    {
        if (varPresetNameInput != null)
            varPresetNameInput.text = "";

        ClearChessItems();
        ClearCardItems();
    }

    /// <summary>
    /// 更新底部按钮状态
    /// </summary>
    private void UpdateFooterButtons()
    {
        bool hasPreset = m_CurrentPresetIndex >= 0;
        bool isDefault = BattlePresetManager.Instance.GetDefaultPresetIndex() == m_CurrentPresetIndex;

        if (varBtnSave != null)
            varBtnSave.interactable = hasPreset;
        if (varBtnDelete != null)
            varBtnDelete.interactable = hasPreset;
        if (varBtnSetDefault != null)
            varBtnSetDefault.interactable = hasPreset && !isDefault;
        if (varBtnReset != null)
            varBtnReset.interactable = hasPreset;
    }

    #endregion

    #region 棋子区域

    /// <summary>
    /// 刷新棋子区域（已选 + 可选池）
    /// </summary>
    private void RefreshChessSection()
    {
        ClearChessItems();

        if (m_EditingPreset == null)
            return;

        // 更新区段标题
        if (varChessCountText != null)
            varChessCountText.text = $"{m_EditingPreset.UnitCardIds.Count}";

        var allChessIds = BattlePresetManager.Instance.GetAvailableChessIds();

        // 已选棋子
        foreach (int chessId in m_EditingPreset.UnitCardIds)
        {
            CreateSelectedChessItem(chessId);
        }

        // 可选棋子池（排除已选）
        var selectedSet = new HashSet<int>(m_EditingPreset.UnitCardIds);
        foreach (int chessId in allChessIds)
        {
            CreatePoolChessItem(chessId, selectedSet.Contains(chessId));
        }
    }

    private void CreateSelectedChessItem(int chessId)
    {
        if (varChessItemTemplate == null || varSelectedChessContainer == null)
            return;

        var go = Instantiate(varChessItemTemplate, varSelectedChessContainer.transform);
        go.SetActive(true);

        var chessItemUI = go.GetComponent<ChessItemUI>();
        if (chessItemUI != null)
        {
            string fakeInstanceId = $"preset_{chessId}";
            chessItemUI.SetData(fakeInstanceId, chessId, (_) => OnSelectedChessClicked(chessId));
        }

        m_SelectedChessItems.Add(go);
    }

    private void CreatePoolChessItem(int chessId, bool isSelected)
    {
        if (varChessItemTemplate == null || varChessPoolContainer == null)
            return;

        var go = Instantiate(varChessItemTemplate, varChessPoolContainer.transform);
        go.SetActive(true);

        var chessItemUI = go.GetComponent<ChessItemUI>();
        if (chessItemUI != null)
        {
            string fakeInstanceId = $"pool_{chessId}";
            chessItemUI.SetData(fakeInstanceId, chessId, (_) => OnPoolChessClicked(chessId));

            // 已选中的棋子显示遮罩
            if (isSelected)
            {
                var canvasGroup = go.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.4f;
                canvasGroup.interactable = false;
            }
        }

        m_PoolChessItems.Add(go);
    }

    /// <summary>
    /// 已选棋子被点击（移除）
    /// </summary>
    private void OnSelectedChessClicked(int chessId)
    {
        if (m_EditingPreset == null) return;

        m_EditingPreset.UnitCardIds.Remove(chessId);
        RefreshChessSection();

        DebugEx.LogModule("BattlePresetUI", $"移除棋子: {chessId}");
    }

    /// <summary>
    /// 可选棋子池被点击（添加）
    /// </summary>
    private void OnPoolChessClicked(int chessId)
    {
        if (m_EditingPreset == null) return;

        // 检查是否已选
        if (m_EditingPreset.UnitCardIds.Contains(chessId))
            return;

        m_EditingPreset.UnitCardIds.Add(chessId);
        RefreshChessSection();

        DebugEx.LogModule("BattlePresetUI", $"添加棋子: {chessId}");
    }

    #endregion

    #region 策略卡区域

    /// <summary>
    /// 刷新策略卡区域（已选 + 可选池）
    /// </summary>
    private void RefreshCardSection()
    {
        ClearCardItems();

        if (m_EditingPreset == null)
            return;

        // 更新区段标题
        if (varCardCountText != null)
            varCardCountText.text = $"{m_EditingPreset.StrategyCardIds.Count} / {BattlePresetManager.MAX_CARD_COUNT}";

        var allCardIds = BattlePresetManager.Instance.GetAvailableCardIds();

        // 已选策略卡
        foreach (int cardId in m_EditingPreset.StrategyCardIds)
        {
            CreateSelectedCardItem(cardId);
        }

        // 可选策略卡池（排除已选）
        var selectedSet = new HashSet<int>(m_EditingPreset.StrategyCardIds);
        foreach (int cardId in allCardIds)
        {
            CreatePoolCardItem(cardId, selectedSet.Contains(cardId));
        }
    }

    private void CreateSelectedCardItem(int cardId)
    {
        if (varCardItemTemplate == null || varSelectedCardContainer == null)
            return;

        var go = Instantiate(varCardItemTemplate, varSelectedCardContainer.transform);
        go.SetActive(true);

        var cardSlotItem = go.GetComponent<CardSlotItem>();
        if (cardSlotItem != null)
        {
            var cardTable = GF.DataTable.GetDataTable<CardTable>();
            var row = cardTable?.GetDataRow(cardId);
            if (row != null)
            {
                cardSlotItem.SetData(new CardData(cardId, row));
            }
        }

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            int capturedId = cardId;
            btn.onClick.AddListener(() => OnSelectedCardClicked(capturedId));
        }

        m_SelectedCardItems.Add(go);
    }

    private void CreatePoolCardItem(int cardId, bool isSelected)
    {
        if (varCardItemTemplate == null || varCardPoolContainer == null)
            return;

        var go = Instantiate(varCardItemTemplate, varCardPoolContainer.transform);
        go.SetActive(true);

        var cardSlotItem = go.GetComponent<CardSlotItem>();
        if (cardSlotItem != null)
        {
            var cardTable = GF.DataTable.GetDataTable<CardTable>();
            var row = cardTable?.GetDataRow(cardId);
            if (row != null)
            {
                cardSlotItem.SetData(new CardData(cardId, row));
            }
        }

        if (isSelected)
        {
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.4f;
            canvasGroup.interactable = false;
        }
        else
        {
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                int capturedId = cardId;
                btn.onClick.AddListener(() => OnPoolCardClicked(capturedId));
            }
        }

        m_PoolCardItems.Add(go);
    }

    /// <summary>
    /// 已选策略卡被点击（移除）
    /// </summary>
    private void OnSelectedCardClicked(int cardId)
    {
        if (m_EditingPreset == null) return;

        m_EditingPreset.StrategyCardIds.Remove(cardId);
        RefreshCardSection();

        DebugEx.LogModule("BattlePresetUI", $"移除策略卡: {cardId}");
    }

    /// <summary>
    /// 可选策略卡池被点击（添加）
    /// </summary>
    private void OnPoolCardClicked(int cardId)
    {
        if (m_EditingPreset == null) return;

        if (m_EditingPreset.StrategyCardIds.Contains(cardId))
            return;

        if (m_EditingPreset.StrategyCardIds.Count >= BattlePresetManager.MAX_CARD_COUNT)
        {
            DebugEx.WarningModule("BattlePresetUI", "策略卡数量已达上限");
            return;
        }

        m_EditingPreset.StrategyCardIds.Add(cardId);
        RefreshCardSection();

        DebugEx.LogModule("BattlePresetUI", $"添加策略卡: {cardId}");
    }

    #endregion

    #region 清理

    private void ClearPresetSlots()
    {
        foreach (var go in m_PresetSlotItems)
        {
            if (go != null) Destroy(go);
        }
        m_PresetSlotItems.Clear();
    }

    private void ClearChessItems()
    {
        foreach (var go in m_SelectedChessItems)
        {
            if (go != null) Destroy(go);
        }
        m_SelectedChessItems.Clear();

        foreach (var go in m_PoolChessItems)
        {
            if (go != null) Destroy(go);
        }
        m_PoolChessItems.Clear();
    }

    private void ClearCardItems()
    {
        foreach (var go in m_SelectedCardItems)
        {
            if (go != null) Destroy(go);
        }
        m_SelectedCardItems.Clear();

        foreach (var go in m_PoolCardItems)
        {
            if (go != null) Destroy(go);
        }
        m_PoolCardItems.Clear();
    }

    private void ClearAllItems()
    {
        ClearPresetSlots();
        ClearChessItems();
        ClearCardItems();
    }

    #endregion
}
