using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;

/// <summary>
/// 敌方棋子详情管理器
/// 负责战斗阶段的敌方棋子点击检测和详情面板显示
/// </summary>
public class EnemyChessDetailManager
{
    /// <summary>敌方棋子被点击时触发（参数：被点击的敌方棋子）</summary>
    public static event Action<ChessEntity> OnEnemyChessClicked;

    /// <summary>敌方棋子被取消点击时触发</summary>
    public static event Action OnEnemyChessDeselected;

    #region 单例

    private static EnemyChessDetailManager s_Instance;
    public static EnemyChessDetailManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new EnemyChessDetailManager();
            }
            return s_Instance;
        }
    }

    private EnemyChessDetailManager() { }

    #endregion

    #region 字段

    /// <summary>是否启用</summary>
    private bool m_IsEnabled;

    /// <summary>当前点击显示详情的敌方棋子</summary>
    private ChessEntity m_CurrentDetailChess;

    /// <summary>棋子Layer</summary>
    private LayerMask m_ChessLayerMask;

    /// <summary>射线检测距离</summary>
    private float m_MaxRaycastDistance = 100f;

    /// <summary>敌方阵营ID</summary>
    private const int ENEMY_CAMP = 1;

    #endregion

    #region 公共接口

    /// <summary>
    /// 初始化管理器
    /// </summary>
    public void Initialize()
    {
        int chessLayer = LayerMask.NameToLayer("Chess");
        if (chessLayer != -1)
        {
            m_ChessLayerMask = 1 << chessLayer;
            DebugEx.Log(nameof(EnemyChessDetailManager), $"Chess Layer找到，Layer={chessLayer}");
        }
        else
        {
            DebugEx.Warning(nameof(EnemyChessDetailManager), "未找到Chess Layer");
            m_ChessLayerMask = 0;
        }
    }

    /// <summary>
    /// 启用敌方棋子详情系统
    /// </summary>
    public void Enable()
    {
        m_IsEnabled = true;
        DebugEx.Log(nameof(EnemyChessDetailManager), "已启用");
    }

    /// <summary>
    /// 禁用敌方棋子详情系统
    /// </summary>
    public void Disable()
    {
        m_IsEnabled = false;
        DeselectEnemyChess();
        DebugEx.Log(nameof(EnemyChessDetailManager), "已禁用");
    }

    /// <summary>
    /// 更新(需要在某个MonoBehaviour的Update中调用)
    /// </summary>
    public void Tick()
    {
        if (!m_IsEnabled)
            return;

        var inputManager = PlayerInputManager.Instance;
        if (inputManager == null)
            return;

        // 左键点击
        if (inputManager.LeftMouseButtonDown)
        {
            // 点击在 UI 上 → 不处理
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            HandleMouseClick();
        }

        // 右键取消详情显示
        if (inputManager.RightMouseButtonDown)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            DeselectEnemyChess();
        }
    }

    /// <summary>
    /// 清理管理器
    /// </summary>
    public void Cleanup()
    {
        Disable();
        DebugEx.Log(nameof(EnemyChessDetailManager), "已清理");
    }

    /// <summary>
    /// 获取当前显示详情的敌方棋子（只读）
    /// </summary>
    public ChessEntity CurrentDetailChess => m_CurrentDetailChess;

    #endregion

    #region 私有方法

    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    private void HandleMouseClick()
    {
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // 检测棋子（只关注敌方）
        if (Physics.Raycast(ray, out RaycastHit hitInfo, m_MaxRaycastDistance, m_ChessLayerMask))
        {
            var entity = hitInfo.collider.GetComponentInParent<ChessEntity>();
            if (entity != null && entity.Camp == ENEMY_CAMP)
            {
                // 如果点击同一敌方棋子 → 取消显示
                if (m_CurrentDetailChess == entity)
                {
                    DebugEx.Log(nameof(EnemyChessDetailManager), $"再次点击同一敌方棋子，取消详情显示");
                    DeselectEnemyChess();
                }
                else
                {
                    // 点击不同敌方棋子 → 切换显示
                    SelectEnemyChess(entity);
                }
                return;
            }
        }

        // 点击到非敌方棋子 → 取消显示
        if (m_CurrentDetailChess != null)
        {
            DeselectEnemyChess();
        }
    }

    /// <summary>
    /// 选择敌方棋子显示详情
    /// </summary>
    private void SelectEnemyChess(ChessEntity entity)
    {
        // 如果有已显示的，先取消
        if (m_CurrentDetailChess != null)
        {
            DeselectEnemyChess();
        }

        m_CurrentDetailChess = entity;

        // 触发事件，通知UI显示敌方棋子详情
        OnEnemyChessClicked?.Invoke(entity);

        DebugEx.Log(nameof(EnemyChessDetailManager), $"点击敌方棋子详情: {entity.Config.Name} [ID: {entity.ChessId}]");
    }

    /// <summary>
    /// 取消显示敌方棋子详情
    /// </summary>
    private void DeselectEnemyChess()
    {
        if (m_CurrentDetailChess != null)
        {
            DebugEx.Log(nameof(EnemyChessDetailManager), $"取消敌方棋子详情: {m_CurrentDetailChess.Config.Name}");
            m_CurrentDetailChess = null;

            // 触发事件，通知UI隐藏详情
            OnEnemyChessDeselected?.Invoke();
        }
    }

    #endregion
}
