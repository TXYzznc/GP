using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class InventoryUI : UIFormBase
{
    #region 字段

    /// <summary>装备栏格子UI列表</summary>
    private readonly List<object> m_EquipSlotUIList = new();

    /// <summary>背包栏格子UI列表</summary>
    private readonly List<object> m_InventorySlotUIList = new();

    /// <summary>快捷栏格子UI列表</summary>
    private readonly List<object> m_FastSlotUIList = new();

    /// <summary>每行格子数</summary>
    private const int COLUMNS_PER_ROW = 4;

    /// <summary>背包管理器缓存</summary>
    private InventoryManager m_InventoryManager;

    #endregion

    #region Unity 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        DebugEx.Log("InventoryUI", "背包UI初始化开始");

        // 配置 ScrollRect
        ConfigureScrollRect();

        // 配置 GridLayoutGroup
        ConfigureGridLayout();

        // 初始化格子UI列表
        InitializeSlotUIList();

        // 绑定按钮事件
        BindButtonEvents();

        DebugEx.Success("InventoryUI", "背包UI初始化完成");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        DebugEx.Log("InventoryUI", "打开背包UI");

        // 订阅背包事件
        SubscribeInventoryEvents();

        // 刷新背包显示
        RefreshInventory();

        // 锁定玩家移动
        LockPlayerMovement(true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        // 取消订阅背包事件
        UnsubscribeInventoryEvents();

        // 解锁玩家移动
        LockPlayerMovement(false);

        DebugEx.Log("InventoryUI", "关闭背包UI");
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 配置 ScrollRect
    /// </summary>
    private void ConfigureScrollRect()
    {
        if (varEquipScrollView == null || varInventoryScrollView == null)
        {
            DebugEx.Warning("InventoryUI", "ScrollRect未配置");
            return;
        }

        // 配置装备栏
        varEquipScrollView.horizontal = true;
        varEquipScrollView.vertical = false;

        // 配置背包栏
        varInventoryScrollView.horizontal = false;
        varInventoryScrollView.vertical = true;

        DebugEx.Log("InventoryUI", "ScrollRect已配置");
    }

    /// <summary>
    /// 配置 GridLayoutGroup
    /// </summary>
    private void ConfigureGridLayout()
    {
        if (varEquipContent == null || varInventoryContent == null || varFastContent == null)
        {
            DebugEx.Warning("InventoryUI", "GridLayoutGroup未配置");
            return;
        }

        // 配置装备栏 - 横向排列
        varEquipContent.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        varEquipContent.constraintCount = 1;

        // 配置背包栏 - 每行4个格子
        varInventoryContent.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        varInventoryContent.constraintCount = COLUMNS_PER_ROW;

        // 配置快捷栏 - 横向排列
        varFastContent.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        varFastContent.constraintCount = 1;

        DebugEx.Log("InventoryUI", "GridLayoutGroup已配置");
    }

    /// <summary>
    /// 初始化格子UI列表
    /// </summary>
    private void InitializeSlotUIList()
    {
        DebugEx.Log("InventoryUI", "初始化背包格子UI");

        m_EquipSlotUIList.Clear();
        m_InventorySlotUIList.Clear();
        m_FastSlotUIList.Clear();

        // 获取装备栏格子 - 使用 GetComponentsInChildren 获取所有组件
        if (varEquipContent != null)
        {
            var equipSlots = varEquipContent.GetComponentsInChildren(
                System.Type.GetType("InventorySlotUI"),
                true
            );
            foreach (var slotUI in equipSlots)
            {
                m_EquipSlotUIList.Add(slotUI);
                if (slotUI is MonoBehaviour mb)
                    mb.gameObject.SetActive(true);
            }
        }

        // 获取背包栏格子
        if (varInventoryContent != null)
        {
            var inventorySlots = varInventoryContent.GetComponentsInChildren(
                System.Type.GetType("InventorySlotUI"),
                true
            );
            foreach (var slotUI in inventorySlots)
            {
                m_InventorySlotUIList.Add(slotUI);
                if (slotUI is MonoBehaviour mb)
                    mb.gameObject.SetActive(true);
            }
        }

        // 获取快捷栏格子
        if (varFastContent != null)
        {
            var fastSlots = varFastContent.GetComponentsInChildren(
                System.Type.GetType("InventorySlotUI"),
                true
            );
            foreach (var slotUI in fastSlots)
            {
                m_FastSlotUIList.Add(slotUI);
                if (slotUI is MonoBehaviour mb)
                    mb.gameObject.SetActive(true);
            }
        }

        DebugEx.Success(
            "InventoryUI",
            $"格子UI初始化完成 - 装备栏:{m_EquipSlotUIList.Count} 背包栏:{m_InventorySlotUIList.Count} 快捷栏:{m_FastSlotUIList.Count}"
        );
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (varCloseBtn != null)
        {
            varCloseBtn.onClick.AddListener(OnCloseBtnClick);
        }
    }

    #endregion

    #region 事件订阅

    /// <summary>
    /// 订阅背包事件
    /// </summary>
    private void SubscribeInventoryEvents()
    {
        m_InventoryManager = InventoryManager.Instance;
        if (m_InventoryManager == null)
        {
            DebugEx.Warning("InventoryUI", "InventoryManager 实例为空");
            return;
        }

        // 直接订阅事件
        m_InventoryManager.OnInventoryChanged += OnInventoryChanged;

        DebugEx.Log("InventoryUI", "已订阅背包事件");
    }

    /// <summary>
    /// 取消订阅背包事件
    /// </summary>
    private void UnsubscribeInventoryEvents()
    {
        if (m_InventoryManager == null)
            return;

        // 取消订阅事件
        m_InventoryManager.OnInventoryChanged -= OnInventoryChanged;

        DebugEx.Log("InventoryUI", "已取消订阅背包事件");
    }

    #endregion

    #region 刷新显示

    /// <summary>
    /// 刷新背包显示
    /// </summary>
    private void RefreshInventory()
    {
        DebugEx.Log("InventoryUI", "刷新背包显示");

        if (m_InventoryManager == null)
        {
            DebugEx.Warning("InventoryUI", "背包管理器未初始化");
            return;
        }

        // 获取所有格子
        var allSlots = m_InventoryManager.GetAllSlots();
        if (allSlots == null || allSlots.Count == 0)
        {
            DebugEx.Warning("InventoryUI", "格子列表为空");
            return;
        }

        // 更新格子显示
        UpdateSlotDisplay(allSlots);

        DebugEx.Log("InventoryUI", $"背包刷新完成，共 {allSlots.Count} 个格子");
    }

    /// <summary>
    /// 更新格子显示
    /// </summary>
    private void UpdateSlotDisplay(List<InventorySlot> slots)
    {
        DebugEx.Log("InventoryUI", $"更新格子显示，共 {slots.Count} 个格子");

        for (int i = 0; i < m_InventorySlotUIList.Count; i++)
        {
            var slotUIObj = m_InventorySlotUIList[i];
            if (slotUIObj == null)
                continue;

            // 通过反射调用 SetData 和 Clear 方法
            var slotUIType = slotUIObj.GetType();

            if (i < slots.Count)
            {
                var setDataMethod = slotUIType.GetMethod(
                    "SetData",
                    new[] { typeof(InventorySlot) }
                );
                if (setDataMethod != null)
                {
                    setDataMethod.Invoke(slotUIObj, new object[] { slots[i] });
                }
                else
                {
                    DebugEx.Warning("InventoryUI", $"未找到 SetData 方法，格子索引: {i}");
                }
            }
            else
            {
                var clearMethod = slotUIType.GetMethod("Clear", System.Type.EmptyTypes);
                if (clearMethod != null)
                {
                    clearMethod.Invoke(slotUIObj, null);
                }
            }
        }

        DebugEx.Success("InventoryUI", "格子显示更新完成");
    }

    /// <summary>
    /// 更新负重显示
    /// </summary>
    private void UpdateWeightDisplay()
    {
        if (m_InventoryManager == null)
            return;

        int usedSlots = m_InventoryManager.UsedSlotCount;
        int maxSlots = m_InventoryManager.MaxSlotCount;

        DebugEx.Log("InventoryUI", $"容量: {usedSlots}/{maxSlots}");
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 背包变化事件
    /// </summary>
    private void OnInventoryChanged()
    {
        DebugEx.Log("InventoryUI", "背包内容已变化");
        RefreshInventory();
    }

    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseBtnClick()
    {
        DebugEx.Log("InventoryUI", "点击关闭按钮");
        OnClickClose();
    }

    #endregion

    #region 玩家移动锁定

    /// <summary>
    /// 锁定/解锁玩家移动
    /// </summary>
    private void LockPlayerMovement(bool locked)
    {
        // TODO: 调用玩家控制器的锁定方法
        // 这里需要根据项目的实际玩家控制器实现
        DebugEx.Log("InventoryUI", $"玩家移动已{(locked ? "锁定" : "解锁")}");
    }

    #endregion
}
