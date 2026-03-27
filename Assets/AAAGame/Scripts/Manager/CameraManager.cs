using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 摄像机管理器 - 负责管理游戏中的摄像机切换和激活
/// </summary>
public class CameraManager : SingletonBase<CameraManager>
{
    #region 属性

    /// <summary>
    /// 当前激活的摄像机
    /// </summary>
    public Camera ActiveCamera { get; private set; }

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
    }

    #endregion

    /// <summary>
    /// 激活指定游戏对象上的摄像机（默认激活第一个）
    /// </summary>
    /// <param name="target">目标游戏对象</param>
    /// <param name="cameraIndex">摄像机索引（默认为0，即第一个）</param>
    /// <returns>是否成功激活摄像机</returns>
    public bool ActivateCamera(GameObject target, int cameraIndex = 0)
    {
        if (target == null)
        {
            Log.Warning("CameraManager: 目标对象为空，无法激活摄像机");
            return false;
        }

        // 获取目标对象上的所有摄像机
        Camera[] cameras = target.GetComponentsInChildren<Camera>(true);

        if (cameras == null || cameras.Length == 0)
        {
            Log.Warning($"CameraManager: 目标对象 {target.name} 上没有找到摄像机组件");
            return false;
        }

        // 检查索引是否有效
        if (cameraIndex < 0 || cameraIndex >= cameras.Length)
        {
            Log.Warning(
                $"CameraManager: 摄像机索引 {cameraIndex} 超出范围 (0-{cameras.Length - 1})，使用第一个摄像机"
            );
            cameraIndex = 0;
        }

        // 禁用之前激活的摄像机
        if (ActiveCamera != null && ActiveCamera != cameras[cameraIndex])
        {
            ActiveCamera.enabled = false;
            Log.Info($"CameraManager: 已禁用摄像机 {ActiveCamera.name}");
        }

        // 激活新摄像机
        Camera targetCamera = cameras[cameraIndex];
        targetCamera.enabled = true;
        ActiveCamera = targetCamera;

        Log.Info($"CameraManager: 已激活摄像机 {targetCamera.name} (位于 {target.name})");
        return true;
    }

    /// <summary>
    /// 直接激活指定的摄像机组件
    /// </summary>
    /// <param name="camera">要激活的摄像机</param>
    /// <returns>是否成功激活</returns>
    public bool ActivateCamera(Camera camera)
    {
        if (camera == null)
        {
            Log.Warning("CameraManager: 摄像机组件为空");
            return false;
        }

        // 禁用之前激活的摄像机
        if (ActiveCamera != null && ActiveCamera != camera)
        {
            ActiveCamera.enabled = false;
            Log.Info($"CameraManager: 已禁用摄像机 {ActiveCamera.name}");
        }

        // 激活新摄像机
        camera.enabled = true;
        ActiveCamera = camera;

        Log.Info($"CameraManager: 已激活摄像机 {camera.name}");
        return true;
    }

    /// <summary>
    /// 禁用当前激活的摄像机
    /// </summary>
    public void DeactivateCurrentCamera()
    {
        if (ActiveCamera != null)
        {
            ActiveCamera.enabled = false;
            Log.Info($"CameraManager: 已禁用摄像机 {ActiveCamera.name}");
            ActiveCamera = null;
        }
    }

    /// <summary>
    /// 获取指定对象上的所有摄像机
    /// </summary>
    /// <param name="target">目标对象</param>
    /// <returns>摄像机数组</returns>
    public Camera[] GetCameras(GameObject target)
    {
        if (target == null)
            return null;
        return target.GetComponentsInChildren<Camera>(true);
    }
}
