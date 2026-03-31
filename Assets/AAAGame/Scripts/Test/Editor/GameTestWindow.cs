#if UNITY_EDITOR
using UnityEditor;
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
    private CombatTestController m_CombatTestController;
    private EnemyTestController m_EnemyTestController;
    private ProjectileTestController m_ProjectileTestController;

    // 游戏状态
    private GameTestManager m_GameTestManager;

    // 日志相关
    private bool m_ShowLogs = true;
    private bool m_ShowInfo = true;
    private bool m_ShowWarning = true;
    private bool m_ShowError = true;

    #endregion

    #region EditorWindow 生命周期

    [MenuItem("工具/Clash of Gods/Test Manager")]
    public static void ShowWindow()
    {
        GetWindow<GameTestWindow>("测试管理");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🎮 游戏测试管理窗口", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        // 背包系统测试
        DrawInventoryTestSection();
        EditorGUILayout.Space(15);

        // 战斗系统测试
        DrawCombatTestSection();
        EditorGUILayout.Space(15);

        // 敌人系统测试
        DrawEnemyTestSection();
        EditorGUILayout.Space(15);

        // 投射物系统测试
        DrawProjectileTestSection();
        EditorGUILayout.Space(15);

        // 游戏状态测试
        DrawGameStateTestSection();
        EditorGUILayout.Space(15);

        // 日志系统
        DrawLogsSection();

        EditorGUILayout.EndScrollView();
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
        m_ShowInfo = EditorGUILayout.Toggle("Info", m_ShowInfo, GUILayout.Width(60));
        m_ShowWarning = EditorGUILayout.Toggle("Warning", m_ShowWarning, GUILayout.Width(80));
        m_ShowError = EditorGUILayout.Toggle("Error", m_ShowError, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // 日志统计
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"总日志: {logBuffer.Logs.Count}");
        EditorGUILayout.LabelField($"Info: {logBuffer.GetLogCountByType(LogType.Log)}");
        EditorGUILayout.LabelField($"Warning: {logBuffer.GetLogCountByType(LogType.Warning)}");
        EditorGUILayout.LabelField($"Error: {logBuffer.GetLogCountByType(LogType.Error)}");
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

    #endregion
}

#endif
