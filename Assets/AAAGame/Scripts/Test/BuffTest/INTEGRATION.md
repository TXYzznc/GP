# Buff 测试工具 - 集成指南

## 📋 文件清单

| 文件名 | 说明 | 必需 |
|-------|------|------|
| `BuffTestTool.cs` | 核心逻辑（应用、移除、查询 Buff） | ✅ |
| `BuffPresetManager.cs` | 预设管理（保存和加载 Buff 组合） | ✅ |
| `BuffEffectVerifier.cs` | 效果验证（验证 Buff 是否生效） | ✅ |
| `BuffTestUIManager.cs` | UI 界面（可视化操作面板） | ✅ |
| `BuffTestIntegration.cs` | 集成脚本（一键初始化） | ✅ |
| `BuffTestExample.cs` | 使用示例（12 个常见场景） | ❌ |
| `README.md` | 快速开始指南 | 📖 |
| `INTEGRATION.md` | 本文件 | 📖 |

---

## 🚀 三种集成方式

### **方式 1：自动集成（推荐）**

最简单，一行代码搞定：

```csharp
// 在 CombatUI.cs 的任何初始化方法中添加
var testIntegration = gameObject.AddComponent<BuffTestIntegration>();
testIntegration.Initialize();
```

**优点**：
- 一行代码完成初始化
- 自动创建 UI 和快捷键处理
- 开箱即用

**快捷键**：
- `Ctrl + B`：打开/关闭工具
- `F`：快速选择目标

---

### **方式 2：手动集成**

如果需要更多控制：

```csharp
// 1. 创建 UI 管理器
var uiGO = new GameObject("BuffTestUIManager");
var uiManager = uiGO.AddComponent<BuffTestUIManager>();

// 2. 设置目标
uiManager.SetTarget(target);

// 3. 显示 UI
uiManager.ShowUI();

// 4. 也可以直接调用工具
BuffTestTool.Instance.ApplyBuffToTarget(10101, target);
```

**优点**：
- 更灵活的控制
- 可以集成到自定义 UI 中
- 不需要快捷键

---

### **方式 3：纯代码使用**

无需 UI，直接在代码中测试：

```csharp
// 直接调用工具 API
BuffTestTool.Instance.ApplyBuffToTarget(10101, target);
BuffTestTool.Instance.ApplyBuffs(new int[] { 10101, 10102 }, target);
BuffTestTool.Instance.ClearAllBuffs(target);

// 验证效果
var report = BuffEffectVerifier.Instance.GenerateTestReport(target);
Debug.Log(report);
```

**优点**：
- 最轻量级
- 完全不依赖 UI
- 适合自动化测试

---

## 📍 推荐集成位置

### **选项 A：在 CombatUI 中**（推荐）

```csharp
// Assets/AAAGame/Scripts/UI/CombatUI.cs

public partial class CombatUI : StateAwareUIForm
{
    private BuffTestIntegration m_BuffTestIntegration;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 初始化 Buff 测试工具
        var testGO = new GameObject("BuffTestIntegration");
        testGO.transform.SetParent(gameObject.transform);
        m_BuffTestIntegration = testGO.AddComponent<BuffTestIntegration>();
        m_BuffTestIntegration.Initialize();

        DebugEx.LogModule("CombatUI", "Buff 测试工具已初始化");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (m_BuffTestIntegration != null)
        {
            Destroy(m_BuffTestIntegration.gameObject);
        }

        base.OnClose(isShutdown, userData);
    }
}
```

**位置**：`OnOpen()` 方法中添加初始化代码

---

### **选项 B：在 CombatState 中**

```csharp
// Assets/AAAGame/Scripts/Game/States/CombatState.cs

public class CombatState : FsmState<InGameState>
{
    private BuffTestIntegration m_BuffTestIntegration;

    protected override void OnEnter(IFsm<InGameState> fsm)
    {
        base.OnEnter(fsm);

        // 初始化工具
        var testGO = new GameObject("BuffTestIntegration");
        m_BuffTestIntegration = testGO.AddComponent<BuffTestIntegration>();
        m_BuffTestIntegration.Initialize();
    }

    protected override void OnLeave(IFsm<InGameState> fsm)
    {
        if (m_BuffTestIntegration != null)
        {
            Object.Destroy(m_BuffTestIntegration.gameObject);
        }

        base.OnLeave(fsm);
    }
}
```

**位置**：`OnEnter()` 方法中添加初始化代码

---

### **选项 C：独立测试脚本**

```csharp
// Assets/AAAGame/Scripts/Test/BuffTest/BuffTestRunner.cs

public class BuffTestRunner : MonoBehaviour
{
    private void Start()
    {
        // 初始化工具
        gameObject.AddComponent<BuffTestIntegration>().Initialize();

        // 可选：运行自动化测试
        var example = gameObject.AddComponent<BuffTestExample>();
        example.RunAllExamples();
    }
}
```

**位置**：场景中新建一个空 GameObject，挂上这个脚本

---

## ⚙️ 配置说明

### **修改默认预设**

编辑 `BuffPresetManager.cs`：

```csharp
private void InitializeDefaultPresets()
{
    // 修改这里的 Buff ID
    AddPreset("伤害组合", new int[] { 10101, 10102 });
    AddPreset("控制组合", new int[] { 10104, 10105 });
    AddPreset("防守组合", new int[] { 10301, 10103 });
    AddPreset("辅助组合", new int[] { 10201, 10106 });
}
```

**获取正确的 Buff ID：**

```csharp
var allBuffs = BuffTestTool.Instance.GetAllAvailableBuffs();
foreach (var buff in allBuffs)
{
    Debug.Log($"{buff.Name} = {buff.BuffId}");
}
```

---

### **修改快捷键**

编辑 `BuffTestIntegration.cs`：

```csharp
private void Update()
{
    // 修改这里的快捷键
    if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.LeftControl))
    {
        if (m_UIManager != null)
        {
            m_UIManager.ToggleUI();
        }
    }

    // 修改选择目标的快捷键
    if (Input.GetKeyDown(KeyCode.F))
    {
        SelectTargetFromMouse();
    }
}
```

---

## 🎯 常见集成场景

### **场景 1：在战斗开始时初始化**

```csharp
// 在战斗触发时
public void OnCombatStart()
{
    var testGO = new GameObject("BuffTestIntegration");
    testGO.AddComponent<BuffTestIntegration>().Initialize();
}
```

---

### **场景 2：创建测试菜单**

```csharp
// 添加到游戏主菜单中
public void CreateDebugMenu()
{
    var menuGO = new GameObject("DebugMenu");
    
    // 创建 "打开 Buff 测试工具" 按钮
    var button = CreateButton(menuGO, "打开 Buff 测试工具");
    button.onClick.AddListener(() =>
    {
        menuGO.AddComponent<BuffTestIntegration>().Initialize();
    });
}
```

---

### **场景 3：自动化 CI/CD 测试**

```csharp
// Assets/AAAGame/Scripts/Test/BuffTest/BuffTestAutoRunner.cs

public class BuffTestAutoRunner : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void AutoRun()
    {
        if (Application.isPlaying && IsDebugMode())
        {
            var tester = new GameObject("AutoBuffTester");
            tester.AddComponent<BuffTestExample>().RunAllExamples();
        }
    }

    private static bool IsDebugMode()
    {
        return Debug.isDebugBuild;
    }
}
```

---

## 🔍 验证集成是否成功

### **检查 1：UI 是否显示**

```csharp
// 按 Ctrl+B 后，右下角应该出现测试工具面板
// 如果没有出现，检查：
var uiManager = FindObjectOfType<BuffTestUIManager>();
if (uiManager == null)
{
    Debug.LogError("BuffTestUIManager 未找到");
}
```

---

### **检查 2：Buff 是否能应用**

```csharp
// 手动测试
var target = FindObjectOfType<ChessEntity>().gameObject;
BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

// 检查日志是否输出
// 预期输出: "[BuffTestTool] ✓ 应用 Buff..."
```

---

### **检查 3：预设是否正常工作**

```csharp
// 测试预设
var presets = BuffPresetManager.Instance.GetAllPresets();
Debug.Log($"预设数量: {presets.Count}");

// 预期输出: "预设数量: 4" (默认预设数)
```

---

## ❌ 常见问题和解决

### **问题 1：UI 没有显示**

```
症状：按 Ctrl+B 后 UI 没有出现
原因：Canvas 可能未正确创建

解决：
1. 检查场景中是否有 Canvas
2. 检查 BuffTestUIManager 是否成功创建
3. 查看控制台是否有报错
```

---

### **问题 2：找不到目标**

```
症状：设置目标时报错 "目标对象为空"
原因：没有正确的 ChessEntity 实体

解决：
1. 确保场景中有棋子实体
2. 确保棋子上有 BuffManager 组件
3. 使用 F 键快速选择目标
```

---

### **问题 3：Buff 无法应用**

```
症状：应用 Buff 时报错
原因：BuffTable 未加载或 Buff ID 错误

解决：
1. 确认 BuffTable 已加载
2. 使用 GetAllAvailableBuffs() 查询正确的 ID
3. 检查目标是否有 ChessAttribute 组件
```

---

## 🧪 测试集成的完整脚本

创建这个脚本快速验证集成是否成功：

```csharp
// Assets/AAAGame/Scripts/Test/BuffTest/BuffTestIntegrationTest.cs

public class BuffTestIntegrationTest : MonoBehaviour
{
    public void TestIntegration()
    {
        DebugEx.LogModule("BuffTestIntegrationTest", "=== 开始集成测试 ===");

        // 1. 检查工具是否初始化
        if (BuffTestTool.Instance == null)
        {
            DebugEx.ErrorModule("BuffTestIntegrationTest", "✗ BuffTestTool 未初始化");
            return;
        }
        DebugEx.LogModule("BuffTestIntegrationTest", "✓ BuffTestTool 已初始化");

        // 2. 检查预设管理器
        var presets = BuffPresetManager.Instance.GetAllPresets();
        DebugEx.LogModule("BuffTestIntegrationTest", $"✓ 预设管理器就绪 (共 {presets.Count} 个预设)");

        // 3. 检查效果验证器
        var allBuffs = BuffTestTool.Instance.GetAllAvailableBuffs();
        DebugEx.LogModule("BuffTestIntegrationTest", $"✓ 效果验证器就绪 (共 {allBuffs.Count} 个 Buff)");

        // 4. 测试应用 Buff
        var target = FindObjectOfType<ChessEntity>();
        if (target != null)
        {
            BuffTestTool.Instance.ApplyBuffToTarget(10101, target.gameObject);
            var result = BuffEffectVerifier.Instance.VerifyBuffApplied(10101, target.gameObject);
            if (result.IsApplied)
            {
                DebugEx.LogModule("BuffTestIntegrationTest", "✓ Buff 应用测试成功");
                BuffTestTool.Instance.ClearAllBuffs(target.gameObject);
            }
            else
            {
                DebugEx.ErrorModule("BuffTestIntegrationTest", "✗ Buff 应用测试失败");
            }
        }

        DebugEx.LogModule("BuffTestIntegrationTest", "=== 集成测试完成 ===");
    }
}
```

**使用方法**：

```csharp
// 在任何地方调用
var test = gameObject.AddComponent<BuffTestIntegrationTest>();
test.TestIntegration();
```

---

## 📦 打包成 Prefab（可选）

如果想要快速部署，可以将集成脚本打包成预制体：

```csharp
// 创建预制体
1. 在场景中创建一个空 GameObject，命名为 "BuffTestToolPrefab"
2. 添加 BuffTestIntegration 脚本
3. 将 GameObject 拖入 Assets/AAAGame/Prefabs/
4. 后续需要时直接实例化即可：

Instantiate(Resources.Load("Prefabs/BuffTestToolPrefab"));
```

---

## ✅ 集成检查清单

使用以下清单确保集成完成：

- [ ] 所有脚本文件已复制到 `Scripts/Test/BuffTest/` 目录
- [ ] 在目标位置添加了初始化代码
- [ ] 修改了默认预设中的 Buff ID（根据实际情况）
- [ ] 测试了快捷键是否工作（Ctrl+B, F）
- [ ] 成功应用了至少一个 Buff
- [ ] UI 面板能正确显示 Buff 列表
- [ ] 运行了集成测试脚本并通过

---

## 🎓 后续优化建议

集成完成后，可以考虑以下优化：

1. **保存/加载预设**
   - 将预设保存到文件
   - 支持导入导出 CSV

2. **性能优化**
   - 缓存 Buff 列表
   - 优化 UI 更新频率

3. **高级功能**
   - 添加 Buff 参数编辑
   - 实时属性对比
   - 自动化测试脚本生成

4. **CI/CD 集成**
   - 创建命令行工具
   - 生成测试报告

---

## 📞 获取帮助

如遇到问题：

1. 查看 `README.md` 中的常见问题部分
2. 查看控制台日志，寻找错误信息
3. 运行 `BuffTestIntegrationTest` 检查集成状态
4. 参考 `BuffTestExample.cs` 中的示例代码

---

## 📄 许可和版本

- **版本**：1.0
- **用途**：仅用于开发和测试
- **发布**：不要在正式版本中包含此工具

---

**祝集成顺利！** 🚀
