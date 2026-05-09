using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗模拟器面板 - 一键生成棋子对并启动战斗测试
/// 支持 AI 自动战斗和手动触发技能两种模式
/// </summary>
[ToolHubItem("战斗工具/战斗模拟器", "一键生成棋子对并模拟战斗，支持AI自动战斗和手动技能触发", 50)]
public class CombatSimulatorPanel : IToolHubPanel
{
    #region DPS统计器

    private class DpsTracker
    {
        private readonly Queue<(float time, double damage)> m_History = new();
        public double CurrentDps { get; private set; }

        public void Record(double damage) => m_History.Enqueue((Time.time, damage));

        public void Tick()
        {
            float cutoff = Time.time - 1f;
            while (m_History.Count > 0 && m_History.Peek().time < cutoff)
                m_History.Dequeue();
            double total = 0;
            foreach (var (_, d) in m_History) total += d;
            CurrentDps = total;
        }
    }

    #endregion

    #region 配置字段

    private List<int> m_AllyChessIds = new() { 1001 };
    private List<int> m_EnemyChessIds = new() { 2001 };
    private Vector3 m_AllyBasePos = new Vector3(-3f, 0f, 0f);
    private Vector3 m_EnemyBasePos = new Vector3(3f, 0f, 0f);
    private float m_ChessSpacing = 2f; // 棋子间距

    #endregion

    #region 运行时状态

    private List<ChessEntity> m_AllyChessList = new();
    private List<ChessEntity> m_EnemyChessList = new();
    private bool m_IsCombatActive;
    private bool m_IsSpawning;
    private Vector2 m_ScrollPos;
    private int m_SelectedManualTargetSide; // 0=友方操作, 1=敌方操作
    private int m_SelectedManualTargetIndex; // 操作的棋子索引

    /// <summary>秒伤统计 - key为ChessEntity</summary>
    private Dictionary<ChessEntity, DpsTracker> m_DpsTrackers = new();

    /// <summary>Buff 控制</summary>
    private int m_BuffIdInput = 1;
    private int m_BuffOpTargetSide; // 0=友方, 1=敌方
    private int m_BuffOpTargetIndex; // 操作的棋子索引

    /// <summary>装备模拟</summary>
    private int m_EquipTableId = 1;
    private int m_EquipSlot = 0;

    /// <summary>数据刷新</summary>
    private string m_LastRefreshInfo = "未刷新";

    /// <summary>锁血系统 - key为ChessEntity</summary>
    private Dictionary<ChessEntity, bool> m_LockedChess = new();
    private Dictionary<ChessEntity, double> m_LockRecoverTimes = new();
    private const double k_LockRecoverInterval = 0.1; // 每0.1秒恢复一次
    private const double k_LockRecoverRatio = 0.05; // 每次恢复最大HP的5%

    /// <summary>UI展开状态</summary>
    private bool m_ShowAllyList;
    private bool m_ShowEnemyList;
    private Dictionary<ChessEntity, bool> m_ShowBuffs = new();
    private Dictionary<ChessEntity, float> m_HpSliders = new();
    private Dictionary<ChessEntity, bool> m_HpEditing = new();

    /// <summary>选中的棋子详细显示</summary>
    private ChessEntity m_SelectedDetailChess;
    private Vector2 m_ChessButtonScrollPos;

    /// <summary>持有工具箱 EditorWindow 引用，用于主动 Repaint</summary>
    private EditorWindow m_OwnerWindow;
    /// <summary>上次刷新时间</summary>
    private double m_LastRepaintTime;
    /// <summary>战斗中刷新间隔（30 FPS）</summary>
    private const double k_ActiveRepaintInterval = 0.033;
    /// <summary>非战斗中刷新间隔（5 FPS）</summary>
    private const double k_IdleRepaintInterval = 0.2;

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
        m_AllyChessList.Clear();
        m_EnemyChessList.Clear();
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
            DrawDataRefreshControl();
            EditorGUILayout.Space(4);
            DrawChessConfig();
            EditorGUILayout.Space(4);
            DrawSpawnControls();
            EditorGUILayout.Space(4);
            DrawCombatControls();
            EditorGUILayout.Space(4);
            DrawManualSkillControls();
            EditorGUILayout.Space(8);
            DrawDpsPanel();
            EditorGUILayout.Space(4);
            DrawChessSelector();
            EditorGUILayout.Space(4);
            DrawSelectedChessDetail();
            EditorGUILayout.Space(4);
            DrawBuffAndEquipControl();
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
        DrawChessIdList(m_AllyChessIds, "友方棋子ID");
        m_AllyBasePos = EditorGUILayout.Vector3Field("生成基点", m_AllyBasePos);
        EditorGUILayout.EndVertical();

        // 敌方
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("敌方棋子", EditorStyles.miniBoldLabel);
        DrawChessIdList(m_EnemyChessIds, "敌方棋子ID");
        m_EnemyBasePos = EditorGUILayout.Vector3Field("生成基点", m_EnemyBasePos);
        EditorGUILayout.EndVertical();

        // 通用参数
        EditorGUILayout.BeginVertical("box");
        m_ChessSpacing = EditorGUILayout.FloatField("棋子间距", m_ChessSpacing);
        EditorGUILayout.EndVertical();
    }

    private void DrawChessIdList(List<int> idList, string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        for (int i = 0; i < idList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
            idList[i] = EditorGUILayout.IntField(idList[i]);
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                idList.RemoveAt(i);
                i--;
                EditorGUILayout.EndHorizontal();
                continue;
            }
            EditorGUILayout.EndHorizontal();
            DrawChessPreview(idList[i]);
        }

        if (GUILayout.Button("+ 添加棋子", GUILayout.Height(22)))
        {
            idList.Add(1001);
        }
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

        bool hasChess = m_AllyChessList.Count > 0 || m_EnemyChessList.Count > 0;

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(m_IsSpawning);
        if (GUILayout.Button("生成棋子", GUILayout.Height(30)))
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
        EditorGUILayout.LabelField($"  友方: {m_AllyChessList.Count} 个棋子已生成");
        EditorGUILayout.LabelField($"  敌方: {m_EnemyChessList.Count} 个棋子已生成");
    }

    private void DrawCombatControls()
    {
        EditorGUILayout.LabelField("战斗控制", EditorStyles.boldLabel);

        bool hasChess = m_AllyChessList.Count > 0 && m_EnemyChessList.Count > 0;

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
        bool hasChess = m_AllyChessList.Count > 0 && m_EnemyChessList.Count > 0;
        if (!hasChess) return;

        EditorGUILayout.LabelField("手动技能触发", EditorStyles.boldLabel);

        string[] targets = { "操作友方棋子", "操作敌方棋子" };
        m_SelectedManualTargetSide = GUILayout.Toolbar(m_SelectedManualTargetSide, targets);

        var sideList = m_SelectedManualTargetSide == 0 ? m_AllyChessList : m_EnemyChessList;
        if (sideList.Count == 0) return;

        m_SelectedManualTargetIndex = Mathf.Clamp(m_SelectedManualTargetIndex, 0, sideList.Count - 1);
        EditorGUILayout.LabelField($"选择棋子: [{m_SelectedManualTargetIndex}] / {sideList.Count - 1}");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("◀", GUILayout.Width(30)))
            m_SelectedManualTargetIndex = Mathf.Max(0, m_SelectedManualTargetIndex - 1);
        if (GUILayout.Button("▶", GUILayout.Width(30)))
            m_SelectedManualTargetIndex = Mathf.Min(sideList.Count - 1, m_SelectedManualTargetIndex + 1);
        EditorGUILayout.EndHorizontal();

        var attacker = sideList[m_SelectedManualTargetIndex];
        var defenderList = m_SelectedManualTargetSide == 0 ? m_EnemyChessList : m_AllyChessList;
        var defender = defenderList.Count > 0 ? defenderList[0] : null;

        if (attacker == null || attacker.CombatController == null)
        {
            EditorGUILayout.HelpBox("棋子或战斗控制器为空", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("普攻", GUILayout.Height(28)))
        {
            if (defender != null)
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
        if (m_AllyChessList.Count == 0 && m_EnemyChessList.Count == 0) return;

        EditorGUILayout.LabelField("战斗信息", EditorStyles.boldLabel);

        // 友方棋子列表
        m_ShowAllyList = EditorGUILayout.Foldout(m_ShowAllyList, $"友方 ({m_AllyChessList.Count})");
        if (m_ShowAllyList)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_AllyChessList.Count; i++)
            {
                var chess = m_AllyChessList[i];
                DrawEntityStatusCompact($"友方[{i}]", chess);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // 敌方棋子列表
        m_ShowEnemyList = EditorGUILayout.Foldout(m_ShowEnemyList, $"敌方 ({m_EnemyChessList.Count})");
        if (m_ShowEnemyList)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_EnemyChessList.Count; i++)
            {
                var chess = m_EnemyChessList[i];
                DrawEntityStatusCompact($"敌方[{i}]", chess);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawEntityStatusCompact(string label, ChessEntity entity)
    {
        if (entity == null) return;

        var attr = entity.Attribute;
        if (attr == null) return;

        EditorGUILayout.BeginVertical("box");

        // 名称和DPS
        double dps = 0;
        if (m_DpsTrackers.TryGetValue(entity, out var tracker))
            dps = tracker.CurrentDps;
        bool isLocked = m_LockedChess.ContainsKey(entity) && m_LockedChess[entity];
        string lockStr = isLocked ? " 🔒" : "";
        EditorGUILayout.LabelField($"{label}: {entity.Config?.Name ?? "N/A"} | DPS:{dps:F1}{lockStr}", EditorStyles.miniBoldLabel);

        // HP/MP 进度条
        DrawProgressBar("HP", attr.CurrentHp, attr.MaxHp, new Color(0.2f, 0.8f, 0.2f));
        DrawProgressBar("MP", attr.CurrentMp, attr.MaxMp, new Color(0.3f, 0.5f, 0.9f));

        // 核心属性
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"攻击:{attr.AtkDamage:F0}", GUILayout.Width(70));
        EditorGUILayout.LabelField($"护甲:{attr.Armor:F0}", GUILayout.Width(70));
        EditorGUILayout.LabelField($"魔抗:{attr.MagicResist:F0}", GUILayout.Width(70));
        EditorGUILayout.LabelField($"法强:{attr.SpellPower:F0}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        // 状态快捷按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("HP满", GUILayout.ExpandWidth(true))) attr.SetHp(attr.MaxHp);
        if (GUILayout.Button("MP满", GUILayout.ExpandWidth(true))) attr.SetMp(attr.MaxMp);
        if (GUILayout.Button("半血", GUILayout.ExpandWidth(true))) attr.SetHp(attr.MaxHp * 0.5);
        if (GUILayout.Button("濒死", GUILayout.ExpandWidth(true))) attr.SetHp(attr.MaxHp * 0.05);

        // 锁血按钮
        GUIStyle lockStyle = new GUIStyle(GUI.skin.button);
        lockStyle.normal.textColor = isLocked ? new Color(1f, 0.2f, 0.2f) : Color.white;
        if (GUILayout.Button(isLocked ? "🔒锁血中" : "🔓解锁血量", lockStyle, GUILayout.ExpandWidth(true)))
        {
            m_LockedChess[entity] = !isLocked;
            m_LockRecoverTimes[entity] = EditorApplication.timeSinceStartup;
        }
        EditorGUILayout.EndHorizontal();

        // HP 滑条
        if (!m_HpSliders.ContainsKey(entity)) m_HpSliders[entity] = 1f;
        if (!m_HpEditing.ContainsKey(entity)) m_HpEditing[entity] = false;

        EditorGUI.BeginChangeCheck();
        if (!m_HpEditing[entity]) m_HpSliders[entity] = (float)(attr.CurrentHp / attr.MaxHp);
        float newHpRatio = EditorGUILayout.Slider("HP调节", m_HpSliders[entity], 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            m_HpEditing[entity] = true;
            m_HpSliders[entity] = newHpRatio;
            attr.SetHp(attr.MaxHp * newHpRatio);
        }
        else { m_HpEditing[entity] = false; }

        // Buff 列表
        if (entity.BuffManager != null)
        {
            var buffs = entity.BuffManager.GetAllBuffs();
            if (!m_ShowBuffs.ContainsKey(entity)) m_ShowBuffs[entity] = false;
            m_ShowBuffs[entity] = EditorGUILayout.Foldout(m_ShowBuffs[entity], $"Buff ({buffs.Count}个)");
            if (m_ShowBuffs[entity] && buffs.Count > 0)
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

    private void DrawDpsPanel()
    {
        if (m_AllyChessList.Count == 0 && m_EnemyChessList.Count == 0) return;

        EditorGUILayout.LabelField("秒伤统计（实时）", EditorStyles.boldLabel);

        double allyTotalDps = 0;
        foreach (var chess in m_AllyChessList)
        {
            if (m_DpsTrackers.TryGetValue(chess, out var tracker))
                allyTotalDps += tracker.CurrentDps;
        }

        double enemyTotalDps = 0;
        foreach (var chess in m_EnemyChessList)
        {
            if (m_DpsTrackers.TryGetValue(chess, out var tracker))
                enemyTotalDps += tracker.CurrentDps;
        }

        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"友方总DPS: {allyTotalDps:F1}", GUILayout.Width(150));
        EditorGUILayout.LabelField($"敌方总DPS: {enemyTotalDps:F1}", GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawChessSelector()
    {
        if (m_AllyChessList.Count == 0 && m_EnemyChessList.Count == 0) return;

        EditorGUILayout.LabelField("对战对象", EditorStyles.boldLabel);

        m_ChessButtonScrollPos = EditorGUILayout.BeginScrollView(m_ChessButtonScrollPos, GUILayout.Height(60));

        EditorGUILayout.BeginHorizontal("box");

        // 友方按钮
        for (int i = 0; i < m_AllyChessList.Count; i++)
        {
            var chess = m_AllyChessList[i];
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            if (m_SelectedDetailChess == chess)
            {
                btnStyle.normal.textColor = Color.green;
                btnStyle.normal.background = Texture2D.whiteTexture;
            }

            if (GUILayout.Button($"友[{i}]\n{chess.Config?.Name}", btnStyle, GUILayout.Width(80), GUILayout.Height(40)))
            {
                m_SelectedDetailChess = chess;
            }
        }

        // 敌方按钮
        for (int i = 0; i < m_EnemyChessList.Count; i++)
        {
            var chess = m_EnemyChessList[i];
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            if (m_SelectedDetailChess == chess)
            {
                btnStyle.normal.textColor = Color.red;
                btnStyle.normal.background = Texture2D.whiteTexture;
            }

            if (GUILayout.Button($"敌[{i}]\n{chess.Config?.Name}", btnStyle, GUILayout.Width(80), GUILayout.Height(40)))
            {
                m_SelectedDetailChess = chess;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedChessDetail()
    {
        if (m_SelectedDetailChess == null) return;

        EditorGUILayout.LabelField("详细信息", EditorStyles.boldLabel);
        DrawEntityStatusCompact("详情", m_SelectedDetailChess);
    }

    private void DrawBuffAndEquipControl()
    {
        bool hasChess = m_AllyChessList.Count > 0 || m_EnemyChessList.Count > 0;
        if (!hasChess) return;

        EditorGUILayout.LabelField("Buff / 装备控制", EditorStyles.boldLabel);

        string[] sides = { "友方", "敌方" };
        m_BuffOpTargetSide = GUILayout.Toolbar(m_BuffOpTargetSide, sides);
        var sideList = m_BuffOpTargetSide == 0 ? m_AllyChessList : m_EnemyChessList;

        if (sideList.Count == 0)
        {
            EditorGUILayout.HelpBox("该阵营无棋子", MessageType.Warning);
            return;
        }

        m_BuffOpTargetIndex = Mathf.Clamp(m_BuffOpTargetIndex, 0, sideList.Count - 1);
        EditorGUILayout.LabelField($"选择棋子: [{m_BuffOpTargetIndex}] {sideList[m_BuffOpTargetIndex].Config?.Name ?? "N/A"}");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("◀", GUILayout.Width(30)))
            m_BuffOpTargetIndex = Mathf.Max(0, m_BuffOpTargetIndex - 1);
        if (GUILayout.Button("▶", GUILayout.Width(30)))
            m_BuffOpTargetIndex = Mathf.Min(sideList.Count - 1, m_BuffOpTargetIndex + 1);
        EditorGUILayout.EndHorizontal();

        var target = sideList[m_BuffOpTargetIndex];
        if (target == null || target.BuffManager == null)
        {
            EditorGUILayout.HelpBox("棋子或 Buff 管理器为空", MessageType.Warning);
            return;
        }

        // Buff 操作
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Buff 操作", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        m_BuffIdInput = EditorGUILayout.IntField("Buff ID", m_BuffIdInput, GUILayout.Width(160));
        if (GUILayout.Button("添加", GUILayout.Width(50)))
        {
            target.BuffManager.AddBuff(m_BuffIdInput);
        }
        if (GUILayout.Button("移除", GUILayout.Width(50)))
        {
            target.BuffManager.RemoveBuff(m_BuffIdInput);
        }
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            target.BuffManager.ClearAll();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // 装备操作
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("装备操作", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        m_EquipTableId = EditorGUILayout.IntField("装备表ID", m_EquipTableId, GUILayout.Width(160));
        m_EquipSlot = EditorGUILayout.IntField("槽位(0-2)", m_EquipSlot, GUILayout.Width(120));
        if (GUILayout.Button("穿戴", GUILayout.Width(50)))
        {
            SimulateEquip(target, m_EquipTableId, m_EquipSlot);
        }
        if (GUILayout.Button("卸下", GUILayout.Width(50)))
        {
            if (ChessEquipmentManager.Instance != null)
            {
                ChessEquipmentManager.Instance.UnequipItem(target.ChessId, m_EquipSlot);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawDataRefreshControl()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"配置热更新  {m_LastRefreshInfo}", GUILayout.ExpandWidth(true));
        if (GUILayout.Button("更新数据", GUILayout.Width(80)))
        {
            ChessDataManager.Instance.ReloadConfigs();
            m_LastRefreshInfo = $"已刷新 {System.DateTime.Now:HH:mm:ss}（注：仅刷新内存缓存，不重读文件）";
        }
        EditorGUILayout.EndHorizontal();
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
            for (int i = 0; i < m_AllyChessIds.Count; i++)
            {
                Vector3 pos = m_AllyBasePos + Vector3.right * i * m_ChessSpacing;
                var chess = await SummonChessManager.Instance.SpawnChessAsync(
                    m_AllyChessIds[i], pos, 0);

                if (chess != null)
                {
                    m_AllyChessList.Add(chess);
                    var tracker = new DpsTracker();
                    m_DpsTrackers[chess] = tracker;
                    SubscribeDpsEvents(chess, tracker);
                    m_LockedChess[chess] = false;
                    m_LockRecoverTimes[chess] = EditorApplication.timeSinceStartup;
                    m_ShowBuffs[chess] = false;
                    m_HpSliders[chess] = 1f;
                    m_HpEditing[chess] = false;
                }
                else
                {
                    Debug.LogError($"CombatSimulator: 友方棋子生成失败 ID={m_AllyChessIds[i]}");
                }
            }

            // 生成敌方
            for (int i = 0; i < m_EnemyChessIds.Count; i++)
            {
                Vector3 pos = m_EnemyBasePos + Vector3.left * i * m_ChessSpacing;
                var chess = await SummonChessManager.Instance.SpawnChessAsync(
                    m_EnemyChessIds[i], pos, 1);

                if (chess != null)
                {
                    m_EnemyChessList.Add(chess);
                    var tracker = new DpsTracker();
                    m_DpsTrackers[chess] = tracker;
                    SubscribeDpsEvents(chess, tracker);
                    m_LockedChess[chess] = false;
                    m_LockRecoverTimes[chess] = EditorApplication.timeSinceStartup;
                    m_ShowBuffs[chess] = false;
                    m_HpSliders[chess] = 1f;
                    m_HpEditing[chess] = false;
                }
                else
                {
                    Debug.LogError($"CombatSimulator: 敌方棋子生成失败 ID={m_EnemyChessIds[i]}");
                }
            }

            // 让敌方面向友方
            if (m_AllyChessList.Count > 0 && m_EnemyChessList.Count > 0)
            {
                var allyChess = m_AllyChessList[0];
                foreach (var enemyChess in m_EnemyChessList)
                {
                    enemyChess.transform.LookAt(allyChess.transform);
                    allyChess.transform.LookAt(enemyChess.transform);
                }
            }

            Debug.Log($"CombatSimulator: 棋子生成完毕 - 友方{m_AllyChessList.Count}个, 敌方{m_EnemyChessList.Count}个");
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
        if (m_AllyChessList.Count == 0 || m_EnemyChessList.Count == 0) return;

        _ = DamageFloatingTextManager.Instance;

        // 构建敌人缓存
        CombatEntityTracker.Instance?.BuildEnemyCache();

        // 启用所有棋子的战斗控制器
        foreach (var chess in m_AllyChessList)
            chess.CombatController?.Enable();
        foreach (var chess in m_EnemyChessList)
            chess.CombatController?.Enable();

        // 标记战斗状态
        CombatManager.Instance.StartCombat();

        m_IsCombatActive = true;
        Debug.Log("CombatSimulator: AI 战斗已启动");
    }

    private void PauseCombat()
    {
        foreach (var chess in m_AllyChessList)
            chess.CombatController?.Disable();
        foreach (var chess in m_EnemyChessList)
            chess.CombatController?.Disable();
        Debug.Log("CombatSimulator: AI 已暂停");
    }

    private void ResumeCombat()
    {
        foreach (var chess in m_AllyChessList)
            chess.CombatController?.Enable();
        foreach (var chess in m_EnemyChessList)
            chess.CombatController?.Enable();
        Debug.Log("CombatSimulator: AI 已恢复");
    }

    private void StopCombat()
    {
        // 停用所有棋子的战斗控制器
        foreach (var chess in m_AllyChessList)
            chess.CombatController?.Disable();
        foreach (var chess in m_EnemyChessList)
            chess.CombatController?.Disable();

        // 通知战斗管理器结束战斗
        if (CombatManager.Instance != null && CombatManager.Instance.IsInCombat)
        {
            CombatManager.Instance.EndCombat(false);
        }

        m_IsCombatActive = false;
        Debug.Log("CombatSimulator: 战斗已结束");
    }

    private void ClearAllChess()
    {
        // 1. 先停止战斗（清理 AI、战斗控制器）
        if (m_IsCombatActive)
        {
            StopCombat();
        }

        // 2. 销毁所有棋子
        if (SummonChessManager.Instance != null)
        {
            foreach (var chess in m_AllyChessList)
                SummonChessManager.Instance.DestroyChess(chess);
            foreach (var chess in m_EnemyChessList)
                SummonChessManager.Instance.DestroyChess(chess);
        }

        // 3. 清理数据字典
        m_AllyChessList.Clear();
        m_EnemyChessList.Clear();
        m_DpsTrackers.Clear();
        m_LockedChess.Clear();
        m_LockRecoverTimes.Clear();
        m_ShowBuffs.Clear();
        m_HpSliders.Clear();
        m_HpEditing.Clear();
        m_SelectedDetailChess = null;

        // 4. 清理任何残留的棋子
        if (SummonChessManager.Instance != null)
        {
            var allChess = SummonChessManager.Instance.GetAllChess();
            if (allChess.Count > 0)
            {
                Debug.LogWarning($"CombatSimulator: 发现 {allChess.Count} 个残留的棋子，执行清理");
                SummonChessManager.Instance.DestroyAllChess();
            }
        }

        Debug.Log("CombatSimulator: 所有棋子已销毁，数据已清理");
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

        // ItemManager 加载物品配置表
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.LoadAllTables();
        }

        // CombatEntityTracker 和 BattleChessManager 都是懒加载单例，访问即创建
        _ = CombatEntityTracker.Instance;
        _ = BattleChessManager.Instance;
    }

    private void SubscribeDpsEvents(ChessEntity entity, DpsTracker tracker)
    {
        if (entity?.Attribute == null) return;
        entity.Attribute.OnDamageDealt += (dmg, _) => tracker.Record(dmg);
    }

    private void SimulateEquip(ChessEntity entity, int equipTableId, int slot)
    {
        if (ChessEquipmentManager.Instance == null)
        {
            Debug.LogWarning("ChessEquipmentManager 未初始化");
            return;
        }

        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("ItemManager 未初始化");
            return;
        }

        var item = ItemManager.Instance.CreateItem(equipTableId) as EquipmentItem;
        if (item == null)
        {
            Debug.LogWarning($"创建装备失败 ID:{equipTableId}");
            return;
        }

        ChessEquipmentManager.Instance.EquipItem(entity.ChessId, item, slot);
    }

    #endregion

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying) return;
        if (m_AllyChessList.Count == 0 && m_EnemyChessList.Count == 0) return;
        if (m_OwnerWindow == null) return;

        double now = EditorApplication.timeSinceStartup;

        // 锁血恢复
        foreach (var chess in m_AllyChessList)
        {
            if (chess?.Attribute != null && m_LockedChess.ContainsKey(chess) && m_LockedChess[chess])
            {
                if (now - m_LockRecoverTimes[chess] >= k_LockRecoverInterval)
                {
                    m_LockRecoverTimes[chess] = now;
                    double recoverAmount = chess.Attribute.MaxHp * k_LockRecoverRatio;
                    chess.Attribute.SetHp(chess.Attribute.CurrentHp + recoverAmount);
                }
            }
        }

        foreach (var chess in m_EnemyChessList)
        {
            if (chess?.Attribute != null && m_LockedChess.ContainsKey(chess) && m_LockedChess[chess])
            {
                if (now - m_LockRecoverTimes[chess] >= k_LockRecoverInterval)
                {
                    m_LockRecoverTimes[chess] = now;
                    double recoverAmount = chess.Attribute.MaxHp * k_LockRecoverRatio;
                    chess.Attribute.SetHp(chess.Attribute.CurrentHp + recoverAmount);
                }
            }
        }

        // 秒伤计算
        foreach (var tracker in m_DpsTrackers.Values)
            tracker.Tick();

        double interval = m_IsCombatActive ? k_ActiveRepaintInterval : k_IdleRepaintInterval;
        if (now - m_LastRepaintTime >= interval)
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
