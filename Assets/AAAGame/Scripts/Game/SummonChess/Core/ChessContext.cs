using UnityEngine;

/// <summary>
/// 棋子运行时上下文
/// 用于在棋子各组件之间传递运行时数据
/// 参考 PlayerSkillContext 的设计
/// </summary>
public class ChessContext
{
    #region 基础引用

    /// <summary>棋子GameObject</summary>
    public GameObject Owner;

    /// <summary>棋子Transform</summary>
    public Transform Transform;

    #endregion

    #region 组件引用

    /// <summary>属性组件</summary>
    public ChessAttribute Attribute;

    /// <summary>棋子实体组件</summary>
    public ChessEntity Entity;

    /// <summary>Buff管理组件</summary>
    public BuffManager BuffManager;

    #endregion

    #region 配置数据

    /// <summary>阵营（0=玩家，1=敌人）</summary>
    public int Camp;

    /// <summary>棋子配置</summary>
    public SummonChessConfig Config;

    #endregion

    #region 后续扩展字段

    // 可能需加入：
    // - Animator 动画控制器
    // - 目标选择系统
    // - 音效管理器
    // - 特效管理器

    #endregion
}
