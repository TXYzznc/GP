## 1. 创建 OutlineController 组件

- [ ] 1.1 将 `OutlineTest.cs` 重命名为 `OutlineController.cs`，改造 API 为 `ShowOutline(Color, float)` / `HideOutline()` / `IsOutlineActive`，移除对 OutlineConfig 的依赖，Awake 缓存 Renderer，OnDestroy 自动清理
- [ ] 1.2 定义描边颜色常量：`SelectionColor`（黄）、`AllyColor`（绿）、`EnemyColor`（红）、`DefaultSize`

## 2. 清理旧描边系统

- [ ] 2.1 删除 `ChessOutlineController.cs`
- [ ] 2.2 删除 `OutlineDisplayManager.cs`
- [ ] 2.3 删除 `OutlineDisplayManagerEditor.cs`
- [ ] 2.4 修改 `ChessEntity.cs`：将 `OutlineController` 属性类型从 `ChessOutlineController` 改为 `OutlineController`，简化初始化逻辑（获取或添加组件，无需调用 Initialize）

## 3. 选中描边集成

- [ ] 3.1 修改 `ChessSelectionManager.SelectChess()`：调用 `entity.OutlineController.ShowOutline(OutlineController.SelectionColor, OutlineController.DefaultSize)`
- [ ] 3.2 修改 `ChessSelectionManager.DeselectChess()`：调用 `OutlineController.HideOutline()`

## 4. 策略卡目标描边集成

- [ ] 4.1 在 `CardSlotItem` 中添加 `m_PreviewTargets` 缓存列表和 `m_IsCardOutlineActive` 状态标记
- [ ] 4.2 在 `UpdateCardPreview()` 战场区域分支中：调用 `GetAffectedTargets()` 获取目标，遍历目标调用 `ShowOutline`（根据 Camp 选绿/红），增量更新（对比新旧列表，移除不再是目标的描边）
- [ ] 4.3 新增 `ClearCardTargetOutlines()` 方法：遍历 `m_PreviewTargets` 调用 `HideOutline()`，然后恢复选中棋子的黄色描边（如果有）
- [ ] 4.4 在 `OnEndDrag()` 中调用 `ClearCardTargetOutlines()`
- [ ] 4.5 在拖拽离开战场区域时（进入吸附区/无效区）也调用 `ClearCardTargetOutlines()`

## 5. 编译验证

- [ ] 5.1 全量编译确认无引用错误，grep 确认无残留的旧类引用
