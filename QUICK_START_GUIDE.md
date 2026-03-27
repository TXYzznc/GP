# 快速开始指南

在开始后续工作前，请按照以下步骤操作。

---

## ✅ 第1步：更新BuffTable配置（15分钟）

### 操作步骤

1. **打开Excel文件**
   - 路径：`AAAGameData/DataTables/BuffTable.xlsx`

2. **添加三个新列**
   - 在最后一列（EffectId）之后插入新列
   - 列名1：`IsInitiativeBuff` (类型：Boolean)
   - 列名2：`IsSneakDebuff` (类型：Boolean)
   - 列名3：`BuffOwner` (类型：Integer)

3. **填充现有Buff的值**
   - 所有现有Buff：IsInitiativeBuff=false, IsSneakDebuff=false, BuffOwner=2

4. **添加新的先手Buff（玩家用）**
   ```
   ID: 2001, Name: 先手·速度提升, ... IsInitiativeBuff=true, IsSneakDebuff=false, BuffOwner=0
   ID: 2002, Name: 先手·攻击提升, ... IsInitiativeBuff=true, IsSneakDebuff=false, BuffOwner=0
   ID: 2003, Name: 先手·首回合免伤, IsInitiativeBuff=true, IsSneakDebuff=false, BuffOwner=0
   ```

5. **添加新的偷袭Debuff（敌人用）**
   ```
   ID: 3001, Name: 偷袭·防御降低, ... IsInitiativeBuff=false, IsSneakDebuff=true, BuffOwner=1
   ID: 3002, Name: 偷袭·眩晕, ... IsInitiativeBuff=false, IsSneakDebuff=true, BuffOwner=1
   ID: 3003, Name: 偷袭·持续流血, IsInitiativeBuff=false, IsSneakDebuff=true, BuffOwner=1
   ```

6. **保存Excel文件**

7. **在Unity中重新生成代码**
   - 菜单：`Tools > GameFramework > DataTableGenerator > GenerateAll`
   - 等待完成（查看Console确认无错误）

✅ **验证**：检查 `Assets/AAAGame/Scripts/DataTable/BuffTable.cs` 应包含新属性

---

## ✅ 第2步：创建EscapeRuleTable配置（10分钟）

### 操作步骤

1. **创建新Excel文件**
   - 名称：`EscapeRuleTable.xlsx`
   - 位置：`AAAGameData/DataTables/`

2. **创建表结构**
   - 列1：`Id` (Integer)
   - 列2：`#` (String, 备注)
   - 列3：`EnemyType` (Integer)
   - 列4：`BaseSuccessRate` (Float)
   - 列5：`TimeBonus` (Float)
   - 列6：`MaxSuccessRate` (Float)
   - 列7：`CorruptionCost` (Integer)
   - 列8：`HealthLossPenalty` (Float)
   - 列9：`CooldownTurns` (Integer)

3. **添加数据行**
   ```
   1 | 普通敌人 | 0 | 0.6  | 0.05 | 0.9 | 10 | 0.2 | 2
   2 | 精英敌人 | 1 | 0.4  | 0.03 | 0.8 | 15 | 0.3 | 3
   3 | Boss敌人 | 2 | 0.2  | 0.02 | 0.6 | 25 | 0.4 | 5
   ```

4. **保存文件**

5. **在Unity中重新生成代码**
   - 菜单：`Tools > GameFramework > DataTableGenerator > GenerateAll`

✅ **验证**：检查 `Assets/AAAGame/Scripts/DataTable/EscapeRuleTable.cs` 应包含所有属性

---

## 🔴 第3步：创建UI预制体（30分钟）

### 3.1 SneakDebuffSelectionUI 预制体

**路径**：`Assets/AAAGame/Prefabs/UI/SneakDebuffSelectionUI.prefab`

**结构**（使用RectTransform布局）：
```
SneakDebuffSelectionUI (Canvas Dialog 或 Panel)
├── Title (Text) - 显示"选择偷袭效果"
├── DebuffOptionContainer (GridLayoutGroup 或 HorizontalLayoutGroup)
│   ├── varDebuffOption1 (Button, Preferred Width: 150)
│   │   ├── varDebuffOption1Icon (Image, 宽高: 64×64)
│   │   ├── varDebuffOption1Name (Text)
│   │   └── varDebuffOption1Description (Text)
│   ├── varDebuffOption2 (Button)
│   │   ├── varDebuffOption2Icon (Image)
│   │   ├── varDebuffOption2Name (Text)
│   │   └── varDebuffOption2Description (Text)
│   └── varDebuffOption3 (Button)
│       ├── varDebuffOption3Icon (Image)
│       ├── varDebuffOption3Name (Text)
│       └── varDebuffOption3Description (Text)
```

**步骤**：
1. 在Hierarchy中创建 Canvas > Panel，命名 SneakDebuffSelectionUI
2. 添加 LayoutElement 和 CanvasGroup 组件
3. 创建上述嵌套结构
4. 为SneakDebuffSelectionUI添加 **SneakDebuffSelectionUI** 脚本
5. 将Panel另存为Prefab到 `Assets/AAAGame/Prefabs/UI/`
6. 运行UI变量生成工具：`Tools > GenerateUIVariables` 或类似
   - 这会自动生成 `SneakDebuffSelectionUI.Variables.cs`

### 3.2 InitiativeBuffNotificationUI 预制体

**路径**：`Assets/AAAGame/Prefabs/UI/InitiativeBuffNotificationUI.prefab`

**结构**：
```
InitiativeBuffNotificationUI (Canvas Tips/Notification)
├── varBackground (Image, 可选，半透明黑色，Alpha: 0.5)
├── HorizontalLayout
│   ├── varBuffIcon (Image, 宽高: 64×64)
│   └── VerticalLayout
│       ├── varBuffName (Text, 显示"玩家获得：...")
│       └── varBuffDescription (Text)
```

**步骤**：
1. 创建Canvas > Panel，命名 InitiativeBuffNotificationUI
2. 添加 **CanvasGroup** 组件（用于淡入淡出动画）
3. 创建上述嵌套结构
4. 为InitiativeBuffNotificationUI添加 **InitiativeBuffNotificationUI** 脚本
5. 将Panel另存为Prefab
6. 运行UI变量生成工具

### 3.3 EscapeResultUI 预制体

**路径**：`Assets/AAAGame/Prefabs/UI/EscapeResultUI.prefab`

**结构**：
```
EscapeResultUI (Canvas Dialog)
├── varResultTitle (Text, 宽度: 200, "脱战成功"/"脱战失败")
├── varResultIcon (Image, 宽高: 128×128)
├── varResultMessage (Text, "污染值 +10" 或 "生命值 -20%")
└── varConfirmButton (Button, "确认")
```

**步骤**：
1. 创建Canvas > Panel，命名 EscapeResultUI
2. 创建上述结构
3. 添加 **EscapeResultUI** 脚本
4. 将Panel另存为Prefab
5. 运行UI变量生成工具

---

## ✅ 第4步：验证编译（5分钟）

完成以上步骤后，检查Unity编译是否无错误：

1. 打开 Unity Editor
2. 查看 Console 窗口
3. 确认没有编译错误（可能有Warning，但不应有Error）
4. 如有错误，根据错误信息检查对应的文件

---

## 🟡 第5步：集成Buff逻辑（可选，需要代码编写）

当前 `CombatTriggerManager.cs` 中有两个TODO方法：

### GetSneakDebuffPool()

```csharp
private List<int> GetSneakDebuffPool()
{
    var buffTable = GF.DataTable.GetDataTable<BuffTable>();
    if (buffTable == null) return new List<int>();

    List<int> debuffIds = new List<int>();

    // TODO: 遍历BuffTable，筛选出 IsSneakDebuff=true 的Buff ID
    // 提示：使用 foreach (var row in buffTable.GetAllDataRows())

    return debuffIds;
}
```

### GetRandomInitiativeBuff()

```csharp
private int GetRandomInitiativeBuff()
{
    var buffTable = GF.DataTable.GetDataTable<BuffTable>();
    if (buffTable == null) return 0;

    List<int> initiativeBuffIds = new List<int>();

    // TODO: 遍历BuffTable，筛选出 IsInitiativeBuff=true 的Buff ID
    // 提示：使用 foreach (var row in buffTable.GetAllDataRows())

    if (initiativeBuffIds.Count == 0) return 0;
    return initiativeBuffIds[Random.Range(0, initiativeBuffIds.Count)];
}
```

---

## 🟢 第6步：测试（可选）

完成以上步骤后，可以进行基本测试：

### 测试清单

- [ ] 运行游戏，进入探索场景
- [ ] 靠近敌人，观察敌人警觉度UI是否显示
- [ ] 从敌人背后接近（距离<3m，警觉度<0.3），观察是否显示偷袭UI
- [ ] 按Space键触发战斗，观察是否进入战斗状态
- [ ] 查看Console日志，确认所有系统正确初始化

### 调试建议

- 在Scene视图中观察Gizmos（敌人视野检测范围）
- 启用 `DebugEx.LogModule` 输出，观察各系统的日志信息
- 检查DataTable是否正确加载（查看Console启动信息）

---

## 常见问题

**Q: Excel文件编辑后，Unity不识别新列怎么办？**
A: 运行 `Tools > GameFramework > DataTableGenerator > GenerateAll` 重新生成代码。确保Excel文件已保存。

**Q: UI变量生成后，SneakDebuffSelectionUI.Variables.cs 还是空的？**
A: 检查预制体中的UI控件是否正确命名（以 `var` 开头），然后重新运行变量生成工具。

**Q: 游戏运行时报错 "BuffTable未加载"？**
A: 检查配置表是否已在游戏启动流程中加载。查看游戏的DataTable加载逻辑。

**Q: Buff没有应用到敌人身上？**
A: 这是正常的，因为集成逻辑还未完全实现。等待后续的代码集成步骤。

---

## 预计完成时间

| 步骤 | 时间 |
|-----|------|
| 1. BuffTable更新 | 15分钟 |
| 2. EscapeRuleTable创建 | 10分钟 |
| 3. UI预制体创建 | 30分钟 |
| 4. 验证编译 | 5分钟 |
| **总计** | **~1小时** |

---

## 完成确认

完成以上所有步骤后，您将准备好进行游戏的集成测试。

下一步：
1. 实现 `CombatTriggerManager` 的Buff筛选逻辑
2. 在战斗准备阶段应用先手效果
3. 集成脱战UI到战斗界面
4. 进行端到端测试

祝您游戏开发顺利！🎮
