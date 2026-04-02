---
name: DetailInfoUI 双模式实现总结
description: DetailInfoUI 增加棋子数据显示功能，支持卡牌和棋子两种模式
type: project
---

# DetailInfoUI 双模式实现总结 (2026-04-03)

## 修改概述

在原有 **策略卡信息显示** 的基础上，扩展了 DetailInfoUI 以支持 **棋子详细信息显示**，实现两种模式的自动切换。

## 主要修改

### 1. 数据字段扩展
```csharp
private ChessEntity m_ChessEntity;                                    // 棋子数据
private System.Collections.Generic.Dictionary<int, BuffItem> m_BuffItems;  // Buff项缓存
private int m_CurrentMode = 0;  // 0=卡牌, 1=棋子
```

### 2. 数据设置方法

#### SetData(CardData)
- 原有方法保留，用于设置策略卡数据
- 自动设置模式为 0（卡牌模式）

#### SetChessUnitData(ChessEntity)
- **新增方法**，用于设置棋子数据
- 自动设置模式为 1（棋子模式）
- 参数：棋子实体引用

### 3. UI 刷新逻辑重构

原 `RefreshUI()` 拆分为三层：

```
RefreshUI() [入口]
├─ RefreshCardUI()      [卡牌模式]
│  ├─ 隐藏 varBuffBg
│  ├─ 显示卡牌名称
│  ├─ 显示卡牌描述
│  └─ 显示灵力消耗 + 范围
│
└─ RefreshChessUnitUI() [棋子模式]
   ├─ 显示 varBuffBg
   ├─ 显示棋子名称 + 星级
   ├─ 显示棋子描述
   ├─ 清空 OtherText
   └─ 刷新 Buff 显示
```

### 4. Buff 管理模块

新增 Buff 管理功能（参考 SummonChessStateUI 的实现）：

- **RefreshAllBuffs()** - 刷新所有 Buff 显示
  - 清除已有的 BuffItem
  - 从 ChessEntity.BuffManager 获取当前所有 Buff
  - 逐个添加对应的 BuffItem

- **AddBuffItem(buffId, stackCount)** - 添加单个 Buff
  - 实例化 varBuffItem 预制体到 varBuffBg 容器
  - 获取 BuffItem 组件，设置数据和层数
  - 缓存到 Dictionary 中

- **ClearAllBuffItems()** - 清除所有 Buff
  - 销毁所有 BuffItem GameObject
  - 清空缓存字典

## UI 组件配置

### 已使用组件
| 组件 | 用途 | 卡牌模式 | 棋子模式 |
|------|------|---------|---------|
| varTitleText | 标题文本 | ✅ 显示卡牌名称 | ✅ 显示棋子名+星级 |
| varDescText | 描述文本 | ✅ 显示卡牌描述 | ✅ 显示棋子描述 |
| varOtherText | 其他信息 | ✅ 灵力消耗+范围 | ✅ 清空（暂不显示） |
| varBuffBg | Buff 容器 | ❌ 隐藏 | ✅ 显示 Buff 列表 |
| varBuffItem | Buff 预制体 | - | ✅ 用于 Buff 实例化 |

## 使用示例

### 显示策略卡信息
```csharp
var detailUI = GetComponent<DetailInfoUI>();
detailUI.SetData(cardData);      // 设置数据
detailUI.RefreshUI();             // 刷新显示
detailUI.ShowWithAnimation();     // 播放动画
```

### 显示棋子信息
```csharp
var detailUI = GetComponent<DetailInfoUI>();
detailUI.SetChessUnitData(chessEntity);  // 设置数据
detailUI.RefreshUI();                     // 刷新显示
detailUI.ShowWithAnimation();             // 播放动画
```

## 关键设计

### 1. 模式自动切换
- 通过 `m_CurrentMode` 标记当前模式
- `RefreshUI()` 自动判断调用对应的刷新方法
- 调用方无需关心具体模式

### 2. Buff 显示一致性
- 使用与 SummonChessStateUI 相同的 Buff 管理机制
- 显示效果和战斗 UI 中的棋子状态 Buff 一致
- 支持 Buff 的动态增删（通过 BuffManager 事件）

### 3. 灵活的信息组织
- 棋子星级用 ★ 符号表示
- 卡牌其他信息清晰展示
- 两种模式各自最大化展示各自的关键信息

## 时序说明

**棋子 Buff 显示时序**：
1. SetChessUnitData(chessEntity) 保存棋子引用
2. RefreshUI() → RefreshChessUnitUI()
3. RefreshChessUnitUI() → RefreshAllBuffs()
4. RefreshAllBuffs() 从 BuffManager 中提取 Buff 列表
5. 逐个实例化 BuffItem 并显示

## 后续扩展

- [ ] 添加棋子星级升级后的 UI 提示
- [ ] 支持 Buff 变化的实时监听（如需要动态更新）
- [ ] 添加棋子技能信息展示区域
- [ ] 支持棋子品质的颜色标记

## 关键约束

- ⚠️ varBuffBg 和 varBuffItem 必须由 UI 工具生成并配置
- ⚠️ BuffItem 的 SetData() 需要 DataTable 中的 Buff 配置已加载
- ⚠️ ChessEntity.BuffManager 必须在初始化时正确配置
- ⚠️ Buff 显示在棋子模式下自动刷新，卡牌模式下隐藏

---

**修改文件**：`Assets/AAAGame/Scripts/UI/Item/DetailInfoUI.cs`
**行数**：约 269 行
**新增代码量**：~180 行
**修改日期**：2026-04-03
