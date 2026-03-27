using UnityEngine;

/// <summary>
/// 敌人信息缓存
/// 战斗开始前预加载，避免实时查询
/// 职责：
/// 1. 缓存敌人的静态信息（初始位置、最大血量、攻击力等）
/// 2. 提供动态信息的快速访问（当前位置、当前血量等）
/// 3. 判断敌人是否存活
/// </summary>
public class EnemyInfoCache
{
    #region 静态信息（战斗开始时缓存）

    /// <summary>敌人实体引用（棋子）；召唤师条目此字段为 null</summary>
    public ChessEntity Entity { get; set; }

    /// <summary>召唤师战斗代理（非召唤师条目此字段为 null）</summary>
    public SummonerCombatProxy SummonerProxy { get; set; }

    /// <summary>敌人阵营</summary>
    public int Camp { get; set; }

    /// <summary>初始位置</summary>
    public Vector3 InitialPosition { get; set; }

    /// <summary>最大血量</summary>
    public double MaxHp { get; set; }

    /// <summary>攻击力</summary>
    public double AttackPower { get; set; }

    /// <summary>攻击距离</summary>
    public float AttackRange { get; set; }

    #endregion

    #region 动态信息（实时获取）

    /// <summary>是否存活</summary>
    public bool IsAlive => SummonerProxy != null
        ? !SummonerProxy.IsDead
        : Entity != null && !Entity.Attribute.IsDead;

    /// <summary>当前位置</summary>
    public Vector3 CurrentPosition => SummonerProxy != null
        ? SummonerProxy.transform.position
        : (Entity != null ? Entity.transform.position : InitialPosition);

    /// <summary>当前血量</summary>
    public double CurrentHp => SummonerProxy != null
        ? (SummonerRuntimeDataManager.Instance?.CurrentHP ?? 0f)
        : (Entity != null ? Entity.Attribute.CurrentHp : 0);

    /// <summary>血量百分比（0-1）</summary>
    public float HpPercent => MaxHp > 0 ? (float)(CurrentHp / MaxHp) : 0f;

    #endregion

    #region 构造函数

    /// <summary>
    /// 从棋子实体创建缓存
    /// </summary>
    public static EnemyInfoCache FromEntity(ChessEntity entity)
    {
        if (entity == null)
        {
            DebugEx.Error("EnemyInfoCache", "尝试从空实体创建缓存");
            return null;
        }

        return new EnemyInfoCache
        {
            Entity = entity,
            Camp = entity.Camp,
            InitialPosition = entity.transform.position,
            MaxHp = entity.Attribute.MaxHp,
            AttackPower = entity.Attribute.AtkDamage,
            AttackRange = (float)entity.Attribute.AtkRange,
        };
    }

    #endregion

    /// <summary>
    /// 从召唤师战斗代理创建缓存（召唤师作为敌方目标时使用）
    /// </summary>
    public static EnemyInfoCache FromSummonerProxy(SummonerCombatProxy proxy)
    {
        if (proxy == null)
        {
            DebugEx.Error("EnemyInfoCache", "尝试从空召唤师代理创建缓存");
            return null;
        }

        float maxHp = SummonerRuntimeDataManager.Instance?.MaxHP ?? 100f;
        return new EnemyInfoCache
        {
            SummonerProxy = proxy,
            Entity = null,
            Camp = proxy.Camp,
            InitialPosition = proxy.transform.position,
            MaxHp = maxHp,
            AttackPower = 0,
            AttackRange = 0,
        };
    }

    #region 调试

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string ToString()
    {
        string name = Entity != null ? Entity.Config?.Name : "Unknown";
        return $"[{name}] Camp={Camp}, HP={CurrentHp:F0}/{MaxHp:F0} ({HpPercent:P0}), Atk={AttackPower:F0}, Alive={IsAlive}";
    }

    #endregion
}
