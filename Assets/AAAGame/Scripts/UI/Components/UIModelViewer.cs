using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// UI模型查看器 - 使用RenderTexture在UI中显示3D模型
/// 支持拖拽旋转、双击交互
/// </summary>
public class UIModelViewer : MonoBehaviour
{
    #region 配置参数

    [Header("渲染设置")]
    [SerializeField]
    private int renderTextureWidth = 512;

    [SerializeField]
    private int renderTextureHeight = 512;

    [SerializeField]
    private float cameraDistance = 3f;

    [SerializeField]
    private float cameraHeight = 1f;

    [SerializeField]
    private Vector3 modelOffset = Vector3.zero;

    [Header("交互设置")]
    [SerializeField]
    private float rotationSpeed = 0.5f;

    [SerializeField]
    private float doubleClickThreshold = 0.3f;

    [SerializeField]
    private bool enableDragRotation = true;

    [SerializeField]
    private bool enableDoubleClick = true;

    [Header("光源设置")]
    [SerializeField]
    private LightType lightType = LightType.Directional;

    [SerializeField]
    private float lightIntensity = 1.6f;

    [SerializeField]
    private Color lightColor = Color.white;

    [SerializeField]
    private Vector3 lightPosition = new Vector3(0f, 3f, -2f);

    [SerializeField]
    private Vector3 lightRotation = new Vector3(0f, -20f, 0f); // 欧拉角

    [SerializeField]
    private float spotAngle = 30f; // Spot Light 的圆锥大小

    [SerializeField]
    private float lightRange = 10f; // Point/Spot Light 范围

    [Header("模型位置（独立坐标，远离主场景）")]
    [SerializeField]
    private Vector3 modelWorldPosition = new Vector3(1000f, 0f, 1000f);

    #endregion

    #region 私有字段

    private RawImage m_TargetImage;
    private RenderTexture m_RenderTexture;
    private Camera m_ModelCamera;
    private GameObject m_ModelRoot;
    private GameObject m_CurrentModel;
    private Light m_Light;
    private GameObject m_LightObject;

    // 交互状态
    private bool m_IsDragging = false;
    private float m_LastMouseX = 0f;
    private float m_LastClickTime = 0f;

    // 动画控制 - 使用专门的 ModelController
    private ModelController m_ModelController;

    // 事件
    public System.Action OnDoubleClick;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化模型查看器
    /// </summary>
    /// <param name="targetImage">目标RawImage组件</param>
    public void Initialize(RawImage targetImage)
    {
        m_TargetImage = targetImage;

        // 创建RenderTexture
        CreateRenderTexture();

        // 创建专用相机和模型根节点
        CreateModelCameraAndRoot();

        Log.Info("UIModelViewer 初始化完成");
    }

    /// <summary>
    /// 创建RenderTexture
    /// </summary>
    private void CreateRenderTexture()
    {
        if (m_RenderTexture != null)
        {
            m_RenderTexture.Release();
            Destroy(m_RenderTexture);
        }

        m_RenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
        m_RenderTexture.antiAliasing = 2;
        m_RenderTexture.Create();

        // 将RenderTexture赋值给RawImage
        if (m_TargetImage != null)
        {
            m_TargetImage.texture = m_RenderTexture;
        }
    }

    /// <summary>
    /// 创建专用相机和模型根节点
    /// </summary>
    private void CreateModelCameraAndRoot()
    {
        // 创建模型根节点（放在远离主场景的位置）
        m_ModelRoot = new GameObject("UIModelViewer_Root");
        m_ModelRoot.transform.position = modelWorldPosition;

        // 创建专用相机
        GameObject cameraObj = new GameObject("UIModelViewer_Camera");
        cameraObj.transform.SetParent(m_ModelRoot.transform);
        cameraObj.transform.localPosition = new Vector3(0f, cameraHeight, -cameraDistance);
        cameraObj.transform.LookAt(
            m_ModelRoot.transform.position + Vector3.up * cameraHeight * 0.5f
        );

        m_ModelCamera = cameraObj.AddComponent<Camera>();
        m_ModelCamera.clearFlags = CameraClearFlags.SolidColor;
        m_ModelCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // 透明背景
        m_ModelCamera.cullingMask = 1 << LayerMask.NameToLayer("UI3DModel"); // 只渲染特定层
        m_ModelCamera.targetTexture = m_RenderTexture;
        m_ModelCamera.fieldOfView = 30f;
        m_ModelCamera.nearClipPlane = 0.1f;
        m_ModelCamera.farClipPlane = 100f;

        // 添加光源
        CreateLight();
    }

    /// <summary>
    /// 创建光源
    /// </summary>
    private void CreateLight()
    {
        m_LightObject = new GameObject("UIModelViewer_Light");
        m_LightObject.transform.SetParent(m_ModelRoot.transform);
        m_LightObject.transform.localPosition = lightPosition;
        m_LightObject.transform.localEulerAngles = lightRotation;

        m_Light = m_LightObject.AddComponent<Light>();
        m_Light.type = lightType;
        m_Light.intensity = lightIntensity;
        m_Light.color = lightColor;
        m_Light.cullingMask = 1 << LayerMask.NameToLayer("UI3DModel");

        // 启用软阴影
        m_Light.shadows = LightShadows.Soft;
        m_Light.shadowStrength = 0.6f;

        DebugEx.LogModule("UIModelViewer", $"光源创建完成 - 类型: {lightType}, 强度: {lightIntensity}, 旋转: {lightRotation}, 阴影强度: 0.6");

        // 根据光源类型设置特定参数
        switch (lightType)
        {
            case LightType.Spot:
                m_Light.spotAngle = spotAngle;
                m_Light.range = lightRange;
                break;
            case LightType.Point:
                m_Light.range = lightRange;
                break;
        }
    }

    #endregion

    #region 模型管理

    /// <summary>
    /// 设置模型
    /// </summary>
    /// <summary>
    /// 设置模型
    /// </summary>
    public void SetModel(GameObject modelPrefab)
    {
        // 清除旧模型
        ClearModel();

        if (modelPrefab == null)
            return;

        // 实例化新模型
        m_CurrentModel = Instantiate(modelPrefab, m_ModelRoot.transform);
        m_CurrentModel.transform.localPosition = modelOffset;
        m_CurrentModel.transform.localRotation = Quaternion.identity;
        m_CurrentModel.transform.localScale = Vector3.one;

        // 设置模型层级，确保被专用相机渲染
        LayerHelper.SetLayerRecursively(m_CurrentModel, LayerHelper.Layer.UI3DModel);

        // 添加并初始化 ModelController 组件
        m_ModelController = m_CurrentModel.AddComponent<ModelController>();
        if (m_ModelController != null && m_ModelController.HasValidAnimator())
        {
            DebugEx.LogModule("UIModelViewer", "已添加 ModelController 组件并初始化");
        }
        else
        {
            DebugEx.Warning("UIModelViewer", "ModelController 初始化失败或模型没有 Animator 组件");
        }

        Log.Info($"UIModelViewer 设置模型成功: {modelPrefab.name}");
    }

    /// <summary>
    /// 异步加载并设置模型
    /// </summary>
    public async UniTask SetModelAsync(int resourceConfigId)
    {
        // 检查配置是否存在
        if (!GameExtension.ResourceExtension.HasResourceConfig(resourceConfigId))
        {
            Log.Warning($"模型资源配置不存在: ConfigId={resourceConfigId}");
            return;
        }

        try
        {
            var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(resourceConfigId);
            if (prefab != null)
            {
                SetModel(prefab);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"加载模型失败: ConfigId={resourceConfigId}, Error={ex.Message}");
        }
    }

    /// <summary>
    /// 清除当前模型
    /// </summary>
    /// <summary>
    /// 清除当前模型
    /// </summary>
    public void ClearModel()
    {
        if (m_CurrentModel != null)
        {
            // 清理 ModelController 组件
            if (m_ModelController != null)
            {
                m_ModelController.StopInteractAnimation();
                DebugEx.LogModule("UIModelViewer", "已清理 ModelController 组件");
            }

            Destroy(m_CurrentModel);
            m_CurrentModel = null;
            m_ModelController = null;
        }
    }

    /// <summary>
    /// 递归设置GameObject及其所有子物体的Layer
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    #endregion

    #region 交互处理

    /// <summary>
    /// 处理交互（在Update中调用）
    /// </summary>
    public void HandleInteraction()
    {
        if (m_TargetImage == null || m_CurrentModel == null)
            return;

        // 检查鼠标是否在目标区域内
        if (!IsPointerOverTarget())
        {
            m_IsDragging = false;
            return;
        }

        // 处理双击
        if (enableDoubleClick && Input.GetMouseButtonDown(0))
        {
            float currentTime = Time.time;
            if (currentTime - m_LastClickTime < doubleClickThreshold)
            {
                OnDoubleClick?.Invoke();
            }
            m_LastClickTime = currentTime;
        }

        // 处理拖拽旋转
        if (enableDragRotation)
        {
            HandleDragRotation();
        }
    }

    /// <summary>
    /// 处理拖拽旋转
    /// </summary>
    private void HandleDragRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_IsDragging = true;
            m_LastMouseX = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_IsDragging = false;
        }

        if (m_IsDragging && m_CurrentModel != null)
        {
            float deltaX = Input.mousePosition.x - m_LastMouseX;
            m_CurrentModel.transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.Self);
            m_LastMouseX = Input.mousePosition.x;
        }
    }

    /// <summary>
    /// 检查鼠标是否在目标RawImage区域内
    /// </summary>
    private bool IsPointerOverTarget()
    {
        if (m_TargetImage == null)
            return false;

        RectTransform rectTransform = m_TargetImage.rectTransform;
        Vector2 localPoint;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                Input.mousePosition,
                GF.UICamera,
                out localPoint
            ) && rectTransform.rect.Contains(localPoint);
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 设置模型旋转
    /// </summary>
    public void SetModelRotation(float yAngle)
    {
        if (m_CurrentModel != null)
        {
            m_CurrentModel.transform.localRotation = Quaternion.Euler(0f, yAngle, 0f);
        }
    }

    /// <summary>
    /// 重置模型旋转
    /// </summary>
    public void ResetModelRotation()
    {
        SetModelRotation(0f);
    }

    /// <summary>
    /// 设置相机距离
    /// </summary>
    public void SetCameraDistance(float distance)
    {
        cameraDistance = distance;
        if (m_ModelCamera != null)
        {
            m_ModelCamera.transform.localPosition = new Vector3(0f, cameraHeight, -cameraDistance);
            m_ModelCamera.transform.LookAt(
                m_ModelRoot.transform.position + Vector3.up * cameraHeight * 0.5f
            );
        }
    }

    /// <summary>
    /// 获取当前模型
    /// </summary>
    public GameObject GetCurrentModel()
    {
        return m_CurrentModel;
    }

    /// <summary>
    /// 是否有模型
    /// </summary>
    public bool HasModel()
    {
        return m_CurrentModel != null;
    }

    #endregion

    #region 光源控制

    /// <summary>
    /// 设置光源强度
    /// </summary>
    public void SetLightIntensity(float intensity)
    {
        lightIntensity = intensity;
        if (m_Light != null)
        {
            m_Light.intensity = intensity;
        }
    }

    /// <summary>
    /// 设置光源颜色
    /// </summary>
    public void SetLightColor(Color color)
    {
        lightColor = color;
        if (m_Light != null)
        {
            m_Light.color = color;
        }
    }

    /// <summary>
    /// 设置光源位置
    /// </summary>
    public void SetLightPosition(Vector3 position)
    {
        lightPosition = position;
        if (m_LightObject != null)
        {
            m_LightObject.transform.localPosition = position;
        }
    }

    /// <summary>
    /// 设置光源旋转
    /// </summary>
    public void SetLightRotation(Vector3 eulerAngles)
    {
        lightRotation = eulerAngles;
        if (m_LightObject != null)
        {
            m_LightObject.transform.localEulerAngles = eulerAngles;
        }
    }

    /// <summary>
    /// 设置聚光灯圆锥大小
    /// </summary>
    public void SetSpotAngle(float angle)
    {
        spotAngle = angle;
        if (m_Light != null && m_Light.type == LightType.Spot)
        {
            m_Light.spotAngle = angle;
        }
    }

    /// <summary>
    /// 设置点光源/聚光灯范围
    /// </summary>
    public void SetLightRange(float range)
    {
        lightRange = range;
        if (m_Light != null && (m_Light.type == LightType.Point || m_Light.type == LightType.Spot))
        {
            m_Light.range = range;
        }
    }

    /// <summary>
    /// 设置光源类型（会重新创建光源）
    /// </summary>
    public void SetLightType(LightType type)
    {
        if (lightType == type)
            return;

        lightType = type;

        // 重新创建光源
        if (m_LightObject != null)
        {
            Destroy(m_LightObject);
            CreateLight();
        }
    }

    /// <summary>
    /// 让光源朝向模型中心
    /// </summary>
    public void LookAtModel()
    {
        if (m_LightObject != null && m_ModelRoot != null)
        {
            m_LightObject.transform.LookAt(m_ModelRoot.transform.position);
            lightRotation = m_LightObject.transform.localEulerAngles;
        }
    }

    /// <summary>
    /// 获取光源组件
    /// </summary>
    public Light GetLight()
    {
        return m_Light;
    }

    #endregion

    #region 动画控制

    /// <summary>
    /// 播放交互动画
    /// </summary>
    /// <param name="interactIndex">交互动画索引</param>
    public void PlayInteractAnimation(int interactIndex = 0)
    {
        if (m_ModelController == null)
        {
            DebugEx.Warning("UIModelViewer", "ModelController 为空，无法播放交互动画");
            return;
        }

        m_ModelController.PlayInteractAnimation(interactIndex);
    }

    /// <summary>
    /// 播放 Idle 动画
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (m_ModelController == null)
        {
            DebugEx.Warning("UIModelViewer", "ModelController 为空，无法播放 Idle 动画");
            return;
        }

        m_ModelController.PlayIdleAnimation();
    }

    /// <summary>
    /// 获取 Animator 组件
    /// </summary>
    public Animator GetAnimator()
    {
        if (m_ModelController != null)
        {
            return m_ModelController.Animator;
        }
        return null;
    }

    /// <summary>
    /// 检查是否正在播放交互动画
    /// </summary>
    public bool IsPlayingInteractAnimation()
    {
        if (m_ModelController != null)
        {
            return m_ModelController.IsInteracting;
        }
        return false;
    }

    #endregion

    #region 清理资源

    private void OnDestroy()
    {
        // 清理资源
        ClearModel();

        if (m_RenderTexture != null)
        {
            m_RenderTexture.Release();
            Destroy(m_RenderTexture);
            m_RenderTexture = null;
        }

        if (m_ModelRoot != null)
        {
            Destroy(m_ModelRoot);
            m_ModelRoot = null;
        }

        Log.Info("UIModelViewer 已清理");
    }

    #endregion
}
