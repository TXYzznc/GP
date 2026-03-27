using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class InventoryUI : UIFormBase
{
    #region 字段

    private ItemType m_CurrentCategory = ItemType.Consumable; // 当前选中的分类
    private List<InventorySlot> m_FilteredSlots = new List<InventorySlot>(); // 过滤后的格子列表
    private List<InventorySlotUI> m_SlotUIList = new List<InventorySlotUI>(); // 格子UI列表
    private int m_FirstVisibleRowIndex = 0; // 第一个可见行的索引
    private const int COLUMNS_PER_ROW = 4; // 每行的列数（格子数）
    private const int VISIBLE_ROWS = 7; // 可见的行数
    private const int TOTAL_SLOTS = COLUMNS_PER_ROW * VISIBLE_ROWS; // 总格子数 28
    #endregion

    #region Unity 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        DebugEx.Log("InventoryUI", "背包UI初始化开始");

        // 配置ScrollRect为水平滚动
        ConfigureScrollRect();

        // 配置GridLayoutGroup为竖向排列
        ConfigureGridLayout();

        // 初始化格子UI列表
        InitializeSlotUIList();

        // 绑定按钮事件
        BindButtonEvents();

        // 绑定滚动事件
        varScrollView.onValueChanged.AddListener(OnScrollValueChanged);

        DebugEx.Success("InventoryUI", "背包UI初始化完成");
    }

    /// <summary>
    /// 配置ScrollRect为竖向滚动
    /// </summary>
    private void ConfigureScrollRect()
    {
        if (varScrollView == null)
        {
            DebugEx.Warning("InventoryUI", "ScrollRect未配置");
            return;
        }

        // 设置为竖向滚动
        varScrollView.horizontal = false;
        varScrollView.vertical = true;

        DebugEx.Log("InventoryUI", "ScrollRect已配置为竖向滚动");
    }

    /// <summary>
    /// 配置GridLayoutGroup为每行4个格子
    /// </summary>
    private void ConfigureGridLayout()
    {
        if (varContent == null)
        {
            DebugEx.Warning("InventoryUI", "GridLayoutGroup未配置");
            return;
        }

        // 设置为每行4个格子
        varContent.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        varContent.constraintCount = COLUMNS_PER_ROW; // 每行4个格子

        DebugEx.Log("InventoryUI", "GridLayoutGroup已配置为每行4个格子");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        DebugEx.Log("InventoryUI", "打开背包UI");

        // 监听背包变化事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChangedHandler;
        }

        // 刷新召唤师信息
        RefreshSummonerInfo();

        // 默认选中第一个分类（道具）
        SelectCategory(ItemType.Consumable);

        // 刷新背包显示
        RefreshInventory();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        // 取消监听背包变化事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChangedHandler;
        }

        DebugEx.Log("InventoryUI", "关闭背包UI");
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化格子UI列表
    /// </summary>
    private void InitializeSlotUIList()
    {
        DebugEx.Log("InventoryUI", "初始化背包格子UI");

        m_SlotUIList.Clear();

        // 获取Content下已有的所有InventorySlotUI
        var existingSlots = varContent.GetComponentsInChildren<InventorySlotUI>(true);

        // 如果格子数量不足,创建新的
        int neededCount = TOTAL_SLOTS - existingSlots.Length;
        for (int i = 0; i < neededCount; i++)
        {
            var slotGO = Instantiate(varInventorySlot, varContent.transform);
            slotGO.SetActive(true);
        }

        // 重新获取所有格子UI
        existingSlots = varContent.GetComponentsInChildren<InventorySlotUI>(true);

        // 添加到列表
        foreach (var slotUI in existingSlots)
        {
            m_SlotUIList.Add(slotUI);
            slotUI.gameObject.SetActive(true);
        }

        DebugEx.Success("InventoryUI", $"格子UI初始化完成,共 {m_SlotUIList.Count} 个");
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 关闭按钮
        varCloseBtn.onClick.AddListener(OnCloseBtnClick);

        // 分类按钮
        if (varLabel1Arr != null && varLabel1Arr.Length >= 4)
        {
            varLabel1Arr[0].onClick.AddListener(() => OnCategoryBtnClick(ItemType.Consumable));
            varLabel1Arr[1].onClick.AddListener(() => OnCategoryBtnClick(ItemType.Equipment));
            varLabel1Arr[2].onClick.AddListener(() => OnCategoryBtnClick(ItemType.Treasure));
            varLabel1Arr[3].onClick.AddListener(() => OnCategoryBtnClick(ItemType.Quest));
        }
    }

    #endregion

    #region 刷新显示

    /// <summary>
    /// 刷新召唤师信息
    /// </summary>
    private void RefreshSummonerInfo()
    {
        DebugEx.Log("InventoryUI", "刷新召唤师信息");

        // 获取当前召唤师配置
        var summonerConfig = PlayerAccountDataManager.Instance?.GetCurrentSummonerConfig();
        if (summonerConfig == null)
        {
            DebugEx.Warning("InventoryUI", "未找到召唤师配置");
            return;
        }

        // 加载召唤师立绘
        if (summonerConfig.PortraitId > 0)
        {
            LoadSummonerPortrait(summonerConfig.PortraitId);
        }

        // 刷新召唤师状态文本
        RefreshSummonerState(summonerConfig);
    }

    /// <summary>
    /// 刷新召唤师状态
    /// </summary>
    private void RefreshSummonerState(SummonerTable summonerConfig)
    {
        var playerData = PlayerAccountDataManager.Instance?.CurrentSaveData; // 修改为属性访问
        if (playerData == null)
        {
            return;
        }

        // 构建状态文本
        var stateText = $"<b>{summonerConfig.Name}</b>\n\n";
        stateText += $"生命: {summonerConfig.BaseHP}\n";
        stateText += $"灵力: {summonerConfig.BaseMP}\n";
        stateText += $"灵力恢复: {summonerConfig.MPRegen}\n";
        stateText += $"移动速度: {summonerConfig.MoveSpeed}\n";
        stateText += $"\n阶段: {summonerConfig.Phase}";

        varSummonerStateText.text = stateText;
    }

    /// <summary>
    /// 加载召唤师立绘
    /// </summary>
    private async void LoadSummonerPortrait(int portraitId)
    {
        try
        {
            DebugEx.Log("InventoryUI", $"开始加载召唤师立绘: PortraitId={portraitId}");

            if (varPlayerImg != null)
            {
                await GameExtension.ResourceExtension.LoadSpriteAsync(portraitId, varPlayerImg, 1f, null);
                DebugEx.Success("InventoryUI", "召唤师立绘加载成功");
            }
            else
            {
                DebugEx.Warning("InventoryUI", "召唤师立绘加载失败: Image为null");
            }
        }
        catch (Exception e)
        {
            DebugEx.Error("InventoryUI", $"加载召唤师立绘异常: Error:{e.Message}");
        }
    }

    /// <summary>
    /// 刷新背包显示
    /// </summary>
    private void RefreshInventory()
    {
        DebugEx.Log("InventoryUI", $"刷新背包显示,当前分类:{m_CurrentCategory}");

        // 过滤当前分类的物品
        FilterSlotsByCategory();

        // 重置滚动位置
        m_FirstVisibleRowIndex = 0;
        varScrollView.verticalNormalizedPosition = 1f;

        // 更新可见格子
        UpdateVisibleSlots();

        // 更新Content高度
        UpdateContentHeight();
    }

    /// <summary>
    /// 根据分类过滤格子
    /// </summary>
    private void FilterSlotsByCategory()
    {
        m_FilteredSlots.Clear();

        var allSlots = InventoryManager.Instance?.GetAllSlots();
        if (allSlots == null)
        {
            DebugEx.Warning("InventoryUI", "背包管理器未初始化");
            return;
        }

        foreach (var slot in allSlots)
        {
            if (slot.IsEmpty)
            {
                continue;
            }

            var item = slot.ItemStack.Item;
            if (item.Type == m_CurrentCategory)
            {
                m_FilteredSlots.Add(slot);
            }
        }

        DebugEx.Log("InventoryUI", $"过滤完成,共 {m_FilteredSlots.Count} 个物品");
    }

    /// <summary>
    /// 更新可见格子（行级虚拟滚动）
    /// </summary>
    private void UpdateVisibleSlots()
    {
        // 计算总行数（每行4个格子）
        int totalRows = Mathf.CeilToInt((float)m_FilteredSlots.Count / COLUMNS_PER_ROW);

        for (int i = 0; i < m_SlotUIList.Count; i++)
        {
            var slotUI = m_SlotUIList[i];
            
            // 计算当前格子所在的行和列
            int rowIndex = m_FirstVisibleRowIndex + (i / COLUMNS_PER_ROW);
            int columnInRow = i % COLUMNS_PER_ROW;
            
            // 计算在过滤列表中的索引
            int dataIndex = rowIndex * COLUMNS_PER_ROW + columnInRow;

            if (dataIndex < m_FilteredSlots.Count && rowIndex < totalRows)
            {
                // 有数据，显示物品
                var slot = m_FilteredSlots[dataIndex];
                slotUI.SetData(slot);
            }
            else
            {
                // 无数据，清空显示
                slotUI.Clear();
            }
        }

        DebugEx.Log("InventoryUI", $"更新可见格子 - 第一个可见行: {m_FirstVisibleRowIndex}, 总行数: {totalRows}");
    }

    /// <summary>
    /// 更新Content高度
    /// </summary>
    private void UpdateContentHeight()
    {
        // 计算需要的总行数
        int totalRows = Mathf.CeilToInt((float)m_FilteredSlots.Count / COLUMNS_PER_ROW);

        // 至少显示7行
        totalRows = Mathf.Max(totalRows, VISIBLE_ROWS);

        // 获取GridLayoutGroup的设置
        var gridLayout = varContent;
        float cellHeight = gridLayout.cellSize.y;
        float spacingY = gridLayout.spacing.y;
        float paddingTop = gridLayout.padding.top;
        float paddingBottom = gridLayout.padding.bottom;

        // 计算总高度
        float totalHeight =
            paddingTop + paddingBottom + (cellHeight * totalRows) + (spacingY * (totalRows - 1));

        // 设置Content高度
        var contentRect = varContent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);

        DebugEx.Log("InventoryUI", $"更新Content高度 - 总行数: {totalRows}, 总高度: {totalHeight}");
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseBtnClick()
    {
        DebugEx.Log("InventoryUI", "点击关闭按钮");
        OnClickClose(); // 修改为基类的方法
    }

    /// <summary>
    /// 分类按钮点击
    /// </summary>
    private void OnCategoryBtnClick(ItemType category)
    {
        DebugEx.Log("InventoryUI", $"切换分类:{category}");
        SelectCategory(category);
        RefreshInventory();
    }

    /// <summary>
    /// 选中分类
    /// </summary>
    private void SelectCategory(ItemType category)
    {
        m_CurrentCategory = category;

        // 更新按钮状态
        UpdateCategoryButtonState();
    }

    /// <summary>
    /// 更新分类按钮状态
    /// </summary>
    private void UpdateCategoryButtonState()
    {
        if (varLabel1Arr == null || varLabel1Arr.Length < 4)
        {
            return;
        }

        // 根据当前分类设置按钮颜色
        for (int i = 0; i < varLabel1Arr.Length; i++)
        {
            var btn = varLabel1Arr[i];
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                ItemType btnCategory = ItemType.None;
                switch (i)
                {
                    case 0:
                        btnCategory = ItemType.Consumable;
                        break;
                    case 1:
                        btnCategory = ItemType.Equipment;
                        break;
                    case 2:
                        btnCategory = ItemType.Treasure;
                        break;
                    case 3:
                        btnCategory = ItemType.Quest;
                        break;
                }

                // 选中状态用高亮颜色,未选中用普通颜色
                img.color = (btnCategory == m_CurrentCategory) ? Color.yellow : Color.white;
            }
        }
    }

    /// <summary>
    /// 滚动值变化（行级虚拟滚动）
    /// </summary>
    private void OnScrollValueChanged(Vector2 value)
    {
        // 获取GridLayoutGroup设置
        var gridLayout = varContent;
        float cellHeight = gridLayout.cellSize.y;
        float spacingY = gridLayout.spacing.y;
        float rowHeight = cellHeight + spacingY;

        // 计算当前滚动到的行数
        var contentRect = varContent.GetComponent<RectTransform>();
        float scrollY = contentRect.anchoredPosition.y;
        int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / rowHeight));

        // 如果行索引变化，更新显示
        if (firstVisibleRow != m_FirstVisibleRowIndex)
        {
            m_FirstVisibleRowIndex = firstVisibleRow;
            UpdateVisibleSlots();
            DebugEx.Log("InventoryUI", $"滚动到行 {firstVisibleRow}");
        }
    }

    /// <summary>
    /// 背包内容变化处理
    /// </summary>
    private void OnInventoryChangedHandler()
    {
        DebugEx.Log("InventoryUI", "收到背包变化通知,刷新显示");
        RefreshInventory();
    }

    #endregion
}
