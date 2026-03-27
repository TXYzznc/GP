using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 净化系统
/// 处理敌人净化逻辑和圣水消耗
/// </summary>
/// <summary>
/// 净化系统
/// 处理敌人净化逻辑和圣水消耗
/// </summary>
public class PurificationSystem : SingletonBase<PurificationSystem>
{
    #region 私有字段

    /// <summary>待净化的敌人列表</summary>
    private List<EnemyEntity> m_PendingPurificationEnemies = new List<EnemyEntity>();

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        DebugEx.LogModule("PurificationSystem", "初始化完成");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion
}
