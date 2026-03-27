using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 棋子Ghost预览
/// 在放置过程中显示棋子预览(不修改参数,保持原样)
/// </summary>
public class ChessGhostPreview : MonoBehaviour
{
    #region 私有字段

    /// <summary>当前预览实例</summary>
    private GameObject m_PreviewInstance;

    /// <summary>底部偏移量（用于对齐底部到目标位置）</summary>
    private float m_BottomOffset;


    #endregion

    #region 属性

    /// <summary>是否正在显示预览</summary>
    public bool IsShowing => m_PreviewInstance != null && m_PreviewInstance.activeSelf;

    #endregion

    #region 公共接口

    /// <summary>
    /// 显示预览
    /// </summary>
    /// <param name="prefab">棋子预制体</param>
    /// <param name="position">初始位置</param>
    public void Show(GameObject prefab, Vector3 position)
    {
        Hide();

        if (prefab == null) return;

        // 先在原点生成，用于计算底部偏移
        m_PreviewInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        m_PreviewInstance.name = "ChessGhostPreview";

        // 禁用所有非渲染组件(Collider和脚本保留)
        DisableNonVisualComponents(m_PreviewInstance);

        // 计算底部偏移量
        m_BottomOffset = EntityPositionHelper.CalculateBottomOffset(m_PreviewInstance);

        // 应用底部对齐后的位置
        m_PreviewInstance.transform.position = new Vector3(position.x, position.y + m_BottomOffset, position.z);

        Log.Info($"ChessGhostPreview: 显示预览, 底部偏移={m_BottomOffset}");
    }

    /// <summary>
    /// 更新预览位置和有效性
    /// </summary>
    /// <param name="position">新位置</param>
    /// <param name="isValid">是否为有效放置位置(暂未使用颜色)</param>
    public void UpdatePreview(Vector3 position, bool isValid)
    {
        if (m_PreviewInstance == null) return;

        // 应用底部对齐后的位置
        m_PreviewInstance.transform.position = new Vector3(position.x, position.y + m_BottomOffset, position.z);
    }

    /// <summary>
    /// 隐藏并销毁预览
    /// </summary>
    public void Hide()
    {
        if (m_PreviewInstance != null)
        {
            Destroy(m_PreviewInstance);
            m_PreviewInstance = null;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 禁用非视觉组件
    /// </summary>
    private void DisableNonVisualComponents(GameObject obj)
    {
        // 禁用Collider
        var colliders = obj.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        // 禁用MonoBehaviour脚本(保留Transform和Renderer)
        var scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            // 跳过自己
            if (scripts[i] == this) continue;
            scripts[i].enabled = false;
        }

        // 禁用Animator(避免播放动画干扰预览)
        var animators = obj.GetComponentsInChildren<Animator>();
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }
    }

    #endregion

    #region 生命周期

    private void OnDestroy()
    {
        Hide();
    }

    #endregion
}
