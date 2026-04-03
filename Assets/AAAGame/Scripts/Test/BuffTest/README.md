# Buff 测试工具 - 快速开始

## 📦 文件说明

```
BuffTestTool.cs              ← 核心逻辑：应用、移除、查询 Buff
BuffPresetManager.cs         ← 预设管理：保存和加载 Buff 组合
BuffEffectVerifier.cs        ← 效果验证：验证 Buff 是否生效
BuffTestUIManager.cs         ← UI 界面：可视化操作面板
BuffTestIntegration.cs       ← 集成脚本：一键初始化所有功能
README.md                    ← 本文件
```

---

## 🚀 一分钟快速开始

### **步骤 1：在战斗场景中添加工具**

在 `CombatUI.cs` 或任何战斗相关脚本的 `Awake()` 或 `Start()` 方法中添加：

```csharp
public partial class CombatUI : StateAwareUIForm
{
    private void Start()
    {
        // ... 其他初始化代码 ...
        
        // 初始化 Buff 测试工具
        var testGO = new GameObject("BuffTestIntegration");
        testGO.transform.SetParent(transform);
        testGO.AddComponent<BuffTestIntegration>();
    }
}
```

### **步骤 2：打开工具**

进入战斗场景后：
- 按 `Ctrl + B` 打开/关闭测试工具 UI

### **步骤 3：选择目标并测试**

- 按 `F` 鼠标指向棋子快速选择目标
- 在 UI 面板中选择 Buff
- 点击 [应用] 按钮
- 观察 Buff 是否正常生效

---

## 💡 基本用法

### **A. 快速应用单个 Buff**

```csharp
// 创建目标引用（可通过检视面板或代码获取）
var target = GetComponent<ChessEntity>().gameObject;

// 应用 Buff（ID=10101）
BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

// 结果：日志输出 "✓ 应用 Buff..." 并在 UI 中显示
```

### **B. 批量应用预设**

```csharp
// 应用 "伤害组合" 预设
BuffPresetManager.Instance.ApplyPreset("伤害组合", target);

// 或者手动指定多个 Buff
int[] buffIds = { 10101, 10102, 10105 };
BuffTestTool.Instance.ApplyBuffs(buffIds, target);
```

### **C. 验证 Buff 是否生效**

```csharp
// 获取 Buff 详情
var details = BuffEffectVerifier.Instance.GetBuffDetails(target);
foreach (var buff in details)
{
    Debug.Log($"{buff.Name} - 堆叠: {buff.StackCount}");
}

// 检查是否有控制状态（眩晕、冰冻等）
var controls = BuffEffectVerifier.Instance.GetControlStates(target);
if (controls.Contains("眩晕"))
{
    Debug.Log("目标已被眩晕！");
}

// 生成完整的测试报告
var report = BuffEffectVerifier.Instance.GenerateTestReport(target);
Debug.Log(report);
```

---

## 🎯 常见使用场景

### **场景 1：测试单个 Buff 效果**

```csharp
// 在检视面板中选择棋子，按 F 选择，然后操作 UI
// 或者在代码中：
BuffTestTool.Instance.ApplyBuffToTarget(10101, knight);
var attr = BuffEffectVerifier.Instance.GetTargetAttributes(knight);
Debug.Log($"攻击力: {attr.AtkDamage}");
```

### **场景 2：测试 Buff 组合交互**

```csharp
// 应用多个 Buff 看是否会互相影响
BuffPresetManager.Instance.ApplyPreset("控制组合", target);

// 验证两个控制状态是否同时生效
var controls = BuffEffectVerifier.Instance.GetControlStates(target);
Debug.Log($"控制状态数: {controls.Count}");
```

### **场景 3：批量测试所有 Buff**

```csharp
// 遍历所有 Buff 并逐个应用测试
var allBuffs = BuffTestTool.Instance.GetAllAvailableBuffs();
foreach (var buff in allBuffs)
{
    BuffTestTool.Instance.ApplyBuffToTarget(buff.BuffId, target);
    var result = BuffEffectVerifier.Instance.VerifyBuffApplied(buff.BuffId, target);
    Debug.Log($"{result.BuffName}: {(result.IsApplied ? "✓" : "✗")}");
    BuffTestTool.Instance.ClearAllBuffs(target);
}
```

---

## 📊 API 速查表

### **BuffTestTool**

| 方法 | 说明 |
|------|------|
| `ApplyBuffToTarget(id, target)` | 应用单个 Buff |
| `ApplyBuffs(ids[], target)` | 批量应用多个 Buff |
| `RemoveBuffFromTarget(id, target)` | 移除指定 Buff |
| `ClearAllBuffs(target)` | 清空所有 Buff |
| `GetTargetBuffs(target)` | 获取目标的所有 Buff 列表 |
| `GetBuff(id, target)` | 获取指定的 Buff 实例 |
| `GetAllAvailableBuffs()` | 获取所有可用的 Buff 配置 |

### **BuffPresetManager**

| 方法 | 说明 |
|------|------|
| `AddPreset(name, ids[])` | 保存新预设 |
| `ApplyPreset(name, target)` | 应用预设 |
| `LoadPreset(name)` | 获取预设的 Buff 列表 |
| `GetAllPresets()` | 获取所有预设 |
| `DeletePreset(name)` | 删除预设 |

### **BuffEffectVerifier**

| 方法 | 说明 |
|------|------|
| `GetBuffDetails(target)` | 获取所有 Buff 的详细信息 |
| `GetTargetAttributes(target)` | 获取目标的属性（HP、MP 等） |
| `GetControlStates(target)` | 获取目标的控制状态列表 |
| `GetBuffAndDebuffCount(target)` | 统计增益和减益数量 |
| `VerifyBuffApplied(id, target)` | 验证 Buff 是否成功应用 |
| `GenerateTestReport(target)` | 生成完整的测试报告 |

### **BuffTestUIManager**

| 方法 | 说明 |
|------|------|
| `SetTarget(target)` | 设置 UI 的测试目标 |
| `ShowUI()` | 显示 UI 界面 |
| `HideUI()` | 隐藏 UI 界面 |
| `ToggleUI()` | 切换 UI 显示状态 |

---

## ⚡ 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + B` | 打开/关闭工具 UI |
| `F` | 快速选择鼠标指向的棋子 |

---

## 🔍 常见 Buff ID 参考

这些是默认的预设 Buff ID，实际值应根据你的 BuffTable 调整：

| ID | 名称 | 类型 |
|----|------|------|
| 10101 | StatModBuff-攻击+25% | 属性修改 |
| 10102 | BleedBuff-出血 | 周期伤害 |
| 10104 | StunBuff-眩晕 | 控制 |
| 10105 | FrostBuff-冰冻 | 控制 |
| 10301 | 护盾 | 防御 |

**查询完整的 Buff 列表：**

```csharp
var allBuffs = BuffTestTool.Instance.GetAllAvailableBuffs();
foreach (var buff in allBuffs)
{
    Debug.Log($"ID: {buff.BuffId}, Name: {buff.Name}");
}
```

---

## 🐛 故障排除

### **Q1: "目标没有 BuffManager 组件"**

**A:** 确保你选择的目标是正确的实体。检查目标是否有 `BuffManager` 组件：

```csharp
var buffManager = target.GetComponent<BuffManager>();
if (buffManager == null)
{
    Debug.LogError("目标缺少 BuffManager 组件");
}
```

### **Q2: Buff 应用了但看不到效果**

**A:** 使用验证工具查看 Buff 是否真的被应用了：

```csharp
var result = BuffEffectVerifier.Instance.VerifyBuffApplied(10101, target);
Debug.Log($"Buff 已应用: {result.IsApplied}");
Debug.Log($"堆叠数: {result.StackCount}");
```

### **Q3: BuffTable 加载失败**

**A:** 确保 BuffTable 已经在 GameFramework 中加载：

```csharp
var buffTable = GF.DataTable.GetDataTable<BuffTable>();
if (buffTable == null)
{
    Debug.LogError("BuffTable 未加载");
}
```

---

## 🎓 编写自动化测试

### **示例：验证 Buff 叠加**

```csharp
public class BuffStackTest
{
    [Test]
    public void TestBuffStacking()
    {
        var target = new GameObject();
        var buffManager = target.AddComponent<BuffManager>();
        target.AddComponent<ChessAttribute>();

        // 应用相同 Buff 3 次
        for (int i = 0; i < 3; i++)
        {
            BuffTestTool.Instance.ApplyBuffToTarget(10101, target);
        }

        // 验证堆叠
        var buff = BuffTestTool.Instance.GetBuff(10101, target);
        Assert.AreEqual(3, buff.StackCount);
    }
}
```

---

## 💾 保存和加载预设

### **添加自定义预设**

编辑 `BuffPresetManager.cs` 中的 `InitializeDefaultPresets()` 方法：

```csharp
private void InitializeDefaultPresets()
{
    // 现有预设...
    AddPreset("伤害组合", new int[] { 10101, 10102 });

    // 添加新预设
    AddPreset("我的组合", new int[] { 10101, 10105, 10301 });
}
```

### **动态保存预设**

```csharp
// 玩家自定义的 Buff 组合
int[] customBuffs = { 10101, 10102, 10104 };
BuffPresetManager.Instance.AddPreset("超级伤害", customBuffs);

// 后续可以快速应用
BuffPresetManager.Instance.ApplyPreset("超级伤害", target);
```

---

## ✅ 完整的测试工作流

```csharp
// 1. 初始化
var target = GetTestTarget();

// 2. 应用 Buff
BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

// 3. 验证应用
var result = BuffEffectVerifier.Instance.VerifyBuffApplied(10101, target);
if (!result.IsApplied)
{
    Debug.LogError($"Buff 应用失败: {result.Message}");
    return;
}

// 4. 检查效果
var attr = BuffEffectVerifier.Instance.GetTargetAttributes(target);
Debug.Log($"当前攻击力: {attr.AtkDamage}");

// 5. 验证控制状态
var controls = BuffEffectVerifier.Instance.GetControlStates(target);
Debug.Log($"控制状态: {string.Join(", ", controls)}");

// 6. 生成报告
var report = BuffEffectVerifier.Instance.GenerateTestReport(target);
Debug.Log(report);

// 7. 清理
BuffTestTool.Instance.ClearAllBuffs(target);
```

---

## 📝 注意事项

1. ✅ **正式版本中删除** - 这个工具仅用于开发测试，不要在正式版本中发布
2. ✅ **配置 Buff ID** - 根据你的 BuffTable 更新预设中的 ID
3. ✅ **性能考虑** - 不要同时应用超过 50 个 Buff
4. ✅ **时序问题** - 某些 Buff 效果依赖于特定的游戏阶段

---

## 📖 详细文档

完整的使用指南请参考：`项目知识库（AI）/Buff测试工具使用指南.md`

---

祝你测试愉快！🎉
