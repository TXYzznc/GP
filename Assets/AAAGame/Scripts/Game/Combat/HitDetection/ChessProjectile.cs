using System;
using UnityEngine;

/// <summary>
/// 棋子投射物组件
/// 挂载在子弹/箭矢预制体上
/// 支持两种模式：
/// 1. 目标模式：朝目标位置或目标实体飞行
/// 2. 方向模式：沿固定方向直线飞行
/// </summary>
public class ChessProjectile : MonoBehaviour
{
    #region 配置

    [SerializeField]
    [Tooltip("投射物速度")]
    private float m_Speed = 10f;

    [SerializeField]
    [Tooltip("最大飞行时间（秒）")]
    private float m_MaxLifetime = 10f;

    [SerializeField]
    [Tooltip("命中后是否销毁")]
    private bool m_DestroyOnHit = true;

    [SerializeField]
    [Tooltip("是否穿透（可命中多个目标）")]
    private bool m_IsPiercing = false;

    [Header("调试可视化")]
    [SerializeField]
    [Tooltip("是否显示瞄准线和目标点（仅编辑器）")]
    private bool m_ShowDebugGizmos = true;

    [SerializeField]
    [Tooltip("目标点球体大小")]
    private float m_TargetGizmoSize = 0.3f;

    #endregion

    #region 私有字段

    /// <summary>飞行模式</summary>
    private ProjectileMode m_Mode;

    /// <summary>目标位置（目标模式）</summary>
    private Vector3 m_TargetPosition;

    /// <summary>飞行方向（方向模式）</summary>
    private Vector3 m_Direction;

    /// <summary>目标实体（追踪弹）</summary>
    private ChessEntity m_TargetEntity;

    /// <summary>是否追踪目标</summary>
    private bool m_IsHoming;

    /// <summary>攻击者阵营</summary>
    private int m_OwnerCamp;

    /// <summary>命中回调（通知检测器有目标被命中）</summary>
    private Action<ChessEntity> m_OnHitCallback;

    /// <summary>销毁回调</summary>
    private Action m_OnDestroyCallback;

    /// <summary>已命中的目标</summary>
    private System.Collections.Generic.HashSet<ChessEntity> m_HitTargets;

    /// <summary>最大穿透数（可命中的敌人数量）</summary>
    private int m_MaxPenetrationCount;

    /// <summary>当前已穿透数</summary>
    private int m_CurrentPenetrationCount;

    /// <summary>生存计时器</summary>
    private float m_LifetimeTimer;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region 枚举

    /// <summary>
    /// 投射物飞行模式
    /// </summary>
    private enum ProjectileMode
    {
        /// <summary>目标模式：朝目标位置飞行</summary>
        Target,
        /// <summary>方向模式：沿固定方向飞行</summary>
        Direction
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化投射物（追踪模式）
    /// </summary>
    /// <param name="ownerCamp">拥有者阵营</param>
    /// <param name="target">锁定目标</param>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="launchDirection">发射方向（已计算好）</param>
    /// <param name="speed">投射物速度</param>
    /// <param name="penetrationCount">穿透数量（可命中的敌人数量）</param>
    /// <param name="onHitCallback">命中回调</param>
    public void Initialize(
        int ownerCamp,
        ChessEntity target,
        Vector3 targetPosition,
        Vector3 launchDirection,
        float speed,
        int penetrationCount,
        Action<ChessEntity> onHitCallback)
    {
        m_Mode = ProjectileMode.Target;
        m_OwnerCamp = ownerCamp;
        m_OnHitCallback = onHitCallback;
        m_MaxPenetrationCount = penetrationCount > 0 ? penetrationCount : 1;

        // ⭐ 根据穿透数量自动设置穿透属性
        m_IsPiercing = m_MaxPenetrationCount > 1;

        // 设置速度
        if (speed > 0)
        {
            m_Speed = speed;
        }

        // 设置目标（追踪模式必须有目标）
        m_TargetEntity = target;
        m_TargetPosition = targetPosition;
        m_IsHoming = true;

        // ⭐ 设置初始朝向（使用传入的发射方向）
        if (launchDirection != Vector3.zero)
        {
            transform.forward = launchDirection;
        }

        m_HitTargets = new System.Collections.Generic.HashSet<ChessEntity>();
        m_CurrentPenetrationCount = 0;
        m_LifetimeTimer = 0f;
        m_IsInitialized = true;

        DebugEx.LogModule("ChessProjectile",
            $"初始化完成（追踪模式）- ownerCamp={m_OwnerCamp}, target={target?.Config?.Name}(Camp={target?.Camp}), " +
            $"目标位置: {m_TargetPosition}, 发射方向: {launchDirection}, 速度: {m_Speed}, 穿透: {m_IsPiercing}, 最大穿透数: {m_MaxPenetrationCount}");
    }

    /// <summary>
    /// 初始化投射物（方向模式）
    /// </summary>
    /// <param name="ownerCamp">拥有者阵营</param>
    /// <param name="direction">飞行方向（已归一化）</param>
    /// <param name="speed">投射物速度</param>
    /// <param name="penetrationCount">穿透数量</param>
    /// <param name="onHitCallback">命中回调</param>
    public void InitializeWithDirection(
        int ownerCamp,
        Vector3 direction,
        float speed,
        int penetrationCount,
        Action<ChessEntity> onHitCallback)
    {
        m_Mode = ProjectileMode.Direction;
        m_OwnerCamp = ownerCamp;
        m_OnHitCallback = onHitCallback;
        m_MaxPenetrationCount = penetrationCount > 0 ? penetrationCount : 1;

        // ⭐ 根据穿透数量自动设置穿透属性
        m_IsPiercing = m_MaxPenetrationCount > 1;

        // 设置速度
        if (speed > 0)
        {
            m_Speed = speed;
        }

        // 设置方向（方向模式不需要目标）
        m_Direction = direction.normalized;
        m_IsHoming = false;

        m_HitTargets = new System.Collections.Generic.HashSet<ChessEntity>();
        m_CurrentPenetrationCount = 0;
        m_LifetimeTimer = 0f;
        m_IsInitialized = true;

        // 设置初始朝向
        if (m_Direction != Vector3.zero)
        {
            transform.forward = m_Direction;
        }

        DebugEx.LogModule("ChessProjectile",
            $"初始化完成（方向模式）- ownerCamp={m_OwnerCamp}, 方向: {m_Direction}, 速度: {m_Speed}, " +
            $"穿透: {m_IsPiercing}, 最大穿透数: {m_MaxPenetrationCount}");
    }

    /// <summary>
    /// 设置销毁回调
    /// </summary>
    public void SetDestroyCallback(Action callback)
    {
        m_OnDestroyCallback = callback;
    }

    #endregion

    #region Unity生命周期

    private void Update()
    {
        if (!m_IsInitialized) return;

        // 更新生存时间
        m_LifetimeTimer += Time.deltaTime;
        if (m_LifetimeTimer >= m_MaxLifetime)
        {
            DestroyProjectile();
            return;
        }

        // 根据模式更新移动
        if (m_Mode == ProjectileMode.Direction)
        {
            // 方向模式：沿固定方向飞行
            float moveDistance = m_Speed * Time.deltaTime;
            transform.position += m_Direction * moveDistance;
        }
        else
        {
            // 目标模式：朝目标位置飞行
            UpdateTargetMode();
        }
    }

    /// <summary>
    /// 更新目标模式的移动
    /// </summary>
    private void UpdateTargetMode()
    {
        // 更新目标位置（追踪弹）
        if (m_IsHoming && m_TargetEntity != null && m_TargetEntity.CurrentState != ChessState.Dead)
        {
            // ⭐ 使用目标的中心位置，并启用日志调试
            Vector3 targetCenter = EntityPositionHelper.GetCenterPosition(m_TargetEntity, true);
            m_TargetPosition = targetCenter;

            //DebugEx.LogModule("ChessProjectile",$"追踪目标更新: {m_TargetEntity.Config?.Name}, 位置={targetCenter}");
        }

        // 计算方向和移动
        Vector3 direction = (m_TargetPosition - transform.position).normalized;
        float moveDistance = m_Speed * Time.deltaTime;

        // ⭐ 移除到达检测，让投射物通过碰撞来判断命中
        // 投射物会一直朝目标飞行，直到碰撞或超时
        transform.position += direction * moveDistance;

        // 只更新朝向，不检查距离
        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_IsInitialized) return;

        // 获取棋子实体
        ChessEntity target = other.GetComponent<ChessEntity>();
        if (target == null)
        {
            target = other.GetComponentInParent<ChessEntity>();
        }

        if (target == null) return;

        TryHitTarget(target);
    }

    /// <summary>
    /// 粒子系统碰撞回调
    /// </summary>
    private void OnParticleCollision(GameObject other)
    {
        if (!m_IsInitialized)
        {
            DebugEx.LogModule("ChessProjectile", "粒子碰撞被忽略：未初始化");
            return;
        }

        DebugEx.LogModule("ChessProjectile", $"粒子系统碰撞触发: {other.name}");

        // 注意：other 是被碰撞的对象
        // 尝试获取棋子实体
        ChessEntity target = other.GetComponent<ChessEntity>();
        if (target == null)
        {
            target = other.GetComponentInParent<ChessEntity>();
        }

        if (target == null)
        {
            DebugEx.LogModule("ChessProjectile",
                $"粒子碰撞到非棋子对象: {other.name}");
            return;
        }

        DebugEx.Success("ChessProjectile",
            $"粒子碰撞到棋子: {target.Config?.Name}");

        TryHitTarget(target);
    }

    private void OnDestroy()
    {
        m_OnDestroyCallback?.Invoke();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 尝试命中目标
    /// </summary>
    private void TryHitTarget(ChessEntity target)
    {
        // ✅ 使用阵营服务检查是否为敌人
        if (!CampRelationService.IsEnemy(m_OwnerCamp, target.Camp))
        {
            DebugEx.LogModule("ChessProjectile",

                // 第 310-340 行（需要替换）
                $"碰撞目标不是敌人，忽略: {target.Config?.Name}, 目标阵营={target.Camp}, 攻击者阵营={m_OwnerCamp}");
            return;
        }

        // 检查是否已命中过
        if (m_HitTargets.Contains(target))
        {
            DebugEx.LogModule("ChessProjectile",
                $"目标已被命中过，忽略: {target.Config?.Name}");
            return;
        }

        // 检查穿透数量
        if (m_CurrentPenetrationCount >= m_MaxPenetrationCount)
        {
            DebugEx.LogModule("ChessProjectile",
                $"已达到最大穿透数 ({m_MaxPenetrationCount})，投射物销毁");
            DestroyProjectile();
            return;
        }

        // 检查是否存活
        if (target.CurrentState == ChessState.Dead)
        {
            DebugEx.LogModule("ChessProjectile",
                $"目标已死亡，忽略: {target.Config?.Name}");
            return;
        }

        // 记录命中
        m_HitTargets.Add(target);
        m_CurrentPenetrationCount++;

        // ⭐ 通知检测器有目标被命中（由检测器负责伤害和 Buff 应用）
        m_OnHitCallback?.Invoke(target);

        DebugEx.Success("ChessProjectile",
            $"投射物命中: {target.Config?.Name}, 穿透计数={m_CurrentPenetrationCount}/{m_MaxPenetrationCount}");

        // 非穿透弹命中后销毁
        if (!m_IsPiercing && m_DestroyOnHit)
        {
            DestroyProjectile();
        }
    }

    /// <summary>
    /// 销毁投射物
    /// </summary>
    private void DestroyProjectile()
    {
        m_IsInitialized = false;
        Destroy(gameObject);
    }

    #endregion

    #region 调试可视化

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!m_ShowDebugGizmos) return;
        if (!m_IsInitialized) return;

        Vector3 currentPos = transform.position;

        // 根据模式绘制不同的可视化
        if (m_Mode == ProjectileMode.Target)
        {
            DrawTargetModeGizmos(currentPos);
        }
        else if (m_Mode == ProjectileMode.Direction)
        {
            DrawDirectionModeGizmos(currentPos);
        }

        // 绘制投射物当前位置标记
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentPos, 0.15f);
    }

    /// <summary>
    /// 绘制目标模式的 Gizmos
    /// </summary>
    private void DrawTargetModeGizmos(Vector3 currentPos)
    {
        // 绘制瞄准线
        if (m_CurrentPenetrationCount >= m_MaxPenetrationCount)
        {
            // 已达到最大穿透数 - 红色
            Gizmos.color = Color.red;
        }
        else if (m_IsHoming && m_TargetEntity != null && m_TargetEntity.CurrentState != ChessState.Dead)
        {
            // 追踪存活目标 - 绿色
            Gizmos.color = Color.green;
        }
        else
        {
            // 普通目标模式 - 青色
            Gizmos.color = Color.cyan;
        }

        // 绘制从当前位置到目标位置的线
        Gizmos.DrawLine(currentPos, m_TargetPosition);

        // 绘制目标点
        Gizmos.color = m_IsHoming ? Color.green : Color.cyan;
        Gizmos.DrawSphere(m_TargetPosition, m_TargetGizmoSize);

        // 如果是追踪弹，绘制目标实体信息
        if (m_IsHoming && m_TargetEntity != null)
        {
            // 绘制目标实体的边界框
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 半透明绿色
            Collider targetCollider = m_TargetEntity.GetComponent<Collider>();
            if (targetCollider != null)
            {
                Gizmos.DrawWireCube(targetCollider.bounds.center, targetCollider.bounds.size);
            }

            // 绘制从投射物到目标的方向箭头
            Vector3 direction = (m_TargetPosition - currentPos).normalized;
            DrawArrow(currentPos, currentPos + direction * 2f, Color.yellow);
        }

        // 绘制穿透信息文本
        UnityEditor.Handles.Label(
            currentPos + Vector3.up * 0.5f,
            $"穿透: {m_CurrentPenetrationCount}/{m_MaxPenetrationCount}\n" +
            $"速度: {m_Speed:F1}\n" +
            $"存活: {m_LifetimeTimer:F1}s"
        );
    }

    /// <summary>
    /// 绘制方向模式的 Gizmos
    /// </summary>
    private void DrawDirectionModeGizmos(Vector3 currentPos)
    {
        // 绘制飞行方向
        Gizmos.color = m_CurrentPenetrationCount >= m_MaxPenetrationCount ? Color.red : Color.magenta;

        // 绘制方向射线（长度为速度的2倍，便于观察）
        Vector3 endPoint = currentPos + m_Direction * (m_Speed * 2f);
        Gizmos.DrawLine(currentPos, endPoint);

        // 绘制方向箭头
        DrawArrow(currentPos, endPoint, Gizmos.color);

        // 绘制穿透信息
        UnityEditor.Handles.Label(
            currentPos + Vector3.up * 0.5f,
            $"方向: {m_Direction}\n" +
            $"穿透: {m_CurrentPenetrationCount}/{m_MaxPenetrationCount}\n" +
            $"速度: {m_Speed:F1}"
        );
    }

    /// <summary>
    /// 绘制箭头
    /// </summary>
    private void DrawArrow(Vector3 start, Vector3 end, Color color)
    {
        Gizmos.color = color;
        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 arrowHead1 = end - direction * 0.3f + right * 0.15f;
        Vector3 arrowHead2 = end - direction * 0.3f - right * 0.15f;

        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
    }
#endif

    #endregion
}
