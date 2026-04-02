using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;

/// <summary>
/// 棋子选择管理器
/// 负责战斗阶段的棋子选择和移动指令
/// </summary>
public class ChessSelectionManager
{
    /// <summary>棋子被选中时触发（参数：选中的棋子实体）</summary>
    public static event Action<ChessEntity> OnChessSelected;

    /// <summary>棋子被取消选中时触发</summary>
    public static event Action OnChessDeselected;
    #region 单例

    private static ChessSelectionManager s_Instance;
    public static ChessSelectionManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ChessSelectionManager();
            }
            return s_Instance;
        }
    }

    private ChessSelectionManager() { }

    #endregion

    #region 字段

    /// <summary>是否启用</summary>
    private bool m_IsEnabled;

    /// <summary>当前选中的棋子</summary>
    private ChessEntity m_SelectedChess;

    /// <summary>棋子Layer</summary>
    private LayerMask m_ChessLayerMask;

    /// <summary>地面Layer</summary>
    private LayerMask m_GroundLayerMask;

    /// <summary>射线检测距离</summary>
    private float m_MaxRaycastDistance = 100f;

    /// <summary>是否为仅选择模式（战斗阶段使用，不允许召回棋子）</summary>
    private bool m_IsSelectionOnlyMode;

    #endregion

    #region 公共接口

    /// <summary>
    /// 初始化管理器
    /// </summary>
    public void Initialize()
    {
        // 修改Layer Mask配置
        int chessLayer = LayerMask.NameToLayer("Chess");
        if (chessLayer != -1)
        {
            m_ChessLayerMask = 1 << chessLayer;
            Log.Info($"ChessSelectionManager: Chess Layer找到，Layer={chessLayer}, Mask={m_ChessLayerMask}");
        }
        else
        {
            Log.Error("ChessSelectionManager: 未找到Chess Layer，请在Unity编辑器中设置Layer 14为Chess");
            m_ChessLayerMask = 0;
        }

        // 地面Layer（包括PlacementPlane）
        m_GroundLayerMask = LayerMask.GetMask("PlacementPlane");

        Log.Info($"ChessSelectionManager: 初始化完成 (ChessLayerMask={m_ChessLayerMask}, GroundLayerMask={m_GroundLayerMask.value})");
    }

    /// <summary>
    /// 启用选择系统
    /// </summary>
    public void Enable()
    {
        m_IsEnabled = true;
        m_IsSelectionOnlyMode = false; // 完整模式（允许召回）
        Log.Info($"ChessSelectionManager: 已启用 (ChessLayerMask={m_ChessLayerMask}, GroundLayerMask={m_GroundLayerMask.value})");
    }

    /// <summary>
    /// 启用仅选择模式（战斗阶段使用，只允许选择和移动，不允许召回棋子）
    /// </summary>
    public void EnableSelectionOnly()
    {
        m_IsEnabled = true;
        m_IsSelectionOnlyMode = true;
        Log.Info($"ChessSelectionManager: 已启用（仅选择模式）");
    }

    /// <summary>
    /// 禁用选择系统
    /// </summary>
    public void Disable()
    {
        m_IsEnabled = false;
        m_IsSelectionOnlyMode = false; // 重置
        DeselectChess();
        Log.Info("ChessSelectionManager: 已禁用");
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
            // 检查是否点击 UI 上
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // 点击在 UI 上 → 取消选择
                if (m_SelectedChess != null)
                {
                    Log.Info("ChessSelectionManager: 点击在UI上，取消选择");
                    DeselectChess();
                }
                return;
            }

            Log.Info("ChessSelectionManager: 检测到鼠标左键点击");
            HandleMouseClick();
        }

        // 右键点击 - 召回棋子
        if (inputManager.RightMouseButtonDown)
        {
            HandleRightClick();
        }
    }

    /// <summary>
    /// 清理管理器
    /// </summary>
    public void Cleanup()
    {
        Disable();
        Log.Info("ChessSelectionManager: 已清理");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    private void HandleMouseClick()
    {
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null) return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // 先检测棋子
        if (Physics.Raycast(ray, out RaycastHit chessHitInfo, m_MaxRaycastDistance, m_ChessLayerMask))
        {
            var entity = chessHitInfo.collider.GetComponentInParent<ChessEntity>();
            if (entity != null && entity.Camp == 0) // 只能选择玩家棋子
            {
                // 如果同一个棋子 → 取消选择
                if (m_SelectedChess == entity)
                {
                    Log.Info($"ChessSelectionManager: 再次点击已选中棋子，取消选择");
                    DeselectChess();
                }
                else
                {
                    // 点击其他棋子 → 切换选择
                    SelectChess(entity);
                }
                return;
            }
        }

        // 如果有选中的棋子，检测地面
        if (m_SelectedChess != null)
        {
            if (Physics.Raycast(ray, out RaycastHit groundHitInfo, m_MaxRaycastDistance, m_GroundLayerMask))
            {
                // 战斗准备阶段：检查目标位置是否在我方区域内
                if (!m_IsSelectionOnlyMode
                    && BattleArenaManager.Instance != null
                    && !BattleArenaManager.Instance.IsInPlayerZone(groundHitInfo.point))
                {
                    Log.Info("ChessSelectionManager: 目标位置不在我方区域，取消移动");
                    DeselectChess();
                    return;
                }

                // 在有效区域 → 移动
                Log.Info($"ChessSelectionManager: 移动棋子到 {groundHitInfo.point}");
                MoveChessTo(groundHitInfo.point);
            }
            else
            {
                // 不在有效区域 → 取消选择
                Log.Info("ChessSelectionManager: 点击无效区域，取消选择");
                DeselectChess();
            }
        }
    }

    /// <summary>
    /// 处理右键点击 - 召回选中的棋子
    /// </summary>
    private void HandleRightClick()
    {
        // 在选择模式下，右键只取消选择，不召回棋子
        if (m_IsSelectionOnlyMode)
        {
            if (m_SelectedChess != null)
            {
                Log.Info("ChessSelectionManager: 仅选择模式，右键取消选择");
                DeselectChess();
            }
            return;
        }

        // 没有选中棋子时，右键无效
        if (m_SelectedChess == null)
            return;

        // 检查是否点击 UI 上
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null) return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // 由于不再有Layer区分，直接使用棋子Layer
        int combinedMask = m_ChessLayerMask;

        // 检测是否点击棋子
        if (Physics.Raycast(ray, out RaycastHit hitInfo, m_MaxRaycastDistance, combinedMask))
        {
            var entity = hitInfo.collider.GetComponentInParent<ChessEntity>();

            // 只有右键点击的是当前选中的棋子才能召回
            if (entity != null && entity == m_SelectedChess)
            {
                RecallChess(entity);
            }
        }
    }

    /// <summary>
    /// 召回棋子（只销毁场景中的 GameObject，保留实体数据）
    /// </summary>
    private void RecallChess(ChessEntity entity)
    {
        if (entity == null)
            return;

        // 1. 获取实例ID
        string instanceId = ChessDeploymentTracker.Instance.GetInstanceIdByEntity(entity);
        if (string.IsNullOrEmpty(instanceId))
        {
            Log.Warning($"ChessSelectionManager: 无法召回棋子，未找到对应的实例ID");
            return;
        }

        // 2. 获取配置，用于返还统帅值
        var config = entity.Config;
        int popCost = config != null ? config.PopCost : 0;

        // 3. 取消选择（因为要销毁这个棋子）
        DeselectChess();

        // 4. 重置开启状态，标记为未出战（实体数据保留）
        ChessDeploymentTracker.Instance.RecallChess(instanceId);

        // 5. 返还统帅值
        if (popCost > 0 && CombatSessionData.Instance != null)
        {
            CombatSessionData.Instance.ReturnPopulation(popCost);
        }

        // 6. 只销毁场景中的 GameObject，不影响实体数据
        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.DestroyChess(entity);
        }

        Log.Info($"ChessSelectionManager: 召回棋子成功 instanceId={instanceId}, 返还统帅值={popCost}");
    }

    /// <summary>
    /// 选择棋子
    /// </summary>
    private void SelectChess(ChessEntity entity)
    {
        // 如果有已选中,先取消
        if (m_SelectedChess != null)
        {
            Log.Info($"ChessSelectionManager: 取消之前的选中 {m_SelectedChess.Config.Name}");
            DeselectChess();
        }

        m_SelectedChess = entity;

        // 使用描边控制器设置选中状态
        if (entity.OutlineController != null)
        {
            entity.OutlineController.SetSelected(true);
        }

        // 通知UI显示棋子详情
        OnChessSelected?.Invoke(entity);

        // 通知测试输入组件（已选中）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (entity.TestInput != null)
        {
            entity.TestInput.SetSelected(true);
        }
#endif

        Log.Info($"ChessSelectionManager: 选中棋子 {entity.Config.Name}");
    }

    /// <summary>
    /// 取消选择
    /// </summary>
    private void DeselectChess()
    {
        if (m_SelectedChess != null)
        {
            // 使用描边控制器取消选中状态
            if (m_SelectedChess.OutlineController != null)
            {
                m_SelectedChess.OutlineController.SetSelected(false);
            }

            // 通知测试输入组件（取消选中）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (m_SelectedChess.TestInput != null)
            {
                m_SelectedChess.TestInput.SetSelected(false);
            }
#endif

            Log.Info($"ChessSelectionManager: 取消选中 {m_SelectedChess.Config.Name}");
            m_SelectedChess = null;

            // 通知UI隐藏棋子详情
            OnChessDeselected?.Invoke();
        }
    }

    /// <summary>
    /// 移动棋子到目标位置
    /// </summary>
    private void MoveChessTo(Vector3 targetPos)
    {
        if (m_SelectedChess == null)
        {
            Log.Warning("ChessSelectionManager: MoveChessTo被调用但无选中棋子");
            return;
        }

        // 计算棋子底部对齐的目标位置
        Vector3 finalTargetPos = targetPos;

        // 获取棋子的 Collider，计算底部偏移
        Collider collider = m_SelectedChess.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            // 计算当前底部 Y 坐标
            float currentBottomY = collider.bounds.min.y;

            // 计算物体中心到底部的偏移量
            float bottomOffset = m_SelectedChess.transform.position.y - currentBottomY;

            // 目标位置 = 点击位置 + 底部偏移
            finalTargetPos = new Vector3(targetPos.x, targetPos.y + bottomOffset, targetPos.z);

            Log.Info($"ChessSelectionManager: 底部对齐计算 - 点击Y={targetPos.y}, 底部偏移={bottomOffset}, 最终Y={finalTargetPos.y}");
        }
        else
        {
            Log.Warning($"ChessSelectionManager: 棋子 {m_SelectedChess.Config.Name} 没有 Collider，使用原始位置");
        }

        // 战斗阶段：通过战斗控制器发送移动指令（然后由AI执行）
        if (m_IsSelectionOnlyMode && m_SelectedChess.CombatController != null)
        {
            m_SelectedChess.CombatController.SetPlayerMoveCommand(finalTargetPos);
            Log.Info($"ChessSelectionManager: 发送移动指令 - {m_SelectedChess.Config.Name} -> {finalTargetPos}");
        }
        else
        {
            // 准备阶段：直接移动
            m_SelectedChess.Movement.MoveTo(finalTargetPos);
            Log.Info($"ChessSelectionManager: 棋子 {m_SelectedChess.Config.Name} 正在移动到 {finalTargetPos}");
        }
    }

    #endregion
}
