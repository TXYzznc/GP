# 设计文档：自走棋棋子系统

## 概述

本文档描述自走棋棋子系统的技术设计。系统采用数据驱动架构，通过配置表管理棋子数据，使用组件化设计实现棋子功能的模块化和可扩展性。

系统核心包括：
- 配置数据层：从配置表加载和管理棋子数据
- 实体层：运行时棋子实例及其组件
- 管理层：棋子生命周期管理和查询
- AI层：棋子战斗行为控制

## 架构设计

### 设计原则

参考项目现有的 `PlayerSkill` 系统架构，棋子系统采用以下设计原则：

1. **接口驱动**：使用接口定义核心行为（IChessAI、IChessSkill）
2. **工厂模式**：使用工厂类创建AI和技能实例，避免反射
3. **上下文传递**：使用Context对象传递棋子运行时数据
4. **ScriptableObject配置**：使用SO存储可配置参数
5. **数据驱动**：配置表与代码分离，支持热更新

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                   SummonChessManager                     │
│  - 棋子生成/销毁（类似PlayerSkillManager）                │
│  - 资源管理                                               │
│  - 查询接口                                               │
│  - 更新所有棋子的Tick                                     │
└────────────┬────────────────────────────────────────────┘
             │
             ├──────────────┬──────────────┬──────────────┐
             │              │              │              │
      ┌──────▼─────┐ ┌─────▼──────┐ ┌────▼─────┐ ┌─────▼──────┐
      │ ChessData  │ │ChessEntity │ │ ChessAI  │ │ChessUnlock │
      │  Manager   │ │            │ │ Factory  │ │  Manager   │
      │(配置加载)  │ │(实体容器)  │ │(AI创建)  │ │(解锁管理)  │
      └────────────┘ └─────┬──────┘ └──────────┘ └────────────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
       ┌──────▼──────┐ ┌──▼────────┐ ┌▼──────────┐
       │  Attribute  │ │IChessSkill│ │ IChessAI  │
       │  Component  │ │(接口实现) │ │(接口实现) │
       └─────────────┘ └───────────┘ └───────────┘
```

### 核心类关系

```
ChessEntity (MonoBehaviour)
    ├── ChessContext (运行时上下文)
    ├── ChessAttribute (属性组件)
    ├── IChessSkill (技能接口实现)
    └── IChessAI (AI接口实现)

ChessFactory (静态工厂)
    ├── CreateAI(aiType) → IChessAI
    └── CreateSkill(skillId) → IChessSkill

SummonChessManager (单例)
    ├── List<ChessEntity> (所有活跃棋子)
    ├── SpawnChess(configId) → ChessEntity
    └── Update() → 调用所有棋子的Tick()
```

### 数据流

```
配置表 → ChessDataManager → ChessConfig
                                  ↓
玩家请求 → SummonChessManager → ChessFactory.CreateAI/CreateSkill
                                  ↓
                          创建ChessEntity + 初始化组件
                                  ↓
                          每帧Update调用所有棋子的Tick()
                                  ↓
                          AI.Tick() 控制行为 + Skill.Tick() 更新冷却
                                  ↓
                          事件系统通知其他模块
```

## 组件设计

### 1. 配置数据层

#### SummonChessConfig
棋子配置数据类，对应配置表的一行数据。

```csharp
public class SummonChessConfig
{
    public int Id;                    // 棋子ID
    public string Name;               // 棋子名称
    public int Quality;               // 品质（1-4）
    public int PopCost;               // 人口消耗
    public int[] Races;               // 种族ID数组
    public int[] Classes;             // 职业ID数组
    public int StarLevel;             // 星级（1-3）
    public int NextStarId;            // 下一星级ID
    public int PrefabPath;            // 预制体资源ID
    public int IconPath;              // 图标资源ID
    public double MaxHp;              // 最大生命值
    public double MaxMp;              // 最大法力值
    public double InitialMp;          // 初始法力值
    public double AtkDamage;          // 攻击力
    public double AtkSpeed;           // 攻击速度
    public double AtkRange;           // 攻击范围
    public double Armor;              // 护甲
    public double MagicResist;        // 魔抗
    public double MoveSpeed;          // 移动速度
    public int SkillId;               // 技能ID
    public int AIType;                // AI类型
    public string Description;        // 描述
}
```

#### ChessDataManager
配置数据管理器，负责加载和查询配置数据。

```csharp
public class ChessDataManager : Singleton<ChessDataManager>
{
    private Dictionary<int, SummonChessConfig> m_ConfigDict;
    
    // 加载配置表
    public void LoadConfigs()
    {
        var table = GF.DataTable.GetDataTable<SummonChessTable>();
        // 解析配置表数据到m_ConfigDict
    }
    
    // 查询配置
    public SummonChessConfig GetConfig(int chessId);
    public bool TryGetConfig(int chessId, out SummonChessConfig config);
    
    // 验证配置
    private bool ValidateConfig(SummonChessConfig config);
}
```

### 2. 上下文与接口层

#### ChessContext
棋子运行时上下文，类似 `PlayerSkillContext`。

```csharp
public class ChessContext
{
    public GameObject Owner;              // 棋子GameObject
    public Transform Transform;           // 棋子Transform
    public ChessAttribute Attribute;      // 属性组件
    public int Camp;                      // 阵营（0=玩家，1=敌人）
    public SummonChessConfig Config;      // 配置数据
    
    // 可扩展：目标选择、动画控制器等
}
```

#### IChessAI
AI行为接口，类似 `IPlayerSkill`。

```csharp
public interface IChessAI
{
    int AIType { get; }
    
    // 初始化
    void Init(ChessContext ctx);
    
    // 每帧更新
    void Tick(float dt);
    
    // 查找目标
    ChessEntity FindTarget();
    
    // 执行移动
    void Move(Vector3 targetPosition, float dt);
    
    // 执行攻击
    void Attack(ChessEntity target);
}
```

#### IChessSkill
技能接口，类似 `IPlayerSkill`。

```csharp
public interface IChessSkill
{
    int SkillId { get; }
    
    // 初始化
    void Init(ChessContext ctx, ChessSkillConfig config);
    
    // 每帧更新（冷却计时）
    void Tick(float dt);
    
    // 尝试释放技能
    bool TryCast();
    
    // 检查是否可以释放
    bool CanCast();
}
```

### 3. 工厂层

#### ChessFactory
棋子AI和技能的工厂类，类似 `SkillFactory`。

```csharp
public static class ChessFactory
{
    private static readonly Dictionary<int, Func<IChessAI>> s_AICreators = new();
    private static readonly Dictionary<int, Func<IChessSkill>> s_SkillCreators = new();
    
    // 注册所有AI类型（游戏启动时调用一次）
    public static void RegisterAllAI()
    {
        s_AICreators.Clear();
        
        // 注册AI类型
        RegisterAI(1, () => new MeleeAI());      // 近战AI
        RegisterAI(2, () => new RangedAI());     // 远程AI
        // 后续可扩展更多AI类型
    }
    
    // 注册所有技能（游戏启动时调用一次）
    public static void RegisterAllSkills()
    {
        s_SkillCreators.Clear();
        
        // 根据技能ID注册技能实现
        // RegisterSkill(1001, () => new ChessFireballSkill());
        // RegisterSkill(1002, () => new ChessHealSkill());
    }
    
    // 注册AI
    public static void RegisterAI(int aiType, Func<IChessAI> creator)
    {
        if (creator == null)
        {
            Debug.LogError($"ChessFactory.RegisterAI creator is null, aiType={aiType}");
            return;
        }
        s_AICreators[aiType] = creator;
    }
    
    // 注册技能
    public static void RegisterSkill(int skillId, Func<IChessSkill> creator)
    {
        if (creator == null)
        {
            Debug.LogError($"ChessFactory.RegisterSkill creator is null, skillId={skillId}");
            return;
        }
        s_SkillCreators[skillId] = creator;
    }
    
    // 创建AI实例
    public static IChessAI CreateAI(int aiType)
    {
        if (s_AICreators.TryGetValue(aiType, out var creator))
            return creator();
        
        Debug.LogWarning($"ChessFactory.CreateAI: AI type {aiType} not registered");
        return null;
    }
    
    // 创建技能实例
    public static IChessSkill CreateSkill(int skillId)
    {
        if (skillId == 0) return null;  // 技能ID为0表示无技能
        
        if (s_SkillCreators.TryGetValue(skillId, out var creator))
            return creator();
        
        Debug.LogWarning($"ChessFactory.CreateSkill: Skill {skillId} not registered");
        return null;
    }
}
```

### 4. 实体层

#### ChessEntity
棋子实体，运行时的棋子实例（MonoBehaviour）。

```csharp
public class ChessEntity : MonoBehaviour
{
    #region 配置数据
    
    public int ChessId { get; private set; }
    public SummonChessConfig Config { get; private set; }
    
    #endregion
    
    #region 组件引用
    
    public ChessAttribute Attribute { get; private set; }
    public IChessSkill Skill { get; private set; }
    public IChessAI AI { get; private set; }
    
    #endregion
    
    #region 运行时数据
    
    public int InstanceId { get; private set; }
    public int Camp { get; set; }  // 阵营（0=玩家，1=敌人）
    public ChessState CurrentState { get; private set; }
    
    private ChessContext m_Context;
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化棋子（由SummonChessManager调用）
    /// </summary>
    public void Initialize(int chessId, SummonChessConfig config, int camp)
    {
        ChessId = chessId;
        Config = config;
        Camp = camp;
        InstanceId = GetInstanceID();
        
        // 初始化属性组件
        Attribute = gameObject.AddComponent<ChessAttribute>();
        Attribute.Initialize(config);
        
        // 创建上下文
        m_Context = new ChessContext
        {
            Owner = gameObject,
            Transform = transform,
            Attribute = Attribute,
            Camp = camp,
            Config = config
        };
        
        // 创建AI（使用工厂）
        AI = ChessFactory.CreateAI(config.AIType);
        if (AI != null)
        {
            AI.Init(m_Context);
        }
        
        // 创建技能（使用工厂）
        if (config.SkillId != 0)
        {
            Skill = ChessFactory.CreateSkill(config.SkillId);
            if (Skill != null)
            {
                // 从配置表加载技能配置
                var skillConfig = LoadSkillConfig(config.SkillId);
                Skill.Init(m_Context, skillConfig);
            }
        }
        
        // 初始状态
        CurrentState = ChessState.Idle;
    }
    
    private ChessSkillConfig LoadSkillConfig(int skillId)
    {
        // 从SummonChessSkillTable加载技能配置
        // 返回技能配置数据
        return default;
    }
    
    #endregion
    
    #region 生命周期
    
    private void Update()
    {
        float dt = Time.deltaTime;
        
        // 更新AI
        AI?.Tick(dt);
        
        // 更新技能冷却
        Skill?.Tick(dt);
    }
    
    #endregion
    
    #region 查询接口
    
    public bool HasRace(int raceId)
    {
        if (Config.Races == null) return false;
        for (int i = 0; i < Config.Races.Length; i++)
        {
            if (Config.Races[i] == raceId)
                return true;
        }
        return false;
    }
    
    public bool HasClass(int classId)
    {
        if (Config.Classes == null) return false;
        for (int i = 0; i < Config.Classes.Length; i++)
        {
            if (Config.Classes[i] == classId)
                return true;
        }
        return false;
    }
    
    #endregion
    
    #region 状态管理
    
    public void ChangeState(ChessState newState)
    {
        if (CurrentState == newState) return;
        
        ChessState oldState = CurrentState;
        CurrentState = newState;
        
        // 触发状态变化事件
        OnStateChanged?.Invoke(oldState, newState);
    }
    
    public event Action<ChessState, ChessState> OnStateChanged;
    
    #endregion
}

/// <summary>
/// 棋子状态枚举
/// </summary>
public enum ChessState
{
    Idle,       // 待机
    Moving,     // 移动
    Attacking,  // 攻击
    Casting,    // 施法
    Dead        // 死亡
}
```
    public int StarLevel;             // 星级（1-3）
    public int NextStarId;            // 下一星级ID
    public int PrefabPath;            // 预制体资源ID
    public int IconPath;              // 图标资源ID
    public double MaxHp;              // 最大生命值
    public double MaxMp;              // 最大法力值
    public double InitialMp;          // 初始法力值
    public double AtkDamage;          // 攻击力
    public double AtkSpeed;           // 攻击速度
    public double AtkRange;           // 攻击范围
    public double Armor;              // 护甲
    public double MagicResist;        // 魔抗
    public double MoveSpeed;          // 移动速度
    public int SkillId;               // 技能ID
    public int AIType;                // AI类型
    public string Description;        // 描述
}
```

#### ChessDataManager
配置数据管理器，负责加载和查询配置数据。

```csharp
public class ChessDataManager : Singleton<ChessDataManager>
{
    private Dictionary<int, SummonChessConfig> m_ConfigDict;
    
    // 加载配置表
    public void LoadConfigs();
    
    // 查询配置
    public SummonChessConfig GetConfig(int chessId);
    public bool TryGetConfig(int chessId, out SummonChessConfig config);
    
    // 验证配置
    private bool ValidateConfig(SummonChessConfig config);
}
```

### 2. 实体层

#### ChessEntity
棋子实体，运行时的棋子实例。

```csharp
public class ChessEntity : MonoBehaviour
{
    // 配置数据
    public int ChessId { get; private set; }
    public SummonChessConfig Config { get; private set; }
    
    // 组件引用
    public ChessAttribute Attribute { get; private set; }
    public ChessSkill Skill { get; private set; }
    public ChessState State { get; private set; }
    
    // 运行时数据
    public int InstanceId { get; private set; }
    public int Camp { get; set; }  // 阵营（0=玩家，1=敌人）
    
    // 初始化
    public void Initialize(int chessId, SummonChessConfig config);
    
    // 查询接口
    public bool HasRace(int raceId);
    public bool HasClass(int classId);
}
```

#### ChessAttribute
属性组件，管理棋子的属性值。

```csharp
public class ChessAttribute : MonoBehaviour
{
    // 属性值
    private double m_CurrentHp;
    private double m_CurrentMp;
    private double m_MaxHp;
    private double m_MaxMp;
    
    // 属性访问
    public double CurrentHp => m_CurrentHp;
    public double CurrentMp => m_CurrentMp;
    public double MaxHp => m_MaxHp;
    public double MaxMp => m_MaxMp;
    
    // 属性修改
    public void ModifyHp(double delta);
    public void ModifyMp(double delta);
    public void SetHp(double value);
    public void SetMp(double value);
    
    // 伤害计算
    public double CalculatePhysicalDamage(double baseDamage, double armor);
    public double CalculateMagicDamage(double baseDamage, double magicResist);
    
    // 事件
    public event Action<double, double> OnHpChanged;  // (oldValue, newValue)
    public event Action<double, double> OnMpChanged;
}
```

#### ChessAttribute
属性组件，管理棋子的属性值（MonoBehaviour）。

```csharp
public class ChessAttribute : MonoBehaviour
{
    #region 属性值
    
    private double m_CurrentHp;
    private double m_CurrentMp;
    private double m_MaxHp;
    private double m_MaxMp;
    private double m_AtkDamage;
    private double m_AtkSpeed;
    private double m_AtkRange;
    private double m_Armor;
    private double m_MagicResist;
    private double m_MoveSpeed;
    
    #endregion
    
    #region 属性访问
    
    public double CurrentHp => m_CurrentHp;
    public double CurrentMp => m_CurrentMp;
    public double MaxHp => m_MaxHp;
    public double MaxMp => m_MaxMp;
    public double AtkDamage => m_AtkDamage;
    public double AtkSpeed => m_AtkSpeed;
    public double AtkRange => m_AtkRange;
    public double Armor => m_Armor;
    public double MagicResist => m_MagicResist;
    public double MoveSpeed => m_MoveSpeed;
    
    #endregion
    
    #region 初始化
    
    public void Initialize(SummonChessConfig config)
    {
        m_MaxHp = config.MaxHp;
        m_MaxMp = config.MaxMp;
        m_CurrentHp = config.MaxHp;
        m_CurrentMp = config.InitialMp;
        m_AtkDamage = config.AtkDamage;
        m_AtkSpeed = config.AtkSpeed;
        m_AtkRange = config.AtkRange;
        m_Armor = config.Armor;
        m_MagicResist = config.MagicResist;
        m_MoveSpeed = config.MoveSpeed;
    }
    
    #endregion
    
    #region 属性修改
    
    public void ModifyHp(double delta)
    {
        double oldValue = m_CurrentHp;
        m_CurrentHp = Math.Clamp(m_CurrentHp + delta, 0, m_MaxHp);
        
        if (Math.Abs(m_CurrentHp - oldValue) > 0.001)
        {
            OnHpChanged?.Invoke(oldValue, m_CurrentHp);
        }
    }
    
    public void ModifyMp(double delta)
    {
        double oldValue = m_CurrentMp;
        m_CurrentMp = Math.Clamp(m_CurrentMp + delta, 0, m_MaxMp);
        
        if (Math.Abs(m_CurrentMp - oldValue) > 0.001)
        {
            OnMpChanged?.Invoke(oldValue, m_CurrentMp);
        }
    }
    
    public void SetHp(double value)
    {
        ModifyHp(value - m_CurrentHp);
    }
    
    public void SetMp(double value)
    {
        ModifyMp(value - m_CurrentMp);
    }
    
    #endregion
    
    #region 伤害计算
    
    /// <summary>
    /// 计算物理伤害（考虑护甲减伤）
    /// </summary>
    public double CalculatePhysicalDamage(double baseDamage)
    {
        // 简化的护甲减伤公式：实际伤害 = 基础伤害 * (100 / (100 + 护甲))
        double damageReduction = 100.0 / (100.0 + m_Armor);
        double actualDamage = baseDamage * damageReduction;
        return Math.Max(0, actualDamage);
    }
    
    /// <summary>
    /// 计算魔法伤害（考虑魔抗减伤）
    /// </summary>
    public double CalculateMagicDamage(double baseDamage)
    {
        // 简化的魔抗减伤公式：实际伤害 = 基础伤害 * (100 / (100 + 魔抗))
        double damageReduction = 100.0 / (100.0 + m_MagicResist);
        double actualDamage = baseDamage * damageReduction;
        return Math.Max(0, actualDamage);
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(double damage, bool isMagic = false)
    {
        double actualDamage = isMagic ? CalculateMagicDamage(damage) : CalculatePhysicalDamage(damage);
        ModifyHp(-actualDamage);
        
        OnDamageTaken?.Invoke(actualDamage, isMagic);
    }
    
    #endregion
    
    #region 事件
    
    public event Action<double, double> OnHpChanged;  // (oldValue, newValue)
    public event Action<double, double> OnMpChanged;  // (oldValue, newValue)
    public event Action<double, bool> OnDamageTaken;  // (damage, isMagic)
    
    #endregion
}
```

### 5. 管理层

#### SummonChessManager
棋子管理器，负责棋子的生命周期管理（单例MonoBehaviour）。

```csharp
public class SummonChessManager : MonoBehaviour
{
    public static SummonChessManager Instance { get; private set; }
    
    #region 私有字段
    
    private List<ChessEntity> m_AllChess = new List<ChessEntity>();
    private Dictionary<int, ChessEntity> m_ChessDict = new Dictionary<int, ChessEntity>();
    private int m_NextInstanceId = 1;
    
    // 资源缓存
    private Dictionary<int, GameObject> m_PrefabCache = new Dictionary<int, GameObject>();
    
    #endregion
    
    #region 生命周期
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Update()
    {
        // 棋子的Tick在各自的Update中调用
        // 这里可以处理全局逻辑
    }
    
    #endregion
    
    #region 棋子生成
    
    /// <summary>
    /// 生成棋子
    /// </summary>
    public async UniTask<ChessEntity> SpawnChessAsync(int chessId, Vector3 position, int camp)
    {
        // 1. 获取配置
        if (!ChessDataManager.Instance.TryGetConfig(chessId, out var config))
        {
            Debug.LogError($"SpawnChess failed: config not found for chessId={chessId}");
            return null;
        }
        
        // 2. 加载预制体
        GameObject prefab = await LoadPrefabAsync(config.PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"SpawnChess failed: prefab not found for chessId={chessId}");
            return null;
        }
        
        // 3. 实例化
        GameObject chessObj = Instantiate(prefab, position, Quaternion.identity);
        chessObj.name = $"Chess_{config.Name}_{m_NextInstanceId}";
        
        // 4. 添加ChessEntity组件
        ChessEntity entity = chessObj.GetComponent<ChessEntity>();
        if (entity == null)
        {
            entity = chessObj.AddComponent<ChessEntity>();
        }
        
        // 5. 初始化
        entity.Initialize(chessId, config, camp);
        
        // 6. 注册到管理器
        int instanceId = m_NextInstanceId++;
        m_AllChess.Add(entity);
        m_ChessDict[instanceId] = entity;
        
        // 7. 监听死亡事件
        entity.Attribute.OnHpChanged += (oldHp, newHp) =>
        {
            if (newHp <= 0)
            {
                DestroyChess(entity);
            }
        };
        
        // 8. 触发生成事件
        OnChessSpawned?.Invoke(entity);
        
        return entity;
    }
    
    /// <summary>
    /// 异步加载预制体
    /// </summary>
    private async UniTask<GameObject> LoadPrefabAsync(int resourceId)
    {
        // 检查缓存
        if (m_PrefabCache.TryGetValue(resourceId, out var cached))
        {
            return cached;
        }
        
        // 使用ResourceExtension加载
        var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(resourceId);
        
        if (prefab != null)
        {
            m_PrefabCache[resourceId] = prefab;
        }
        
        return prefab;
    }
    
    #endregion
    
    #region 棋子销毁
    
    /// <summary>
    /// 销毁棋子
    /// </summary>
    public void DestroyChess(ChessEntity entity)
    {
        if (entity == null) return;
        
        // 1. 从列表中移除
        m_AllChess.Remove(entity);
        m_ChessDict.Remove(entity.InstanceId);
        
        // 2. 触发销毁事件
        OnChessDestroyed?.Invoke(entity);
        
        // 3. 销毁GameObject
        Destroy(entity.gameObject);
    }
    
    #endregion
    
    #region 查询接口
    
    /// <summary>
    /// 获取所有棋子
    /// </summary>
    public IReadOnlyList<ChessEntity> GetAllChess()
    {
        return m_AllChess;
    }
    
    /// <summary>
    /// 根据实例ID查询棋子
    /// </summary>
    public ChessEntity GetChess(int instanceId)
    {
        m_ChessDict.TryGetValue(instanceId, out var entity);
        return entity;
    }
    
    /// <summary>
    /// 查询指定阵营的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessByCamp(int camp)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].Camp == camp)
            {
                result.Add(m_AllChess[i]);
            }
        }
        return result;
    }
    
    /// <summary>
    /// 查询指定范围内的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessInRange(Vector3 center, float radius, int camp = -1)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        float radiusSqr = radius * radius;
        
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            var chess = m_AllChess[i];
            
            // 阵营过滤
            if (camp >= 0 && chess.Camp != camp)
                continue;
            
            // 距离过滤
            float distSqr = (chess.transform.position - center).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                result.Add(chess);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 查询指定种族的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessByRace(int raceId)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].HasRace(raceId))
            {
                result.Add(m_AllChess[i]);
            }
        }
        return result;
    }
    
    /// <summary>
    /// 查询指定职业的所有棋子
    /// </summary>
    public List<ChessEntity> GetChessByClass(int classId)
    {
        List<ChessEntity> result = new List<ChessEntity>();
        for (int i = 0; i < m_AllChess.Count; i++)
        {
            if (m_AllChess[i].HasClass(classId))
            {
                result.Add(m_AllChess[i]);
            }
        }
        return result;
    }
    
    #endregion
    
    #region 事件
    
    public event Action<ChessEntity> OnChessSpawned;
    public event Action<ChessEntity> OnChessDestroyed;
    
    #endregion
}
```

### 6. AI系统

#### 基础AI实现示例

```csharp
/// <summary>
/// 近战AI - 寻找最近的敌人并移动到攻击范围内
/// </summary>
public class MeleeAI : IChessAI
{
    public int AIType => 1;
    
    private ChessContext m_Context;
    private ChessEntity m_Target;
    private float m_AttackCooldown;
    
    public void Init(ChessContext ctx)
    {
        m_Context = ctx;
    }
    
    public void Tick(float dt)
    {
        // 更新攻击冷却
        if (m_AttackCooldown > 0)
        {
            m_AttackCooldown -= dt;
        }
        
        // 查找目标
        if (m_Target == null || m_Target.Attribute.CurrentHp <= 0)
        {
            m_Target = FindTarget();
        }
        
        if (m_Target == null)
        {
            // 没有目标，待机
            return;
        }
        
        // 计算距离
        float distance = Vector3.Distance(m_Context.Transform.position, m_Target.transform.position);
        
        if (distance > m_Context.Attribute.AtkRange)
        {
            // 超出攻击范围，移动
            Vector3 direction = (m_Target.transform.position - m_Context.Transform.position).normalized;
            Move(m_Target.transform.position, dt);
        }
        else
        {
            // 在攻击范围内，攻击
            if (m_AttackCooldown <= 0)
            {
                Attack(m_Target);
                m_AttackCooldown = 1f / (float)m_Context.Attribute.AtkSpeed;
            }
        }
    }
    
    public ChessEntity FindTarget()
    {
        // 查找敌方阵营的所有棋子
        int enemyCamp = m_Context.Camp == 0 ? 1 : 0;
        var enemies = SummonChessManager.Instance.GetChessByCamp(enemyCamp);
        
        if (enemies.Count == 0)
            return null;
        
        // 找最近的敌人
        ChessEntity nearest = null;
        float minDist = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(m_Context.Transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
    
    public void Move(Vector3 targetPosition, float dt)
    {
        Vector3 direction = (targetPosition - m_Context.Transform.position).normalized;
        float moveDistance = (float)m_Context.Attribute.MoveSpeed * dt;
        m_Context.Transform.position += direction * moveDistance;
    }
    
    public void Attack(ChessEntity target)
    {
        if (target == null || target.Attribute == null)
            return;
        
        // 造成伤害
        target.Attribute.TakeDamage(m_Context.Attribute.AtkDamage, false);
    }
}

/// <summary>
/// 远程AI - 在原地攻击范围内的敌人
/// </summary>
public class RangedAI : IChessAI
{
    public int AIType => 2;
    
    private ChessContext m_Context;
    private ChessEntity m_Target;
    private float m_AttackCooldown;
    
    public void Init(ChessContext ctx)
    {
        m_Context = ctx;
    }
    
    public void Tick(float dt)
    {
        // 更新攻击冷却
        if (m_AttackCooldown > 0)
        {
            m_AttackCooldown -= dt;
        }
        
        // 查找目标
        if (m_Target == null || m_Target.Attribute.CurrentHp <= 0)
        {
            m_Target = FindTarget();
        }
        
        if (m_Target == null)
        {
            // 没有目标，待机
            return;
        }
        
        // 远程单位不移动，只攻击
        if (m_AttackCooldown <= 0)
        {
            Attack(m_Target);
            m_AttackCooldown = 1f / (float)m_Context.Attribute.AtkSpeed;
        }
    }
    
    public ChessEntity FindTarget()
    {
        // 查找攻击范围内的敌人
        int enemyCamp = m_Context.Camp == 0 ? 1 : 0;
        var enemies = SummonChessManager.Instance.GetChessInRange(
            m_Context.Transform.position,
            (float)m_Context.Attribute.AtkRange,
            enemyCamp
        );
        
        // 返回第一个敌人
        return enemies.Count > 0 ? enemies[0] : null;
    }
    
    public void Move(Vector3 targetPosition, float dt)
    {
        // 远程单位不移动
    }
    
    public void Attack(ChessEntity target)
    {
        if (target == null || target.Attribute == null)
            return;
        
        // 造成伤害
        target.Attribute.TakeDamage(m_Context.Attribute.AtkDamage, false);
    }
}
```

### 7. 解锁管理器

#### ChessUnlockManager
管理玩家已解锁的棋子列表。

```csharp
public class ChessUnlockManager : Singleton<ChessUnlockManager>
{
    private HashSet<int> m_UnlockedChess = new HashSet<int>();
    
    /// <summary>
    /// 解锁棋子
    /// </summary>
    public bool UnlockChess(int chessId)
    {
        if (m_UnlockedChess.Contains(chessId))
        {
            return false;  // 已解锁
        }
        
        m_UnlockedChess.Add(chessId);
        OnChessUnlocked?.Invoke(chessId);
        return true;
    }
    
    /// <summary>
    /// 检查棋子是否已解锁
    /// </summary>
    public bool IsChessUnlocked(int chessId)
    {
        return m_UnlockedChess.Contains(chessId);
    }
    
    /// <summary>
    /// 获取所有已解锁的棋子ID
    /// </summary>
    public IReadOnlyCollection<int> GetUnlockedChess()
    {
        return m_UnlockedChess;
    }
    
    /// <summary>
    /// 获取已解锁棋子数量
    /// </summary>
    public int GetUnlockedCount()
    {
        return m_UnlockedChess.Count;
    }
    
    /// <summary>
    /// 序列化到存档数据
    /// </summary>
    public List<int> SerializeToSaveData()
    {
        return new List<int>(m_UnlockedChess);
    }
    
    /// <summary>
    /// 从存档数据反序列化
    /// </summary>
    public void DeserializeFromSaveData(List<int> unlockedChessIds)
    {
        m_UnlockedChess.Clear();
        
        if (unlockedChessIds != null)
        {
            foreach (var id in unlockedChessIds)
            {
                m_UnlockedChess.Add(id);
            }
        }
    }
    
    /// <summary>
    /// 清空解锁列表（新存档）
    /// </summary>
    public void Clear()
    {
        m_UnlockedChess.Clear();
    }
    
    public event Action<int> OnChessUnlocked;
}
```

## 数据模型

### 配置表结构

#### SummonChessTable
```
ID | Name | Quality | PopCost | Races | Classes | StarLevel | NextStarId | ...
1  | 后羿 | 2       | 1       | 1     | 1       | 1         | 2          | ...
```

#### SummonChessSkillTable
```
ID | Name | Cooldown | ManaCost | EffectType | ...
```

### 存档数据结构

```csharp
[Serializable]
public class ChessSaveData
{
    public List<int> UnlockedChessIds;  // 已解锁的棋子ID列表
}
```

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真。属性是人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1：配置数据完整性
*对于任意*有效的棋子ID，查询配置数据应该返回非空的配置对象，且配置对象的所有必需字段都应该有效。

**验证：需求 1.2, 1.5**

### 属性 2：棋子生成一致性
*对于任意*有效的配置ID，生成棋子后，棋子的属性值应该与配置数据一致。

**验证：需求 2.1, 2.3**

### 属性 3：生命值边界约束
*对于任意*棋子，其当前生命值应该始终在 [0, MaxHp] 范围内。

**验证：需求 3.5**

### 属性 4：法力值边界约束
*对于任意*棋子，其当前法力值应该始终在 [0, MaxMp] 范围内。

**验证：需求 3.6**

### 属性 5：伤害计算非负性
*对于任意*伤害值和护甲/魔抗值，计算后的实际伤害应该大于等于0。

**验证：需求 8.4**

### 属性 6：属性变化事件触发
*对于任意*属性值变化，如果新值与旧值不同，应该触发对应的属性变化事件。

**验证：需求 3.3, 3.4**

### 属性 7：棋子列表一致性
*对于任意*时刻，SummonChessManager维护的棋子列表应该与实际存在的棋子实例一致。

**验证：需求 2.6**

### 属性 8：死亡触发销毁
*对于任意*棋子，当其生命值降为0时，应该被标记为死亡状态并最终被销毁。

**验证：需求 2.4, 9.4**

### 属性 9：种族职业查询正确性
*对于任意*棋子和种族/职业ID，查询结果应该与配置数据中的种族/职业数组一致。

**验证：需求 7.3, 7.4, 7.5, 7.6**

### 属性 10：解锁列表去重
*对于任意*棋子ID，重复解锁不应该导致解锁列表中出现重复的ID。

**验证：需求 13.2**

### 属性 11：存档序列化往返一致性
*对于任意*已解锁棋子列表，序列化后再反序列化应该得到相同的列表。

**验证：需求 13.3, 13.4**

### 属性 12：查询过滤正确性
*对于任意*查询条件（阵营、范围、种族、职业），查询结果中的所有棋子都应该满足该条件。

**验证：需求 11.2, 11.3, 11.4, 11.5**

### 属性 13：AI目标选择有效性
*对于任意*AI实例，FindTarget()返回的目标应该属于敌方阵营且生命值大于0。

**验证：需求 4.1, 4.2, 4.3**

### 属性 14：攻击频率一致性
*对于任意*棋子，其实际攻击频率应该与配置的攻击速度一致（误差在合理范围内）。

**验证：需求 4.4**

### 属性 15：资源缓存有效性
*对于任意*预制体资源ID，多次加载相同资源应该返回缓存的实例。

**验证：需求 10.2**

## 错误处理

### 配置错误
- 配置表加载失败：记录错误日志，返回空配置
- 配置数据无效：跳过该条数据，记录警告日志
- 必需字段缺失：使用默认值，记录警告日志

### 运行时错误
- 预制体加载失败：取消生成，返回null
- AI类型未注册：使用默认AI或不创建AI
- 技能ID未注册：不创建技能组件
- 目标查找失败：进入待机状态

### 边界情况
- 空数组字段：解析为空数组，不报错
- 技能ID为0：视为无技能，不创建技能组件
- 3星棋子：NextStarId为0，不允许继续进化
- 伤害计算结果为负：强制设为0

## 测试策略

### 单元测试
- 配置数据加载和解析
- 属性计算（伤害减免、边界约束）
- 查询接口（种族、职业、阵营过滤）
- 解锁管理（去重、序列化）

### 属性测试
- 使用属性测试框架（如C#的FsCheck）
- 每个属性测试运行至少100次迭代
- 生成随机的配置数据、属性值、查询条件
- 验证所有正确性属性

### 集成测试
- 完整的棋子生成流程
- AI行为测试（目标选择、移动、攻击）
- 技能释放流程
- 存档系统集成

### 性能测试
- 大量棋子同时存在时的性能
- 查询接口的性能（特别是范围查询）
- 资源加载和缓存效率
