using UnityEngine;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 战斗系统测试控制器
/// 用于快速测试战斗配置加载和敌人生成
/// 手动挂载到场景中的空对象上
/// </summary>
public class CombatTestController : MonoBehaviour
{
    #region Inspector 参数

    [Header("测试配置")]
    [SerializeField]
    [Tooltip("要测试的战斗配置ID")]
    private int m_TestBattleConfigId = 1;

    [SerializeField]
    [Tooltip("战斗场地生成位置")]
    private Vector3 m_ArenaSpawnPosition = Vector3.zero;

    #endregion

    #region 私有字段

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region Unity 生命周期

    private void Start()
    {
        DebugEx.LogModule("CombatTestController", "战斗测试控制器已启动");
        m_IsInitialized = true;
    }

    #endregion

    #region 测试功能实现

    /// <summary>
    /// 测试配置加载
    /// </summary>
    public void TestConfigLoading()
    {
        DebugEx.LogModule("CombatTestController", $"开始测试配置加载 ID={m_TestBattleConfigId}");

        // 测试加载 EnemyTable
        var dataTable = GF.DataTable.GetDataTable<EnemyTable>();
        if (dataTable == null)
        {
            DebugEx.ErrorModule("CombatTestController", "EnemyTable 数据表未加载！");
            return;
        }

        var enemyData = dataTable.GetDataRow(m_TestBattleConfigId);
        if (enemyData == null)
        {
            DebugEx.ErrorModule("CombatTestController", $"未找到配置 ID={m_TestBattleConfigId}");
            return;
        }

        // 输出配置信息
        string formationName = GetFormationName(enemyData.FormationType);
        DebugEx.Success("CombatTestController",
            $"配置加载成功：\n" +
            $"名称：{enemyData.EnemyName}\n" +
            $"棋子数量：{enemyData.ChessIds?.Length ?? 0}\n" +
            $"阵型类型：{formationName}\n" +
            $"间距：{enemyData.Spacing}\n" +
            $"难度倍率：{enemyData.DifficultyMultiplier}");
    }

    /// <summary>
    /// 生成战斗场地
    /// </summary>
    public async void SpawnBattleArena()
    {
        DebugEx.LogModule("CombatTestController", "开始生成战斗场地");

        if (BattleArenaManager.Instance == null)
        {
            DebugEx.ErrorModule("CombatTestController", "BattleArenaManager 未初始化");
            return;
        }

        // 如果已有场地，先销毁
        if (BattleArenaManager.Instance.CurrentArena != null)
        {
            DebugEx.LogModule("CombatTestController", "检测到已有场地，先销毁");
            BattleArenaManager.Instance.DestroyArena();
        }

        // 获取玩家位置（如果有）
        if (PlayerCharacterManager.Instance?.CurrentPlayerCharacter != null)
        {
            await BattleArenaManager.Instance.SpawnArenaAsync(PlayerCharacterManager.Instance.CurrentPlayerCharacter.transform);
            DebugEx.Success("CombatTestController", "战斗场地生成完成");
        }
    }

    /// <summary>
    /// 生成测试敌人
    /// </summary>
    [ContextMenu("生成敌人")]
    public async void SpawnTestEnemies()
    {
        DebugEx.LogModule("CombatTestController", $"开始生成测试敌人 ConfigID={m_TestBattleConfigId}");

        // 确保管理器存在
        if (EnemySpawnManager.Instance == null)
        {
            DebugEx.ErrorModule("CombatTestController", "EnemySpawnManager 未初始化");
            return;
        }

        if (BattleArenaManager.Instance == null)
        {
            DebugEx.ErrorModule("CombatTestController", "BattleArenaManager 未初始化");
            return;
        }

        // 如果没有战斗场地，先生成
        if (BattleArenaManager.Instance.CurrentArena == null)
        {
            DebugEx.LogModule("CombatTestController", "战斗场地不存在，先生成场地");
            await SpawnBattleArenaInternal();
        }

        // 加载配置并生成敌人
        EnemySpawnManager.Instance.LoadFromEnemyTable(m_TestBattleConfigId);
        await EnemySpawnManager.Instance.SpawnWaveAsync();

        DebugEx.Success("CombatTestController", "测试敌人生成完成");
    }

    /// <summary>
    /// 清理战斗系统
    /// </summary>
    [ContextMenu("清理战斗系统")]
    public void CleanupCombatSystem()
    {
        DebugEx.LogModule("CombatTestController", "开始清理战斗系统");

        // 清空所有棋子
        if (SummonChessManager.Instance != null)
        {
            var allChess = SummonChessManager.Instance.GetAllChess();
            int count = allChess.Count;

            for (int i = allChess.Count - 1; i >= 0; i--)
            {
                SummonChessManager.Instance.DestroyChess(allChess[i]);
            }

            DebugEx.LogModule("CombatTestController", $"已清空 {count} 个棋子");
        }

        // 清理棋子管理器
        if (CombatEntityTracker.Instance != null)
        {
            CombatEntityTracker.Instance.Clear();
        }

        // 销毁战斗场地
        if (BattleArenaManager.Instance != null)
        {
            BattleArenaManager.Instance.DestroyArena();
        }

        // 清理敌人管理器
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.Cleanup();
        }

        // 清理战斗会话数据
        if (CombatSessionData.Instance != null)
        {
            CombatSessionData.Instance.Clear();
        }

        DebugEx.Success("CombatTestController", "战斗系统清理完成");
    }

    /// <summary>
    /// 内部生成战斗场地（不输出额外日志）
    /// </summary>
    private async UniTask SpawnBattleArenaInternal()
    {
        if (PlayerCharacterManager.Instance?.CurrentPlayerCharacter != null)
        {
            await BattleArenaManager.Instance.SpawnArenaAsync(PlayerCharacterManager.Instance.CurrentPlayerCharacter.transform);
        }
    }

    /// <summary>
    /// 获取阵型名称
    /// </summary>
    private string GetFormationName(int formationType)
    {
        return formationType switch
        {
            1 => "横排",
            2 => "竖排",
            3 => "矩形",
            _ => "未知"
        };
    }

    #endregion

#if UNITY_EDITOR

    #region 自定义 Inspector

    [CustomEditor(typeof(CombatTestController), true)]
    public class CombatTestControllerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            CombatTestController controller = (CombatTestController)target;

            // 只在运行时显示按钮
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请先运行游戏", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("测试按钮", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 第一行：测试配置加载 | 生成战斗场地
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("测试配置加载", GUILayout.Height(35)))
            {
                controller.TestConfigLoading();
            }
            if (GUILayout.Button("生成战斗场地", GUILayout.Height(35)))
            {
                controller.SpawnBattleArena();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 第二行：生成敌人 | 清理战斗系统
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成敌人", GUILayout.Height(35)))
            {
                controller.SpawnTestEnemies();
            }
            if (GUILayout.Button("清理战斗系统", GUILayout.Height(35)))
            {
                controller.CleanupCombatSystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 调试信息
            EditorGUILayout.LabelField("调试信息", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // 显示棋子数量
            if (SummonChessManager.Instance != null)
            {
                var allChess = SummonChessManager.Instance.GetAllChess();
                EditorGUILayout.LabelField("场景中棋子数量", allChess.Count.ToString());

                if (CombatEntityTracker.Instance != null)
                {
                    var playerChess = CombatEntityTracker.Instance.GetAllies(0);
                    var enemyChess = CombatEntityTracker.Instance.GetAllies(1);
                    EditorGUILayout.LabelField("我方棋子", playerChess?.Count.ToString() ?? "0");
                    EditorGUILayout.LabelField("敌方棋子", enemyChess?.Count.ToString() ?? "0");
                }
            }

            // 显示战斗场地状态
            if (BattleArenaManager.Instance != null)
            {
                bool hasArena = BattleArenaManager.Instance.CurrentArena != null;
                EditorGUILayout.LabelField("战斗场地", hasArena ? "已生成" : "未生成");
            }

            EditorGUI.indentLevel--;

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    #endregion

#endif
}
