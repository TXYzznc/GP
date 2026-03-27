using UnityEngine;

/// <summary>
/// 敌人视野检测器
/// 实现周围圈检测 + 前方扇形检测
/// 驱动警觉度系统（0-1浮点数）
/// </summary>
public class VisionConeDetector : MonoBehaviour
{
    #region 私有字段

    /// <summary>所属敌人实体</summary>
    private EnemyEntity m_Entity;

    /// <summary>当前警觉度（0-1）</summary>
    private float m_AlertLevel;

    /// <summary>上次更新检测的时间</summary>
    private float m_LastDetectionUpdateTime;

    /// <summary>玩家距离</summary>
    private float m_PlayerDistance;

    /// <summary>玩家是否在周围圈内</summary>
    private bool m_PlayerInCircle;

    /// <summary>玩家是否在扇形内</summary>
    private bool m_PlayerInCone;

    /// <summary>缓存的玩家Transform</summary>
    private Transform m_PlayerTransform;

    /// <summary>缓存的玩家战后隐身组件</summary>
    private PostCombatStealth m_PlayerStealth;

    /// <summary>用于OverlapSphere检测的缓存数组</summary>
    private Collider[] m_OverlapResults = new Collider[10];

    #endregion

    #region 属性

    /// <summary>当前警觉度（0-1）</summary>
    public float AlertLevel => m_AlertLevel;

    /// <summary>玩家是否在检测范围内</summary>
    public bool PlayerInDetectionRange => m_PlayerInCircle || m_PlayerInCone;

    #endregion

    #region Unity生命周期

    private void Awake()
    {
        m_Entity = GetComponent<EnemyEntity>();
        if (m_Entity == null)
        {
            DebugEx.ErrorModule("VisionConeDetector", $"{gameObject.name} 上未找到EnemyEntity组件");
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || m_Entity == null) return;

        EnemyEntityTable config = m_Entity.Config;
        if (config == null) return;

        // 绘制周围圈检测范围（绿色）
        Gizmos.color = Color.green;
        DrawGizmoCircle(transform.position, config.VisionCircleRadius, 16);

        // 绘制扇形视野检测范围（黄色）
        Gizmos.color = Color.yellow;
        DrawGizmoVisionCone(transform.position, transform.forward, config.VisionConeAngle, config.VisionConeDistance, 12);

        // 如果玩家在范围内，绘制连接线（红色）
        if (m_PlayerTransform != null && PlayerInDetectionRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, m_PlayerTransform.position);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化检测器
    /// </summary>
    public void Initialize()
    {
        m_AlertLevel = 0f;
        m_LastDetectionUpdateTime = Time.time;

        DebugEx.LogModule("VisionConeDetector", $"{m_Entity.Config.Name} 检测器初始化完成");
    }

    /// <summary>
    /// 重置警觉度为 0（战斗结束后调用，防止玩家一返回就立刻被追击）
    /// </summary>
    public void ResetAlert()
    {
        m_AlertLevel = 0f;
        EnemyAlertUIManager.Instance?.HideAlert(m_Entity);
    }

    /// <summary>
    /// 更新检测（应该每帧调用，但检测逻辑会限制频率）
    /// </summary>
    public void UpdateDetection(Transform playerTransform, float deltaTime)
    {
        if (playerTransform == null)
        {
            m_PlayerTransform = null;
            DecayAlertLevel(deltaTime);
            return;
        }

        if (m_Entity?.Config == null)
        {
            return;
        }

        m_PlayerTransform = playerTransform;

        // 缓存隐身组件（Transform 变化时重新获取）
        if (m_PlayerStealth == null)
            m_PlayerStealth = playerTransform.GetComponent<PostCombatStealth>();

        // 保底视野屏蔽（溶解过渡期）或玩家处于战后隐身时，忽略检测，衰减警觉度
        if ((EnemyEntityManager.Instance != null && EnemyEntityManager.Instance.IsDetectionBlocked)
            || (m_PlayerStealth != null && m_PlayerStealth.IsActive))
        {
            m_PlayerInCircle = false;
            m_PlayerInCone = false;
            DecayAlertLevel(deltaTime);
            EnemyAlertUIManager.Instance?.HideAlert(m_Entity);
            return;
        }

        // 计算与玩家的距离
        m_PlayerDistance = Vector3.Distance(transform.position, playerTransform.position);

        // 检查周围圈检测
        m_PlayerInCircle = m_PlayerDistance <= m_Entity.Config.VisionCircleRadius;

        // 检查扇形检测
        m_PlayerInCone = IsInVisionCone(playerTransform.position);

        // 根据检测范围更新警觉度
        if (m_PlayerInCone)
        {
            // 在扇形内快速增长
            IncreaseAlertLevel(m_Entity.Config.AlertIncreaseRate * 1.5f, deltaTime);
        }
        else if (m_PlayerInCircle)
        {
            // 在圈内慢速增长
            IncreaseAlertLevel(m_Entity.Config.AlertIncreaseRate, deltaTime);
        }
        else
        {
            // 超出范围衰减
            DecayAlertLevel(deltaTime);
        }

        // 通知UI管理器（警觉度>0.1f时显示UI）
        if (m_AlertLevel > 0.1f)
        {
            EnemyAlertUIManager.Instance?.ShowOrUpdateAlert(m_Entity, m_AlertLevel);
        }
        else if (m_AlertLevel <= 0f)
        {
            EnemyAlertUIManager.Instance?.HideAlert(m_Entity);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 判断玩家是否在视野锥内
    /// </summary>
    private bool IsInVisionCone(Vector3 playerPos)
    {
        if (m_Entity?.Config == null)
            return false;

        // 检查距离
        if (m_PlayerDistance > m_Entity.Config.VisionConeDistance)
            return false;

        // 检查角度（向前方向与到玩家的方向的夹角）
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0; // 忽略高度差

        if (toPlayer.sqrMagnitude < 0.01f) // 距离太近，直接返回true
            return true;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        return angle <= m_Entity.Config.VisionConeAngle * 0.5f; // 使用半角（因为VisionConeAngle是总角度）
    }

    /// <summary>
    /// 增加警觉度
    /// </summary>
    private void IncreaseAlertLevel(float increaseRate, float deltaTime)
    {
        m_AlertLevel += increaseRate * deltaTime;
        m_AlertLevel = Mathf.Min(m_AlertLevel, 1f); // 最多1.0
    }

    /// <summary>
    /// 衰减警觉度
    /// </summary>
    private void DecayAlertLevel(float deltaTime)
    {
        if (m_Entity?.Config == null)
            return;

        float decreaseRate = m_Entity.Config.AlertDecreaseRate;
        m_AlertLevel -= decreaseRate * deltaTime;
        m_AlertLevel = Mathf.Max(m_AlertLevel, 0f); // 最少0
    }

    /// <summary>
    /// Gizmos辅助方法：绘制圆形
    /// </summary>
    private void DrawGizmoCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    /// <summary>
    /// Gizmos辅助方法：绘制视野锥
    /// </summary>
    private void DrawGizmoVisionCone(Vector3 origin, Vector3 direction, float coneAngle, float coneDistance, int segments)
    {
        float halfAngle = coneAngle * 0.5f;
        float angleStep = coneAngle / segments;

        // 绘制锥的两条边界线
        Vector3 leftDir = Quaternion.AngleAxis(halfAngle, Vector3.up) * direction;
        Vector3 rightDir = Quaternion.AngleAxis(-halfAngle, Vector3.up) * direction;

        Gizmos.DrawLine(origin, origin + leftDir * coneDistance);
        Gizmos.DrawLine(origin, origin + rightDir * coneDistance);

        // 绘制锥的弧线
        Vector3 prevPoint = origin + leftDir * coneDistance;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = halfAngle - angleStep * i;
            Vector3 currentDir = Quaternion.AngleAxis(currentAngle, Vector3.up) * direction;
            Vector3 currentPoint = origin + currentDir * coneDistance;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    #endregion
}
