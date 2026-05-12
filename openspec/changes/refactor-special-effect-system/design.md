## Context

当前系统中 SpecialEffect 被当作 Buff 处理：GameEffectService.Execute() 直接读取 BuffIds 并调用 BuffManager.AddBuff()。但实际需求中，特殊效果分为多种类型——即时效果（消耗品加金币）、Buff 效果（战斗中给目标加 Buff）、被动效果（装备/宝物在身时永久生效，无图标不可驱散）、触发效果（满足条件时触发）。

现有架构的关键组件：
- **SpecialEffectTable**：存储效果定义（ID、Name、BuffIds、EffectParams）
- **SpecialEffectData**：运行时数据，有 GetParamValue<T>() 方法解析 JSON 参数
- **GameEffectService**：效果执行入口（Execute 方法，支持 GameEffectContext 多目标）
- **ItemEffectFactory/ItemEffectExecutor**：消耗品效果工厂模式（IItemEffect 接口）
- **BuffManager**：Buff 生命周期管理（AddBuff/RemoveBuff/Tick）
- **ChessAttribute**：棋子属性直接修改模型，有 OnDamageTaken 等事件
- **SynergyManager**：羁绊检测与激活，已使用 GameEffectService

## Goals / Non-Goals

**Goals:**
- SpecialEffectTable 保持为统一效果定义表，新增 EffectType 字段区分效果类型
- 引入 SpecialEffectInstance 抽象层，按类型分派执行逻辑
- 被动效果（Passive）直接修改 ChessAttribute，不走 Buff 系统（无图标、不可驱散）
- Buff 效果仍通过现有 BuffManager 执行，保持兼容
- 效果生命周期明确：Apply（应用）→ 持续 → Remove（移除）

**Non-Goals:**
- 不修改 BuffTable 结构或 BuffManager 核心逻辑
- 不改变 ChessAttribute 的属性修改接口
- 不修改 SynergyManager 的羁绊检测逻辑（只改效果应用方式）
- 暂不实现 TriggerEffect 的完整事件订阅机制（预留接口即可）
- 不涉及 UI 层改动

## Decisions

**Decision 1：保留统一的 SpecialEffectTable**
- **选择**：不拆分为 5 张表，保持 SpecialEffectTable 为统一效果定义表，新增 EffectType 字段
- **原因**：效果本身是跨领域通用概念（同一个效果可被装备、宝物、羁绊共用）；拆分会导致重复定义和跨表引用混乱
- **备选**：按领域拆分为 5 张表 → 同一效果被多处引用时需要重复配置，违反 DRY

**Decision 2：EffectType 分类**
- **选择**：四种类型——Instant(1)、Buff(2)、Passive(3)、Trigger(4)
- **Instant**：立即执行一次，无持续（消耗品：加金币、回血、解锁卡牌）
- **Buff**：通过 BuffManager 添加 Buff（有持续时间、有图标、可驱散）
- **Passive**：穿戴期间永久生效，无图标、不可驱散、卸下即移除（装备/宝物属性加成）
- **Trigger**：满足条件时触发（预留，如"受到攻击时 30% 概率反击"）
- **原因**：覆盖当前所有已知需求，且未来可扩展新类型

**Decision 3：Passive 效果的实现方式**
- **选择**：直接修改 ChessAttribute，不使用 BuffManager
- **原因**：
  - Buff 系统设计用于"临时状态"（有 Tick、有 UI、可被驱散/净化）
  - 被动效果是"装备属性的一部分"，语义上不是 Buff
  - 用 Buff 实现需要 hack（IsHidden=true, Duration=永久, 不可驱散），且增加 BuffManager 的遍历负担
  - ChessAttribute 已有 ModifyAttribute 接口，直接用即可
- **备选**：使用 BuffTable 的 IsHidden=true + Duration≤0 → 语义不清，性能有轻微影响（每帧 Tick 一个永久 Buff），且"不可驱散"需要额外标记

**Decision 4：SpecialEffectManager 职责**
- **选择**：新增 SpecialEffectManager 作为效果生命周期管理器
- **职责**：
  1. 根据 EffectType 创建对应的 SpecialEffectInstance
  2. 调用 instance.Apply() 应用效果
  3. 记录已应用的效果（用于移除）
  4. 调用 instance.Remove() 移除效果
- **原因**：GameEffectService 目前只做"执行"不做"管理"（无 Remove 概念）。需要一个管理器跟踪"谁身上有哪些活跃效果"以支持移除

**Decision 5：效果的所有权和移除**
- **选择**：效果以 (ownerId, sourceType, sourceId) 三元组标识
  - ownerId = 棋子 ID
  - sourceType = Equipment / Treasure / Synergy / Combat
  - sourceId = 具体物品/羁绊的 ID
- **原因**：卸下装备时需要精确移除"该装备贡献的效果"；一个棋子可能同时有多个来源的同类效果

**Decision 6：与现有系统的集成方式**
- **GameEffectService**：保留现有接口，内部改为调用 SpecialEffectManager
- **ItemEffectFactory**：保留用于 Instant 类型效果（消耗品），不变
- **BuffManager**：Buff 类型效果仍通过 BuffManager.AddBuff() 执行
- **ChessEquipmentManager**：穿戴/卸下时调用 SpecialEffectManager.ApplyEffect() / RemoveEffect()
- **SynergyManager.ActivateSynergy()**：改为调用 SpecialEffectManager 而非直接调 GameEffectService

## Architecture

### SpecialEffectTable 字段（更新后）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 效果 ID |
| Name | string | 效果名称 |
| EffectType | int | 效果类型：1=Instant, 2=Buff, 3=Passive, 4=Trigger |
| BuffIds | int[] | Buff 类型时使用，指向 BuffTable |
| AttributeModifiers | string | Passive 类型时使用，JSON 格式属性修改（如 {"Atk":10,"Def":5}） |
| EffectParams | string | 通用参数（Instant 的具体效果参数、Trigger 的触发条件等） |

### 类层次结构

```
SpecialEffectInstance (abstract)
├── InstantEffect          # 立即执行，无持续
│   └── 通过 ItemEffectFactory 创建具体效果并执行
├── BuffSpecialEffect      # 添加 Buff
│   └── 调用 BuffManager.AddBuff() 添加 BuffIds
├── PassiveEffect          # 被动常驻
│   └── 直接修改 ChessAttribute（Apply 时加，Remove 时减）
└── TriggerEffect          # 条件触发（预留）
    └── 订阅 ChessAttribute 事件，满足条件时执行
```

### 效果生命周期

```
Apply:
  Instant  → Execute() 后立即完成，无需 Remove
  Buff     → BuffManager.AddBuff()，Buff 自己管理生命周期
  Passive  → ChessAttribute.ModifyAttribute()，记录修改值
  Trigger  → 订阅事件，记录订阅引用

Remove:
  Instant  → 无操作（已执行完毕）
  Buff     → BuffManager.RemoveBuff()（按 sourceId 移除）
  Passive  → ChessAttribute.ModifyAttribute() 反向操作
  Trigger  → 取消事件订阅
```

### 数据流

```
穿戴装备:
  EquipmentTable.EffectId → SpecialEffectTable → SpecialEffectManager.ApplyEffect()
  → PassiveEffect.Apply() → ChessAttribute += modifiers

卸下装备:
  SpecialEffectManager.RemoveEffectBySource(chessId, Equipment, equipId)
  → PassiveEffect.Remove() → ChessAttribute -= modifiers

消耗品使用:
  ConsumableTable.UseEffectId → SpecialEffectTable → SpecialEffectManager.ApplyEffect()
  → InstantEffect.Execute() → ItemEffectFactory.Create(type).Execute()

羁绊激活:
  SynergyTable.EffectId → SpecialEffectTable → SpecialEffectManager.ApplyEffect()
  → BuffSpecialEffect.Apply() → BuffManager.AddBuff(buffIds) to all participants

战斗先手:
  CombatRuleTable.InitiativeEffectId → SpecialEffectTable → SpecialEffectManager.ApplyEffect()
  → BuffSpecialEffect.Apply() → BuffManager.AddBuff(buffIds)
```

## Risks / Trade-offs

**[Risk] Passive 效果的属性回滚精确性**
- ChessAttribute 直接修改时，如果多个 Passive 效果修改同一属性，移除顺序可能导致数值偏差
- **缓解**：使用加法模型（每个效果记录自己的增量值），移除时减去自己的值，与顺序无关

**[Risk] Buff 类型效果的移除**
- BuffManager 当前按 BuffId 管理，不按 source 管理。需要扩展支持"按来源移除"
- **缓解**：为 BuffManager.AddBuff() 添加 sourceTag 参数，或在 SpecialEffectManager 中记录 (effectId → buffInstanceIds) 映射

**[Risk] 与 SynergyManager 现有流程的兼容**
- SynergyManager.ActivateSynergy() 当前直接调用 GameEffectService.Execute()
- **缓解**：GameEffectService.Execute() 内部改为委托给 SpecialEffectManager，SynergyManager 代码改动最小

**[Trade-off] Passive 不走 Buff vs 走 Buff**
- 不走 Buff：语义清晰、性能好、但需要自己管理属性回滚
- 走 Buff（IsHidden+永久）：复用现有系统、但语义模糊、增加 BuffManager 负担
- **选择**：不走 Buff，因为被动效果在数量上可能很多（每件装备/宝物都有），性能和清晰度优先

## Open Questions

1. **TriggerEffect 的事件订阅机制**：当前版本只预留接口，具体的触发条件 DSL 如何设计？
   - 建议：后续版本根据实际需求设计，当前 EffectParams 中预留 triggerEvent 和 triggerCondition 字段

2. **Passive 效果是否需要支持百分比修改？**
   - 建议：AttributeModifiers 的 JSON 格式支持 {"Atk": "+10", "Def": "+5%"}，由 PassiveEffect 解析

3. **多个 Buff 类型效果指向同一个 BuffId 时如何避免重复添加？**
   - 建议：由 BuffManager 的现有去重逻辑处理（如果有）；或在 SpecialEffectManager 中做检查
