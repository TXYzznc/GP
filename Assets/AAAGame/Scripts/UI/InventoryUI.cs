using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class InventoryUI : UIFormBase
{
    #region 字段

    private readonly List<InventorySlotUI> m_InventorySlots = new();
    private readonly List<InventorySlotUI> m_FastSlots = new(); // 快捷栏（预置 5 格）

    private const int COLUMNS_PER_ROW = 6;
    private const int SLOTS_PER_PAGE = 42; // 每页格子数（6列×7行）

    // 页签选中颜色
    private static readonly UnityEngine.Color s_TabSelectedColor = new(0.55f, 0.55f, 0.55f, 1f);
    private static readonly UnityEngine.Color s_TabNormalColor = UnityEngine.Color.white;

    private InventoryManager m_InventoryManager;
    private FastBarManager m_FastBarManager;

    // 容器组件
    private InventorySlotContainerImpl m_InventorySlotContainer;
    private FastBarSlotContainerImpl m_FastBarSlotContainer;
    private EquipSlotContainerImpl m_EquipSlotContainer;

    // 分页
    private int m_CurrentPage = 0;
    private readonly List<Button> m_PageLabels = new(); // 动态生成的页签按钮

    // 装备栏
    private readonly List<InventorySlotUI> m_EquipSlots = new();

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 初始化容器组件
        m_InventorySlotContainer = GetComponent<InventorySlotContainerImpl>();
        if (m_InventorySlotContainer == null)
            m_InventorySlotContainer = gameObject.AddComponent<InventorySlotContainerImpl>();

        m_FastBarSlotContainer = GetComponent<FastBarSlotContainerImpl>();
        if (m_FastBarSlotContainer == null)
            m_FastBarSlotContainer = gameObject.AddComponent<FastBarSlotContainerImpl>();

        m_EquipSlotContainer = GetComponent<EquipSlotContainerImpl>();
        if (m_EquipSlotContainer == null)
            m_EquipSlotContainer = gameObject.AddComponent<EquipSlotContainerImpl>();

        ConfigureScrollRect();
        ConfigureGridLayout();
        CollectSlotUIs();
        InitializeEquipSlots();
        BindButtonEvents();

        DebugEx.Success("InventoryUI", "背包UI初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        m_InventoryManager = InventoryManager.Instance;
        if (m_InventoryManager != null)
            m_InventoryManager.OnSlotChanged += OnInventorySlotChanged;

        m_FastBarManager = FastBarManager.Instance;
        if (!m_FastBarManager.IsInitialized)
            m_FastBarManager.Initialize();
        if (m_FastBarManager != null)
            m_FastBarManager.OnSlotChanged += OnFastBarSlotChanged;

        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager != null)
            warehouseManager.OnSlotChanged += OnWarehouseSlotChanged;

        m_CurrentPage = 0;
        BuildPageLabels();
        RefreshAll();
        OutputInventoryData();
        LockPlayerMovement(true);

        // 请求解锁鼠标（通过引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseUnlock();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        if (m_InventoryManager != null)
            m_InventoryManager.OnSlotChanged -= OnInventorySlotChanged;

        if (m_FastBarManager != null)
            m_FastBarManager.OnSlotChanged -= OnFastBarSlotChanged;

        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager != null)
            warehouseManager.OnSlotChanged -= OnWarehouseSlotChanged;

        LockPlayerMovement(false);

        // 请求锁定鼠标（通过引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseLock();
    }

    private void OnInventorySlotChanged(SlotChangeEventArgs args)
    {
        // 页数变化时重建页签
        if (PageCount != m_PageLabels.Count)
            BuildPageLabels();

        RefreshInventorySlotAt(args.SlotIndex);
        RefreshWeightState();
        RefreshEquipSlots();
    }

    private void OnFastBarSlotChanged(SlotChangeEventArgs args)
    {
        RefreshHotbarSlot(args.SlotIndex);
    }

    private void OnWarehouseSlotChanged(SlotChangeEventArgs args)
    {
        // 仓库变化不影响背包显示，不做全量刷新
        DebugEx.Log("InventoryUI", $"仓库格子变化: slot={args.SlotIndex}");
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 检查菜单外部点击关闭
        CheckContextMenuClickOutside();

        var input = PlayerInputManager.Instance;
        if (input == null)
            return;

        // A/D 翻页
        if (input.InventoryPagePrevTriggered)
            SwitchPage(m_CurrentPage - 1);
        else if (input.InventoryPageNextTriggered)
            SwitchPage(m_CurrentPage + 1);

        // 数字键 1-5 快捷使用
        for (int i = 1; i <= m_FastSlots.Count; i++)
        {
            if (input.GetHotbarKeyDown(i))
            {
                UseHotbarSlot(i - 1);
                break;
            }
        }
    }

    /// <summary>
    /// 检查菜单外部点击，自动关闭菜单
    /// 使用 EventSystem.IsPointerOverGameObject 判断是否点击在 UI 上
    /// </summary>
    private void CheckContextMenuClickOutside()
    {
        // 如果菜单未显示，不需要检查
        if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
            return;

        // 检查是否有鼠标点击
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            // 延迟一帧检查：先让 EventSystem 处理按钮点击，再判断是否关闭菜单
            // 这样可以避免菜单按钮点击被误判为"菜单外部点击"
            CheckMenuClickDelayedAsync().Forget();
        }
    }

    private async UniTask CheckMenuClickDelayedAsync()
    {
        // 等待一帧，让 EventSystem 处理完按钮点击事件，再判断是否关闭菜单
        await UniTask.Yield();

        if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
            return;

        var menuRect = m_CachedContextMenu.GetComponent<RectTransform>();
        if (menuRect == null)
            return;

        // Screen Space-Camera 模式必须传 worldCamera，否则坐标换算错误
        var parentCanvas = GetComponentInParent<Canvas>();
        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition, cam))
        {
            m_CachedContextMenu.HideContextMenu();
            DebugEx.Log("InventoryUI", "菜单外部点击，关闭菜单");
        }
    }

    #endregion

    #region 初始化

    private void ConfigureScrollRect()
    {
        // ScrollRect 配置已在 Prefab 中完成，无需代码设置
    }

    private void ConfigureGridLayout()
    {
        if (varInventoryContent != null)
        {
            varInventoryContent.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            varInventoryContent.constraintCount = COLUMNS_PER_ROW;
        }

        // 快捷栏已在预制体中设置为固定竖直排列，无需代码配置
        DebugEx.Log("InventoryUI", "背包网格布局配置完成");
    }

    private void CollectSlotUIs()
    {
        // 背包格子：根据玩家数据初始化（可用格 + 锁定格）
        if (varInventoryContent != null && varInventorySlotUI != null)
        {
            var existingSlots = varInventoryContent.GetComponentsInChildren<InventorySlotUI>(true);
            if (existingSlots.Length > 0)
            {
                // 手动放置的格子，直接收集
                foreach (var slot in existingSlots)
                {
                    slot.SetContainerType(SlotContainerType.Inventory);
                    slot.SetSlotContainer(m_InventorySlotContainer);
                    m_InventorySlots.Add(slot);
                }
            }
            else
            {
                // 动态创建：获取玩家背包容量，生成可用格 + 锁定格
                int inventorySize = GetPlayerInventorySize();
                int totalSlots = inventorySize + GetLockedSlotsCount(inventorySize);

                for (int i = 0; i < totalSlots; i++)
                {
                    var go = Instantiate(varInventorySlotUI, varInventoryContent.transform);
                    if (go.TryGetComponent<InventorySlotUI>(out var slot))
                    {
                        slot.SetSlotIndex(i);
                        slot.SetContainerType(SlotContainerType.Inventory);
                        slot.SetSlotContainer(m_InventorySlotContainer);

                        // 设置是否可用（前 inventorySize 个可用，之后的锁定）
                        bool available = i < inventorySize;
                        slot.SetAvailable(available);

                        m_InventorySlots.Add(slot);
                    }
                }
            }

            DebugEx.Log("InventoryUI", $"背包格子初始化完成 - 总数:{m_InventorySlots.Count}");
        }

        // 快捷栏格子（使用预置的 varInventorySlotUI1Arr）
        if (varInventorySlotUI1Arr != null)
        {
            for (int i = 0; i < varInventorySlotUI1Arr.Length; i++)
            {
                var go = varInventorySlotUI1Arr[i];
                if (go != null && go.TryGetComponent<InventorySlotUI>(out var slotUI))
                {
                    slotUI.SetSlotIndex(i);
                    slotUI.SetContainerType(SlotContainerType.FastBar);
                    slotUI.SetSlotContainer(m_FastBarSlotContainer);
                    slotUI.SetAvailable(true);
                    m_FastSlots.Add(slotUI);
                }
            }
        }

        DebugEx.Success(
            "InventoryUI",
            $"格子收集完成 - 背包栏:{m_InventorySlots.Count} 快捷栏:{m_FastSlots.Count}"
        );
    }

    /// <summary>
    /// 获取玩家当前的背包容量
    /// </summary>
    private int GetPlayerInventorySize()
    {
        var playerTable = GF.DataTable.GetDataTable<PlayerDataTable>();
        if (playerTable == null)
            return 100; // 默认值

        var playerRow = playerTable.GetDataRow(1); // 假设 ID=1 是玩家配置
        return playerRow != null ? playerRow.InventorySize : 100;
    }

    /// <summary>
    /// 计算锁定格数量：n = (6 - InventorySize % 6) % 6
    /// 保证背包行数是完整的（每行 6 列）
    /// </summary>
    private int GetLockedSlotsCount(int inventorySize)
    {
        int remainder = inventorySize % 6;
        return remainder == 0 ? 0 : (6 - remainder);
    }

    private void BindButtonEvents()
    {
        if (varCloseBtn != null)
            varCloseBtn.onClick.AddListener(OnClickClose);

        if (varSortBtn != null)
            varSortBtn.onClick.AddListener(OnClickSort);
    }

    /// <summary>
    /// 初始化装备栏格子（OnInit 时调用一次）
    /// 预创建 9 个装备槽，始终显示这 9 个格子
    /// </summary>
    private void InitializeEquipSlots()
    {
        if (varEquipContent == null || varEquipSlotUI == null)
            return;

        // 预创建 9 个装备槽，始终显示（无论是否有装备）
        const int equipSlotsCount = 9;
        for (int i = 0; i < equipSlotsCount; i++)
        {
            var go = Instantiate(varEquipSlotUI, varEquipContent.transform);
            if (!go.TryGetComponent<InventorySlotUI>(out var slotUI))
                continue;

            slotUI.SetSlotIndex(i);
            slotUI.SetContainerType(SlotContainerType.Equip);
            slotUI.SetSlotContainer(m_EquipSlotContainer);
            slotUI.gameObject.SetActive(true); // 始终激活
            m_EquipSlots.Add(slotUI);
        }

        DebugEx.Log("InventoryUI", $"装备栏初始化完成，共 {m_EquipSlots.Count} 个装备槽");
    }

    #endregion

    #region 刷新

    /// <summary>
    /// 增量刷新单个背包格子
    /// </summary>
    private void RefreshInventorySlotAt(int slotIndex)
    {
        // 检查该格子是否在当前页内
        int pageStart = m_CurrentPage * SLOTS_PER_PAGE;
        int pageEnd = pageStart + SLOTS_PER_PAGE;
        if (slotIndex < pageStart || slotIndex >= pageEnd)
            return;

        if (slotIndex >= m_InventorySlots.Count)
            return;

        var slotUI = m_InventorySlots[slotIndex];
        if (slotUI == null || !slotUI.gameObject.activeSelf)
            return;

        var inventorySlot = m_InventoryManager.GetSlot(slotIndex);
        ItemStack itemStack = null;

        if (inventorySlot != null && !inventorySlot.IsEmpty)
        {
            var itemTable = GF.DataTable.GetDataTable<ItemTable>();
            var itemRow = itemTable?.GetDataRow(inventorySlot.ItemId);
            if (itemRow == null || itemRow.Type != 0)
            {
                itemStack = inventorySlot.ItemStack;
            }
        }

        slotUI.SetData(itemStack);
    }

    public void RefreshAll()
    {
        RefreshInventory();
        RefreshHotbar();
        RefreshEquipSlots();
        RefreshWeightState();
        RefreshSummonerState();
    }

    /// <summary>
    /// 输出背包当前所有数据
    /// </summary>
    private void OutputInventoryData()
    {
        if (m_InventoryManager == null)
            return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【背包内容】");
        var allSlots = m_InventoryManager.GetAllSlots();
        int totalItems = 0;
        foreach (var slot in allSlots)
        {
            if (slot != null && !slot.IsEmpty && slot.ItemStack?.Item != null)
            {
                sb.AppendLine($"  格子{slot.SlotIndex}: {slot.ItemStack.Item.Name} x{slot.Count}");
                totalItems += slot.Count;
            }
        }
        sb.AppendLine($"物品总数: {totalItems}");
        DebugEx.LogModule("InventoryUI", sb.ToString());
    }

    private void RefreshInventory()
    {
        if (m_InventoryManager == null)
        {
            DebugEx.Warning("InventoryUI", "RefreshInventory: m_InventoryManager 为空");
            return;
        }

        var allSlots = m_InventoryManager.GetAllSlots();
        var itemTable = GF.DataTable.GetDataTable<ItemTable>();
        int pageStart = m_CurrentPage * SLOTS_PER_PAGE;
        int pageEnd = pageStart + SLOTS_PER_PAGE;
        int inventorySize = GetPlayerInventorySize();

        DebugEx.Log(
            "InventoryUI",
            $"RefreshInventory: allSlots.Count={allSlots.Count}, inventorySize={inventorySize}, m_InventorySlots.Count={m_InventorySlots.Count}, currentPage={m_CurrentPage}"
        );

        for (int i = 0; i < m_InventorySlots.Count; i++)
        {
            // 不在当前页的格子隐藏
            bool inPage = i >= pageStart && i < pageEnd;
            m_InventorySlots[i].gameObject.SetActive(inPage);
            if (!inPage)
                continue;

            var slotUI = m_InventorySlots[i];

            // 更新格子可用性（是否已解锁）
            bool available = i < inventorySize;
            slotUI.SetAvailable(available);

            // 如果格子未解锁，清空显示
            if (!available)
            {
                slotUI.SetData(null);
                continue;
            }

            // 格子已解锁，设置物品数据（格子自动处理刷新）
            ItemStack itemStack = null;
            if (i < allSlots.Count && !allSlots[i].IsEmpty)
            {
                // 检查物品类型，过滤 Type 0 资源项
                var itemRow = itemTable?.GetDataRow(allSlots[i].ItemStack.Item.ItemId);
                if (itemRow != null && itemRow.Type == 0)
                {
                    // Type 0 资源项不显示在背包中
                    DebugEx.Log(
                        "InventoryUI",
                        $"过滤资源项: slot[{i}] = {allSlots[i].ItemStack.Item.Name} (Type 0)"
                    );
                    slotUI.SetData(null);
                    continue;
                }

                itemStack = allSlots[i].ItemStack;
                DebugEx.Log(
                    "InventoryUI",
                    $"显示物品: slot[{i}] = {itemStack.Item.Name} x{itemStack.Count}"
                );
            }

            slotUI.SetData(itemStack);
        }

        DebugEx.Success("InventoryUI", "RefreshInventory 完成");
    }

    private void RefreshHotbar()
    {
        DebugEx.Log("InventoryUI", "开始刷新快捷栏");
        if (m_FastBarManager != null && m_FastBarManager.IsInitialized)
        {
            for (int i = 0; i < m_FastSlots.Count; i++)
                RefreshHotbarSlot(i);
        }
        DebugEx.Success("InventoryUI", "快捷栏刷新完成");
    }

    private void RefreshHotbarSlot(int hotbarIndex)
    {
        if (hotbarIndex >= m_FastSlots.Count || m_FastBarManager == null)
            return;

        var slotUI = m_FastSlots[hotbarIndex];
        if (slotUI == null)
            return;

        var fastBarSlot = m_FastBarManager.GetSlot(hotbarIndex);
        ItemStack itemStack = null;

        if (fastBarSlot != null && !fastBarSlot.IsEmpty)
        {
            itemStack = fastBarSlot.ItemStack;
            DebugEx.Log("InventoryUI", $"快捷栏[{hotbarIndex}] 显示物品: {itemStack.Item.Name}");
        }
        else
        {
            DebugEx.Log("InventoryUI", $"快捷栏[{hotbarIndex}] 为空");
        }

        // 统一使用 SetData，格子自动处理UI刷新和背景颜色
        slotUI.SetData(itemStack);
    }

    /// <summary>
    /// 刷新装备栏：过滤背包中 Type == Equipment(4) 的物品并显示
    /// 如果装备数量超过当前槽位，动态创建新槽位
    /// </summary>
    private void RefreshEquipSlots()
    {
        if (m_InventoryManager == null)
        {
            DebugEx.Warning("InventoryUI", "RefreshEquipSlots: m_InventoryManager 为空");
            return;
        }

        if (varEquipSlotUI == null)
        {
            DebugEx.Warning("InventoryUI", "RefreshEquipSlots: varEquipSlotUI 为空");
            return;
        }

        if (varEquipContent == null)
        {
            DebugEx.Warning("InventoryUI", "RefreshEquipSlots: varEquipContent 为空");
            return;
        }

        var itemTable = GF.DataTable.GetDataTable<ItemTable>();
        var allSlots = m_InventoryManager.GetAllSlots();

        // 收集背包中所有 Equipment 类型的物品（过滤 Type 0 资源项）
        int equipmentCount = 0;
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot.IsEmpty)
                continue;

            var row = itemTable?.GetDataRow(slot.ItemStack.Item.ItemId);
            if (row != null && row.Type == (int)ItemType.Equipment)
            {
                equipmentCount++;
                DebugEx.Log(
                    "InventoryUI",
                    $"找到装备: {slot.ItemStack.Item.Name} (ID:{slot.ItemStack.Item.ItemId})"
                );
            }
        }

        DebugEx.Log(
            "InventoryUI",
            $"装备栏刷新: 找到 {equipmentCount} 个装备，可用槽位 {m_EquipSlots.Count}"
        );

        // 显示装备物品（使用预创建的9个格子）
        int displayIndex = 0;
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (slot.IsEmpty)
                continue;

            var row = itemTable?.GetDataRow(slot.ItemStack.Item.ItemId);
            if (row == null || row.Type != (int)ItemType.Equipment)
                continue;

            // 显示装备
            if (displayIndex >= m_EquipSlots.Count)
                break; // 不应该发生，但防卫性编程

            var equipSlot = m_EquipSlots[displayIndex];
            equipSlot.gameObject.SetActive(true);

            // 统一使用 SetData，格子自动处理UI刷新和背景颜色
            equipSlot.SetData(slot.ItemStack);
            displayIndex++;
        }

        // 清空未显示的装备槽（始终激活所有格子）
        for (int i = displayIndex; i < m_EquipSlots.Count; i++)
        {
            m_EquipSlots[i].gameObject.SetActive(true); // 始终激活，不隐藏

            // 统一使用 SetData(null) 清空显示
            m_EquipSlots[i].SetData(null);
        }

        DebugEx.Success(
            "InventoryUI",
            $"装备栏刷新完成: 显示 {displayIndex} 个装备，总槽位 {m_EquipSlots.Count}"
        );
    }

    private void RefreshSummonerState()
    {
        if (varSummonerStateText == null)
            return;

        var saveData = PlayerAccountDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
        {
            varSummonerStateText.text = string.Empty;
            return;
        }

        // 召唤师名称 + 阶段
        var summonerConfig = PlayerAccountDataManager.Instance.GetCurrentSummonerConfig();
        string summonerName = summonerConfig != null ? summonerConfig.Name : "未知";
        string phaseName =
            summonerConfig != null ? $"阶段{ToRoman(summonerConfig.Phase)}" : string.Empty;

        // 等级 + 经验
        int level = saveData.GlobalLevel;
        int currentExp = saveData.CurrentExp;
        int requiredExp = 0;
        var playerTable = GF.DataTable.GetDataTable<PlayerDataTable>();
        if (playerTable != null)
        {
            var levelRow = playerTable.GetDataRow(r => r.Level == level);
            if (levelRow != null)
                requiredExp = levelRow.RequiredExp;
        }

        varSummonerStateText.text =
            $"{summonerName} · {phaseName}\nLv.{level}  经验: {currentExp} / {requiredExp}";
    }

    private static string ToRoman(int phase)
    {
        return phase switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => phase.ToString(),
        };
    }

    private void RefreshWeightState()
    {
        if (varWeightStateText == null)
        {
            DebugEx.Warning("InventoryUI", "RefreshWeightState: varWeightStateText 未连接");
            return;
        }

        if (m_InventoryManager == null)
        {
            varWeightStateText.text = "负重: 0";
            DebugEx.Warning("InventoryUI", "RefreshWeightState: m_InventoryManager 为空");
            return;
        }

        float currentWeight = m_InventoryManager.CalculateCurrentWeight();
        varWeightStateText.text = $"负重: {currentWeight:F0}";
        DebugEx.Log("InventoryUI", $"RefreshWeightState: 负重 = {currentWeight}");
    }

    #endregion

    #region 快捷栏操作

    /// <summary>
    /// 获取指定快捷栏槽位的物品（供外部查询）
    /// </summary>
    public InventorySlot GetFastBarSlot(int hotbarIndex)
    {
        if (m_FastBarManager == null)
            return null;
        return m_FastBarManager.GetSlot(hotbarIndex);
    }

    private void UseHotbarSlot(int hotbarIndex)
    {
        if (m_FastBarManager == null || m_InventoryManager == null)
            return;

        var fastSlot = m_FastBarManager.GetSlot(hotbarIndex);
        if (fastSlot == null || fastSlot.IsEmpty)
            return;

        var item = fastSlot.ItemStack.Item;
        // TODO: 调用物品使用逻辑
        DebugEx.Log("InventoryUI", $"使用快捷栏物品: {item.Name}");

        // 使用后刷新快捷栏显示
        RefreshHotbarSlot(hotbarIndex);
    }

    #endregion

    #region 玩家移动锁定

    private void LockPlayerMovement(bool locked)
    {
        // TODO: 接入玩家控制器
        DebugEx.Log("InventoryUI", $"玩家移动已{(locked ? "锁定" : "解锁")}");
    }

    #endregion

    #region 物品详情显示

    /// <summary>
    /// 显示物品详情
    /// </summary>
    public void ShowItemDetail(ItemStack itemStack)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            ClearItemDetail();
            return;
        }

        var item = itemStack.Item;
        var itemData = item.ItemData;

        DebugEx.Log("InventoryUI", $"显示物品详情: {item.Name}");

        // 显示物品名称
        if (varItemName != null)
        {
            varItemName.text = item.Name;
        }

        // 显示物品图标
        if (varItemDetailImg != null)
        {
            _ = LoadItemDetailIconAsync(itemData.GetIconId());
        }

        // 显示稀有度
        if (varRarityText != null)
        {
            var itemTable = GF.DataTable.GetDataTable<ItemTable>();
            var row = itemTable?.GetDataRow(item.ItemId);
            if (row != null)
            {
                varRarityText.text = $"品质: {row.Quality}";

                // 获取品质对应的颜色
                var rarityColor = RarityColorHelper.GetColor(row.Quality);
                varRarityText.color = rarityColor;
            }
        }

        // 显示重量
        if (varWeightText != null)
        {
            var itemTable = GF.DataTable.GetDataTable<ItemTable>();
            var row = itemTable?.GetDataRow(item.ItemId);
            if (row != null)
            {
                varWeightText.text = $"重量: {row.Weight}";
            }
        }

        // 显示描述
        if (varDescriptionText != null)
        {
            varDescriptionText.text = itemData.Description ?? "暂无描述";
        }

        DebugEx.Success("InventoryUI", $"物品详情显示完成: {item.Name}");
    }

    /// <summary>
    /// 清空物品详情
    /// </summary>
    private void ClearItemDetail()
    {
        if (varItemName != null)
            varItemName.text = "";

        if (varItemDetailImg != null)
        {
            varItemDetailImg.sprite = null;
            varItemDetailImg.color = new Color(1, 1, 1, 0);
        }

        if (varRarityText != null)
            varRarityText.text = "";

        if (varWeightText != null)
            varWeightText.text = "";

        if (varDescriptionText != null)
            varDescriptionText.text = "";

        DebugEx.Log("InventoryUI", "物品详情已清空");
    }

    /// <summary>
    /// 异步加载物品详情图标
    /// </summary>
    private async UniTask LoadItemDetailIconAsync(int iconId)
    {
        if (iconId <= 0)
        {
            DebugEx.Warning("InventoryUI", "物品图标ID无效");
            return;
        }

        try
        {
            if (varItemDetailImg != null)
            {
                await GameExtension.ResourceExtension.LoadSpriteAsync(
                    iconId,
                    varItemDetailImg,
                    1f,
                    null
                );
                varItemDetailImg.color = Color.white;
                DebugEx.Log("InventoryUI", $"物品详情图标加载成功: IconId={iconId}");
            }
        }
        catch (System.Exception e)
        {
            DebugEx.Error(
                "InventoryUI",
                $"加载物品详情图标异常: IconId={iconId}, Error:{e.Message}"
            );
        }
    }

    /// <summary>
    /// 获取物品上下文菜单
    /// </summary>
    public ItemContextMenu GetItemContextMenu()
    {
        if (varItemContextMenu == null)
        {
            DebugEx.Warning("InventoryUI", "varItemContextMenu 未设置");
            return null;
        }

        return varItemContextMenu.GetComponent<ItemContextMenu>();
    }

    #endregion

    #region 分页

    private int PageCount =>
        m_InventorySlots.Count == 0
            ? 1
            : Mathf.CeilToInt((float)m_InventorySlots.Count / SLOTS_PER_PAGE);

    /// <summary>
    /// 构建页签按钮（每次打开背包时重建）
    /// </summary>
    private void BuildPageLabels()
    {
        if (varPages == null || varPageLabelItem == null)
            return;

        foreach (var btn in m_PageLabels)
            if (btn != null)
                Destroy(btn.gameObject);
        m_PageLabels.Clear();

        for (int i = 0; i < PageCount; i++)
        {
            int pageIndex = i;
            var go = Instantiate(varPageLabelItem.gameObject, varPages.transform);
            if (!go.TryGetComponent(out Button btn))
                continue;

            var txt = go.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = (i + 1).ToString();

            btn.onClick.AddListener(() => SwitchPage(pageIndex));
            m_PageLabels.Add(btn);
        }

        RefreshPageLabelHighlight();
    }

    /// <summary>切换到指定页（越界自动钳制）</summary>
    private void SwitchPage(int page)
    {
        int clamped = Mathf.Clamp(page, 0, PageCount - 1);
        if (clamped == m_CurrentPage)
            return;

        m_CurrentPage = clamped;
        RefreshPageLabelHighlight();
        RefreshInventory();
        DebugEx.Log("InventoryUI", $"切换到第 {m_CurrentPage + 1} 页");
    }

    /// <summary>高亮当前页签：选中页签颜色变深且不可点击，其余可点击</summary>
    private void RefreshPageLabelHighlight()
    {
        for (int i = 0; i < m_PageLabels.Count; i++)
        {
            var btn = m_PageLabels[i];
            if (btn == null)
                continue;

            bool selected = i == m_CurrentPage;
            btn.interactable = !selected;

            // 直接修改 Image 颜色表现选中态
            if (btn.TryGetComponent<Image>(out var img))
                img.color = selected ? s_TabSelectedColor : s_TabNormalColor;
        }
    }

    #endregion

    #region 整理功能

    /// <summary>
    /// 整理按钮点击事件
    /// </summary>
    private void OnClickSort()
    {
        if (m_InventoryManager == null)
        {
            DebugEx.Warning("InventoryUI", "InventoryManager 未初始化");
            return;
        }

        DebugEx.Log("InventoryUI", "开始整理背包");

        // 调用整理算法
        m_InventoryManager.SortInventory();

        // 刷新 UI
        RefreshAll();

        DebugEx.Success("InventoryUI", "背包整理完成");
    }

    #endregion

    #region 物品上下文菜单

    private ItemContextMenu m_CachedContextMenu;

    /// <summary>
    /// 显示物品上下文菜单（动态加载预制体版本）
    /// </summary>
    public void ShowItemContextMenu(ItemStack itemStack, int slotIndex, RectTransform slotRect)
    {
        if (itemStack == null || itemStack.IsEmpty)
        {
            DebugEx.Warning("InventoryUI", "ShowItemContextMenu: 物品为空");
            return;
        }

        // 获取或加载菜单预制体
        if (m_CachedContextMenu == null)
        {
            if (varItemContextMenu == null)
            {
                DebugEx.Error(
                    "InventoryUI",
                    "ShowItemContextMenu: varItemContextMenu 预制体未设置"
                );
                return;
            }

            // 动态加载菜单预制体到 Canvas（不是 InventoryContent）
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                DebugEx.Error("InventoryUI", "ShowItemContextMenu: 未找到 Canvas");
                return;
            }

            var menuGO = Instantiate(varItemContextMenu, canvas.transform);
            m_CachedContextMenu = menuGO.GetComponent<ItemContextMenu>();

            if (m_CachedContextMenu == null)
            {
                DebugEx.Error(
                    "InventoryUI",
                    "ShowItemContextMenu: 菜单预制体中没有 ItemContextMenu 组件"
                );
                Destroy(menuGO);
                return;
            }

            DebugEx.Log("InventoryUI", "ShowItemContextMenu: 菜单预制体已加载");
        }

        // 显示菜单
        m_CachedContextMenu.ShowContextMenu(itemStack, slotIndex, Vector2.zero, slotRect);
    }

    #endregion
}
