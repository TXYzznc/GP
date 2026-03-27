using UnityEngine;
using Cysharp.Threading.Tasks;

public class DashSkill : IPlayerSkill
{
    public int SkillId => common.Id;

    #region 私有字段

    private PlayerSkillContext ctx;
    private SkillCommonConfig common;
    private DashParamSO param;

    private float cdRemain;

    // 手部挂点缓存
    private Transform m_HandTransform;

    // 道具实例
    private DashItemController m_CurrentItem;

    // 状态标志
    private bool m_IsWaitingForThrow;     // 是否等待投掷
    private bool m_IsWaitingForTeleport;  // 是否等待传送

    // 新增：AimUI 的 UIForm ID
    private int m_AimUIFormId = -1;

    #endregion

    #region 初始化

    public void Init(PlayerSkillContext ctx, SkillCommonConfig common, SkillParamSO _param)
    {
        this.ctx = ctx;
        this.common = common;
        cdRemain = 0f;

        param = _param as DashParamSO;
        if (param == null)
        {
            DebugEx.ErrorModule("DashSkill", $"missing DashParamSO for skillId={common.Id}");
            return;
        }

        // 查找并缓存手部挂点
        FindHandTransform();
    }

    /// <summary>
    /// 查找手部挂点（性能优化：先用Tag缩小范围，再用名字精确查找）
    /// </summary>
    private void FindHandTransform()
    {
        if (ctx == null || ctx.Owner == null)
        {
            DebugEx.WarningModule("DashSkill", "上下文或Owner为空，无法查找手部挂点");
            return;
        }

        // 先通过Tag查找所有候选对象
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(param.handBoneTag);

        if (taggedObjects.Length == 0)
        {
            DebugEx.WarningModule("DashSkill", $"未找到Tag为'{param.handBoneTag}'的对象");
            return;
        }

        // 在候选对象中通过名字精确查找
        foreach (var obj in taggedObjects)
        {
            // 检查是否是玩家的子对象
            if (!obj.transform.IsChildOf(ctx.Owner.transform))
                continue;

            if (obj.name == param.handBoneName)
            {
                m_HandTransform = obj.transform;
                DebugEx.LogModule("DashSkill", $"找到手部挂点: {obj.name}");
                return;
            }
        }

        DebugEx.WarningModule("DashSkill", $"未找到名为'{param.handBoneName}'的手部挂点");
    }

    #endregion

    #region 技能逻辑

    public void Tick(float dt)
    {
        // 冷却计时
        if (cdRemain > 0f)
            cdRemain -= dt;

        // 新增：在等待投掷状态时，实时更新轨迹预测
        if (m_IsWaitingForThrow && m_CurrentItem != null && !m_CurrentItem.IsDestroyed)
        {
            Camera mainCamera = CameraRegistry.PlayerCamera;

            if (mainCamera != null)
            {
                Vector3 predictedDirection = CalculateThrowDirection(mainCamera);
                m_CurrentItem.SetPredictedTrajectory(predictedDirection, param.throwForce);
            }
        }

        // 检测鼠标右键输入
        if (PlayerInputManager.Instance != null &&
            PlayerInputManager.Instance.RightMouseButtonDown)
        {
            HandleRightMouseButton();
        }
    }

    public bool TryCast()
    {
        // 检查冷却
        if (cdRemain > 0f)
        {
            DebugEx.LogModule("DashSkill", $"技能冷却中，剩余{cdRemain:F1}秒");
            return false;
        }

        // 检查是否已有道具存在
        if (m_CurrentItem != null && !m_CurrentItem.IsDestroyed)
        {
            DebugEx.WarningModule("DashSkill", "已有道具存在，无法重复使用");
            return false;
        }

        // 检查手部挂点
        if (m_HandTransform == null)
        {
            DebugEx.WarningModule("DashSkill", "手部挂点未找到，尝试重新查找");
            FindHandTransform();

            if (m_HandTransform == null)
            {
                DebugEx.ErrorModule("DashSkill", "手部挂点查找失败，无法使用技能");
                return false;
            }
        }

        // 输出使用技能日志
        DebugEx.LogModule("DashSkill", $"使用技能：{common.Name}");

        // 打开 AimUI
        OpenAimUI();

        // 生成道具
        SpawnItemAsync().Forget();

        // 进入冷却
        cdRemain = common.Cooldown;

        return true;
    }

    #endregion

    #region AimUI 控制

    /// <summary>
    /// 打开 AimUI
    /// </summary>
    private void OpenAimUI()
    {
        // 如果已经打开，先关闭
        if (m_AimUIFormId != -1)
        {
            CloseAimUI();
        }

        m_AimUIFormId = GF.UI.OpenUIForm(UIViews.AimUI);
        DebugEx.LogModule("DashSkill", "打开 AimUI");
    }

    /// <summary>
    /// 关闭 AimUI
    /// </summary>
    private void CloseAimUI()
    {
        if (m_AimUIFormId != -1)
        {
            GF.UI.CloseUIForm(m_AimUIFormId);
            m_AimUIFormId = -1;
            DebugEx.LogModule("DashSkill", "关闭 AimUI");
        }
    }

    #endregion

    #region 道具生成

    /// <summary>
    /// 异步生成道具
    /// </summary>
    private async UniTaskVoid SpawnItemAsync()
    {
        try
        {
            // 使用 ResourceExtension 加载道具预制体
            var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(param.itemResourceId);

            if (prefab == null)
            {
                DebugEx.ErrorModule("DashSkill", $"加载道具预制体失败，ResourceId={param.itemResourceId}");
                // 加载失败时关闭 AimUI
                CloseAimUI();
                return;
            }

            // 实例化道具
            Vector3 spawnPosition = m_HandTransform.position;
            Quaternion spawnRotation = m_HandTransform.rotation;
            GameObject itemObj = Object.Instantiate(prefab, spawnPosition, spawnRotation);

            // 获取控制器组件
            m_CurrentItem = itemObj.GetComponent<DashItemController>();
            if (m_CurrentItem == null)
            {
                m_CurrentItem = itemObj.GetComponentInChildren<DashItemController>();
            }

            if (m_CurrentItem == null)
            {
                DebugEx.ErrorModule("DashSkill", "道具预制体上缺少 DashItemController 组件");
                Object.Destroy(itemObj);
                // 组件缺失时关闭 AimUI
                CloseAimUI();
                return;
            }

            // 初始化道具，传入回调
            m_CurrentItem.Initialize(
                m_HandTransform,
                param.waitingDuration,
                param.flyingDuration,
                param.enableCollision,
                param.gravityScale,
                ctx.Owner,
                OnItemThrown,    // 投掷回调
                OnItemDestroyed  // 销毁回调
            );

            // 设置状态标志
            m_IsWaitingForThrow = true;
            m_IsWaitingForTeleport = false;

            DebugEx.LogModule("DashSkill", "道具生成成功，等待投掷");
        }
        catch (System.Exception ex)
        {
            DebugEx.ErrorModule("DashSkill", $"生成道具时发生错误: {ex.Message}");
            // 异常时关闭 AimUI
            CloseAimUI();
        }
    }

    #endregion

    #region 道具回调

    /// <summary>
    /// 道具投掷回调
    /// </summary>
    private void OnItemThrown()
    {
        DebugEx.LogModule("DashSkill", "道具已投掷");
        // 关闭 AimUI
        CloseAimUI();
    }

    /// <summary>
    /// 道具销毁回调
    /// </summary>
    private void OnItemDestroyed()
    {
        DebugEx.LogModule("DashSkill", "道具已销毁");
        // 关闭 AimUI（防御性编程，确保UI被关闭）
        CloseAimUI();
    }

    #endregion

    #region 输入处理

    /// <summary>
    /// 处理鼠标右键输入
    /// </summary>
    private void HandleRightMouseButton()
    {
        if (m_CurrentItem == null || m_CurrentItem.IsDestroyed)
            return;

        // 等待投掷状态
        if (m_IsWaitingForThrow)
        {
            ThrowItem();
        }
        // 等待传送状态
        else if (m_IsWaitingForTeleport)
        {
            TeleportToItem();
        }
    }

    #endregion

    #region 投掷逻辑

    /// <summary>
    /// 投掷道具
    /// </summary>
    private void ThrowItem()
    {
        if (m_CurrentItem == null || m_CurrentItem.State != DashItemController.DashItemState.Waiting)
            return;

        // 获取主摄像机
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            DebugEx.WarningModule("DashSkill", "找不到主摄像机");
            return;
        }

        // 改进：使用屏幕中心点计算投掷方向
        Vector3 throwDirection = CalculateThrowDirection(mainCamera);

        // 投掷道具（会触发 OnItemThrown 回调，自动关闭 AimUI）
        m_CurrentItem.ThrowItem(throwDirection, param.throwForce);

        // 更新状态标志
        m_IsWaitingForThrow = false;
        m_IsWaitingForTeleport = true;

        DebugEx.LogModule("DashSkill", $"投掷道具，方向={throwDirection}, 力度={param.throwForce}");
    }

    /// <summary>
    /// 计算投掷方向（朝向画面中心，带可选的向上偏移）
    /// </summary>
    private Vector3 CalculateThrowDirection(Camera camera)
    {
        // 获取屏幕中心点
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // 从屏幕中心发射射线
        Ray ray = camera.ScreenPointToRay(screenCenter);

        // 投掷方向 = 从手部位置指向射线上的目标点
        Vector3 targetPoint = ray.GetPoint(50f);
        Vector3 direction = (targetPoint - m_HandTransform.position).normalized;

        // 如果需要向上偏移，使用世界坐标的 up 向量
        if (param.throwAngle > 0f)
        {
            // 计算水平方向（去掉Y分量）
            Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z).normalized;

            // 在垂直平面内旋转（向上偏移）
            float angleRad = param.throwAngle * Mathf.Deg2Rad;
            direction = horizontalDir * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad);
        }

        return direction;
    }

    #endregion

    #region 传送逻辑

    /// <summary>
    /// 传送到道具位置
    /// </summary>
    private void TeleportToItem()
    {
        if (m_CurrentItem == null || m_CurrentItem.State != DashItemController.DashItemState.Flying)
            return;

        if (ctx == null || ctx.Transform == null)
        {
            DebugEx.WarningModule("DashSkill", "上下文为空，无法传送");
            return;
        }

        // 获取道具当前位置
        Vector3 targetPosition = m_CurrentItem.GetCurrentPosition();

        // 计算传送方向（从玩家当前位置指向道具位置）
        Vector3 teleportDirection = (targetPosition - ctx.Transform.position).normalized;

        // 执行传送
        if (ctx.Controller != null)
        {
            // 使用 PlayerController 的 TeleportTo 方法，传入朝向
            ctx.Controller.TeleportTo(targetPosition, teleportDirection);
            DebugEx.LogModule("DashSkill", $"传送到道具位置: {targetPosition}, 朝向: {teleportDirection}");
        }
        else
        {
            // 否则直接设置位置和朝向
            ctx.Transform.position = targetPosition;

            // 手动设置朝向
            if (teleportDirection.sqrMagnitude > 0.01f)
            {
                Vector3 horizontalDir = new Vector3(teleportDirection.x, 0f, teleportDirection.z).normalized;
                if (horizontalDir.sqrMagnitude > 0.01f)
                {
                    ctx.Transform.rotation = Quaternion.LookRotation(horizontalDir);
                }
            }

            DebugEx.LogModule("DashSkill", $"直接传送到: {targetPosition}, 朝向: {teleportDirection}");
        }

        // 销毁道具（会触发 OnItemDestroyed 回调，自动关闭 AimUI）
        m_CurrentItem.DestroyItem();
        m_CurrentItem = null;

        // 清理状态标志
        m_IsWaitingForThrow = false;
        m_IsWaitingForTeleport = false;
    }

    #endregion
}
