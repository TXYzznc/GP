# 敌人AI系统独立测试场景

## 📋 场景概述

`AITestScene.unity` 是专门用于测试敌人AI行为的隔离环境，提供：

- ✅ 快速生成敌人
- ✅ 实时玩家控制
- ✅ AI状态日志输出
- ✅ 视野范围可视化（Gizmo）
- ✅ 预设测试场景

## 🎮 场景搭建（手动步骤）

### 1️⃣ 创建新场景

```
右键 Assets/AAAGame/Scenes/ → Create → Scene → AITestScene.unity
```

### 2️⃣ 场景基础配置

**添加到场景中的GameObject：**

```
场景层级结构：
├─ SceneEnvironment (GameObject)
│  ├─ Plane (基础地面)
│  │  └─ Material: LitMaterial (灰色)
│  │  └─ Scale: (10, 1, 10)
│  │  └─ Position: (0, -0.5, 0)
│  └─ Lighting (光源)
│     └─ Directional Light
│        └─ Rotation: (45, 45, 0)
│
├─ GameFrameworkInitializer (必需)
│  └─ 脚本: GameFrameworkInitializer (初始化GF系统)
│
├─ TestManager (GameObject)
│  └─ 脚本: EnemyAITestManager
│     ├─ Player Spawn Point: (0, 0, 0)
│     ├─ Enemy Spawn Point: (0, 0, 5)
│     └─ Auto Log AI State: ✓ checked
│
└─ NavMeshPlane (用于AI导航)
   └─ Baker NavMesh
```

### 3️⃣ NavMesh烘焙

1. 打开菜单：`Window → AI → Navigation`
2. 选择地面Plane
3. 在Inspector中：`Bake` → 勾选 `Walkable`
4. 点击 `Bake` 按钮
5. 验证蓝色覆盖区域是否正确

### 4️⃣ 相机配置

**Main Camera**：
- Position: (0, 2, -5)
- Rotation: (20, 0, 0)
- FOV: 60

## 🎯 使用方式

### 方式一：编辑器菜单（推荐）

```
Test → AI → 快速启动AI测试场景
```

然后按 `Play`，使用编辑器窗口：

```
Test → AI → 打开AI测试控制面板
```

### 方式二：直接在Unity中

1. 在Project中找到 `AITestScene.unity`
2. 双击打开或右键 → `Open Scene`
3. 按 `Play`
4. 在Hierarchy中选择 `TestManager`，在Inspector中调整参数

## ⌨️ 运行时控制

### 玩家控制

| 按键 | 功能 |
|-----|------|
| **W/A/S/D** | 移动玩家 |
| **Q/E** | 旋转玩家视角 |
| **Space** | 跳跃（预留） |

### 编辑器窗口控制

- 调整 `敌人配置ID` 选择不同敌人
- 调整 `初始距离` 改变敌人生成位置
- 点击 `生成敌人` 添加测试敌人
- 勾选 `显示视野Gizmo` 查看敌人检测范围
- 勾选 `输出AI状态日志` 打印AI状态变化

## 📊 测试场景预设

### 场景1：普通敌人巡逻

```
敌人ID: 1001
初始距离: 10m
预期行为：
- 敌人巡逻
- 玩家进入圆形范围(8m)时警觉
- 警觉度缓慢增长
- 超过阈值后切换到Alert状态
```

### 场景2：精英敌人视野检测

```
敌人ID: 1002
初始距离: 12m
预期行为：
- 敌人前方60度视野范围内检测更快
- 进入视野锥时快速警觉（1.5倍速率）
- 转身离开视野后警觉衰减
```

### 场景3：Boss追击战

```
敌人ID: 1003
初始距离: 15m
预期行为：
- Boss警觉阈值更低
- 检测到玩家后快速追击
- 保持追击直到玩家逃脱
```

## 🔍 调试信息输出

### Console输出示例

```
✓ 测试环境初始化完成
WASD 移动玩家 | Q/E 旋转 | Space 跳跃

✓ 已生成敌人 - ID:1001, 距离:10m, 位置:(0.0, 0.0, 10.0)

[TestEnemy_1001] 状态:Patrol | 警觉:0.05 | 距离:10.0m
[TestEnemy_1001] 状态:Alert | 警觉:0.35 | 距离:8.5m
[TestEnemy_1001] 状态:Chase | 警觉:1.00 | 距离:5.2m
```

### 可视化调试（Gizmo）

- 🔵 蓝色圆形：圆形检测范围(8m)
- 🔴 红色线段：视野锥前方距离(12m)
- 🟡 黄色圆形：玩家位置(0.5m)

## 🐛 常见问题

### 敌人不移动？

1. **NavMesh未烘焙** - 检查是否完成 Navigation 烘焙
2. **NavMeshAgent未初始化** - 确保EnemyEntity初始化时创建了NavMeshAgent
3. **敌人状态未切换** - 在Console查看是否有报错信息

### 敌人无法检测玩家？

1. 检查玩家Tag是否为"Player"
2. 检查敌人视野配置（VisionCircleRadius等）
3. 检查敌人与玩家的距离是否足够

### 日志输出不清晰？

1. 在Inspector中调整 `m_LogInterval` 增加输出间隔
2. 在Console右上角勾选 `Collapse` 去重
3. 查看项目 INDEX.md 中的 [敌人战斗测试指南]

## 📚 相关文档

- [AI战斗系统完整设计](../../项目知识库（AI自行维护）/wiki/系统设计/战斗系统/AI战斗系统.md) - 系统架构详解
- [敌人战斗测试指南](../../项目知识库（AI自行维护）/wiki/系统设计/战斗系统/敌人战斗测试指南.md) - 战斗配置说明
- [敌人探索系统架构](../../项目知识库（AI自行维护）/wiki/系统设计/探索系统/敌人探索系统架构.md) - 探索AI详解

## 🎬 快速启动命令

```csharp
// 在Game窗口Console中运行（需要在Play时输入）
EnemyAITestManager testMgr = FindObjectOfType<EnemyAITestManager>();
testMgr.SpawnEnemyAtDistance(1001, 8f);  // 生成普通敌人，距离8m
testMgr.SpawnEnemyAtDistance(1002, 12f); // 生成精英敌人，距离12m
testMgr.ClearAllEnemies();               // 清空所有敌人
```

## 注意事项

⚠️ **重要**：
- 此场景用于单独测试AI行为，不包含完整的游戏流程
- 敌人生成时会自动创建必要的组件（NavMeshAgent等）
- 玩家对象是临时创建的，仅用于测试
- 测试完成后需要返回正常的游戏场景

✅ **最佳实践**：
1. 针对性地测试特定敌人类型
2. 组合多个敌人进行压力测试
3. 记录日志用于性能分析
4. 修改配置参数后比对结果变化
