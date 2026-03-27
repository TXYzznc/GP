## MODIFIED Requirements

### Requirement: CombatTriggerManager 不直接依赖 UI 层
`CombatTriggerManager` SHALL NOT 持有或调用任何 UI 类的引用（包括 `CombatPreparationUI` 及其他 UIForm）。
所有需要通知 UI 的时机 SHALL 通过 `CombatTriggerEvents` 静态事件类发布。

#### Scenario: 敌方先手触发时通知 UI
- **WHEN** `CombatTriggerManager` 检测到敌方先手并确定效果 ID
- **THEN** SHALL 调用 `CombatTriggerEvents.OnEnemyInitiativeTriggered?.Invoke(effectId)`
- **THEN** SHALL NOT 直接获取或调用 `CombatPreparationUI` 的任何方法

#### Scenario: 偷袭触发时通知 UI
- **WHEN** `CombatTriggerManager` 检测到偷袭并生成可选 Debuff 池
- **THEN** SHALL 调用 `CombatTriggerEvents.OnSneakAttackTriggered?.Invoke(debuffPool)`

#### Scenario: UI 订阅触发事件
- **WHEN** `CombatPreparationUI` 初始化时
- **THEN** SHALL 订阅 `CombatTriggerEvents.OnEnemyInitiativeTriggered` 并实现对应显示逻辑

### Requirement: CombatTrigger 相关文件位于 Combat/Trigger/ 目录
以下文件 SHALL 位于 `Assets/AAAGame/Scripts/Game/Combat/Trigger/` 目录下：
- `CombatTriggerManager.cs`
- `CombatTriggerType.cs`
- `CombatTriggerContext.cs`
- `CombatOpportunityDetector.cs`
- `CombatTriggerEvents.cs`（新增）

#### Scenario: 文件位置验证
- **WHEN** 在项目中查找 `CombatTriggerManager.cs`
- **THEN** 文件路径 SHALL 为 `Game/Combat/Trigger/CombatTriggerManager.cs`
- **THEN** 文件路径 SHALL NOT 包含 `Explore/`

### Requirement: CombatTriggerEvents 静态事件类
系统 SHALL 提供 `CombatTriggerEvents` 静态类，定义以下事件：
- `OnEnemyInitiativeTriggered: Action<int>` — 参数为 effectId
- `OnSneakAttackTriggered: Action<List<int>>` — 参数为可选 Debuff ID 列表
- `OnCombatContextCleared: Action`

#### Scenario: 事件安全调用
- **WHEN** 无订阅者时触发事件
- **THEN** SHALL 使用 `?.Invoke()` 模式，不抛出 NullReferenceException
