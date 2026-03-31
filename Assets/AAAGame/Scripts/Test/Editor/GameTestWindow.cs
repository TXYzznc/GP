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
    private InventoryTester m_InventoryTester;
    private CombatTestController m_CombatTestController;
    private EnemyTestController m_EnemyTestController;
    private ProjectileTestController m_ProjectileTestController;

    // 游戏状态
    private GameTestManager m_GameTestManager;

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

    #region 工具方法

    private void OnHierarchyChange()
    {
        // 检测场景中的组件是否改变
        Repaint();
    }

    #endregion
}

#endif
