## 1. 收集变更信息

- [ ] 1.1 确认 DataTableGenerator 已重新生成所有配置表 .bytes 文件（AffixTable / BuffTable / SpecialEffectTable / SummonChessTable / SynergyTable）
- [ ] 1.2 确认项目可在 Unity Editor Play Mode 下正常编译运行
- [ ] 1.3 打开 CombatSimulatorPanel 确认面板可正常加载

## 2. Buff 伤害来源追踪测试

- [ ] 2.1 放置一个带 BleedBuff 的棋子，进入战斗，观察 DoT tick 日志，确认 casterAttribute 非 null
- [ ] 2.2 放置带 BurnBuff 的棋子（叠 3 层），确认伤害值 = damagePerStack × 3，飘字显示火焰伤害
- [ ] 2.3 放置带 PoisonBuff 的棋子，确认伤害 = MaxHp × ratio，飘字显示毒伤害
- [ ] 2.4 令施法者在 Buff tick 前死亡，确认不抛出 NullReferenceException
- [ ] 2.5 放置带 ReflectDamageBuff 的棋子，令敌方攻击，确认反伤飘字出现，且不触发二次反弹

## 3. 动态战场阵营测试

- [ ] 3.1 在 Editor 中调用 AllocateBattlefield() 两次，日志确认第一次返回(100,101)，第二次返回(102,103)
- [ ] 3.2 确认 IsEnemy(100,101)==true，IsEnemy(100,103)==false（跨战场不互敌）
- [ ] 3.3 调用 ReleaseAllBattlefields() 后再次 AllocateBattlefield()，确认从(100,101)重新开始
- [ ] 3.4 调用 ClearCustomRelations() 后验证 IsEnemy(Player,Enemy)==true，IsEnemy(Neutral,Player)==false
- [ ] 3.5 在场上放置邪灵，大招时放一个同阵营友方单位，确认友方不受伤害

## 4. 战斗结束投射物清理测试

- [ ] 4.1 构造场景：远程棋子发射投射物飞行中手动触发 EndBattle（可用 SimulatorPanel 或 Debug 按钮）
- [ ] 4.2 确认日志输出"已销毁 N 个飞行中的投射物"
- [ ] 4.3 确认战斗结束后场景中无残留 ChessProjectile 对象（Hierarchy 检查）
- [ ] 4.4 构造无投射物的战斗结束场景，确认 EndBattle 正常完成无报错

## 5. HpFloor 锁血机制测试

- [ ] 5.1 通过脚本设置棋子 HpFloor=1，令其受到超过当前 HP 的伤害，确认 CurrentHp==1，IsDead==false
- [ ] 5.2 不设置 HpFloor（默认 0），令棋子受致命伤害，确认正常死亡（IsDead==true）
- [ ] 5.3 设置 HpFloor=-10，确认 HpFloor 被修正为 0
- [ ] 5.4 HpFloor=100 时，对棋子施加治疗，确认 CurrentHp 上限为 MaxHp 而非无限增加

## 6. 装备特效绑定测试

- [ ] 6.1 穿戴 SpecialEffectId > 0 的装备，确认 SpecialEffectManager 对应效果已应用（日志或属性变化验证）
- [ ] 6.2 穿戴 SpecialEffectId=0 的装备，确认 SpecialEffectManager 不被多余调用
- [ ] 6.3 卸下步骤 6.1 的装备，确认特效被正确移除（属性/效果回退验证）
- [ ] 6.4 在配置 BaseAttributes 中写 "MagicPower": 50，确认装备穿戴后 SpellPower 正确增加 50
- [ ] 6.5 写 "SpellPower": 50，确认同样效果，验证新旧格式兼容

## 7. 羁绊动态刷新测试

- [ ] 7.1 满足羁绊 A 条件（上棋），确认 OnSynergyStateChanged(id, true) 触发，效果应用
- [ ] 7.2 在羁绊 A 已激活的情况下再上一个同羁绊棋子，确认效果不重复叠加（先移除再应用）
- [ ] 7.3 下掉棋子使羁绊 A 条件不满足，确认 OnSynergyStateChanged(id, false) 触发，效果移除
- [ ] 7.4 快速连续上下棋子（≥ 3 次），确认羁绊效果数值与实际棋子数匹配，无叠加异常

## 8. 宝物词条自动生成测试

- [ ] 8.1 通过 ItemManager.CreateItem(treasureId) 创建宝物，确认 TreasureItem.Affixes 不为 null
- [ ] 8.2 对比 Rarity=1 和 Rarity=3 的宝物词条数量，确认高品质词条数 >= 低品质
- [ ] 8.3 检查生成词条的 AttributeType 在 AffixTable 中存在，数值在合理范围
- [ ] 8.4 调用 ItemManager.GetAllAffixData()，确认返回列表 Count > 0
- [ ] 8.5 验证宝物创建时不抛出异常（AffixGenerator 配置正确加载）

## 9. GameEffectService 兼容层测试

- [ ] 9.1 通过 GameEffectService.Execute(effectId, context) 触发单目标效果，确认 SpecialEffectManager 被调用且效果生效
- [ ] 9.2 多目标效果验证：context.Targets 含 2+ 个目标，确认所有目标均受到效果
- [ ] 9.3 传入 context=null，确认返回 false 且不抛出异常
- [ ] 9.4 EffectSource.Item → EffectSourceType.Consumable 映射验证（日志确认）
- [ ] 9.5 EffectSource.Synergy → EffectSourceType.Synergy 映射验证

## 10. 配置表回归测试

- [ ] 10.1 进入战斗，确认 ResourceConfigTable 加载正常（资源不再报"找不到资源"错误）
- [ ] 10.2 验证 SummonChessTable 中棋子配置数据完整（名称/技能/阵营字段非空）
- [ ] 10.3 验证 SynergyTable 种族/职业条件读取正确（Type=1 按种族，Type=3 按职业）
- [ ] 10.4 验证 SpecialEffectTable 中 EffectParams 字段正常解析（无旧版 BuffIds 兼容层报错）
