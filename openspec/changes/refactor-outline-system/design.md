## Context

当前描边系统有两套管理逻辑：
1. **ChessOutlineController** — 挂在棋子上，按阵营关系常驻显示不同颜色描边（敌红/友绿/中立黄），配置通过 `#if UNITY_EDITOR` 的 AssetDatabase 加载（打包后失效）
2. **OutlineDisplayManager** — 场景单例，按 Layer 自动扫描范围内物体并添加描边，使用 FindObjectsOfType 全场扫描（性能问题）

底层渲染管线（`OutlineRenderFeature` + Shader）表现良好，无需修改。

新需求将描边简化为两个按需触发的场景，不再需要常驻描边。

## Goals / Non-Goals

**Goals:**
- 实现通用的 `OutlineController` 组件，替代两套旧系统
- 选中棋子时显示黄色描边
- 策略卡拖拽时对目标棋子显示阵营描边（己方绿/敌方红）
- 清理所有旧系统代码和引用

**Non-Goals:**
- 不修改渲染管线（OutlineRenderFeature、Shader、OutlineConfig）
- 不修改阵营关系系统（CampRelationService）
- 不重做 OutlineConfig 资产（复用现有 .asset 文件）
- 不做 Layer 自动扫描描边（OutlineDisplayManager 功能不保留）

## Decisions

### 1. OutlineController 设计 — 轻量包装组件

**方案**：`OutlineController` 直接取代 `OutlineTest`，保留其 Renderer 缓存和 RenderFeature 调用逻辑，简化 API 为 `ShowOutline(Color, float)` / `HideOutline()`。

```
OutlineController (取代 OutlineTest)
  ├── ShowOutline(Color color, float size)  — 显示/更新描边
  ├── HideOutline()                         — 隐藏描边
  ├── IsOutlineActive                       — 查询状态
  └── 内部：List<Renderer> 缓存 + OutlineRenderFeature 调用
```

**理由**：
- `OutlineTest` 已有完整的 Renderer 缓存和 RenderFeature 调用逻辑，直接在此基础上改造即可
- 移除对 `OutlineConfig` 资产的依赖，颜色和大小改为由调用方传入参数
- 移除 `OutlineDisplayManager` 相关的被动调用模式（ApplyOutline/UpdateOutline 分离），统一为 ShowOutline 一个入口

### 2. 选中描边 — ChessSelectionManager 直接调用

**方案**：在 `ChessSelectionManager.SelectChess()` / `DeselectChess()` 中直接调用 `OutlineController.ShowOutline(黄色)` / `HideOutline()`。

**理由**：选中逻辑已经在 ChessSelectionManager 中集中管理，无需额外中间层。

### 3. 策略卡目标描边 — CardSlotItem 拖拽流程集成

**方案**：在 `CardSlotItem.UpdateCardPreview()` 中：
1. 调用已有的 `GetAffectedTargets()` 获取目标列表
2. 遍历目标，根据 `chess.Camp` 判断颜色（Player→绿，Enemy→红）
3. 调用 `target.OutlineController.ShowOutline(color, size)`
4. 在 `OnEndDrag` / 拖拽取消时，遍历之前的目标列表调用 `HideOutline()`

**缓存策略**：CardSlotItem 维护一个 `List<ChessEntity> m_PreviewTargets` 缓存当前预览的目标列表，拖拽结束时统一清理。拖拽中每次更新时，对比新旧列表，移除不再是目标的描边、添加新目标的描边。

**理由**：
- `GetAffectedTargets()` 已有完整的目标判断逻辑（TargetType + AreaRadius + 距离检测）
- 阵营判断直接用 `chess.Camp` 比较，不需要走 CampRelationService（当前 PVE 模式下 0=玩家 1=敌人 足够）

### 4. 描边颜色常量

在 `OutlineController` 中定义静态常量：
```csharp
public static readonly Color SelectionColor = new Color(1f, 0.85f, 0f);  // 黄色
public static readonly Color AllyColor = Color.green;                      // 己方绿
public static readonly Color EnemyColor = Color.red;                       // 敌方红
public static readonly float DefaultSize = 20f;                            // 默认宽度
```

**理由**：目前只有 3 种颜色场景，用常量最简单。未来如需配置化可改为 ScriptableObject。

### 5. 删除文件清单

| 文件 | 操作 |
|------|------|
| `ChessOutlineController.cs` | 删除 |
| `OutlineDisplayManager.cs` | 删除 |
| `OutlineDisplayManagerEditor.cs` | 删除 |
| `OutlineTest.cs` | 删除（由 OutlineController 替代） |

### 6. 修改文件清单

| 文件 | 修改内容 |
|------|----------|
| `ChessEntity.cs` | `OutlineController` 属性类型从 `ChessOutlineController` 改为 `OutlineController`，初始化逻辑简化 |
| `ChessSelectionManager.cs` | 选中/取消选中改为调用 `OutlineController.ShowOutline/HideOutline` |
| `CardSlotItem.cs` | `UpdateCardPreview` 添加目标描边，`OnEndDrag` 添加描边清理 |

## Risks / Trade-offs

**[风险] OutlineController 替代 OutlineTest 后，OutlineDisplayManager 引用断裂**
→ 缓解：OutlineDisplayManager 整个删除，不存在断裂问题

**[风险] 策略卡拖拽中频繁刷新描边目标列表导致性能问题**
→ 缓解：`UpdateCardPreview` 已有 0.1 秒节流（PerformRaycast 频率），描边更新跟随此频率；对比新旧列表只做增量更新

**[风险] 选中描边和策略卡描边同时存在时冲突**
→ 缓解：策略卡使用时处于战斗阶段，ChessSelectionManager 此时为 SelectionOnly 模式。如果同时选中+策略卡，后调用的 ShowOutline 会覆盖前一个颜色（OutlineRenderFeature.DrawOrUpdateOutlines 的 update 语义）。这是可接受的行为 — 策略卡描边优先级应高于选中描边。

**[取舍] 颜色硬编码 vs 配置化**
→ 当前选择硬编码常量，3 种颜色场景足够简单。如果未来需要更多描边场景或美术调色需求，再改为 ScriptableObject 配置。
