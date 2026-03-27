using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 溶解效果控制器 - 控制单个物体的材质溶解动画
/// </summary>
public class DissolveController : MonoBehaviour
{
    #region 常量

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    #endregion

    #region 私有字段

    private List<Material> m_Materials = new List<Material>();
    private float m_CurrentAmount = 0f;
    private float m_TargetAmount = 0f;
    private float m_Speed = 1f;
    private bool m_IsAnimating = false;
    private System.Action m_OnComplete;

    #endregion

    #region 公共属性

    /// <summary>当前溶解程度 (0=完全显示, 1=完全溶解)</summary>
    public float CurrentAmount => m_CurrentAmount;

    /// <summary>是否正在播放动画</summary>
    public bool IsAnimating => m_IsAnimating;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        CollectMaterials();
    }

    private void Update()
    {
        if (!m_IsAnimating) return;

        // 平滑过渡到目标值
        m_CurrentAmount = Mathf.MoveTowards(m_CurrentAmount, m_TargetAmount, m_Speed * Time.deltaTime);
        ApplyDissolveAmount(m_CurrentAmount);

        // 检查是否完成
        if (Mathf.Approximately(m_CurrentAmount, m_TargetAmount))
        {
            m_IsAnimating = false;
            m_OnComplete?.Invoke();
            m_OnComplete = null;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 收集所有 Renderer 的材质
    /// </summary>
    public void CollectMaterials()
    {
        m_Materials.Clear();
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            // 使用材质实例，避免修改共享材质
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty(DissolveAmountId))
                {
                    m_Materials.Add(mat);
                }
            }
        }
    }

    /// <summary>
    /// 立即设置溶解程度（无动画）
    /// </summary>
    public void SetDissolveAmount(float amount)
    {
        m_CurrentAmount = Mathf.Clamp01(amount);
        m_TargetAmount = m_CurrentAmount;
        m_IsAnimating = false;
        ApplyDissolveAmount(m_CurrentAmount);
    }

    /// <summary>
    /// 播放溶解动画
    /// </summary>
    /// <param name="targetAmount">目标溶解程度 (0=显示, 1=隐藏)</param>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="onComplete">完成回调</param>
    public void AnimateTo(float targetAmount, float duration, System.Action onComplete = null)
    {
        m_TargetAmount = Mathf.Clamp01(targetAmount);
        m_Speed = duration > 0 ? 1f / duration : 100f;
        m_OnComplete = onComplete;
        m_IsAnimating = true;
    }

    /// <summary>
    /// 溶解隐藏（从当前状态溶解到完全隐藏）
    /// </summary>
    public void DissolveOut(float duration, System.Action onComplete = null)
    {
        AnimateTo(1f, duration, onComplete);
    }

    /// <summary>
    /// 溶解显示（从当前状态溶解到完全显示）
    /// </summary>
    public void DissolveIn(float duration, System.Action onComplete = null)
    {
        AnimateTo(0f, duration, onComplete);
    }

    /// <summary>
    /// 停止动画
    /// </summary>
    public void StopAnimation()
    {
        m_IsAnimating = false;
        m_OnComplete = null;
    }

    #endregion

    #region 私有方法

    private void ApplyDissolveAmount(float amount)
    {
        foreach (var mat in m_Materials)
        {
            if (mat != null)
            {
                mat.SetFloat(DissolveAmountId, amount);
            }
        }
    }

    #endregion
}
