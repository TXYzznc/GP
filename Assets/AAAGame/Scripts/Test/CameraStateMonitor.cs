using UnityEngine;

/// <summary>
/// Camera状态监控工具 - 用于调试UICamera黑屏问题
/// 将此脚本挂载到UICamera上，实时监控Camera状态
/// </summary>
public class CameraStateMonitor : MonoBehaviour
{
    private Camera m_Camera;
    private int m_LastFrameCount = -1;
    private bool m_LastEnabledState = true;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
        if (m_Camera == null)
        {
            DebugEx.Error("CameraStateMonitor", "未找到Camera组件");
            enabled = false;
            return;
        }

        DebugEx.Log(
            "CameraStateMonitor",
            $"开始监控Camera: {gameObject.name}, InstanceID={gameObject.GetInstanceID()}"
        );
    }

    private void LateUpdate()
    {
        if (m_Camera == null)
            return;

        // 检测Camera enabled状态变化
        if (m_Camera.enabled != m_LastEnabledState)
        {
            DebugEx.Warning(
                "CameraStateMonitor",
                $"[{gameObject.name}] Camera.enabled 状态变化: {m_LastEnabledState} -> {m_Camera.enabled}, Frame={Time.frameCount}"
            );
            m_LastEnabledState = m_Camera.enabled;
        }

        // 每60帧输出一次状态（约1秒）
        if (Time.frameCount - m_LastFrameCount >= 60)
        {
            m_LastFrameCount = Time.frameCount;

            DebugEx.Log(
                "CameraStateMonitor",
                $"[{gameObject.name}] 状态报告 - "
                    + $"enabled={m_Camera.enabled}, "
                    + $"depth={m_Camera.depth}, "
                    + $"clearFlags={m_Camera.clearFlags}, "
                    + $"cullingMask={m_Camera.cullingMask}, "
                    + $"targetTexture={(m_Camera.targetTexture != null ? m_Camera.targetTexture.GetInstanceID().ToString() : "null")}, "
                    + $"Frame={Time.frameCount}"
            );
        }
    }

    private void OnDestroy()
    {
        DebugEx.Log(
            "CameraStateMonitor",
            $"Camera监控结束: {gameObject.name}, InstanceID={gameObject.GetInstanceID()}"
        );
    }
}
