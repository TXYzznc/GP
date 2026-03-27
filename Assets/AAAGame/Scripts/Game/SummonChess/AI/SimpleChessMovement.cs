using System;
using UnityEngine;

/// <summary>
/// 简单棋子移动实现
/// 使用MoveTowards简单移动，后续可替换为NavMesh版本
/// </summary>
public class SimpleChessMovement : MonoBehaviour, IChessMovement
{
    #region 私有字段

    /// <summary>目标位置</summary>
    private Vector3 m_TargetPosition;

    /// <summary>是否正在移动</summary>
    private bool m_IsMoving;

    /// <summary>移动速度</summary>
    private float m_MoveSpeed = 3f;

    /// <summary>到达判定距离</summary>
    private const float ARRIVE_THRESHOLD = 0.3f;

    #endregion

    #region 事件

    /// <summary>
    /// 到达目标位置事件
    /// </summary>
    public event Action OnArrived;

    #endregion

    #region IChessMovement 实现

    public bool IsMoving => m_IsMoving;

    public float MoveSpeed
    {
        get => m_MoveSpeed;
        set => m_MoveSpeed = Mathf.Max(0.1f, value);
    }

    public void MoveTo(Vector3 targetPosition)
    {
        m_TargetPosition = targetPosition;
        m_IsMoving = true;

        DebugEx.LogModule(
            "SimpleChessMovement",
            $"{gameObject.name} 开始移动到目标位置: {targetPosition}, 当前位置: {transform.position}"
        );

        // 面向目标方向（仅转Y轴）
        Vector3 lookDir = targetPosition - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    public void Stop()
    {
        m_IsMoving = false;
    }

    public void Tick(float deltaTime)
    {
        if (!m_IsMoving)
            return;

        // MoveTowards移动
        Vector3 newPos = Vector3.MoveTowards(
            transform.position,
            m_TargetPosition,
            m_MoveSpeed * deltaTime
        );
        transform.position = newPos;

        // 到达判定
        float distSqr = (transform.position - m_TargetPosition).sqrMagnitude;
        float distance = Mathf.Sqrt(distSqr);

        if (distSqr <= ARRIVE_THRESHOLD * ARRIVE_THRESHOLD)
        {
            m_IsMoving = false;

            DebugEx.LogModule(
                "SimpleChessMovement",
                $"{gameObject.name} 到达目标位置，距离={distance:F3}, 阈值={ARRIVE_THRESHOLD}"
            );

            // 触发到达事件
            OnArrived?.Invoke();
        }
    }

    #endregion

    #region 清理

    private void OnDestroy()
    {
        OnArrived = null;
    }

    #endregion
}
