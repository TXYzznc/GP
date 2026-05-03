using System.Collections.Generic;
using UnityEngine;

namespace Tool_Plugins.CurvePlacer
{
    /// <summary>
    /// 曲线放置工具的数据（存储在场景对象上）
    /// </summary>
    public class CurvePlacerData : MonoBehaviour
    {
        [Header("曲线节点")]
        public List<Vector3> controlPoints = new List<Vector3>();

        [Header("放置设置")]
        public GameObject prefabToPlace;
        public float objectSpacing = 2f;
        public float segmentStep = 0.1f;
        public bool IsClosed = false;

        // ── 轴锁定 ────────────────────────────────────────────────
        public bool lockAxis = false;
        public int lockAxisIndex = 1; // 0=X  1=Y  2=Z
        public float lockAxisValue = 0f; // 锁定到的坐标值

        // ── 旋转控制 ──────────────────────────────────────────────
        public bool alignToTangent = true;
        public Vector3 rotationOffset = Vector3.zero; // 在切线旋转基础上的偏移
        public Vector3 uniformRotation = Vector3.zero; // 不对齐切线时的统一旋转

        [Header("已生成的对象")]
        public List<GameObject> placedObjects = new List<GameObject>();
    }
}
