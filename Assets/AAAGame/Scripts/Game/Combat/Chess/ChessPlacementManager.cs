using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;

/// <summary>
/// 棋子放置管理器
/// 负责战斗准备阶段的棋子放置逻辑
/// </summary>
public class ChessPlacementManager
{
    #region 单例

    private static ChessPlacementManager s_Instance;
    public static ChessPlacementManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ChessPlacementManager();
            }
            return s_Instance;
        }
    }

    private ChessPlacementManager() { }

    #endregion

    #region 字段

    /// <summary>是否启用</summary>
    private bool m_IsEnabled;

    /// <summary>当前正在放置的棋子ID</summary>
    private int m_CurrentPlacingChessId;

    /// <summary>是否正在放置中</summary>
    private bool m_IsPlacing;

    /// <summary>拖拽模式：true=拖拽模式，false=点击模式</summary>
    private bool m_IsDragMode;

    /// <summary>Ghost预览组件</summary>
    private ChessGhostPreview m_GhostPreview;

    /// <summary>当前正在放置的棋子实例ID</summary>
    private string m_CurrentPlacingInstanceId;

    /// <summary>放置平面Layer</summary>
    private LayerMask m_PlacementLayerMask;

    /// <summary>棋子Layer</summary>
    private int m_ChessLayer;

    /// <summary>射线检测距离</summary>
    private float m_MaxRaycastDistance = 100f;

    /// <summary>鼠标位置预览对象</summary>
    private GameObject m_MousePreviewObject;

    /// <summary>预览图片 SpriteRenderer</summary>
    private SpriteRenderer m_PreviewSpriteRenderer;

    /// <summary>预览图片是否已加载</summary>
    private bool m_PreviewLoaded;

    /// <summary>预览图片相对于地面的高度偏移</summary>
    private float m_PreviewHeightOffset = 0.05f;

    // 新增：仅预览模式标志
    /// <summary>是否为仅预览模式（战斗阶段使用）</summary>
    private bool m_IsPreviewOnlyMode;

    #endregion

    #region 公共接口

    /// <summary>
    /// 初始化放置管理器
    /// </summary>
    public void Initialize()
    {
        // 获取GhostPreview组件(挂载到CombatTickDriver上)
        var updater = CombatTickDriver.Instance;
        if (updater != null)
        {
            m_GhostPreview = updater.gameObject.GetComponent<ChessGhostPreview>();
            if (m_GhostPreview == null)
            {
                m_GhostPreview = updater.gameObject.AddComponent<ChessGhostPreview>();
            }
        }
        else
        {
            Log.Error("ChessPlacementManager: CombatTickDriver 未找到,无法创建GhostPreview");
        }

        // 设置Layer Mask
        m_PlacementLayerMask = LayerMask.GetMask("PlacementPlane");

        // 获取棋子Layer(如果Chess Layer不存在,使用Default)
        m_ChessLayer = LayerMask.NameToLayer("Chess");
        if (m_ChessLayer == -1)
        {
            m_ChessLayer = 0; // Default Layer
            Log.Warning("ChessPlacementManager: 未找到Chess Layer,使用Default Layer");
        }

        // 初始化鼠标位置预览
        InitializeMousePreview();

        Log.Info("ChessPlacementManager: 初始化完成");
    }

    /// <summary>
    /// 启用放置系统
    /// </summary>
    public void Enable()
    {
        m_IsEnabled = true;
        m_IsPreviewOnlyMode = false; // 完整模式
        Log.Info("ChessPlacementManager: 已启用");
    }

    /// <summary>
    /// 启用仅预览模式（战斗阶段使用，只显示鼠标位置预览，不允许放置棋子）
    /// </summary>
    public void EnablePreviewOnly()
    {
        m_IsEnabled = true;
        m_IsPreviewOnlyMode = true;
        Log.Info("ChessPlacementManager: 已启用（仅预览模式）");
    }

    /// <summary>
    /// 禁用放置系统
    /// </summary>
    public void Disable()
    {
        m_IsEnabled = false;
        m_IsPreviewOnlyMode = false; // 重置标志
        CancelPlacement();
        Log.Info("ChessPlacementManager: 已禁用");
    }

    /// <summary>
    /// 开始放置棋子（统一入口）
    /// </summary>
    /// <param name="instanceId">棋子实例ID</param>
    /// <param name="isDragMode">是否为拖拽模式</param>
    public async void StartPlacement(string instanceId, bool isDragMode = false)
    {
        // 必须在拖拽模式标记后，在 await 之前设置，确保 OnEndDrag 时能正确判断
        m_IsDragMode = isDragMode;

        if (!await PrepareForPlacement(instanceId))
        {
            m_IsDragMode = false; // 准备失败时重置
            return;
        }

        Log.Info(
            $"ChessPlacementManager: 开始放置棋子 instanceId={instanceId}, isDragMode={isDragMode}"
        );
    }

    /// <summary>
    /// 准备放置（通用逻辑）
    /// </summary>
    private async UniTask<bool> PrepareForPlacement(string instanceId)
    {
        if (!m_IsEnabled)
        {
            Log.Warning("ChessPlacementManager: 放置系统未启用");
            return false;
        }

        // 获取实例数据
        var instance = ChessDeploymentTracker.Instance.GetInstance(instanceId);
        if (instance == null)
        {
            Log.Error($"ChessPlacementManager: 棋子实例不存在 instanceId={instanceId}");
            return false;
        }

        // 检查是否可以出战（未出战且未死亡）
        if (!instance.CanDeploy)
        {
            if (instance.IsDead)
                Log.Warning($"ChessPlacementManager: 棋子已死亡，无法放置 instanceId={instanceId}");
            else
                Log.Warning(
                    $"ChessPlacementManager: 棋子已出战，无法重复放置 instanceId={instanceId}"
                );
            return false;
        }

        // 获取配置
        if (!ChessDataManager.Instance.TryGetConfig(instance.ChessId, out var config))
        {
            Log.Error($"ChessPlacementManager: 棋子配置不存在 Id={instance.ChessId}");
            return false;
        }

        // 如果正在放置其他棋子，先取消
        if (m_IsPlacing)
        {
            CancelPlacement();
        }

        m_CurrentPlacingInstanceId = instanceId;
        m_CurrentPlacingChessId = instance.ChessId;
        m_IsPlacing = true;

        // 加载预制体
        GameObject prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(config.PrefabId);
        if (prefab == null)
        {
            Log.Error($"ChessPlacementManager: 预制体加载失败 Id={instance.ChessId}");
            CancelPlacement();
            return false;
        }

        // 显示GhostPreview（初始位置为鼠标位置）
        Vector3 initialPos = GetMouseWorldPosition();
        m_GhostPreview.Show(prefab, initialPos);

        return true;
    }

    /// <summary>
    /// 拖拽确认放置（检查是否在有效区域）
    /// </summary>
    public void ConfirmPlacementFromDrag()
    {
        if (!m_IsPlacing || !m_IsDragMode)
            return;

        // 检查是否释放在 UI 上
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            DebugEx.LogModule("ChessPlacementManager", "拖拽释放在UI上，取消放置");
            CancelPlacement();
            return;
        }

        // 检查是否在有效区域
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_PlacementLayerMask))
        {
            // 检查是否在我方区域
            if (
                BattleArenaManager.Instance != null
                && !BattleArenaManager.Instance.IsInPlayerZone(hit.point)
            )
            {
                DebugEx.WarningModule(
                    "ChessPlacementManager",
                    "拖拽释放位置不在我方区域，取消放置"
                );
                CancelPlacement();
                return;
            }

            // 在我方区域 → 确认放置
            ConfirmPlacementInternal();
        }
        else
        {
            // 不在有效区域 → 取消放置
            DebugEx.LogModule("ChessPlacementManager", "拖拽释放位置不在有效区域，取消放置");
            CancelPlacement();
        }
    }

    /// <summary>
    /// 取消放置
    /// </summary>
    public void CancelPlacement()
    {
        if (m_IsPlacing)
        {
            m_IsPlacing = false;
            m_IsDragMode = false;
            m_CurrentPlacingInstanceId = null;
            m_CurrentPlacingChessId = 0;
            m_GhostPreview.Hide();
            Log.Info("ChessPlacementManager: 取消放置");
        }
    }

    /// <summary>
    /// 更新(需要在某个MonoBehaviour的Update中调用)
    /// </summary>
    public void Tick()
    {
        if (!m_IsEnabled)
            return;

        // 鼠标位置预览始终更新（无论是否在放置中）
        UpdateMousePreview();

        // 在预览模式下，不处理放置逻辑
        if (m_IsPreviewOnlyMode)
            return;

        if (!m_IsPlacing)
            return;

        // 更新Ghost位置
        UpdateGhostPosition();

        // 处理输入（仅非拖拽模式）
        if (!m_IsDragMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击 UI 上
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    // 点击在 UI 上 → 取消放置
                    Log.Info("ChessPlacementManager: 点击在UI上，取消放置");
                    CancelPlacement();
                }
                else
                {
                    // 点击在场景中 → 尝试放置
                    TryConfirmPlacement();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                // 右键取消
                CancelPlacement();
            }
        }
    }

    /// <summary>
    /// 尝试确认放置（检查是否在有效区域）
    /// </summary>
    private void TryConfirmPlacement()
    {
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_PlacementLayerMask))
        {
            // 检查是否在我方区域
            if (
                BattleArenaManager.Instance != null
                && !BattleArenaManager.Instance.IsInPlayerZone(hit.point)
            )
            {
                DebugEx.WarningModule("ChessPlacementManager", "点击位置不在我方区域，取消放置");
                CancelPlacement();
                return;
            }

            // 在我方区域 → 确认放置
            ConfirmPlacementInternal();
        }
        else
        {
            // 不在有效区域 → 取消放置
            DebugEx.LogModule("ChessPlacementManager", "点击位置不在有效区域，取消放置");
            CancelPlacement();
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        if (m_GhostPreview != null)
        {
            m_GhostPreview.Hide();
        }

        // 清理鼠标位置预览
        if (m_MousePreviewObject != null)
        {
            Object.Destroy(m_MousePreviewObject);
            m_MousePreviewObject = null;
            m_PreviewSpriteRenderer = null;
            m_PreviewLoaded = false;
        }

        m_IsEnabled = false;
        m_IsPlacing = false;
        m_IsDragMode = false;
        m_CurrentPlacingChessId = 0;

        Log.Info("ChessPlacementManager: 已清理");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取鼠标在世界空间的位置
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return Vector3.zero;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_PlacementLayerMask))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 更新Ghost预览位置
    /// </summary>
    private void UpdateGhostPosition()
    {
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_PlacementLayerMask))
        {
            m_GhostPreview.UpdatePreview(hit.point, true);
        }
    }

    /// <summary>
    /// 确认放置（内部实现）
    /// </summary>
    private async void ConfirmPlacementInternal()
    {
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_PlacementLayerMask))
        {
            Log.Warning("ChessPlacementManager: 未检测到放置平面");
            return;
        }

        Vector3 spawnPos = hit.point;
        string instanceId = m_CurrentPlacingInstanceId;
        int chessId = m_CurrentPlacingChessId;

        // 获取配置
        if (!ChessDataManager.Instance.TryGetConfig(chessId, out var config))
        {
            Log.Error($"ChessPlacementManager: 棋子配置不存在 Id={chessId}");
            CancelPlacement();
            return;
        }

        // 检查统帅值是否足够
        if (!CombatSessionData.Instance.CanPlace(config.PopCost))
        {
            Log.Warning(
                $"ChessPlacementManager: 统帅值不足，无法放置棋子 (需要{config.PopCost},剩余{CombatSessionData.Instance.AvailablePopulation})"
            );
            CancelPlacement();
            return;
        }

        // 隐藏Ghost
        m_GhostPreview.Hide();
        m_IsPlacing = false;
        m_IsDragMode = false;

        // 确保SummonChessManager存在
        if (SummonChessManager.Instance == null)
        {
            Log.Error("ChessPlacementManager: SummonChessManager.Instance is null");
            m_CurrentPlacingInstanceId = null;
            m_CurrentPlacingChessId = 0;
            return;
        }

        // 生成棋子
        var entity = await SummonChessManager.Instance.SpawnChessAsync(chessId, spawnPos, 0);
        if (entity == null)
        {
            Log.Error($"ChessPlacementManager: 棋子生成失败 Id={chessId}");
            m_CurrentPlacingInstanceId = null;
            m_CurrentPlacingChessId = 0;
            return;
        }

        // 设置Layer
        entity.gameObject.layer = m_ChessLayer;

        // 扣除统帅值
        CombatSessionData.Instance.ConsumePopulation(config.PopCost);

        // 标记为已出战
        ChessDeploymentTracker.Instance.DeployChess(instanceId, entity);

        Log.Info(
            $"ChessPlacementManager: 棋子放置成功 instanceId={instanceId}, chessId={chessId} at {spawnPos}"
        );

        m_CurrentPlacingInstanceId = null;
        m_CurrentPlacingChessId = 0;
    }

    /// <summary>
    /// 初始化鼠标位置预览
    /// </summary>
    private async void InitializeMousePreview()
    {
        // 创建预览对象
        m_MousePreviewObject = new GameObject("MousePlacementPreview");
        m_MousePreviewObject.SetActive(false);

        // 添加 SpriteRenderer
        m_PreviewSpriteRenderer = m_MousePreviewObject.AddComponent<SpriteRenderer>();
        m_PreviewSpriteRenderer.sortingOrder = 100; // 确保显示在上层

        // 设置旋转，使图片平铺在地面上
        m_MousePreviewObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 从 ResourceConfigTable 加载预览图片
        var dataTable = GF.DataTable.GetDataTable<ResourceConfigTable>();
        if (dataTable != null)
        {
            var row = dataTable.GetDataRow(ResourceIds.ICON_PLACEMENT_PREVIEW);
            if (row != null)
            {
                var sprite = await GameExtension.ResourceExtension.LoadSpriteAsync(row.Id);
                if (sprite != null)
                {
                    m_PreviewSpriteRenderer.sprite = sprite;
                    m_PreviewLoaded = true;
                    Log.Info("ChessPlacementManager: 鼠标位置预览图片加载成功");
                }
                else
                {
                    Log.Warning($"ChessPlacementManager: 预览图片加载失败 Path={row.Path}");
                }
            }
            else
            {
                Log.Warning(
                    $"ChessPlacementManager: 未找到预览图片配置 ID={ResourceIds.ICON_PLACEMENT_PREVIEW}"
                );
            }
        }
    }

    /// <summary>
    /// 更新鼠标位置预览
    /// </summary>
    private void UpdateMousePreview()
    {
        if (!m_PreviewLoaded || m_MousePreviewObject == null)
            return;

        // 检查是否在 UI 上
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HideMousePreview();
            return;
        }

        // 射线检测战斗场地
        Camera playerCamera = CameraRegistry.PlayerCamera;
        if (playerCamera == null)
            return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_PlacementLayerMask))
        {
            // 检查是否在我方区域（战斗准备阶段需要限制）
            if (!m_IsPreviewOnlyMode && BattleArenaManager.Instance != null)
            {
                bool isInPlayerZone = BattleArenaManager.Instance.IsInPlayerZone(hit.point);
                if (isInPlayerZone)
                {
                    // 在我方区域 → 显示绿色预览
                    ShowMousePreview(hit.point, true);
                }
                else
                {
                    // 在敌方区域 → 显示红色预览
                    ShowMousePreview(hit.point, false);
                }
            }
            else
            {
                // 预览模式或未初始化 → 显示默认预览
                ShowMousePreview(hit.point, true);
            }
        }
        else
        {
            // 不在有效区域 → 隐藏预览
            HideMousePreview();
        }
    }

    /// <summary>
    /// 显示鼠标位置预览
    /// </summary>
    /// <param name="position">预览位置</param>
    /// <param name="isValid">是否为有效区域（true=绿色，false=红色）</param>
    private void ShowMousePreview(Vector3 position, bool isValid)
    {
        if (m_MousePreviewObject == null || m_PreviewSpriteRenderer == null)
            return;

        m_MousePreviewObject.SetActive(true);
        m_MousePreviewObject.transform.position = position + Vector3.up * m_PreviewHeightOffset;

        // 根据区域有效性设置颜色
        m_PreviewSpriteRenderer.color = isValid ? Color.white : Color.red;
    }

    /// <summary>
    /// 隐藏鼠标位置预览
    /// </summary>
    private void HideMousePreview()
    {
        if (m_MousePreviewObject != null)
        {
            m_MousePreviewObject.SetActive(false);
        }
    }
    #endregion
}
