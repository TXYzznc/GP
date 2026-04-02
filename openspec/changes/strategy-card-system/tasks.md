## 1. 数据结构与管理器

- [x] 1.1 创建 CardData 数据类，包含 CardId、CardTable.Row 引用、运行时状态
- [x] 1.2 创建 CardManager 单例类，维护 List<CardData> 卡牌列表
- [x] 1.3 实现 CardManager.GetAvailableCards() 方法
- [x] 1.4 实现 CardManager.RemoveCard(cardId) 方法
- [x] 1.5 实现 CardManager.HasCard(cardId) 方法
- [x] 1.6 添加 CardManager 事件：OnCardAdded、OnCardRemoved
- [x] 1.7 添加 CardManager.CurrentSelectedCard 属性，记录当前选中卡牌
- [x] 1.8 实现 CardManager 战斗开始时初始化逻辑（随机加载 8 张卡）
- [x] 1.9 实现 CardManager 战斗结束时清理逻辑

## 2. UI 卡槽显示

- [x] 2.1 扩展 CombatUI.RefreshCardSlots() 方法，从 CardManager 获取数据
- [x] 2.2 实现 CardSlotItem.SetData(CardData) 方法，绑定卡牌数据
- [x] 2.3 使用 ResourceExtension.LoadSpriteAsync() 加载卡牌图标到 Btn 的 Image 组件
- [x] 2.4 显示卡牌名称、描述、灵力消耗
- [x] 2.5 CombatUI 监听 CardManager.OnCardRemoved 事件，自动刷新卡槽
- [x] 2.6 实现卡牌使用后的销毁动画（DOTween 淡出）

## 3. 卡牌选中交互

- [x] 3.1 CardSlotItem 添加 isSelected 状态字段
- [x] 3.2 实现 OnCardClicked() 方法，处理左键点击事件
- [x] 3.3 点击未选中卡牌：设为选中状态，使用 DOTween 向上移动 20 单位
- [x] 3.4 点击已选中卡牌：取消选中状态，使用 DOTween 恢复原位置
- [x] 3.5 选中卡牌时调用 CombatUI.varDetailInfoUI.SetData(cardData)
- [x] 3.6 选中卡牌时调用 CombatUI.varDetailInfoUI.RefreshUI() 并显示 DetailInfoUI
- [x] 3.7 取消选中时清除 DetailInfoUI 数据并隐藏
- [x] 3.8 实现单选逻辑：选中新卡牌时自动取消之前选中的卡牌

## 4. 拖拽交互系统

- [x] 4.1 CardSlotItem 实现 IBeginDragHandler 接口
- [x] 4.2 OnBeginDrag：取消选中状态（如果已选中），隐藏 DetailInfoUI
- [x] 4.3 OnBeginDrag：创建拖拽预览对象（半透明卡牌图标）
- [x] 4.4 CardSlotItem 实现 IDragHandler 接口
- [x] 4.5 OnDrag：更新拖拽预览位置，跟随鼠标
- [x] 4.6 OnDrag：执行射线检测（限制频率为 0.1 秒/次）
- [x] 4.7 OnDrag：检测鼠标是否在 varCardSlotAdsorptionArea 区域
- [x] 4.8 CardSlotItem 实现 IEndDragHandler 接口
- [x] 4.9 OnEndDrag：判断释放位置（战场/吸附区域/无效区域）
- [x] 4.10 OnEndDrag：战场区域释放时执行卡牌效果
- [x] 4.11 OnEndDrag：吸附区域或无效区域释放时返回卡槽

## 5. 范围预览系统

- [x] 5.1 创建范围预览预制体（包含 Projector 组件和黄色材质）
- [x] 5.2 创建 CardRangePreview 类，管理范围预览对象
- [x] 5.3 实现范围预览对象池（初始化 2 个实例）
- [x] 5.4 实现 ShowPreview(Vector3 position, float radius) 方法
- [x] 5.5 实现 HidePreview() 方法
- [x] 5.6 根据 CardTable.AreaRadius 动态调整投影大小
- [x] 5.7 支持特殊范围类型（单体目标显示小型指示器）
- [x] 5.8 实现卡槽吸附区域提示（显示/隐藏 varCardSlotAdsorptionArea）

## 6. 效果执行系统

- [x] 6.1 创建 ICardEffect 接口，定义 Init() 和 Execute() 方法
- [x] 6.2 创建 CardEffectExecutor 类，根据 CardId 创建效果实例
- [x] 6.3 实现目标选择逻辑（根据 TargetType 自动选择目标）
- [x] 6.4 实现伤害效果执行（支持物理/魔法/真实伤害）
- [x] 6.5 实现 Buff 效果执行（解析 InstantBuffs 和 HitBuffs）
- [x] 6.6 实现特效播放（加载 EffectId 和 HitEffectId）
- [x] 6.7 实现 ParamsConfig JSON 解析（使用 JsonUtility）
- [x] 6.8 添加异常捕获和日志输出（DebugEx.Error）

## 7. 独立卡牌效果脚本

- [x] 7.1 创建 HolyShieldCardEffect（神圣庇护，ID=1001）
- [x] 7.2 创建 FlameStormCardEffect（烈焰风暴，ID=1002）
- [x] 7.3 创建 TimeRewindCardEffect（时间回溯，ID=1003）
- [x] 7.4 创建 WarCryCardEffect（战争号角，ID=1004）
- [x] 7.5 创建 ShadowAssaultCardEffect（暗影突袭，ID=1005）
- [x] 7.6 创建 LifeDrainCardEffect（生命汲取，ID=1006）
- [x] 7.7 创建 FrostNovaCardEffect（冰霜新星，ID=1007）
- [x] 7.8 创建 BerserkCardEffect（狂暴，ID=1008）
- [x] 7.9 创建 GroupHealCardEffect（群体治疗，ID=1009）
- [x] 7.10 创建 ThunderStrikeCardEffect（雷霆一击，ID=1010）
- [x] 7.11 创建 ChaosCurseCardEffect（混乱诅咒，ID=1011）
- [x] 7.12 创建 ResurrectionCardEffect（不屈意志，ID=1012）

## 8. 集成与测试

- [x] 8.1 在 CombatUI.OnCombatEnter 中初始化 CardManager
- [x] 8.2 在 CombatUI.OnCombatLeave 中清理 CardManager
- [x] 8.3 测试卡牌选中交互（点击选中/取消选中，DetailInfoUI 显示/隐藏）
- [ ] 8.4 测试卡牌拖拽交互（拖拽、预览、释放）
- [ ] 8.5 测试范围预览显示（战场区域、吸附区域、无效区域）
- [ ] 8.6 测试卡牌效果执行（伤害、治疗、Buff）
- [ ] 8.7 测试卡牌使用后销毁和列表更新
- [ ] 8.8 测试特效播放和销毁
- [ ] 8.9 测试 JSON 配置解析和异常处理
- [ ] 8.10 性能测试（射线检测频率、对象池效果）
- [ ] 8.11 测试 DetailInfoUI 与卡牌选中的联动
- [ ] 8.12 完整战斗流程测试（开始→选中卡牌→拖拽使用→结束）

## 9. 动效增强（中高优先级）

- [x] 9.1 卡牌悬停缩放（1.05x，0.2s）
- [x] 9.2 卡牌点击脉冲（1.0 → 1.1 → 1.0）
- [x] 9.3 拖拽时卡牌变暗（透明度 0.5）
- [x] 9.4 拖拽预览旋转（±5°，循环）
- [x] 9.5 范围圈脉冲（1.0 → 1.05 → 1.0，循环）
- [x] 9.6 范围圈淡入淡出（0.2s）

## 10. 动效增强（低优先级）

- [x] 10.1 卡牌使用闪光（白色闪烁 0.2s）
- [x] 10.2 吸附区域高亮（拖拽进入时增加透明度）
- [x] 10.3 卡牌移除缩小消失（0.3s 缩小到 0.5x + 淡出）
- [x] 10.4 卡槽重排动画（从左侧滑入 0.3s，错开 0.05s）
