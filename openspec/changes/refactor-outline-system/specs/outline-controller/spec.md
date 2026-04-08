## ADDED Requirements

### Requirement: OutlineController 组件提供描边显示/隐藏 API
`OutlineController` SHALL 作为 MonoBehaviour 组件挂载在需要描边的物体上，提供 `ShowOutline(Color color, float size)` 和 `HideOutline()` 两个公共方法。组件初始化时 SHALL 自动缓存所有子级 Renderer。

#### Scenario: 显示描边
- **WHEN** 外部调用 `ShowOutline(Color.yellow, 20f)`
- **THEN** 该物体的所有子级 Renderer 通过 `OutlineRenderFeature.DrawOrUpdateOutlines` 显示指定颜色和宽度的描边

#### Scenario: 隐藏描边
- **WHEN** 外部调用 `HideOutline()`
- **THEN** 该物体的描边通过 `OutlineRenderFeature.RemoveDrawOutlines` 被移除

#### Scenario: 重复调用 ShowOutline 更新颜色
- **WHEN** 描边已显示为黄色，再次调用 `ShowOutline(Color.red, 20f)`
- **THEN** 描边颜色 SHALL 更新为红色，不产生重复渲染数据

### Requirement: OutlineController 生命周期管理
`OutlineController` SHALL 在 `OnDestroy` 时自动移除描边。组件 SHALL 提供 `IsOutlineActive` 只读属性查询当前描边状态。

#### Scenario: 物体销毁时自动清理
- **WHEN** 挂载 OutlineController 的 GameObject 被销毁
- **THEN** 描边 SHALL 自动从 OutlineRenderFeature 中移除，不产生残留

### Requirement: 删除旧描边管理系统
SHALL 删除 `ChessOutlineController`、`OutlineDisplayManager`、`OutlineDisplayManagerEditor`、`OutlineTest`，并清理所有对这些类的引用。

#### Scenario: 旧系统代码完全移除
- **WHEN** 重构完成后编译项目
- **THEN** 不存在对 `ChessOutlineController`、`OutlineDisplayManager`、`OutlineTest` 的任何引用，编译无错误

### Requirement: ChessEntity 集成 OutlineController
`ChessEntity` SHALL 在初始化时获取或添加 `OutlineController` 组件，并通过 `OutlineController` 属性暴露。

#### Scenario: 棋子初始化后可访问 OutlineController
- **WHEN** ChessEntity.Initialize() 执行完成
- **THEN** `entity.OutlineController` 不为 null，类型为 `OutlineController`

### Requirement: 选中棋子显示黄色描边
`ChessSelectionManager` 选中棋子时 SHALL 调用 `OutlineController.ShowOutline` 显示黄色描边，取消选中时 SHALL 调用 `HideOutline`。

#### Scenario: 左键点击选中棋子
- **WHEN** 玩家左键点击一个玩家阵营棋子
- **THEN** 该棋子显示黄色描边

#### Scenario: 取消选中棋子
- **WHEN** 已选中棋子后，玩家左键点击空地或右键取消
- **THEN** 之前选中棋子的描边被移除

#### Scenario: 切换选中目标
- **WHEN** 已选中棋子 A，玩家点击棋子 B
- **THEN** 棋子 A 描边移除，棋子 B 显示黄色描边
