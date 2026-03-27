using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Dash技能道具控制器
/// 管理道具的状态：等待 → 飞行 → 销毁
/// </summary>
public class DashItemController : MonoBehaviour
{
    #region 状态枚举

    public enum DashItemState
    {
        Waiting,    // 等待投掷（跟随手部）
        Flying,     // 飞行中（已投掷）
        Destroyed   // 已销毁
    }

    #endregion

    #region 私有字段

    private DashItemState m_State = DashItemState.Waiting;
    private float m_StateTimer = 0f;
    private float m_WaitingDuration = 3f;
    private float m_FlyingDuration = 3f;
    private float m_GravityScale = 1.0f;  // 重力缩放
    private Rigidbody m_Rigidbody;
    private Collider m_Collider;
    private Transform m_HandTransform;  // 手部挂点
    private Collider[] m_IgnoredColliders;  // 需要忽略的碰撞体列表

    // 回调事件
    private Action m_OnThrown;    // 投掷时回调
    private Action m_OnDestroyed; // 销毁时回调

    // 轨迹预测
    private Vector3 m_PredictedDirection;  // 预测投掷方向
    private float m_PredictedForce;        // 预测投掷力度
    private List<Vector3> m_TrajectoryPoints = new List<Vector3>();  // 轨迹点

    #endregion

    #region 公共属性

    /// <summary>
    /// 当前状态
    /// </summary>
    public DashItemState State => m_State;

    /// <summary>
    /// 是否已销毁
    /// </summary>
    public bool IsDestroyed => m_State == DashItemState.Destroyed;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化道具
    /// </summary>
    public void Initialize(
        Transform handTransform,
        float waitingDuration,
        float flyingDuration,
        bool enableCollision,
        float gravityScale,
        GameObject playerGameObject = null,
        Action onThrown = null,
        Action onDestroyed = null)
    {
        m_HandTransform = handTransform;
        m_WaitingDuration = waitingDuration;
        m_FlyingDuration = flyingDuration;
        m_GravityScale = gravityScale;
        m_OnThrown = onThrown;
        m_OnDestroyed = onDestroyed;

        // 获取组件
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<Collider>();

        // 初始化刚体设置
        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = true;
            m_Rigidbody.useGravity = false;
        }

        // 设置碰撞
        if (m_Collider != null)
        {
            m_Collider.enabled = enableCollision;
        }

        // 忽略与玩家的碰撞
        if (playerGameObject != null && m_Collider != null)
        {
            IgnorePlayerCollision(playerGameObject);
        }

        // 进入等待状态
        m_State = DashItemState.Waiting;
        m_StateTimer = 0f;

        DebugEx.Log($"[DashItem] 初始化完成，等待投掷（{m_WaitingDuration}秒），重力缩放={m_GravityScale}");
    }

    /// <summary>
    /// 忽略与玩家的碰撞
    /// </summary>
    private void IgnorePlayerCollision(GameObject playerGameObject)
    {
        m_IgnoredColliders = playerGameObject.GetComponentsInChildren<Collider>();

        if (m_IgnoredColliders == null || m_IgnoredColliders.Length == 0)
        {
            DebugEx.Warning("[DashItem] 玩家身上未找到碰撞体");
            return;
        }

        foreach (var playerCollider in m_IgnoredColliders)
        {
            if (playerCollider != null && playerCollider.enabled)
            {
                Physics.IgnoreCollision(m_Collider, playerCollider, true);
            }
        }

        DebugEx.Log($"[DashItem] 已忽略与玩家的 {m_IgnoredColliders.Length} 个碰撞体");
    }

    #endregion

    #region Unity 生命周期

    private void Update()
    {
        if (m_State == DashItemState.Destroyed)
            return;

        // 更新计时器
        m_StateTimer += Time.deltaTime;

        // 状态逻辑
        switch (m_State)
        {
            case DashItemState.Waiting:
                UpdateWaitingState();
                break;

            case DashItemState.Flying:
                UpdateFlyingState();
                break;
        }
    }

    private void FixedUpdate()
    {
        // 在 FixedUpdate 中手动施加自定义重力（仅飞行状态）
        if (m_State == DashItemState.Flying && m_Rigidbody != null && !m_Rigidbody.isKinematic)
        {
            Vector3 customGravity = Physics.gravity * m_GravityScale;
            m_Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
        }
    }

    #endregion

    #region 状态更新

    /// <summary>
    /// 更新等待状态
    /// </summary>
    private void UpdateWaitingState()
    {
        // 跟随手部位置
        if (m_HandTransform != null)
        {
            transform.position = m_HandTransform.position;
            transform.rotation = m_HandTransform.rotation;
        }

        // 超时检测
        if (m_StateTimer >= m_WaitingDuration)
        {
            DebugEx.Log("[DashItem] 等待超时，自动销毁");
            DestroyItem();
        }
    }

    /// <summary>
    /// 更新飞行状态
    /// </summary>
    private void UpdateFlyingState()
    {
        // 超时检测
        if (m_StateTimer >= m_FlyingDuration)
        {
            DebugEx.Log("[DashItem] 飞行超时，自动销毁");
            DestroyItem();
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 设置预测的轨迹（用于可视化）
    /// </summary>
    /// <param name="direction">投掷方向</param>
    /// <param name="force">投掷力度</param>
    public void SetPredictedTrajectory(Vector3 direction, float force)
    {
        m_PredictedDirection = direction;
        m_PredictedForce = force;

        // 计算轨迹点
        CalculateTrajectory();
    }

    /// <summary>
    /// 投掷道具
    /// </summary>
    public void ThrowItem(Vector3 direction, float force)
    {
        if (m_State != DashItemState.Waiting)
        {
            DebugEx.Warning("[DashItem] 只能在等待状态下投掷");
            return;
        }

        // 切换到飞行状态
        m_State = DashItemState.Flying;
        m_StateTimer = 0f;

        // 启用物理
        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.useGravity = false;
            m_Rigidbody.velocity = direction * force;
        }

        // 断开与手部的连接
        m_HandTransform = null;

        // 清空轨迹预测（已经投掷，不需要预测）
        m_TrajectoryPoints.Clear();

        DebugEx.Log($"[DashItem] 投掷道具，方向={direction}, 力度={force}, 重力缩放={m_GravityScale}");

        // 触发投掷回调
        m_OnThrown?.Invoke();
    }

    /// <summary>
    /// 获取当前位置
    /// </summary>
    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// 销毁道具
    /// </summary>
    public void DestroyItem()
    {
        if (m_State == DashItemState.Destroyed)
            return;

        m_State = DashItemState.Destroyed;

        // 恢复忽略的碰撞
        RestorePlayerCollision();

        // 触发销毁回调
        m_OnDestroyed?.Invoke();

        Destroy(gameObject);
        DebugEx.Log("[DashItem] 道具已销毁");
    }

    /// <summary>
    /// 恢复与玩家的碰撞
    /// </summary>
    private void RestorePlayerCollision()
    {
        if (m_IgnoredColliders == null || m_Collider == null)
            return;

        foreach (var playerCollider in m_IgnoredColliders)
        {
            if (playerCollider != null)
            {
                Physics.IgnoreCollision(m_Collider, playerCollider, false);
            }
        }
    }

    #endregion

    #region 轨迹预测

    /// <summary>
    /// 计算投掷轨迹
    /// </summary>
    private void CalculateTrajectory()
    {
        m_TrajectoryPoints.Clear();

        if (m_State != DashItemState.Waiting)
            return;

        Vector3 startPos = transform.position;
        Vector3 velocity = m_PredictedDirection * m_PredictedForce;
        Vector3 gravity = Physics.gravity * m_GravityScale;

        float timeStep = 0.1f;  // 每0.1秒采样一次
        float maxTime = m_FlyingDuration;  // 最多预测到飞行时长结束
        int maxPoints = 50;  // 最多50个点

        for (int i = 0; i < maxPoints; i++)
        {
            float t = i * timeStep;
            if (t > maxTime)
                break;

            // 抛物线公式：position = startPos + velocity * t + 0.5 * gravity * t^2
            Vector3 point = startPos + velocity * t + 0.5f * gravity * t * t;
            m_TrajectoryPoints.Add(point);

            // 如果落到地面，停止预测
            if (point.y < 0f)
                break;
        }
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 绘制当前速度方向（飞行状态）
        if (m_State == DashItemState.Flying && m_Rigidbody != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, m_Rigidbody.velocity.normalized * 2f);
        }

        // 绘制预测轨迹（等待状态）
        if (m_State == DashItemState.Waiting && m_TrajectoryPoints.Count > 1)
        {
            Gizmos.color = Color.cyan;

            // 绘制轨迹线
            for (int i = 0; i < m_TrajectoryPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(m_TrajectoryPoints[i], m_TrajectoryPoints[i + 1]);
            }

            // 绘制轨迹点
            foreach (var point in m_TrajectoryPoints)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }

            // 绘制落点（最后一个点）
            if (m_TrajectoryPoints.Count > 0)
            {
                Gizmos.color = Color.red;
                Vector3 landingPoint = m_TrajectoryPoints[m_TrajectoryPoints.Count - 1];
                Gizmos.DrawSphere(landingPoint, 0.3f);
            }
        }
    }
#endif
}
