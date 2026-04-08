#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 游戏测试统一管理窗口
/// 集中管理所有模块的测试功能
/// </summary>
public class GameTestWindow : EditorWindow
{
    #region 常量

    private const float BUTTON_HEIGHT = 30f;
    private const float BUTTON_SPACING = 5f;

    #endregion

    #region 字段

    private Vector2 m_ScrollPosition;
    private Vector2 m_LogScrollPosition;
    private InventoryTester m_InventoryTester;
    private object m_WarehouseTester; // 使用 object 避免程序集问题
    private CombatTestController m_CombatTestController;
    private EnemyTestController m_EnemyTestController;
    private ProjectileTestController m_ProjectileTestController;
    private object m_OutlineTestController; // 使用 object 避免程序集问题

    // 描边测试相关
    private GameObject m_OutlineTestTarget;
    private Color m_CustomOutlineColor = Color.cyan;
    private float m_CustomOutlineSize = 20f;

    // 游戏状态
    private GameTestManager m_GameTestManager;

    // Buff 测试相关
    private GameObject m_SelectedBuffTarget;
    private int m_SelectedBuffIndex = 0;
    private int m_SelectedPresetIndex = 0;
    private List<BuffInfo> m_CachedBuffList = new List<BuffInfo>();
    private List<BuffPreset> m_CachedPresetList = new List<BuffPreset>();

    // 日志相关
    private bool m_ShowInfo = true;
    private bool m_ShowWarning = true;
    private bool m_ShowError = true;

    // UI 优化
    private bool m_ExpandInventorySection = true;
    private bool m_ExpandWarehouseSection = true;
    private bool m_ExpandCombatSection = true;

    // 页签系统
    private int m_SelectedTab = 0;
    private readonly string[] m_TabNames = { "🧪 Buff", "📦 物品", "⚔️ 战斗", "🎲 状态", "✨ 描边", "📋 日志" };

    #endregion

    #region EditorWindow 生命周期

    [MenuItem("工具/Clash of Gods/Test Manager")]
    public static void ShowWindow()
    {
        GetWindow<GameTestWindow>("测试管理");
    }

    private void OnGUI()
    {
        // 标题栏
        EditorGUILayout.LabelField("🎮 游戏测试管理窗口 v2.0", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("集中管理所有游戏系统的测试功能，支持 Buff、物品、战斗等多个模块", MessageType.Info);
        EditorGUILayout.Space();

        // 顶部状态栏
        EditorGUILayout.BeginHorizontal("box");
        string playModeText = EditorApplication.isPlaying ? "▶ 运行中" : "⏸ 停止";
        Color statusColor = EditorApplication.isPlaying ? new Color(0.3f, 0.7f, 0.3f) : Color.gray;
        GUI.color = statusColor;
        EditorGUILayout.LabelField($"游戏状态: {playModeText}", GUILayout.Width(150));
        GUI.color = Color.white;
        EditorGUILayout.LabelField($"场景: {EditorSceneManager.GetActiveScene().name}", GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // 页签按钮
        m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, m_TabNames, GUILayout.Height(30));
        EditorGUILayout.Space();

        // 内容区域
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        switch (m_SelectedTab)
        {
            case 0: // Buff 测试
                DrawBuffTestSection();
                break;

            case 1: // 物品系统（背包 + 仓库）
                m_ExpandInventorySection = EditorGUILayout.Foldout(m_ExpandInventorySection, "📦 背包系统", true);
                if (m_ExpandInventorySection)
                {
                    DrawInventoryTestSection();
                    EditorGUILayout.Space(15);
                }

                m_ExpandWarehouseSection = EditorGUILayout.Foldout(m_ExpandWarehouseSection, "🏪 仓库系统", true);
                if (m_ExpandWarehouseSection)
                {
                    DrawWarehouseTestSection();
                    EditorGUILayout.Space(15);
                }
                break;

            case 2: // 战斗系统（战斗 + 敌人 + 投射物）
                m_ExpandCombatSection = EditorGUILayout.Foldout(m_ExpandCombatSection, "⚔️ 战斗系统", true);
                if (m_ExpandCombatSection)
                {
                    DrawCombatTestSection();
                    EditorGUILayout.Space(15);
                }

                EditorGUILayout.LabelField("👹 敌人系统", EditorStyles.boldLabel);
                DrawEnemyTestSection();
                EditorGUILayout.Space(15);

                EditorGUILayout.LabelField("🔫 投射物系统", EditorStyles.boldLabel);
                DrawProjectileTestSection();
                break;

            case 3: // 游戏状态
                EditorGUILayout.LabelField("🎲 游戏状态", EditorStyles.boldLabel);
                DrawGameStateTestSection();
                break;

            case 4: // 描边测试
                DrawOutlineTestSection();
                break;

            case 5: // 日志系统
                EditorGUILayout.LabelField("📋 日志系统", EditorStyles.boldLabel);
                DrawLogsSection();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Buff 系统测试

    /// <summary>
    /// 绘制 Buff 测试部分
    /// </summary>
    private void DrawBuffTestSection()
    {
        EditorGUILayout.HelpBox("快速测试和应用 Buff 效果，验证 Buff 系统的功能", MessageType.Info);

        // 目标选择
        EditorGUILayout.LabelField("目标选择", EditorStyles.boldLabel);
        m_SelectedBuffTarget = EditorGUILayout.ObjectField("当前目标:", m_SelectedBuffTarget, typeof(GameObject), true) as GameObject;

        if (m_SelectedBuffTarget == null)
        {
            EditorGUILayout.HelpBox("请从场景中选择一个棋子作为测试目标", MessageType.Warning);
            return;
        }

        // 验证目标是否有 BuffManager
        var buffManager = m_SelectedBuffTarget.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            EditorGUILayout.HelpBox($"目标 '{m_SelectedBuffTarget.name}' 没有 BuffManager 组件", MessageType.Error);
            return;
        }

        EditorGUILayout.Space();

        // 快速施加 Buff
        EditorGUILayout.LabelField("快速施加 Buff", EditorStyles.boldLabel);

        // 缓存管理
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"已缓存 Buff: {m_CachedBuffList.Count}", GUILayout.Width(120));
        if (GUILayout.Button("🔄 刷新缓存", GUILayout.Height(BUTTON_HEIGHT)))
        {
            m_CachedBuffList.Clear();
            m_CachedBuffList.AddRange(BuffTestTool.Instance.GetAllAvailableBuffs());
            EditorUtility.DisplayDialog("刷新成功", $"已加载 {m_CachedBuffList.Count} 个 Buff", "确定");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // 更新缓存列表（如果为空）
        if (m_CachedBuffList.Count == 0)
        {
            m_CachedBuffList.AddRange(BuffTestTool.Instance.GetAllAvailableBuffs());
        }

        // Buff 选择下拉菜单
        string[] buffOptions = new string[m_CachedBuffList.Count];
        for (int i = 0; i < m_CachedBuffList.Count; i++)
        {
            buffOptions[i] = $"{m_CachedBuffList[i].Name} (ID={m_CachedBuffList[i].BuffId})";
        }

        m_SelectedBuffIndex = EditorGUILayout.Popup("选择 Buff:", m_SelectedBuffIndex, buffOptions);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用选中 Buff", GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (m_SelectedBuffIndex >= 0 && m_SelectedBuffIndex < m_CachedBuffList.Count)
            {
                int buffId = m_CachedBuffList[m_SelectedBuffIndex].BuffId;
                BuffTestTool.Instance.ApplyBuffToTarget(buffId, m_SelectedBuffTarget);
                EditorUtility.DisplayDialog("成功", $"已应用 Buff: {m_CachedBuffList[m_SelectedBuffIndex].Name}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 预设方案
        EditorGUILayout.LabelField("预设方案", EditorStyles.boldLabel);

        // 更新预设列表
        if (m_CachedPresetList.Count == 0)
        {
            m_CachedPresetList.AddRange(BuffPresetManager.Instance.GetAllPresets());
        }

        string[] presetOptions = new string[m_CachedPresetList.Count];
        for (int i = 0; i < m_CachedPresetList.Count; i++)
        {
            presetOptions[i] = m_CachedPresetList[i].Name;
        }

        m_SelectedPresetIndex = EditorGUILayout.Popup("选择预设:", m_SelectedPresetIndex, presetOptions);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用预设", GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (m_SelectedPresetIndex >= 0 && m_SelectedPresetIndex < m_CachedPresetList.Count)
            {
                string presetName = m_CachedPresetList[m_SelectedPresetIndex].Name;
                BuffPresetManager.Instance.ApplyPreset(presetName, m_SelectedBuffTarget);
                EditorUtility.DisplayDialog("成功", $"已应用预设: {presetName}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 清理操作
        EditorGUILayout.LabelField("清理", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("清空所有 Buff", GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有 Buff 吗？", "确定", "取消"))
            {
                BuffTestTool.Instance.ClearAllBuffs(m_SelectedBuffTarget);
                EditorUtility.DisplayDialog("成功", "已清空所有 Buff", "确定");
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 状态信息
        EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);

        var buffList = BuffTestTool.Instance.GetTargetBuffs(m_SelectedBuffTarget);
        var attrInfo = BuffEffectVerifier.Instance.GetTargetAttributes(m_SelectedBuffTarget);

        EditorGUILayout.LabelField($"当前 Buff 数: {buffList.Count}");
        EditorGUILayout.LabelField($"HP: {attrInfo.HP:F0}/{attrInfo.MaxHP:F0}");
        EditorGUILayout.LabelField($"MP: {attrInfo.MP:F0}/{attrInfo.MaxMP:F0}");
        EditorGUILayout.LabelField($"攻击力: {attrInfo.AtkDamage:F0}");

        if (buffList.Count > 0)
        {
            EditorGUILayout.LabelField("Buff 列表:", EditorStyles.boldLabel);
            foreach (var buff in buffList)
            {
                EditorGUILayout.LabelField($"  • {buff.BuffId} (堆叠={buff.StackCount})");
            }
        }

        EditorGUILayout.Space();

        // Buff 诊断
        EditorGUILayout.LabelField("🔍 Buff 效果诊断", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("详细分析每个 Buff 如何修改属性值，用于调试 Buff 不生效的问题", MessageType.Info);

        if (buffList.Count > 0)
        {
            if (GUILayout.Button("生成诊断报告", GUILayout.Height(BUTTON_HEIGHT)))
            {
                var report = BuffDiagnoser.Instance.GenerateReport(m_SelectedBuffTarget);
                var message = report.ToString();
                DebugEx.LogModule("BuffDiagnoser", message);
                EditorUtility.DisplayDialog("诊断报告", message, "关闭");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Buff 修改详情:", EditorStyles.boldLabel);

            // 显示每个StatModBuff的具体修改
            var attribute = m_SelectedBuffTarget.GetComponent<ChessAttribute>();
            var dtBuff = GF.DataTable.GetDataTable<BuffTable>();

            if (attribute != null)
            {
                foreach (var buff in buffList)
                {
                    if (buff is StatModBuff statModBuff)
                    {
                        var modDetails = statModBuff.GetModDetails();
                        var appliedValues = statModBuff.GetAppliedValues();
                        var buffName = dtBuff?.GetDataRow(buff.BuffId)?.Name ?? $"Buff_{buff.BuffId}";

                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField($"📌 {buffName} (ID={buff.BuffId})", EditorStyles.boldLabel);

                        for (int i = 0; i < modDetails.Count; i++)
                        {
                            var mod = modDetails[i];
                            var appliedValue = appliedValues[i];
                            var modStr = mod.IsPercent
                                ? $"{mod.Value * 100:F1}% (实际: {appliedValue:F1})"
                                : $"{appliedValue:F1}";

                            EditorGUILayout.LabelField($"  {mod.Type}: +{modStr}");
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("没有应用任何 Buff，无法生成诊断报告", MessageType.Warning);
        }
    }

    #endregion

    #region 背包系统测试

    private void DrawInventoryTestSection()
    {
        EditorGUILayout.LabelField("📦 背包系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("背包、物品、快捷栏测试功能", MessageType.Info);

        // 获取InventoryTester
        if (m_InventoryTester == null)
            m_InventoryTester = FindObjectOfType<InventoryTester>();

        if (m_InventoryTester == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 InventoryTester 组件，请先添加到场景", MessageType.Warning);
            return;
        }

        // 按钮布局：2列
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("打开背包", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.OpenInventoryUI();
        if (GUILayout.Button("关闭背包", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.CloseInventoryUI();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加物品", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestAddItems();
        if (GUILayout.Button("移除物品", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestRemoveItems();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("使用物品", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestUseItem();
        if (GUILayout.Button("可堆叠测试", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestStackableItems();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("背包满测试", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestFullInventory();
        if (GUILayout.Button("存档读档", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestSaveAndLoad();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("边界测试", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.TestEdgeCases();
        if (GUILayout.Button("打印状态", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.PrintInventoryStatus();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("清空背包", GUILayout.Height(BUTTON_HEIGHT)))
            m_InventoryTester.ClearAllItems();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 仓库系统测试

    private void DrawWarehouseTestSection()
    {
        EditorGUILayout.LabelField("🏪 仓库系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("仓库、物品存取、容量管理测试功能", MessageType.Info);

        // 获取 WarehouseTester（使用反射避免程序集问题）
        if (m_WarehouseTester == null)
        {
            var type = System.Type.GetType("WarehouseTester");
            if (type != null)
            {
                m_WarehouseTester = FindObjectOfType(type);
            }
        }

        if (m_WarehouseTester == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 WarehouseTester 组件，请先添加到场景", MessageType.Warning);
            return;
        }

        // 按钮布局：2列
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("打开仓库", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "OpenWarehouseUI");
        if (GUILayout.Button("关闭仓库", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "CloseWarehouseUI");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("初始化仓库", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "InitializeWarehouse");
        if (GUILayout.Button("存入物品", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "TestStoreItem");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("取出物品", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "TestRetrieveItem");
        if (GUILayout.Button("一键存入", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "TestStoreAll");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("扩展容量", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "TestExpandCapacity");
        if (GUILayout.Button("打印状态", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "PrintWarehouseStatus");
        EditorGUILayout.EndHorizontal();

        // 背包和仓库交互测试
        EditorGUILayout.LabelField("交互与管理", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("交互流程测试", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "TestBackpackWarehouseInteraction");
        if (GUILayout.Button("容量管理测试", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "TestWarehouseCapacityManagement");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("清空仓库", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_WarehouseTester, "ClearWarehouse");
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 战斗系统测试

    private void DrawCombatTestSection()
    {
        EditorGUILayout.LabelField("⚔️ 战斗系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("战斗配置、场地生成测试功能", MessageType.Info);

        // 获取CombatTestController
        if (m_CombatTestController == null)
            m_CombatTestController = FindObjectOfType<CombatTestController>();

        if (m_CombatTestController == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 CombatTestController 组件", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("测试配置加载", GUILayout.Height(BUTTON_HEIGHT)))
            m_CombatTestController.TestConfigLoading();
        if (GUILayout.Button("生成战斗场地", GUILayout.Height(BUTTON_HEIGHT)))
            m_CombatTestController.SpawnBattleArena();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 敌人系统测试

    private void DrawEnemyTestSection()
    {
        EditorGUILayout.LabelField("👹 敌人系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("敌人生成、AI测试功能", MessageType.Info);

        // 获取EnemyTestController
        if (m_EnemyTestController == null)
            m_EnemyTestController = FindObjectOfType<EnemyTestController>();

        if (m_EnemyTestController == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 EnemyTestController 组件", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成敌人", GUILayout.Height(BUTTON_HEIGHT)))
            m_EnemyTestController.SpawnTestEnemy();
        if (GUILayout.Button("批量生成", GUILayout.Height(BUTTON_HEIGHT)))
            m_EnemyTestController.SpawnEnemiesBatch();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清除所有敌人", GUILayout.Height(BUTTON_HEIGHT)))
            m_EnemyTestController.DestroyAllEnemies();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 投射物系统测试

    private void DrawProjectileTestSection()
    {
        EditorGUILayout.LabelField("🔫 投射物系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("投射物发射、轨迹测试功能", MessageType.Info);

        // 获取ProjectileTestController
        if (m_ProjectileTestController == null)
            m_ProjectileTestController = FindObjectOfType<ProjectileTestController>();

        if (m_ProjectileTestController == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 ProjectileTestController 组件", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("发射投射物", GUILayout.Height(BUTTON_HEIGHT)))
            m_ProjectileTestController.FireProjectile();
        if (GUILayout.Button("重置统计", GUILayout.Height(BUTTON_HEIGHT)))
            m_ProjectileTestController.ResetStats();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 描边系统测试

    private void DrawOutlineTestSection()
    {
        EditorGUILayout.LabelField("✨ 描边系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("测试 OutlineController 的描边效果（选中/友方/敌方/自定义）", MessageType.Info);

        // 获取 OutlineTestController（反射方式）
        if (m_OutlineTestController == null)
        {
            var type = System.Type.GetType("OutlineTestController");
            if (type != null)
            {
                m_OutlineTestController = FindObjectOfType(type);
            }
        }

        if (m_OutlineTestController == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 OutlineTestController 组件，请先添加到场景中的 GameTestManager 上", MessageType.Warning);
            if (GUILayout.Button("自动添加到 GameTestManager", GUILayout.Height(BUTTON_HEIGHT)))
            {
                var gtm = FindObjectOfType<GameTestManager>();
                if (gtm != null)
                {
                    var type = System.Type.GetType("OutlineTestController");
                    if (type != null)
                    {
                        m_OutlineTestController = gtm.gameObject.AddComponent(type);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "场景中未找到 GameTestManager", "确定");
                }
            }
            return;
        }

        // 测试目标
        EditorGUILayout.LabelField("测试目标", EditorStyles.boldLabel);
        m_OutlineTestTarget = EditorGUILayout.ObjectField("目标对象:", m_OutlineTestTarget, typeof(GameObject), true) as GameObject;

        // 通过反射设置 ManualTarget 字段
        if (m_OutlineTestTarget != null)
        {
            var field = m_OutlineTestController.GetType().GetField("ManualTarget");
            if (field != null)
            {
                field.SetValue(m_OutlineTestController, m_OutlineTestTarget);
            }
        }

        EditorGUILayout.Space();

        // 单体描边测试
        EditorGUILayout.LabelField("单体描边", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.85f, 0f); // 黄色
        if (GUILayout.Button("选中描边（黄）", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "TestSelectionOutline");
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("友方描边（绿）", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "TestAllyOutline");
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("敌方描边（红）", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "TestEnemyOutline");
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 自定义描边
        EditorGUILayout.LabelField("自定义描边", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        m_CustomOutlineColor = EditorGUILayout.ColorField("颜色:", m_CustomOutlineColor, GUILayout.Width(250));
        m_CustomOutlineSize = EditorGUILayout.Slider("宽度:", m_CustomOutlineSize, 1f, 50f);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("应用自定义描边", GUILayout.Height(BUTTON_HEIGHT)))
        {
            var method = m_OutlineTestController.GetType().GetMethod("TestCustomOutline");
            if (method != null)
            {
                method.Invoke(m_OutlineTestController, new object[] { m_CustomOutlineColor, m_CustomOutlineSize });
            }
        }

        EditorGUILayout.Space();

        // 批量描边测试
        EditorGUILayout.LabelField("批量描边", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("所有友方（绿）", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "TestAllAllyOutlines");
        if (GUILayout.Button("所有敌方（红）", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "TestAllEnemyOutlines");
        if (GUILayout.Button("按阵营全显示", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "TestAllCampOutlines");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 状态和清理
        EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);
        var getCountMethod = m_OutlineTestController.GetType().GetMethod("GetActiveCount");
        int activeCount = getCountMethod != null ? (int)getCountMethod.Invoke(m_OutlineTestController, null) : 0;
        EditorGUILayout.LabelField($"当前活跃描边数: {activeCount}");

        EditorGUILayout.Space();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("清除所有描边", GUILayout.Height(BUTTON_HEIGHT)))
            InvokeMethod(m_OutlineTestController, "ClearAll");
        GUI.backgroundColor = Color.white;
    }

    #endregion

    #region 游戏状态测试

    private void DrawGameStateTestSection()
    {
        EditorGUILayout.LabelField("🎲 游戏状态", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("游戏流程状态切换测试", MessageType.Info);

        // 获取GameTestManager
        if (m_GameTestManager == null)
            m_GameTestManager = FindObjectOfType<GameTestManager>();

        if (m_GameTestManager == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 GameTestManager 组件", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("切换到探索", GUILayout.Height(BUTTON_HEIGHT)))
        {
            var gm = GameStateManager.Instance;
            if (gm != null)
                gm.SwitchToExploration();
        }
        if (GUILayout.Button("切换到战斗准备", GUILayout.Height(BUTTON_HEIGHT)))
        {
            var gm = GameStateManager.Instance;
            if (gm != null)
            {
                var inGameState = gm.GetInGameState();
                if (inGameState != null && inGameState.CurrentSubState != InGameStateType.CombatPreparation)
                {
                    inGameState.SwitchToCombatPreparation();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 日志系统

    private void DrawLogsSection()
    {
        EditorGUILayout.LabelField("📋 日志系统", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("捕获和导出游戏日志", MessageType.Info);

        // 启动/停止日志捕获
        var logBuffer = TestLogBuffer.Instance;
        if (logBuffer == null)
        {
            EditorGUILayout.HelpBox("日志缓冲器未初始化", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = logBuffer.IsListening ? Color.green : Color.red;
        string buttonText = logBuffer.IsListening ? "停止捕获" : "启动捕获";
        if (GUILayout.Button(buttonText, GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (logBuffer.IsListening)
                logBuffer.StopListening();
            else
                logBuffer.StartListening();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // 日志过滤选项
        EditorGUILayout.BeginHorizontal();
        m_ShowInfo = EditorGUILayout.Toggle("Info", m_ShowInfo);
        GUILayout.Space(10);
        m_ShowWarning = EditorGUILayout.Toggle("Warning", m_ShowWarning);
        GUILayout.Space(10);
        m_ShowError = EditorGUILayout.Toggle("Error", m_ShowError);
        EditorGUILayout.EndHorizontal();

        // 日志统计（两行显示）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"总日志: {logBuffer.Logs.Count}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"Info: {logBuffer.GetLogCountByType(LogType.Log)}", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Warning: {logBuffer.GetLogCountByType(LogType.Warning)}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"Error: {logBuffer.GetLogCountByType(LogType.Error)}", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // 日志显示区域
        EditorGUILayout.LabelField("日志内容", EditorStyles.boldLabel);
        m_LogScrollPosition = EditorGUILayout.BeginScrollView(m_LogScrollPosition, EditorStyles.helpBox, GUILayout.Height(200));

        foreach (var log in logBuffer.Logs)
        {
            // 按类型筛选
            if (!ShouldShowLog(log.Type))
                continue;

            // 根据类型设置颜色
            GUI.color = GetLogColor(log.Type);
            EditorGUILayout.LabelField(log.ToString(), EditorStyles.wordWrappedLabel);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndScrollView();

        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("导出日志", GUILayout.Height(BUTTON_HEIGHT)))
        {
            string filePath = logBuffer.ExportLogsToFile();
            if (!string.IsNullOrEmpty(filePath))
            {
                EditorUtility.RevealInFinder(filePath);
            }
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("清空日志", GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有日志吗？", "确定", "取消"))
            {
                logBuffer.ClearLogs();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private bool ShouldShowLog(LogType type)
    {
        return (type == LogType.Log && m_ShowInfo) ||
               (type == LogType.Warning && m_ShowWarning) ||
               ((type == LogType.Error || type == LogType.Exception) && m_ShowError);
    }

    private Color GetLogColor(LogType type)
    {
        return type switch
        {
            LogType.Log => Color.white,
            LogType.Warning => new Color(1, 0.9f, 0),
            LogType.Error => Color.red,
            LogType.Exception => Color.red,
            _ => Color.white
        };
    }

    #endregion

    #region 工具方法

    private void OnHierarchyChange()
    {
        // 检测场景中的组件是否改变
        Repaint();
    }

    /// <summary>
    /// 通过反射调用对象的方法
    /// </summary>
    private void InvokeMethod(object target, string methodName)
    {
        if (target == null)
            return;

        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(target, null);
        }
    }

    #endregion
}

#endif
