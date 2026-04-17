> **最后更新**: 2026-04-17
> **状态**: 有效
> **版本**: 3.0（合并版：AI战斗系统设计 + 数据参考）

---

# AI战斗系统完整设计

## 目录

- [系统概述](#系统概述)
- [系统架构图](#系统架构图)
- [核心组件详解](#核心组件详解)
- [数据流程](#数据流程)
- [文件组织结构](#文件组织结构)
- [配置表字段与数据参考](#配置表字段与数据参考)
- [性能优化策略](#性能优化策略)
- [测试用例](#测试用例)
- [已知限制与改进方向](#已知限制与改进方向)
- [关键依赖](#关键依赖)

---

## 系统概述

AI战斗系统负责探索阶段的敌人行为检测和战斗触发，核心功能包括：

- 警觉度可视化反馈
- 多种战斗触发方式（偷袭、遭遇战、敌方先手）
- 战斗准备阶段的先手效果
- 战斗中的脱战机制

**核心设计原则**：
1. 模块化：各系统独立但可相互通信
2. 数据驱动：配置表驱动所有参数
3. 事件驱动：基于GameFramework事件系统
4. 对象池：UI指示器使用对象池优化性能

---

## 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                   游戏主循环                                  │
└────────┬────────────────────────────────────┬────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────────────┐      ┌──────────────────────────┐
│   敌人AI系统             │      │   玩家战斗检测系统        │
│ (EnemyEntityAI)         │      │ (CombatOpportunityDet.) │
└────────┬────────────────┘      └──────┬───────────────────┘
         │                              │
         ▼                              ▼
┌─────────────────────────┐      ┌──────────────────────────┐
│  视野检测器              │      │  机会检测（偷袭/遭遇）   │
│ (VisionConeDetector)    │      │  条件判定+UI显示        │
└────────┬────────────────┘      └──────┬───────────────────┘
         │                              │
         │  警觉度变化                  │  触发条件满足
         ▼                              ▼
┌─────────────────────────┐      ┌──────────────────────────┐
│   警示UI管理器           │      │  战斗触发管理器          │
│ (EnemyAlertUIManager)   │      │ (CombatTriggerManager)  │
│  - 对象池               │      │  - 创建上下文           │
│  - 显示/隐藏指示器      │      │  - 分配先手效果        │
│  - 距离排序             │      │  - 通知战斗系统        │
└────────┬────────────────┘      └──────┬───────────────────┘
         │                              │
         ▼                              ▼
    敌人信息UI              进入战斗准备阶段
┌──────────────────┐
│ EnemyMask Item   │       ┌──────────────────────────────┐
│ - 头像           │       │  战斗准备阶段                 │
│ - 进度条         │       │ (CombatPreparationState)     │
│ - 距离           │       │  - 应用先手Buff/Debuff       │
└──────────────────┘       │  - 显示偷袭Debuff选择UI     │
                            │  - 显示先手效果通知        │
                            └──────────────────────────────┘
                                    │
                            ┌───────┴───────┐
                            ▼               ▼
                    (若为偷袭)         (若为遭遇/先手)
                    ┌─────────────┐  ┌──────────────┐
                    │ 选择Debuff  │  │ 显示先手UI   │
                    │ UI          │  │ 应用Buff     │
                    └─────────────┘  └──────────────┘
```

---

## 核心组件详解

### 1. 视野检测系统 (VisionConeDetector)

**职责**：
- 监测玩家是否在敌人检测范围内
- 计算和维护警觉度（0-1浮点数）
- 通知UI管理器显示/隐藏警示

**检测方式**（二层检测）：

```
圈形检测 (周围范围)
├─ 范围：360度圆形
├─ 距离：VisionCircleRadius (默认8米)
├─ 增长率：AlertIncreaseRate (默认0.5/秒)
└─ 用途：听觉/感知检测

扇形检测 (前方视野)
├─ 范围：VisionConeAngle度扇形 (默认60度)
├─ 距离：VisionConeDistance (默认12米)
├─ 增长率：AlertIncreaseRate * 1.5 (更快)
└─ 用途：视觉检测
```

**警觉度状态转换**：
```
离开范围 → 衰减 → [0.0 - 0.1) → 未检测
未检测 → 进入圈 → 缓慢增长 → [0.1 - AlertThreshold)
未检测 → 进入扇形 → 快速增长 → [0.3 - AlertThreshold)
检测到 → 锁定追击 → [AlertThreshold - 1.0]
追击失败 → 衰减 → 回到未检测
```

### 2. 警示UI系统 (EnemyAlertUIManager + EnemyMask)

**EnemyAlertUIManager（单例）**：
- 维护EnemyMask对象池
- 追踪所有活跃敌人的指示器
- 按距离排序（最多显示5个）
- 定时更新进度条

**EnemyMask（Item容器）**：
- 显示敌人头像
- 实时更新警觉度进度条
- 可选显示敌人名称和距离

**流程**：
```
VisionConeDetector 警觉度 > 0.1f
    ↓
EnemyAlertUIManager.ShowOrUpdateAlert(enemy, alertProgress)
    ├─ 如果已有指示器 → 更新进度条
    └─ 如果无指示器 → 从对象池取出 → Setup → 显示

警觉度 ≤ 0
    ↓
EnemyAlertUIManager.HideAlert(enemy)
    └─ 回收到对象池
```

### 3. 战斗机会检测 (CombatOpportunityDetector)

**条件判定逻辑**：

```
优先级1：偷袭 (SneakAttack)
├─ 距离 < 3米
├─ 玩家在敌人背后 (夹角 < 60°)
├─ 敌人未警觉 (AlertLevel < 0.3)
└─ 玩家面向敌人 (夹角 < 45°)

优先级2：遭遇战 (Encounter)
├─ 距离 < 5米
├─ 敌人未警觉 (AlertLevel < 0.5)
├─ 不满足偷袭条件 (不在背后)
└─ 玩家面向敌人 (夹角 < 45°)

否则：无机会 → 隐藏交互UI
```

### 4. 战斗触发系统 (CombatTriggerManager)

**触发流程**：
```
战斗触发信息输入
    ├─ 创建CombatTriggerContext
    │   ├─ TriggerType (触发类型)
    │   ├─ TriggerEnemy (敌人)
    │   ├─ AvailableDebuffs (可选Debuff)
    │   ├─ InitiativeBuffId (先手Buff)
    │   └─ PlayerHasInitiative (是否玩家先手)
    │
    ├─ 根据类型分配效果
    │   ├─ SneakAttack → 获取3个Debuff供选择
    │   ├─ Encounter → 随机先手Buff给玩家
    │   └─ EnemyInitiated → 随机先手Buff给敌人
    │
    └─ 通知战斗系统开始战斗
```

**关键方法**：
```csharp
// 获取偷袭Debuff池（Fisher-Yates洗牌）
private List<int> GetSneakDebuffPool()
{
    // 筛选条件：IsSneakDebuff == true，BuffOwner == 1(敌人) 或 2(通用)
    // 随机算法：Fisher-Yates，O(n)复杂度
    // 返回：随机排列的Debuff ID列表（预期：[3001, 3002, 3003]）
}

// 获取随机先手Buff（根据战斗类型智能筛选）
private int GetRandomInitiativeBuff()
{
    // 敌方先手: BuffOwner == 1 或 2
    // 玩家先手: BuffOwner == 0 或 2
    // 筛选：IsInitiativeBuff == true
}
```

### 5. 战斗准备阶段效果应用

**SneakAttack处理**：
```
显示SneakDebuffSelectionUI → 获取3个可选Debuff → 等待玩家点击选择
玩家选择后 → 应用Debuff到敌人 → 进入战斗
```

**Encounter/EnemyInitiated处理**：
```
随机选择先手Buff → 显示InitiativeBuffNotificationUI → 应用Buff → 自动关闭UI(3秒) → 进入战斗
```

### 6. 脱战系统 (CombatEscapeSystem)

**成功率计算**：
```
成功率 = 基础成功率 + (当前回合数 × 回合加成) - (冷却中 ? 50% : 0) ≤ 上限成功率
```

**脱战结果**：
```
脱战成功：增加污染值(CorruptionCost) → 返回探索状态
脱战失败：降低召唤师生命(HealthLossPenalty%) → 进入冷却(CooldownTurns回合) → 继续战斗
```

---

## 数据流程

### 事件链 1：AI检测玩家
```
EnemyEntityAI.Tick()
    └─ UpdatePlayerDetection()
        └─ VisionDetector.UpdateDetection(playerTransform, deltaTime)
            ├─ 计算距离和角度
            ├─ 检查圈形和扇形范围
            ├─ 更新警觉度
            ├─ 检查超过警觉阈值
            │   └─ EnemyAlertUIManager.ShowOrUpdateAlert(enemy, alertLevel)
            └─ 返回是否检测到玩家
                └─ AI根据结果切换状态 (Patrol → Alert → Chase)
```

### 事件链 2：玩家检测战斗机会
```
CombatOpportunityDetector.Update()
    ├─ 每0.1秒更新一次
    ├─ DetectCombatOpportunities()
    │   ├─ CheckSneakAttackOpportunity() → ShowOpportunityUI(SneakAttack)
    │   ├─ CheckEncounterOpportunity() → ShowOpportunityUI(Encounter)
    │   └─ 无机会 → HideCombatInteract()
    └─ Input.GetKeyDown(Space) → TriggerCombat()
        └─ CombatTriggerManager.TriggerCombat(enemy, triggerType)
```

### 事件链 3：进入战斗准备
```
CombatPreparationState.OnEnter()
    └─ ApplyInitiativeEffects()
        ├─ SneakAttack → GetSneakDebuffPool() → OpenSneakDebuffSelectionUI → OnDebuffSelected → ApplyBuffToEnemy
        ├─ Encounter → GetRandomInitiativeBuff() → ApplyPlayerInitiativeBuff → ShowInitiativeBuffNotification
        └─ EnemyInitiated → ApplyEnemyInitiativeBuff → ShowInitiativeBuffNotification
```

---

## 文件组织结构

```
Assets/AAAGame/Scripts/
├── Game/
│   ├── Explore/
│   │   ├── Enemy/
│   │   │   ├── Core/
│   │   │   │   ├── EnemyEntity.cs（已集成VisionDetector）
│   │   │   │   └── EnemyEntityAI.cs（已调用UpdateDetection）
│   │   │   └── Detection/
│   │   │       ├── VisionConeDetector.cs（新系统）
│   │   │       └── PlayerDetectionInfo.cs
│   │   └── Combat/
│   │       ├── CombatTriggerType.cs（枚举）
│   │       ├── CombatTriggerContext.cs（上下文）
│   │       ├── CombatTriggerManager.cs（管理器）
│   │       └── CombatOpportunityDetector.cs（检测器）
│   └── Combat/
│       └── Escape/
│           └── CombatEscapeSystem.cs（脱战系统）
├── UI/
│   ├── SneakDebuffSelectionUI.cs
│   ├── InitiativeBuffNotificationUI.cs（先手通知，3秒自动关闭，DOTween淡入淡出）
│   ├── EscapeResultUI.cs（成功绿色/失败红色，含确认按钮）
│   └── Item/
│       ├── EnemyMask.cs（敌人指示器）
│       └── BuffChooseItem.cs（Buff选项）
└── System/
    └── EnemyAlertUIManager.cs（警示管理）
```

---

## 配置表字段与数据参考

### EnemyEntityTable - 敌人视野配置

| 字段 | 类型 | 默认值 | 说明 |
|-----|-----|-------|------|
| VisionCircleRadius | float | 8.0 | 周围圈半径(米) |
| VisionConeAngle | float | 60.0 | 扇形视野角度(度) |
| VisionConeDistance | float | 12.0 | 扇形视野距离(米) |
| AlertIncreaseRate | float | 0.5 | 警觉增长速率(/秒) |
| AlertDecreaseRate | float | 0.2 | 警觉衰减速率(/秒) |
| AlertThreshold | float | 1.0 | 检测阈值(0-1) |

### BuffTable - 先手Buff扩展字段

| 字段 | 类型 | 说明 |
|-----|-----|------|
| IsInitiativeBuff | bool | 是否为先手Buff |
| IsSneakDebuff | bool | 是否为偷袭Debuff |
| BuffOwner | int | 所有者(0=玩家, 1=敌人, 2=通用) |

**预设数据**：

| ID | 名称 | 类型 | Owner | 描述 |
|----|-----|------|-------|------|
| 2001 | 先手·速度提升 | 先手Buff | 玩家(0) | 移动速度提升20%，战斗持续 |
| 2002 | 先手·攻击提升 | 先手Buff | 玩家(0) | 攻击力提升15%，战斗持续 |
| 2003 | 先手·首回合免伤 | 先手Buff | 玩家(0) | 首回合受伤减少50% |
| 3001 | 偷袭·防御降低 | 偷袭Debuff | 敌人(1) | 防御力降低30%，持续2回合 |
| 3002 | 偷袭·眩晕 | 偷袭Debuff | 敌人(1) | 目标眩晕，跳过下一回合，持续1回合 |
| 3003 | 偷袭·持续流血 | 偷袭Debuff | 敌人(1) | 每回合流失10%生命值，持续3回合 |

### EscapeRuleTable - 脱战规则

| 规则ID | 敌人类型 | 基础成功率 | 每回合增长 | 最大成功率 | 污染代价 | 失败生命损失 | 冷却回合 |
|--------|---------|-----------|-----------|-----------|---------|------------|---------|
| 1 | 普通(0) | 60% | +5% | 90% | +10 | -20% | 2 |
| 2 | 精英(1) | 40% | +3% | 80% | +15 | -30% | 3 |
| 3 | Boss(2) | 20% | +2% | 60% | +25 | -40% | 5 |

**脱战成功率计算示例（普通敌人）**：
```
回合1：rate = 0.6 + (1 × 0.05) = 0.65 (65%)
回合5：rate = 0.6 + (5 × 0.05) = 0.85 (85%)
回合10：rate = min(0.6 + 10×0.05, 0.9) = 0.9 (90%，达上限)
失败冷却中：rate = (0.6 + 0.05) × 0.5 = 0.325 (32.5%)
```

### UITable - 新增UI配置

```
26  偷袭Debuff选择               2  SneakDebuffSelectionUI           True  2  False
27  先手Buff通知                 2  InitiativeBuffNotificationUI     True  2  False
28  脱战结果界面                 2  EscapeResultUI                   True  2  False
```

### 配置文件位置
- `AAAGameData/DataTables/BuffTable.xlsx`
- `AAAGameData/DataTables/EscapeRuleTable.xlsx`
- `Assets/AAAGame/DataTable/Core/UITable.txt`

---

## 性能优化策略

### 对象池策略
```
初始化：预热10个EnemyMask实例，扩展时动态创建
使用：最多显示5个（按距离排序），超出范围自动回收
销毁：场景切换时清空所有
```

### 检测频率优化
```
VisionConeDetector.UpdateDetection()     → 每0.15秒调用一次
CombatOpportunityDetector               → 每0.1秒检测一次
EnemyAlertUIManager 距离排序            → 每0.5秒重新排序一次
```

### 物理检测优化
```
使用 Physics.OverlapSphereNonAlloc()（复用缓存数组，避免GC）
圈形+扇形二层判定（先判定距离快，再判定角度慢）
```

---

## 测试用例

### 单元测试

| 场景 | 预期行为 | 验证方式 |
|-----|--------|--------|
| 玩家进入圈形范围 | 警觉度缓慢增长 | 每帧打印警觉度 |
| 玩家进入扇形范围 | 警觉度快速增长 | 每帧打印警觉度 |
| 警觉度超过阈值 | AI切换到Alert/Chase | 检查AI状态 |
| 敌人丢失玩家 | 警觉度衰减到0 | 检查警觉度归零时间 |
| 显示>=5个敌人 | 只显示最近的5个 | 检查UI指示器数量 |
| 偷袭条件满足 | 显示偷袭交互UI | 检查UI是否显示 |

### 编辑器菜单测试
```
Test > Combat > Test Sneak Debuff Pool         → 期望输出: 3001, 3002, 3003 (3个)
Test > Combat > Test Initiative Buff - Player  → 期望输出: 随机返回 2001/2002/2003
Test > Combat > Test Initiative Buff - Enemy   → 期望输出: 敌人类型Buff ID
```

### 集成测试流程
1. 完整AI流程：敌人巡逻 → 发现玩家 → 警觉 → 追击 → 战斗
2. 偷袭流程：潜入敌人背后 → 显示偷袭UI → 按空格 → 选择Debuff → 进入战斗
3. 遭遇战流程：正面接近 → 显示遭遇UI → 按空格 → 先手Buff通知(3秒) → 进入战斗
4. 脱战流程：战斗中 → 脱战按钮 → 计算成功率 → 结果UI → 返回探索/继续战斗

---

## 已知限制与改进方向

### 当前限制
1. 单一玩家检测（同时只检测一个玩家）
2. 无视线遮挡（仅基于距离和角度）
3. 无记忆系统（敌人丢失玩家后立即忘记）

### 可能的改进
1. 视线遮挡检测（添加Raycast验证视线）
2. 记忆系统（保留丢失玩家位置信息一段时间）
3. 听觉系统（根据玩家移动速度调整检测）
4. 小队协作（敌人之间广播玩家位置）

---

## 关键依赖

- **GameFramework**：事件系统、UI框架、数据表系统
- **Layer配置**：Enemy层用于碰撞检测
- **Tag配置**：Player tag用于快速查找玩家

---

**合并内容**：AI战斗系统设计（原文件1）+ 配置表数据参考（原文件2）
**最后更新**：2026-04-17
