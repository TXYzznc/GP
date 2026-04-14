using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 棋子技能测试面板 - 用于测试棋子技能效果、动画和特效
/// </summary>
[ToolHubItem("测试工具/棋子技能测试器", "测试棋子的技能效果、动画和特效，支持运行时参数修改", 60)]
public class ChessSkillTestPanel : IToolHubPanel
{
    #region 字段

    private ChessEntity m_TargetChess;
    private ChessEntity m_EnemyTarget;
    private Vector2 m_ScrollPos = Vector2.zero;

    // 当前选中的技能配置
    private int m_SelectedSkillTab = 0; // 0=普攻, 1=技能1, 2=大招

    // 运行时修改的技能参数（用于覆盖配置表）
    private SkillRuntimeParams m_NormalAttackParams;
    private SkillRuntimeParams m_Skill1Params;
    private SkillRuntimeParams m_Skill2Params;

    // 技能ID输入
    private int m_InputNormalAttackId = 0;
    private int m_InputSkill1Id = 0;
    private int m_InputSkill2Id = 0;

    #endregion

    #region 数据结构

    /// <summary>
    /// 运行时技能参数（对应 SummonChessSkillTable 的字段）
    /// </summary>
    [Serializable]
    private class SkillRuntimeParams
    {
        // 基础参数
        public string Name = "";
        public int SkillType = 0;
        public int DamageType = 0;
        public double DamageCoeff = 1.0;
        public int EffectHitType = 0;

        // 投射物
        public GameObject ProjectilePrefab;
        public double ProjectileSpeed = 20.0;

        // Buff
        public int BuffTriggerType = 0;
        public List<int> BuffIds = new List<int>();
        public List<int> SelfBuffIds = new List<int>();

        // 伤害和消耗
        public double BaseDamage = 0;
        public double MpCost = 0;
        public double MpRestore = 0;

        // 范围和时间
        public double Cooldown = 0;
        public double CastRange = 0;
        public double AreaRadius = 0;
        public double Duration = 0;

        // 触发次数
        public int HitCount = 1;
        public int PenetrationCount = 1;

        // 特效
        public GameObject EffectPrefab;
        public float EffectSpawnHeight = 0f;
        public GameObject HitEffectPrefab;
        public GameObject IconSprite;

        public string Desc = "";

        /// <summary>
        /// 从配置表加载参数
        /// </summary>
        public void LoadFromConfig(SummonChessSkillTable config)
        {
            if (config == null)
                return;

            Name = config.Name;
            SkillType = config.SkillType;
            DamageType = config.DamageType;
            DamageCoeff = config.DamageCoeff;
            EffectHitType = config.EffectHitType;
            BuffTriggerType = config.BuffTriggerType;
            BaseDamage = config.BaseDamage;
            MpCost = config.MpCost;
            MpRestore = config.MpRestore;
            Cooldown = config.Cooldown;
            CastRange = config.CastRange;
            AreaRadius = config.AreaRadius;
            Duration = config.Duration;
            HitCount = config.HitCount;
            PenetrationCount = config.PenetrationCount;
            ProjectileSpeed = config.ProjectileSpeed;
            EffectSpawnHeight = config.EffectSpawnHeight;
            Desc = config.Desc;

            // 加载Buff列表
            BuffIds.Clear();
            if (config.BuffIds != null)
            {
                BuffIds.AddRange(config.BuffIds);
            }

            SelfBuffIds.Clear();
            if (config.SelfBuffIds != null)
            {
                SelfBuffIds.AddRange(config.SelfBuffIds);
            }

            // 加载资源引用（通过资源ID）
            LoadResourceReferences(config);
        }

        /// <summary>
        /// 通过资源ID加载资源引用
        /// </summary>
        private void LoadResourceReferences(SummonChessSkillTable config)
        {
            // 注意：这里需要在运行时通过GF.Resource加载
            // Editor模式下可以通过AssetDatabase加载
            // 暂时留空，运行时再实现
        }
    }

    #endregion

    #region IToolHubPanel 实现

    public void OnEnable()
    {
        m_NormalAttackParams = new SkillRuntimeParams();
        m_Skill1Params = new SkillRuntimeParams();
        m_Skill2Params = new SkillRuntimeParams();
    }

    public void OnDisable() { }

    public void OnDestroy() { }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("棋子技能测试工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawChessSelection();
        EditorGUILayout.Space();

        if (m_TargetChess != null)
        {
            DrawSkillButtons();
            EditorGUILayout.Space();

            DrawSkillParametersEditor();
        }
        else
        {
            EditorGUILayout.HelpBox("请在场景中选择一个棋子实体进行测试", MessageType.Info);
        }
    }

    #endregion

    #region UI 绘制

    /// <summary>
    /// 绘制棋子选择区域
    /// </summary>
    private void DrawChessSelection()
    {
        EditorGUILayout.LabelField("测试对象", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        m_TargetChess = (ChessEntity)
            EditorGUILayout.ObjectField("目标棋子", m_TargetChess, typeof(ChessEntity), true);

        if (EditorGUI.EndChangeCheck() && m_TargetChess != null)
        {
            // 加载棋子的技能配置
            LoadChessSkillConfigs();
        }

        m_EnemyTarget = (ChessEntity)
            EditorGUILayout.ObjectField(
                "索敌目标（可选）",
                m_EnemyTarget,
                typeof(ChessEntity),
                true
            );

        if (m_TargetChess != null)
        {
            EditorGUILayout.HelpBox(
                $"当前棋子: {m_TargetChess.Config?.Name}\n"
                    + $"阵营: {m_TargetChess.Camp}\n"
                    + $"HP: {m_TargetChess.Attribute?.CurrentHp}/{m_TargetChess.Attribute?.MaxHp}\n"
                    + $"MP: {m_TargetChess.Attribute?.CurrentMp}/{m_TargetChess.Attribute?.MaxMp}",
                MessageType.None
            );
        }
    }

    /// <summary>
    /// 绘制技能触发按钮
    /// </summary>
    private void DrawSkillButtons()
    {
        EditorGUILayout.LabelField("技能触发", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请在运行模式下使用此工具", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // 普攻按钮
        EditorGUI.BeginDisabledGroup(m_TargetChess.NormalAttack == null);
        if (GUILayout.Button("普攻", GUILayout.Height(40)))
        {
            TriggerNormalAttack();
        }
        EditorGUI.EndDisabledGroup();

        // 技能1按钮
        EditorGUI.BeginDisabledGroup(m_TargetChess.Skill1 == null);
        if (GUILayout.Button("技能一", GUILayout.Height(40)))
        {
            TriggerSkill1();
        }
        EditorGUI.EndDisabledGroup();

        // 大招按钮
        EditorGUI.BeginDisabledGroup(m_TargetChess.Skill2 == null);
        if (GUILayout.Button("大招", GUILayout.Height(40)))
        {
            TriggerSkill2();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // 显示技能状态
        EditorGUILayout.Space();
        DrawSkillStatus();
    }

    /// <summary>
    /// 绘制技能状态信息
    /// </summary>
    private void DrawSkillStatus()
    {
        EditorGUILayout.LabelField("技能状态", EditorStyles.boldLabel);

        if (m_TargetChess.Skill1 != null)
        {
            bool canCast1 = m_TargetChess.Skill1.CanCast();
            EditorGUILayout.LabelField($"技能一: {(canCast1 ? "可用" : "冷却中")}");
        }

        if (m_TargetChess.Skill2 != null)
        {
            bool canCast2 = m_TargetChess.Skill2.CanCast();
            EditorGUILayout.LabelField($"大招: {(canCast2 ? "可用" : "冷却中")}");
        }
    }

    /// <summary>
    /// 绘制技能参数编辑器
    /// </summary>
    private void DrawSkillParametersEditor()
    {
        EditorGUILayout.LabelField("技能参数编辑", EditorStyles.boldLabel);

        // Tab 选择
        string[] tabs = { "普攻", "技能一", "大招" };
        m_SelectedSkillTab = GUILayout.Toolbar(m_SelectedSkillTab, tabs);

        EditorGUILayout.Space();

        // 技能ID输入和加载按钮
        DrawSkillIdLoader();

        EditorGUILayout.Space();

        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        switch (m_SelectedSkillTab)
        {
            case 0:
                DrawSkillParams("普攻配置", m_NormalAttackParams);
                break;
            case 1:
                DrawSkillParams("技能一配置", m_Skill1Params);
                break;
            case 2:
                DrawSkillParams("大招配置", m_Skill2Params);
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制技能ID加载器
    /// </summary>
    private void DrawSkillIdLoader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("从配置表加载", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        switch (m_SelectedSkillTab)
        {
            case 0:
                m_InputNormalAttackId = EditorGUILayout.IntField(
                    "技能ID",
                    m_InputNormalAttackId,
                    GUILayout.Width(200)
                );
                if (GUILayout.Button("加载配置", GUILayout.Width(80)))
                {
                    LoadSkillConfigById(m_InputNormalAttackId, m_NormalAttackParams);
                }
                break;
            case 1:
                m_InputSkill1Id = EditorGUILayout.IntField(
                    "技能ID",
                    m_InputSkill1Id,
                    GUILayout.Width(200)
                );
                if (GUILayout.Button("加载配置", GUILayout.Width(80)))
                {
                    LoadSkillConfigById(m_InputSkill1Id, m_Skill1Params);
                }
                break;
            case 2:
                m_InputSkill2Id = EditorGUILayout.IntField(
                    "技能ID",
                    m_InputSkill2Id,
                    GUILayout.Width(200)
                );
                if (GUILayout.Button("加载配置", GUILayout.Width(80)))
                {
                    LoadSkillConfigById(m_InputSkill2Id, m_Skill2Params);
                }
                break;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "输入技能ID后点击加载按钮，从SummonChessSkillTable配置表中读取参数",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制单个技能的参数
    /// </summary>
    private void DrawSkillParams(string title, SkillRuntimeParams skillParams)
    {
        if (skillParams == null)
            return;

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 基础信息
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        skillParams.Name = EditorGUILayout.TextField("技能名称", skillParams.Name);
        skillParams.SkillType = EditorGUILayout.IntField("技能类型", skillParams.SkillType);
        skillParams.Desc = EditorGUILayout.TextField("描述", skillParams.Desc);

        EditorGUILayout.Space();

        // 伤害参数
        EditorGUILayout.LabelField("伤害参数", EditorStyles.boldLabel);
        skillParams.DamageType = EditorGUILayout.IntField("伤害类型", skillParams.DamageType);
        skillParams.DamageCoeff = EditorGUILayout.DoubleField("伤害系数", skillParams.DamageCoeff);
        skillParams.BaseDamage = EditorGUILayout.DoubleField("基础伤害", skillParams.BaseDamage);
        skillParams.HitCount = EditorGUILayout.IntField("触发次数", skillParams.HitCount);

        EditorGUILayout.Space();

        // 命中类型
        EditorGUILayout.LabelField("命中参数", EditorStyles.boldLabel);
        skillParams.EffectHitType = EditorGUILayout.IntField("命中类型", skillParams.EffectHitType);
        skillParams.CastRange = EditorGUILayout.DoubleField("施法范围", skillParams.CastRange);
        skillParams.AreaRadius = EditorGUILayout.DoubleField("AOE半径", skillParams.AreaRadius);
        skillParams.PenetrationCount = EditorGUILayout.IntField(
            "穿透数量",
            skillParams.PenetrationCount
        );

        EditorGUILayout.Space();

        // 投射物参数
        if (skillParams.EffectHitType == 2) // 投射物类型
        {
            EditorGUILayout.LabelField("投射物参数", EditorStyles.boldLabel);
            skillParams.ProjectilePrefab = (GameObject)
                EditorGUILayout.ObjectField(
                    "投射物预制体",
                    skillParams.ProjectilePrefab,
                    typeof(GameObject),
                    false
                );
            skillParams.ProjectileSpeed = EditorGUILayout.DoubleField(
                "投射物速度",
                skillParams.ProjectileSpeed
            );
            EditorGUILayout.Space();
        }

        // 消耗和冷却
        EditorGUILayout.LabelField("消耗和冷却", EditorStyles.boldLabel);
        skillParams.MpCost = EditorGUILayout.DoubleField("法力消耗", skillParams.MpCost);
        skillParams.MpRestore = EditorGUILayout.DoubleField("法力回复", skillParams.MpRestore);
        skillParams.Cooldown = EditorGUILayout.DoubleField("冷却时间", skillParams.Cooldown);
        skillParams.Duration = EditorGUILayout.DoubleField("持续时间", skillParams.Duration);

        EditorGUILayout.Space();

        // Buff参数
        EditorGUILayout.LabelField("Buff参数", EditorStyles.boldLabel);
        skillParams.BuffTriggerType = EditorGUILayout.IntField(
            "Buff触发类型",
            skillParams.BuffTriggerType
        );

        // Buff列表编辑
        DrawBuffList("附加Buff", skillParams.BuffIds);
        DrawBuffList("自身Buff", skillParams.SelfBuffIds);

        EditorGUILayout.Space();

        // 特效参数
        EditorGUILayout.LabelField("特效参数", EditorStyles.boldLabel);
        skillParams.EffectPrefab = (GameObject)
            EditorGUILayout.ObjectField(
                "技能特效",
                skillParams.EffectPrefab,
                typeof(GameObject),
                false
            );
        skillParams.EffectSpawnHeight = EditorGUILayout.FloatField(
            "特效高度",
            skillParams.EffectSpawnHeight
        );
        skillParams.HitEffectPrefab = (GameObject)
            EditorGUILayout.ObjectField(
                "受击特效",
                skillParams.HitEffectPrefab,
                typeof(GameObject),
                false
            );
        skillParams.IconSprite = (GameObject)
            EditorGUILayout.ObjectField(
                "技能图标",
                skillParams.IconSprite,
                typeof(GameObject),
                false
            );
    }

    /// <summary>
    /// 绘制Buff列表编辑器
    /// </summary>
    private void DrawBuffList(string label, List<int> buffIds)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"数量: {buffIds.Count}");
        if (GUILayout.Button("添加", GUILayout.Width(60)))
        {
            buffIds.Add(0);
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < buffIds.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            buffIds[i] = EditorGUILayout.IntField($"Buff[{i}]", buffIds[i]);
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                buffIds.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion

    #region 技能触发逻辑

    /// <summary>
    /// 加载棋子的技能配置
    /// </summary>
    private void LoadChessSkillConfigs()
    {
        if (m_TargetChess == null)
            return;

        // 加载普攻配置
        if (m_TargetChess.NormalAttackConfig != null)
        {
            m_NormalAttackParams.LoadFromConfig(m_TargetChess.NormalAttackConfig);
            m_InputNormalAttackId = m_TargetChess.Config.NormalAtkId;
        }

        // 加载技能1配置
        if (m_TargetChess.Skill1Config != null)
        {
            m_Skill1Params.LoadFromConfig(m_TargetChess.Skill1Config);
            m_InputSkill1Id = m_TargetChess.Config.Skill1Id;
        }

        // 加载大招配置
        if (m_TargetChess.Skill2Config != null)
        {
            m_Skill2Params.LoadFromConfig(m_TargetChess.Skill2Config);
            m_InputSkill2Id = m_TargetChess.Config.Skill2Id;
        }

        Debug.Log($"ChessSkillTestPanel: 已加载棋子 {m_TargetChess.Config?.Name} 的技能配置");
    }

    /// <summary>
    /// 通过技能ID从配置表加载技能配置
    /// </summary>
    private void LoadSkillConfigById(int skillId, SkillRuntimeParams targetParams)
    {
        if (skillId <= 0)
        {
            Debug.LogWarning("ChessSkillTestPanel: 技能ID无效");
            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogWarning("ChessSkillTestPanel: 需要在运行模式下加载配置表");
            return;
        }

        try
        {
            var skillTable = GF.DataTable.GetDataTable<SummonChessSkillTable>();
            if (skillTable == null)
            {
                Debug.LogError("ChessSkillTestPanel: 无法获取SummonChessSkillTable配置表");
                return;
            }

            var skillConfig = skillTable.GetDataRow(skillId);
            if (skillConfig == null)
            {
                Debug.LogWarning($"ChessSkillTestPanel: 未找到技能ID={skillId}的配置");
                return;
            }

            targetParams.LoadFromConfig(skillConfig);
            Debug.Log(
                $"ChessSkillTestPanel: 成功加载技能配置 ID={skillId}, Name={skillConfig.Name}"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChessSkillTestPanel: 加载技能配置失败 - {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 触发普攻
    /// </summary>
    private void TriggerNormalAttack()
    {
        if (m_TargetChess == null || m_TargetChess.CombatController == null)
        {
            Debug.LogWarning("ChessSkillTestPanel: 目标棋子或战斗控制器为空");
            return;
        }

        // 如果有索敌目标，使用目标触发攻击
        if (m_EnemyTarget != null)
        {
            m_TargetChess.CombatController.TriggerAttackFromAI(m_EnemyTarget);
            Debug.Log(
                $"ChessSkillTestPanel: {m_TargetChess.Config?.Name} 对 {m_EnemyTarget.Config?.Name} 触发普攻"
            );
        }
        else
        {
            // 没有目标时，尝试查找最近的敌人
            ChessEntity nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                m_TargetChess.CombatController.TriggerAttackFromAI(nearestEnemy);
                Debug.Log(
                    $"ChessSkillTestPanel: {m_TargetChess.Config?.Name} 对最近敌人 {nearestEnemy.Config?.Name} 触发普攻"
                );
            }
            else
            {
                Debug.LogWarning("ChessSkillTestPanel: 未找到攻击目标");
            }
        }
    }

    /// <summary>
    /// 触发技能1
    /// </summary>
    private void TriggerSkill1()
    {
        if (m_TargetChess == null || m_TargetChess.CombatController == null)
        {
            Debug.LogWarning("ChessSkillTestPanel: 目标棋子或战斗控制器为空");
            return;
        }

        m_TargetChess.CombatController.TriggerSkill1FromAI();
        Debug.Log($"ChessSkillTestPanel: {m_TargetChess.Config?.Name} 触发技能1");
    }

    /// <summary>
    /// 触发大招
    /// </summary>
    private void TriggerSkill2()
    {
        if (m_TargetChess == null || m_TargetChess.CombatController == null)
        {
            Debug.LogWarning("ChessSkillTestPanel: 目标棋子或战斗控制器为空");
            return;
        }

        m_TargetChess.CombatController.TriggerSkill2FromAI();
        Debug.Log($"ChessSkillTestPanel: {m_TargetChess.Config?.Name} 触发大招");
    }

    /// <summary>
    /// 查找最近的敌人
    /// </summary>
    private ChessEntity FindNearestEnemy()
    {
        if (m_TargetChess == null)
            return null;

        ChessEntity[] allChess = GameObject.FindObjectsOfType<ChessEntity>();
        ChessEntity nearestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var chess in allChess)
        {
            // 跳过自己和同阵营的棋子
            if (chess == m_TargetChess || chess.Camp == m_TargetChess.Camp)
                continue;

            // 跳过已死亡的棋子
            if (chess.CurrentState == ChessState.Dead)
                continue;

            float distance = Vector3.Distance(
                m_TargetChess.transform.position,
                chess.transform.position
            );
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = chess;
            }
        }

        return nearestEnemy;
    }

    #endregion
}
