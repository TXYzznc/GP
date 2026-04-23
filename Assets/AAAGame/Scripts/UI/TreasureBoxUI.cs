using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class TreasureBoxUI : UIFormBase
{
    #region 字段

    private readonly List<InventorySlotUI> m_Slots = new();
    private TreasureBoxSlotContainerImpl m_Container;
    private ItemContextMenu m_CachedContextMenu;
    private bool m_IsAnimating;
    private int m_InitialSlotCount = -1;  // 首次打开时确定的格子数

    #endregion

    #region 动画参数

    private const float SLOT_ANIMATION_DURATION = 0.2f;  // 每个格子动画时长
    private const float SLOT_ANIMATION_DELAY = 0.05f;    // 格子间隔（产生瀑布效果）
    private const float SLOT_SCALE_START = 0.5f;         // 起始缩放
    private const float SLOT_ALPHA_START = 0f;           // 起始透明度

    #endregion

    #region 宝箱配置

    private const int SLOTS_PER_ROW = 6;  // 一行6个格子

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 容器由 OnOpen 时的参数传递，不使用默认容器
        BindButtonEvents();
        DebugEx.Success("TreasureBoxUI", "宝箱UI初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        SetPlayerInputEnabled(false);

        // 请求解锁鼠标（通过引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseUnlock();

        // 设置标题（参数应该是即时的）
        var treasureBoxName = Params?.Get("TreasureBoxName") as string ?? "宝箱";
        if (varTitleText != null)
            varTitleText.text = treasureBoxName;

        // 取消前一个容器的订阅（防止事件混淆）
        if (m_Container != null)
            m_Container.OnSlotChanged -= OnTreasureBoxChangedArgs;

        // 异步等待容器准备好，然后初始化
        WaitForContainerAndInitializeAsync().Forget();
    }

    /// <summary>
    /// 异步等待容器准备好，然后初始化UI
    /// </summary>
    private async UniTask WaitForContainerAndInitializeAsync()
    {
        // 先尝试直接获取一次
        m_Container = Params?.Get("TreasureBoxContainer") as TreasureBoxSlotContainerImpl;

        // 如果第一次失败，等待几帧再试（不要在WaitUntil中重复Get，避免引用混乱）
        if (m_Container == null)
        {
            const int maxRetries = 100;
            int retryCount = 0;

            await UniTask.WaitUntil(() =>
            {
                retryCount++;
                return retryCount >= maxRetries;
            }, cancellationToken: this.GetCancellationTokenOnDestroy());

            // 最后一次尝试获取
            m_Container = Params?.Get("TreasureBoxContainer") as TreasureBoxSlotContainerImpl;

            if (m_Container == null)
            {
                DebugEx.Error("TreasureBoxUI", "[WaitForContainerAndInitializeAsync] 超时：无法获取容器");
                return;
            }

            DebugEx.Log("TreasureBoxUI", "[WaitForContainerAndInitializeAsync] 延迟获取容器成功");
        }
        else
        {
            DebugEx.Log("TreasureBoxUI", "[WaitForContainerAndInitializeAsync] 即时获取容器成功");
        }

        InitializeTreasureBoxUI();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        // 取消订阅事件
        if (m_Container != null)
            m_Container.OnSlotChanged -= OnTreasureBoxChangedArgs;

        // 清空格子显示（不销毁 GameObject，下次 OnOpen 复用）
        ResetSlots();

        // 清空容器引用，防止下次打开不同宝箱时混淆
        m_Container = null;

        SetPlayerInputEnabled(true);

        // 请求锁定鼠标（通过引用计数管理）
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.RequestMouseLock();

        // 重置初始格子数，下次打开时重新计算（如果是其他宝箱）
        m_InitialSlotCount = -1;

        // 容器数据持久保存在 TreasureChestInteractable 中，下次打开时继续使用
    }

    /// <summary>
    /// 初始化宝箱UI（确保容器已准备好）
    /// </summary>
    private void InitializeTreasureBoxUI()
    {
        if (m_Container == null)
            return;

        // 订阅容器变化事件（仓库模式）
        m_Container.OnSlotChanged += OnTreasureBoxChangedArgs;

        // 首次打开时计算格子数（必须是6的倍数），后续保持不变
        if (m_InitialSlotCount < 0)
        {
            m_InitialSlotCount = CalculateSlotCount();
            DebugEx.Log("TreasureBoxUI", $"[InitializeTreasureBoxUI] 首次打开，确定格子数={m_InitialSlotCount}");
        }
        else
        {
            // 非首次打开，在复用格子前清理旧的事件订阅，防止事件累积
            foreach (var slot in m_Slots)
            {
                if (slot != null)
                    slot.ClearItemQuantitySubscription();
            }
        }

        BuildSlots(m_InitialSlotCount);

        // 输出宝箱当前数据
        OutputTreasureBoxData();

        RefreshSlots();

        // 初始化动画状态并播放开箱动效
        PlayTreasureOpenSequenceAsync().Forget();

        DebugEx.Log("TreasureBoxUI", "[InitializeTreasureBoxUI] 宝箱UI初始化完成");
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        CheckContextMenuClickOutside();
    }

    #endregion

    #region 动效

    /// <summary>
    /// 开箱完整流程：初始化动画状态 → 播放动效
    /// </summary>
    private async UniTask PlayTreasureOpenSequenceAsync()
    {
        // 1. 等一帧让所有格子的 OnInit 执行完成
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

        // 2. 初始化所有格子的动画状态（先设置初始值，再激活显示）
        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (i >= m_Slots.Count)
                continue;

            var slot = m_Slots[i];
            var rect = slot.GetComponent<RectTransform>();
            var canvasGroup = slot.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = slot.gameObject.AddComponent<CanvasGroup>();

            // 设置初始状态（小、透明）
            canvasGroup.alpha = SLOT_ALPHA_START;
            rect.localScale = Vector3.one * SLOT_SCALE_START;

            // 激活格子，准备播放动画
            if (!slot.gameObject.activeSelf)
                slot.gameObject.SetActive(true);
        }

        // 3. 播放开箱动效
        await PlayTreasureOpenAnimationAsync();
    }

    /// <summary>
    /// 播放开箱动效：物品逐个缩放+淡入显示
    /// 动效播放到一半时解锁交互，允许用户尽早操作格子
    /// </summary>
    private async UniTask PlayTreasureOpenAnimationAsync()
    {
        m_IsAnimating = true;

        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (!m_Slots[i].gameObject.activeSelf) continue;

            // 播放该格子的动画（延迟错开，产生瀑布效果）
            await UniTask.Delay((int)(SLOT_ANIMATION_DELAY * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());

            PlaySlotAnimationAsync(m_Slots[i]).Forget();
        }

        // 所有格子动画启动完成后，立即恢复按钮交互（允许用户尽早操作）
        m_IsAnimating = false;

        if (varCloseBtn != null) varCloseBtn.interactable = true;
        if (varTakeAllBtn != null) varTakeAllBtn.interactable = true;

        // 继续等待动效完全播放完成（按钮已可用，但格子还在动画中）
        float totalAnimationTime = (m_Slots.Count - 1) * SLOT_ANIMATION_DELAY + SLOT_ANIMATION_DURATION;
        await UniTask.Delay((int)(totalAnimationTime * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 单个格子的动画：从小到大缩放 + 淡入
    /// 初始状态由 PrepareSlotAnimationState() 提前设置
    /// </summary>
    private async UniTask PlaySlotAnimationAsync(InventorySlotUI slot)
    {
        var rect = slot.GetComponent<RectTransform>();
        var canvasGroup = slot.GetComponent<CanvasGroup>();

        if (rect == null || canvasGroup == null)
            return;

        // 缩放 + 淡入动画
        var scaleTween = rect.DOScale(Vector3.one, SLOT_ANIMATION_DURATION)
            .SetEase(Ease.OutBack)
            .SetLink(slot.gameObject);

        canvasGroup.DOFade(1f, SLOT_ANIMATION_DURATION)
            .SetEase(Ease.OutCubic)
            .SetLink(slot.gameObject);

        await scaleTween.AsyncWaitForCompletion();
    }

    #endregion

    #region 初始化

    private void BindButtonEvents()
    {
        if (varCloseBtn != null)
            varCloseBtn.onClick.AddListener(OnClickClose);

        if (varTakeAllBtn != null)
            varTakeAllBtn.onClick.AddListener(OnClickTakeAll);
    }

    /// <summary>
    /// 根据物品数量动态创建格子（先激活让 OnInit 执行，再设置初始动画状态）
    /// </summary>
    private void BuildSlots(int count)
    {
        if (varContent == null || varInventorySlotUI == null)
        {
            DebugEx.Error("TreasureBoxUI", "varContent 或 varInventorySlotUI 未设置");
            return;
        }

        // 创建新格子
        for (int i = m_Slots.Count; i < count; i++)
        {
            var go = Instantiate(varInventorySlotUI, varContent.transform);
            go.name = $"TreasureSlot_{i}";
            go.SetActive(true);  // 激活让 OnInit 执行

            var slotUI = go.GetComponent<InventorySlotUI>();
            if (slotUI == null)
            {
                DebugEx.Error("TreasureBoxUI", $"格子预制体 {i} 找不到 InventorySlotUI 组件");
                Destroy(go);
                continue;
            }

            slotUI.SetSlotIndex(i);
            slotUI.SetContainerType(SlotContainerType.TreasureBox);
            slotUI.SetSlotContainer(m_Container);
            slotUI.SetAvailable(true);
            m_Slots.Add(slotUI);
        }

        // 激活需要显示的格子（包括已存在但被禁用的格子），并重新设置容器
        for (int i = 0; i < count; i++)
        {
            if (i < m_Slots.Count)
            {
                m_Slots[i].SetSlotContainer(m_Container);  // ⭐ 重新绑定容器
                if (!m_Slots[i].gameObject.activeSelf)
                    m_Slots[i].gameObject.SetActive(true);
            }
        }

        // 隐藏超出数量的格子
        for (int i = count; i < m_Slots.Count; i++)
            m_Slots[i].gameObject.SetActive(false);
    }

    #endregion

    #region 刷新

    public void RefreshSlots()
    {
        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (!m_Slots[i].gameObject.activeSelf) continue;

            var slot = m_Container.GetSlot(i);
            var itemStack = (slot != null && !slot.IsEmpty) ? slot.ItemStack : null;
            m_Slots[i].SetData(itemStack);
        }

        DebugEx.Log("TreasureBoxUI", "宝箱格子刷新完成");
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 宝箱内容变化回调（增量刷新）
    /// </summary>
    private void OnTreasureBoxChangedArgs(SlotChangeEventArgs args)
    {
        if (args.SlotIndex < 0)
        {
            // 全量刷新（初始化或全部拿走）
            RefreshSlots();
        }
        else
        {
            // 增量刷新：只刷新变化的格子
            RefreshSlot(args.SlotIndex);
        }
    }

    /// <summary>
    /// 刷新单个格子
    /// </summary>
    private void RefreshSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_Slots.Count)
            return;

        var slotUI = m_Slots[slotIndex];
        if (slotUI == null || !slotUI.gameObject.activeSelf)
            return;

        var slot = m_Container.GetSlot(slotIndex);
        var itemStack = (slot != null && !slot.IsEmpty) ? slot.ItemStack : null;
        slotUI.SetData(itemStack);
    }

    /// <summary>
    /// 计算宝箱应该显示的格子数（必须是6的倍数）
    /// </summary>
    private int CalculateSlotCount()
    {
        if (m_Container == null)
            return SLOTS_PER_ROW;

        // 找到最后一个有物品的格子
        int maxSlotIndex = -1;
        for (int i = 0; i < 50; i++)
        {
            var slot = m_Container.GetSlot(i);
            if (slot != null && !slot.IsEmpty)
                maxSlotIndex = i;
        }

        // 如果没有物品，至少显示1行（6个格子）
        if (maxSlotIndex < 0)
            return SLOTS_PER_ROW;

        // 计算需要多少行来容纳所有物品
        int requiredCount = maxSlotIndex + 1;
        // 向上对齐到6的倍数
        int rows = (requiredCount + SLOTS_PER_ROW - 1) / SLOTS_PER_ROW;
        return rows * SLOTS_PER_ROW;
    }

    /// <summary>
    /// 输出宝箱当前所有数据
    /// </summary>
    private void OutputTreasureBoxData()
    {
        if (m_Container == null)
            return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【宝箱内容】");
        int totalItems = 0;
        for (int i = 0; i < 50; i++)
        {
            var slot = m_Container.GetSlot(i);
            if (slot != null && !slot.IsEmpty && slot.ItemStack?.Item != null)
            {
                sb.AppendLine($"  格子{i}: {slot.ItemStack.Item.Name} x{slot.Count}");
                totalItems += slot.Count;
            }
        }
        sb.AppendLine($"物品总数: {totalItems}");
        DebugEx.LogModule("TreasureBoxUI", sb.ToString());
    }

    #endregion

    #region 按钮事件

    private void OnClickTakeAll()
    {
        // 动效播放中不允许操作
        if (m_IsAnimating)
            return;

        if (m_Container == null)
            return;

        int taken = m_Container.TakeAll();
        // TakeAll 触发 OnSlotChanged 事件，会自动调用 RefreshSlots
        DebugEx.Log("TreasureBoxUI", $"全部拿走: {taken} 件物品放入背包");

        // 全部拿走后自动关闭
        if (m_Container.IsEmpty())
            GF.UI.CloseUIForm(this.UIForm);
    }

    #endregion

    #region 上下文菜单

    public void ShowItemContextMenu(ItemStack itemStack, int slotIndex, RectTransform slotRect)
    {
        if (itemStack == null || itemStack.IsEmpty)
            return;

        if (m_CachedContextMenu == null)
        {
            if (varItemContextMenu == null)
            {
                DebugEx.Error("TreasureBoxUI", "varItemContextMenu 未设置");
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                DebugEx.Error("TreasureBoxUI", "未找到 Canvas");
                return;
            }

            var menuGO = Instantiate(varItemContextMenu, canvas.transform);
            m_CachedContextMenu = menuGO.GetComponent<ItemContextMenu>();

            if (m_CachedContextMenu == null)
            {
                DebugEx.Error("TreasureBoxUI", "菜单预制体中没有 ItemContextMenu 组件");
                Destroy(menuGO);
                return;
            }
        }

        m_CachedContextMenu.ShowContextMenu(itemStack, slotIndex, Vector2.zero, slotRect);
    }

    private void CheckContextMenuClickOutside()
    {
        if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            CheckContextMenuClickOutsideDelayedAsync().Forget();
        }
    }

    private async UniTask CheckContextMenuClickOutsideDelayedAsync()
    {
        await UniTask.Yield();

        if (m_CachedContextMenu == null || !m_CachedContextMenu.gameObject.activeSelf)
            return;

        var menuRect = m_CachedContextMenu.GetComponent<RectTransform>();
        if (menuRect == null)
            return;

        var parentCanvas = GetComponentInParent<Canvas>();
        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition, cam))
        {
            m_CachedContextMenu.HideContextMenu();
        }
    }

    #endregion

    #region 玩家输入锁定

    /// <summary>
    /// 设置玩家输入启用/禁用（WASD移动 + F交互）
    /// </summary>
    private void SetPlayerInputEnabled(bool enabled)
    {
        var input = PlayerInputManager.Instance;
        if (input != null)
            input.SetEnable(enabled);

        DebugEx.Log("TreasureBoxUI", $"玩家输入已{(enabled ? "启用" : "禁用")}");
    }

    #endregion

    #region 格子管理

    /// <summary>
    /// 重置所有格子显示（不销毁 GameObject，下次 OnOpen 复用）
    /// </summary>
    private void ResetSlots()
    {
        foreach (var slot in m_Slots)
        {
            if (slot != null)
            {
                slot.ClearItemQuantitySubscription();
                slot.SetSlotContainer(null);  // 清空容器引用
                slot.SetData(null);
                slot.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}
