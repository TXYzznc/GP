using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 通用描边控制组件
/// 挂载在需要描边效果的物体上，提供显示/隐藏描边的统一 API
/// </summary>
public class OutlineController : MonoBehaviour
{
    #region 描边颜色常量

    /// <summary>选中描边颜色（黄色）</summary>
    public static readonly Color SelectionColor = new Color(1f, 0.85f, 0f);

    /// <summary>己方阵营描边颜色（绿色）</summary>
    public static readonly Color AllyColor = Color.green;

    /// <summary>敌方阵营描边颜色（红色）</summary>
    public static readonly Color EnemyColor = Color.red;

    /// <summary>默认描边宽度</summary>
    public static readonly float DefaultSize = 20f;

    #endregion

    #region 私有字段

    private List<Renderer> m_Renderers = new List<Renderer>();
    private bool m_IsOutlineActive;

    #endregion

    #region 公共属性

    /// <summary>描边是否正在显示</summary>
    public bool IsOutlineActive => m_IsOutlineActive;

    #endregion

    #region Unity 生命周期

    void Awake()
    {
        CacheRenderers();
    }

    void OnDestroy()
    {
        HideOutline();
    }

    #endregion

    #region 公共 API

    /// <summary>
    /// 显示或更新描边
    /// </summary>
    /// <param name="color">描边颜色</param>
    /// <param name="size">描边宽度</param>
    public void ShowOutline(Color color, float size)
    {
        if (m_Renderers.Count == 0)
        {
            CacheRenderers();
            if (m_Renderers.Count == 0)
                return;
        }

        OutlineRenderFeature.Instance?.DrawOrUpdateOutlines(m_Renderers, color, size);
        m_IsOutlineActive = true;
    }

    /// <summary>
    /// 隐藏描边
    /// </summary>
    public void HideOutline()
    {
        if (!m_IsOutlineActive || m_Renderers.Count == 0)
            return;

        OutlineRenderFeature.Instance?.RemoveDrawOutlines(m_Renderers);
        m_IsOutlineActive = false;
    }

    /// <summary>
    /// 刷新 Renderer 列表（模型变化时调用）
    /// </summary>
    public void RefreshRenderers()
    {
        bool wasActive = m_IsOutlineActive;
        if (wasActive)
        {
            HideOutline();
        }

        CacheRenderers();
    }

    #endregion

    #region 私有方法

    private void CacheRenderers()
    {
        m_Renderers.Clear();
        m_Renderers = transform.GetComponentsInChildren<Renderer>().ToList();
    }

    #endregion
}
