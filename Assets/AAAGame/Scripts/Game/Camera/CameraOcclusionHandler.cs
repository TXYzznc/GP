using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 相机遮挡处理器
/// 检测并处理相机与角色之间的遮挡物，使其透明
/// </summary>
public class CameraOcclusionHandler : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("目标角色")]
    public Transform target;

    [Header("遮挡设置")]
    [Tooltip("淡出速度")]
    [SerializeField] private float fadeSpeed = 5f;
    [Tooltip("目标透明度")]
    [SerializeField] private float targetAlpha = 0.3f;
    [Tooltip("遮挡层级")]
    [SerializeField] private LayerMask occlusionLayers = -1;
    [Tooltip("最多同时淡出的物体数")]
    [SerializeField] private int maxFadedObjects = 10;

    private Camera m_Camera;
    private Dictionary<Renderer, MaterialData> m_FadedRenderers = new Dictionary<Renderer, MaterialData>();
    private List<Renderer> m_CurrentOccluders = new List<Renderer>();
    private List<Renderer> m_TempOccluders = new List<Renderer>();

    /// <summary>
    /// 材质数据，用于恢复原始状态
    /// </summary>
    private class MaterialData
    {
        public Material originalMaterial;
        public Material fadedMaterial;
        public float currentAlpha;
        public bool isFading;
    }

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
        if (m_Camera == null)
        {
            m_Camera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (target == null || m_Camera == null) return;

        // 检测遮挡
        DetectOcclusion();

        // 更新淡出效果
        UpdateFading();
    }

    /// <summary>
    /// 检测遮挡
    /// </summary>
    private void DetectOcclusion()
    {
        m_TempOccluders.Clear();

        Vector3 cameraPosition = m_Camera.transform.position;
        Vector3 targetPosition = target.position;
        Vector3 direction = targetPosition - cameraPosition;
        float distance = direction.magnitude;

        // 执行射线检测
        RaycastHit[] hits = Physics.RaycastAll(cameraPosition, direction.normalized, distance, occlusionLayers);

        foreach (RaycastHit hit in hits)
        {
            // 跳过目标本身
            if (hit.transform == target || hit.transform.IsChildOf(target))
                continue;

            // 获取渲染器
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && !m_TempOccluders.Contains(renderer))
            {
                m_TempOccluders.Add(renderer);
            }
        }

        // 更新当前遮挡物列表
        UpdateOccludersList();
    }

    /// <summary>
    /// 更新遮挡物列表
    /// </summary>
    private void UpdateOccludersList()
    {
        // 检查需要恢复的渲染器
        foreach (Renderer renderer in m_CurrentOccluders)
        {
            if (!m_TempOccluders.Contains(renderer))
            {
                // 不再遮挡，开始恢复
                if (m_FadedRenderers.ContainsKey(renderer))
                {
                    m_FadedRenderers[renderer].isFading = false;
                }
            }
        }

        // 检查新的遮挡物
        foreach (Renderer renderer in m_TempOccluders)
        {
            if (!m_CurrentOccluders.Contains(renderer))
            {
                // 新的遮挡物，开始淡出
                if (m_FadedRenderers.Count < maxFadedObjects)
                {
                    ApplyFade(renderer);
                }
            }
        }

        // 更新当前遮挡物列表
        m_CurrentOccluders.Clear();
        m_CurrentOccluders.AddRange(m_TempOccluders);
    }

    /// <summary>
    /// 应用淡出效果
    /// </summary>
    private void ApplyFade(Renderer renderer)
    {
        if (renderer == null) return;

        // 如果已经在淡出列表中，标记为淡出
        if (m_FadedRenderers.ContainsKey(renderer))
        {
            m_FadedRenderers[renderer].isFading = true;
            return;
        }

        try
        {
            // 创建材质数据
            MaterialData data = new MaterialData
            {
                originalMaterial = renderer.material,
                fadedMaterial = new Material(renderer.material),
                currentAlpha = 1f,
                isFading = true
            };

            // 设置材质为透明模式
            SetMaterialTransparent(data.fadedMaterial);

            // 应用淡出材质
            renderer.material = data.fadedMaterial;

            // 添加到字典
            m_FadedRenderers.Add(renderer, data);
        }
        catch (System.Exception ex)
        {
            Log.Warning($"应用淡出效果失败: {renderer.name}, Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新淡出效果
    /// </summary>
    private void UpdateFading()
    {
        List<Renderer> renderersToRemove = new List<Renderer>();

        foreach (var kvp in m_FadedRenderers)
        {
            Renderer renderer = kvp.Key;
            MaterialData data = kvp.Value;

            if (renderer == null)
            {
                renderersToRemove.Add(renderer);
                continue;
            }

            // 计算目标透明度
            float targetAlphaValue = data.isFading ? targetAlpha : 1f;

            // 平滑过渡
            data.currentAlpha = Mathf.Lerp(data.currentAlpha, targetAlphaValue, fadeSpeed * Time.deltaTime);

            // 应用透明度
            Color color = data.fadedMaterial.color;
            color.a = data.currentAlpha;
            data.fadedMaterial.color = color;

            // 如果已经完全恢复，移除
            if (!data.isFading && Mathf.Abs(data.currentAlpha - 1f) < 0.01f)
            {
                // 恢复原始材质
                renderer.material = data.originalMaterial;
                
                // 销毁淡出材质
                if (data.fadedMaterial != null)
                {
                    Destroy(data.fadedMaterial);
                }

                renderersToRemove.Add(renderer);
            }
        }

        // 移除已恢复的渲染器
        foreach (Renderer renderer in renderersToRemove)
        {
            m_FadedRenderers.Remove(renderer);
        }
    }

    /// <summary>
    /// 设置材质为透明模式
    /// </summary>
    private void SetMaterialTransparent(Material material)
    {
        // 设置渲染模式为透明
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    /// <summary>
    /// 清除所有淡出效果
    /// </summary>
    public void ClearAllFades()
    {
        foreach (var kvp in m_FadedRenderers)
        {
            Renderer renderer = kvp.Key;
            MaterialData data = kvp.Value;

            if (renderer != null)
            {
                // 恢复原始材质
                renderer.material = data.originalMaterial;
            }

            // 销毁淡出材质
            if (data.fadedMaterial != null)
            {
                Destroy(data.fadedMaterial);
            }
        }

        m_FadedRenderers.Clear();
        m_CurrentOccluders.Clear();
    }

    private void OnDestroy()
    {
        // 清除所有淡出效果
        ClearAllFades();
    }

    private void OnDisable()
    {
        // 禁用时清除
        ClearAllFades();
    }
}
