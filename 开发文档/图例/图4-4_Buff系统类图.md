# 图4-4 Buff系统类图

> **图表类型：** 类图（UML）  
> **论文位置：** 第4章 系统设计 - 4.2.3 Buff系统设计  
> **尺寸建议：** A4 宽度 85%  
> **标题编号：** 图 4-4

---

## 📋 设计规范

**标准UML类图，展示Buff系统的类继承体系和关系**

### 元素规范
- ✅ **类**：矩形框，分为类名、属性、方法三个区域
- ✅ **抽象类**：类名用斜体表示，或标注《abstract》
- ✅ **泛化**（继承）：实线带三角箭头，指向父类
- ✅ **实现**（接口）：虚线带三角箭头
- ✅ **关联**：实线标注关系名和基数

### 整体风格
- ✅ 纯黑白，无颜色填充
- ✅ 线条 1px 黑色
- ✅ 文字清晰，12-13px
- ✅ 重要类垂直排列，子类在下方

---

## 🎨 ASCII 设计示意

```
┌──────────────────────────────────────────────────────────┐
│                  Buff系统类图                             │
├──────────────────────────────────────────────────────────┤
│                                                          │
│        ┌────────────────────────────────────┐           │
│        │  << interface >>                   │           │
│        │  IBuff                            │           │
│        ├────────────────────────────────────┤           │
│        │  + Apply(target)                  │           │
│        │  + Tick()                         │           │
│        │  + Remove()                       │           │
│        │  + IsActive()                     │           │
│        └────────────┬───────────────────────┘           │
│                     │                                    │
│                  实现 (虚线)                             │
│                     │                                    │
│        ┌────────────▼───────────────────────┐           │
│        │  BuffBase (《abstract》)           │           │
│        ├────────────────────────────────────┤           │
│        │  # buffID                          │           │
│        │  # duration                        │           │
│        │  # level                           │           │
│        │  # startTime                       │           │
│        │  # isActive                        │           │
│        ├────────────────────────────────────┤           │
│        │  + Apply(target)                  │           │
│        │  + Tick()                         │           │
│        │  + Remove()                       │           │
│        │  + IsActive()                     │           │
│        │  # OnApply(target) [抽象]         │           │
│        │  # OnTick() [抽象]                │           │
│        │  # OnRemove(target) [抽象]        │           │
│        └────────────┬───────────────────────┘           │
│                     │ 泛化 (实线)                       │
│           ┌─────────┼─────────┬──────────┐              │
│           │         │         │          │              │
│      ┌────▼──┐  ┌───▼──┐ ┌──▼───┐  ┌──▼────┐          │
│      │StatMod│  │ Dot  │ │ CC   │  │Shield │          │
│      │Buff   │  │Buff  │ │Buff  │  │Buff   │          │
│      ├───────┤  ├──────┤ ├──────┤  ├───────┤          │
│      │-attr  │  │-dmg  │ │-type │  │-shield│          │
│      │-value │  │-tick │ │-dur  │  │Value  │          │
│      ├───────┤  ├──────┤ ├──────┤  ├───────┤          │
│      │+OnApp │  │+OnTic│ │+OnApp│  │+OnApp │          │
│      │ly()   │  │k()   │ │ly()  │  │ly()   │          │
│      │+OnTic │  │+OnRem│ │+OnRem│  │+OnTic │          │
│      │k()    │  │ove() │ │ove() │  │k()    │          │
│      │+OnRem │  └──────┘ └──────┘  └───────┘          │
│      │ove()  │                                         │
│      └───────┘                                         │
│                                                          │
│        ┌──────────────────────────────────┐             │
│        │   BuffManager                    │             │
│        ├──────────────────────────────────┤             │
│        │   - buffList: List<IBuff>        │             │
│        │   - owner: ChessEntity           │             │
│        ├──────────────────────────────────┤             │
│        │   + ApplyBuff(buffID): void      │             │
│        │   + RemoveBuff(buffID): void     │             │
│        │   + Update(): void               │             │
│        │   + GetBuffsByType(type): List   │             │
│        │   - CreateBuffInstance(id)      │             │
│        └──────────────────────────────────┘             │
│            ◇ 聚集                                       │
│            │ (1:N)                                      │
│            │                                            │
│        ┌───▼────────────────────┐                       │
│        │  BuffConfig (配置)     │                       │
│        ├────────────────────────┤                       │
│        │  + buffID              │                       │
│        │  + buffName            │                       │
│        │  + buffType            │                       │
│        │  + parameters          │                       │
│        │  + maxDuration         │                       │
│        └────────────────────────┘                       │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

## 📊 Buff系统类详细表

| 类名 | 职责 | 关键属性 | 关键方法 |
|------|------|--------|--------|
| **IBuff** | Buff接口（定义契约） | 无 | Apply(target)、Tick()、Remove()、IsActive() |
| **BuffBase** | Buff基类（抽象） | buffID、duration、level、startTime、isActive | Apply(target)、Tick()、Remove()、IsActive()、OnApply()【抽象】、OnTick()【抽象】、OnRemove()【抽象】 |
| **StatModBuff** | 属性修正Buff（继承BuffBase） | attributeName、attributeValue | OnApply()、OnTick()、OnRemove() |
| **DotBuff** | 持续伤害Buff（继承BuffBase） | damagePerTick、tickInterval | OnApply()、OnTick()、OnRemove() |
| **CCBuff** | 控制Buff（继承BuffBase） | controlType（眩晕/冻结等） | OnApply()、OnTick()、OnRemove() |
| **ShieldBuff** | 护盾Buff（继承BuffBase） | shieldValue、shieldMax | OnApply()、OnTick()、OnRemove() |
| **BuffManager** | Buff管理器 | buffList、owner | ApplyBuff(buffID)、RemoveBuff(buffID)、Update()、GetBuffsByType(type) |
| **BuffConfig** | Buff配置（数据） | buffID、buffName、buffType、parameters、maxDuration | 无方法 |

### 关键关系说明

| 关系 | 源类 | 目标类 | 类型 | 说明 |
|------|------|--------|------|------|
| 实现 | BuffBase | IBuff | 虚线 | BuffBase实现IBuff接口 |
| 泛化 | StatModBuff | BuffBase | 实线 | 属性修正Buff继承BuffBase |
| 泛化 | DotBuff | BuffBase | 实线 | 持续伤害Buff继承BuffBase |
| 泛化 | CCBuff | BuffBase | 实线 | 控制Buff继承BuffBase |
| 泛化 | ShieldBuff | BuffBase | 实线 | 护盾Buff继承BuffBase |
| 聚集 | BuffManager | BuffBase | 空心菱形 | BuffManager管理多个Buff实例 |
| 依赖 | BuffManager | BuffConfig | 虚线 | BuffManager依赖BuffConfig创建Buff |

---

## 📝 文字介绍

### Buff系统类设计

Buff系统的类设计（如图4-4所示）采用**接口+抽象基类+具体实现**的三层设计模式，提供了高度的灵活性和扩展性。

#### 设计模式分析

**接口定义统一契约**  
IBuff 接口定义了所有Buff必须实现的四个核心方法：Apply（应用Buff）、Tick（每帧更新）、Remove（移除Buff）、IsActive（检查Buff是否活跃）。这个接口作为Buff系统的核心抽象，确保所有Buff类型都遵循统一的生命周期管理。

**抽象基类提供公共实现**  
BuffBase 作为抽象基类实现了 IBuff 接口，提供了Buff的通用属性（buffID、duration、level、startTime、isActive）和通用方法。其中 Apply、Tick、Remove 这三个方法实现了通用的生命周期逻辑，而 OnApply、OnTick、OnRemove 三个抽象方法留给子类实现具体的效果逻辑。

这种设计实现了**模板方法模式**：
- Apply() 方法先调用 OnApply() 执行初始化逻辑，然后标记为活跃
- Tick() 方法检查时间，若未过期则调用 OnTick()，否则调用 Remove()
- Remove() 方法先调用 OnRemove() 执行清理逻辑，然后标记为非活跃

**具体Buff的多样化实现**  
四个具体Buff类继承自 BuffBase，各自实现不同的效果：
- **StatModBuff**：修改属性（攻击力、防御力等）
- **DotBuff**：每秒造成伤害（持续伤害效果）
- **CCBuff**：控制效果（眩晕、冻结等，通常禁止玩家操作）
- **ShieldBuff**：创建护盾（吸收伤害的临时层）

这种多样化实现支持了不同的战斗效果需求，同时保持了代码的统一性。

**管理器模式的生命周期管理**  
BuffManager 是Buff系统的管理中心，负责：
- 维护当前活跃的Buff列表
- 根据buffID创建Buff实例（通过BuffConfig配置）
- 每帧更新所有Buff的状态（调用Tick方法）
- 提供查询接口（如按类型获取Buff）

---

## 🎯 使用场景

- **Buff效果框架设计**：指导新Buff类型的添加
- **系统性能分析**：Buff是战斗中高频调用的模块
- **测试用例设计**：为每个Buff类型设计单元测试
- **效果平衡调整**：根据Buff的类型调整参数

---

## ✅ 质量检查清单

- [ ] 所有类用矩形框表示
- [ ] 抽象类标注<<abstract>>或斜体
- [ ] 接口标注<<interface>>
- [ ] 属性和方法列出清晰
- [ ] 泛化关系用带三角的实线表示
- [ ] 实现关系用虚线带三角表示
- [ ] 聚集关系用空心菱形标注
- [ ] 所有线条 1px 黑色，无填充
- [ ] 文字标注清晰完整

---

## 📌 与其他图表的关系

- **图4-1（分层架构）**：Buff系统位于业务逻辑层
- **图4-3（战斗系统类图）**：Buff是战斗系统的组成部分
- **第4.2.3章详细实现**：各Buff类型的具体实现细节

---

## 📖 论文引用示例

在论文 4.2.3 段落，引入本图：

> Buff系统的类设计如图4-4所示，采用**接口+抽象基类+具体实现**的三层设计模式。
>
> **统一的Buff契约**  
> IBuff 接口定义了所有Buff必须实现的四个核心方法：Apply（应用时执行初始化）、Tick（每帧检查并更新状态）、Remove（移除时执行清理）、IsActive（查询当前活跃状态）。这个接口作为系统的核心抽象，确保所有Buff类型都遵循统一的生命周期管理。
>
> **模板方法模式的应用**  
> BuffBase 作为抽象基类实现了 IBuff 接口，通过模板方法模式将Buff的生命周期分为两层：通用层（Apply、Tick、Remove）和特化层（OnApply、OnTick、OnRemove）。通用层实现了公共的时间管理和状态转换逻辑，特化层由子类实现具体的效果逻辑。这种分离使得新增Buff类型时只需关注效果实现，无需重复编写生命周期代码。
>
> **多样化的Buff类型**  
> 系统提供了四个主要的Buff类型：StatModBuff（属性修正）、DotBuff（持续伤害）、CCBuff（控制效果）、ShieldBuff（护盾）。每个类型通过继承BuffBase并实现特化方法来定义自己的效果。这种设计支持了多种战斗效果需求，同时保持了代码的统一性和可扩展性。新增Buff类型只需继承BuffBase并实现三个抽象方法。
>
> **集中化的Buff管理**  
> BuffManager 负责Buff的集中管理，包括创建、更新、移除等操作。BuffManager 通过 BuffConfig 配置表获取Buff的参数信息，实现了数据驱动的Buff系统。每帧通过调用 Update() 方法批量更新所有Buff的状态，提高了系统的性能效率。
