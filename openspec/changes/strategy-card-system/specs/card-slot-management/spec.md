## ADDED Requirements

### Requirement: CardManager 单例管理
系统 SHALL 提供 CardManager 单例类，管理当前战斗中的策略卡数据。

#### Scenario: 单例访问
- **WHEN** 任何系统需要访问卡牌数据时
- **THEN** 通过 CardManager.Instance 获取单例实例

#### Scenario: 战斗开始时初始化
- **WHEN** 战斗进入时（CombatEnterEventArgs）
- **THEN** CardManager 初始化并加载 8 张随机策略卡

### Requirement: 卡牌数据结构
CardManager SHALL 维护 List<CardData> 存储当前可用卡牌。

#### Scenario: CardData 包含完整配置
- **WHEN** 创建 CardData 实例时
- **THEN** CardData 包含 CardId、CardTable.Row 引用、以及运行时状态

#### Scenario: 从 CardTable 加载数据
- **WHEN** CardManager 初始化卡牌时
- **THEN** 从 CardTable 读取配置数据填充 CardData

### Requirement: 卡牌列表管理
CardManager SHALL 提供添加、移除、查询卡牌的方法。

#### Scenario: 获取所有可用卡牌
- **WHEN** CombatUI 需要刷新卡槽时
- **THEN** 调用 CardManager.GetAvailableCards() 获取卡牌列表

#### Scenario: 移除已使用卡牌
- **WHEN** 玩家使用卡牌后
- **THEN** 调用 CardManager.RemoveCard(cardId) 从列表中移除

#### Scenario: 查询卡牌是否存在
- **WHEN** 需要验证卡牌是否可用时
- **THEN** 调用 CardManager.HasCard(cardId) 返回 bool 结果

### Requirement: 卡牌变化事件
CardManager SHALL 提供事件通知卡牌列表变化。

#### Scenario: 卡牌移除事件
- **WHEN** CardManager.RemoveCard() 被调用
- **THEN** 触发 OnCardRemoved 事件，传递被移除的 CardId

#### Scenario: 卡牌添加事件
- **WHEN** CardManager.AddCard() 被调用
- **THEN** 触发 OnCardAdded 事件，传递新增的 CardData

#### Scenario: CombatUI 监听事件刷新
- **WHEN** CardManager 触发 OnCardRemoved 事件
- **THEN** CombatUI 监听事件并调用 RefreshCardSlots() 刷新显示

### Requirement: CombatUI 卡槽刷新
CombatUI SHALL 扩展 RefreshCardSlots() 方法，从 CardManager 获取数据并创建 UI。

#### Scenario: 战斗开始时刷新卡槽
- **WHEN** CombatUI 收到 CombatEnterEventArgs 事件
- **THEN** 调用 RefreshCardSlots() 创建 8 个 CardSlotItem

#### Scenario: 清理旧卡槽
- **WHEN** RefreshCardSlots() 被调用时
- **THEN** 先销毁 varCardSlots 下的所有子对象（除了 varCardSlotItem 模板）

#### Scenario: 创建新卡槽
- **WHEN** RefreshCardSlots() 遍历 CardManager.GetAvailableCards()
- **THEN** 为每张卡实例化 varCardSlotItem，调用 SetData(cardData) 设置数据

### Requirement: CardSlotItem 数据绑定
CardSlotItem SHALL 扩展 SetData() 方法，显示卡牌信息。

#### Scenario: 显示卡牌图标
- **WHEN** CardSlotItem.SetData(cardData) 被调用
- **THEN** 使用 ResourceExtension.LoadSpriteAsync(cardData.IconId, varCardIcon) 加载图标

#### Scenario: 显示卡牌名称
- **WHEN** CardSlotItem.SetData(cardData) 被调用
- **THEN** 设置 varCardName.text = cardData.Name

#### Scenario: 显示卡牌描述
- **WHEN** CardSlotItem.SetData(cardData) 被调用
- **THEN** 设置 varCardDesc.text = cardData.Desc

#### Scenario: 显示灵力消耗
- **WHEN** CardSlotItem.SetData(cardData) 被调用
- **THEN** 设置 varSpiritCost.text = cardData.SpiritCost.ToString()

### Requirement: 卡牌使用后销毁
CardSlotItem SHALL 在卡牌使用后从 UI 中移除。

#### Scenario: 使用后销毁 UI
- **WHEN** 卡牌效果执行完成
- **THEN** 调用 Destroy(gameObject) 销毁 CardSlotItem 对象

#### Scenario: 播放销毁动画
- **WHEN** 卡牌使用后准备销毁时
- **THEN** 使用 DOTween 播放淡出动画，动画完成后销毁对象

### Requirement: 战斗结束时清理
CardManager SHALL 在战斗结束时清理卡牌数据。

#### Scenario: 战斗离开时清理
- **WHEN** 收到 CombatLeaveEventArgs 事件
- **THEN** CardManager 清空卡牌列表，重置状态

#### Scenario: 保存卡牌使用记录
- **WHEN** 战斗结束时
- **THEN** CardManager 将已使用卡牌记录保存到 CombatSessionData
