# Buff 测试工具使用指南

## 📦 工具组成

```
Assets/AAAGame/Scripts/Test/BuffTest/
├── BuffTestTool.cs              # 核心工具（Buff 应用、移除、查询）
├── BuffPresetManager.cs          # 预设管理（保存/加载 Buff 组合）
├── BuffEffectVerifier.cs         # 效果验证（属性、状态、报告）
├── BuffTestUIManager.cs          # UI 管理（可视化界面）
└── BuffTestIntegration.cs        # 集成脚本（战斗场景初始化）
```

---

## 🚀 快速开始

### 1️⃣ **集成到战斗场景**

在 CombatUI 或其他合适的位置添加 `BuffTestIntegration` 脚本：

```csharp
// 在 CombatUI.cs 中的 OnOpen 方法里
protected override void OnOpen(object userData)
{
    base.OnOpen(userData);
    
    // 初始化 Buff 测试工具
    var testIntegration = gameObject.AddComponent<BuffTestIntegration>();
    testIntegration.Initialize();
}
```

### 2️⃣ **打开工具**

**方式 A：快捷键**
- `Ctrl + B`：打开/关闭测试工具 UI

**方式 B：代码调用**
```csharp
// 获取集成脚本实例
var integration = GetComponent<BuffTestIntegration>();
integration.OpenUI();
```

### 3️⃣ **选择测试目标**

**方式 A：快捷键选择**
- `F`：鼠标指向棋子后按 F 快速选择

**方式 B：UI 选择**
- 在 UI 的 "当前目标" 下拉菜单中选择（需扩展实现）

---

## 📖 使用方式

### **场景 1：快速测试单个 Buff**

```
1. 打开工具 (Ctrl+B)
2. 选择目标 (F 键指向棋子)
3. 在 Buff 下拉菜单中选择要测试的 Buff
4. 点击 [应用] 按钮
5. 观察：
   - UI 中 "当前 Buff 列表" 显示新增的 Buff
   - "属性信息" 显示实时的属性变化
   - 游戏日志输出应用信息
6. 点击 [清空] 移除所有 Buff
```

**日志示例**
```
[BuffTestTool] ✓ 应用 Buff: StatModBuff-攻击+25% (ID=10101) 到 Knight
[BuffTestTool] ✓ 清空 Knight 的所有 Buff（共 1 个）
```

---

### **场景 2：测试 Buff 组合和交互**

```
1. 选择预设方案：[伤害组合]
2. 点击 [应用] 按钮
3. 工具会一次施加预设中的所有 Buff
4. 观察 Buff 之间的交互效果：
   - 同时应用多个 Buff 是否生效
   - 属性修改是否正确叠加
   - 状态改变是否同时生效
```

---

### **场景 3：验证 Buff 生效**

工具会自动显示：

| 信息 | 说明 |
|------|------|
| **当前 Buff 列表** | 实时显示目标身上的所有激活 Buff |
| **堆叠数量** | 同一 Buff 的堆叠层数 |
| **属性信息** | 当前 HP、MP、攻击、防御等属性 |
| **增益/减益** | 增益和减益的数量统计 |

---

## 🛠️ 代码调用示例

### **直接调用工具**

```csharp
// 应用单个 Buff
BuffTestTool.Instance.ApplyBuffToTarget(10101, target);

// 批量应用多个 Buff
int[] buffIds = { 10101, 10102, 10104 };
BuffTestTool.Instance.ApplyBuffs(buffIds, target);

// 移除指定 Buff
BuffTestTool.Instance.RemoveBuffFromTarget(10101, target);

// 清空所有 Buff
BuffTestTool.Instance.ClearAllBuffs(target);

// 获取目标的所有 Buff
var buffList = BuffTestTool.Instance.GetTargetBuffs(target);
foreach (var buff in buffList)
{
    Debug.Log($"Buff: {buff.BuffId}, 堆叠: {buff.StackCount}");
}
```

---

### **验证 Buff 效果**

```csharp
// 获取目标属性
var attr = BuffEffectVerifier.Instance.GetTargetAttributes(target);
Debug.Log($"HP: {attr.HP}/{attr.MaxHP}");
Debug.Log($"攻击: {attr.AtkDamage}");

// 获取所有 Buff 详情
var buffDetails = BuffEffectVerifier.Instance.GetBuffDetails(target);
foreach (var buff in buffDetails)
{
    Debug.Log($"{buff.Name} - 堆叠: {buff.StackCount}");
}

// 获取控制状态
var controls = BuffEffectVerifier.Instance.GetControlStates(target);
if (controls.Contains("眩晕"))
{
    Debug.Log("目标已眩晕！");
}

// 生成完整报告
var report = BuffEffectVerifier.Instance.GenerateTestReport(target);
Debug.Log(report);
```

---

### **管理预设**

```csharp
// 保存自定义预设
int[] myBuffs = { 10101, 10105, 10301 };
BuffPresetManager.Instance.AddPreset("我的组合", myBuffs);

// 应用预设
BuffPresetManager.Instance.ApplyPreset("我的组合", target);

// 获取所有预设
var presets = BuffPresetManager.Instance.GetAllPresets();
foreach (var preset in presets)
{
    Debug.Log($"预设: {preset.Name}");
}

// 删除预设
BuffPresetManager.Instance.DeletePreset("我的组合");
```

---

## 🎯 默认预设说明

| 预设名 | 包含 Buff | 用途 |
|--------|---------|------|
| **伤害组合** | StatModBuff + BleedBuff | 测试攻击增益和持续伤害 |
| **控制组合** | StunBuff + FrostBuff | 测试控制状态的叠加 |
| **防守组合** | StatModBuff(防御) + 护盾 | 测试防御和护盾交互 |
| **辅助组合** | 加血 + 速度提升 | 测试辅助效果 |

**如何修改预设：**

编辑 `BuffPresetManager.cs` 的 `InitializeDefaultPresets()` 方法：

```csharp
private void InitializeDefaultPresets()
{
    // 修改这里的 Buff ID 和名称
    AddPreset("自定义名称", new int[] { 10101, 10102, ... });
}
```

---

## 📊 Buff ID 参考

常用 Buff ID：

| ID | 名称 | 类型 |
|----|------|------|
| 10101 | StatModBuff-攻击+25% | 属性修改 |
| 10102 | BleedBuff-出血 | 周期伤害 |
| 10103 | StatModBuff-防御+10% | 属性修改 |
| 10104 | StunBuff-眩晕 | 控制状态 |
| 10105 | FrostBuff-冰冻 | 控制状态 |
| 10106 | StatModBuff-速度+5% | 属性修改 |
| 10201 | 加血/治疗 | 治疗 |
| 10301 | 护盾 | 护盾 |

**查询所有 Buff ID：**

```csharp
var allBuffs = BuffTestTool.Instance.GetAllAvailableBuffs();
foreach (var buff in allBuffs)
{
    Debug.Log($"ID: {buff.BuffId}, Name: {buff.Name}, Type: {buff.BuffType}");
}
```

---

## ⚡ 快捷键一览

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + B` | 打开/关闭工具 UI |
| `F` | 快速选择鼠标指向的棋子 |

---

## 🐛 故障排除

### **问题 1：UI 无法显示**

**原因**：Canvas 可能没有正确创建

**解决**：
```csharp
// 检查是否有 Canvas
var canvas = FindObjectOfType<Canvas>();
if (canvas == null)
{
    Debug.LogError("场景中没有 Canvas");
}
```

---

### **问题 2：Buff 不生效**

**检查清单**：
- [ ] BuffManager 组件是否挂载在目标上
- [ ] Buff ID 是否正确（可用 `GetAllAvailableBuffs()` 查询）
- [ ] BuffTable 是否已加载
- [ ] 目标的 ChessAttribute 组件是否存在

```csharp
// 诊断脚本
public void DiagnosticTarget(GameObject target)
{
    var buffManager = target.GetComponent<BuffManager>();
    Debug.Log($"有 BuffManager: {buffManager != null}");

    var attribute = target.GetComponent<ChessAttribute>();
    Debug.Log($"有 ChessAttribute: {attribute != null}");

    var buffs = BuffTestTool.Instance.GetTargetBuffs(target);
    Debug.Log($"当前 Buff 数: {buffs.Count}");
}
```

---

### **问题 3：Buff 应用后看不到效果**

**原因**：可能是 Buff 效果需要特定条件触发

**解决**：
1. 查看 Buff 的 `OnEnter()` 方法，确认是否需要手动触发
2. 使用 `BuffEffectVerifier.GenerateTestReport()` 生成完整报告
3. 查看游戏日志，是否有报错信息

---

## 📝 扩展建议

### **添加新预设**

```csharp
// 在 BuffPresetManager.cs 中
private void InitializeDefaultPresets()
{
    // ... 已有预设 ...
    
    // 新增自定义预设
    AddPreset("超级伤害", new int[] { 10101, 10102, 10106, 10107 });
}
```

---

### **自定义 Buff 参数编辑**

如需动态修改 Buff 参数（持续时间、伤害值等），可扩展：

```csharp
// 在 BuffTestTool.cs 中
public void ModifyBuffParameter(int buffId, GameObject target, string paramName, float value)
{
    var buff = GetBuff(buffId, target);
    if (buff == null) return;
    
    // 通过反射修改 Buff 属性
    var field = buff.GetType().GetField(paramName);
    if (field != null)
    {
        field.SetValue(buff, value);
    }
}
```

---

## 💡 最佳实践

1. **逐个测试** - 先测试单个 Buff，再测试组合效果
2. **记录结果** - 使用 `GenerateTestReport()` 保存测试结果
3. **边界值测试** - 测试极限堆叠、快速切换等边界情况
4. **性能监测** - 同时应用大量 Buff 时监测帧率
5. **自动化脚本** - 编写测试脚本自动化常见测试场景

---

## 🎓 示例测试场景

### **场景 A：验证 Buff 叠加**

```csharp
// 应用相同的 Buff 3 次
for (int i = 0; i < 3; i++)
{
    BuffTestTool.Instance.ApplyBuffToTarget(10101, target);
}

// 验证
var buff = BuffTestTool.Instance.GetBuff(10101, target);
Assert.AreEqual(3, buff.StackCount); // 应该是 3 层
```

---

### **场景 B：验证 Buff 失效**

```csharp
// 应用出血 Buff
BuffTestTool.Instance.ApplyBuffToTarget(10102, target);

// 等待持续时间过期
await UniTask.Delay(11000); // 假设持续时间是 10 秒

// 验证是否自动移除
var buff = BuffTestTool.Instance.GetBuff(10102, target);
Assert.IsNull(buff);
```

---

### **场景 C：验证控制状态**

```csharp
// 应用眩晕
BuffTestTool.Instance.ApplyBuffToTarget(10104, target);

// 验证无法移动/攻击
var controls = BuffEffectVerifier.Instance.GetControlStates(target);
Assert.IsTrue(controls.Contains("眩晕"));
```

---

## ✅ 测试清单

使用此工具时推荐的测试清单：

- [ ] 单个 Buff 应用和自动移除
- [ ] 同一 Buff 多层叠加
- [ ] 不同 Buff 组合效果
- [ ] Buff 属性修改是否生效
- [ ] 控制状态是否生效
- [ ] 周期性伤害是否持续
- [ ] Buff 移除后属性恢复
- [ ] 大量 Buff 应用的性能
- [ ] 快速切换目标测试

---

## 📞 注意事项

1. **这是开发工具**，仅用于测试，不要在正式版本中发布
2. **需要手动配置** Buff ID 预设，请根据实际的 BuffTable 修改
3. **性能考虑** - 不要同时应用过多 Buff（>50 个），可能影响帧率
4. **时序问题** - 注意 Buff 生效的时序，某些 Buff 需要在特定阶段生效
