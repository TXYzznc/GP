using UnityEngine;

/// <summary>
/// Buff 上下文，传递 Buff 运行时需要的基础信息
/// </summary>
public class BuffContext
{
    /// <summary>
    /// Buff 的承受者（谁身上挂载了这个 Buff）
    /// </summary>
    public GameObject Owner;

    /// <summary>
    /// Buff 的施法者（谁施加了这个 Buff，可能为 null）
    /// </summary>
    public GameObject Caster;

    /// <summary>
    /// 承受者的 Transform 组件
    /// </summary>
    public Transform Transform;

    #region 棋子系统扩展

    /// <summary>
    /// 承受者的属性组件（棋子系统使用）
    /// </summary>
    public ChessAttribute OwnerAttribute;

    /// <summary>
    /// 施法者的属性组件（棋子系统使用，灼烧等需要获取施法者法强）
    /// </summary>
    public ChessAttribute CasterAttribute;

    /// <summary>
    /// 承受者的 BuffManager 组件（Buff 间交互使用，如冰霜检测灼烧）
    /// </summary>
    public BuffManager OwnerBuffManager;

    #endregion
}
