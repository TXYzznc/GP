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

    #endregion

    #region 动画参数

    private const float SLOT_ANIMATION_DURATION = 0.4f;  // 每个格子动画时长
    private const float SLOT_ANIMATION_DELAY = 0.08f;    // 格子间隔（产生瀑布效果）
    private const float SLOT_SCALE_START = 0.5f;         // 起始缩放
    private const float SLOT_ALPHA_START = 0f;           // 起始透明度

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        m_Container = GetComponent<TreasureBoxSlotContainerImpl>();
        if (m_Container == null)
            m_Container = gameObject.AddComponent<TreasureBoxSlotContainerImpl>();

        BindButtonEvents();
        DebugEx.Success("TreasureBoxUI", "宝箱UI初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 从参数获取宝箱容器引用
        m_Container = Params?.Get("TreasureBoxContainer") as TreasureBoxSlotContainerImpl;
        if (m_Container == null)
        {
            DebugEx.Error("TreasureBoxUI", "未传入 TreasureBoxContainer");
            return;
        }

        // 订阅容器变化事件（仓库模式）
        m_Container.OnSlotChanged += OnTreasureBoxChanged;

        // 设置标题
        var treasureBoxName = Params?.Get("TreasureBoxName") as string ?? "宝箱";
        if (varTitleText != null)
            varTitleText.text = treasureBoxName;

        // 计算需要显示的格子数（宝箱中有物品的格子数）
        int slotCount = 0;
        for (int i = 0; i < 50; i++)
        {
            var slot = m_Container.GetSlot(i);
            if (slot != null && !slot.IsEmpty)
                slotCount = i + 1;
        }

        BuildSlots(slotCount);
        RefreshSlots();
        SetPlayerInputEnabled(false);

        // 初始化动画状态并播放开箱动效
        PlayTreasureOpenSequenceAsync().Forget();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        // 取消订阅事件
        if (m_Container != null)
            m_Container.OnSlotChanged -= OnTreasureBoxChanged;

        // 清空格子显示（不销毁 GameObject，下次 OnOpen 复用）
        ResetSlots();
        SetPlayerInputEnabled(true);

        // 容器数据持久保存在 TreasureChestInteractable 中，下次打开时继续使用
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
    /// </summary>
    private async UniTask PlayTreasureOpenAnimationAsync()
    {
        m_IsAnimating = true;

        // 动效播放中禁用按钮交互
        if (varCloseBtn != null) varCloseBtn.interactable = false;
        if (varTakeAllBtn != null) varTakeAllBtn.interactable = false;

        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (!m_Slots[i].gameObject.activeSelf) continue;

            // 播放该格子的动画（延迟错开，产生瀑布效果）
            await UniTask.Delay((int)(SLOT_ANIMATION_DELAY * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());

            PlaySlotAnimationAsync(m_Slots[i]).Forget();
        }

        // 等待最后一个格子动画完成
        await UniTask.Delay((int)((m_Slots.Count - 1) * SLOT_ANIMATION_DELAY * 1000 + SLOT_ANIMATION_DURATION * 1000),
                            cancellationToken: this.GetCancellationTokenOnDestroy());

        m_IsAnimating = false;

        // 动效完成后恢复按钮交互
        if (varCloseBtn != null) varCloseBtn.interactable = true;
        if (varTakeAllBtn != null) varTakeAllBtn.interactable = true;

        DebugEx.Log("TreasureBoxUI", "开箱动效播放完成");
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

        // 复用已有格子
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

        // 只隐藏超出数量的格子
        for (int i = count; i < m_Slots.Count; i++)
            m_Slots[i].gameObject.SetActive(false);

        DebugEx.Log("TreasureBoxUI", $"格子构建完成: 共 {m_Slots.Count} 个，显示 {count} 个");
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
    /// 宝箱内容变化回调（仓库模式）
    /// </summary>
    private void OnTreasureBoxChanged() => RefreshSlots();

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
            var menuRect = m_CachedContextMenu.GetComponent<RectTransform>();
            if (menuRect != null && !RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition))
            {
                m_CachedContextMenu.HideContextMenu();
            }
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
                slot.SetData(null);
                slot.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}
