# 敌人探索AI行为测试指南

> **创建时间**：2026-05-08  
> **类型**：快速启动指南  
> **依赖**：ExploreAITestController.cs  

---

## 快速开始（3分钟）

### 1️⃣ 创建Test场景

在 `Assets/AAAGame/Scenes/` 创建新场景 `ExploreAITestScene.unity`

### 2️⃣ 场景搭建

添加以下对象到场景：

**基础设施**
```
场景层级：
├─ SceneEnvironment
│  ├─ Plane (地面，Scale 10×1×10)
│  └─ Lighting (Directional Light)
├─ GameFrameworkInitializer (初始化GF框架)
├─ PlayerObject (Player)
│  ├─ 添加 CharacterController
│  ├─ 设置 Tag = "Player"
│  └─ 添加简单Capsule视觉表示（蓝色）
├─ TestManager (空GameObject)
│  └─ 添加脚本 ExploreAITestController
└─ Main Camera (位置:0,2,-5 旋转:20,0,0)
```

**重要：烘焙NavMesh**
```
Window → AI → Navigation
选择地面Plane，勾选"Walkable"
点击"Bake"
```

### 3️⃣ 配置并运行

1. 选择 `TestManager` GameObject
2. Inspector中配置：
   - `Player Object`: 拖拽玩家GameObject
   - `Enemy Entity Config ID`: 输入要测试的敌人ID（如 1001）
   - `Enemy Spawn Position`: 设置敌人生成位置（如 5, 0, 0）
3. 点击 Play
4. 在Inspector中点击 "生成敌人" 按钮

---

## 运行时控制

| 按键 | 功能 |
|-----|------|
| **W/A/S/D** | 移动玩家 |
| **Q/E** | 旋转玩家视角 |
| **Inspector按钮** | 生成/销毁敌人 |

---

## 观察敌人AI行为

### Console输出示例

```
✓ 敌人生成成功: 普通敌人
敌人信息 - 类型:Normal | 状态:Idle

[普通敌人] 状态:Patrol | 警觉:0.00 | 距离:5.0m
[普通敌人] 状态:Patrol | 警觉:0.15 | 距离:4.5m
[普通敌人] 状态:Alert | 警觉:0.35 | 距离:4.2m
[普通敌人] 状态:Chase | 警觉:1.00 | 距离:3.5m
```

### 可视化Debug（Gizmo）

运行时在Scene窗口观察：

- 🟠 橙色圆：敌人位置
- 🟢 绿色圆：圆形检测范围（VisionCircleRadius，默认8m）
- 🔴 红色线：扇形视野方向（VisionConeDistance，默认12m）
- 🟡 黄线：警觉度可视化（上方黄线高度表示警觉度 0-1）
- 🔵 蓝色圆：玩家位置
- 🔷 青色线：敌人到玩家连线

---

## 测试场景

### 场景1：普通敌人视野检测

**敌人ID**: 1001  
**目标**: 测试警觉度增长和状态转换

**操作步骤**:
1. 敌人配置ID改为 1001
2. 生成敌人
3. 从敌人8m外缓慢靠近

**预期行为**:
- 敌人初始状态：`Patrol`（巡逻）
- 距离 < 8m：`Patrol`（警觉缓慢增长 0.5/秒）
- 警觉度 > 阈值：`Alert`（警戒）
- 继续靠近：`Chase`（追击，警觉达到1.0）

### 场景2：视野锥测试

**目标**: 测试扇形视野（60度、12m）的加速检测

**操作步骤**:
1. 从敌人正前方靠近（在60度视锥内）
2. 从敌人侧面靠近（在圆形范围内但视锥外）
3. 对比警觉增长速度

**预期结果**:
- 正前方：警觉增长 1.5倍速（0.75/秒）
- 侧面：警觉增长正常速（0.5/秒）

### 场景3：警觉衰减测试

**目标**: 测试玩家逃脱时的警觉衰减

**操作步骤**:
1. 惹怒敌人（让其进入Chase状态）
2. 快速远离，离开视野范围
3. 观察警觉度下降速度

**预期行为**:
- 离开视野：警觉缓慢衰减（0.2/秒）
- 衰减到0：敌人回到 Patrol 状态

---

## 常见问题

### Q: 敌人不动？

**A**: 检查：
1. NavMesh是否烘焙成功（Window → AI → Navigation）
2. 地面是否标记为 Walkable
3. 敌人是否卡在地形缝隙中（检查Spawn位置）

### Q: 没有Console输出？

**A**: 确认：
1. `m_LogInterval` 不要设置太长（默认0.5秒）
2. 敌人是否成功初始化（Inspector看到Config信息）
3. Console窗口是否打开（Window → General → Console）

### Q: Gizmo不显示？

**A**: 检查：
1. `Show Vision Gizmo` 是否勾选
2. Scene窗口右上角Gizmo按钮是否打开
3. 敌人是否存在（生成失败时无Gizmo）

### Q: 玩家控制无反应？

**A**: 确认：
1. PlayerObject是否有CharacterController组件
2. PlayerObject的Tag是否为"Player"
3. ExploreAITestController中是否正确赋值了PlayerObject

---

## 详细配置参数

### EnemyEntityTable 中的视野相关字段

| 字段 | 默认值 | 说明 |
|-----|-------|------|
| `VisionCircleRadius` | 8.0 | 圆形检测范围（米） |
| `VisionConeAngle` | 60.0 | 扇形视野角度（度） |
| `VisionConeDistance` | 12.0 | 扇形检测范围（米） |
| `AlertIncreaseRate` | 0.5 | 警觉增长速率（/秒） |
| `AlertDecreaseRate` | 0.2 | 警觉衰减速率（/秒） |
| `AlertThreshold` | 1.0 | 警觉阈值（0-1） |

这些值都可以在配置表中修改，然后运行 DataTableGenerator 更新。

---

## 与战斗系统的联动

敌人探索AI和战斗系统的连接点：

```
EnemyEntityAI.Chase 状态
    ↓ （玩家触发战斗）
CombatTriggerManager（检测触发类型）
    ├─ SneakAttack（偷袭）
    ├─ Encounter（遭遇战）
    └─ EnemyInitiative（敌方先手）
        ↓
CombatState（进入战斗）
```

测试敌人AI时，确保敌人能正确地从 Chase 状态切换到 Combat 状态。

---

## 进阶：修改AI参数测试

### 快速调整敌人检测能力

在 Inspector 中修改后 Play（需要重新生成敌人）：

```csharp
// 脚本中可以这样临时修改（用于测试）
m_TestEnemy.Config.VisionCircleRadius = 10f;  // 加大检测范围
m_TestEnemy.Config.AlertIncreaseRate = 1.0f;  // 加快警觉增长
```

### 添加多个敌人进行压力测试

修改 ExploreAITestController，添加方法：

```csharp
public async void SpawnMultipleEnemies(int count)
{
    for (int i = 0; i < count; i++)
    {
        m_EnemySpawnPosition.x += 3f;  // 横向间隔
        await SpawnTestEnemyAtPosition(m_EnemySpawnPosition);
    }
}
```

---

## 相关文档

- 📖 [AI战斗系统完整设计](../wiki/系统设计/战斗系统/AI战斗系统.md) - 完整的系统架构
- 📖 [敌人探索系统架构](../wiki/系统设计/探索系统/敌人探索系统架构.md) - AI行为详解
- 📖 [敌人战斗测试指南](../wiki/系统设计/战斗系统/敌人战斗测试指南.md) - 战斗相关测试

---

## 快速排查清单

启用测试前检查：

- [ ] NavMesh已烘焙（绿色覆盖地面）
- [ ] 玩家GameObject存在且有CharacterController
- [ ] 玩家Tag为"Player"
- [ ] GameFrameworkInitializer在场景中
- [ ] ExploreAITestController已挂载到TestManager
- [ ] 敌人配置ID有效（查EnemyEntityTable）

测试中观察：

- [ ] 敌人是否正确生成和初始化
- [ ] Console是否有实时日志输出
- [ ] Scene窗口Gizmo是否显示视野范围
- [ ] 玩家移动时警觉度是否变化
- [ ] 状态转换是否符合预期

---

## 技巧

💡 **屏幕录制演示**：
- 在Inspector中勾选"Show Vision Gizmo"
- 在Scene窗口按G隐藏UI
- 按Record录制玩家靠近敌人的过程

💡 **性能分析**：
- Window → Analysis → Profiler
- 监测CPU占用和内存使用
- 多敌人场景下观察性能表现

💡 **快速重置**：
- 点击"销毁敌人"后再"生成敌人"即可快速重置
- 不需要Stop并重新Play

---

**快乐测试！** 🎮
