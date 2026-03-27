using UnityEngine;

[CreateAssetMenu(fileName = "OutlineConfig", menuName = "Rendering/Outline Config", order = 1)]
public class OutlineConfig : ScriptableObject
{
    [Header("外轮廓颜色")]
    public Color OutlineColor = Color.white;
    
    [Header("外轮廓宽度")]
    [Range(1f, 100f)]
    public float OutlineSize = 20f;
    
    [Header("距离配置")]
    [Tooltip("启用基于距离的宽度调整")]
    public bool EnableDistanceScaling = true;
    
    [Tooltip("最小距离（此距离内保持最大宽度）")]
    [Range(0f, 50f)]
    public float MinDistance = 5f;
    
    [Tooltip("最大距离（超过此距离不渲染外轮廓）")]
    [Range(1f, 200f)]
    public float MaxDistance = 50f;
    
    [Tooltip("距离衰减曲线")]
    public AnimationCurve DistanceFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0.2f);
    
    [Header("刷新设置")]
    [Tooltip("是否自动刷新（根据距离变化）")]
    public bool AutoRefresh = true;
    
    [Tooltip("刷新频率（每秒检查次数）")]
    [Range(1, 60)]
    public int RefreshRate = 5;
    
    [Header("启动配置")]
    [Tooltip("是否在启动时自动应用")]
    public bool AutoApplyOnStart = true;
    
    /// <summary>
    /// 根据距离计算外轮廓宽度
    /// </summary>
    public float CalculateOutlineSize(float distance)
    {
        if (!EnableDistanceScaling)
            return OutlineSize;
        
        if (distance <= MinDistance)
            return OutlineSize;
        
        if (distance >= MaxDistance)
            return 0f;
        
        // 归一化距离 (0-1)
        float normalizedDistance = (distance - MinDistance) / (MaxDistance - MinDistance);
        
        // 使用曲线计算衰减
        float falloff = DistanceFalloffCurve.Evaluate(normalizedDistance);
        
        return OutlineSize * falloff;
    }
    
    /// <summary>
    /// 检查距离是否在渲染范围内
    /// </summary>
    public bool IsInRenderRange(float distance)
    {
        if (!EnableDistanceScaling)
            return true;
        
        return distance < MaxDistance;
    }
}