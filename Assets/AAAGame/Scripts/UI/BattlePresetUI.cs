using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameExtension;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    /// <summary>预设槽位未选中图标（资源ID 1005）</summary>
    private Sprite m_SlotNormalSprite;

    /// <summary>预设槽位选中图标（资源ID 1006）</summary>
    private Sprite m_SlotSelectedSprite;

    /// <summary>预设槽位新增图标（资源ID 1007）</summary>
    private Sprite m_SlotAddSprite;

    /// <summary>当前显示的是棋子Tab（true=棋子，false=策略卡）</summary>
    private bool m_IsChessTab = true;

    /// <summary>当前搜索关键词</summary>
    private string m_SearchKeyword = string.Empty;

    /// <summary>当前页码（从0开始）</summary>
    private int m_CurrentPage = 0;

    /// <summary>每页显示数量</summary>
    private const int PAGE_SIZE = 15;

    /// <summary>当前过滤后的棋子ID列表（用于分页）</summary>
    private List<int> m_FilteredChessIds = new List<int>();

    /// <summary>当前过滤后的策略卡ID列表（用于分页）</summary>
    private List<int> m_FilteredCardIds = new List<int>();

    /// <summary>当前编辑的预设索引（-1表示未选中）</summary>
    private int m_CurrentPresetIndex = -1;

    /// <summary>当前编辑中的预设数据（临时副本）</summary>
    private DeckData m_EditingPreset;

    /// <summary>已生成的预设槽位UI列表</summary>
    private List<GameObject> m_PresetSlotItems = new List<GameObject>();

    /// <summary>已选棋子区域的UI项列表（对象池）</summary>
    private List<GameObject> m_SelectedChessItems = new List<GameObject>();

    /// <summary>可选棋子池的UI项字典（key=chessId, value=GameObject）</summary>
    private Dictionary<int, GameObject> m_PoolChessItemsDict = new Dictionary<int, GameObject>();

    /// <summary>已选策略卡区域的UI项列表（对象池）</summary>
    private List<GameObject> m_SelectedCardItems = new List<GameObject>();

    /// <summary>可选策略卡池的UI项字典（key=cardId, value=GameObject）</summary>
    private Dictionary<int, GameObject> m_PoolCardItemsDict = new Dictionary<int, GameObject>();

    /// <summary>棋子已选区域的空位对象池</summary>
    private List<GameObject> m_ChessEmptySlots = new List<GameObject>();

    /// <summary>策略卡已选区域的空位对象池</summary>
    private List<GameObject> m_CardEmptySlots = new List<GameObject>();

    /// <summary>棋子计数文本的默认颜色</summary>
    private Color m_ChessCountDefaultColor;

    /// <summary>策略卡计数文本的默认颜色</summary>
    private Color m_CardCountDefaultColor;

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        DebugEx.Log(nameof(BattlePresetUI), "初始化");

        // 记录计数文本的默认颜色（由预制体决定）
        if (varChessCountText != null)
            m_ChessCountDefaultColor = varChessCountText.color;
        if (varCardCountText != null)
            m_CardCountDefaultColor = varCardCountText.color;
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        DebugEx.Log(nameof(BattlePresetUI), "已打开");

        // 绑定按钮事件
        BindButtons();

        // 异步加载图标后再刷新列表
        _ = OpenAsync();
    }

    private async UniTaskVoid OpenAsync()
    {
        // 加载预设槽位图标
        m_SlotNormalSprite = await ResourceExtension.LoadSpriteAsync(1005);
        m_SlotSelectedSprite = await ResourceExtension.LoadSpriteAsync(1006);
        m_SlotAddSprite = await ResourceExtension.LoadSpriteAsync(1007);
        DebugEx.Success(
            nameof(BattlePresetUI),
            $"槽位图标加载完成: normal={m_SlotNormalSprite != null}, selected={m_SlotSelectedSprite != null}, add={m_SlotAddSprite != null}"
        );

        // 刷新预设列表
        RefreshPresetList();

        // 初始化Tab状态（默认显示棋子Tab）
        m_IsChessTab = true;
        if (varChessPoolContainer != null)
            varChessPoolContainer.SetActive(true);
        if (varCardPoolContainer != null)
            varCardPoolContainer.SetActive(false);
        UpdateTabSelectionMark(true);

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

        // 播放入场动画
        PlayOpenAnimation();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理所有动画
        KillAllAnimations();

        ClearAllItems();
        m_CurrentPresetIndex = -1;
        m_EditingPreset = null;

        base.OnClose(isShutdown, userData);
        DebugEx.Log(nameof(BattlePresetUI), "已关闭");
    }

    #endregion

    #region 按钮事件

    private void BindButtons()
    {
        if (varBtnBack != null)
        {
            varBtnBack.onClick.RemoveAllListeners();
            varBtnBack.onClick.AddListener(OnBackClicked);
            AddButtonAnimation(varBtnBack);
        }

        if (varBtnSave != null)
        {
            varBtnSave.onClick.RemoveAllListeners();
            varBtnSave.onClick.AddListener(OnSaveClicked);
            AddButtonAnimation(varBtnSave);
        }

        if (varBtnDelete != null)
        {
            varBtnDelete.onClick.RemoveAllListeners();
            varBtnDelete.onClick.AddListener(OnDeleteClicked);
            AddButtonAnimation(varBtnDelete);
        }

        if (varBtnSetDefault != null)
        {
            varBtnSetDefault.onClick.RemoveAllListeners();
            varBtnSetDefault.onClick.AddListener(OnSetDefaultClicked);
            AddButtonAnimation(varBtnSetDefault);
        }

        if (varBtnReset != null)
        {
            varBtnReset.onClick.RemoveAllListeners();
            varBtnReset.onClick.AddListener(OnResetClicked);
            AddButtonAnimation(varBtnReset);
        }

        // Tab 切换
        if (varBtnTabChess != null)
        {
            varBtnTabChess.onClick.RemoveAllListeners();
            varBtnTabChess.onClick.AddListener(() => SwitchTab(true));
        }
        if (varBtnTabCard != null)
        {
            varBtnTabCard.onClick.RemoveAllListeners();
            varBtnTabCard.onClick.AddListener(() => SwitchTab(false));
        }

        // 搜索框
        if (varPoolSearchInput != null)
        {
            varPoolSearchInput.onValueChanged.RemoveAllListeners();
            varPoolSearchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        // 分页按钮
        if (varBtnPagePrev != null)
        {
            varBtnPagePrev.onClick.RemoveAllListeners();
            varBtnPagePrev.onClick.AddListener(OnPagePrev);
        }
        if (varBtnPageNext != null)
        {
            varBtnPageNext.onClick.RemoveAllListeners();
            varBtnPageNext.onClick.AddListener(OnPageNext);
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

        DebugEx.Log(nameof(BattlePresetUI), $"保存预设: {m_EditingPreset.DeckName}");
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

        // 播放设为默认动画
        if (m_CurrentPresetIndex < m_PresetSlotItems.Count)
        {
            var slotItem = m_PresetSlotItems[m_CurrentPresetIndex];
            var defaultBadge = slotItem.transform.Find("DefaultBadge");
            if (defaultBadge != null)
            {
                PlaySetDefaultAnimation(defaultBadge.gameObject);
            }
        }
    }

    private void OnResetClicked()
    {
        if (m_CurrentPresetIndex < 0)
            return;

        // 重新加载当前预设数据
        SelectPreset(m_CurrentPresetIndex);
    }

    #endregion

    #region Tab / 搜索 / 分页

    /// <summary>切换棋子/策略卡Tab</summary>
    private void SwitchTab(bool isChess)
    {
        m_IsChessTab = isChess;
        m_CurrentPage = 0;

        // 清空搜索框
        if (varPoolSearchInput != null)
            varPoolSearchInput.SetTextWithoutNotify(string.Empty);
        m_SearchKeyword = string.Empty;

        // 切换容器显示
        if (varChessPoolContainer != null)
            varChessPoolContainer.SetActive(isChess);
        if (varCardPoolContainer != null)
            varCardPoolContainer.SetActive(!isChess);

        // 更新Tab选中标记
        UpdateTabSelectionMark(isChess);

        // 刷新过滤和分页
        RefreshFilterAndPage();

        DebugEx.Log(nameof(BattlePresetUI), $"切换Tab: {(isChess ? "棋子" : "策略卡")}");
    }

    /// <summary>更新Tab按钮的SelectionMark显示</summary>
    private void UpdateTabSelectionMark(bool isChess)
    {
        if (varBtnTabChess != null)
        {
            var mark = varBtnTabChess.transform.Find("SelectionMark");
            if (mark != null)
                mark.gameObject.SetActive(isChess);
        }
        if (varBtnTabCard != null)
        {
            var mark = varBtnTabCard.transform.Find("SelectionMark");
            if (mark != null)
                mark.gameObject.SetActive(!isChess);
        }
    }

    /// <summary>搜索框内容变化</summary>
    private void OnSearchChanged(string keyword)
    {
        m_SearchKeyword = keyword ?? string.Empty;
        m_CurrentPage = 0;
        RefreshFilterAndPage();
    }

    /// <summary>上一页</summary>
    private void OnPagePrev()
    {
        if (m_CurrentPage <= 0)
            return;
        m_CurrentPage--;
        RefreshPageDisplay();
    }

    /// <summary>下一页</summary>
    private void OnPageNext()
    {
        var filteredList = m_IsChessTab ? m_FilteredChessIds : m_FilteredCardIds;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredList.Count / PAGE_SIZE));
        if (m_CurrentPage >= totalPages - 1)
            return;
        m_CurrentPage++;
        RefreshPageDisplay();
    }

    /// <summary>重新过滤并刷新分页（搜索词或Tab变化时调用）</summary>
    private void RefreshFilterAndPage()
    {
        string kw = m_SearchKeyword.ToLower();

        if (m_IsChessTab)
        {
            var allIds = BattlePresetManager.Instance.GetAvailableChessIds();
            m_FilteredChessIds.Clear();
            foreach (int id in allIds)
            {
                if (string.IsNullOrEmpty(kw))
                {
                    m_FilteredChessIds.Add(id);
                }
                else
                {
                    // 通过ChessPresetItem或配置表匹配名称
                    if (m_PoolChessItemsDict.TryGetValue(id, out var go))
                    {
                        var item = go.GetComponent<ChessPresetItem>();
                        string name = item != null ? item.GetChessName().ToLower() : id.ToString();
                        if (name.Contains(kw))
                            m_FilteredChessIds.Add(id);
                    }
                }
            }
        }
        else
        {
            var allIds = BattlePresetManager.Instance.GetAvailableCardIds();
            m_FilteredCardIds.Clear();
            var cardTable = GF.DataTable.GetDataTable<CardTable>();
            foreach (int id in allIds)
            {
                if (string.IsNullOrEmpty(kw))
                {
                    m_FilteredCardIds.Add(id);
                }
                else
                {
                    var row = cardTable?.GetDataRow(id);
                    string name = row != null ? row.Name.ToLower() : id.ToString();
                    if (name.Contains(kw))
                        m_FilteredCardIds.Add(id);
                }
            }
        }

        RefreshPageDisplay();
    }

    /// <summary>根据当前页和过滤列表刷新池中item的显示</summary>
    private void RefreshPageDisplay()
    {
        var filteredList = m_IsChessTab ? m_FilteredChessIds : m_FilteredCardIds;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredList.Count / PAGE_SIZE));
        m_CurrentPage = Mathf.Clamp(m_CurrentPage, 0, totalPages - 1);

        int startIdx = m_CurrentPage * PAGE_SIZE;
        int endIdx = Mathf.Min(startIdx + PAGE_SIZE, filteredList.Count);

        // 构建本页可见ID集合
        var pageIds = new HashSet<int>();
        for (int i = startIdx; i < endIdx; i++)
            pageIds.Add(filteredList[i]);

        if (m_IsChessTab)
        {
            foreach (var kv in m_PoolChessItemsDict)
                kv.Value.SetActive(pageIds.Contains(kv.Key));
        }
        else
        {
            foreach (var kv in m_PoolCardItemsDict)
                kv.Value.SetActive(pageIds.Contains(kv.Key));
        }

        // 更新分页文本和按钮状态
        if (varPageText != null)
            varPageText.text = $"{m_CurrentPage + 1}/{totalPages}";
        if (varBtnPagePrev != null)
            varBtnPagePrev.interactable = m_CurrentPage > 0;
        if (varBtnPageNext != null)
            varBtnPageNext.interactable = m_CurrentPage < totalPages - 1;

        DebugEx.Log(
            nameof(BattlePresetUI),
            $"分页刷新: 第{m_CurrentPage + 1}/{totalPages}页, 显示{pageIds.Count}项"
        );
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
            summaryText.text =
                $"棋子 {data.UnitCardIds.Count} · 策略卡 {data.StrategyCardIds.Count}/{BattlePresetManager.MAX_STRATEGY_CARD_COUNT}";

        // 默认标记
        var defaultBadge = go.transform.Find("DefaultBadge");
        if (defaultBadge != null)
            defaultBadge.gameObject.SetActive(isDefault);

        // 选中图标切换
        var slotIcon = go.GetComponent<Image>();
        if (slotIcon != null)
        {
            slotIcon.sprite = isSelected ? m_SlotSelectedSprite : m_SlotNormalSprite;
            DebugEx.Log(nameof(BattlePresetUI), $"槽位[{index}] 图标设置: isSelected={isSelected}");
        }

        // 按钮事件
        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            int capturedIndex = index;
            btn.onClick.AddListener(() => SelectPreset(capturedIndex));

            // 添加按钮悬停和点击动画
            AddButtonAnimation(btn);
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

        // 空槽位显示新增图标
        var slotIcon = go.GetComponent<Image>();
        if (slotIcon != null)
            slotIcon.sprite = m_SlotAddSprite;

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

                    // 播放创建动画
                    PlayCreatePresetAnimation(m_PresetSlotItems[newIndex]);
                }
            });

            // 添加按钮动画
            AddButtonAnimation(btn);
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
            StrategyCardIds = new List<int>(preset.StrategyCardIds),
        };

        // 刷新预设列表选中状态
        RefreshPresetList();

        // 刷新编辑区域
        RefreshEditArea();

        DebugEx.Log(nameof(BattlePresetUI), $"选中预设: index={index}, name={preset.DeckName}");
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
        bool isDefault =
            BattlePresetManager.Instance.GetDefaultPresetIndex() == m_CurrentPresetIndex;

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
        if (m_EditingPreset == null)
            return;

        // 更新已选棋子数量（格式：x/8）
        if (varChessCountText != null)
            varChessCountText.text = $"{m_EditingPreset.UnitCardIds.Count}/8";

        var allChessIds = BattlePresetManager.Instance.GetAvailableChessIds();
        var selectedSet = new HashSet<int>(m_EditingPreset.UnitCardIds);

        // 更新可选棋子数量
        if (varChessCountText2 != null)
            varChessCountText2.text = $"{allChessIds.Count}";

        // 清空已选区域（对象池复用）
        foreach (var go in m_SelectedChessItems)
        {
            if (go != null)
                go.SetActive(false);
        }

        // 更新已选棋子（只显示有数据的格子）
        for (int i = 0; i < m_EditingPreset.UnitCardIds.Count; i++)
        {
            GameObject go;
            if (i < m_SelectedChessItems.Count)
            {
                go = m_SelectedChessItems[i];
                // 重置动画残留状态
                DOTween.Kill(go.transform);
                go.transform.localScale = Vector3.one;
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    DOTween.Kill(cg);
                    cg.alpha = 1f;
                }
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(varChessItemTemplate, varSelectedChessContainer.transform);
                m_SelectedChessItems.Add(go);
            }

            // ⭐ 确保 ChessPresetItem 在 Container 中的 sibling 顺序与 index 一致
            go.transform.SetSiblingIndex(i);

            var chessPresetItem = go.GetComponent<ChessPresetItem>();
            if (chessPresetItem != null)
            {
                int chessId = m_EditingPreset.UnitCardIds[i];
                chessPresetItem.SetData(chessId, (_) => OnSelectedChessClicked(chessId));
                chessPresetItem.HideMask();
            }
        }

        // 刷新空位数量
        RefreshChessEmptySlots();

        // 更新可选棋子池（不销毁，只改变Mask状态）
        foreach (int chessId in allChessIds)
        {
            if (!m_PoolChessItemsDict.ContainsKey(chessId))
            {
                // 首次创建
                var go = Instantiate(varChessItemTemplate, varChessPoolContainer.transform);
                go.SetActive(true);

                var chessPresetItem = go.GetComponent<ChessPresetItem>();
                if (chessPresetItem != null)
                {
                    chessPresetItem.SetData(chessId, (_) => OnPoolChessClicked(chessId));
                }

                m_PoolChessItemsDict[chessId] = go;
            }

            // 更新Mask状态
            var poolGo = m_PoolChessItemsDict[chessId];
            var poolItem = poolGo.GetComponent<ChessPresetItem>();
            if (poolItem != null)
            {
                if (selectedSet.Contains(chessId))
                {
                    poolItem.ShowSelectedMask();

                    var canvasGroup = poolGo.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                        canvasGroup = poolGo.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0.4f;
                    canvasGroup.interactable = false;
                }
                else
                {
                    poolItem.HideMask();

                    var canvasGroup = poolGo.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 1f;
                        canvasGroup.interactable = true;
                    }
                }
            }
        }

        DebugEx.Log(
            nameof(BattlePresetUI),
            $"刷新棋子区域: 已选={m_EditingPreset.UnitCardIds.Count}, 可选={allChessIds.Count}"
        );

        // 如果当前是棋子Tab，刷新分页
        if (m_IsChessTab)
            RefreshFilterAndPage();
    }

    /// <summary>
    /// 已选棋子被点击（移除）
    /// </summary>
    private void OnSelectedChessClicked(int chessId)
    {
        if (m_EditingPreset == null)
            return;

        int removedIndex = m_EditingPreset.UnitCardIds.IndexOf(chessId);
        if (removedIndex < 0)
            return;

        var removedItem = m_SelectedChessItems[removedIndex];
        GameObject poolItem = m_PoolChessItemsDict.ContainsKey(chessId)
            ? m_PoolChessItemsDict[chessId]
            : null;

        // 先停止这个项上的所有动画
        DOTween.Kill(removedItem.transform);
        var removedCanvasGroup = removedItem.GetComponent<CanvasGroup>();
        if (removedCanvasGroup != null)
            DOTween.Kill(removedCanvasGroup);

        // ⭐ 立即从数据中移除，避免动画期间快速操作导致数据/UI不同步
        m_EditingPreset.UnitCardIds.Remove(chessId);

        // 立即刷新已选区域（数据已更新）
        removedItem.SetActive(false);
        RefreshSelectedChessFromIndex(removedIndex);

        // 更新计数文本
        if (varChessCountText != null)
        {
            string newCount = $"{m_EditingPreset.UnitCardIds.Count}/8";
            varChessCountText.text = newCount;
        }

        // 激活一个空位
        RefreshChessEmptySlots();

        // 播放移除动画（纯视觉，不影响数据）
        PlayItemRemoveAnimation(removedItem, poolItem, null);

        // 立即更新可选池中这一个棋子的状态
        UpdatePoolChessState(chessId, false);

        DebugEx.Log(nameof(BattlePresetUI), $"移除棋子: {chessId}");
    }

    /// <summary>
    /// 从指定索引开始刷新已选棋子（用于移除后重新排列）
    /// </summary>
    private void RefreshSelectedChessFromIndex(int startIndex)
    {
        // 更新从 startIndex 开始的所有项
        for (int i = startIndex; i < m_EditingPreset.UnitCardIds.Count; i++)
        {
            int chessId = m_EditingPreset.UnitCardIds[i];

            // 确保索引有效
            if (i >= m_SelectedChessItems.Count)
                break;

            var go = m_SelectedChessItems[i];
            if (go == null)
                continue;

            // 停止这个项上的所有动画
            DOTween.Kill(go.transform);
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                DOTween.Kill(canvasGroup);

            // 确保激活
            if (!go.activeSelf)
                go.SetActive(true);

            // ⭐ 确保 sibling 顺序正确
            go.transform.SetSiblingIndex(i);

            var chessPresetItem = go.GetComponent<ChessPresetItem>();
            if (chessPresetItem != null)
            {
                chessPresetItem.SetData(chessId, (_) => OnSelectedChessClicked(chessId));
                chessPresetItem.HideMask();
            }

            // 恢复正常状态（防止被动画污染）
            go.transform.localScale = Vector3.one;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        // 隐藏多余的项（从数据长度开始到列表末尾）
        for (int i = m_EditingPreset.UnitCardIds.Count; i < m_SelectedChessItems.Count; i++)
        {
            if (m_SelectedChessItems[i] != null && m_SelectedChessItems[i].activeSelf)
            {
                // 停止动画
                DOTween.Kill(m_SelectedChessItems[i].transform);
                var canvasGroup = m_SelectedChessItems[i].GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    DOTween.Kill(canvasGroup);

                m_SelectedChessItems[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 可选棋子池被点击（添加）
    /// </summary>
    private void OnPoolChessClicked(int chessId)
    {
        if (m_EditingPreset == null)
            return;

        // 检查是否已选
        if (m_EditingPreset.UnitCardIds.Contains(chessId))
            return;

        // 检查是否已达到最大数量限制（8个）
        if (m_EditingPreset.UnitCardIds.Count >= BattlePresetManager.MAX_CHESS_COUNT)
        {
            DebugEx.Warning(nameof(BattlePresetUI), "已达到最大棋子数量限制");

            // 播放达到上限动画
            if (varChessCountText != null)
            {
                PlayLimitShakeAnimation(varChessCountText, m_ChessCountDefaultColor);
            }
            return;
        }

        m_EditingPreset.UnitCardIds.Add(chessId);

        // 增量更新：只添加新棋子到已选区域
        AddChessToSelected(chessId);

        // 获取池项和已选项
        GameObject poolItem = m_PoolChessItemsDict.ContainsKey(chessId)
            ? m_PoolChessItemsDict[chessId]
            : null;
        GameObject selectedItem = m_SelectedChessItems[m_EditingPreset.UnitCardIds.Count - 1];

        // 播放添加动画
        PlayItemAddAnimation(poolItem, selectedItem);

        // 只更新可选池中这一个棋子的状态
        UpdatePoolChessState(chessId, true);

        // 更新计数文本（格式：x/8）
        if (varChessCountText != null)
        {
            string newCount = $"{m_EditingPreset.UnitCardIds.Count}/8";
            PlayCounterUpdateAnimation(varChessCountText, newCount);
        }

        // 隐藏一个空位
        RefreshChessEmptySlots();

        DebugEx.Log(nameof(BattlePresetUI), $"添加棋子: {chessId}");
    }

    /// <summary>
    /// 添加单个棋子到已选区域
    /// </summary>
    private void AddChessToSelected(int chessId)
    {
        GameObject go;
        int index = m_EditingPreset.UnitCardIds.Count - 1;

        if (index < m_SelectedChessItems.Count)
        {
            go = m_SelectedChessItems[index];

            // 重置动画残留状态，防止上次移除动画的 scale/alpha 污染
            DOTween.Kill(go.transform);
            go.transform.localScale = Vector3.one;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                DOTween.Kill(cg);
                cg.alpha = 1f;
            }

            go.SetActive(true);
        }
        else
        {
            go = Instantiate(varChessItemTemplate, varSelectedChessContainer.transform);
            m_SelectedChessItems.Add(go);
        }

        // ⭐ 确保插入到正确的 sibling 位置（在 EmptySlot 之前）
        go.transform.SetSiblingIndex(index);

        var chessPresetItem = go.GetComponent<ChessPresetItem>();
        if (chessPresetItem != null)
        {
            chessPresetItem.SetData(chessId, (_) => OnSelectedChessClicked(chessId));
            chessPresetItem.HideMask();
        }
    }

    /// <summary>
    /// 更新可选池中单个棋子的状态（不播放动画，只更新Mask和交互状态）
    /// </summary>
    private void UpdatePoolChessState(int chessId, bool isSelected)
    {
        if (!m_PoolChessItemsDict.ContainsKey(chessId))
            return;

        var poolGo = m_PoolChessItemsDict[chessId];
        var poolItem = poolGo.GetComponent<ChessPresetItem>();

        if (poolItem != null)
        {
            if (isSelected)
            {
                poolItem.ShowSelectedMask();

                var canvasGroup = poolGo.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = poolGo.AddComponent<CanvasGroup>();
                canvasGroup.interactable = false;
            }
            else
            {
                poolItem.HideMask();

                var canvasGroup = poolGo.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                }
            }
        }
    }

    #endregion

    #region 策略卡区域

    /// <summary>
    /// 刷新策略卡区域（已选 + 可选池）
    /// </summary>
    private void RefreshCardSection()
    {
        if (m_EditingPreset == null)
            return;

        // 更新已选策略卡数量
        if (varCardCountText != null)
        {
            varCardCountText.text =
                $"{m_EditingPreset.StrategyCardIds.Count}/{BattlePresetManager.MAX_STRATEGY_CARD_COUNT}";
            varCardCountText.color = m_CardCountDefaultColor; // 重置颜色，防止上限红色残留
        }

        var allCardIds = BattlePresetManager.Instance.GetAvailableCardIds();
        var selectedSet = new HashSet<int>(m_EditingPreset.StrategyCardIds);

        // 更新可选策略卡数量
        if (varCardCountText2 != null)
            varCardCountText2.text = $"{allCardIds.Count}";

        // 清空已选区域（对象池复用）
        foreach (var go in m_SelectedCardItems)
        {
            if (go != null)
                go.SetActive(false);
        }

        // 更新已选策略卡（只显示有数据的格子）
        var cardTableForSelected = GF.DataTable.GetDataTable<CardTable>();
        for (int i = 0; i < m_EditingPreset.StrategyCardIds.Count; i++)
        {
            GameObject go;
            if (i < m_SelectedCardItems.Count)
            {
                go = m_SelectedCardItems[i];
                // 重置动画残留状态
                DOTween.Kill(go.transform);
                go.transform.localScale = Vector3.one;
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    DOTween.Kill(cg);
                    cg.alpha = 1f;
                }
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(varCardItemTemplate, varSelectedCardContainer.transform);
                m_SelectedCardItems.Add(go);
            }

            var cardPresetItem = go.GetComponent<CardPresetItem>();
            if (cardPresetItem != null)
            {
                int cardId = m_EditingPreset.StrategyCardIds[i];
                var row = cardTableForSelected?.GetDataRow(cardId);
                if (row != null)
                {
                    cardPresetItem.SetData(
                        new CardData(cardId, row),
                        (_) => OnSelectedCardClicked(cardId)
                    );
                }
            }
        }

        // 刷新空位数量
        RefreshCardEmptySlots();

        // 更新可选策略卡池（不销毁，只改变状态）
        foreach (int cardId in allCardIds)
        {
            if (!m_PoolCardItemsDict.ContainsKey(cardId))
            {
                // 首次创建
                var go = Instantiate(varCardItemTemplate, varCardPoolContainer.transform);
                go.SetActive(true);

                var cardPresetItem = go.GetComponent<CardPresetItem>();
                if (cardPresetItem != null)
                {
                    var cardTable = GF.DataTable.GetDataTable<CardTable>();
                    var row = cardTable?.GetDataRow(cardId);
                    if (row != null)
                    {
                        cardPresetItem.SetData(
                            new CardData(cardId, row),
                            (_) => OnPoolCardClicked(cardId)
                        );
                    }
                }

                m_PoolCardItemsDict[cardId] = go;
            }

            // 更新状态
            var poolGo = m_PoolCardItemsDict[cardId];
            if (selectedSet.Contains(cardId))
            {
                var canvasGroup = poolGo.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = poolGo.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.4f;
                canvasGroup.interactable = false;
            }
            else
            {
                var canvasGroup = poolGo.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                }
            }
        }

        DebugEx.Log(
            nameof(BattlePresetUI),
            $"刷新策略卡区域: 已选={m_EditingPreset.StrategyCardIds.Count}, 可选={allCardIds.Count}"
        );

        // 如果当前是策略卡Tab，刷新分页
        if (!m_IsChessTab)
            RefreshFilterAndPage();
    }

    /// <summary>
    /// 已选策略卡被点击（移除）
    /// </summary>
    private void OnSelectedCardClicked(int cardId)
    {
        if (m_EditingPreset == null)
            return;

        int removedIndex = m_EditingPreset.StrategyCardIds.IndexOf(cardId);
        if (removedIndex < 0)
            return;

        var removedItem = m_SelectedCardItems[removedIndex];
        GameObject poolItem = m_PoolCardItemsDict.ContainsKey(cardId)
            ? m_PoolCardItemsDict[cardId]
            : null;

        // 先停止这个项上的所有动画
        DOTween.Kill(removedItem.transform);
        var removedCanvasGroup = removedItem.GetComponent<CanvasGroup>();
        if (removedCanvasGroup != null)
            DOTween.Kill(removedCanvasGroup);

        // 播放移除动画，动画完成后再从数据中移除
        PlayItemRemoveAnimation(
            removedItem,
            poolItem,
            () =>
            {
                // 动画完成后从数据中移除
                m_EditingPreset.StrategyCardIds.Remove(cardId);

                // 隐藏这个项
                removedItem.SetActive(false);

                // 重新排列已选区域
                RefreshSelectedCardsFromIndex(removedIndex);

                // 更新计数文本
                if (varCardCountText != null)
                {
                    string newCount =
                        $"{m_EditingPreset.StrategyCardIds.Count}/{BattlePresetManager.MAX_STRATEGY_CARD_COUNT}";
                    varCardCountText.text = newCount;
                }

                // 激活一个空位
                RefreshCardEmptySlots();
            }
        );

        // 立即更新可选池中这一个策略卡的状态
        UpdatePoolCardState(cardId, false);

        DebugEx.Log(nameof(BattlePresetUI), $"移除策略卡: {cardId}");
    }

    /// <summary>
    /// 从指定索引开始刷新已选策略卡（用于移除后重新排列）
    /// </summary>
    private void RefreshSelectedCardsFromIndex(int startIndex)
    {
        var cardTable = GF.DataTable.GetDataTable<CardTable>();

        // 更新从 startIndex 开始的所有项
        for (int i = startIndex; i < m_EditingPreset.StrategyCardIds.Count; i++)
        {
            int cardId = m_EditingPreset.StrategyCardIds[i];

            // 确保索引有效
            if (i >= m_SelectedCardItems.Count)
                break;

            var go = m_SelectedCardItems[i];
            if (go == null)
                continue;

            // 停止这个项上的所有动画
            DOTween.Kill(go.transform);
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                DOTween.Kill(canvasGroup);

            // 确保激活
            if (!go.activeSelf)
                go.SetActive(true);

            var cardPresetItem = go.GetComponent<CardPresetItem>();
            if (cardPresetItem != null)
            {
                var row = cardTable?.GetDataRow(cardId);
                if (row != null)
                {
                    cardPresetItem.SetData(
                        new CardData(cardId, row),
                        (_) => OnSelectedCardClicked(cardId)
                    );
                }
            }

            // 恢复正常状态（防止被动画污染）
            go.transform.localScale = Vector3.one;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        // 隐藏多余的项（从数据长度开始到列表末尾）
        for (int i = m_EditingPreset.StrategyCardIds.Count; i < m_SelectedCardItems.Count; i++)
        {
            if (m_SelectedCardItems[i] != null && m_SelectedCardItems[i].activeSelf)
            {
                // 停止动画
                DOTween.Kill(m_SelectedCardItems[i].transform);
                var canvasGroup = m_SelectedCardItems[i].GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    DOTween.Kill(canvasGroup);

                m_SelectedCardItems[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 可选策略卡池被点击（添加）
    /// </summary>
    private void OnPoolCardClicked(int cardId)
    {
        if (m_EditingPreset == null)
            return;

        if (m_EditingPreset.StrategyCardIds.Contains(cardId))
            return;

        if (m_EditingPreset.StrategyCardIds.Count >= BattlePresetManager.MAX_STRATEGY_CARD_COUNT)
        {
            DebugEx.Warning(nameof(BattlePresetUI), "策略卡数量已达上限");

            // 播放达到上限动画
            if (varCardCountText != null)
            {
                PlayLimitShakeAnimation(varCardCountText, m_CardCountDefaultColor);
            }
            return;
        }

        m_EditingPreset.StrategyCardIds.Add(cardId);

        // 增量更新：只添加新策略卡到已选区域
        AddCardToSelected(cardId);

        // 获取池项和已选项
        GameObject poolItem = m_PoolCardItemsDict.ContainsKey(cardId)
            ? m_PoolCardItemsDict[cardId]
            : null;
        GameObject selectedItem = m_SelectedCardItems[m_EditingPreset.StrategyCardIds.Count - 1];

        // 播放添加动画
        PlayItemAddAnimation(poolItem, selectedItem);

        // 只更新可选池中这一个策略卡的状态
        UpdatePoolCardState(cardId, true);

        // 更新计数文本
        if (varCardCountText != null)
        {
            string newCount =
                $"{m_EditingPreset.StrategyCardIds.Count}/{BattlePresetManager.MAX_STRATEGY_CARD_COUNT}";
            PlayCounterUpdateAnimation(varCardCountText, newCount);
        }

        // 隐藏一个空位
        RefreshCardEmptySlots();

        DebugEx.Log(nameof(BattlePresetUI), $"添加策略卡: {cardId}");
    }

    /// <summary>
    /// 添加单个策略卡到已选区域
    /// </summary>
    private void AddCardToSelected(int cardId)
    {
        GameObject go;
        int index = m_EditingPreset.StrategyCardIds.Count - 1;

        if (index < m_SelectedCardItems.Count)
        {
            go = m_SelectedCardItems[index];

            // ⭐ 重置动画残留状态
            DOTween.Kill(go.transform);
            go.transform.localScale = Vector3.one;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                DOTween.Kill(cg);
                cg.alpha = 1f;
            }

            go.SetActive(true);
        }
        else
        {
            go = Instantiate(varCardItemTemplate, varSelectedCardContainer.transform);
            m_SelectedCardItems.Add(go);
        }

        var cardPresetItem = go.GetComponent<CardPresetItem>();
        if (cardPresetItem != null)
        {
            var cardTable = GF.DataTable.GetDataTable<CardTable>();
            var row = cardTable?.GetDataRow(cardId);
            if (row != null)
            {
                cardPresetItem.SetData(
                    new CardData(cardId, row),
                    (_) => OnSelectedCardClicked(cardId)
                );
            }
        }
    }

    /// <summary>
    /// 更新可选池中单个策略卡的状态（不播放动画，只更新交互状态）
    /// </summary>
    private void UpdatePoolCardState(int cardId, bool isSelected)
    {
        if (!m_PoolCardItemsDict.ContainsKey(cardId))
            return;

        var poolGo = m_PoolCardItemsDict[cardId];

        if (isSelected)
        {
            var canvasGroup = poolGo.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = poolGo.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
        }
        else
        {
            var canvasGroup = poolGo.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
            }
        }
    }

    #endregion

    #region EmptySlot 管理

    /// <summary>刷新棋子已选区域的空位数量</summary>
    private void RefreshChessEmptySlots()
    {
        if (varEmptySlot == null || varSelectedChessContainer == null)
            return;

        int emptyCount = BattlePresetManager.MAX_CHESS_COUNT - m_EditingPreset.UnitCardIds.Count;

        // 隐藏所有空位
        foreach (var go in m_ChessEmptySlots)
            if (go != null)
                go.SetActive(false);

        // 激活/创建需要的数量
        for (int i = 0; i < emptyCount; i++)
        {
            if (i < m_ChessEmptySlots.Count)
            {
                m_ChessEmptySlots[i].SetActive(true);
            }
            else
            {
                var go = Instantiate(varEmptySlot, varSelectedChessContainer.transform);
                go.SetActive(true);
                m_ChessEmptySlots.Add(go);
            }
        }

        DebugEx.Log(nameof(BattlePresetUI), $"棋子空位刷新: {emptyCount}个");
    }

    /// <summary>刷新策略卡已选区域的空位数量</summary>
    private void RefreshCardEmptySlots()
    {
        if (varEmptySlot == null || varSelectedCardContainer == null)
            return;

        int emptyCount =
            BattlePresetManager.MAX_STRATEGY_CARD_COUNT - m_EditingPreset.StrategyCardIds.Count;

        // 隐藏所有空位
        foreach (var go in m_CardEmptySlots)
            if (go != null)
                go.SetActive(false);

        // 激活/创建需要的数量
        for (int i = 0; i < emptyCount; i++)
        {
            if (i < m_CardEmptySlots.Count)
            {
                m_CardEmptySlots[i].SetActive(true);
            }
            else
            {
                var go = Instantiate(varEmptySlot, varSelectedCardContainer.transform);
                go.SetActive(true);
                m_CardEmptySlots.Add(go);
            }
        }

        DebugEx.Log(nameof(BattlePresetUI), $"策略卡空位刷新: {emptyCount}个");
    }

    #endregion

    #region 清理

    private void ClearPresetSlots()
    {
        foreach (var go in m_PresetSlotItems)
        {
            if (go != null)
                Destroy(go);
        }
        m_PresetSlotItems.Clear();
    }

    private void ClearChessItems()
    {
        // 清空已选棋子（对象池）
        foreach (var go in m_SelectedChessItems)
        {
            if (go != null)
                Destroy(go);
        }
        m_SelectedChessItems.Clear();

        // 清空棋子空位
        foreach (var go in m_ChessEmptySlots)
        {
            if (go != null)
                Destroy(go);
        }
        m_ChessEmptySlots.Clear();

        // 清空可选棋子池
        foreach (var go in m_PoolChessItemsDict.Values)
        {
            if (go != null)
                Destroy(go);
        }
        m_PoolChessItemsDict.Clear();
    }

    private void ClearCardItems()
    {
        // 清空已选策略卡（对象池）
        foreach (var go in m_SelectedCardItems)
        {
            if (go != null)
                Destroy(go);
        }
        m_SelectedCardItems.Clear();

        // 清空策略卡空位
        foreach (var go in m_CardEmptySlots)
        {
            if (go != null)
                Destroy(go);
        }
        m_CardEmptySlots.Clear();

        // 清空可选策略卡池
        foreach (var go in m_PoolCardItemsDict.Values)
        {
            if (go != null)
                Destroy(go);
        }
        m_PoolCardItemsDict.Clear();
    }

    private void ClearAllItems()
    {
        ClearPresetSlots();
        ClearChessItems();
        ClearCardItems();
    }

    #endregion

    #region 动画系统

    /// <summary>
    /// 播放界面打开动画
    /// </summary>
    private void PlayOpenAnimation()
    {
        // 左侧预设列表入场动画
        if (varPresetSlotContainer != null)
        {
            var canvasGroup = varPresetSlotContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = varPresetSlotContainer.AddComponent<CanvasGroup>();

            var rectTransform = varPresetSlotContainer.GetComponent<RectTransform>();

            canvasGroup.alpha = 0;
            var originalPos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = new Vector2(originalPos.x - 30, originalPos.y);

            canvasGroup.DOFade(1, 0.4f).SetEase(Ease.OutQuart);
            rectTransform.DOAnchorPos(originalPos, 0.4f).SetEase(Ease.OutQuart);
        }

        // 右侧编辑区域入场动画
        if (varSelectedChessContainer != null && varSelectedChessContainer.transform.parent != null)
        {
            var editArea = varSelectedChessContainer.transform.parent.parent;
            if (editArea != null)
            {
                var canvasGroup = editArea.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = editArea.gameObject.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 0;
                canvasGroup.DOFade(1, 0.4f).SetEase(Ease.OutQuart).SetDelay(0.15f);
            }
        }
    }

    /// <summary>
    /// 播放创建新预设动画
    /// </summary>
    private void PlayCreatePresetAnimation(GameObject slotItem)
    {
        if (slotItem == null)
            return;

        slotItem.transform.localScale = Vector3.one * 0.5f;
        slotItem
            .transform.DOScale(1.1f, 0.25f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                slotItem.transform.DOScale(1.0f, 0.1f);
            });

        var canvasGroup = slotItem.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = slotItem.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.35f);
    }

    /// <summary>
    /// 为按钮添加悬停和点击动画
    /// </summary>
    private void AddButtonAnimation(Button button)
    {
        if (button == null)
            return;

        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // 悬停进入
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter,
        };
        pointerEnter.callback.AddListener(
            (data) =>
            {
                if (button.interactable)
                    button.transform.DOScale(1.05f, 0.2f).SetEase(Ease.OutQuart);
            }
        );
        eventTrigger.triggers.Add(pointerEnter);

        // 悬停退出
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit,
        };
        pointerExit.callback.AddListener(
            (data) =>
            {
                button.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuart);
            }
        );
        eventTrigger.triggers.Add(pointerExit);

        // 点击按压
        var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown,
        };
        pointerDown.callback.AddListener(
            (data) =>
            {
                if (button.interactable)
                {
                    button
                        .transform.DOScale(0.95f, 0.075f)
                        .OnComplete(() =>
                        {
                            button.transform.DOScale(1.05f, 0.075f);
                        });
                }
            }
        );
        eventTrigger.triggers.Add(pointerDown);
    }

    /// <summary>
    /// 播放棋子/策略卡添加动画
    /// </summary>
    private void PlayItemAddAnimation(GameObject poolItem, GameObject selectedItem)
    {
        if (poolItem == null || selectedItem == null)
            return;

        // 可选池项脉冲
        poolItem
            .transform.DOScale(1.15f, 0.1f)
            .SetEase(Ease.OutQuint)
            .OnComplete(() =>
            {
                poolItem.transform.DOScale(1.0f, 0.1f).SetEase(Ease.OutQuint);
            });

        // 可选池项变灰（动画）
        var poolCanvasGroup = poolItem.GetComponent<CanvasGroup>();
        if (poolCanvasGroup == null)
            poolCanvasGroup = poolItem.AddComponent<CanvasGroup>();
        poolCanvasGroup.DOFade(0.4f, 0.25f).SetEase(Ease.OutQuart);

        // 已选项弹出
        selectedItem.transform.localScale = Vector3.one * 0.5f;
        var selectedCanvasGroup = selectedItem.GetComponent<CanvasGroup>();
        if (selectedCanvasGroup == null)
            selectedCanvasGroup = selectedItem.AddComponent<CanvasGroup>();
        selectedCanvasGroup.alpha = 0;

        selectedItem
            .transform.DOScale(1.1f, 0.2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                selectedItem.transform.DOScale(1.0f, 0.1f).SetEase(Ease.OutQuart);
            });
        selectedCanvasGroup.DOFade(1, 0.3f).SetEase(Ease.OutQuart);
    }

    /// <summary>
    /// 播放棋子/策略卡移除动画
    /// </summary>
    private void PlayItemRemoveAnimation(
        GameObject selectedItem,
        GameObject poolItem,
        System.Action onComplete
    )
    {
        if (selectedItem == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 已选项缩放淡出
        var selectedCanvasGroup = selectedItem.GetComponent<CanvasGroup>();
        if (selectedCanvasGroup == null)
            selectedCanvasGroup = selectedItem.AddComponent<CanvasGroup>();

        selectedItem
            .transform.DOScale(0.8f, 0.15f)
            .SetEase(Ease.InQuart)
            .OnComplete(() =>
            {
                selectedItem.transform.DOScale(0, 0.1f).SetEase(Ease.InQuart);
            });

        selectedCanvasGroup
            .DOFade(0, 0.25f)
            .SetEase(Ease.InQuart)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });

        // 可选池项恢复并脉冲
        if (poolItem != null)
        {
            var poolCanvasGroup = poolItem.GetComponent<CanvasGroup>();
            if (poolCanvasGroup == null)
                poolCanvasGroup = poolItem.AddComponent<CanvasGroup>();

            poolCanvasGroup.DOFade(1.0f, 0.3f).SetEase(Ease.OutQuart);

            poolItem
                .transform.DOScale(1.1f, 0.15f)
                .SetDelay(0.1f)
                .SetEase(Ease.OutQuint)
                .OnComplete(() =>
                {
                    poolItem.transform.DOScale(1.0f, 0.15f).SetEase(Ease.OutQuint);
                });
        }
    }

    /// <summary>
    /// 播放数量计数器更新动画（纯数字滚动，不改颜色）
    /// </summary>
    private void PlayCounterUpdateAnimation(TMP_Text counterText, string newText)
    {
        if (counterText == null)
            return;

        var originalPos = counterText.transform.localPosition;

        // 旧数字向上淡出
        counterText.DOFade(0, 0.125f);
        counterText
            .transform.DOLocalMoveY(originalPos.y + 10, 0.125f)
            .OnComplete(() =>
            {
                counterText.text = newText;
                counterText.transform.localPosition = new Vector3(
                    originalPos.x,
                    originalPos.y - 10,
                    originalPos.z
                );

                // 新数字从下方淡入
                counterText.DOFade(1, 0.125f);
                counterText.transform.DOLocalMoveY(originalPos.y, 0.125f);
            });
    }

    /// <summary>
    /// 播放达到上限的抖动提示（变红 → 抖动 → 恢复默认色）
    /// </summary>
    private void PlayLimitShakeAnimation(TMP_Text counterText, Color defaultColor)
    {
        if (counterText == null)
            return;

        DOTween.Kill(counterText);
        counterText
            .DOColor(Color.red, 0.1f)
            .OnComplete(() =>
            {
                counterText
                    .transform.DOShakeRotation(0.3f, new Vector3(0, 0, 5), 10)
                    .OnComplete(() =>
                    {
                        counterText.DOColor(defaultColor, 0.2f);
                    });
            });
    }

    /// <summary>
    /// 播放设为默认动画
    /// </summary>
    private void PlaySetDefaultAnimation(GameObject defaultBadge)
    {
        if (defaultBadge == null)
            return;

        defaultBadge.transform.localScale = Vector3.zero;
        defaultBadge
            .transform.DOScale(1.2f, 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                defaultBadge.transform.DOScale(1.0f, 0.1f);
            });

        defaultBadge.transform.DORotate(new Vector3(0, 0, 360), 0.4f, RotateMode.FastBeyond360);
    }

    /// <summary>
    /// 清理所有动画
    /// </summary>
    private void KillAllAnimations()
    {
        if (varPresetSlotContainer != null)
            DOTween.Kill(varPresetSlotContainer.transform);

        if (varSelectedChessContainer != null && varSelectedChessContainer.transform.parent != null)
        {
            var editArea = varSelectedChessContainer.transform.parent.parent;
            if (editArea != null)
                DOTween.Kill(editArea);
        }

        foreach (var item in m_PresetSlotItems)
        {
            if (item != null)
                DOTween.Kill(item.transform);
        }

        foreach (var item in m_SelectedChessItems)
        {
            if (item != null)
                DOTween.Kill(item.transform);
        }

        foreach (var item in m_PoolChessItemsDict.Values)
        {
            if (item != null)
                DOTween.Kill(item.transform);
        }

        foreach (var item in m_SelectedCardItems)
        {
            if (item != null)
                DOTween.Kill(item.transform);
        }

        foreach (var item in m_PoolCardItemsDict.Values)
        {
            if (item != null)
                DOTween.Kill(item.transform);
        }
    }

    #endregion
}
