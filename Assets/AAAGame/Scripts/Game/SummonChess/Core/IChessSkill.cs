using UnityEngine;

/// <summary>
/// 棋子技能接口（主动技能/大招）
/// 技能实现类完全控制技能执行流程
/// </summary>
public interface IChessSkill
{
    #region 基础信息
    
    /// <summary>技能ID</summary>
    int SkillId { get; }

    /// <summary>技能类型：3=主动技能 4=大招</summary>
    int SkillType { get; }
    
    /// <summary>技能配置</summary>
    SummonChessSkillTable Config { get; }
    
    #endregion
    
    #region 生命周期
    
    /// <summary>
    /// 初始化技能
    /// </summary>
    void Init(ChessContext ctx, SummonChessSkillTable config);

    /// <summary>
    /// 每帧更新（冷却倒计时等）
    /// </summary>
    void Tick(float dt);
    
    #endregion
    
    #region 条件检查

    /// <summary>
    /// 尝试释放技能（检查条件、消耗资源、进入冷却）
    /// </summary>
    /// <returns>是否成功释放</returns>
    bool TryCast();

    /// <summary>
    /// 检查是否可以释放
    /// </summary>
    bool CanCast();

    /// <summary>
    /// 获取当前冷却剩余时间
    /// </summary>
    float GetCooldownRemaining();
    
    #endregion
    
    #region 核心执行
    
    /// <summary>
    /// 执行技能完整流程
    /// 职责：查找目标、计算伤害、构建上下文、播放特效、执行命中检测
    /// </summary>
    /// <param name="caster">施法者</param>
    void ExecuteSkill(ChessEntity caster);
    
    #endregion
}

/// <summary>
/// 棋子被动技能接口
/// 被动技能不需要手动释放，通过事件驱动
/// </summary>
public interface IChessPassive
{
    /// <summary>被动技能ID</summary>
    int PassiveId { get; }

    /// <summary>
    /// 初始化（注册事件监听等）
    /// </summary>
    void Init(ChessContext ctx, SummonChessSkillTable config);

    /// <summary>
    /// 每帧更新（检测昼夜等持续效果）
    /// </summary>
    void Tick(float dt);

    /// <summary>
    /// 清理（取消事件监听等）
    /// </summary>
    void Dispose();
}

/// <summary>
/// 棋子普攻效果接口
/// 普攻实现类完全控制攻击执行流程
/// </summary>
public interface IChessNormalAttack
{
    #region 基础信息
    
    /// <summary>普攻效果ID</summary>
    int AttackId { get; }
    
    /// <summary>普攻配置</summary>
    SummonChessSkillTable Config { get; }
    
    #endregion
    
    #region 生命周期

    /// <summary>
    /// 初始化
    /// </summary>
    void Init(ChessContext ctx, SummonChessSkillTable config);
    
    #endregion
    
    #region 核心执行
    
    /// <summary>
    /// 执行普攻完整流程
    /// 职责：计算伤害、构建上下文、播放特效、执行命中检测
    /// </summary>
    /// <param name="caster">攻击者</param>
    /// <param name="target">目标（由 AI 传入）</param>
    void ExecuteAttack(ChessEntity caster, ChessEntity target);
    
    #endregion
}
