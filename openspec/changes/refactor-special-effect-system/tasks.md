## 1. 前置配置表更新

- [x] 1.1 SpecialEffectTable EffectType 列已存在，重新定义含义（1=Instant/2=Buff/3=Passive/4=Trigger）
- [x] 1.2 AttributeModifiers 放入 EffectParams JSON 中（无需新增列）
- [x] 1.3 已更新 SpecialEffectTable.txt：消耗品标 1，战斗Buff标 2
- [x] 1.4 已为所有装备/宝物新增 Passive 效果行（ID 3001~3003, 4001~4003, 5001~5006, 6001~6006）
- [x] 1.5 EquipmentTable 已有 SpecialEffectId 字段，指向 SpecialEffectTable
- [x] 1.6 TreasureTable 已有 SpecialEffectId 字段，指向 SpecialEffectTable
- [x] 1.7 SynergyTable.EffectId 已更新（3004, 5101, 5102）
- [ ] 1.8 用户运行 DataTableGenerator 重新生成（SpecialEffectTable.cs 列结构未变，可能不需要）

## 2. SpecialEffectInstance 类层次结构

- [x] 2.1 新建 SpecialEffectInstance 抽象基类（Apply/Remove/IsActive）
- [x] 2.2 新建 InstantEffect 子类（调用 ItemEffectFactory）
- [x] 2.3 新建 BuffSpecialEffect 子类（BuffManager.AddBuff/RemoveBuff）
- [x] 2.4 新建 PassiveEffect 子类（直接修改 ChessAttribute）
- [x] 2.5 新建 TriggerEffect 子类（预留框架）
- [x] 2.6 工厂逻辑内置到 SpecialEffectManager.CreateInstance()

## 3. SpecialEffectManager 效果生命周期管理

- [x] 3.1 新建 SpecialEffectManager 类
- [x] 3.2 实现 ApplyEffect(effectId, target, sourceType, sourceId)
- [x] 3.3 实现 RemoveEffectBySource(target, sourceType, sourceId)
- [x] 3.4 实现活跃效果字典（EffectKey struct with proper equality）
- [x] 3.5 实现 GetActiveEffects(target)
- [x] 3.6 定义 EffectSourceType 枚举（在 ItemEnums.cs 中）

## 4. ItemManager 效果表加载逻辑

- [x] 4.1 ItemManager 已加载 SpecialEffectTable（LoadSpecialEffectTable 方法）
- [x] 4.2 GetSpecialEffectData(effectId) 可用
- [x] 4.3 SpecialEffectManager 通过 ItemManager.GetSpecialEffectData() 获取配置

## 5. 装备效果集成（ChessEquipmentManager）

- [x] 5.1 ApplyEquipmentStats() 中调用 SpecialEffectManager.ApplyEffect()
- [x] 5.2 RemoveEquipmentStats() 中调用 SpecialEffectManager.RemoveEffectBySource()
- [x] 5.3 BaseAttributes 逻辑独立保留
- [ ] 5.4 测试穿戴/卸下装备的 Passive 效果

## 6. 宝物效果集成

- [ ] 6.1 在宝物穿戴逻辑中：若 EffectId != 0，调用 SpecialEffectManager.ApplyEffect()
- [ ] 6.2 在宝物卸下逻辑中：调用 SpecialEffectManager.RemoveEffectBySource()
- [ ] 6.3 确保 BaseAttributes 逻辑不受影响
- [ ] 6.4 测试宝物穿戴/卸下的效果

## 7. 消耗品效果集成

- [x] 7.1 ConsumableItem.OnUse() 通过 GameEffectService → SpecialEffectManager 路径执行
- [x] 7.2 InstantEffect 内部正确调用 ItemEffectFactory
- [x] 7.3 与现有 ItemEffectFactory 完全兼容
- [ ] 7.4 测试消耗品使用流程

## 8. 战斗效果集成

- [x] 8.1 战斗效果通过 GameEffectService → SpecialEffectManager 路径执行（兼容层）
- [x] 8.2 CombatTriggerManager 无需修改（已通过 GameEffectService 兼容）
- [x] 8.3 与 BuffPool 机制兼容
- [ ] 8.4 CombatRuleTable 新增 InitiativeEffectId/SneakAttackEffectId（后续优化）
- [ ] 8.5 测试先手/偷袭效果

## 9. 羁绊效果集成

- [x] 9.1 SynergyManager.ActivateSynergy() 改用 SpecialEffectManager.ApplyEffect()
- [x] 9.2 SynergyManager.DeactivateSynergy() 改用 SpecialEffectManager.RemoveEffectBySource()
- [x] 9.3 多重羁绊通过不同 synergyId 作为 sourceId 互不干扰
- [ ] 9.4 测试羁绊激活和解除
- [ ] 9.5 测试宝物 Passive + 羁绊 Buff 叠加

## 10. GameEffectService 兼容层

- [x] 10.1 GameEffectService.Execute() 委托给 SpecialEffectManager
- [x] 10.2 公开接口不变，向后兼容
- [ ] 10.3 后续逐步将调用方迁移到直接使用 SpecialEffectManager

## 11. 测试与验证

- [ ] 11.1 编译通过
- [ ] 11.2 测试 Passive 效果
- [ ] 11.3 测试多件装备叠加
- [ ] 11.4 测试 Buff 效果（羁绊）
- [ ] 11.5 测试 Instant 效果（消耗品）
- [ ] 11.6 测试战斗效果
- [ ] 11.7 测试综合叠加场景
- [ ] 11.8 完整游戏流程验证

## 12. 待完成

- [ ] 12.1 宝物穿戴逻辑集成 SpecialEffectManager（需找到宝物穿戴代码）
- [ ] 12.2 羁绊 Buff 实际配置（BuffTable 中创建三神器/月神/炎魔的羁绊 Buff）
- [ ] 12.3 SynergyManager 中移除废弃的 m_SynergyBuffMapping 字段
- [ ] 12.4 更新项目知识库文档
