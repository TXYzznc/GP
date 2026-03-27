using UnityEngine;

/// <summary>
/// 摄像机注册表
/// 静态类，用于缓存和快速访问各类摄像机引用
/// 避免频繁使用 FindWithTag 或 Camera.main 造成性能损耗
/// </summary>
public static class CameraRegistry
{
    #region 摄像机缓存

    /// <summary>本地玩家摄像机</summary>
    private static Camera s_PlayerCamera;

    /// <summary>UI摄像机</summary>
    private static Camera s_UICamera;

    /// <summary>第三人称摄像机控制器</summary>
    private static ThirdPersonCamera s_ThirdPersonCamera;

    #endregion

    #region 公共属性

    /// <summary>
    /// 获取本地玩家摄像机
    /// 如果未注册，返回 Camera.main 作为备用
    /// </summary>
    public static Camera PlayerCamera
    {
        get
        {
            // ✅ 日志：检查缓存的摄像机状态
            if (s_PlayerCamera != null)
            {
                if (s_PlayerCamera.isActiveAndEnabled)
                {
                    //Debug.Log($"[CameraRegistry] 返回已注册的摄像机: {s_PlayerCamera.name}");
                    return s_PlayerCamera;
                }
                else
                {
                    Debug.LogWarning($"[CameraRegistry] 已注册的摄像机未激活: {s_PlayerCamera.name}, isActive={s_PlayerCamera.gameObject.activeInHierarchy}, enabled={s_PlayerCamera.enabled}");
                }
            }
            else
            {
                Debug.LogWarning("[CameraRegistry] 未注册玩家摄像机，尝试使用 Camera.main 作为备用");
            }

            // 备用方案
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Debug.Log($"[CameraRegistry] 使用 Camera.main 作为备用: {mainCam.name}");
            }
            else
            {
                Debug.LogError("[CameraRegistry] Camera.main 也为空！场景中可能没有标记为 MainCamera 的摄像机");
            }

            return mainCam;
        }
    }

    /// <summary>
    /// 获取UI摄像机
    /// </summary>
    public static Camera UICamera => s_UICamera;

    /// <summary>
    /// 获取第三人称摄像机控制器
    /// </summary>
    public static ThirdPersonCamera ThirdPersonCamera => s_ThirdPersonCamera;

    /// <summary>
    /// 玩家摄像机是否已注册
    /// </summary>
    public static bool HasPlayerCamera => s_PlayerCamera != null;

    #endregion

    #region 注册/注销API

    /// <summary>
    /// 注册玩家摄像机
    /// 在 PlayerCharacterManager.SetupCameraRig() 中调用
    /// </summary>
    /// <param name="camera">玩家摄像机</param>
    /// <param name="cameraRig">第三人称摄像机控制器（可选）</param>
    public static void RegisterPlayerCamera(Camera camera, ThirdPersonCamera cameraRig = null)
    {
        // ✅ 日志：记录注册前的状态
        if (s_PlayerCamera != null)
        {
            Debug.LogWarning($"[CameraRegistry] 覆盖已注册的摄像机: {s_PlayerCamera.name} -> {camera?.name}");
        }

        s_PlayerCamera = camera;
        s_ThirdPersonCamera = cameraRig;

        // ✅ 日志：验证注册结果
        if (camera != null)
        {
            Debug.Log($"[CameraRegistry] ✅ 注册玩家摄像机成功: {camera.name}, Tag={camera.tag}, Active={camera.gameObject.activeInHierarchy}, Enabled={camera.enabled}");

            if (cameraRig != null)
            {
                Debug.Log($"[CameraRegistry] ✅ 注册第三人称摄像机控制器: {cameraRig.name}");
            }
            else
            {
                Debug.LogWarning("[CameraRegistry] ⚠️ 第三人称摄像机控制器为空");
            }
        }
        else
        {
            Debug.LogError("[CameraRegistry] ❌ 注册失败：摄像机为空！");
        }
    }

    /// <summary>
    /// 注销玩家摄像机
    /// 在角色销毁时调用
    /// </summary>
    public static void UnregisterPlayerCamera()
    {
        if (s_PlayerCamera != null)
        {
            Debug.Log($"[CameraRegistry] 注销玩家摄像机: {s_PlayerCamera.name}");
        }
        else
        {
            Debug.LogWarning("[CameraRegistry] 尝试注销摄像机，但当前没有已注册的摄像机");
        }

        s_PlayerCamera = null;
        s_ThirdPersonCamera = null;

        Debug.Log("[CameraRegistry] 摄像机已注销");
    }

    /// <summary>
    /// 注册UI摄像机
    /// </summary>
    /// <param name="camera">UI摄像机</param>
    public static void RegisterUICamera(Camera camera)
    {
        s_UICamera = camera;
        Debug.Log($"[CameraRegistry] 注册UI摄像机: {camera?.name}");
    }

    /// <summary>
    /// 注销UI摄像机
    /// </summary>
    public static void UnregisterUICamera()
    {
        s_UICamera = null;
    }

    /// <summary>
    /// 清除所有缓存
    /// 在场景切换或游戏退出时调用
    /// </summary>
    public static void ClearAll()
    {
        s_PlayerCamera = null;
        s_UICamera = null;
        s_ThirdPersonCamera = null;
        Debug.Log("[CameraRegistry] 已清除所有摄像机缓存");
    }

    #endregion
}
