using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 单例基类 - 提供 DontDestroyOnLoad 单例的标准实现
/// 自动处理编辑器模式下的对象残留问题
/// </summary>
public abstract class SingletonBase<T> : MonoBehaviour
    where T : SingletonBase<T>
{
    #region 单例实例

    protected static T s_Instance;

    public static T Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType<T>();
                if (s_Instance == null)
                {
                    GameObject go = new GameObject($"[{typeof(T).Name}]");
                    s_Instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                    DebugEx.LogModule(typeof(T).Name, $"自动创建单例实例");
                }
            }
            return s_Instance;
        }
    }

    #endregion

    #region Unity 生命周期

    protected virtual void Awake()
    {
        // 检查是否已存在实例
        if (s_Instance != null && s_Instance != this)
        {
            DebugEx.WarningModule(typeof(T).Name, $"检测到重复的单例实例，销毁当前对象");
            Destroy(gameObject);
            return;
        }

        s_Instance = this as T;
        DontDestroyOnLoad(gameObject);

        DebugEx.LogModule(typeof(T).Name, $"单例初始化完成");
    }

    protected virtual void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
            DebugEx.LogModule(typeof(T).Name, $"单例已销毁");
        }
    }

    #endregion

    #region 编辑器清理

#if UNITY_EDITOR

    /// <summary>
    /// 编辑器模式下，停止播放时清理 DontDestroyOnLoad 对象
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void CleanupOnEditorStop()
    {
        EditorSceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        // 在编辑器停止播放时清理单例
        if (!Application.isPlaying)
        {
            if (s_Instance != null)
            {
                DebugEx.LogModule(typeof(T).Name, $"编辑器停止播放，清理单例对象");
                DestroyImmediate(s_Instance.gameObject);
                s_Instance = null;
            }
        }
    }

#endif

    #endregion

    /// <summary>
    /// 手动销毁单例（在需要重新初始化时调用）
    /// </summary>
    public static void DestroyInstance()
    {
        if (s_Instance != null)
        {
            Destroy(s_Instance.gameObject);
            s_Instance = null;
            DebugEx.LogModule(typeof(T).Name, $"单例已手动销毁");
        }
    }
}
