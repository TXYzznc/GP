# 图16：Buff 数据结构

**位置**: 第4章 战斗系统  
**章节**: 4.4 Buff 系统  
**类型**: 类图  
**用途**: 展示 Buff 的数据模型

## Mermaid 代码

```mermaid
classDiagram
    class Buff {
        -int id
        -string name
        -int duration
        -int remaining_time
        -float value
        -string effect_type
        -int stack_count
        -Entity target
        +Apply()
        +Update()
        +Remove()
        +GetRemainingTime()
        +IsActive()
    }

    class BuffEffect {
        -string type
        -float value
        -string condition
        +Execute()
        +Undo()
    }

    class BuffManager {
        -List~Buff~ active_buffs
        +AddBuff()
        +RemoveBuff()
        +UpdateBuffs()
        +GetBuffsByType()
        +GetTotalValue()
    }

    class Entity {
        -int id
        -float hp
        -float attack
        -float defense
        -List~Buff~ buffs
        +ApplyBuff()
        +RemoveBuff()
        +GetModifiedStats()
    }

    Buff --> BuffEffect
    BuffManager --> Buff
    Entity --> Buff
    Entity --> BuffManager
```

## 说明

Buff 数据结构的设计：

1. **Buff 类** - 表示单个 Buff 实例
   - 属性：ID、名称、持续时间、效果值、效果类型、堆叠数
   - 方法：应用、更新、移除、获取剩余时间

2. **BuffEffect 类** - 表示 Buff 的具体效果
   - 属性：效果类型、效果值、触发条件
   - 方法：执行效果、撤销效果

3. **BuffManager 类** - 管理所有活跃的 Buff
   - 维护活跃 Buff 列表
   - 提供 Buff 查询和管理接口

4. **Entity 类** - 游戏实体（角色、敌人等）
   - 维护应用到该实体的 Buff 列表
   - 计算 Buff 对属性的修改

