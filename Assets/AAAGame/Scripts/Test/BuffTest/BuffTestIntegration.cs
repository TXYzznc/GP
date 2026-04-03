using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// Buff 测试工具集成脚本
/// 在战斗场景中自动初始化测试工具
/// </summary>
public class BuffTestIntegration : MonoBehaviour
{
    #region 字段

    private BuffTestUIManager m_UIManager;
    private bool m_IsInitialized = false;

    #endregion

    #region 生命周期

    private void Start()
    {
        if (!m_IsInitialized)
        {
            Initialize();
        }
    }

    private void Update()
    {
        // 监听输入切换 UI
        if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.LeftControl))
        {
            if (m_UIManager != null)
            {
                m_UIManager.ToggleUI();
            }
        }

        // F 快速选择鼠标指向的棋子
        if (Input.GetKeyDown(KeyCode.F))
        {
            SelectTargetFromMouse();
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化测试工具
    /// </summary>
    public void Initialize()
    {
        // 创建 UI 管理器
        var uiGO = new GameObject("BuffTestUIManager");
        uiGO.transform.SetParent(transform);
        m_UIManager = uiGO.AddComponent<BuffTestUIManager>();

        m_IsInitialized = true;

        DebugEx.LogModule("BuffTestIntegration", "✓ Buff 测试工具已初始化");
        DebugEx.LogModule("BuffTestIntegration", "快捷键: Ctrl+B 打开/关闭工具 | F 快速选择目标");
    }

    #endregion

    #region 目标选择

    /// <summary>
    /// 从鼠标位置选择目标
    /// </summary>
    private void SelectTargetFromMouse()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f))
        {
            var entity = hit.collider.GetComponent<ChessEntity>();
            if (entity != null && m_UIManager != null)
            {
                m_UIManager.SetTarget(entity.gameObject);
                DebugEx.LogModule("BuffTestIntegration", $"已选择: {entity.Config?.Name}");
                return;
            }
        }

        DebugEx.WarningModule("BuffTestIntegration", "未点中任何棋子");
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 打开 UI
    /// </summary>
    public void OpenUI()
    {
        if (m_UIManager != null)
        {
            m_UIManager.ShowUI();
        }
    }

    /// <summary>
    /// 关闭 UI
    /// </summary>
    public void CloseUI()
    {
        if (m_UIManager != null)
        {
            m_UIManager.HideUI();
        }
    }

    /// <summary>
    /// 设置测试目标
    /// </summary>
    public void SetTarget(GameObject target)
    {
        if (m_UIManager != null)
        {
            m_UIManager.SetTarget(target);
        }
    }

    #endregion
}
