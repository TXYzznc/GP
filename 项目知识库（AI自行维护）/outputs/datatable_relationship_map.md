# 配置表关系地图

## 核心关系链

```
棋子配置表 (SummonChessTable)
  ↓ [Races/Classes ID]
羁绊配置表 (SynergyTable)
  ↓ [EffectId]
特殊效果表 (SpecialEffectTable)
  ↓ [BuffIds/SelfBuffIds]
Buff配置表 (BuffTable)
```

## 详细关系

| 源表 | 字段 | → | 目标表 | 用途 |
|------|------|---|--------|------|
| **SummonChessTable** | Races (1-3) | → | **SynergyTable** Id | 棋子触发种族羁绊 |
| **SummonChessTable** | Classes (101-103) | → | **SynergyTable** Id | 棋子触发职业羁绊 |
| **TreasureTable** | SynergyIds | → | **SynergyTable** Id | 宝物关联宝物羁绊(201-203) |
| **TreasureTable** | SpecialEffectId | → | **SpecialEffectTable** Id | 宝物特殊效果(3001-3009) |
| **EquipmentTable** | SpecialEffectId | → | **SpecialEffectTable** Id | 装备特殊效果(2001-2009) |
| **ConsumableTable** | UseEffectId | → | **SpecialEffectTable** Id | 消耗品效果(1001-1030) |
| **SynergyTable** | EffectId | → | **SpecialEffectTable** Id | 羁绊效果(4001-4009) |
| **SpecialEffectTable** | BuffIds/SelfBuffIds | → | **BuffTable** Id | 应用具体Buff |

## 羁绊系统专用

| Buff ID范围 | 对应羁绊 | 触发条件 |
|-----------|--------|--------|
| 6001 | 日月(种族1) | 场上2名日月种族棋子 |
| 6002 | 污染生物(种族2) | 场上2名污染生物种族棋子 |
| 6003 | 灵魂(种族3) | 场上1名灵魂种族棋子 |
| 6004 | 射手(职业101) | 场上1名射手职业棋子 |
| 6005 | 法师(职业102) | 场上2名法师职业棋子 |
| 6006 | 战士(职业103) | 场上2名战士职业棋子 |
| 6007 | 三神器(宝物201) | 集齐三件宝物 |
| 6008 | 月神(宝物202) | 集齐三件宝物 |
| 6009 | 炎魔(宝物203) | 集齐三件宝物 |

## SpecialEffectTable ID范围

| 范围 | 用途 | 引用来源 |
|-----|------|--------|
| 1001-2000 | 消耗品效果 | ConsumableTable |
| 2001-3000 | 装备效果 | EquipmentTable |
| 3001-4000 | 宝物效果 | TreasureTable |
| 4001-5000 | 羁绊效果 | SynergyTable |

## 数据流向

**棋子上场 → 羁绊触发 → 应用效果 → 更新Buff**

1. 棋子的 Races/Classes 匹配 SynergyTable 中的羁绊ID
2. 激活条件满足(RequireCount)时，触发 SynergyTable.EffectId 指向的特殊效果
3. SpecialEffectTable 定义该效果的逻辑（可能包含 BuffIds）
4. BuffTable 中的Buff通过属性修改(StatMods)实际改变棋子属性

**特例：羁绊Buff直接应用**
- 羁绊触发时，游戏系统可直接应用 BuffTable 中的对应Buff(6001-6009)
- 无需通过 SpecialEffectTable 的 BuffIds 字段
