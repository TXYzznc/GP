using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 仓库系统测试器
/// 提供仓库的各种测试功能
/// </summary>
public class WarehouseTester : MonoBehaviour
{
    #region 测试方法

    /// <summary>打开仓库UI</summary>
    public void OpenWarehouseUI()
    {
        string uiAssetName = GF.UI.GetUIFormAssetName(UIViews.WarehouseUI);
        if (string.IsNullOrEmpty(uiAssetName))
        {
            DebugEx.Error("WarehouseTester", "无法获取仓库UI资源名称");
            return;
        }

        var uiForm = GF.UI.GetUIForm(uiAssetName);
        if (uiForm != null)
        {
            DebugEx.Log("WarehouseTester", "仓库UI已打开");
            return;
        }

        GF.UI.OpenUIForm(UIViews.WarehouseUI);
        DebugEx.Success("WarehouseTester", "打开仓库UI");
    }

    /// <summary>关闭仓库UI</summary>
    public void CloseWarehouseUI()
    {
        string uiAssetName = GF.UI.GetUIFormAssetName(UIViews.WarehouseUI);
        if (string.IsNullOrEmpty(uiAssetName))
        {
            DebugEx.Error("WarehouseTester", "无法获取仓库UI资源名称");
            return;
        }

        var uiForm = GF.UI.GetUIForm(uiAssetName);
        if (uiForm == null)
        {
            DebugEx.Warning("WarehouseTester", "仓库UI未打开");
            return;
        }

        GF.UI.CloseUIForm(uiForm);
        DebugEx.Success("WarehouseTester", "关闭仓库UI");
    }

    /// <summary>初始化仓库</summary>
    public void InitializeWarehouse()
    {
        var warehouseManager = WarehouseManager.Instance;
        warehouseManager.Initialize(50);
        DebugEx.Success("WarehouseTester", "仓库初始化完成");
    }

    /// <summary>存入物品到仓库</summary>
    public void TestStoreItem()
    {
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        // 存入几个测试物品
        bool success1 = warehouseManager.StoreItem(1001, 5);
        bool success2 = warehouseManager.StoreItem(1002, 3);
        bool success3 = warehouseManager.StoreItem(1003, 1);

        if (success1 && success2 && success3)
            DebugEx.Success("WarehouseTester", "物品存入成功");
        else
            DebugEx.Warning("WarehouseTester", "部分物品存入失败");
    }

    /// <summary>从仓库取出物品</summary>
    public void TestRetrieveItem()
    {
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        bool success1 = warehouseManager.RetrieveItem(1001, 2);
        bool success2 = warehouseManager.RetrieveItem(1002, 1);

        if (success1 && success2)
            DebugEx.Success("WarehouseTester", "物品取出成功");
        else
            DebugEx.Warning("WarehouseTester", "部分物品取出失败");
    }

    /// <summary>一键存入所有背包物品</summary>
    public void TestStoreAll()
    {
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        bool success = warehouseManager.StoreAll();
        if (success)
            DebugEx.Success("WarehouseTester", "一键存入完成");
        else
            DebugEx.Warning("WarehouseTester", "一键存入失败");
    }

    /// <summary>扩展仓库容量</summary>
    public void TestExpandCapacity()
    {
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        int oldCapacity = warehouseManager.WarehouseCapacity;
        warehouseManager.ExpandCapacity(25);
        int newCapacity = warehouseManager.WarehouseCapacity;

        DebugEx.Success("WarehouseTester", $"仓库容量扩展: {oldCapacity} -> {newCapacity}");
    }

    /// <summary>打印仓库状态</summary>
    public void PrintWarehouseStatus()
    {
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        var allItems = warehouseManager.GetAllItems();
        DebugEx.Log("WarehouseTester", $"========== 仓库状态 ==========");
        DebugEx.Log("WarehouseTester", $"容量: {warehouseManager.UsedSlots}/{warehouseManager.WarehouseCapacity}");
        DebugEx.Log("WarehouseTester", $"物品数量: {allItems.Count}");

        foreach (var item in allItems)
        {
            DebugEx.Log("WarehouseTester", $"  - ID={item.ItemId}, 数量={item.Count}, 格子={item.SlotIndex}");
        }

        DebugEx.Log("WarehouseTester", $"===============================");
    }

    /// <summary>清空仓库</summary>
    public void ClearWarehouse()
    {
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        warehouseManager.Cleanup();
        warehouseManager.Initialize(50);

        DebugEx.Success("WarehouseTester", "仓库已清空");
    }

    /// <summary>测试背包和仓库的交互流程</summary>
    public void TestBackpackWarehouseInteraction()
    {
        DebugEx.Log("WarehouseTester", "========== 背包和仓库交互测试开始 ==========");

        var inventoryManager = InventoryManager.Instance;
        var warehouseManager = WarehouseManager.Instance;

        // 检查初始化
        if (inventoryManager == null || !inventoryManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "背包未初始化");
            return;
        }

        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        // 1. 向背包添加物品
        DebugEx.Log("WarehouseTester", "[步骤1] 向背包添加物品");
        inventoryManager.AddItem(1001, 10);
        inventoryManager.AddItem(1002, 5);
        inventoryManager.AddItem(1003, 3);
        DebugEx.Log("WarehouseTester", $"  背包已使用: {inventoryManager.UsedSlotCount}/{inventoryManager.MaxSlotCount}");

        // 2. 部分物品存入仓库
        DebugEx.Log("WarehouseTester", "[步骤2] 部分物品存入仓库");
        bool result1 = warehouseManager.StoreItem(1001, 5);
        bool result2 = warehouseManager.StoreItem(1002, 3);
        if (result1 && result2)
        {
            DebugEx.Success("WarehouseTester", "物品已存入仓库");
            DebugEx.Log("WarehouseTester", $"  仓库已使用: {warehouseManager.UsedSlots}/{warehouseManager.WarehouseCapacity}");
        }

        // 3. 从仓库取出物品
        DebugEx.Log("WarehouseTester", "[步骤3] 从仓库取出部分物品");
        bool result3 = warehouseManager.RetrieveItem(1001, 3);
        if (result3)
        {
            DebugEx.Success("WarehouseTester", "物品已取出到背包");
            DebugEx.Log("WarehouseTester", $"  背包已使用: {inventoryManager.UsedSlotCount}/{inventoryManager.MaxSlotCount}");
            DebugEx.Log("WarehouseTester", $"  仓库已使用: {warehouseManager.UsedSlots}/{warehouseManager.WarehouseCapacity}");
        }

        // 4. 一键存入背包所有物品
        DebugEx.Log("WarehouseTester", "[步骤4] 一键存入背包所有物品到仓库");
        bool result4 = warehouseManager.StoreAll();
        if (result4)
        {
            DebugEx.Success("WarehouseTester", "背包物品已全部存入仓库");
            DebugEx.Log("WarehouseTester", $"  背包已使用: {inventoryManager.UsedSlotCount}/{inventoryManager.MaxSlotCount}");
            DebugEx.Log("WarehouseTester", $"  仓库已使用: {warehouseManager.UsedSlots}/{warehouseManager.WarehouseCapacity}");
        }

        DebugEx.Log("WarehouseTester", "========== 交互测试完成 ==========");
    }

    /// <summary>测试仓库容量管理</summary>
    public void TestWarehouseCapacityManagement()
    {
        DebugEx.Log("WarehouseTester", "========== 仓库容量管理测试开始 ==========");

        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null || !warehouseManager.IsInitialized)
        {
            DebugEx.Error("WarehouseTester", "仓库未初始化");
            return;
        }

        // 测试容量限制
        DebugEx.Log("WarehouseTester", $"初始容量: {warehouseManager.WarehouseCapacity}");

        // 尝试填满仓库
        for (int i = 0; i < warehouseManager.WarehouseCapacity; i++)
        {
            if (!warehouseManager.StoreItem(1001 + (i % 5), 1))
            {
                DebugEx.Warning("WarehouseTester", $"第 {i} 个物品存入失败，仓库已满");
                break;
            }
        }

        DebugEx.Log("WarehouseTester", $"填充后使用: {warehouseManager.UsedSlots}/{warehouseManager.WarehouseCapacity}");

        // 测试扩展容量
        DebugEx.Log("WarehouseTester", "测试容量扩展...");
        warehouseManager.ExpandCapacity(25);
        DebugEx.Success("WarehouseTester", $"容量扩展后: {warehouseManager.WarehouseCapacity}");

        DebugEx.Log("WarehouseTester", "========== 容量管理测试完成 ==========");
    }

    #endregion
}
