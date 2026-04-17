> **最后更新**: 2026-04-17
> **状态**: 有效
> **分类**: 系统设计

---

# DetailInfoUI

**文件**：`Assets/AAAGame/Scripts/UI/Item/DetailInfoUI.cs`（约 269 行）

## 目录

- [功能概述](#功能概述)
- [双模式设计](#双模式设计)
- [UI 组件配置](#ui-组件配置)
- [使用示例](#使用示例)
- [布局问题诊断](#布局问题诊断)

---

## 功能概述

DetailInfoUI 支持**卡牌模式**和**棋子模式**两种显示模式，自动根据数据类型切换。

- **卡牌模式**（mode=0）：显示策略卡名称、描述、灵力消耗和范围
- **棋子模式**（mode=1）：显示棋子名称（含星级）、描述、Buff 列表

---

## 双模式设计

### 核心字段

```csharp
private ChessEntity m_ChessEntity;
private Dictionary<int, BuffItem> m_BuffItems;
private int m_CurrentMode = 0;  // 0=卡牌, 1=棋子
```

### 数据设置方法

- `SetData(CardData)` — 原有方法，设置策略卡数据，自动切换为卡牌模式
- `SetChessUnitData(ChessEntity)` — 新增方法，设置棋子数据，自动切换为棋子模式

### UI 刷新逻辑

```
RefreshUI() [入口，根据 m_CurrentMode 分发]
├─ RefreshCardUI()       [卡牌模式]
│  ├─ 隐藏 varBuffBg
│  ├─ 显示卡牌名称
│  ├─ 显示卡牌描述
│  └─ 显示灵力消耗 + 范围
│
└─ RefreshChessUnitUI() [棋子模式]
   ├─ 显示 varBuffBg
   ├─ 显示棋子名称 + 星级（★ 符号）
   ├─ 显示棋子描述
   ├─ 清空 OtherText
   └─ 刷新 Buff 显示
```

### Buff 管理

棋子模式下新增 Buff 管理（参考 SummonChessStateUI 的实现）：

- `RefreshAllBuffs()` — 清除已有 BuffItem，从 ChessEntity.BuffManager 获取当前所有 Buff 并逐个添加
- `AddBuffItem(buffId, stackCount)` — 实例化 varBuffItem 预制体到 varBuffBg 容器，设置数据和层数
- `ClearAllBuffItems()` — 销毁所有 BuffItem GameObject，清空缓存字典

**棋子 Buff 显示时序**：
```
SetChessUnitData(chessEntity)
  ↓ RefreshUI() → RefreshChessUnitUI()
  ↓ RefreshChessUnitUI() → RefreshAllBuffs()
  ↓ RefreshAllBuffs() 从 BuffManager 提取 Buff 列表
  ↓ 逐个实例化 BuffItem 并显示
```

---

## UI 组件配置

| 组件 | 用途 | 卡牌模式 | 棋子模式 |
|------|------|---------|---------|
| varTitleText | 标题文本 | 显示卡牌名称 | 显示棋子名+星级 |
| varDescText | 描述文本 | 显示卡牌描述 | 显示棋子描述 |
| varOtherText | 其他信息 | 灵力消耗+范围 | 清空（暂不显示） |
| varBuffBg | Buff 容器 | 隐藏 | 显示 Buff 列表 |
| varBuffItem | Buff 预制体 | 不使用 | 用于实例化 Buff 项 |

### 预制体结构

```
DetailInfoUI (RectTransform)
└─ DetailInfoBg (VerticalLayoutGroup)
   ├─ TitleBg (LayoutElement)
   │  └─ TitleText
   ├─ BuffBg (LayoutElement + GridLayoutGroup)
   │  └─ BuffItem (预制体)
   ├─ DescBg (LayoutElement)    ← 灵活伸缩，铺满剩余高度
   │  └─ DescText
   └─ OtherBg (LayoutElement)
      └─ OtherText
```

**VerticalLayoutGroup 高度分配**：
- TitleBg: preferredHeight=80
- BuffBg: preferredHeight=130
- DescBg: preferredHeight=-1, flexibleHeight=1（自动填满剩余空间）
- OtherBg: preferredHeight=60
- spacing=10, padding(t:10, b:10)

---

## 使用示例

```csharp
var detailUI = GetComponent<DetailInfoUI>();

// 显示策略卡信息
detailUI.SetData(cardData);
detailUI.RefreshUI();
detailUI.ShowWithAnimation();

// 显示棋子信息
detailUI.SetChessUnitData(chessEntity);
detailUI.RefreshUI();
detailUI.ShowWithAnimation();
```

---

## 布局问题诊断

**问题**：VerticalLayoutGroup 与子对象手动 RectTransform 设置冲突，导致布局高度不正确。

### 根本原因

- LayoutElement 设置 preferredHeight（如 TitleBg=80）
- 同时手动设置了 offsetMin/offsetMax（如 sizeDelta.y=0）
- **RectTransform 优先级高于 LayoutElement**，导致 LayoutElement 被忽视

### 典型错误配置

```
TitleBg:
  offsetMin: (10, -10), offsetMax: (350, -10)
  sizeDelta: (340, 0)   ← ⚠️ 高度为 0，与 preferredHeight=80 冲突
```

### 修复方案（推荐）

让 VerticalLayoutGroup 完全接管：所有子对象重置 offsetMin/offsetMax/sizeDelta 为 (0,0)，锚点设为 (0,1)~(1,1)，仅通过 LayoutElement 控制高度。

```
TitleBg:  anchorMin=(0,1), anchorMax=(1,1), offsetMin=(0,0), offsetMax=(0,0), sizeDelta=(0,0)
          → LayoutElement: preferredHeight=80
BuffBg:   同上 → LayoutElement: preferredHeight=130
DescBg:   同上 → LayoutElement: preferredHeight=-1, flexibleHeight=1
OtherBg:  同上 → LayoutElement: preferredHeight=60
```

**规范**：要么用 LayoutGroup，要么用手动 offset，**不混用**。

### 关键约束

- varBuffBg 和 varBuffItem 必须由 UI 工具生成并配置
- BuffItem 的 SetData() 需要 DataTable 中的 Buff 配置已加载
- ChessEntity.BuffManager 必须在初始化时正确配置
- Buff 显示在棋子模式下自动刷新，卡牌模式下隐藏

### 后续扩展

- [ ] 添加棋子星级升级后的 UI 提示
- [ ] 支持 Buff 变化的实时监听
- [ ] 添加棋子技能信息展示区域
- [ ] 支持棋子品质的颜色标记
