using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 目标高亮管理器（预留，暂未使用）
/// </summary>
public class TargetHighlightManager : MonoBehaviour
{
    #region 单例

    private static TargetHighlightManager s_Instance;
    public static TargetHighlightManager Instance => s_Instance;

    #endregion

    #region 字段

    private List<ChessEntity> m_HighlightedTargets = new List<ChessEntity>();
    private Dictionary<ChessEntity, Color> m_OriginalColors = new Dictionary<ChessEntity, Color>();

    private const float HIGHLIGHT_COLOR_BRIGHTNESS = 1.5f;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
            DebugEx.LogModule("TargetHighlightManager", "初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
            ClearHighlight();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 高亮显示目标
    /// </summary>
    public void HighlightTargets(List<ChessEntity> targets)
    {
        ClearHighlight();

        if (targets == null || targets.Count == 0)
            return;

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
                continue;

            // 保存原始颜色
            m_OriginalColors[target] = renderer.color;

            // 应用高亮色（黄色）
            renderer.color = Color.yellow;

            m_HighlightedTargets.Add(target);
        }

        DebugEx.LogModule("TargetHighlightManager", $"高亮 {m_HighlightedTargets.Count} 个目标");
    }

    /// <summary>
    /// 清除所有高亮
    /// </summary>
    public void ClearHighlight()
    {
        foreach (var target in m_HighlightedTargets)
        {
            if (target == null)
                continue;

            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer && m_OriginalColors.TryGetValue(target, out var originalColor))
            {
                renderer.color = originalColor;
            }
        }

        m_HighlightedTargets.Clear();
        m_OriginalColors.Clear();
    }

    #endregion
}
