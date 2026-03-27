using System;
using UnityEngine;

/// <summary>
/// 棋子武器碰撞器
/// 挂载在武器 GameObject 上，用于近战攻击检测
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChessWeaponCollider : MonoBehaviour
{
    #region 私有字段

    /// <summary>碰撞器组件</summary>
    private Collider m_Collider;

    /// <summary>命中回调</summary>
    private Action<ChessEntity> m_OnHitCallback;

    /// <summary>拥有者阵营</summary>
    private int m_OwnerCamp;

    /// <summary>是否启用</summary>
    private bool m_IsEnabled;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_Collider = GetComponent<Collider>();
        if (m_Collider != null)
        {
            m_Collider.isTrigger = true;
            m_Collider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_IsEnabled || m_OnHitCallback == null) return;

        // 获取棋子实体
        ChessEntity target = other.GetComponent<ChessEntity>();
        if (target == null)
        {
            target = other.GetComponentInParent<ChessEntity>();
        }

        if (target == null) return;

        // 使用阵营服务检查是否为敌人
        if (!CampRelationService.IsEnemy(m_OwnerCamp, target.Camp)) return;

        // 触发回调
        m_OnHitCallback.Invoke(target);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置命中回调
    /// </summary>
    public void SetHitCallback(Action<ChessEntity> callback)
    {
        m_OnHitCallback = callback;
    }

    /// <summary>
    /// 清除命中回调
    /// </summary>
    public void ClearHitCallback()
    {
        m_OnHitCallback = null;
    }

    /// <summary>
    /// 设置拥有者阵营
    /// </summary>
    public void SetOwnerCamp(int camp)
    {
        m_OwnerCamp = camp;
    }

    /// <summary>
    /// 启用碰撞器
    /// </summary>
    public void EnableCollider()
    {
        m_IsEnabled = true;
        if (m_Collider != null)
        {
            m_Collider.enabled = true;
        }
    }

    /// <summary>
    /// 禁用碰撞器
    /// </summary>
    public void DisableCollider()
    {
        m_IsEnabled = false;
        if (m_Collider != null)
        {
            m_Collider.enabled = false;
        }
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled => m_IsEnabled;

    #endregion
}

