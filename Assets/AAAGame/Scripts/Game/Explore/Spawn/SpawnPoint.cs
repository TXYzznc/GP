using UnityEngine;

/// <summary>
/// 场景生成点标记
/// 在场景中放置此组件标记敌人/宝箱的生成位置
/// </summary>
public enum SpawnPointType
{
    Enemy,
    TreasureBox,
}

public class SpawnPoint : MonoBehaviour
{
    [Header("生成配置")]
    [SerializeField]
    private SpawnPointType m_Type = SpawnPointType.Enemy;

    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("随机偏移半径")]
    private float m_Radius = 1f;

    [SerializeField]
    [Range(1f, 20f)]
    [Tooltip("NavMesh 采样范围")]
    private float m_NavSampleRadius = 5f;

    public SpawnPointType Type => m_Type;
    public float Radius => m_Radius;
    public float NavSampleRadius => m_NavSampleRadius;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 绘制生成点
        Gizmos.color = m_Type == SpawnPointType.Enemy ? new Color(1, 0, 0, 0.7f) : new Color(1, 1, 0, 0.7f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        // 绘制随机偏移范围
        Gizmos.color = new Color(1, 1, 1, 0.3f);
        DrawCircle(transform.position, m_Radius, 16);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }
#endif
}
