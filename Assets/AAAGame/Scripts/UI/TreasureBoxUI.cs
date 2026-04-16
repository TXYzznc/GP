using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class TreasureBoxUI : UIFormBase
{
    #region 字段

    private readonly List<InventorySlotUI> m_Slots = new();
    private TreasureBoxSlotContainerImpl m_Container;
    private ItemContextMenu m_CachedContextMenu;

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

        var data = Params?.Get("TreasureBoxData") as TreasureBoxUIData;
        if (data == null)
        {
            DebugEx.Warning("TreasureBoxUI", "未传入 TreasureBoxUIData，使用空宝箱");
            data = new TreasureBoxUIData(new List<ItemStack>());
        }

        // 设置标题
        if (varTitleText != null)
            varTitleText.text = data.Title;

        // 初始化容器数据
        m_Container.SetItems(data.Items);

        BuildSlots(data.Items.Count);
        RefreshSlots();
        LockPlayerMovement(true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        ClearSlots();
        LockPlayerMovement(false);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        CheckContextMenuClickOutside();
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
    /// 根据物品数量动态创建格子
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

        // 显示/隐藏格子
        for (int i = 0; i < m_Slots.Count; i++)
            m_Slots[i].gameObject.SetActive(i < count);

        DebugEx.Log("TreasureBoxUI", $"格子构建完成: 共 {m_Slots.Count} 个，显示 {count} 个");
    }

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

    #region 按钮事件

    private void OnClickTakeAll()
    {
        int taken = m_Container.TakeAll();
        RefreshSlots();
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

    #region 玩家锁定

    private void LockPlayerMovement(bool locked)
    {
        DebugEx.Log("TreasureBoxUI", $"玩家移动已{(locked ? "锁定" : "解锁")}");
    }

    #endregion
}
