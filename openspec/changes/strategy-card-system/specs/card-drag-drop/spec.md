## ADDED Requirements

### Requirement: 卡牌拖拽开始
CardSlotItem 组件 SHALL 实现 IBeginDragHandler 接口，当玩家按下鼠标左键并开始拖动卡牌时触发拖拽开始事件。

#### Scenario: 玩家开始拖拽卡牌
- **WHEN** 玩家在 CardSlotItem 上按下鼠标左键并移动鼠标
- **THEN** 系统创建拖拽预览对象，显示卡牌图标和半透明效果

#### Scenario: 拖拽开始时显示范围预览
- **WHEN** 玩家开始拖拽卡牌
- **THEN** 系统根据 CardTable.AreaRadius 创建范围预览对象（Projector）

### Requirement: 拖拽过程中的实时反馈
CardSlotItem 组件 SHALL 实现 IDragHandler 接口，在拖拽过程中实时更新预览位置和状态。

#### Scenario: 拖拽到战场区域
- **WHEN** 玩家拖拽卡牌，鼠标射线检测到战场地面
- **THEN** 系统在射线命中点显示黄色范围预览圈，范围半径为 CardTable.AreaRadius

#### Scenario: 拖拽到卡槽吸附区域
- **WHEN** 玩家拖拽卡牌，鼠标位置在 varCardSlotAdsorptionArea 区域内
- **THEN** 系统显示 varCardSlotAdsorptionArea 的 Image 组件，提示可以返回卡槽

#### Scenario: 拖拽到无效区域
- **WHEN** 玩家拖拽卡牌，鼠标既不在战场区域也不在卡槽吸附区域
- **THEN** 系统隐藏范围预览圈和吸附区域提示

### Requirement: 拖拽结束判定
CardSlotItem 组件 SHALL 实现 IEndDragHandler 接口，根据释放位置执行不同操作。

#### Scenario: 在战场区域释放卡牌
- **WHEN** 玩家在战场区域松开鼠标
- **THEN** 系统执行卡牌效果，销毁卡牌 UI，从 CardManager 移除卡牌数据

#### Scenario: 在卡槽吸附区域释放卡牌
- **WHEN** 玩家在 varCardSlotAdsorptionArea 区域松开鼠标
- **THEN** 系统将卡牌返回原卡槽位置，播放回归动画

#### Scenario: 在无效区域释放卡牌
- **WHEN** 玩家在无效区域（既不在战场也不在吸附区域）松开鼠标
- **THEN** 系统将卡牌返回原卡槽位置，播放取消音效

### Requirement: 拖拽预览对象管理
系统 SHALL 在拖拽开始时创建预览对象，在拖拽结束时销毁预览对象。

#### Scenario: 创建拖拽预览
- **WHEN** 玩家开始拖拽卡牌
- **THEN** 系统创建包含卡牌图标的 UI 对象，跟随鼠标移动，透明度设为 0.6

#### Scenario: 销毁拖拽预览
- **WHEN** 玩家释放鼠标结束拖拽
- **THEN** 系统销毁拖拽预览对象和范围预览对象

### Requirement: 射线检测优化
系统 SHALL 限制射线检测频率，避免性能问题。

#### Scenario: 限制检测频率
- **WHEN** 玩家拖拽卡牌时
- **THEN** 系统每 0.1 秒执行一次射线检测，而非每帧检测

#### Scenario: 使用对象池管理预览对象
- **WHEN** 系统需要创建范围预览对象时
- **THEN** 系统从对象池获取预制体实例，使用完毕后回收到对象池
