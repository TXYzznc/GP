using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// 战斗模拟器配置缓存
/// </summary>
[Serializable]
public class CombatSimulatorConfig
{
    [Serializable]
    public class ChessLockState
    {
        public int Index;
        public bool IsLocked;
    }

    public List<int> AllyChessIds = new();
    public List<int> EnemyChessIds = new();
    public Vector3 AllyBasePos;
    public Vector3 EnemyBasePos;
    public float ChessSpacing = 2f;
    public List<ChessLockState> AllyLockStates = new();
    public List<ChessLockState> EnemyLockStates = new();

    public static CombatSimulatorConfig Load()
    {
        string json = EditorPrefs.GetString("CombatSimulatorConfig", "");
        if (string.IsNullOrEmpty(json))
            return new CombatSimulatorConfig();
        try
        {
            return JsonUtility.FromJson<CombatSimulatorConfig>(json);
        }
        catch
        {
            return new CombatSimulatorConfig();
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(this, true);
        EditorPrefs.SetString("CombatSimulatorConfig", json);
    }
}

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
        private readonly Queue<(float time, double damage)> m_DealtHistory = new();
        public double CurrentDealDps { get; private set; }
        public double TotalDealtDamage { get; private set; }

        private readonly Queue<(float time, double damage)> m_ReceivedHistory = new();
        public double CurrentReceivedDps { get; private set; }
        public double TotalReceivedDamage { get; private set; }

        // 每0.5秒的伤害时间线（从StartTimeline调用时开始计算）
        private const float k_TimelineInterval = 0.5f;
        private float m_TimelineStart;
        private float m_NextTimelineSlot;
        private double m_CurrentSlotDamage;
        private bool m_TimelineActive;
        public List<(float time, double damage)> DamageTimeline { get; } = new();

        public void RecordDealtDamage(double damage)
        {
            m_DealtHistory.Enqueue((Time.time, damage));
            TotalDealtDamage += damage;
            AccumulateTimeline(damage);
        }

        public void RecordReceivedDamage(double damage)
        {
            m_ReceivedHistory.Enqueue((Time.time, damage));
            TotalReceivedDamage += damage;
        }

        public void StartTimeline()
        {
            m_TimelineStart = Time.time;
            m_NextTimelineSlot = m_TimelineStart + k_TimelineInterval;
            m_TimelineActive = true;
        }

        private void AccumulateTimeline(double damage)
        {
            if (!m_TimelineActive) return;
            m_CurrentSlotDamage += damage;
        }

        public void Tick()
        {
            float cutoff = Time.time - 1f;

            while (m_DealtHistory.Count > 0 && m_DealtHistory.Peek().time < cutoff)
                m_DealtHistory.Dequeue();
            double dealtTotal = 0;
            foreach (var (_, d) in m_DealtHistory) dealtTotal += d;
            CurrentDealDps = dealtTotal;

            while (m_ReceivedHistory.Count > 0 && m_ReceivedHistory.Peek().time < cutoff)
                m_ReceivedHistory.Dequeue();
            double receivedTotal = 0;
            foreach (var (_, d) in m_ReceivedHistory) receivedTotal += d;
            CurrentReceivedDps = receivedTotal;

            // 推进时间线槽位
            while (m_TimelineActive && Time.time >= m_NextTimelineSlot)
            {
                float elapsed = m_NextTimelineSlot - m_TimelineStart;
                DamageTimeline.Add((elapsed, m_CurrentSlotDamage));
                m_CurrentSlotDamage = 0;
                m_NextTimelineSlot += k_TimelineInterval;
            }
        }

        public void FlushTimeline()
        {
            if (!m_TimelineActive) return;
            float elapsed = Time.time - m_TimelineStart;
            DamageTimeline.Add((elapsed, m_CurrentSlotDamage));
            m_CurrentSlotDamage = 0;
            m_TimelineActive = false;
        }

        public void Reset()
        {
            m_DealtHistory.Clear();
            CurrentDealDps = 0;
            TotalDealtDamage = 0;
            m_ReceivedHistory.Clear();
            CurrentReceivedDps = 0;
            TotalReceivedDamage = 0;
            DamageTimeline.Clear();
            m_TimelineStart = 0;
            m_NextTimelineSlot = 0;
            m_CurrentSlotDamage = 0;
            m_TimelineActive = false;
        }

        public string GetDetailedInfo()
        {
            return $"造成伤害: {TotalDealtDamage:F0} | 承受伤害: {TotalReceivedDamage:F0}";
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
    private float m_TimeScale = 1f;

    /// <summary>UI折叠状态</summary>
    private bool m_FoldChessConfig = true;
    private bool m_FoldManualSkill;
    private bool m_FoldBattleDetail;
    private bool m_FoldBuffEquip;
    private bool m_FoldBatchTest;

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
    private const double k_LockRecoverInterval = 0.1;
    private const double k_LockRecoverRatio = 0.05;
    private const double k_LockMinHpRatio = 0.05;

    /// <summary>UI展开状态</summary>
    private bool m_ShowAllyList;
    private bool m_ShowEnemyList;
    private Dictionary<ChessEntity, bool> m_ShowBuffs = new();
    private Dictionary<ChessEntity, float> m_HpSliders = new();
    private Dictionary<ChessEntity, bool> m_HpEditing = new();

    /// <summary>选中的棋子详细显示</summary>
    private ChessEntity m_SelectedDetailChess;
    private Vector2 m_ChessButtonScrollPos;

    /// <summary>战斗计时（使用游戏时间Time.time，与timeScale一致）</summary>
    private float m_CombatStartGameTime;
    private float m_CombatDuration;

    /// <summary>批量测试</summary>
    private bool m_BatchRunning;
    private string m_BatchSideA = "1001";
    private string m_BatchSideB = "2001";
    private int m_BatchDuration = 30;
    private int m_BatchRepeat = 3;
    private float m_BatchTimeScale = 3f;
    private bool m_BatchBothSidesLocked = true;
    private bool m_BatchParallel = true;
    private float m_BatchArenaSpacing = 30f;
    private List<BatchTestResult> m_BatchResults = new();
    private string m_BatchProgress = "";
    private Vector2 m_BatchScrollPos;

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
        LoadConfig();
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
        if (m_OwnerWindow == null)
            m_OwnerWindow = EditorWindow.focusedWindow;

        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        DrawEnvironmentStatus();
        EditorGUILayout.Space(4);

        bool ready = Application.isPlaying && IsDataTableLoaded();

        EditorGUI.BeginDisabledGroup(!ready);
        {
            // ===== 核心区：始终显示 =====
            DrawDataRefreshControl();
            EditorGUILayout.Space(4);

            m_FoldChessConfig = EditorGUILayout.Foldout(m_FoldChessConfig, "棋子配置", true, EditorStyles.foldoutHeader);
            if (m_FoldChessConfig)
                DrawChessConfig();
            EditorGUILayout.Space(4);

            DrawSpawnControls();
            EditorGUILayout.Space(4);
            DrawCombatControls();
            EditorGUILayout.Space(4);
            DrawTimeScaleControl();
            EditorGUILayout.Space(8);

            // ===== 统计区：战斗中显示 =====
            DrawDpsPanel();
            EditorGUILayout.Space(4);
            DrawChessSelector();
            EditorGUILayout.Space(4);
            DrawSelectedChessDetail();

            // ===== 可折叠区 =====
            EditorGUILayout.Space(4);
            m_FoldManualSkill = EditorGUILayout.Foldout(m_FoldManualSkill, "手动技能触发", true, EditorStyles.foldoutHeader);
            if (m_FoldManualSkill)
                DrawManualSkillControls();

            EditorGUILayout.Space(4);
            m_FoldBuffEquip = EditorGUILayout.Foldout(m_FoldBuffEquip, "Buff / 装备控制", true, EditorStyles.foldoutHeader);
            if (m_FoldBuffEquip)
                DrawBuffAndEquipControl();

            EditorGUILayout.Space(4);
            m_FoldBattleDetail = EditorGUILayout.Foldout(m_FoldBattleDetail, "详细战斗信息", true, EditorStyles.foldoutHeader);
            if (m_FoldBattleDetail)
                DrawBattleStatus();

            EditorGUILayout.Space(8);
            m_FoldBatchTest = EditorGUILayout.Foldout(m_FoldBatchTest, "批量平衡测试", true, EditorStyles.foldoutHeader);
            if (m_FoldBatchTest)
                DrawBatchTestPanel();
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

    private void DrawTimeScaleControl()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("游戏速度", GUILayout.Width(55));
        float newScale = EditorGUILayout.Slider(m_TimeScale, 0f, 10f);
        if (Mathf.Abs(newScale - m_TimeScale) > 0.01f)
        {
            m_TimeScale = newScale;
            Time.timeScale = m_TimeScale;
        }
        if (GUILayout.Button("1x", GUILayout.Width(30))) { m_TimeScale = 1f; Time.timeScale = 1f; }
        if (GUILayout.Button("3x", GUILayout.Width(30))) { m_TimeScale = 3f; Time.timeScale = 3f; }
        if (GUILayout.Button("5x", GUILayout.Width(30))) { m_TimeScale = 5f; Time.timeScale = 5f; }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawManualSkillControls()
    {
        bool hasChess = m_AllyChessList.Count > 0 && m_EnemyChessList.Count > 0;
        if (!hasChess) return;

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
            dps = tracker.CurrentDealDps;
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
            SetLockHp(entity, !isLocked);
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

        EditorGUILayout.LabelField("战斗统计", EditorStyles.boldLabel);

        double allyDealDps = 0, allyDealDamage = 0, allyReceivedDps = 0, allyReceivedDamage = 0;
        foreach (var chess in m_AllyChessList)
        {
            if (m_DpsTrackers.TryGetValue(chess, out var tracker))
            {
                allyDealDps += tracker.CurrentDealDps;
                allyDealDamage += tracker.TotalDealtDamage;
                allyReceivedDps += tracker.CurrentReceivedDps;
                allyReceivedDamage += tracker.TotalReceivedDamage;
            }
        }

        double enemyDealDps = 0, enemyDealDamage = 0, enemyReceivedDps = 0, enemyReceivedDamage = 0;
        foreach (var chess in m_EnemyChessList)
        {
            if (m_DpsTrackers.TryGetValue(chess, out var tracker))
            {
                enemyDealDps += tracker.CurrentDealDps;
                enemyDealDamage += tracker.TotalDealtDamage;
                enemyReceivedDps += tracker.CurrentReceivedDps;
                enemyReceivedDamage += tracker.TotalReceivedDamage;
            }
        }

        // 战斗时长（游戏时间）
        float duration = m_IsCombatActive
            ? Time.time - m_CombatStartGameTime
            : m_CombatDuration;
        int minutes = (int)(duration / 60);
        int seconds = (int)(duration % 60);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField($"战斗时长: {minutes:D2}:{seconds:D2}", EditorStyles.miniBoldLabel);

        EditorGUILayout.Space(2);

        // 友方统计
        double allyAvgDps = duration > 0 ? allyDealDamage / duration : 0;
        EditorGUILayout.LabelField("【友方】输出 vs 承伤", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"造成伤害: DPS {allyDealDps:F1} | 平均DPS {allyAvgDps:F1} | 总计 {allyDealDamage:F0}");
        EditorGUILayout.LabelField($"承受伤害: DPS {allyReceivedDps:F1} | 总计 {allyReceivedDamage:F0}");

        EditorGUILayout.Space(4);

        // 敌方统计
        double enemyAvgDps = duration > 0 ? enemyDealDamage / duration : 0;
        EditorGUILayout.LabelField("【敌方】输出 vs 承伤", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"造成伤害: DPS {enemyDealDps:F1} | 平均DPS {enemyAvgDps:F1} | 总计 {enemyDealDamage:F0}");
        EditorGUILayout.LabelField($"承受伤害: DPS {enemyReceivedDps:F1} | 总计 {enemyReceivedDamage:F0}");

        EditorGUILayout.Space(4);

        // 导出按钮
        if (GUILayout.Button("导出统计数据"))
        {
            ExportCombatStats(duration);
        }

        EditorGUILayout.EndVertical();
    }

    private void ExportCombatStats(float duration)
    {
        // 先刷新所有tracker的时间线残余数据
        foreach (var tracker in m_DpsTrackers.Values)
            tracker.FlushTimeline();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 战斗统计数据 ===");
        sb.AppendLine($"战斗时长: {(int)(duration / 60):D2}:{(int)(duration % 60):D2} ({duration:F1}秒)");
        sb.AppendLine();

        int chessIndex = 0;
        void AppendChessStats(ChessEntity chess, string side)
        {
            if (!m_DpsTrackers.TryGetValue(chess, out var tracker)) return;
            chessIndex++;
            double avgDps = duration > 0 ? tracker.TotalDealtDamage / duration : 0;
            string name = chess.Config?.Name ?? "N/A";

            sb.AppendLine($"[{side}] {name}（{chessIndex}） 战斗时长：{duration:F1}s");

            // 时间线数据
            if (tracker.DamageTimeline.Count > 0)
            {
                sb.Append("  伤害时间线: ");
                for (int i = 0; i < tracker.DamageTimeline.Count; i++)
                {
                    var (t, dmg) = tracker.DamageTimeline[i];
                    sb.Append($"{t:F1}s:{dmg:F0}");
                    if (i < tracker.DamageTimeline.Count - 1) sb.Append(" | ");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"  平均DPS={avgDps:F1}，造成总伤害={tracker.TotalDealtDamage:F0}，受到总伤害={tracker.TotalReceivedDamage:F0}");
            sb.AppendLine();
        }

        sb.AppendLine("--- 友方 ---");
        foreach (var chess in m_AllyChessList)
            AppendChessStats(chess, "友方");

        sb.AppendLine("--- 敌方 ---");
        foreach (var chess in m_EnemyChessList)
            AppendChessStats(chess, "敌方");

        // 写入文件
        string dir = "Assets/测试输出日志";
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        string fileName = $"CombatStats_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        string filePath = System.IO.Path.Combine(dir, fileName);
        System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"CombatSimulator: 统计数据已导出到 {filePath}\n{sb}");
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

        // 显示此棋子的伤害统计
        if (m_DpsTrackers.TryGetValue(m_SelectedDetailChess, out var tracker))
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("伤害统计", EditorStyles.miniBoldLabel);

            // 主统计：承受伤害（最完整）
            EditorGUILayout.LabelField("➤ 承受伤害（主要统计）", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  DPS: {tracker.CurrentReceivedDps:F1}", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField($"  总计: {tracker.TotalReceivedDamage:F0}", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            // 辅助参考：造成伤害（可能不完整）
            EditorGUILayout.LabelField("  造成伤害（参考值）", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  DPS: {tracker.CurrentDealDps:F1}", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField($"  总计: {tracker.TotalDealtDamage:F0}", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("重置统计", GUILayout.Height(22)))
            {
                tracker.Reset();
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawBuffAndEquipControl()
    {
        bool hasChess = m_AllyChessList.Count > 0 || m_EnemyChessList.Count > 0;
        if (!hasChess) return;

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
        m_BuffIdInput = EditorGUILayout.IntField("Buff ID", m_BuffIdInput);
        if (GUILayout.Button("添加", GUILayout.ExpandWidth(true)))
        {
            target.BuffManager.AddBuff(m_BuffIdInput);
        }
        if (GUILayout.Button("移除", GUILayout.ExpandWidth(true)))
        {
            target.BuffManager.RemoveBuff(m_BuffIdInput);
        }
        if (GUILayout.Button("清空", GUILayout.ExpandWidth(true)))
        {
            target.BuffManager.ClearAll();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // 装备操作
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("装备操作", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        m_EquipTableId = EditorGUILayout.IntField("装备表ID", m_EquipTableId);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        m_EquipSlot = EditorGUILayout.IntField("槽位(0-2)", m_EquipSlot);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("穿戴", GUILayout.ExpandWidth(true)))
        {
            SimulateEquip(target, m_EquipTableId, m_EquipSlot);
        }
        if (GUILayout.Button("卸下", GUILayout.ExpandWidth(true)))
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

        // 配置缓存控制
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("配置缓存", GUILayout.Width(60));
        if (GUILayout.Button("保存配置", GUILayout.ExpandWidth(true)))
        {
            SaveConfig();
        }
        if (GUILayout.Button("加载配置", GUILayout.ExpandWidth(true)))
        {
            LoadConfig();
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

            // 所有棋子生成完毕，统一订阅伤害事件
            SubscribeAllDpsEvents();

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

        // 显示运行时统计窗口
        CombatStatsOverlay.Show();

        m_IsCombatActive = true;
        m_CombatStartGameTime = Time.time;
        m_CombatDuration = 0;
        foreach (var tracker in m_DpsTrackers.Values)
            tracker.StartTimeline();
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

        // 隐藏运行时统计窗口
        CombatStatsOverlay.Hide();

        m_CombatDuration = Time.time - m_CombatStartGameTime;
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
        foreach (var tracker in m_DpsTrackers.Values)
            tracker.Reset();
        m_DpsTrackers.Clear();
        UnsubscribeAllLockHp();
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

        // 5. 清除全局持久化状态，确保下次生成棋子是满血
        GlobalChessManager.Instance?.Clear();
        BattleChessManager.Instance?.Clear();

        Debug.Log("CombatSimulator: 所有棋子已销毁，数据已清理");
    }

    private void SetLockHp(ChessEntity chess, bool locked)
    {
        m_LockedChess[chess] = locked;
        m_LockRecoverTimes[chess] = EditorApplication.timeSinceStartup;

        if (chess?.Attribute != null)
        {
            chess.Attribute.HpFloor = locked ? chess.Attribute.MaxHp * k_LockMinHpRatio : 0;
        }
    }

    private void UnsubscribeAllLockHp()
    {
        foreach (var kvp in m_LockedChess)
        {
            if (kvp.Key?.Attribute != null)
                kvp.Key.Attribute.HpFloor = 0;
        }
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

    /// <summary>
    /// 统一订阅所有棋子的伤害事件（在所有棋子生成完毕后调用）
    /// 使用 OnDamageTakenWithSource 实现 100% 准确的双向伤害记录：
    /// - 受害者视角：自身 OnDamageTakenWithSource 触发 → 记录"承受伤害"
    /// - 攻击者视角：敌方 OnDamageTakenWithSource 触发且 attacker == 自身 → 记录"造成伤害"
    /// - 无来源伤害（DoT）：attacker == null → 归入对立阵营的总输出（平摊到对方所有棋子）
    /// </summary>
    private void SubscribeAllDpsEvents()
    {
        // 为每个棋子订阅"承受伤害"
        foreach (var chess in m_AllyChessList)
            SubscribeReceivedDamage(chess);
        foreach (var chess in m_EnemyChessList)
            SubscribeReceivedDamage(chess);

        // 通过受害者的 OnDamageTakenWithSource 反向归因"造成伤害"
        foreach (var victim in m_AllyChessList)
            SubscribeDealtDamageViaVictim(victim, m_EnemyChessList);
        foreach (var victim in m_EnemyChessList)
            SubscribeDealtDamageViaVictim(victim, m_AllyChessList);
    }

    private void SubscribeReceivedDamage(ChessEntity entity)
    {
        if (entity?.Attribute == null) return;
        entity.Attribute.OnDamageTakenWithSource += (dmg, isMagic, attackerAttr) =>
        {
            if (dmg > 0 && m_DpsTrackers.TryGetValue(entity, out var tracker))
                tracker.RecordReceivedDamage(dmg);
        };
    }

    /// <summary>
    /// 订阅受害者的伤害事件，将伤害归因到攻击方的 tracker
    /// </summary>
    private void SubscribeDealtDamageViaVictim(ChessEntity victim, List<ChessEntity> attackerSide)
    {
        if (victim?.Attribute == null) return;

        victim.Attribute.OnDamageTakenWithSource += (dmg, isMagic, attackerAttr) =>
        {
            if (dmg <= 0) return;

            if (attackerAttr != null)
            {
                // 有来源：精确归因到攻击者
                foreach (var attacker in attackerSide)
                {
                    if (attacker?.Attribute == attackerAttr)
                    {
                        if (m_DpsTrackers.TryGetValue(attacker, out var tracker))
                            tracker.RecordDealtDamage(dmg);
                        return;
                    }
                }
            }

            // 无来源（DoT/爆炸等）：平摊到对方阵营所有棋子
            if (attackerSide.Count > 0)
            {
                double perChessDmg = dmg / attackerSide.Count;
                foreach (var attacker in attackerSide)
                {
                    if (m_DpsTrackers.TryGetValue(attacker, out var tracker))
                        tracker.RecordDealtDamage(perChessDmg);
                }
            }
        };
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

        // 更新运行时统计显示
        if (m_IsCombatActive)
        {
            var statsCache = new Dictionary<ChessEntity, (double, double, double, double)>();
            foreach (var chess in m_AllyChessList)
            {
                if (m_DpsTrackers.TryGetValue(chess, out var tracker))
                    statsCache[chess] = (tracker.CurrentDealDps, tracker.TotalDealtDamage, tracker.CurrentReceivedDps, tracker.TotalReceivedDamage);
            }
            foreach (var chess in m_EnemyChessList)
            {
                if (m_DpsTrackers.TryGetValue(chess, out var tracker))
                    statsCache[chess] = (tracker.CurrentDealDps, tracker.TotalDealtDamage, tracker.CurrentReceivedDps, tracker.TotalReceivedDamage);
            }
            CombatStatsOverlay.UpdateStats(statsCache);
        }

        double interval = m_IsCombatActive ? k_ActiveRepaintInterval : k_IdleRepaintInterval;
        if (now - m_LastRepaintTime >= interval)
        {
            m_LastRepaintTime = now;
            m_OwnerWindow.Repaint();
        }
    }

    private void SaveConfig()
    {
        var config = new CombatSimulatorConfig();
        config.AllyChessIds = new List<int>(m_AllyChessIds);
        config.EnemyChessIds = new List<int>(m_EnemyChessIds);
        config.AllyBasePos = m_AllyBasePos;
        config.EnemyBasePos = m_EnemyBasePos;
        config.ChessSpacing = m_ChessSpacing;

        // 保存锁血状态
        config.AllyLockStates.Clear();
        for (int i = 0; i < m_AllyChessList.Count; i++)
        {
            var chess = m_AllyChessList[i];
            if (m_LockedChess.TryGetValue(chess, out var locked))
            {
                config.AllyLockStates.Add(new CombatSimulatorConfig.ChessLockState { Index = i, IsLocked = locked });
            }
        }

        config.EnemyLockStates.Clear();
        for (int i = 0; i < m_EnemyChessList.Count; i++)
        {
            var chess = m_EnemyChessList[i];
            if (m_LockedChess.TryGetValue(chess, out var locked))
            {
                config.EnemyLockStates.Add(new CombatSimulatorConfig.ChessLockState { Index = i, IsLocked = locked });
            }
        }

        config.Save();
        Debug.Log("CombatSimulator: 配置已保存");
    }

    private void LoadConfig()
    {
        var config = CombatSimulatorConfig.Load();

        m_AllyChessIds = new List<int>(config.AllyChessIds.Count > 0 ? config.AllyChessIds : new List<int> { 1001 });
        m_EnemyChessIds = new List<int>(config.EnemyChessIds.Count > 0 ? config.EnemyChessIds : new List<int> { 2001 });
        m_AllyBasePos = config.AllyBasePos != Vector3.zero ? config.AllyBasePos : new Vector3(-3f, 0f, 0f);
        m_EnemyBasePos = config.EnemyBasePos != Vector3.zero ? config.EnemyBasePos : new Vector3(3f, 0f, 0f);
        m_ChessSpacing = config.ChessSpacing > 0 ? config.ChessSpacing : 2f;

        // 恢复锁血状态（仅在已生成棋子的情况下）
        foreach (var state in config.AllyLockStates)
        {
            if (state.Index < m_AllyChessList.Count)
                m_LockedChess[m_AllyChessList[state.Index]] = state.IsLocked;
        }

        foreach (var state in config.EnemyLockStates)
        {
            if (state.Index < m_EnemyChessList.Count)
                m_LockedChess[m_EnemyChessList[state.Index]] = state.IsLocked;
        }

        Debug.Log("CombatSimulator: 配置已加载");
    }

    #region 批量测试

    private class BatchTestResult
    {
        public string SideADesc;
        public string SideBDesc;
        public double Duration;
        public double SideAAvgDps;
        public double SideATotalDealt;
        public double SideATotalReceived;
        public double SideBAvgDps;
        public double SideBTotalDealt;
        public double SideBTotalReceived;
        public int SideAWins;
        public int SideBWins;
        public int Draws;
        public int TotalRounds;
    }

    private class BattleArena
    {
        public int ArenaIndex;
        public int CampA;
        public int CampB;
        public Vector3 CenterPos;
        public List<int> SideAIds;
        public List<int> SideBIds;
        public List<ChessEntity> SideAChess = new();
        public List<ChessEntity> SideBChess = new();
        public Dictionary<ChessEntity, DpsTracker> Trackers = new();
        public float GameTimeStart;
        public bool IsFinished;
        public BatchTestResult Result;
        public int CurrentRound;
    }

    private void DrawBatchTestPanel()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginDisabledGroup(m_BatchRunning);

        EditorGUILayout.LabelField("阵营配置（逗号分隔ID，支持多个棋子）", EditorStyles.miniBoldLabel);
        m_BatchSideA = EditorGUILayout.TextField("A方棋子池", m_BatchSideA);
        m_BatchSideB = EditorGUILayout.TextField("B方棋子池", m_BatchSideB);
        EditorGUILayout.HelpBox("A方和B方分别填入棋子ID。系统会对A池×B池所有组合两两对打。\n每方可填多个ID实现群战（如 \"1001,1002\" vs \"2001,2002\"）", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        m_BatchDuration = EditorGUILayout.IntField("每场时长(秒)", m_BatchDuration);
        m_BatchRepeat = EditorGUILayout.IntField("重复次数", m_BatchRepeat);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_BatchTimeScale = EditorGUILayout.Slider("加速倍率", m_BatchTimeScale, 1f, 10f);
        m_BatchBothSidesLocked = EditorGUILayout.Toggle("双方锁血", m_BatchBothSidesLocked);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_BatchParallel = EditorGUILayout.Toggle("并行测试", m_BatchParallel);
        if (m_BatchParallel)
            m_BatchArenaSpacing = EditorGUILayout.FloatField("战场间距", m_BatchArenaSpacing);
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        // 运行时可通过顶部"游戏速度"滑条调节

        EditorGUILayout.BeginHorizontal();
        if (!m_BatchRunning)
        {
            if (GUILayout.Button("开始批量测试", GUILayout.ExpandWidth(true)))
            {
                StartBatchTestAsync().Forget();
            }
        }
        else
        {
            EditorGUILayout.LabelField(m_BatchProgress);
            if (GUILayout.Button("中止", GUILayout.Width(60)))
            {
                m_BatchRunning = false;
            }
        }
        EditorGUILayout.EndHorizontal();

        // 显示结果
        if (m_BatchResults.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("测试结果", EditorStyles.miniBoldLabel);

            m_BatchScrollPos = EditorGUILayout.BeginScrollView(m_BatchScrollPos, GUILayout.Height(200));

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("A方", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("B方", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("A方平均DPS", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("B方平均DPS", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("时长", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("胜负", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            foreach (var r in m_BatchResults)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(r.SideADesc, GUILayout.Width(80));
                EditorGUILayout.LabelField(r.SideBDesc, GUILayout.Width(80));
                EditorGUILayout.LabelField($"{r.SideAAvgDps:F1}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{r.SideBAvgDps:F1}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{r.Duration:F1}s", GUILayout.Width(50));
                string winStr = m_BatchBothSidesLocked ? "DPS比" : $"{r.SideAWins}:{r.SideBWins}:{r.Draws}";
                EditorGUILayout.LabelField(winStr, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("导出批量测试结果"))
            {
                ExportBatchResults();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private async UniTaskVoid StartBatchTestAsync()
    {
        m_BatchRunning = true;
        m_BatchResults.Clear();

        // 解析双方棋子ID池
        var sideAPool = ParseIdList(m_BatchSideA);
        var sideBPool = ParseIdList(m_BatchSideB);

        if (sideAPool.Count == 0 || sideBPool.Count == 0)
        {
            Debug.LogWarning("CombatSimulator: A方和B方都需要至少1个棋子ID");
            m_BatchRunning = false;
            return;
        }

        // 生成所有对局组合：A池每组 vs B池每组
        // 如果A方和B方各只有1个ID，则视为1v1池模式（两两对打）
        // 如果有多个ID，则视为整组 vs 整组
        var matchups = new List<(List<int> sideA, List<int> sideB)>();

        if (sideAPool.Count == 1 && sideBPool.Count == 1)
        {
            // 单ID模式：每个ID都作为独立个体两两对打
            matchups.Add((sideAPool, sideBPool));
        }
        else
        {
            // 群战模式：A组整体 vs B组整体
            matchups.Add((sideAPool, sideBPool));
        }

        float originalTimeScale = Time.timeScale;
        ClearAllChess();

        try
        {
            m_TimeScale = m_BatchTimeScale;
            Time.timeScale = m_BatchTimeScale;
            EnsureManagersReady();

            int totalMatches = matchups.Count * m_BatchRepeat;
            int completedMatches = 0;

            foreach (var (sideAIds, sideBIds) in matchups)
            {
                var result = new BatchTestResult { TotalRounds = m_BatchRepeat };
                double totalADealt = 0, totalAReceived = 0, totalBDealt = 0, totalBReceived = 0, totalDuration = 0;

                if (m_BatchParallel && m_BatchRepeat > 1)
                {
                    // 并行模式：同时开启多个战场
                    int parallelCount = Mathf.Min(m_BatchRepeat, 8);
                    int batchesNeeded = Mathf.CeilToInt((float)m_BatchRepeat / parallelCount);

                    for (int batch = 0; batch < batchesNeeded && m_BatchRunning; batch++)
                    {
                        int roundsThisBatch = Mathf.Min(parallelCount, m_BatchRepeat - batch * parallelCount);
                        var arenas = new List<BattleArena>();

                        m_BatchProgress = $"并行测试中 批次{batch + 1}/{batchesNeeded} ({roundsThisBatch}场同时)";
                        m_OwnerWindow?.Repaint();

                        // 创建多个战场
                        for (int a = 0; a < roundsThisBatch; a++)
                        {
                            var arena = await CreateArenaAsync(a, sideAIds, sideBIds);
                            if (arena != null) arenas.Add(arena);
                        }

                        if (arenas.Count == 0) continue;

                        // 所有战场创建完后统一重建敌人缓存
                        CombatEntityTracker.Instance.BuildEnemyCache();

                        // 获取名字（首次）
                        if (result.SideADesc == null)
                        {
                            result.SideADesc = GetSideDescription(arenas[0].SideAChess);
                            result.SideBDesc = GetSideDescription(arenas[0].SideBChess);
                        }

                        // 启动所有战场的AI
                        foreach (var arena in arenas)
                        {
                            foreach (var chess in arena.SideAChess)
                                chess.CombatController?.Enable();
                            foreach (var chess in arena.SideBChess)
                                chess.CombatController?.Enable();
                            arena.GameTimeStart = Time.time;
                        }

                        CombatManager.Instance.StartCombat();

                        // 等待所有战场结束
                        while (m_BatchRunning && arenas.Exists(a => !a.IsFinished))
                        {
                            foreach (var arena in arenas)
                            {
                                if (arena.IsFinished) continue;
                                float elapsed = Time.time - arena.GameTimeStart;
                                if (elapsed >= m_BatchDuration)
                                {
                                    arena.IsFinished = true;
                                    continue;
                                }
                                if (!m_BatchBothSidesLocked)
                                {
                                    bool allADead = arena.SideAChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                                    bool allBDead = arena.SideBChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                                    if (allADead || allBDead)
                                        arena.IsFinished = true;
                                }
                            }
                            await UniTask.Yield();
                        }

                        // 收集数据
                        foreach (var arena in arenas)
                        {
                            float duration = Time.time - arena.GameTimeStart;
                            totalDuration += duration;

                            double aDealt = 0, aReceived = 0, bDealt = 0, bReceived = 0;
                            foreach (var chess in arena.SideAChess)
                            {
                                if (arena.Trackers.TryGetValue(chess, out var t))
                                { aDealt += t.TotalDealtDamage; aReceived += t.TotalReceivedDamage; }
                            }
                            foreach (var chess in arena.SideBChess)
                            {
                                if (arena.Trackers.TryGetValue(chess, out var t))
                                { bDealt += t.TotalDealtDamage; bReceived += t.TotalReceivedDamage; }
                            }
                            totalADealt += aDealt; totalAReceived += aReceived;
                            totalBDealt += bDealt; totalBReceived += bReceived;

                            if (!m_BatchBothSidesLocked)
                            {
                                bool allADead = arena.SideAChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                                bool allBDead = arena.SideBChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                                if (allBDead && !allADead) result.SideAWins++;
                                else if (allADead && !allBDead) result.SideBWins++;
                                else result.Draws++;
                            }

                            completedMatches++;
                        }

                        // 清理本批次
                        CleanupBatchArenas(arenas);
                        await UniTask.Yield();
                    }
                }
                else
                {
                    // 串行模式
                    for (int round = 0; round < m_BatchRepeat && m_BatchRunning; round++)
                    {
                        completedMatches++;
                        m_BatchProgress = $"测试中 第{round + 1}/{m_BatchRepeat}轮";
                        m_OwnerWindow?.Repaint();

                        var arena = await CreateArenaAsync(0, sideAIds, sideBIds);
                        if (arena == null) continue;

                        CombatEntityTracker.Instance.BuildEnemyCache();

                        if (result.SideADesc == null)
                        {
                            result.SideADesc = GetSideDescription(arena.SideAChess);
                            result.SideBDesc = GetSideDescription(arena.SideBChess);
                        }

                        foreach (var chess in arena.SideAChess)
                            chess.CombatController?.Enable();
                        foreach (var chess in arena.SideBChess)
                            chess.CombatController?.Enable();

                        CombatManager.Instance.StartCombat();
                        arena.GameTimeStart = Time.time;

                        while (m_BatchRunning && !arena.IsFinished)
                        {
                            float elapsed = Time.time - arena.GameTimeStart;
                            if (elapsed >= m_BatchDuration) { arena.IsFinished = true; break; }
                            if (!m_BatchBothSidesLocked)
                            {
                                bool allADead = arena.SideAChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                                bool allBDead = arena.SideBChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                                if (allADead || allBDead) { arena.IsFinished = true; break; }
                            }
                            await UniTask.Yield();
                        }

                        float dur = Time.time - arena.GameTimeStart;
                        totalDuration += dur;

                        double aD = 0, aR = 0, bD = 0, bR = 0;
                        foreach (var c in arena.SideAChess)
                            if (arena.Trackers.TryGetValue(c, out var t)) { aD += t.TotalDealtDamage; aR += t.TotalReceivedDamage; }
                        foreach (var c in arena.SideBChess)
                            if (arena.Trackers.TryGetValue(c, out var t)) { bD += t.TotalDealtDamage; bR += t.TotalReceivedDamage; }
                        totalADealt += aD; totalAReceived += aR;
                        totalBDealt += bD; totalBReceived += bR;

                        if (!m_BatchBothSidesLocked)
                        {
                            bool allADead = arena.SideAChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                            bool allBDead = arena.SideBChess.TrueForAll(c => c.CurrentState == ChessState.Dead);
                            if (allBDead && !allADead) result.SideAWins++;
                            else if (allADead && !allBDead) result.SideBWins++;
                            else result.Draws++;
                        }

                        CleanupBatchArenas(new List<BattleArena> { arena });
                        await UniTask.Yield();
                    }
                }

                result.Duration = totalDuration / m_BatchRepeat;
                result.SideATotalDealt = totalADealt / m_BatchRepeat;
                result.SideATotalReceived = totalAReceived / m_BatchRepeat;
                result.SideBTotalDealt = totalBDealt / m_BatchRepeat;
                result.SideBTotalReceived = totalBReceived / m_BatchRepeat;
                result.SideAAvgDps = result.Duration > 0 ? result.SideATotalDealt / result.Duration : 0;
                result.SideBAvgDps = result.Duration > 0 ? result.SideBTotalDealt / result.Duration : 0;
                m_BatchResults.Add(result);
            }
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            m_TimeScale = originalTimeScale;
            CampRelationService.ReleaseAllBattlefields();
            ClearAllChess();
            m_BatchRunning = false;
            m_BatchProgress = "测试完成";
            m_OwnerWindow?.Repaint();
            Debug.Log($"CombatSimulator: 批量测试完成，共 {m_BatchResults.Count} 组对局");
        }
    }

    private async UniTask<BattleArena> CreateArenaAsync(int arenaIndex, List<int> sideAIds, List<int> sideBIds)
    {
        var (campA, campB) = CampRelationService.AllocateBattlefield();

        Vector3 arenaCenter = Vector3.forward * arenaIndex * m_BatchArenaSpacing;
        Vector3 sideABase = arenaCenter + Vector3.left * 3f;
        Vector3 sideBBase = arenaCenter + Vector3.right * 3f;

        var arena = new BattleArena
        {
            ArenaIndex = arenaIndex,
            CampA = campA,
            CampB = campB,
            CenterPos = arenaCenter,
            SideAIds = sideAIds,
            SideBIds = sideBIds,
        };

        // 生成A方棋子
        for (int i = 0; i < sideAIds.Count; i++)
        {
            Vector3 pos = sideABase + Vector3.forward * i * 2f;
            var chess = await SummonChessManager.Instance.SpawnChessAsync(sideAIds[i], pos, campA);
            if (chess == null) continue;
            arena.SideAChess.Add(chess);
            arena.Trackers[chess] = new DpsTracker();
            SetLockHp(chess, m_BatchBothSidesLocked);
        }

        // 生成B方棋子
        for (int i = 0; i < sideBIds.Count; i++)
        {
            Vector3 pos = sideBBase + Vector3.forward * i * 2f;
            var chess = await SummonChessManager.Instance.SpawnChessAsync(sideBIds[i], pos, campB);
            if (chess == null) continue;
            arena.SideBChess.Add(chess);
            arena.Trackers[chess] = new DpsTracker();
            SetLockHp(chess, m_BatchBothSidesLocked);
        }

        if (arena.SideAChess.Count == 0 || arena.SideBChess.Count == 0)
        {
            Debug.LogWarning($"CombatSimulator: 战场{arenaIndex}棋子生成不完整");
            CleanupBatchArenas(new List<BattleArena> { arena });
            return null;
        }

        // 让双方面向对方
        Vector3 aCenterPos = Vector3.zero, bCenterPos = Vector3.zero;
        foreach (var c in arena.SideAChess) aCenterPos += c.transform.position;
        foreach (var c in arena.SideBChess) bCenterPos += c.transform.position;
        aCenterPos /= arena.SideAChess.Count;
        bCenterPos /= arena.SideBChess.Count;

        foreach (var c in arena.SideAChess) c.transform.LookAt(bCenterPos);
        foreach (var c in arena.SideBChess) c.transform.LookAt(aCenterPos);

        // 订阅伤害事件
        SubscribeArenaDpsEvents(arena);

        return arena;
    }

    private void SubscribeArenaDpsEvents(BattleArena arena)
    {
        // A方承受伤害
        foreach (var chess in arena.SideAChess)
        {
            if (chess?.Attribute == null) continue;
            chess.Attribute.OnDamageTakenWithSource += (dmg, isMagic, attackerAttr) =>
            {
                if (dmg <= 0) return;
                if (arena.Trackers.TryGetValue(chess, out var tracker))
                    tracker.RecordReceivedDamage(dmg);

                // 归因到B方攻击者
                if (attackerAttr != null)
                {
                    foreach (var attacker in arena.SideBChess)
                    {
                        if (attacker?.Attribute == attackerAttr && arena.Trackers.TryGetValue(attacker, out var aTracker))
                        { aTracker.RecordDealtDamage(dmg); return; }
                    }
                }
                else if (arena.SideBChess.Count > 0)
                {
                    double perChess = dmg / arena.SideBChess.Count;
                    foreach (var attacker in arena.SideBChess)
                        if (arena.Trackers.TryGetValue(attacker, out var aTracker))
                            aTracker.RecordDealtDamage(perChess);
                }
            };
        }

        // B方承受伤害
        foreach (var chess in arena.SideBChess)
        {
            if (chess?.Attribute == null) continue;
            chess.Attribute.OnDamageTakenWithSource += (dmg, isMagic, attackerAttr) =>
            {
                if (dmg <= 0) return;
                if (arena.Trackers.TryGetValue(chess, out var tracker))
                    tracker.RecordReceivedDamage(dmg);

                // 归因到A方攻击者
                if (attackerAttr != null)
                {
                    foreach (var attacker in arena.SideAChess)
                    {
                        if (attacker?.Attribute == attackerAttr && arena.Trackers.TryGetValue(attacker, out var aTracker))
                        { aTracker.RecordDealtDamage(dmg); return; }
                    }
                }
                else if (arena.SideAChess.Count > 0)
                {
                    double perChess = dmg / arena.SideAChess.Count;
                    foreach (var attacker in arena.SideAChess)
                        if (arena.Trackers.TryGetValue(attacker, out var aTracker))
                            aTracker.RecordDealtDamage(perChess);
                }
            };
        }
    }

    private void CleanupBatchArenas(List<BattleArena> arenas)
    {
        foreach (var arena in arenas)
        {
            foreach (var chess in arena.SideAChess)
            {
                chess.CombatController?.Disable();
                if (chess?.Attribute != null) chess.Attribute.HpFloor = 0;
                SummonChessManager.Instance?.DestroyChess(chess);
                m_LockedChess.Remove(chess);
                m_LockRecoverTimes.Remove(chess);
            }
            foreach (var chess in arena.SideBChess)
            {
                chess.CombatController?.Disable();
                if (chess?.Attribute != null) chess.Attribute.HpFloor = 0;
                SummonChessManager.Instance?.DestroyChess(chess);
                m_LockedChess.Remove(chess);
                m_LockRecoverTimes.Remove(chess);
            }
        }

        if (CombatManager.Instance != null && CombatManager.Instance.IsInCombat)
            CombatManager.Instance.EndCombat(false);

        CombatEntityTracker.Instance?.ClearEnemyCache();
    }

    private List<int> ParseIdList(string input)
    {
        var ids = new List<int>();
        foreach (var part in input.Split(','))
        {
            if (int.TryParse(part.Trim(), out int id))
                ids.Add(id);
        }
        return ids;
    }

    private string GetSideDescription(List<ChessEntity> chess)
    {
        if (chess.Count == 0) return "空";
        if (chess.Count == 1) return chess[0].Config?.Name ?? "?";
        return string.Join("+", chess.ConvertAll(c => c.Config?.Name ?? "?"));
    }

    private void ExportBatchResults()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 批量平衡测试结果 ===");
        sb.AppendLine($"测试时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"参数: 每场{m_BatchDuration}秒, 重复{m_BatchRepeat}次, 加速{m_BatchTimeScale}x, 锁血={m_BatchBothSidesLocked}, 并行={m_BatchParallel}");
        sb.AppendLine();
        sb.AppendLine("A方\tB方\t战斗时长\tA方平均DPS\tA方总输出\tA方总承伤\tB方平均DPS\tB方总输出\tB方总承伤\t胜负(A:B:平)");

        foreach (var r in m_BatchResults)
        {
            string winStr = m_BatchBothSidesLocked ? "-" : $"{r.SideAWins}:{r.SideBWins}:{r.Draws}";
            sb.AppendLine($"{r.SideADesc}\t{r.SideBDesc}\t{r.Duration:F1}s\t{r.SideAAvgDps:F1}\t{r.SideATotalDealt:F0}\t{r.SideATotalReceived:F0}\t{r.SideBAvgDps:F1}\t{r.SideBTotalDealt:F0}\t{r.SideBTotalReceived:F0}\t{winStr}");
        }

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"CombatSimulator: 批量测试结果已复制到剪贴板（可直接粘贴到Excel）\n{sb}");
    }

    #endregion

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

/// <summary>
/// 战斗统计运行时显示 - 在游戏运行时实时显示对战数据
/// </summary>
public class CombatStatsOverlay : MonoBehaviour
{
    private static CombatStatsOverlay m_Instance;
    private GUIStyle m_BoxStyle;
    private GUIStyle m_LabelStyle;
    private Dictionary<ChessEntity, (double dealDps, double dealDamage, double receivedDps, double receivedDamage)> m_StatsCache = new();
    private bool m_ShowOverlay = true;
    private Vector2 m_ScrollPos;

    public static void Show()
    {
        if (m_Instance == null)
        {
            var go = new GameObject("[CombatStatsOverlay]");
            m_Instance = go.AddComponent<CombatStatsOverlay>();
        }
        m_Instance.m_ShowOverlay = true;
    }

    public static void Hide()
    {
        if (m_Instance != null)
            m_Instance.m_ShowOverlay = false;
    }

    public static void UpdateStats(Dictionary<ChessEntity, (double dealDps, double dealDamage, double receivedDps, double receivedDamage)> stats)
    {
        if (m_Instance != null)
            m_Instance.m_StatsCache = new Dictionary<ChessEntity, (double, double, double, double)>(stats);
    }

    private void OnGUI()
    {
        if (!m_ShowOverlay || m_StatsCache.Count == 0) return;

        if (m_BoxStyle == null)
        {
            m_BoxStyle = new GUIStyle(GUI.skin.box);
            m_BoxStyle.normal.background = Texture2D.whiteTexture;
            m_BoxStyle.normal.textColor = Color.black;
            m_LabelStyle = new GUIStyle(GUI.skin.label);
            m_LabelStyle.normal.textColor = Color.black;
            m_LabelStyle.fontSize = 11;
        }

        Rect windowRect = new Rect(10, 10, 380, 240);
        GUILayout.BeginArea(windowRect, "战斗统计", m_BoxStyle);

        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(220));

        foreach (var (chess, (dealDps, dealDamage, receivedDps, receivedDamage)) in m_StatsCache)
        {
            GUILayout.BeginVertical("box");

            // 棋子名称
            string chessName = chess.Config?.Name ?? "N/A";
            GUILayout.Label($"【{chessName}】", m_LabelStyle);

            // 造成伤害
            GUILayout.Label($"造成: DPS {dealDps:F1} | 总计 {dealDamage:F0}", m_LabelStyle);

            // 承受伤害
            GUILayout.Label($"承受: DPS {receivedDps:F1} | 总计 {receivedDamage:F0}", m_LabelStyle);

            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
