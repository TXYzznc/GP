## 1. 数据结构与管理器

- [ ] 1.1 创建 CardData 数据类，包含 CardId、CardTable.Row 引用、运行时状态
- [ ] 1.2 创建 CardManager 单例类，维护 List<CardData> 卡牌列表
- [ ] 1.3 实现 CardManager.GetAvailableCards() 方法
- [ ] 1.4 实现 CardManager.RemoveCard(cardId) 方法
- [ ] 1.5 实现 CardManager.HasCard(cardId) 方法
- [ ] 1.6 添加 CardManager 事件：OnCardAdded、OnCardRemoved
- [ ] 1.7 实现 CardManager 战斗开始时初始化逻辑（随机加载 8 张卡）
- [ ] 1.8 实现 CardManager 战斗结束时清理逻辑

## 2. UI 卡槽显示

- [ ] 2.1 扩展 CombatUI.RefreshCardSlots() 方法，从 CardManager 获取数据
- [ ] 2.2 实现 CardSlotItem.SetData(CardData) 方法，绑定卡牌数据
- [ ] 2.3 使用 ResourceExtension.LoadSpriteAsync() 加载卡牌图标
- [ ] 2.4 显示卡牌名称、描述、灵力消耗
- [ ] 2.5 CombatUI 监听 CardManager.OnCardRemoved 事件，自动刷新卡槽
- [ ] 2.6 实现卡牌使用后的销毁动画（DOTween 淡出）

## 3. 拖拽交互系统

- [ ] 3.1 CardSlotItem 实现 IBeginDragHandler 接口
- [ ] 3.2 OnBeginDrag：创建拖拽预览对象（半透明卡牌图标）
- [ ] 3.3 CardSlotItem 实现 IDragHandler 接口
- [ ] 3.4 OnDrag：更新拖拽预览位置，跟随鼠标
- [ ] 3.5 OnDrag：执行射线检测（限制频率为 0.1 秒/次）
- [ ] 3.6 OnDrag：检测鼠标是否在 varCardSlotAdsorptionArea 区域
- [ ] 3.7 CardSlotItem 实现 IEndDragHandler 接口
- [ ] 3.8 OnEndDrag：判断释放位置（战场/吸附区域/无效区域）
- [ ] 3.9 OnEndDrag：战场区域释放时执行卡牌效果
- [ ] 3.10 OnEndDrag：吸附区域或无效区域释放时返回卡槽

## 4. 范围预览系统

- [ ] 4.1 创建范围预览预制体（包含 Projector 组件和黄色材质）
- [ ] 4.2 创建 CardRangePreview 类，管理范围预览对象
- [ ] 4.3 实现范围预览对象池（初始化 2 个实例）
- [ ] 4.4 实现 ShowPreview(Vector3 position, float radius) 方法
- [ ] 4.5 实现 HidePreview() 方法
- [ ] 4.6 根据 CardTable.AreaRadius 动态调整投影大小
- [ ] 4.7 支持特殊范围类型（单体目标显示小型指示器）
- [ ] 4.8 实现卡槽吸附区域提示（显示/隐藏 varCardSlotAdsorptionArea）

## 5. 效果执行系统

- [ ] 5.1 创建 ICardEffect 接口，定义 Init() 和 Execute() 方法
- [ ] 5.2 创建 CardEffectExecutor 类，根据 CardId 创建效果实例
- [ ] 5.3 实现目标选择逻辑（根据 TargetType 自动选择目标）
- [ ] 5.4 实现伤害效果执行（支持物理/魔法/真实伤害）
- [ ] 5.5 实现 Buff 效果执行（解析 InstantBuffs 和 HitBuffs）
- [ ] 5.6 实现特效播放（加载 EffectId 和 HitEffectId）
- [ ] 5.7 实现 ParamsConfig JSON 解析（使用 JsonUtility）
- [ ] 5.8 添加异常捕获和日志输出（DebugEx.Error）

## 6. 独立卡牌效果脚本

- [ ] 6.1 创建 HolyShieldCardEffect（神圣庇护，ID=1001）
- [ ] 6.2 创建 FlameStormCardEffect（烈焰风暴，ID=1002）
- [ ] 6.3 创建 TimeRewindCardEffect（时间回溯，ID=1003）
- [ ] 6.4 创建 WarCryCardEffect（战争号角，ID=1004）
- [ ] 6.5 创建 ShadowAssaultCardEffect（暗影突袭，ID=1005）
- [ ] 6.6 创建 LifeDrainCardEffect（生命汲取，ID=1006）
- [ ] 6.7 创建 FrostNovaCardEffect（冰霜新星，ID=1007）
- [ ] 6.8 创建 BerserkCardEffect（狂暴，ID=1008）
- [ ] 6.9 创建 GroupHealCardEffect（群体治疗，ID=1009）
- [ ] 6.10 创建 ThunderStrikeCardEffect（雷霆一击，ID=1010）
- [ ] 6.11 创建 ChaosCurseCardEffect（混乱诅咒，ID=1011）
- [ ] 6.12 创建 ResurrectionCardEffect（不屈意志，ID=1012）

## 7. 集成与测试

- [ ] 7.1 在 CombatUI.OnCombatEnter 中初始化 CardManager
- [ ] 7.2 在 CombatUI.OnCombatLeave 中清理 CardManager
- [ ] 7.3 测试卡牌拖拽交互（拖拽、预览、释放）
- [ ] 7.4 测试范围预览显示（战场区域、吸附区域、无效区域）
- [ ] 7.5 测试卡牌效果执行（伤害、治疗、Buff）
- [ ] 7.6 测试卡牌使用后销毁和列表更新
- [ ] 7.7 测试特效播放和销毁
- [ ] 7.8 测试 JSON 配置解析和异常处理
- [ ] 7.9 性能测试（射线检测频率、对象池效果）
- [ ] 7.10 完整战斗流程测试（开始→使用卡牌→结束）
