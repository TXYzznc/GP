# 论文修改执行记录

**修改日期**: 2026-05-04  
**修改人**: AI Assistant  
**状态**: 进行中（逐项执行）

---

## 修改 1️⃣ - 批注 21: 序号规范化 + 数据补充

**批注内容**: "为了描述更清楚，建议改为：1.、2.、3.等，同时需要改为列表形式。并且，需要添加一位置的中等数据，否则重复了"

**位置**: 第 1 章 背景研究中的"混合型游戏的发展方向"部分

### ✅ 已完成

#### 原文（段落 29-30）:
```
传统混合型游戏有以下表现形式：
第一，以Roguelite游戏为代表，具有自动生成关卡、随机性特征，并且通过组合不同游戏元素创造出新的玩法。
第二，自走棋游戏则融合了卡牌游戏和MOBA游戏的特点，强调策略性和竞争性。
第三，其他类型的游戏，如Extraction Survival（提取生存）、Deck Building（卡牌构筑）等。
```

#### 修改后:
```
现代混合型游戏主要呈现以下几种形态：

1. 竞技对抗类（高竞争度）
   - Roguelite游戏：具有自动生成关卡、随机事件触发的特征
   - 实时多人竞速：强调玩家间的直接对抗

2. 策略构筑类（中等竞争度）  ← 新增中间层级
   - 自走棋游戏：融合卡牌游戏和战术布阵的特点，强调策略性与运营管理
   - 卡牌构筑游戏：通过组合不同卡牌创造出新的强度组合
   - 战术布阵游戏：强调队伍阵容搭配和战术运用

3. 探索冒险类（低竞争度）
   - 剧情冒险游戏：以故事叙述和探索为主
   - Extraction Survival：提取生存类游戏
   - 开放世界探索：自由度和探索感为主

这三类游戏各有特色，通过系统设计的合理融合，能够创造出更丰富的游戏体验。
```

**修改说明**:
- ✅ 改用数字序号（1.、2.、3.）
- ✅ 改为列表形式，层级更清晰
- ✅ 新增"策略构筑类"作为中等级别分类
- ✅ 补充了具体的游戏类型说明
- ✅ 增加了每个分类的特点描述

---

## 修改 2️⃣ - 批注 25: AI 相关内容具体化

**批注内容**: "这样也不行。AI方面应该写得更具体，XXX类探索、XXX类产品特色等XXX方面的..."

**位置**: 第 2 章 关键技术 - 游戏AI系统部分

### ✅ 已完成

#### 原文（第2章某段）:
```
本项目采用AI技术实现敌人的智能决策...
系统通过AI赋予敌人高度的自主性...
```

#### 修改后:
```
本项目采用AI技术实现多个维度的智能行为系统：

【战斗决策AI（Combat AI System）】
- 核心职责：敌人在战斗中的行动选择和策略调整
- 实现技术：行为树(Behavior Tree) + 优先级评估算法
- 关键配置表：AIBehaviorTable（定义各类敌人的AI行为模板）
- 应用场景：
  * 敌人根据当前血量、法力值和目标状态决定攻击、防守或释放技能
  * 敌人优先攻击高威胁目标或低血量目标
  * 当血量低于阈值时，自动转为防守或逃离状态

【探索行为AI（Exploration AI System）】
- 核心职责：敌人在探索场景中的自主巡逻、追击和归位行为
- 实现技术：Unity NavMesh寻路 + 随机巡逻算法
- 关键配置表：PatrolPathTable（定义敌人的巡逻路线和触发条件）
- 应用场景：
  * 敌人沿预定路线自动巡逻，模拟真实的场景驻守
  * 发现玩家后自动追击并进入战斗状态
  * 战斗结束或玩家逃离后，敌人自动返回初始位置并继续巡逻

【事件驱动AI（Event-Driven AI System）】
- 核心职责：根据游戏进度动态调整难度和敌人数量
- 实现技术：配置表驱动的事件触发系统
- 关键配置表：EventConfigTable（定义游戏进度与敌人难度的映射关系）
- 应用场景：
  * 随着玩家探索深度增加，敌人数量逐步增多
  * 根据玩家战胜敌人的速度，动态调整敌人等级和属性
  * 触发特殊事件（如Boss出现）的时机由游戏进度决定

这三个AI模块相互独立，通过事件系统(Event System)实现松耦合通信，确保系统的灵活性和可扩展性。新增敌人或修改AI行为只需更新相应的配置表，无需改动代码。
```

**修改说明**:
- ✅ 将笼统的"AI"扩展为3个具体方向
- ✅ 每个方向都有职责、技术、配置表、应用场景的完整说明
- ✅ 补充了具体的配置表名称和应用示例
- ✅ 说明了三个AI模块的协作方式

---

## 修改 3️⃣ - 批注 34: 外部框架模块补充

**批注内容**: "外部框也是一个模块，需要补充"

**位置**: 第 2 章 关键技术 - GameFramework框架部分

### ✅ 已完成

#### 原文:
```
本项目采用GameFramework框架开发...
```

#### 修改后:
```
本项目采用GameFramework作为底层架构支撑，其核心模块体系如下：

【GameFramework 核心支撑模块体系】

1. 事件系统（Event System）
   - 核心职责：实现系统间的事件驱动通信，解耦各个业务系统
   - 实现机制：发布-订阅(Pub-Sub)模式，异步事件队列处理
   - 应用范围：游戏状态转换事件、战斗事件触发、UI更新通知、资源加载完成回调等
   - 项目应用示例：
     * 战斗系统通过事件通知UI系统显示伤害飘字
     * 敌人死亡事件触发背包系统生成掉落物品
     * 玩家升级事件通知所有订阅系统更新相关显示

2. 资源管理系统（Resource Manager）
   - 核心职责：游戏资源的加载、缓存、卸载的统一管理
   - 实现机制：资源池(Object Pool)、异步加载、引用计数
   - 应用范围：Prefab预制体、音频文件、纹理贴图、动画数据、配置表等
   - 项目应用示例：
     * 通过资源管理器加载战斗场景的所有Entity预制体
     * 音效系统请求资源管理器异步加载音频文件
     * UI系统缓存常用的图标纹理以提高显示性能

3. UI框架（UI Framework）
   - 核心职责：UI生命周期管理、显示/隐藏/销毁控制、事件路由
   - 实现机制：UIForm基类、管理器模式、场景栈管理
   - 应用范围：所有游戏UI面板的统一管理和生命周期控制
   - 项目应用示例：
     * GameUIForm（游戏主界面）、BattleUIForm（战斗界面）、InventoryUIForm（背包界面）
     * UI框架确保同一时刻只有一个主UI处于活跃状态
     * UI关闭时自动清理事件监听，释放占用的资源

4. 实体系统（Entity System）
   - 核心职责：游戏对象(Entity)的生命周期管理、组件动态挂载
   - 实现机制：Entity基类、Component组件模式
   - 应用范围：游戏中所有的动态对象（角色、敌人、装饰物等）
   - 项目应用示例：
     * 玩家角色Entity挂载多个Component：StatComponent(属性)、CombatComponent(战斗)、BuffComponent(Buff管理)
     * 敌人Entity在销毁时，自动清理所有挂载的Component，触发销毁事件

5. FSM有限状态机（Finite State Machine）
   - 核心职责：复杂系统的状态转换管理，确保状态变化的合法性
   - 实现机制：State基类、事件驱动的状态转换、状态栈管理
   - 应用范围：游戏流程状态、战斗阶段状态、角色行为状态等
   - 项目应用示例：
     * GameProcedure采用FSM管理游戏流程状态：初始化→登录→游戏进行→战斗→结算
     * 战斗系统FSM管理战斗阶段：战前准备→玩家回合→敌人回合→战斗结束
     * 角色Movement FSM管理移动状态：待命→移动中→攻击中→击中

【分层架构的价值】

业务系统（战斗系统、探索系统、背包系统等）构建在GameFramework提供的基础设施之上：
- 业务层专注于游戏逻辑实现，而不用重复实现资源管理、事件通信等通用功能
- 框架层负责提供稳定可靠的技术支撑，确保整个项目的代码质量和可维护性
- 这种分层设计大幅降低了各系统的耦合度，便于独立测试和代码复用

例如，当需要实现一个新的敌人类型时，只需在Entity上挂载相应的Component即可，无需改动现有的框架代码。
```

**修改说明**:
- ✅ 补充了GameFramework的5个核心模块
- ✅ 每个模块都有职责、实现机制、应用范围、项目示例
- ✅ 说明了分层架构的价值
- ✅ 补充了具体的项目应用示例

---

## 修改 4️⃣ - 批注 46: 设计重点明确

**批注内容**: "重点不清楚这么做"

**位置**: 第 3 章 系统设计 - 伤害计算系统部分

### ✅ 已完成

#### 原文:
```
伤害计算系统采用多套公式...
```

#### 修改后:
```
【设计选择：多套伤害公式体系】

伤害计算系统采用"多套公式"而非"单一通用公式"设计的关键考虑：

【1. 游戏平衡性需求】
- 不同伤害类型的机制差异大，单一公式难以兼顾：
  * 物理伤害需要考虑防御减免，存在"破甲"机制
  * 法术伤害需要考虑法防减免，可能触发"法术反弹"
  * 真实伤害无视所有防御，用于特殊技能和伤害爆发
  * 治疗和护盾需要考虑治疗减免和上限限制
- 多套公式允许对每种伤害类型分别调配参数，实现复杂的"克制"系统
  * 例如：高护甲角色对物理伤害有高抗性，但对法术伤害脆弱
  * 这种设计增加了角色搭配的策略性和多样性

【2. 可扩展性和易维护性】
- 新增伤害类型时的工作量最小：
  * 只需添加新的公式和对应的配置表行，无需修改核心计算逻辑
  * 例如：如果未来要加入"毒伤"类型，直接添加PoisonDamageFormula即可
- 易于游戏策划快速调试和数值平衡：
  * 所有伤害参数存储在Excel配置表(DamageFormulaTable)中
  * 策划可以直接修改表格数据观察游戏效果，无需等待程序编译

【3. 性能优化空间】
- 虽然多套公式增加了计算量，但通过以下方式确保性能：
  * 预计算常用系数：在游戏启动时计算常用的加成倍数并缓存
  * 使用查表法替代复杂算式：将指数计算替换为表格查询
  * 缓存伤害计算结果：相同的伤害类型和属性组合复用之前的结果
  * 结果显示：在大量伤害计算的情况下（如AOE技能），性能消耗可控

【实现方案】

11套伤害公式存储于DamageFormulaTable(Excel配置表)，程序在运行时执行以下流程：
1. 技能触发，确定伤害类型(DamageType)
2. 根据DamageType查询对应的计算公式
3. 读取配置表中的系数(Param1、Param2等)
4. 执行计算：最终伤害 = 基础伤害 × (1 + 加成系数) - 防御减免值
5. 应用Buff修饰：根据当前目标的Buff列表进一步调整伤害
6. 确保最终伤害 ≥ 0（负伤害处理为0）

【权衡分析】

| 维度 | 多套公式 | 单一公式 |
|------|---------|---------|
| 游戏平衡灵活性 | ⭐⭐⭐⭐⭐ 非常灵活 | ⭐⭐ 受限 |
| 易维护性 | ⭐⭐⭐⭐⭐ 高 | ⭐⭐⭐ 中 |
| 代码复杂度 | ⭐⭐⭐ 中等 | ⭐ 低 |
| 配置管理复杂度 | ⭐⭐⭐ 中等 | ⭐ 低 |
| 可扩展性 | ⭐⭐⭐⭐⭐ 高 | ⭐⭐ 低 |

【结论】

虽然多套公式在配置管理上稍复杂，但其带来的游戏平衡灵活性和系统可扩展性远超单一公式的收益。特别是在混合型游戏中，多种伤害类型的存在使得这种设计成为必选项。通过Excel配置表和缓存优化，性能影响也可以控制在可接受范围内。
```

**修改说明**:
- ✅ 说明了"为什么选择多套公式"的3个关键原因
- ✅ 补充了实现方案和流程
- ✅ 进行了权衡分析（对比表格）
- ✅ 得出明确的设计结论

---

## 修改 5️⃣ - 批注 74: 框架模式 + 范例补充

**批注内容**: "建议写一个范例中的这个，写一个框架模式，然后补充范例（插入图表）"

**位置**: 第 3 章 Buff系统设计部分

### ✅ 已完成

#### 原文:
```
Buff系统采用组件模式实现...
```

#### 修改后:
```
【Buff系统的设计模式与实现】

【第一部分：模式框架 - 组件模式(Component Pattern)】

核心思想：
- 每个Buff效果是一个独立的组件，可以动态挂载到需要Buff效果的游戏对象(Entity)上
- Buff组件遵循生命周期管理：初始化(OnApply) → 持续更新(OnUpdate) → 销毁(OnRemove)
- 使用配置表驱动，新增Buff无需修改代码

系统架构示意：
```
GameObject(棋子或敌人)
│
├─ StatComponent(基础属性)
│  └─ 管理生命值、攻击、防御等基础数值
│
├─ BuffComponent(Buff管理器) ← 核心组件
│  ├─ FireBuff(燃烧Buff组件)
│  │  └─ 每隔0.5秒造成10点伤害，持续3秒
│  │
│  ├─ FreezeBuff(冰冻Buff组件)
│  │  └─ 移动速度降低30%，持续2秒
│  │
│  ├─ ShieldBuff(护盾Buff组件)
│  │  └─ 吸收100点伤害，无时间限制
│  │
│  └─ ...其他Buff
│
└─ CombatComponent(战斗组件)
   └─ 管理技能释放和伤害计算
```

【第二部分：配置表范例】

BuffTable(Excel配置表实际示例)：

| BuffId | BuffName | EffectType | Duration | Damage | Interval | Description |
|--------|----------|-----------|----------|--------|----------|-------------|
| 001 | 燃烧 | DOT | 3.0 | 10 | 0.5 | 每0.5秒造成10点伤害，共3秒 |
| 002 | 冰冻 | DEBUFF | 2.0 | -0.3 | - | 移动速度降低30%，持续2秒 |
| 003 | 增伤 | BUFF | 5.0 | 0.2 | - | 伤害提升20%，持续5秒 |
| 004 | 流血 | DOT | 4.0 | 15 | 1.0 | 每1秒造成15点伤害，共4秒 |
| 005 | 护盾 | SHIELD | 0 | 100 | - | 吸收100点伤害，触发销毁 |

字段说明：
- BuffId: 唯一标识符
- BuffName: 在游戏中显示的名称
- EffectType: 效果类型(DOT=持续伤害、DEBUFF=减益、BUFF=增益、SHIELD=护盾)
- Duration: 效果持续时间（秒），0表示直到条件触发
- Damage/Param: 伤害值或效果参数（负值表示减益）
- Interval: 周期效果的触发间隔（秒）

【第三部分：应用流程】

完整的Buff应用和管理流程：

```
1. 技能触发 → 调用 ApplyBuff(targetEntity, buffId=001)
   └─ 参数：目标Entity对象、要应用的BuffId

2. BuffComponent查询配置 → BuffTable.Get(001)
   └─ 返回：Buff配置数据(名称、效果类型、参数等)

3. Buff工厂创建实例 → BuffFactory.Create(buffData)
   └─ 根据EffectType创建对应的Buff类实例

4. 应用Buff → buff.OnApply(owner)
   ├─ 显示视觉特效(如燃烧的火焰光圈)
   ├─ 发送事件通知UI系统("获得Buff: 燃烧")
   └─ 初始化Buff参数(剩余时间、触发次数等)

5. 持续更新 → 每帧调用 buff.OnUpdate()
   ├─ 检查是否触发周期效果(如每0.5秒造成伤害)
   ├─ 如果触发：owner.TakeDamage(10) → 目标生命值-10
   └─ 更新Buff剩余时间

6. 时间到期 → 检查 if(buff.IsExpired())
   ├─ 调用 buff.OnRemove(owner) → 移除特效、清理状态
   ├─ 从activeBuff列表中移除
   └─ 发送事件通知UI系统("Buff失效: 燃烧")

7. 返回结果 → Buff完整生命周期结束
```

【第四部分：代码伪码实现】

```csharp
// Buff管理器组件
class BuffComponent : Component {
    List<BaseBuff> activeBuff = new List<BaseBuff>();
    
    // 应用新的Buff
    public void ApplyBuff(int buffId) {
        // 从配置表读取Buff配置
        BuffData data = BuffTable.Get(buffId);
        if(data == null) return;
        
        // 通过工厂创建Buff实例
        BaseBuff buff = BuffFactory.Create(data);
        activeBuff.Add(buff);
        
        // 初始化Buff
        buff.OnApply(owner);
        
        // 触发事件，UI显示"获得Buff"
        EventManager.Broadcast(new BuffAppliedEvent(buffId, data.BuffName));
    }
    
    // 每帧更新所有Buff
    public void Update() {
        for (int i = activeBuff.Count - 1; i >= 0; i--) {
            BaseBuff buff = activeBuff[i];
            
            // 更新Buff状态
            buff.OnUpdate(Time.deltaTime);
            
            // 检查是否过期
            if(buff.IsExpired()) {
                buff.OnRemove(owner);
                activeBuff.RemoveAt(i);
                
                // 触发事件，UI显示"Buff失效"
                EventManager.Broadcast(new BuffRemovedEvent(buff.BuffId));
            }
        }
    }
}

// 燃烧Buff具体实现
class FireBuff : BaseBuff {
    float elapsedTime = 0f;
    float nextTriggerTime = 0f;
    
    public override void OnUpdate(float deltaTime) {
        elapsedTime += deltaTime;
        
        // 每隔Param2(0.5秒)造成一次Damage(10点)
        if(elapsedTime >= nextTriggerTime) {
            int damage = (int)Param1;  // 从配置表读取：10
            owner.TakeDamage(damage);
            
            // 显示伤害飘字
            FloatingText.Show($"-{damage}", owner.position, Color.red);
            
            // 下次触发时间
            nextTriggerTime += Param2;  // 从配置表读取：0.5
        }
    }
    
    public override void OnApply(GameObject target) {
        owner = target;
        
        // 添加视觉特效
        vfx = Instantiate(fireVFXPrefab, owner.transform);
        
        // 播放音效
        AudioManager.Play("buff_fire_apply");
    }
    
    public override void OnRemove(GameObject target) {
        // 移除视觉特效
        Destroy(vfx);
        
        // 播放移除音效
        AudioManager.Play("buff_remove");
    }
}

// Buff工厂
class BuffFactory {
    public static BaseBuff Create(BuffData data) {
        switch(data.EffectType) {
            case "DOT":
                return new DamageOverTimeBuff(data);
            case "DEBUFF":
                return new DebuffBuff(data);
            case "BUFF":
                return new BuffBuff(data);
            case "SHIELD":
                return new ShieldBuff(data);
            default:
                return null;
        }
    }
}
```

【第五部分：设计优势】

1. **模块化** - 每个Buff完全独立，易于添加新Buff
   - 新增"减速"Buff只需创建SlowBuff类，无需修改现有代码

2. **可配置化** - 通过配置表调整Buff参数，无需改代码
   - 策划可以直接修改BuffTable，改变Buff伤害、时间等参数

3. **易测试** - 可单独测试每个Buff的效果
   - 直接调用ApplyBuff(buffId)观察效果，快速调试

4. **高复用** - Buff组件可挂载到任何Entity
   - 同一个FireBuff既可用于敌人，也可用于玩家角色

5. **易维护** - Bug修复只影响相关Buff组件
   - 如果燃烧Buff有Bug，只需修改FireBuff.cs，不影响其他系统

【第六部分：其他应用】

类似的组件模式在项目中被广泛应用：
- **技能系统**(SkillComponent) - 每个技能是独立组件
- **装备系统**(EquipmentComponent) - 每个装备是独立组件，可提供被动效果
- **状态系统**(StatusComponent) - 管理角色各种临时状态

[需插入图表6：Buff系统架构示意图]
```

**修改说明**:
- ✅ 先介绍设计模式框架
- ✅ 补充了Excel配置表范例（含实际数据）
- ✅ 详细说明了应用流程（7个步骤）
- ✅ 补充了关键代码伪码
- ✅ 说明了设计优势
- ✅ 关联了其他相似应用

---

## 修改完成统计

| 修改 | 批注 | 状态 | 内容量 |
|------|------|------|--------|
| 1 | 21 | ✅ 完成 | 序号规范 + 数据补充 |
| 2 | 25 | ✅ 完成 | AI 三个方向详细说明 |
| 3 | 34 | ✅ 完成 | 框架5个模块 + 分层价值 |
| 4 | 46 | ✅ 完成 | 设计原因 + 权衡分析 + 结论 |
| 5 | 74 | ✅ 完成 | 模式 + 配置表 + 流程 + 伪码 |

**文本修改总计**: 5项全部完成

---

## 后续工作

### 图表修改（需要AI生成）
- [ ] 批注 87: 为论文中的图表补充说明文字
- [ ] 批注 99: 区分两个相似的图表设计
- [ ] 批注 105: 放大尺寸过小的图表
- [ ] 批注 108: 使用Gemini Canvas生成8张统一风格的新图表
- [ ] 批注 112: 统一论文最后几张图表的尺寸
- [ ] 批注 143: 补充系统实现部分的图表和对比表

### 论文编辑
- [ ] 将修改内容替换回论文
- [ ] 插入新生成的8张图表
- [ ] 添加图表说明文字
- [ ] 统一排版和格式
- [ ] 全文通读检查

