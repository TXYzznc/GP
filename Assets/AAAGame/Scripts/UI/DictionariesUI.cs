using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴主界面 - 展示所有分类的图鉴条目
/// 左侧7个分类Tab（直接在Prefab中放置），中间网格展示，右侧详情面板
/// </summary>
public partial class DictionariesUI : UIFormBase
{
    #region 常量

    /// <summary>分类配置</summary>
    private static readonly (string Name, DictionaryCategory Category)[] CategoryConfig = new[]
    {
        ("棋子", DictionaryCategory.Chess),
        ("策略卡", DictionaryCategory.StrategyCard),
        ("敌人", DictionaryCategory.Enemy),
        ("装备", DictionaryCategory.Equipment),
        ("宝物", DictionaryCategory.Treasure),
        ("消耗品", DictionaryCategory.Consumable),
        ("任务道具", DictionaryCategory.QuestItem),
    };

    #endregion

    #region 字段

    private int m_CurrentCategoryIndex = 0;
    private List<DictionarySlot> m_Slots = new();
    private bool m_DetailVisible = false;

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 绑定分类Tab按钮（在Prefab中预置的7个按钮）
        BindCategoryButtons();

        // 绑定关闭按钮
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);

    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 默认选中棋子分类
        m_CurrentCategoryIndex = 0;
        HideDetail();

        // 刷新分类Tab状态
        RefreshCategoryTabs();

        // 刷新内容
        RefreshContent();

        // 刷新总进度
        RefreshTotalProgress();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理动态生成的格子
        ClearSlots();

        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
    }

    #endregion

    #region 分类Tab

    /// <summary>
    /// 绑定分类按钮（Prefab中预置7个Button）
    /// varCategoryBtns 是一个Button数组，由Variables自动生成
    /// </summary>
    private void BindCategoryButtons()
    {
        if (varCategoryBtns == null) return;

        for (int i = 0; i < varCategoryBtns.Length && i < CategoryConfig.Length; i++)
        {
            int index = i; // 闭包捕获
            varCategoryBtns[i].onClick.AddListener(() => OnCategoryClicked(index));

            // 设置按钮文字
            var text = varCategoryBtns[i].GetComponentInChildren<Text>();
            if (text != null)
                text.text = CategoryConfig[i].Name;
        }
    }

    private void OnCategoryClicked(int index)
    {
        if (index == m_CurrentCategoryIndex) return;

        m_CurrentCategoryIndex = index;
        HideDetail();
        RefreshCategoryTabs();
        RefreshContent();
    }

    /// <summary>
    /// 刷新分类Tab的选中状态和进度
    /// </summary>
    private void RefreshCategoryTabs()
    {
        if (varCategoryBtns == null) return;

        for (int i = 0; i < varCategoryBtns.Length && i < CategoryConfig.Length; i++)
        {
            var btn = varCategoryBtns[i];
            bool isSelected = (i == m_CurrentCategoryIndex);

            // 高亮选中的Tab（通过颜色区分）
            var colors = btn.colors;
            colors.normalColor = isSelected ? new Color(0.83f, 0.66f, 0.33f, 0.3f) : new Color(1f, 1f, 1f, 0.05f);
            btn.colors = colors;

            // 更新进度文字（如果Tab下有进度Text）
            var countTexts = btn.GetComponentsInChildren<Text>();
            if (countTexts.Length >= 2) // 第二个Text用作进度
            {
                var cat = CategoryConfig[i].Category;
                int unlocked = DictionaryManager.Instance.GetUnlockedCount(cat);
                int total = DictionaryManager.Instance.GetTotalCount(cat);
                countTexts[1].text = $"{unlocked}/{total}";
            }
        }

        // 更新顶部标题
        if (varCategoryTitle != null)
            varCategoryTitle.text = CategoryConfig[m_CurrentCategoryIndex].Name;

        // 更新分类进度
        RefreshCategoryProgress();
    }

    #endregion

    #region 内容刷新

    /// <summary>
    /// 刷新当前分类的网格内容
    /// </summary>
    private void RefreshContent()
    {
        ClearSlots();

        var category = CategoryConfig[m_CurrentCategoryIndex].Category;
        var allIds = DictionaryManager.Instance.GetAllIds(category);

        if (varSlotTemplate == null || varItemContent == null)
        {
            DebugEx.Error("DictionariesUI", "格子模板或内容容器未设置");
            return;
        }

        for (int i = 0; i < allIds.Count; i++)
        {
            var entryData = DictionaryManager.Instance.GetEntryData(category, allIds[i]);

            // 实例化格子
            var slotGo = Instantiate(varSlotTemplate, varItemContent);
            slotGo.SetActive(true);

            var slot = slotGo.GetComponent<DictionarySlot>();
            if (slot != null)
            {
                slot.SetSlotIndex(i);
                slot.SetData(entryData, OnSlotClicked);
                m_Slots.Add(slot);
            }
        }

        DebugEx.LogModule("DictionariesUI", $"刷新图鉴: {category}, 共{allIds.Count}条");
    }

    /// <summary>
    /// 清理所有动态生成的格子
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in m_Slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        m_Slots.Clear();
    }

    #endregion

    #region 进度显示

    private void RefreshCategoryProgress()
    {
        var category = CategoryConfig[m_CurrentCategoryIndex].Category;
        int unlocked = DictionaryManager.Instance.GetUnlockedCount(category);
        int total = DictionaryManager.Instance.GetTotalCount(category);

        if (varCategoryCount != null)
            varCategoryCount.text = $"{unlocked} / {total}";

        if (varCategoryProgress != null)
            varCategoryProgress.value = total > 0 ? (float)unlocked / total : 0f;
    }

    private void RefreshTotalProgress()
    {
        DictionaryManager.Instance.GetTotalProgress(out int unlocked, out int total);

        if (varTotalProgressText != null)
            varTotalProgressText.text = $"{unlocked} / {total}";

        if (varTotalProgress != null)
            varTotalProgress.value = total > 0 ? (float)unlocked / total : 0f;
    }

    #endregion

    #region 详情面板

    private void OnSlotClicked(DictionaryEntryData entryData)
    {
        ShowDetail(entryData);
    }

    /// <summary>
    /// 显示详情面板
    /// </summary>
    private void ShowDetail(DictionaryEntryData entryData)
    {
        if (varPanelDetail == null) return;

        varPanelDetail.SetActive(true);
        m_DetailVisible = true;

        if (!entryData.IsUnlocked)
        {
            // 未解锁：显示锁定信息
            if (varDetailName != null) varDetailName.text = "???";
            if (varDetailDesc != null) varDetailDesc.text = "尚未解锁，继续探索以发现此条目。";
            if (varDetailQuality != null) varDetailQuality.gameObject.SetActive(false);
            if (varDetailIcon != null) varDetailIcon.color = new Color(0.2f, 0.2f, 0.25f);
            if (varDetailLocked != null)
            {
                varDetailLocked.SetActive(true);
            }
            if (varPanelDetailAttrs != null) varPanelDetailAttrs.SetActive(false);
            return;
        }

        // 已解锁：显示完整信息
        if (varDetailLocked != null) varDetailLocked.SetActive(false);

        if (varDetailName != null)
        {
            varDetailName.text = entryData.Name;
            if (entryData.Quality > 0)
                varDetailName.color = RarityColorHelper.GetColor(entryData.Quality);
            else
                varDetailName.color = Color.white;
        }

        if (varDetailDesc != null)
            varDetailDesc.text = entryData.Description ?? "";

        // 品质标签
        if (varDetailQuality != null)
        {
            varDetailQuality.gameObject.SetActive(true);
            varDetailQuality.text = GetQualityLabel(entryData.Category, entryData.Quality);
            if (entryData.Quality > 0)
                varDetailQuality.color = RarityColorHelper.GetColor(entryData.Quality);
        }

        // 图标
        if (varDetailIcon != null && entryData.IconId > 0)
        {
            varDetailIcon.color = Color.white;
            LoadDetailIconAsync(entryData.IconId).Forget();
        }

        // 属性面板（棋子和装备显示属性）
        if (varPanelDetailAttrs != null)
        {
            bool showAttrs = entryData.Category == DictionaryCategory.Chess ||
                             entryData.Category == DictionaryCategory.Equipment;
            varPanelDetailAttrs.SetActive(showAttrs);

            if (showAttrs)
                RefreshDetailAttrs(entryData);
        }
    }

    private void HideDetail()
    {
        if (varPanelDetail != null)
            varPanelDetail.SetActive(false);
        m_DetailVisible = false;
    }

    private async UniTask LoadDetailIconAsync(int iconId)
    {
        if (iconId <= 0 || varDetailIcon == null) return;

        try
        {
            await GameExtension.ResourceExtension.LoadSpriteAsync(iconId, varDetailIcon, 1f, null);
            varDetailIcon.color = Color.white;
        }
        catch (Exception e)
        {
            DebugEx.Error("DictionariesUI", $"加载详情图标异常: {e.Message}");
        }
    }

    /// <summary>
    /// 刷新详情属性面板（棋子属性等）
    /// </summary>
    private void RefreshDetailAttrs(DictionaryEntryData entryData)
    {
        if (entryData.Category == DictionaryCategory.Chess)
        {
            var table = GF.DataTable.GetDataTable<SummonChessTable>();
            var row = table?.GetDataRow(entryData.Id);
            if (row == null) return;

            SetAttrText(varAttr1, "生命", row.MaxHp.ToString("F0"));
            SetAttrText(varAttr2, "攻击", row.AtkDamage.ToString("F0"));
            SetAttrText(varAttr3, "护甲", row.Armor.ToString("F0"));
            SetAttrText(varAttr4, "攻速", row.AtkSpeed.ToString("F2"));
        }
        else if (entryData.Category == DictionaryCategory.Equipment)
        {
            // 装备属性从 EquipmentTable 或 ItemTable 获取
            var table = GF.DataTable.GetDataTable<ItemTable>();
            var row = table?.GetDataRow(entryData.Id);
            if (row == null) return;

            SetAttrText(varAttr1, "品质", GetQualityLabel(entryData.Category, row.Quality));
            SetAttrText(varAttr2, "重量", row.Weight.ToString());
            SetAttrText(varAttr3, "耐久", row.MaxDurability.ToString());
            SetAttrText(varAttr4, "售价", row.SellPrice.ToString());
        }
    }

    private void SetAttrText(TextMeshProUGUI textComponent, string label, string value)
    {
        if (textComponent != null)
            textComponent.text = $"{label}: {value}";
    }

    #endregion

    #region 工具方法

    private string GetQualityLabel(DictionaryCategory category, int quality)
    {
        switch (category)
        {
            case DictionaryCategory.Chess:
                return quality switch
                {
                    1 => "蓝",
                    2 => "紫",
                    3 => "金",
                    4 => "炫彩",
                    _ => "未知"
                };
            case DictionaryCategory.StrategyCard:
                return quality switch
                {
                    1 => "普通",
                    2 => "稀有",
                    3 => "史诗",
                    4 => "传说",
                    _ => "未知"
                };
            case DictionaryCategory.Enemy:
                return $"难度 {new string('★', quality)}";
            default:
                return quality switch
                {
                    1 => "普通",
                    2 => "优秀",
                    3 => "稀有",
                    4 => "史诗",
                    5 => "传说",
                    _ => "未知"
                };
        }
    }

    private void OnClickClose()
    {
        CloseWithAnimation();
    }

    #endregion
}
