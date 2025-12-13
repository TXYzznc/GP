using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 轻量级外轮廓组件 - 只负责渲染，不做任何检测
/// 所有逻辑由OutlineDisplayManager统一管理
/// </summary>
public class OutlineTest : MonoBehaviour
{
    [Header("外轮廓配置")]
    public OutlineConfig OutlineConfig;

    [Header("运行时信息（只读）")]
    [SerializeField] private bool _isOutlineActive;
    [SerializeField] private float _currentOutlineSize;

    private List<Renderer> _renderers = new List<Renderer>();
    private Transform _cachedTransform;

    public bool IsOutlineActive => _isOutlineActive;
    public List<Renderer> Renderers => _renderers;

    void Awake()
    {
        _cachedTransform = transform;
        CacheRenderers();
    }

    /// <summary>
    /// 缓存所有Renderer
    /// </summary>
    private void CacheRenderers()
    {
        _renderers.Clear();
        _renderers = _cachedTransform.GetComponentsInChildren<Renderer>().ToList();
    }

    /// <summary>
    /// 应用外轮廓（由Manager调用）
    /// </summary>
    public void ApplyOutline(OutlineConfig config, float outlineSize)
    {
        if (_renderers.Count == 0)
        {
            CacheRenderers();
            if (_renderers.Count == 0)
                return;
        }

        OutlineConfig = config;
        _currentOutlineSize = outlineSize;

        OutlineRenderFeature.Instance?.DrawOrUpdateOutlines(
            _renderers, 
            config.OutlineColor, 
            outlineSize
        );

        _isOutlineActive = true;
    }

    /// <summary>
    /// 更新外轮廓参数（由Manager调用）
    /// </summary>
    public void UpdateOutline(OutlineConfig config, float outlineSize)
    {
        if (!_isOutlineActive || _renderers.Count == 0)
            return;

        OutlineConfig = config;
        _currentOutlineSize = outlineSize;

        OutlineRenderFeature.Instance?.DrawOrUpdateOutlines(
            _renderers,
            config.OutlineColor,
            outlineSize
        );
    }

    /// <summary>
    /// 移除外轮廓（由Manager调用）
    /// </summary>
    public void RemoveOutline()
    {
        if (!_isOutlineActive || _renderers.Count == 0)
            return;

        OutlineRenderFeature.Instance?.RemoveDrawOutlines(_renderers);
        _isOutlineActive = false;
    }

    /// <summary>
    /// 刷新Renderer列表（对象层级变化时调用）
    /// </summary>
    public void RefreshRenderers()
    {
        bool wasActive = _isOutlineActive;
        if (wasActive)
        {
            RemoveOutline();
        }

        CacheRenderers();

        if (wasActive && OutlineConfig != null)
        {
            ApplyOutline(OutlineConfig, _currentOutlineSize);
        }
    }

    void OnDestroy()
    {
        RemoveOutline();
    }
}