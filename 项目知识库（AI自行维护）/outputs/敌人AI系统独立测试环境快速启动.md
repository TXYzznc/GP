# 敌人AI系统独立测试环境 — 快速启动指南

> **快速开始** | 测试时间：5分钟  
> **创建时间**：2026-05-08  
> **状态**：✅ 完成  

---

## 📦 已为你创建的文件

| 文件 | 位置 | 功能 |
|-----|------|------|
| **AITestHelper.cs** | `Assets/AAAGame/Scripts/Tools/Editor/` | 编辑器菜单 + 测试窗口 |
| **EnemyAITestManager.cs** | `Assets/AAAGame/Scripts/Tools/Runtime/` | 运行时测试管理器 |
| **AITestScene.md** | `Assets/AAAGame/Scenes/` | 场景搭建指南（详细） |
| 本文件 | `项目知识库/outputs/` | 快速启动（本文） |

---

## 🚀 3步快速启动

### Step 1：使用编辑器菜单加载测试场景

```
Unity菜单栏 → Test → AI → 快速启动AI测试场景
```

系统会自动加载 `AITestScene.unity`

<details>
<summary>❌ 如果提示找不到场景？</summary>

你需要**先手动创建**测试场景：

1. 在 `Assets/AAAGame/Scenes/` 右键 → Create → Scene
2. 命名为 `AITestScene.unity`
3. 参考 `Assets/AAAGame/Scenes/AITestScene.md` 进行场景搭建
4. 完成后再使用编辑器菜单

**快速搭建核心步骤**：
- [ ] 添加 Plane（地面，Scale 10x1x10）
- [ ] 添加 Directional Light（光源）
- [ ] 添加 GameFrameworkInitializer（GF初始化）
- [ ] 烘焙 NavMesh（Window → AI → Navigation）
- [ ] 添加 GameObject，命名 "TestManager"
  - [ ] 添加脚本 EnemyAITestManager
  - [ ] 在Inspector配置 Player Spawn Point 和 Enemy Spawn Point
- [ ] 配置 Main Camera（Position: 0,2,-5 / Rotation: 20,0,0）

</details>

### Step 2：打开测试控制面板

```
Unity菜单栏 → Test → AI → 打开AI测试控制面板
```

你会看到一个编辑器窗口，用来快速生成敌人和配置测试参数。

### Step 3：按Play，开始测试

```
Unity Editor → Play 按钮
```

现在你可以：
- 📍 WASD 移动玩家
- 🎮 Q/E 旋转视角
- 👾 在测试窗口点击"生成敌人"
- 📊 观看Console的实时AI状态输出

---

## 🎮 运行时操作

### 玩家控制

| 按键 | 功能 |
|-----|------|
| **W** | 向前移动 |
| **A/D** | 左右移动 |
| **S** | 向后移动 |
| **Q** | 左转 |
| **E** | 右转 |

### 编辑器窗口操作

![测试窗口](test-window-layout.png)  
*（如果看不到图，继续看下面的文字说明）*

#### 快速生成敌人区域

```
敌人配置ID: [1001]        ← 输入要测试的敌人ID
初始距离(米): [8.0]       ← 敌人与玩家的距离
[生成敌人 按钮]           ← 点击生成敌人
```

#### 测试选项区域

```
☐ 显示视野Gizmo           ← 勾选后，Scene窗口中显示绿色视野范围
☐ 输出AI状态日志         ← 勾选后，Console实时打印敌人AI状态
```

#### 测试场景预设

```
[测试场景1: 普通敌人巡逻]      ← 一键启动场景1
[测试场景2: 精英敌人视野检测]  ← 一键启动场景2
[测试场景3: Boss敌人追击]      ← 一键启动场景3
```

---

## 📊 理解Console输出

### 初始化阶段

```
✓ 测试环境初始化完成
WASD 移动玩家 | Q/E 旋转 | Space 跳跃
```

### 敌人生成

```
✓ 已生成敌人 - ID:1001, 距离:8.0m, 位置:(0.0, 0.0, 8.0)
```

### 实时AI状态（每0.5秒输出）

```
[TestEnemy_1001] 状态:Patrol | 警觉:0.00 | 距离:8.0m
[TestEnemy_1001] 状态:Alert | 警觉:0.45 | 距离:7.2m
[TestEnemy_1001] 状态:Chase | 警觉:1.00 | 距离:5.0m
```

**状态说明**：
- `Idle` - 休息
- `Patrol` - 巡逻
- `Alert` - 警戒（玩家进入视野范围）
- `Chase` - 追击（玩家被锁定）
- `AlertedByBroadcast` - 被广播警戒（收到其他敌人的警告）
- `Rest` - 深度休息（不被惊醒）

---

## 🔍 常见测试场景

### 场景1：普通敌人巡逻检测

**操作步骤**：
1. 点击 `[测试场景1: 普通敌人巡逻]`
2. Play运行
3. 移动玩家靠近敌人

**观察内容**：
- ✅ 敌人是否正确巡逻
- ✅ 玩家进入8米范围时，警觉度是否开始增长
- ✅ 警觉增长速度（圆形范围应为 0.5/秒）
- ✅ 警觉达到阈值后，敌人是否切换到Alert/Chase状态

**预期行为**：
```
Patrol → Alert (警觉 0.0-0.5) → Chase (警觉 0.5-1.0)
```

### 场景2：精英敌人视野检测

**操作步骤**：
1. 点击 `[测试场景2: 精英敌人视野检测]`
2. Play运行
3. 尝试从不同角度接近敌人

**观察内容**：
- ✅ 从敌人正前方接近（60度视锥内）vs 从侧面接近
- ✅ 前方接近时警觉增长速度（应为 0.5 × 1.5 = 0.75/秒）
- ✅ 从侧面接近时警觉增长速度（应为 0.5/秒）
- ✅ 警觉衰减速度（敌人失去视线后衰减 0.2/秒）

**预期结果**：
```
正前方接近：警觉 0→1 用时 ~1.3秒（快速）
侧面接近：警觉 0→1 用时 ~2.0秒（缓慢）
```

### 场景3：Boss敌人追击

**操作步骤**：
1. 点击 `[测试场景3: Boss敌人追击]`
2. Play运行
3. 尽量与Boss保持距离

**观察内容**：
- ✅ Boss是否快速进入Chase状态
- ✅ Boss追击时的移动速度
- ✅ 距离敌人5米是否会触发战斗
- ✅ NavMesh导航是否正确（是否卡顿或偏离）

---

## 📈 进阶用法

### 快速生成多个敌人进行压力测试

```
敌人配置ID: 1001 → 点击生成敌人 → 距离8m
敌人配置ID: 1002 → 点击生成敌人 → 距离12m
敌人配置ID: 1003 → 点击生成敌人 → 距离15m
```

现在你有3个不同类型的敌人同时活动，可以观察：
- 多敌人同时追击的行为
- AI状态机的稳定性
- 性能影响（如果敌人数量过多）

### 调整测试参数

在 EnemyAITestManager 的 Inspector 中，你可以修改：

```
m_PlayerMoveSpeed        ← 玩家移动速度（默认5m/s）
m_LogInterval           ← 日志输出间隔（默认0.5秒）
m_ShowGizmo             ← 是否显示Gizmo可视化
m_AutoLogAIState        ← 是否自动输出AI状态
m_AllowPlayerControl    ← 是否允许玩家控制（用于录像）
```

### 调整敌人配置

编辑 `AAAGameData/DataTables/EnemyEntityTable.xlsx`，修改：

```
VisionCircleRadius      ← 圆形检测范围（默认8m）
VisionConeAngle        ← 扇形视野角度（默认60度）
VisionConeDistance     ← 扇形检测范围（默认12m）
AlertIncreaseRate      ← 警觉增长速率（默认0.5/秒）
AlertDecreaseRate      ← 警觉衰减速率（默认0.2/秒）
AlertThreshold         ← 警觉阈值（默认1.0）
```

修改后需要运行 DataTableGenerator 更新配置。

---

## 🐛 故障排除

### 敌人不移动

**原因**：NavMesh未正确烘焙

**解决方案**：
1. Window → AI → Navigation
2. 选择地面Plane
3. 在Inspector中勾选 `Walkable`
4. 点击 `Bake` 按钮

### Console没有日志输出

**原因1**：EnemyAITestManager没有正确初始化  
**解决方案**：确保TestManager GameObject在场景中，且能正常Start()

**原因2**：敌人未初始化  
**解决方案**：检查EnemyEntity的Initialize()是否被调用

### 玩家控制不响应

**原因**：m_AllowPlayerControl 被禁用

**解决方案**：在Inspector勾选 `Allow Player Control`

---

## 📚 扩展阅读

- 📖 [AI战斗系统完整设计](../wiki/系统设计/战斗系统/AI战斗系统.md) - 系统架构详解
- 📖 [敌人战斗测试指南](../wiki/系统设计/战斗系统/敌人战斗测试指南.md) - 配置与测试
- 📖 [敌人探索系统架构](../wiki/系统设计/探索系统/敌人探索系统架构.md) - 行为系统

---

## ✅ 检查清单

在开始使用前，确保：

- [ ] AITestScene.unity 已创建或加载
- [ ] AITestHelper.cs 已添加到项目中
- [ ] EnemyAITestManager.cs 已添加到项目中
- [ ] NavMesh已烘焙
- [ ] GameFramework系统已初始化
- [ ] 敌人配置表数据已加载（EnemyEntityTable）

---

## 💡 提示

- **快速切换场景**：使用编辑器菜单比手动打开更快
- **保存进度**：测试完成后不需要Save Scene，所有更改都是临时的
- **性能测试**：打开Profiler（Window → Analysis → Profiler）监测CPU占用
- **录像演示**：禁用 `m_AllowPlayerControl`，手动移动摄像机，然后录屏

---

## 📞 反馈

如果在使用中遇到问题，请检查：
1. Console中是否有报错信息
2. Hierarchy中是否有 TestManager 和生成的敌人
3. Scene窗口中敌人是否出现
4. 是否正确配置了玩家生成点

---

**快乐测试！** 🎮
