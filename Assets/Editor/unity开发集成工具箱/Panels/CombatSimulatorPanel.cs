using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗模拟器面板 - 一键生成棋子对并启动战斗测试
/// 支持 AI 自动战斗和手动触发技能两种模式
/// </summary>
[ToolHubItem("战斗工具/战斗模拟器", "一键生成棋子对并模拟战斗，支持AI自动战斗和手动技能触发", 50)]
public class CombatSimulatorPanel : IToolHubPanel
{
    #region 配置字段

    private int m_AllyChessId = 1001;
    private int m_EnemyChessId = 2001;
    private Vector3 m_AllyPos = new Vector3(-3f, 0f, 0f);
    private Vector3 m_EnemyPos = new Vector3(3f, 0f, 0f);

    #endregion

    #region 运行时状态

    private ChessEntity m_AllyChess;
    private ChessEntity m_EnemyChess;
    private bool m_IsCombatActive;
    private bool m_IsSpawning;
    private int m_SelectedManualTarget; // 0=友方操作, 1=敌方操作
    private Vector2 m_ScrollPos;
    private bool m_ShowAllyBuffs;
    private bool m_ShowEnemyBuffs;

    /// <summary>持有工具箱 EditorWindow 引用，用于主动 Repaint</summary>
    private EditorWindow m_OwnerWindow;
    /// <summary>上次刷新时间</summary>
    private double m_LastRepaintTime;
    /// <summary>刷新间隔（秒），约 20 FPS</summary>
    private const double k_RepaintInterval = 0.05;

    #endregion

    #region IToolHubPanel

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
        // 清理引用，但不销毁对象（用户可能还在观察）
        m_AllyChess = null;
        m_EnemyChess = null;
    }

    public string GetHelpText()
    {
        return "战斗模拟器：在 PlayMode 下（DataTable 已加载后）使用。\n" +
               "1. 配置友方/敌方棋子 ID 和位置\n" +
               "2. 点击「生成棋子对」创建棋子\n" +
               "3. 启动 AI 自动战斗或手动触发技能";
    }

    public void OnGUI()
    {
        // 首次 OnGUI 时捕获宿主窗口引用
        if (m_OwnerWindow == null)
            m_OwnerWindow = EditorWindow.focusedWindow;

        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        DrawEnvironmentStatus();
        EditorGUILayout.Space(4);

        bool ready = Application.isPlaying && IsDataTableLoaded();

        EditorGUI.BeginDisabledGroup(!ready);
        {
            DrawChessConfig();
            EditorGUILayout.Space(4);
            DrawSpawnControls();
            EditorGUILayout.Space(4);
            DrawCombatControls();
            EditorGUILayout.Space(4);
            DrawManualSkillControls();
            EditorGUILayout.Space(8);
            DrawBattleStatus();
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
        bool isCombatTestMode = isPlaying && CombatTestBootstrapper.IsCombatTestMode;
        bool testReady = isPlaying && CombatTestBootstrapper.IsReady;
        bool dtLoaded = isPlaying && IsDataTableLoaded();
        bool chessDataLoaded = isPlaying && ChessDataManager.Instance.IsLoaded;

        EditorGUILayout.BeginHorizontal();
        DrawStatusLabel("PlayMode", isPlaying);
        if (isCombatTestMode)
            DrawStatusLabel("TestMode", testReady);
        DrawStatusLabel("DataTable", dtLoaded);
        DrawStatusLabel("ChessData", chessDataLoaded);
        EditorGUILayout.EndHorizontal();

        if (!isPlaying)
        {
            EditorGUILayout.HelpBox(
                "使用方式：\n" +
                "1. 在测试场景中放置 CombatTestBootstrapper 组件\n" +
                "2. 从测试场景进入 PlayMode（会自动加载 Launch 场景初始化框架）\n" +
                "3. 或从 Launch 场景正常启动游戏后使用",
                MessageType.Info);
        }
        else if (isCombatTestMode && !testReady)
        {
            EditorGUILayout.HelpBox("正在初始化 GF 框架和加载配置表，请稍候...", MessageType.Info);
        }
        else if (!dtLoaded)
        {
            EditorGUILayout.HelpBox("DataTable 尚未加载，请等待 PreloadProcedure 完成", MessageType.Warning);
        }
        else if (!chessDataLoaded)
        {
            if (GUILayout.Button("手动加载 ChessData"))
            {
                ChessDataManager.Instance.LoadConfigs();
            }
        }
    }

    private void DrawStatusLabel(string label, bool ok)
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = ok ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
        string icon = ok ? "[OK]" : "[X]";
        EditorGUILayout.LabelField($"{icon} {label}", style, GUILayout.Width(120));
    }

    private void DrawChessConfig()
    {
        EditorGUILayout.LabelField("棋子配置", EditorStyles.boldLabel);

        // 友方
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("友方棋子", EditorStyles.miniBoldLabel);
        m_AllyChessId = EditorGUILayout.IntField("棋子ID", m_AllyChessId);
        m_AllyPos = EditorGUILayout.Vector3Field("生成位置", m_AllyPos);
        DrawChessPreview(m_AllyChessId);
        EditorGUILayout.EndVertical();

        // 敌方
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("敌方棋子", EditorStyles.miniBoldLabel);
        m_EnemyChessId = EditorGUILayout.IntField("棋子ID", m_EnemyChessId);
        m_EnemyPos = EditorGUILayout.Vector3Field("生成位置", m_EnemyPos);
        DrawChessPreview(m_EnemyChessId);
        EditorGUILayout.EndVertical();
    }

    private void DrawChessPreview(int chessId)
    {
        if (!Application.isPlaying || !ChessDataManager.Instance.IsLoaded)
            return;

        if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
        {
            EditorGUILayout.LabelField($"  -> {config.Name} (品质:{config.Quality} 人口:{config.PopCost})");
        }
        else
        {
            EditorGUILayout.LabelField("  -> 未找到该ID的配置", EditorStyles.miniLabel);
        }
    }

    private void DrawSpawnControls()
    {
        EditorGUILayout.LabelField("生成控制", EditorStyles.boldLabel);

        bool hasChess = m_AllyChess != null || m_EnemyChess != null;

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(m_IsSpawning);
        if (GUILayout.Button("生成棋子对", GUILayout.Height(30)))
        {
            SpawnChessPairAsync().Forget();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!hasChess);
        if (GUILayout.Button("清除所有棋子", GUILayout.Height(30)))
        {
            ClearAllChess();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // 显示当前棋子状态
        if (m_AllyChess != null)
            EditorGUILayout.LabelField($"  友方: {m_AllyChess.Config?.Name ?? "N/A"} (已生成)");
        if (m_EnemyChess != null)
            EditorGUILayout.LabelField($"  敌方: {m_EnemyChess.Config?.Name ?? "N/A"} (已生成)");
    }

    private void DrawCombatControls()
    {
        EditorGUILayout.LabelField("战斗控制", EditorStyles.boldLabel);

        bool hasChess = m_AllyChess != null && m_EnemyChess != null;

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(!hasChess || m_IsCombatActive);
        if (GUILayout.Button("启动AI战斗", GUILayout.Height(28)))
        {
            StartAICombat();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!m_IsCombatActive);
        if (GUILayout.Button("暂停AI", GUILayout.Height(28)))
        {
            PauseCombat();
        }
        if (GUILayout.Button("恢复AI", GUILayout.Height(28)))
        {
            ResumeCombat();
        }
        if (GUILayout.Button("结束战斗", GUILayout.Height(28)))
        {
            StopCombat();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (m_IsCombatActive)
        {
            EditorGUILayout.LabelField("  状态: 战斗进行中", EditorStyles.miniBoldLabel);
        }
    }

    private void DrawManualSkillControls()
    {
        bool hasChess = m_AllyChess != null && m_EnemyChess != null;
        if (!hasChess) return;

        EditorGUILayout.LabelField("手动技能触发", EditorStyles.boldLabel);

        string[] targets = { "操作友方棋子", "操作敌方棋子" };
        m_SelectedManualTarget = GUILayout.Toolbar(m_SelectedManualTarget, targets);

        ChessEntity attacker = m_SelectedManualTarget == 0 ? m_AllyChess : m_EnemyChess;
        ChessEntity defender = m_SelectedManualTarget == 0 ? m_EnemyChess : m_AllyChess;

        if (attacker == null || attacker.CombatController == null)
        {
            EditorGUILayout.HelpBox("棋子或战斗控制器为空", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("普攻", GUILayout.Height(28)))
        {
            attacker.CombatController.TriggerAttackFromAI(defender);
        }

        EditorGUI.BeginDisabledGroup(attacker.Skill1 == null);
        if (GUILayout.Button("技能1", GUILayout.Height(28)))
        {
            attacker.CombatController.TriggerSkill1FromAI();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(attacker.Skill2 == null);
        if (GUILayout.Button("大招", GUILayout.Height(28)))
        {
            attacker.CombatController.TriggerSkill2FromAI();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // 显示技能信息
        DrawSkillInfo("普攻", attacker.NormalAttackConfig);
        DrawSkillInfo("技能1", attacker.Skill1Config);
        DrawSkillInfo("大招", attacker.Skill2Config);
    }

    private void DrawSkillInfo(string label, SummonChessSkillTable config)
    {
        if (config == null) return;
        EditorGUILayout.LabelField($"  {label}: {config.Name} (伤害系数:{config.DamageCoeff:F2} CD:{config.Cooldown:F1}s)");
    }

    private void DrawBattleStatus()
    {
        if (m_AllyChess == null && m_EnemyChess == null) return;

        EditorGUILayout.LabelField("战斗信息", EditorStyles.boldLabel);

        if (m_AllyChess != null)
        {
            DrawEntityStatus("友方", m_AllyChess, ref m_ShowAllyBuffs);
        }

        EditorGUILayout.Space(2);

        if (m_EnemyChess != null)
        {
            DrawEntityStatus("敌方", m_EnemyChess, ref m_ShowEnemyBuffs);
        }

        // 刷新由 OnEditorUpdate 驱动，此处无需操作
    }

    private void DrawEntityStatus(string label, ChessEntity entity, ref bool showBuffs)
    {
        if (entity == null) return;

        var attr = entity.Attribute;
        if (attr == null) return;

        EditorGUILayout.BeginVertical("box");

        // 名称和阵营
        EditorGUILayout.LabelField($"{label}: {entity.Config?.Name ?? "N/A"}", EditorStyles.miniBoldLabel);

        // HP/MP 进度条
        DrawProgressBar("HP", attr.CurrentHp, attr.MaxHp, new Color(0.2f, 0.8f, 0.2f));
        DrawProgressBar("MP", attr.CurrentMp, attr.MaxMp, new Color(0.3f, 0.5f, 0.9f));

        // 核心属性
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"攻击:{attr.AtkDamage:F0}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"护甲:{attr.Armor:F0}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"魔抗:{attr.MagicResist:F0}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"攻速:{attr.AtkSpeed:F2}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"法强:{attr.SpellPower:F0}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"暴击:{attr.CritRate:P0}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"暴伤:{attr.CritDamage:F2}x", GUILayout.Width(80));
        EditorGUILayout.LabelField($"移速:{attr.MoveSpeed:F1}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // Buff 列表
        if (entity.BuffManager != null)
        {
            var buffs = entity.BuffManager.GetAllBuffs();
            showBuffs = EditorGUILayout.Foldout(showBuffs, $"Buff ({buffs.Count}个)");
            if (showBuffs && buffs.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var buff in buffs)
                {
                    string info = $"- [ID:{buff.BuffId}] x{buff.StackCount}";
                    if (buff.IsFinished) info += " (已结束)";
                    EditorGUILayout.LabelField(info);
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawProgressBar(string label, double current, double max, Color color)
    {
        float ratio = max > 0 ? (float)(current / max) : 0f;
        Rect rect = EditorGUILayout.GetControlRect(false, 18);

        // 背景
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        // 填充
        Rect fillRect = new Rect(rect.x, rect.y, rect.width * ratio, rect.height);
        EditorGUI.DrawRect(fillRect, color);
        // 文字
        EditorGUI.LabelField(rect, $"  {label}: {current:F0} / {max:F0}");
    }

    #endregion

    #region 核心逻辑

    private async UniTaskVoid SpawnChessPairAsync()
    {
        m_IsSpawning = true;

        try
        {
            // 先清除已有的
            ClearAllChess();

            // 确保管理器就绪
            EnsureManagersReady();

            // 生成友方
            m_AllyChess = await SummonChessManager.Instance.SpawnChessAsync(
                m_AllyChessId, m_AllyPos, 0);

            if (m_AllyChess == null)
            {
                Debug.LogError($"CombatSimulator: 友方棋子生成失败 ID={m_AllyChessId}");
                return;
            }

            // 生成敌方
            m_EnemyChess = await SummonChessManager.Instance.SpawnChessAsync(
                m_EnemyChessId, m_EnemyPos, 1);

            if (m_EnemyChess == null)
            {
                Debug.LogError($"CombatSimulator: 敌方棋子生成失败 ID={m_EnemyChessId}");
                return;
            }

            // 让敌方面向友方
            m_EnemyChess.transform.LookAt(m_AllyChess.transform);
            m_AllyChess.transform.LookAt(m_EnemyChess.transform);

            Debug.Log($"CombatSimulator: 棋子对生成完毕 - {m_AllyChess.Config.Name} vs {m_EnemyChess.Config.Name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CombatSimulator: 生成棋子失败 - {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            m_IsSpawning = false;
        }
    }

    private void StartAICombat()
    {
        if (m_AllyChess == null || m_EnemyChess == null) return;

        // 构建敌人缓存
        CombatEntityTracker.Instance?.BuildEnemyCache();

        // 启用双方的战斗控制器
        m_AllyChess.CombatController?.Enable();
        m_EnemyChess.CombatController?.Enable();

        // 标记战斗状态
        CombatManager.Instance.StartCombat();

        m_IsCombatActive = true;
        Debug.Log("CombatSimulator: AI 战斗已启动");
    }

    private void PauseCombat()
    {
        m_AllyChess?.CombatController?.Disable();
        m_EnemyChess?.CombatController?.Disable();
        Debug.Log("CombatSimulator: AI 已暂停");
    }

    private void ResumeCombat()
    {
        m_AllyChess?.CombatController?.Enable();
        m_EnemyChess?.CombatController?.Enable();
        Debug.Log("CombatSimulator: AI 已恢复");
    }

    private void StopCombat()
    {
        // 停用 AI
        m_AllyChess?.CombatController?.Disable();
        m_EnemyChess?.CombatController?.Disable();

        if (CombatManager.Instance.IsInCombat)
        {
            CombatManager.Instance.EndCombat(false);
        }

        m_IsCombatActive = false;
        Debug.Log("CombatSimulator: 战斗已结束");
    }

    private void ClearAllChess()
    {
        if (m_IsCombatActive)
        {
            StopCombat();
        }

        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.DestroyAllChess();
        }

        m_AllyChess = null;
        m_EnemyChess = null;
    }

    private void EnsureManagersReady()
    {
        // SummonChessManager 是 MonoBehaviour 单例，需要 GameObject 承载
        if (SummonChessManager.Instance == null)
        {
            var go = new GameObject("[CombatSimulator] SummonChessManager");
            go.AddComponent<SummonChessManager>();
        }

        // ChessDataManager 如果没加载过配置，手动加载
        if (!ChessDataManager.Instance.IsLoaded)
        {
            ChessDataManager.Instance.LoadConfigs();
        }

        // CombatEntityTracker 和 BattleChessManager 都是懒加载单例，访问即创建
        _ = CombatEntityTracker.Instance;
        _ = BattleChessManager.Instance;
    }

    #endregion

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying) return;
        if (m_AllyChess == null && m_EnemyChess == null) return;
        if (m_OwnerWindow == null) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - m_LastRepaintTime >= k_RepaintInterval)
        {
            m_LastRepaintTime = now;
            m_OwnerWindow.Repaint();
        }
    }

    #region 辅助方法

    private bool IsDataTableLoaded()
    {
        try
        {
            var table = GF.DataTable.GetDataTable<SummonChessTable>();
            return table != null && table.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
