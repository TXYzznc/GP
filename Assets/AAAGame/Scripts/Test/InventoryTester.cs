using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 背包功能测试器
/// 使用 Inspector 面板按钮触发各种测试场景
/// </summary>
public class InventoryTester : MonoBehaviour
{
    #region Inspector 参数

    [Header("测试配置")]
    [SerializeField]
    [Tooltip("每次添加的数量")]
    private int m_TestAddCount = 5;

    [SerializeField]
    [Tooltip("每次移除的数量")]
    private int m_TestRemoveCount = 3;

    [Header("运行时信息（只读）")]
    [Tooltip("可堆叠物品ID列表（从配置表读取）")]
    private List<int> m_StackableItemIds = new List<int>();

    [Tooltip("不可堆叠物品ID列表（从配置表读取）")]
    private List<int> m_NonStackableItemIds = new List<int>();

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        DebugEx.LogModule("InventoryTester", "背包测试器初始化");
        LoadItemIdsFromTable();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 从配置表加载物品ID列表
    /// </summary>
    private void LoadItemIdsFromTable()
    {
        DebugEx.LogModule("InventoryTester", "开始加载物品列表");

        if (ItemManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "ItemManager 创建失败，无法加载物品列表");
            return;
        }

        // 确保配置表已加载
        ItemManager.Instance.LoadItemTable();

        m_StackableItemIds.Clear();
        m_NonStackableItemIds.Clear();

        // 获取所有物品数据
        var table = GF.DataTable.GetDataTable<ItemTable>();
        if (table == null)
        {
            DebugEx.Error("InventoryTester", "ItemTable 配置表未加载");
            return;
        }

        var allRows = table.GetAllDataRows();
        if (allRows == null || allRows.Length == 0)
        {
            DebugEx.Error("InventoryTester", "ItemTable 配置表为空");
            return;
        }

        DebugEx.LogModule("InventoryTester", $"获取到 {allRows.Length} 条物品数据");

        foreach (var row in allRows)
        {
            var itemData = ItemManager.Instance.GetItemData(row.Id);
            if (itemData == null)
            {
                DebugEx.Warning("InventoryTester", $"物品ID {row.Id} 数据为空");
                continue;
            }

            // 根据是否可堆叠分类
            if (itemData.CanStack)
            {
                m_StackableItemIds.Add(row.Id);
            }
            else
            {
                m_NonStackableItemIds.Add(row.Id);
            }
        }

        DebugEx.Success(
            "InventoryTester",
            $"物品列表加载完成 - 可堆叠:{m_StackableItemIds.Count}, 不可堆叠:{m_NonStackableItemIds.Count}"
        );
    }

    #endregion

    #region UI 测试方法

    /// <summary>
    /// 打开背包UI
    /// </summary>
    public void OpenInventoryUI()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：打开背包UI ==========",
            DebugEx.Color.Cyan
        );

        GF.UI.OpenUIForm(UIViews.InventoryUI);

        DebugEx.Success("InventoryTester", "背包UI已打开");
    }

    /// <summary>
    /// 关闭背包UI
    /// </summary>
    public void CloseInventoryUI()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：关闭背包UI ==========",
            DebugEx.Color.Cyan
        );

        string uiAssetName = GF.UI.GetUIFormAssetName(UIViews.InventoryUI);
        if (string.IsNullOrEmpty(uiAssetName))
        {
            DebugEx.Error("InventoryTester", "无法获取背包UI资源名称");
            return;
        }

        var inventoryUI = GF.UI.GetUIForm(uiAssetName);
        if (inventoryUI != null)
        {
            GF.UI.CloseUIForm(inventoryUI);
            DebugEx.Success("InventoryTester", "背包UI已关闭");
        }
        else
        {
            DebugEx.Warning("InventoryTester", "背包UI未打开");
        }
    }

    #endregion

    #region 背包功能测试方法

    /// <summary>
    /// 测试添加物品
    /// </summary>
    public void TestAddItems()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：添加物品 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.ErrorModule("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        // 重新加载物品列表，确保配置表已加载
        LoadItemIdsFromTable();

        // 检查物品列表是否为空，如果为空，尝试再次加载
        if (m_StackableItemIds.Count == 0 && m_NonStackableItemIds.Count == 0)
        {
            DebugEx.WarningModule("InventoryTester", "物品列表为空，尝试再次加载配置表");
            // 手动加载配置表（ItemManager 会自动创建）
            ItemManager.Instance.LoadAllTables();
            // 再次加载物品列表
            LoadItemIdsFromTable();

            // 再次检查
            if (m_StackableItemIds.Count == 0 && m_NonStackableItemIds.Count == 0)
            {
                DebugEx.ErrorModule("InventoryTester", "物品列表为空，请检查配置表是否已加载");
                DebugEx.ErrorModule("InventoryTester", "可能的原因：");
                DebugEx.ErrorModule("InventoryTester", "1. ItemTable 配置表数据为空");
                DebugEx.ErrorModule("InventoryTester", "2. ItemManager.LoadItemTable() 加载失败");
                DebugEx.ErrorModule("InventoryTester", "3. 所有物品的 ItemData 都为 null");
                return;
            }
        }

        DebugEx.LogModule(
            "InventoryTester",
            $"可堆叠物品数量:{m_StackableItemIds.Count}, 不可堆叠物品数量:{m_NonStackableItemIds.Count}"
        );

        // 随机添加各种类型的物品
        var allItemIds = new List<int>();
        allItemIds.AddRange(m_StackableItemIds);
        allItemIds.AddRange(m_NonStackableItemIds);

        if (allItemIds.Count == 0)
        {
            DebugEx.Warning("InventoryTester", "没有可用物品来添加");
            return;
        }

        DebugEx.LogModule("InventoryTester", "随机添加物品...");

        // 随机添加 5-10 种物品
        int itemTypesToAdd = Random.Range(5, Mathf.Min(11, allItemIds.Count + 1));
        var selectedItems = new List<int>();

        // 随机选择不同的物品
        for (int i = 0; i < itemTypesToAdd; i++)
        {
            int randomIdx = Random.Range(0, allItemIds.Count);
            selectedItems.Add(allItemIds[randomIdx]);
        }

        // 添加选中的物品
        foreach (int itemId in selectedItems)
        {
            // 判断是否可堆叠，随机确定数量
            bool isStackable = m_StackableItemIds.Contains(itemId);
            int addCount = isStackable ? Random.Range(1, m_TestAddCount + 1) : 1;

            bool success = InventoryManager.Instance.AddItem(itemId, addCount);
            var itemData = ItemManager.Instance?.GetItemData(itemId);
            string itemName = itemData != null ? itemData.Name : $"ID:{itemId}";
            DebugEx.LogModule(
                "InventoryTester",
                $"添加物品 {itemName} x{addCount} - {(success ? "成功" : "失败")}"
            );
        }

        DebugEx.Success("InventoryTester", $"添加物品测试完成 (共 {selectedItems.Count} 种物品)");
    }

    /// <summary>
    /// 测试移除物品
    /// </summary>
    public void TestRemoveItems()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：移除物品 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        // 测试移除第一个可堆叠物品
        if (m_StackableItemIds.Count > 0)
        {
            int itemId = m_StackableItemIds[0];
            int beforeCount = InventoryManager.Instance.GetItemCount(itemId);
            bool success = InventoryManager.Instance.RemoveItem(itemId, m_TestRemoveCount);

            var itemData = ItemManager.Instance?.GetItemData(itemId);
            string itemName = itemData != null ? itemData.Name : $"ID:{itemId}";

            DebugEx.LogModule(
                "InventoryTester",
                $"移除物品 {itemName} x{m_TestRemoveCount} - {(success ? "成功" : "失败")} (之前:{beforeCount}, 之后:{InventoryManager.Instance.GetItemCount(itemId)})"
            );
        }

        DebugEx.Success("InventoryTester", "移除物品测试完成");
    }

    /// <summary>
    /// 测试使用物品
    /// </summary>
    public void TestUseItem()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：使用物品 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        // 查找第一个非空格子并使用
        var slots = InventoryManager.Instance.GetAllSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty)
            {
                var item = slots[i].ItemStack.Item;
                DebugEx.LogModule("InventoryTester", $"尝试使用格子 {i} 的物品: {item.Name}");

                bool success = InventoryManager.Instance.UseItem(i);
                DebugEx.LogModule(
                    "InventoryTester",
                    $"使用物品 - {(success ? "成功" : "失败")} (可使用:{item.CanUse})"
                );
                break;
            }
        }

        DebugEx.Success("InventoryTester", "使用物品测试完成");
    }

    /// <summary>
    /// 测试可堆叠物品
    /// </summary>
    public void TestStackableItems()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：可堆叠物品 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        if (m_StackableItemIds.Count == 0)
        {
            DebugEx.Warning("InventoryTester", "没有可堆叠物品");
            return;
        }

        int testItemId = m_StackableItemIds[0];
        var itemData = ItemManager.Instance?.GetItemData(testItemId);
        string itemName = itemData != null ? itemData.Name : $"ID:{testItemId}";

        // 多次添加同一物品，测试堆叠
        DebugEx.LogModule("InventoryTester", $"连续添加物品 {itemName}，测试堆叠功能");

        for (int i = 0; i < 5; i++)
        {
            bool success = InventoryManager.Instance.AddItem(testItemId, 10);
            int totalCount = InventoryManager.Instance.GetItemCount(testItemId);
            DebugEx.LogModule(
                "InventoryTester",
                $"第 {i + 1} 次添加 - {(success ? "成功" : "失败")}, 当前总数:{totalCount}"
            );
        }

        DebugEx.Success("InventoryTester", "可堆叠物品测试完成");
    }

    /// <summary>
    /// 测试背包满的情况
    /// </summary>
    public void TestFullInventory()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：背包满 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        if (m_NonStackableItemIds.Count == 0)
        {
            DebugEx.Warning("InventoryTester", "没有不可堆叠物品");
            return;
        }

        int testItemId = m_NonStackableItemIds[0];
        int maxSlots = InventoryManager.Instance.MaxSlotCount;
        int usedSlots = InventoryManager.Instance.UsedSlotCount;

        DebugEx.LogModule("InventoryTester", $"当前背包状态: {usedSlots}/{maxSlots} (已用/总数)");

        // 尝试填满背包
        DebugEx.LogModule("InventoryTester", "尝试填满背包...");
        int addCount = 0;
        for (int i = usedSlots; i < maxSlots + 5; i++)
        {
            bool success = InventoryManager.Instance.AddItem(testItemId, 1);
            if (success)
            {
                addCount++;
            }
            else
            {
                DebugEx.Warning("InventoryTester", $"背包已满，成功添加 {addCount} 个物品");
                break;
            }
        }

        DebugEx.Success("InventoryTester", "背包满测试完成");
    }

    /// <summary>
    /// 测试存档与读档
    /// </summary>
    public void TestSaveAndLoad()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：存档与读档 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        // 保存当前背包数据
        DebugEx.LogModule("InventoryTester", "保存背包数据...");
        var saveData = InventoryManager.Instance.SaveInventory();
        DebugEx.LogModule("InventoryTester", $"保存了 {saveData.Count} 个物品");

        // 添加一些新物品
        if (m_StackableItemIds.Count > 0)
        {
            DebugEx.LogModule("InventoryTester", "添加新物品以改变背包状态...");
            InventoryManager.Instance.AddItem(m_StackableItemIds[0], 99);
        }

        // 读取之前保存的数据
        DebugEx.LogModule("InventoryTester", "加载之前保存的背包数据...");
        InventoryManager.Instance.LoadInventory(saveData);

        DebugEx.Success("InventoryTester", "存档与读档测试完成");
    }

    /// <summary>
    /// 测试边界情况
    /// </summary>
    public void TestEdgeCases()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 测试：边界情况 ==========",
            DebugEx.Color.Cyan
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        // 测试1: 添加不存在的物品
        DebugEx.Log("InventoryTester", "测试添加不存在的物品 ID:99999");
        bool result1 = InventoryManager.Instance.AddItem(99999, 1);
        DebugEx.Log("InventoryTester", $"结果: {(result1 ? "成功" : "失败（预期）")}");

        // 测试2: 移除不存在的物品
        DebugEx.Log("InventoryTester", "测试移除不存在的物品 ID:99999");
        bool result2 = InventoryManager.Instance.RemoveItem(99999, 1);
        DebugEx.Log("InventoryTester", $"结果: {(result2 ? "成功" : "失败（预期）")}");

        // 测试3: 移除数量超过拥有数量
        if (m_StackableItemIds.Count > 0)
        {
            int itemId = m_StackableItemIds[0];
            int currentCount = InventoryManager.Instance.GetItemCount(itemId);
            DebugEx.Log(
                "InventoryTester",
                $"测试移除超量物品 ID:{itemId}, 拥有:{currentCount}, 尝试移除:{currentCount + 100}"
            );
            bool result3 = InventoryManager.Instance.RemoveItem(itemId, currentCount + 100);
            DebugEx.Log("InventoryTester", $"结果: {(result3 ? "成功" : "失败（预期）")}");
        }

        // 测试4: 使用空格子
        DebugEx.Log("InventoryTester", "测试使用空格子");
        var slots = InventoryManager.Instance.GetAllSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                bool result4 = InventoryManager.Instance.UseItem(i);
                DebugEx.Log(
                    "InventoryTester",
                    $"使用空格子 {i} - {(result4 ? "成功" : "失败（预期）")}"
                );
                break;
            }
        }

        // 测试5: 使用越界索引
        DebugEx.Log("InventoryTester", "测试使用越界索引 -1 和 999");
        bool result5a = InventoryManager.Instance.UseItem(-1);
        bool result5b = InventoryManager.Instance.UseItem(999);
        DebugEx.Log(
            "InventoryTester",
            $"结果: {(result5a ? "成功" : "失败（预期）")}, {(result5b ? "成功" : "失败（预期）")}"
        );

        DebugEx.Success("InventoryTester", "边界情况测试完成");
    }

    /// <summary>
    /// 打印背包状态
    /// </summary>
    public void PrintInventoryStatus()
    {
        DebugEx.LogModule(
            "InventoryTester",
            "========== 背包状态 ==========",
            DebugEx.Color.Yellow
        );

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        var slots = InventoryManager.Instance.GetAllSlots();
        int usedCount = InventoryManager.Instance.UsedSlotCount;
        int maxCount = InventoryManager.Instance.MaxSlotCount;

        DebugEx.LogModule("InventoryTester", $"背包容量: {usedCount}/{maxCount}");
        DebugEx.LogModule("InventoryTester", "物品列表:");

        // 统计物品
        Dictionary<int, int> itemCounts = new Dictionary<int, int>();
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty)
            {
                int itemId = slot.ItemId;
                if (!itemCounts.ContainsKey(itemId))
                {
                    itemCounts[itemId] = 0;
                }
                itemCounts[itemId] += slot.Count;
            }
        }

        // 打印统计
        foreach (var kvp in itemCounts)
        {
            var itemData = ItemManager.Instance?.GetItemData(kvp.Key);
            string itemName = itemData != null ? itemData.Name : "未知物品";
            DebugEx.LogModule("InventoryTester", $"  - {itemName} (ID:{kvp.Key}) x{kvp.Value}");
        }

        DebugEx.Success("InventoryTester", "背包状态打印完成");
    }

    /// <summary>
    /// 清空所有物品
    /// </summary>
    public void ClearAllItems()
    {
        DebugEx.LogModule("InventoryTester", "========== 清空背包 ==========", DebugEx.Color.Red);

        if (InventoryManager.Instance == null)
        {
            DebugEx.Error("InventoryTester", "InventoryManager 未初始化");
            return;
        }

        // 通过加载空数据来清空背包
        InventoryManager.Instance.LoadInventory(new List<InventoryItemSaveData>());

        DebugEx.Success("InventoryTester", "背包已清空");
    }

    #endregion

#if UNITY_EDITOR

    #region 自定义 Inspector

    [CustomEditor(typeof(InventoryTester), true)]
    public class InventoryTesterInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "所有测试功能已移至 Tools > Clash of Gods > Test Manager 窗口\n" +
                "在该窗口中管理所有测试功能",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("打开 Test Manager", GUILayout.Height(35)))
            {
                EditorWindow.GetWindow(System.Type.GetType("GameTestWindow,Assembly-CSharp-Editor"));
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    #endregion

#endif
}
