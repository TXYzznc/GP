using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 敌人探索AI测试启动器
/// 放置在Test场景中，自动初始化GF框架
/// 类似 CombatTestBootstrapper，用于独立测试敌人AI行为
/// </summary>
public class ExploreAITestBootstrapper : MonoBehaviour
{
    /// <summary>全局标志：当前是否处于敌人AI测试模式</summary>
    public static bool IsExploreAITestMode { get; private set; }

    /// <summary>初始化是否完成</summary>
    public static bool IsReady { get; private set; }

    /// <summary>测试场景的名称</summary>
    public static string TestSceneName { get; private set; }

    private void Awake()
    {
        IsExploreAITestMode = true;
        IsReady = false;

        // 测试场景名（必须和实际Test场景同名）
        TestSceneName = "ExploreAITestScene";
        DebugEx.Log(nameof(ExploreAITestBootstrapper), $"敌人AI测试模式已激活，测试场景: {TestSceneName}");

        // 不被切场景销毁
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        string savedTestScene = TestSceneName;
        LoadLaunchSceneAsync(savedTestScene).Forget();
    }

    private async UniTaskVoid LoadLaunchSceneAsync(string testSceneName)
    {
        DebugEx.Log(nameof(ExploreAITestBootstrapper), "正在加载 Launch 场景以初始化 GF 框架...");

        // 加载 Launch 场景初始化 GF
        UnityEngine.SceneManagement.SceneManager.LoadScene("Launch");

        // 等待 GF 框架初始化
        await UniTask.WaitUntil(() => GFBuiltin.Instance != null);
        DebugEx.Log(nameof(ExploreAITestBootstrapper), "GF 框架已初始化");

        // 恢复测试场景名
        TestSceneName = testSceneName;

        // 等待 DataTable 加载完成
        await UniTask.WaitUntil(() => IsReady);

        DebugEx.Log(nameof(ExploreAITestBootstrapper), "敌人AI测试环境初始化完成！");
    }

    /// <summary>
    /// 由测试面板或 PreloadProcedure 调用，标记初始化完成
    /// </summary>
    public static void NotifyReady()
    {
        IsReady = true;
        DebugEx.Log("ExploreAITestBootstrapper", "所有系统初始化完成，环境就绪");
    }

    private void OnDestroy()
    {
        IsExploreAITestMode = false;
        IsReady = false;
        TestSceneName = null;
    }
}
