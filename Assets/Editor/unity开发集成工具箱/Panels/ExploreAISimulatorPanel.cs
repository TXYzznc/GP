using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人探索AI行为模拟器面板
/// 支持：玩家配置、敌人配置、延迟生成
/// </summary>
[ToolHubItem("探索工具/敌人AI模拟器", "配置玩家和敌人，测试AI行为（视野检测、警觉、追击等）", 51)]
public class ExploreAISimulatorPanel : IToolHubPanel
{
    #region 配置字段

    private int m_SummonerId = 1001;
    private Vector3 m_PlayerPos = new Vector3(0, 0, -3f);

    private List<EnemySpawnConfig> m_EnemyConfigs = new()
    {
        new EnemySpawnConfig { EnemyEntityId = 1, Count = 1 }
    };

    private Vector3 m_EnemySpawnAreaCenter = new Vector3(0, 0, 5f);
    private float m_EnemySpawnAreaRadius = 10f;

    #endregion

    #region 运行时状态

    private GameObject m_PlayerObject;
    private List<EnemyEntity> m_SpawnedEnemies = new();
    private bool m_IsSpawning;
    private EditorWindow m_OwnerWindow;
    private double m_LastRepaintTime;
    private const double k_RepaintInterval = 0.05;
    private Vector2 m_ScrollPos;
    private List<bool> m_EnemyConfigFoldouts = new();
    private bool m_IsTestRunning = false;

    #endregion

    #region 敌人生成配置

    private class EnemySpawnConfig
    {
        public int EnemyEntityId = 1;
        public int Count = 1;
    }

    #endregion

    #region IToolHubPanel 实现

    public void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    public void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    public void OnDestroy()
    {
        m_PlayerObject = null;
        m_SpawnedEnemies.Clear();
    }

    public string GetHelpText()
    {
        return "敌人AI模拟器：用于独立测试敌人的探索AI行为\n" +
               "1. 在ExploreAITestScene中放置 ExploreAITestBootstrapper\n" +
               "2. 配置玩家和敌人参数\n" +
               "3. 点击「启动测试」后观察AI行为";
    }

    public void OnGUI()
    {
        if (m_OwnerWindow == null)
            m_OwnerWindow = EditorWindow.focusedWindow;

        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        DrawEnvironmentStatus();
        EditorGUILayout.Space(4);

        // 检查是否准备好：在PlayMode + DataTable已加载
        bool ready = Application.isPlaying && IsDataTableLoaded();

        EditorGUI.BeginDisabledGroup(!ready);
        {
            DrawPlayerConfig();
            EditorGUILayout.Space(4);
            DrawEnemyConfig();
            EditorGUILayout.Space(4);
            DrawTestControls();
            EditorGUILayout.Space(4);
            DrawAIMonitoring();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region UI 绘制

    private void DrawEnvironmentStatus()
    {
        EditorGUILayout.LabelField("环境状态", EditorStyles.boldLabel);

        bool isPlaying = Application.isPlaying;
        bool isTestMode = isPlaying && ExploreAITestBootstrapper.IsExploreAITestMode;
        bool dtLoaded = isPlaying && IsDataTableLoaded();

        EditorGUILayout.BeginHorizontal();
        DrawStatusLabel("PlayMode", isPlaying);
        DrawStatusLabel("TestMode", isTestMode);
        DrawStatusLabel("DataLoaded", dtLoaded);
        EditorGUILayout.EndHorizontal();

        if (!isPlaying)
        {
            EditorGUILayout.HelpBox(
                "使用方式：\n" +
                "1. 在ExploreAITestScene中创建空GameObject，挂载 ExploreAITestBootstrapper\n" +
                "2. 从该ExploreAITestScene进入 PlayMode\n" +
                "3. 配置玩家和敌人参数后点击「启动测试\"",
                MessageType.Info);
        }
    }

    private void DrawStatusLabel(string label, bool ok)
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = ok ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
        string icon = ok ? "[OK]" : "[X]";
        EditorGUILayout.LabelField($"{icon} {label}", style, GUILayout.Width(120));
    }

    private void DrawPlayerConfig()
    {
        EditorGUILayout.LabelField("玩家配置", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        m_SummonerId = EditorGUILayout.IntField("玩家ID", m_SummonerId);
        m_PlayerPos = EditorGUILayout.Vector3Field("生成位置", m_PlayerPos);

        if (Application.isPlaying && IsDataTableLoaded())
        {
            var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
            if (summonerTable != null && summonerTable.GetDataRow(m_SummonerId) is var config && config != null)
            {
                EditorGUILayout.LabelField($"  -> {config.Name} (职业: {config.ClassType})");
            }
            else
            {
                EditorGUILayout.LabelField("  -> 未找到该ID的玩家配置", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEnemyConfig()
    {
        EditorGUILayout.LabelField("敌人配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("支持生成多个敌人，每个敌人可配置ID和数量", MessageType.Info);

        // 生成位置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("生成范围", EditorStyles.miniBoldLabel);
        m_EnemySpawnAreaCenter = EditorGUILayout.Vector3Field("中心位置", m_EnemySpawnAreaCenter);
        m_EnemySpawnAreaRadius = EditorGUILayout.FloatField("范围半径", m_EnemySpawnAreaRadius);
        EditorGUILayout.EndVertical();

        // 敌人列表
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("敌人列表", EditorStyles.miniBoldLabel);

        for (int i = 0; i < m_EnemyConfigs.Count; i++)
        {
            bool foldout = i < m_EnemyConfigFoldouts.Count ? m_EnemyConfigFoldouts[i] : false;

            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, $"敌人 {i + 1}");
            if (i < m_EnemyConfigFoldouts.Count)
                m_EnemyConfigFoldouts[i] = foldout;

            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                m_EnemyConfigs.RemoveAt(i);
                if (i < m_EnemyConfigFoldouts.Count)
                    m_EnemyConfigFoldouts.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUI.indentLevel++;
                m_EnemyConfigs[i].EnemyEntityId = EditorGUILayout.IntField("敌人ID", m_EnemyConfigs[i].EnemyEntityId);
                m_EnemyConfigs[i].Count = EditorGUILayout.IntField("数量", m_EnemyConfigs[i].Count);

                if (Application.isPlaying && IsDataTableLoaded())
                {
                    var enemyTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
                    if (enemyTable != null && enemyTable.GetDataRow(m_EnemyConfigs[i].EnemyEntityId) is var config && config != null)
                    {
                        EditorGUILayout.LabelField($"  -> {config.Name} (类型: {config.EnemyType})");
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        // 添加敌人按钮
        if (GUILayout.Button("+ 添加敌人", GUILayout.Height(25)))
        {
            m_EnemyConfigs.Add(new EnemySpawnConfig { EnemyEntityId = 1, Count = 1 });
            m_EnemyConfigFoldouts.Add(false);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTestControls()
    {
        EditorGUILayout.LabelField("测试控制", EditorStyles.boldLabel);

        bool hasObjects = m_PlayerObject != null || m_SpawnedEnemies.Count > 0;

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(m_IsSpawning || m_IsTestRunning);
        if (GUILayout.Button(m_IsTestRunning ? "测试运行中..." : "启动测试", GUILayout.Height(35)))
        {
            if (!m_IsTestRunning)
            {
                StartTestAsync().Forget();
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!hasObjects);
        if (GUILayout.Button("清除", GUILayout.Height(35)))
        {
            ClearAll();
            m_IsTestRunning = false;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // 显示当前状态
        EditorGUILayout.Space(2);
        if (m_IsTestRunning)
        {
            EditorGUILayout.LabelField("✓ 测试运行中", EditorStyles.helpBox);
            if (m_PlayerObject != null)
                EditorGUILayout.LabelField($"  玩家: 已生成");
            EditorGUILayout.LabelField($"  敌人: {m_SpawnedEnemies.Count} 个");
        }
        else if (hasObjects)
        {
            EditorGUILayout.LabelField("(点击「清除」重置)");
        }
    }

    private void DrawAIMonitoring()
    {
        if (m_SpawnedEnemies.Count == 0)
            return;

        EditorGUILayout.LabelField("AI监控", EditorStyles.boldLabel);

        foreach (var enemy in m_SpawnedEnemies)
        {
            if (enemy == null)
                continue;

            var ai = enemy.AI;
            var visionDetector = enemy.VisionDetector;

            float distance = m_PlayerObject != null
                ? Vector3.Distance(enemy.transform.position, m_PlayerObject.transform.position)
                : -1f;

            float alertLevel = visionDetector?.AlertLevel ?? 0f;

            string status = $"[{enemy.Config?.Name}] " +
                           $"状态:{ai?.CurrentState} | " +
                           $"警觉:{alertLevel:F2} | " +
                           $"距离:{(distance >= 0 ? $"{distance:F1}m" : "N/A")}";

            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }
    }

    #endregion

    #region 核心逻辑

    private async UniTaskVoid StartTestAsync()
    {
        m_IsSpawning = true;
        m_IsTestRunning = true;

        try
        {
            ClearAll();

            // 生成玩家
            DebugEx.Log("ExploreAISimulatorPanel", "生成测试玩家...");
            m_PlayerObject = await SpawnPlayerAsync();

            if (m_PlayerObject == null)
            {
                DebugEx.Error("ExploreAISimulatorPanel", "玩家生成失败");
                m_IsTestRunning = false;
                return;
            }

            DebugEx.Success("ExploreAISimulatorPanel", $"✓ 玩家生成成功");

            // 生成敌人
            foreach (var config in m_EnemyConfigs)
            {
                for (int i = 0; i < config.Count; i++)
                {
                    Vector3 spawnPos = m_EnemySpawnAreaCenter +
                                      Random.insideUnitSphere * m_EnemySpawnAreaRadius;
                    spawnPos.y = 0;

                    var enemy = await SpawnEnemyAsync(config.EnemyEntityId, spawnPos);
                    if (enemy != null)
                    {
                        m_SpawnedEnemies.Add(enemy);
                    }
                }
            }

            DebugEx.Success("ExploreAISimulatorPanel",
                $"✓ 测试启动完成：玩家1个，敌人{m_SpawnedEnemies.Count}个");
        }
        catch (System.Exception ex)
        {
            DebugEx.Error("ExploreAISimulatorPanel", $"启动失败：{ex.Message}\n{ex.StackTrace}");
            m_IsTestRunning = false;
        }
        finally
        {
            m_IsSpawning = false;
        }
    }

    private async UniTask<GameObject> SpawnPlayerAsync()
    {
        try
        {
            var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
            if (summonerTable == null || summonerTable.GetDataRow(m_SummonerId) == null)
            {
                DebugEx.Error("ExploreAISimulatorPanel", $"玩家ID {m_SummonerId} 不存在");
                return null;
            }

            var config = summonerTable.GetDataRow(m_SummonerId);
            int prefabId = (int)config.PrefabId;

            // 使用ResourceExtension加载预制体
            var prefab = await ResourceExtension.LoadPrefabAsync(prefabId);
            if (prefab == null)
            {
                DebugEx.Error("ExploreAISimulatorPanel", $"玩家预制体加载失败，PrefabId={prefabId}");
                return null;
            }

            // 实例化
            var playerGO = Object.Instantiate(prefab, m_PlayerPos, Quaternion.identity);
            playerGO.tag = "Player";
            playerGO.name = $"TestPlayer_{m_SummonerId}";

            // 确保有PlayerController
            if (playerGO.GetComponent<PlayerController>() == null)
            {
                playerGO.AddComponent<PlayerController>();
            }

            return playerGO;
        }
        catch (System.Exception ex)
        {
            DebugEx.Error("ExploreAISimulatorPanel", $"生成玩家异常：{ex.Message}");
            return null;
        }
    }

    private async UniTask<EnemyEntity> SpawnEnemyAsync(int enemyEntityId, Vector3 spawnPos)
    {
        try
        {
            var enemyTable = GF.DataTable.GetDataTable<EnemyEntityTable>();
            if (enemyTable == null || enemyTable.GetDataRow(enemyEntityId) == null)
            {
                DebugEx.Warning("ExploreAISimulatorPanel", $"敌人ID {enemyEntityId} 不存在");
                return null;
            }

            var config = enemyTable.GetDataRow(enemyEntityId);
            int prefabId = (int)config.PrefabId;

            // 使用ResourceExtension加载预制体
            var prefab = await ResourceExtension.LoadPrefabAsync(prefabId);
            if (prefab == null)
            {
                DebugEx.Warning("ExploreAISimulatorPanel", $"敌人预制体加载失败，PrefabId={prefabId}");
                return null;
            }

            // 实例化
            var enemyGO = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            enemyGO.name = $"TestEnemy_{enemyEntityId}";

            // 确保有EnemyEntity组件
            if (!enemyGO.TryGetComponent<EnemyEntity>(out var enemyEntity))
            {
                DebugEx.Warning("ExploreAISimulatorPanel", $"敌人预制体缺少EnemyEntity组件");
                Object.Destroy(enemyGO);
                return null;
            }

            // 初始化
            enemyEntity.SetEntityConfigId(enemyEntityId);
            return enemyEntity;
        }
        catch (System.Exception ex)
        {
            DebugEx.Warning("ExploreAISimulatorPanel", $"生成敌人异常：{ex.Message}");
            return null;
        }
    }

    private void ClearAll()
    {
        if (m_PlayerObject != null)
        {
            Object.Destroy(m_PlayerObject);
            m_PlayerObject = null;
        }

        foreach (var enemy in m_SpawnedEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Object.Destroy(enemy.gameObject);
            }
        }
        m_SpawnedEnemies.Clear();

        DebugEx.Log("ExploreAISimulatorPanel", "✓ 已清理所有对象");
    }

    #endregion

    #region 辅助方法

    private bool IsDataTableLoaded()
    {
        try
        {
            var table = GF.DataTable.GetDataTable<SummonerTable>();
            return table != null && table.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying)
            return;
        if (m_OwnerWindow == null)
            return;

        double now = EditorApplication.timeSinceStartup;
        if (now - m_LastRepaintTime >= k_RepaintInterval)
        {
            m_LastRepaintTime = now;
            m_OwnerWindow.Repaint();
        }
    }

    #endregion
}
