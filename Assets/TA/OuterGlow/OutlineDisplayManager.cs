using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/// <summary>
/// 高性能外轮廓显示管理器 - 统一管控所有对象
/// </summary>
public class OutlineDisplayManager : MonoBehaviour
{
    #region 单例
    private static OutlineDisplayManager _instance;
    public static OutlineDisplayManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<OutlineDisplayManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("OutlineDisplayManager");
                    _instance = go.AddComponent<OutlineDisplayManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    #endregion

    #region 配置
    [System.Serializable]
    public class LayerOutlineMapping
    {
        [Tooltip("Layer名称")]
        public string LayerName;
        
        [Tooltip("对应的外轮廓配置")]
        public OutlineConfig OutlineConfig;
        
        [Tooltip("是否启用")]
        public bool Enabled = true;
        
        [HideInInspector] public int LayerIndex = -1;
    }

    [Header("=== Layer配置 ===")]
    public List<LayerOutlineMapping> LayerMappings = new List<LayerOutlineMapping>();

    [Header("=== 检测范围 ===")]
    [Range(10f, 1000f)]
    public float DetectionRadius = 100f;
    public Camera TargetCamera;

    [Header("=== 检测方式 ===")]
    [Tooltip("使用物理检测（需要Collider）")]
    public bool UsePhysicsDetection = true;
    
    [Tooltip("使用Renderer查找（无需Collider，但性能较低）")]
    public bool UseRendererFallback = true;

    [Header("=== 性能优化 ===")]
    [Tooltip("扫描频率（Hz）")]
    [Range(1, 30)]
    public int ScanFrequency = 10;

    [Tooltip("Layer检测频率（Hz）")]
    [Range(10, 60)]
    public int LayerCheckFrequency = 30;

    [Tooltip("每帧最大处理数")]
    [Range(10, 500)]
    public int MaxProcessPerFrame = 100;

    [Tooltip("启用LOD系统")]
    public bool EnableLOD = true;

    [Tooltip("近距离阈值")]
    [Range(10f, 100f)]
    public float NearDistance = 30f;

    [Tooltip("中距离阈值")]
    [Range(30f, 200f)]
    public float MidDistance = 80f;

    [Header("=== 调试信息 ===")]
    [SerializeField] private int _totalTracked;
    [SerializeField] private int _activeOutlines;
    [SerializeField] private int _layerChanges;
    [SerializeField] private float _scanTime;
    [SerializeField] private float _layerCheckTime;
    [SerializeField] private int _queuedCount;
    [SerializeField] private string _debugInfo = "";
    #endregion

    #region 数据结构
    private class TrackedObject
    {
        public GameObject GameObject;
        public Transform Transform;
        public OutlineTest Component;
        public int InstanceID;
        
        public int CurrentLayer;
        public OutlineConfig CurrentConfig;
        public bool IsActive;
        public bool IsInRange;
        public float Distance;
        
        public int UpdateInterval;
        public int LastUpdateFrame;
        
        public Vector3 LastPosition;
        public int LastLayer;
        public bool LastActiveState;

        public TrackedObject(GameObject obj)
        {
            GameObject = obj;
            Transform = obj.transform;
            InstanceID = obj.GetInstanceID();
            CurrentLayer = obj.layer;
            LastLayer = obj.layer;
            IsActive = obj.activeInHierarchy;
            LastActiveState = IsActive;
            LastPosition = Transform.position;
            UpdateInterval = 1;
            LastUpdateFrame = -999;
        }
    }

    private Dictionary<int, TrackedObject> _tracked = new Dictionary<int, TrackedObject>(1000);
    private Dictionary<int, OutlineConfig> _layerConfigMap = new Dictionary<int, OutlineConfig>();
    
    private Queue<TrackedObject> _highPriority = new Queue<TrackedObject>(100);
    private Queue<TrackedObject> _midPriority = new Queue<TrackedObject>(200);
    private Queue<TrackedObject> _lowPriority = new Queue<TrackedObject>(500);
    
    private List<int> _tempRemoveList = new List<int>(50);
    private Collider[] _colliderBuffer = new Collider[500];
    
    private Vector3 _lastCameraPos;
    private int _frameCounter;
    private float _layerChangeCounter;
    private float _layerChangeTimer;
    #endregion

    #region Unity生命周期
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }

    void Start()
    {
        StartCoroutine(ScanCoroutine());
        StartCoroutine(LayerCheckCoroutine());
    }

    void Update()
    {
        _frameCounter++;
        ProcessQueues();
        UpdateStats();
    }

    void OnDestroy()
    {
        CleanupAll();
    }
    #endregion

    #region 初始化
    private void Initialize()
    {
        if (TargetCamera == null)
            TargetCamera = Camera.main;

        BuildLayerConfigMap();
        
        // 验证渲染器
        if (OutlineRenderFeature.Instance == null)
        {
            Debug.LogError("[OutlineManager] OutlineRenderFeature.Instance 为空！请确认已在URP Renderer中添加OutlineRenderFeature");
            _debugInfo = "错误：OutlineRenderFeature未配置";
        }
        else
        {
            Debug.Log("[OutlineManager] 初始化完成 - 统一管控模式");
            _debugInfo = "运行正常";
        }
    }

    /// <summary>
    /// ⭐ 修复：正确构建Layer映射
    /// </summary>
    private void BuildLayerConfigMap()
    {
        _layerConfigMap.Clear();

        foreach (var mapping in LayerMappings)
        {
            if (!mapping.Enabled || mapping.OutlineConfig == null)
                continue;

            // ✅ 修复：使用LayerName获取Layer索引
            int layerIndex = LayerMask.NameToLayer(mapping.LayerName);
            
            if (layerIndex < 0)
            {
                Debug.LogError($"[OutlineManager] Layer '{mapping.LayerName}' 不存在！请检查Layer名称拼写");
                continue;
            }

            mapping.LayerIndex = layerIndex;
            _layerConfigMap[layerIndex] = mapping.OutlineConfig;
            
            Debug.Log($"[OutlineManager] ✅ 映射成功: Layer[{layerIndex}] '{mapping.LayerName}' -> '{mapping.OutlineConfig.name}'");
        }

        if (_layerConfigMap.Count == 0)
        {
            Debug.LogWarning("[OutlineManager] 警告：没有配置任何Layer映射！");
            _debugInfo = "警告：无Layer映射";
        }
    }
    #endregion

    #region 扫描系统
    private IEnumerator ScanCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f / ScanFrequency);

        while (true)
        {
            yield return wait;

            if (TargetCamera == null)
            {
                TargetCamera = Camera.main;
                continue;
            }

            PerformScan();
        }
    }

    /// <summary>
    /// ⭐ 修复：改进扫描逻辑
    /// </summary>
    private void PerformScan()
    {
        float startTime = Time.realtimeSinceStartup;
        Vector3 camPos = TargetCamera.transform.position;
        _lastCameraPos = camPos;

        HashSet<int> currentFrame = new HashSet<int>(500);

        // ✅ 方法1：物理检测（快速但需要Collider）
        if (UsePhysicsDetection)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(camPos, DetectionRadius, _colliderBuffer);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _colliderBuffer[i];
                if (col == null || col.gameObject == null)
                    continue;

                ProcessDetectedObject(col.gameObject, camPos, currentFrame);
            }
        }

        // ✅ 方法2：Renderer查找（慢但能找到所有对象）
        if (UseRendererFallback)
        {
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            
            foreach (var renderer in allRenderers)
            {
                if (renderer == null || renderer.gameObject == null)
                    continue;

                float dist = Vector3.Distance(camPos, renderer.transform.position);
                if (dist <= DetectionRadius)
                {
                    ProcessDetectedObject(renderer.gameObject, camPos, currentFrame);
                }
            }
        }

        CleanupOutOfRange(currentFrame);

        _scanTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        _totalTracked = _tracked.Count;
    }

    /// <summary>
    /// ⭐ 新增：统一处理检测到的对象
    /// </summary>
    private void ProcessDetectedObject(GameObject obj, Vector3 camPos, HashSet<int> currentFrame)
    {
        int id = obj.GetInstanceID();
        currentFrame.Add(id);

        int layer = obj.layer;
        
        // 检查是否有配置
        if (!_layerConfigMap.ContainsKey(layer))
            return;

        float distance = Vector3.Distance(camPos, obj.transform.position);

        if (_tracked.TryGetValue(id, out TrackedObject tracked))
        {
            UpdateTracked(tracked, distance);
        }
        else
        {
            CreateTracked(obj, layer, distance);
        }
    }

    private void UpdateTracked(TrackedObject tracked, float distance)
    {
        tracked.Distance = distance;
        tracked.IsInRange = distance <= DetectionRadius;
        
        if (EnableLOD)
        {
            UpdateLOD(tracked);
        }

        if (ShouldUpdateThisFrame(tracked))
        {
            EnqueueByDistance(tracked);
        }
    }

    private void CreateTracked(GameObject obj, int layer, float distance)
    {
        TrackedObject tracked = new TrackedObject(obj)
        {
            Distance = distance,
            IsInRange = true,
            CurrentConfig = _layerConfigMap[layer],
            CurrentLayer = layer
        };

        _tracked[tracked.InstanceID] = tracked;

        if (EnableLOD)
        {
            UpdateLOD(tracked);
        }

        _highPriority.Enqueue(tracked);
        
        Debug.Log($"[OutlineManager] 新追踪对象: {obj.name}, Layer: {LayerMask.LayerToName(layer)}, 距离: {distance:F1}m");
    }

    private void CleanupOutOfRange(HashSet<int> currentFrame)
    {
        _tempRemoveList.Clear();

        foreach (var kvp in _tracked)
        {
            TrackedObject tracked = kvp.Value;

            if (tracked.GameObject == null)
            {
                RemoveOutline(tracked);
                _tempRemoveList.Add(kvp.Key);
                continue;
            }

            if (!currentFrame.Contains(kvp.Key))
            {
                if (tracked.IsInRange)
                {
                    tracked.IsInRange = false;
                    RemoveOutline(tracked);
                }

                float dist = Vector3.Distance(_lastCameraPos, tracked.Transform.position);
                if (dist > DetectionRadius * 1.5f)
                {
                    _tempRemoveList.Add(kvp.Key);
                }
            }
        }

        foreach (int id in _tempRemoveList)
        {
            _tracked.Remove(id);
        }
    }
    #endregion

    #region Layer变化检测
    private IEnumerator LayerCheckCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f / LayerCheckFrequency);

        while (true)
        {
            yield return wait;
            CheckLayerChanges();
        }
    }

    private void CheckLayerChanges()
    {
        float startTime = Time.realtimeSinceStartup;
        int changes = 0;

        foreach (var tracked in _tracked.Values)
        {
            if (tracked.GameObject == null)
                continue;

            int currentLayer = tracked.GameObject.layer;
            bool currentActive = tracked.GameObject.activeInHierarchy;

            if (currentLayer != tracked.LastLayer)
            {
                HandleLayerChange(tracked, currentLayer);
                tracked.LastLayer = currentLayer;
                changes++;
            }

            if (currentActive != tracked.LastActiveState)
            {
                HandleActiveChange(tracked, currentActive);
                tracked.LastActiveState = currentActive;
            }
        }

        _layerCheckTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        _layerChangeCounter += changes;
    }

    private void HandleLayerChange(TrackedObject tracked, int newLayer)
    {
        string oldLayerName = LayerMask.LayerToName(tracked.CurrentLayer);
        string newLayerName = LayerMask.LayerToName(newLayer);
        
        Debug.Log($"[OutlineManager] ⭐ Layer变化: {tracked.GameObject.name} [{oldLayerName}] -> [{newLayerName}]");

        RemoveOutline(tracked);
        tracked.CurrentLayer = newLayer;

        if (_layerConfigMap.TryGetValue(newLayer, out OutlineConfig newConfig))
        {
            tracked.CurrentConfig = newConfig;

            if (tracked.IsInRange && tracked.IsActive)
            {
                _highPriority.Enqueue(tracked);
                Debug.Log($"[OutlineManager] ✅ 应用新配置: {newConfig.name}");
            }
        }
        else
        {
            tracked.CurrentConfig = null;
            if (tracked.Component != null)
            {
                Destroy(tracked.Component);
                tracked.Component = null;
            }
            Debug.Log($"[OutlineManager] 新Layer无配置");
        }
    }

    private void HandleActiveChange(TrackedObject tracked, bool isActive)
    {
        tracked.IsActive = isActive;

        if (isActive)
        {
            if (tracked.IsInRange && tracked.CurrentConfig != null)
            {
                _highPriority.Enqueue(tracked);
            }
        }
        else
        {
            RemoveOutline(tracked);
        }
    }
    #endregion

    #region LOD系统
    private void UpdateLOD(TrackedObject tracked)
    {
        if (tracked.Distance < NearDistance)
            tracked.UpdateInterval = 1;
        else if (tracked.Distance < MidDistance)
            tracked.UpdateInterval = 3;
        else if (tracked.Distance < DetectionRadius * 0.8f)
            tracked.UpdateInterval = 5;
        else
            tracked.UpdateInterval = 10;
    }

    private bool ShouldUpdateThisFrame(TrackedObject tracked)
    {
        if (!EnableLOD)
            return true;

        int frameDelta = _frameCounter - tracked.LastUpdateFrame;
        return frameDelta >= tracked.UpdateInterval;
    }
    #endregion

    #region 队列处理
    private void EnqueueByDistance(TrackedObject tracked)
    {
        if (tracked.Distance < NearDistance)
            _highPriority.Enqueue(tracked);
        else if (tracked.Distance < MidDistance)
            _midPriority.Enqueue(tracked);
        else
            _lowPriority.Enqueue(tracked);
    }

    private void ProcessQueues()
    {
        int processed = 0;
        int maxProcess = MaxProcessPerFrame;

        while (_highPriority.Count > 0 && processed < maxProcess * 0.5f)
        {
            ProcessObject(_highPriority.Dequeue());
            processed++;
        }

        while (_midPriority.Count > 0 && processed < maxProcess * 0.8f)
        {
            ProcessObject(_midPriority.Dequeue());
            processed++;
        }

        while (_lowPriority.Count > 0 && processed < maxProcess)
        {
            ProcessObject(_lowPriority.Dequeue());
            processed++;
        }

        _queuedCount = _highPriority.Count + _midPriority.Count + _lowPriority.Count;
    }

    private void ProcessObject(TrackedObject tracked)
    {
        if (tracked.GameObject == null || !tracked.IsActive || !tracked.IsInRange)
        {
            RemoveOutline(tracked);
            return;
        }

        if (tracked.CurrentConfig == null)
            return;

        ApplyOutline(tracked);
        tracked.LastUpdateFrame = _frameCounter;
    }
    #endregion

    #region 外轮廓操作
    /// <summary>
    /// ⭐ 增强：添加详细日志
    /// </summary>
    private void ApplyOutline(TrackedObject tracked)
    {
        if (tracked.Component == null)
        {
            tracked.Component = tracked.GameObject.GetComponent<OutlineTest>();
            if (tracked.Component == null)
            {
                tracked.Component = tracked.GameObject.AddComponent<OutlineTest>();
                Debug.Log($"[OutlineManager] 添加OutlineTest组件到 {tracked.GameObject.name}");
            }
        }

        float size = tracked.CurrentConfig.CalculateOutlineSize(tracked.Distance);
        
        if (size <= 0.1f)
        {
            Debug.LogWarning($"[OutlineManager] 外轮廓宽度过小({size:F2})，可能不可见。距离: {tracked.Distance:F1}m");
        }

        tracked.Component.ApplyOutline(tracked.CurrentConfig, size);
        
        Debug.Log($"[OutlineManager] ✅ 应用外轮廓: {tracked.GameObject.name}, " +
                  $"配置: {tracked.CurrentConfig.name}, 宽度: {size:F1}, 颜色: {tracked.CurrentConfig.OutlineColor}");
    }

    private void RemoveOutline(TrackedObject tracked)
    {
        if (tracked.Component != null)
        {
            tracked.Component.RemoveOutline();
        }
    }
    #endregion

    #region 统计
    private void UpdateStats()
    {
        _activeOutlines = 0;
        foreach (var tracked in _tracked.Values)
        {
            if (tracked.Component != null && tracked.Component.IsOutlineActive)
            {
                _activeOutlines++;
            }
        }

        _layerChangeTimer += Time.deltaTime;
        if (_layerChangeTimer >= 1f)
        {
            _layerChanges = Mathf.RoundToInt(_layerChangeCounter);
            _layerChangeCounter = 0;
            _layerChangeTimer = 0;
        }
    }
    #endregion

    #region 公共API
    public void NotifyLayerChanged(GameObject obj, int newLayer)
    {
        if (obj == null)
            return;

        int id = obj.GetInstanceID();
        if (_tracked.TryGetValue(id, out TrackedObject tracked))
        {
            HandleLayerChange(tracked, newLayer);
        }
    }

    public void RefreshObject(GameObject obj)
    {
        if (obj == null)
            return;

        int id = obj.GetInstanceID();
        if (_tracked.TryGetValue(id, out TrackedObject tracked))
        {
            _highPriority.Enqueue(tracked);
        }
    }

    public void RefreshLayerMappings()
    {
        BuildLayerConfigMap();

        foreach (var tracked in _tracked.Values)
        {
            if (tracked.GameObject != null)
            {
                HandleLayerChange(tracked, tracked.GameObject.layer);
            }
        }
    }

    public void CleanupAll()
    {
        foreach (var tracked in _tracked.Values)
        {
            RemoveOutline(tracked);
        }
        _tracked.Clear();
        _highPriority.Clear();
        _midPriority.Clear();
        _lowPriority.Clear();
    }

    public string GetStats()
    {
        return $"追踪: {_totalTracked} | 激活: {_activeOutlines} | Layer变化: {_layerChanges}/s | {_debugInfo}\n" +
               $"扫描: {_scanTime:F2}ms | Layer检测: {_layerCheckTime:F2}ms | 队列: {_queuedCount}";
    }
    #endregion

    #region 调试
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (TargetCamera == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(TargetCamera.transform.position, DetectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(TargetCamera.transform.position, NearDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(TargetCamera.transform.position, MidDistance);

        if (_tracked != null)
        {
            foreach (var tracked in _tracked.Values)
            {
                if (tracked.GameObject == null || !tracked.IsInRange)
                    continue;

                Gizmos.color = tracked.Component != null && tracked.Component.IsOutlineActive 
                    ? Color.green 
                    : Color.red;
                Gizmos.DrawLine(TargetCamera.transform.position, tracked.Transform.position);
                Gizmos.DrawWireSphere(tracked.Transform.position, 1f);
            }
        }
    }
#endif
    #endregion
}