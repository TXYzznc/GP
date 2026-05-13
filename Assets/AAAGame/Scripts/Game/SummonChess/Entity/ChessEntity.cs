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

    /// <summary>棋子等级（1-3）</summary>
    public int Rank { get; private set; }

    /// <summary>设置棋子等级（仅用于战斗中恢复等级）</summary>
    public void SetRank(int rank)
    {
        if (rank >= 1 && rank <= 3)
        {
            Rank = rank;
        }
    }

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
            DebugEx.Error(nameof(ChessEntity), "Initialize: config is null");
            return;
        }

        ChessId = chessId;
        Config = config;
        Camp = camp;
        InstanceId = GetInstanceID();

        // 从全局状态读取等级和经验值
        // 注意：玩家棋子在 GlobalChessManager 中管理，敌方棋子可能尚未注册（由 BattleChessManager.RegisterChessEntity 处理）
        var globalState = GlobalChessManager.Instance?.GetChessState(config.Id);
        if (globalState != null)
        {
            Rank = globalState.Level;
            DebugEx.Log("ChessEntity", $"✅ 棋子 {config.Name} 从全局状态加载等级: {Rank}");
        }
        else
        {
            // 敌方棋子或未注册的棋子，使用配置默认值
            Rank = 1;
            DebugEx.Log("ChessEntity", $"⚠️ 棋子 {config.Name} 无全局记录，使用默认等级 1");
        }

        DebugEx.Log(
            nameof(ChessEntity),
            $"Initialize: 开始初始化棋子 [{config.Name}] (Id={chessId}, Camp={camp}, Rank={Rank})"
        );

        // 1. 初始化属性组件
        Attribute = gameObject.GetComponent<ChessAttribute>();
        if (Attribute == null)
        {
            Attribute = gameObject.AddComponent<ChessAttribute>();
        }
        Attribute.Initialize(this, config, Rank);

        // 1.3 初始化经验组件（如果不存在则创建）
        var expComp = gameObject.GetComponent<ChessEXPComponent>();
        if (expComp == null)
        {
            expComp = gameObject.AddComponent<ChessEXPComponent>();
            DebugEx.Log("ChessEntity", $"✅ 为棋子 {config.Name} 创建 ChessEXPComponent");
        }

        // 从全局状态读取经验值（保留跨战斗的经验数据）
        int initialExp = 0;
        if (globalState != null)
        {
            initialExp = globalState.Experience;
            DebugEx.Log("ChessEntity", $"✅ 棋子 {config.Name} 从全局状态加载经验值: {initialExp}");
        }
        expComp.SetEXP(initialExp);

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
        var passiveIds = config.GetPassiveIds();
        for (int i = 0; i < passiveIds.Length; i++)
        {
            int passiveId = passiveIds[i];
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
                    DebugEx.Log(nameof(ChessEntity), $"被动初始化成功 (Id={passiveId})");
                }
                else
                {
                    DebugEx.Warning(
                        nameof(ChessEntity),
                        $"{config.Name} 被动配置 ID={passiveId} 不存在"
                    );
                }
            }
        }

        // 6. 初始化普攻效果
        int normalAtkId = config.GetNormalAtkId(Rank);
        if (normalAtkId != 0)
        {
            NormalAttackConfig = skillTable?.GetDataRow(normalAtkId);
            if (NormalAttackConfig != null)
            {
                NormalAttack = ChessFactory.CreateNormalAttack(normalAtkId);
                if (NormalAttack != null)
                {
                    NormalAttack.Init(m_Context, NormalAttackConfig);
                    DebugEx.Log(
                        nameof(ChessEntity),
                        $"普攻效果初始化成功 (Id={normalAtkId}, "
                            + $"EffectId={NormalAttackConfig.EffectId}, HitEffectId={NormalAttackConfig.HitEffectId})"
                    );
                }
            }
            else
            {
                DebugEx.Warning(
                    nameof(ChessEntity),
                    $"{config.Name} 普攻配置 ID={normalAtkId} 不存在"
                );
            }
        }

        // 7. 初始化技能一
        int skill1Id = config.GetSkill1Id(Rank);
        if (skill1Id != 0)
        {
            Skill1Config = skillTable?.GetDataRow(skill1Id);
            if (Skill1Config != null)
            {
                Skill1 = ChessFactory.CreateSkill(skill1Id);
                if (Skill1 != null)
                {
                    Skill1.Init(m_Context, Skill1Config);
                    DebugEx.Log(nameof(ChessEntity), $"技能一初始化成功 (Id={skill1Id})");
                }
            }
            else
            {
                DebugEx.Warning(
                    nameof(ChessEntity),
                    $"{config.Name} 技能1配置 ID={skill1Id} 不存在"
                );
            }
        }

        // 8. 初始化大招
        int ultimateId = config.GetUltimateId(Rank);
        if (ultimateId != 0)
        {
            Skill2Config = skillTable?.GetDataRow(ultimateId);
            if (Skill2Config != null)
            {
                Skill2 = ChessFactory.CreateSkill(ultimateId);
                if (Skill2 != null)
                {
                    Skill2.Init(m_Context, Skill2Config);
                    DebugEx.Log(nameof(ChessEntity), $"大招初始化成功 (Id={ultimateId})");
                }
            }
            else
            {
                DebugEx.Warning(
                    nameof(ChessEntity),
                    $"{config.Name} 大招配置 ID={ultimateId} 不存在"
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
        DebugEx.Log(nameof(ChessEntity), $"动画控制器初始化完成: {config.Name}");

        // 11. ⭐ 再初始化战斗控制器（依赖 Animator.EventReceiver）
        CombatController = gameObject.GetComponent<ChessCombatController>();
        if (CombatController == null)
        {
            CombatController = gameObject.AddComponent<ChessCombatController>();
        }
        CombatController.Initialize(this, m_Context);
        DebugEx.Log(nameof(ChessEntity), $"战斗控制器初始化完成: {config.Name}");

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

        DebugEx.Log(nameof(ChessEntity), $"Initialize: 棋子初始化完成 [{config.Name}]");

        // 注册到战斗棋子管理器（如果在战斗准备阶段，管理器可能还未创建，这是正常的）
        try
        {
            if (CombatEntityTracker.Instance != null)
            {
                CombatEntityTracker.Instance.RegisterChess(this);
                DebugEx.Log(nameof(ChessEntity), $"{Config.Name} 已注册到 CombatEntityTracker");
            }
            else
            {
                // 战斗准备阶段棋子会在进入战斗状态时自动注册，这里不需要警告
                DebugEx.Log(
                    nameof(ChessEntity),
                    $"{Config.Name} 初始化完成，等待进入战斗状态后注册"
                );
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.Warning(
                nameof(ChessEntity),
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

        DebugEx.Log(nameof(ChessEntity),
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

    // ── 行为约束聚合属性（第一层：影响整体行动） ──────────────────────
    /// <summary>是否完全无法行动（Stun / Freeze）</summary>
    public bool IsIncapacitated => HasSpecialState("Stun") || HasSpecialState("Freeze");

    // ── AI 约束聚合属性（第二层：影响 AI 决策） ────────────────────────
    /// <summary>是否被沉默（无法使用技能）</summary>
    public bool IsSilenced => HasSpecialState("Silence");

    /// <summary>是否被嘲讽（强制索敌来源）</summary>
    public bool IsTaunted => HasSpecialState("Taunt");

    private void Update()
    {
        if (CurrentState == ChessState.Dead)
        {
            return;
        }

        float dt = Time.deltaTime;

        if (IsIncapacitated)
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

        DebugEx.Log(nameof(ChessEntity), $"{Config?.Name} 已销毁");
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

        DebugEx.Log(nameof(ChessEntity), $"状态改变 [{Config?.Name}] {oldState} -> {newState}");

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
    /// 生命值变化事件
    /// 参数：(当前生命值, 最大生命值)
    /// 用于多阶段Boss等特殊逻辑
    /// </summary>
    public event Action<double, double> OnHealthChanged;

    /// <summary>
    /// 生命值变化处理
    /// </summary>
    private void OnHpChangedHandler(double oldValue, double newValue)
    {
        // 触发 OnHealthChanged 事件
        double maxHp = Attribute != null ? Attribute.MaxHp : 1;
        OnHealthChanged?.Invoke(newValue, maxHp);

        // 检查是否死亡
        if (newValue <= 0 && oldValue > 0)
        {
            ChangeState(ChessState.Dead);
            DebugEx.Log(nameof(ChessEntity), $"棋子死亡 [{Config?.Name}]");

            ChessStateEvents.FireChessDied(this);

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
        DebugEx.Log(nameof(ChessEntity), "=== ChessEntity 信息 ===");
        DebugEx.Log(nameof(ChessEntity), $"名称: {Config?.Name}");
        DebugEx.Log(nameof(ChessEntity), $"ID: {ChessId}");
        DebugEx.Log(nameof(ChessEntity), $"实例ID: {InstanceId}");
        DebugEx.Log(nameof(ChessEntity), $"阵营: {Camp}");
        DebugEx.Log(nameof(ChessEntity), $"状态: {CurrentState}");
        DebugEx.Log(nameof(ChessEntity), $"生命值: {Attribute?.CurrentHp}/{Attribute?.MaxHp}");
        DebugEx.Log(nameof(ChessEntity), $"法力值: {Attribute?.CurrentMp}/{Attribute?.MaxMp}");
        DebugEx.Log(nameof(ChessEntity), $"AI类型: {Config?.AIType}");
        DebugEx.Log(nameof(ChessEntity), $"种族: {string.Join(", ", GetRaces())}");
        DebugEx.Log(nameof(ChessEntity), $"职业: {string.Join(", ", GetClasses())}");
        DebugEx.Log(nameof(ChessEntity), "========================");
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
            DebugEx.Log(
                nameof(ChessEntity),
                $"{Config?.Name} 从Collider获取高度: {m_ModelHeight:F2}m"
            );
            return m_ModelHeight;
        }

        // 尝试从 Renderer 获取高度
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            m_ModelHeight = renderer.bounds.size.y;
            DebugEx.Log(
                nameof(ChessEntity),
                $"{Config?.Name} 从Renderer获取高度: {m_ModelHeight:F2}m"
            );
            return m_ModelHeight;
        }

        // 默认高度
        m_ModelHeight = 2f;
        DebugEx.Warning(
            nameof(ChessEntity),
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

        DebugEx.Log(
            nameof(ChessEntity),
            $"{Config?.Name} 特效位置计算: 归一化高度={normalizedHeight:F2}, 最终Y={position.y:F2}"
        );

        return position;
    }

    #region 升阶系统

    /// <summary>升阶事件（参数为升阶前的阶级）</summary>
    public event Action<int> OnRankAdvanced;

    /// <summary>
    /// 升阶（通过 GlobalChessManager 处理经验和等级）
    /// </summary>
    public void AdvanceRank()
    {
        if (Rank >= 3)
        {
            DebugEx.Warning(nameof(ChessEntity), $"棋子 {Config?.Name} 已是最高阶（{Rank}），不能继续升阶");
            return;
        }

        int oldRank = Rank;

        // 通过 GlobalChessManager 处理升阶（自动消耗经验、提升等级）
        bool advanceSuccess = GlobalChessManager.Instance.AdvanceChessRank(ChessId);
        if (!advanceSuccess)
        {
            DebugEx.Warning(nameof(ChessEntity), $"棋子 {Config?.Name} 升阶失败（经验不足或等级已满）");
            return;
        }

        // 获取全局状态，同步本地 Rank
        var globalState = GlobalChessManager.Instance.GetChessState(ChessId);
        if (globalState == null)
        {
            DebugEx.Error(nameof(ChessEntity), $"棋子 {ChessId} 升阶后无法同步全局状态");
            return;
        }

        Rank = globalState.Level;

        // 同步本地经验值到 ChessEXPComponent
        var expComp = GetComponent<ChessEXPComponent>();
        if (expComp != null)
        {
            expComp.SetEXP(globalState.Experience);
        }

        // 更新属性（根据新等级）
        if (Attribute != null)
        {
            Attribute.Initialize(this, Config, Rank);
        }

        // 更新技能配置
        var skillTable = GF.DataTable.GetDataTable<SummonChessSkillTable>();

        // 更新普攻
        int normalAtkId = Config.GetNormalAtkId(Rank);
        if (normalAtkId != 0)
        {
            NormalAttackConfig = skillTable?.GetDataRow(normalAtkId);
            if (NormalAttackConfig != null)
            {
                NormalAttack = ChessFactory.CreateNormalAttack(normalAtkId);
                if (NormalAttack != null)
                {
                    NormalAttack.Init(m_Context, NormalAttackConfig);
                }
            }
        }

        // 更新技能1
        int skill1Id = Config.GetSkill1Id(Rank);
        if (skill1Id != 0)
        {
            Skill1Config = skillTable?.GetDataRow(skill1Id);
            if (Skill1Config != null)
            {
                Skill1 = ChessFactory.CreateSkill(skill1Id);
                if (Skill1 != null)
                {
                    Skill1.Init(m_Context, Skill1Config);
                }
            }
        }

        // 更新大招
        int ultimateId = Config.GetUltimateId(Rank);
        if (ultimateId != 0)
        {
            Skill2Config = skillTable?.GetDataRow(ultimateId);
            if (Skill2Config != null)
            {
                Skill2 = ChessFactory.CreateSkill(ultimateId);
                if (Skill2 != null)
                {
                    Skill2.Init(m_Context, Skill2Config);
                }
            }
        }

        DebugEx.Log(nameof(ChessEntity), $"棋子 {Config?.Name} 升阶成功：{oldRank} → {Rank}");
        OnRankAdvanced?.Invoke(oldRank);
    }

    #endregion

    #region 技能替换（为多阶段Boss提供支持）

    /// <summary>
    /// 替换普攻技能
    /// </summary>
    public void ReplaceNormalAttack(IChessNormalAttack newAttack)
    {
        NormalAttack = newAttack;
    }

    /// <summary>
    /// 替换技能一
    /// </summary>
    public void ReplaceSkill1(IChessSkill newSkill)
    {
        Skill1 = newSkill;
    }

    /// <summary>
    /// 替换技能二/大招
    /// </summary>
    public void ReplaceSkill2(IChessSkill newSkill)
    {
        Skill2 = newSkill;
    }

    #endregion

    #endregion
}
