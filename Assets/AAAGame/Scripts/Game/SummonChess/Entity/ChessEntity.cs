using System;
using UnityEngine;

/// <summary>
/// 棋子实体类
/// 运行时的棋子实例，整合所有组件
/// </summary>
public class ChessEntity : MonoBehaviour
{
    #region 配置数据

    /// <summary>棋子ID</summary>
    public int ChessId { get; private set; }

    /// <summary>棋子配置</summary>
    public SummonChessConfig Config { get; private set; }

    /// <summary>普攻技能配置</summary>
    public SummonChessSkillTable NormalAttackConfig { get; private set; }

    /// <summary>技能1配置</summary>
    public SummonChessSkillTable Skill1Config { get; private set; }

    /// <summary>技能2配置</summary>
    public SummonChessSkillTable Skill2Config { get; private set; }

    #endregion

    #region 组件引用

    /// <summary>属性组件</summary>
    public ChessAttribute Attribute { get; private set; }

    /// <summary>动画控制器</summary>
    public ChessAnimator Animator { get; private set; }

    /// <summary>测试输入组件（仅开发用）</summary>
    public ChessTestInput TestInput { get; private set; }

    /// <summary>被动技能列表</summary>
    public System.Collections.Generic.List<IChessPassive> Passives { get; private set; }

    /// <summary>普攻效果</summary>
    public IChessNormalAttack NormalAttack { get; private set; }

    /// <summary>技能一</summary>
    public IChessSkill Skill1 { get; private set; }

    /// <summary>技能二/大招</summary>
    public IChessSkill Skill2 { get; private set; }

    /// <summary>Buff管理组件</summary>
    public BuffManager BuffManager { get; private set; }

    /// <summary>AI组件</summary>
    public IChessAI AI { get; private set; }

    /// <summary>移动组件</summary>
    public IChessMovement Movement { get; private set; }

    /// <summary>战斗控制器</summary>
    public ChessCombatController CombatController { get; private set; }

    /// <summary>描边控制器</summary>
    public OutlineController OutlineController { get; private set; }

    #endregion

    #region 运行时数据

    /// <summary>实例ID</summary>
    public int InstanceId { get; private set; }

    /// <summary>阵营（0=玩家，1=敌人）</summary>
    public int Camp { get; set; }

    /// <summary>当前状态</summary>
    public ChessState CurrentState { get; private set; }

    /// <summary>上下文</summary>
    private ChessContext m_Context;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化棋子
    /// 由SummonChessManager调用
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="config">棋子配置</param>
    /// <param name="camp">阵营</param>
    public void Initialize(int chessId, SummonChessConfig config, int camp)
    {
        if (config == null)
        {
            DebugEx.ErrorModule("ChessEntity", "Initialize: config is null");
            return;
        }

        ChessId = chessId;
        Config = config;
        Camp = camp;
        InstanceId = GetInstanceID();

        DebugEx.LogModule(
            "ChessEntity",
            $"Initialize: 开始初始化棋子 [{config.Name}] (Id={chessId}, Camp={camp})"
        );

        // 1. 初始化属性组件
        Attribute = gameObject.GetComponent<ChessAttribute>();
        if (Attribute == null)
        {
            Attribute = gameObject.AddComponent<ChessAttribute>();
        }
        Attribute.Initialize(this, config);

        // 1.5 初始化Buff管理组件（清理可能残留的Buff数据）
        BuffManager = gameObject.GetComponent<BuffManager>();
        if (BuffManager == null)
        {
            BuffManager = gameObject.AddComponent<BuffManager>();
        }
        // ⭐ 重要：清理可能从Prefab残留的Buff数据（防止跨战斗污染）
        BuffManager.ClearAll();

        // 2. 创建上下文
        m_Context = new ChessContext
        {
            Owner = gameObject,
            Transform = transform,
            Attribute = Attribute,
            Entity = this,
            BuffManager = BuffManager,
            Camp = camp,
            Config = config,
        };

        // 3. 创建AI（使用工厂）
        AI = ChessFactory.CreateAI(config.AIType);
        if (AI != null)
        {
            AI.Init(m_Context);
        }

        // 4. 获取技能配置表
        var skillTable = GF.DataTable.GetDataTable<SummonChessSkillTable>();

        // 5. 初始化被动技能
        Passives = new System.Collections.Generic.List<IChessPassive>();
        if (config.PassiveIds != null)
        {
            for (int i = 0; i < config.PassiveIds.Length; i++)
            {
                int passiveId = config.PassiveIds[i];
                if (passiveId == 0)
                    continue;

                var passive = ChessFactory.CreatePassive(passiveId);
                if (passive != null)
                {
                    var skillConfig = skillTable?.GetDataRow(passiveId);
                    if (skillConfig != null)
                    {
                        passive.Init(m_Context, skillConfig);
                        Passives.Add(passive);
                        DebugEx.LogModule("ChessEntity", $"被动初始化成功 (Id={passiveId})");
                    }
                    else
                    {
                        DebugEx.WarningModule(
                            "ChessEntity",
                            $"{config.Name} 被动配置 ID={passiveId} 不存在"
                        );
                    }
                }
            }
        }

        // 6. 初始化普攻效果
        if (config.NormalAtkId != 0)
        {
            NormalAttackConfig = skillTable?.GetDataRow(config.NormalAtkId);
            if (NormalAttackConfig != null)
            {
                NormalAttack = ChessFactory.CreateNormalAttack(config.NormalAtkId);
                if (NormalAttack != null)
                {
                    NormalAttack.Init(m_Context, NormalAttackConfig);
                    DebugEx.LogModule(
                        "ChessEntity",
                        $"普攻效果初始化成功 (Id={config.NormalAtkId}, "
                            + $"EffectId={NormalAttackConfig.EffectId}, HitEffectId={NormalAttackConfig.HitEffectId})"
                    );
                }
            }
            else
            {
                DebugEx.WarningModule(
                    "ChessEntity",
                    $"{config.Name} 普攻配置 ID={config.NormalAtkId} 不存在"
                );
            }
        }

        // 7. 初始化技能一
        if (config.Skill1Id != 0)
        {
            Skill1Config = skillTable?.GetDataRow(config.Skill1Id);
            if (Skill1Config != null)
            {
                Skill1 = ChessFactory.CreateSkill(config.Skill1Id);
                if (Skill1 != null)
                {
                    Skill1.Init(m_Context, Skill1Config);
                    DebugEx.LogModule("ChessEntity", $"技能一初始化成功 (Id={config.Skill1Id})");
                }
            }
            else
            {
                DebugEx.WarningModule(
                    "ChessEntity",
                    $"{config.Name} 技能1配置 ID={config.Skill1Id} 不存在"
                );
            }
        }

        // 8. 初始化技能二/大招
        if (config.Skill2Id != 0)
        {
            Skill2Config = skillTable?.GetDataRow(config.Skill2Id);
            if (Skill2Config != null)
            {
                Skill2 = ChessFactory.CreateSkill(config.Skill2Id);
                if (Skill2 != null)
                {
                    Skill2.Init(m_Context, Skill2Config);
                    DebugEx.LogModule("ChessEntity", $"大招初始化成功 (Id={config.Skill2Id})");
                }
            }
            else
            {
                DebugEx.WarningModule(
                    "ChessEntity",
                    $"{config.Name} 技能2配置 ID={config.Skill2Id} 不存在"
                );
            }
        }

        // 9. 初始化移动组件
        var movement = gameObject.GetComponent<SimpleChessMovement>();
        if (movement == null)
        {
            movement = gameObject.AddComponent<SimpleChessMovement>();
        }
        movement.MoveSpeed = (float)config.MoveSpeed;
        Movement = movement;

        // 10. ⭐ 先初始化动画控制器（CombatController 需要订阅动画事件）
        Animator = gameObject.GetComponent<ChessAnimator>();
        if (Animator == null)
        {
            Animator = gameObject.AddComponent<ChessAnimator>();
        }
        Animator.Initialize(this);
        DebugEx.LogModule("ChessEntity", $"动画控制器初始化完成: {config.Name}");

        // 11. ⭐ 再初始化战斗控制器（依赖 Animator.EventReceiver）
        CombatController = gameObject.GetComponent<ChessCombatController>();
        if (CombatController == null)
        {
            CombatController = gameObject.AddComponent<ChessCombatController>();
        }
        CombatController.Initialize(this, m_Context);
        DebugEx.LogModule("ChessEntity", $"战斗控制器初始化完成: {config.Name}");

        // 12. 初始化测试输入组件（仅开发用）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        TestInput = gameObject.GetComponent<ChessTestInput>();
        if (TestInput == null)
        {
            TestInput = gameObject.AddComponent<ChessTestInput>();
        }
        TestInput.Initialize(this, Animator);
#endif

        // 13. 初始化描边控制器
        OutlineController = gameObject.GetComponent<OutlineController>();
        if (OutlineController == null)
        {
            OutlineController = gameObject.AddComponent<OutlineController>();
        }

        // 14. 注册属性事件
        Attribute.OnHpChanged += OnHpChangedHandler;
        Attribute.OnMpChanged += OnMpChangedHandler;

        DebugEx.LogModule("ChessEntity", $"Initialize: 棋子初始化完成 [{config.Name}]");

        // 注册到战斗棋子管理器（如果在战斗准备阶段，管理器可能还未创建，这是正常的）
        try
        {
            if (CombatEntityTracker.Instance != null)
            {
                CombatEntityTracker.Instance.RegisterChess(this);
                DebugEx.LogModule("ChessEntity", $"{Config.Name} 已注册到 CombatEntityTracker");
            }
            else
            {
                // 战斗准备阶段棋子会在进入战斗状态时自动注册，这里不需要警告
                DebugEx.LogModule(
                    "ChessEntity",
                    $"{Config.Name} 初始化完成，等待进入战斗状态后注册"
                );
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.WarningModule(
                "ChessEntity",
                $"{Config.Name} 注册到 CombatEntityTracker 时发生异常: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// 作为召唤师战斗实体初始化（轻量级，无 AI / 移动 / 动画 / 战斗控制器）。
    /// 防御属性从 SummonChessTable 配置行读取，HP 由外部覆盖为 SummonerTable.BaseHP。
    /// 由 CombatManager.StartCombat() 动态调用。
    /// </summary>
    /// <param name="chessId">SummonerTable.SummonChessId</param>
    /// <param name="config">SummonChessTable 中的召唤师配置行</param>
    /// <param name="camp">阵营（0=玩家方）</param>
    public void InitializeAsSummoner(int chessId, SummonChessConfig config, int camp)
    {
        ChessId = chessId;
        Config = config; // 真实配置，Config?.Name 等字段可正常访问
        Camp = camp;
        InstanceId = GetInstanceID();

        // 属性组件（由外部 AddComponent 后调用，这里直接获取）
        Attribute = gameObject.GetComponent<ChessAttribute>();
        BuffManager = gameObject.GetComponent<BuffManager>();

        // 初始化属性（HP 覆盖为 SummonerRuntimeDataManager.MaxHP）
        if (Attribute != null)
        {
            float maxHp = SummonerRuntimeDataManager.Instance?.MaxHP ?? 100f;
            Attribute.InitializeAsSummoner(this, config, maxHp);
            Attribute.OnHpChanged += OnHpChangedHandler;
        }

        // 最小化上下文（Buff 系统依赖，AI / 技能不需要）
        m_Context = new ChessContext
        {
            Owner = gameObject,
            Transform = transform,
            Attribute = Attribute,
            Entity = this,
            BuffManager = BuffManager,
            Camp = camp,
            Config = config,
        };

        DebugEx.LogModule("ChessEntity",
            $"InitializeAsSummoner 完成: [{config?.Name ?? "召唤师"}] ChessId={chessId}, Camp={camp}");
    }

    #endregion

    #region Unity生命周期

    private readonly System.Collections.Generic.Dictionary<string, int> m_SpecialStateCounts = new System.Collections.Generic.Dictionary<string, int>();

    public void AddSpecialState(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (m_SpecialStateCounts.TryGetValue(key, out int count))
        {
            m_SpecialStateCounts[key] = count + 1;
        }
        else
        {
            m_SpecialStateCounts[key] = 1;
        }
    }

    public void RemoveSpecialState(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!m_SpecialStateCounts.TryGetValue(key, out int count))
        {
            return;
        }

        count--;
        if (count <= 0)
        {
            m_SpecialStateCounts.Remove(key);
            return;
        }

        m_SpecialStateCounts[key] = count;
    }

    public bool HasSpecialState(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        return m_SpecialStateCounts.TryGetValue(key, out int count) && count > 0;
    }

    private void Update()
    {
        if (CurrentState == ChessState.Dead)
        {
            return;
        }

        float dt = Time.deltaTime;

        if (HasSpecialState("Stun"))
        {
            if (Passives != null)
            {
                for (int i = 0; i < Passives.Count; i++)
                {
                    Passives[i].Tick(dt);
                }
            }

            return;
        }

        // 更新AI
        AI?.Tick(dt);

        // 更新战斗控制器
        CombatController?.Tick(dt);

        // 更新被动技能
        if (Passives != null)
        {
            for (int i = 0; i < Passives.Count; i++)
            {
                Passives[i].Tick(dt);
            }
        }

        // 更新技能冷却
        Skill1?.Tick(dt);
        Skill2?.Tick(dt);

        // 更新移动
        Movement?.Tick(dt);
    }

    private void OnDestroy()
    {
        // 清理被动技能
        if (Passives != null)
        {
            for (int i = 0; i < Passives.Count; i++)
            {
                Passives[i].Dispose();
            }
            Passives.Clear();
        }

        // 清理Buff
        if (BuffManager != null)
        {
            BuffManager.ClearAll();
        }

        // 清理事件订阅
        if (Attribute != null)
        {
            Attribute.OnHpChanged -= OnHpChangedHandler;
            Attribute.OnMpChanged -= OnMpChangedHandler;
        }

        OnStateChanged = null;

        // ⭐ 从棋子管理器注销
        if (CombatEntityTracker.Instance != null)
        {
            CombatEntityTracker.Instance.UnregisterChess(this);
        }

        DebugEx.LogModule("ChessEntity", $"{Config?.Name} 已销毁");
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 检查是否拥有指定种族
    /// </summary>
    /// <param name="raceId">种族ID</param>
    /// <returns>是否拥有该种族</returns>
    public bool HasRace(int raceId)
    {
        if (Config == null || Config.Races == null)
        {
            return false;
        }

        for (int i = 0; i < Config.Races.Length; i++)
        {
            if (Config.Races[i] == raceId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查是否拥有指定职业
    /// </summary>
    /// <param name="classId">职业ID</param>
    /// <returns>是否拥有该职业</returns>
    public bool HasClass(int classId)
    {
        if (Config == null || Config.Classes == null)
        {
            return false;
        }

        for (int i = 0; i < Config.Classes.Length; i++)
        {
            if (Config.Classes[i] == classId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取所有种族ID
    /// </summary>
    /// <returns>种族ID数组</returns>
    public int[] GetRaces()
    {
        return Config?.Races ?? Array.Empty<int>();
    }

    /// <summary>
    /// 获取所有职业ID
    /// </summary>
    /// <returns>职业ID数组</returns>
    public int[] GetClasses()
    {
        return Config?.Classes ?? Array.Empty<int>();
    }

    #endregion

    #region 状态管理

    /// <summary>
    /// 改变状态
    /// </summary>
    /// <param name="newState">新状态</param>
    public void ChangeState(ChessState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        ChessState oldState = CurrentState;
        CurrentState = newState;

        DebugEx.LogModule("ChessEntity", $"状态改变 [{Config?.Name}] {oldState} -> {newState}");

        // 触发状态变化事件
        OnStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// 状态变化事件
    /// 参数：(旧状态, 新状态)
    /// </summary>
    public event Action<ChessState, ChessState> OnStateChanged;

    #endregion

    #region 事件处理

    /// <summary>
    /// 法力值满事件（AI可监听此事件来决定技能释放策略）
    /// </summary>
    public event Action OnMpFull;

    /// <summary>
    /// 生命值变化处理
    /// </summary>
    private void OnHpChangedHandler(double oldValue, double newValue)
    {
        // 检查是否死亡
        if (newValue <= 0 && oldValue > 0)
        {
            ChangeState(ChessState.Dead);
            DebugEx.LogModule("ChessEntity", $"棋子死亡 [{Config?.Name}]");

            // ✅ 通知AI切换到死亡状态
            if (AI is ChessAIBase aiBase)
            {
                aiBase.ForceDead();
            }
        }
    }

    /// <summary>
    /// 法力值变化处理
    /// 注意：技能释放逻辑由AI控制，这里只做事件通知
    /// </summary>
    private void OnMpChangedHandler(double oldValue, double newValue)
    {
        // 法力值满时触发事件，由AI决定是否释放技能
        if (newValue >= Attribute.MaxMp && oldValue < Attribute.MaxMp)
        {
            OnMpFull?.Invoke();
        }
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印棋子信息（调试用）
    /// </summary>
    public void DebugPrintInfo()
    {
        DebugEx.LogModule("ChessEntity", "=== ChessEntity 信息 ===");
        DebugEx.LogModule("ChessEntity", $"名称: {Config?.Name}");
        DebugEx.LogModule("ChessEntity", $"ID: {ChessId}");
        DebugEx.LogModule("ChessEntity", $"实例ID: {InstanceId}");
        DebugEx.LogModule("ChessEntity", $"阵营: {Camp}");
        DebugEx.LogModule("ChessEntity", $"状态: {CurrentState}");
        DebugEx.LogModule("ChessEntity", $"生命值: {Attribute?.CurrentHp}/{Attribute?.MaxHp}");
        DebugEx.LogModule("ChessEntity", $"法力值: {Attribute?.CurrentMp}/{Attribute?.MaxMp}");
        DebugEx.LogModule("ChessEntity", $"AI类型: {Config?.AIType}");
        DebugEx.LogModule("ChessEntity", $"种族: {string.Join(", ", GetRaces())}");
        DebugEx.LogModule("ChessEntity", $"职业: {string.Join(", ", GetClasses())}");
        DebugEx.LogModule("ChessEntity", "========================");
    }

    #endregion

    #region 特效辅助方法

    /// <summary>棋子模型高度缓存</summary>
    private float m_ModelHeight = -1f;

    /// <summary>
    /// 获取棋子模型高度
    /// </summary>
    /// <returns>模型高度（米）</returns>
    public float GetModelHeight()
    {
        // 使用缓存避免重复计算
        if (m_ModelHeight > 0)
        {
            return m_ModelHeight;
        }

        // 尝试从 Collider 获取高度
        var collider = GetComponentInChildren<Collider>();
        if (collider != null)
        {
            m_ModelHeight = collider.bounds.size.y;
            DebugEx.LogModule(
                "ChessEntity",
                $"{Config?.Name} 从Collider获取高度: {m_ModelHeight:F2}m"
            );
            return m_ModelHeight;
        }

        // 尝试从 Renderer 获取高度
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            m_ModelHeight = renderer.bounds.size.y;
            DebugEx.LogModule(
                "ChessEntity",
                $"{Config?.Name} 从Renderer获取高度: {m_ModelHeight:F2}m"
            );
            return m_ModelHeight;
        }

        // 默认高度
        m_ModelHeight = 2f;
        DebugEx.WarningModule(
            "ChessEntity",
            $"{Config?.Name} 无法获取模型高度，使用默认值: {m_ModelHeight}m"
        );

        return m_ModelHeight;
    }

    /// <summary>
    /// 根据配置计算特效生成位置
    /// </summary>
    /// <param name="normalizedHeight">归一化高度（0=模型底部，1=模型顶部）</param>
    /// <returns>世界坐标位置</returns>
    public Vector3 GetEffectSpawnPosition(float normalizedHeight)
    {
        // ⭐ 使用工具类按比例获取位置
        Vector3 position = EntityPositionHelper.GetPositionAtRatio(gameObject, normalizedHeight);

        DebugEx.LogModule(
            "ChessEntity",
            $"{Config?.Name} 特效位置计算: 归一化高度={normalizedHeight:F2}, 最终Y={position.y:F2}"
        );

        return position;
    }

    #endregion
}
