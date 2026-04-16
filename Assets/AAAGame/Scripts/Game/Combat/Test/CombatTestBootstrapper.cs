using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 战斗测试引导器 - 放置在 Test 场景中
/// 进入 PlayMode 后自动初始化 GF 框架并加载战斗必需的配置
///
/// 使用方式：
/// 1. 在 Test 场景中创建一个空 GameObject
/// 2. 挂载此脚本
/// 3. 直接在 Test 场景进入 PlayMode
/// 4. 等待初始化完成后使用 ToolHub 的"战斗模拟器"面板
/// </summary>
public class CombatTestBootstrapper : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("是否在初始化完成后自动隐藏 Launch 场景的 UI 对象")]
    [SerializeField] private bool m_HideLaunchUI = true;

    /// <summary>全局标志：当前是否处于战斗测试模式</summary>
    public static bool IsCombatTestMode { get; private set; }

    /// <summary>初始化是否完成</summary>
    public static bool IsReady { get; private set; }

    /// <summary>测试场景的名称（PreloadProcedure 完成后加载此场景）</summary>
    public static string TestSceneName { get; private set; }

    private void Awake()
    {
        // 标记为战斗测试模式
        IsCombatTestMode = true;
        IsReady = false;

        // ⭐ 硬编码测试场景名（CombatTestBootstrapper 必须放在 Test 场景中）
        TestSceneName = "Test";
        DebugEx.LogModule("CombatTest", $"[Awake] 测试场景名: {TestSceneName}");

        // ⭐ 不被切场景销毁
        DontDestroyOnLoad(gameObject);

        DebugEx.LogModule("CombatTest", $"战斗测试模式已激活，测试场景: {TestSceneName}");
    }

    private void Start()
    {
        // ⭐ 保存测试场景名，防止被 GF 初始化覆盖
        string savedTestScene = TestSceneName;
        DebugEx.LogModule("CombatTest", $"保存测试场景名: {savedTestScene}");

        // 加载 Launch 场景来初始化 GF 框架
        LoadLaunchSceneAsync(savedTestScene).Forget();
    }

    private async UniTaskVoid LoadLaunchSceneAsync(string testSceneName)
    {
        DebugEx.LogModule("CombatTest", "正在加载 Launch 场景以初始化 GF 框架...");

        // 加载 Launch 场景（替换当前场景，因为 GF 需要完整的场景环境）
        UnityEngine.SceneManagement.SceneManager.LoadScene("Launch");

        // 等待 GF 框架初始化（GFBuiltin.Instance 在 Awake 中设置）
        await UniTask.WaitUntil(() => GFBuiltin.Instance != null);
        DebugEx.LogModule("CombatTest", "GF 框架已初始化");

        // ⭐ 恢复测试场景名，确保 PreloadProcedure 能正确识别
        TestSceneName = testSceneName;
        DebugEx.LogModule("CombatTest", $"恢复测试场景名: {TestSceneName}");

        // 等待 DataTable 和棋子系统就绪
        // PreloadProcedure 完成后会设置 IsReady（通过修改后的 PreloadProcedure）
        await UniTask.WaitUntil(() => IsReady);

        DebugEx.LogModule("CombatTest", "战斗测试环境初始化完成！可以使用战斗模拟器面板了。");
    }

    /// <summary>
    /// 由 PreloadProcedure 在测试模式下调用，标记初始化完成
    /// </summary>
    public static void NotifyReady()
    {
        IsReady = true;
        DebugEx.LogModule("CombatTest", "所有系统初始化完成，环境就绪");
    }

    private void OnDestroy()
    {
        IsCombatTestMode = false;
        IsReady = false;
        TestSceneName = null;
    }
}
