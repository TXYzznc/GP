# CastRange 系统实现修改清单

## 修改文件列表

### 1. 工具类增强
✅ **ChessTargetFinder.cs** (L146-160)
- 新增方法：`IsInCastRange(ChessEntity self, ChessEntity target, double castRange)`
- 功能：检查目标是否在指定的施法范围内
- 调用：所有需要检查施法范围的地方

### 2. 技能基类增强
✅ **ChessSkillBase.cs** (L122-137)
- 新增方法：`IsInCastRange(ChessEntity caster, ChessEntity target)`
- 功能：包装 ChessTargetFinder 的方法，方便技能类使用
- 注意：使用 `m_Config.CastRange` 获取配置

### 3. AI 基类核心改动
✅ **ChessAIBase.cs** 

#### 3.1 使用技能状态 (L424-488)
- 修改：`TickUsingSkill()` 方法
- **新增逻辑**：在释放技能前检查 `IsSkillInRange()`
- 如果不在范围内：自动切换到 `Moving` 状态

#### 3.2 移动方法增强 (L823-861)
- 修改：`MoveToTarget()` 方法
- **新增逻辑**：
  - 判断是否有待命技能
  - 优先使用技能的 `CastRange`
  - 特殊处理自我技能 (ID=13)
  - 计算移动目标：`targetPos - direction * (range * 0.8f)`

#### 3.3 新增辅助方法 (L780-804)
- 新增方法：`IsSkillInRange(int skillIndex, ChessEntity target)`
- 功能：
  - 获取对应的技能配置
  - 特殊处理自我技能（始终返回 true）
  - 对普通技能调用 `ChessTargetFinder.IsInCastRange()`

### 4. 近战 AI 子类
✅ **FSMMeleeAI.cs** (L12-41)
- 修改：`TickMoving()` 方法
- **新增逻辑**：
  - 先检查待命技能范围
  - 再检查攻击范围
  - 优先级：技能范围 > 攻击范围

### 5. 远程 AI 子类
✅ **FSMRangedAI.cs** (L17-47)
- 修改：`TickMoving()` 方法
- 同上，逻辑完全一致

## 关键参数

| 字段 | 类型 | 来源 | 用途 |
|------|------|------|------|
| `AtkRange` | double | ChessAttribute | 普攻范围 |
| `CastRange` | double | SummonChessSkillTable | 技能施法范围 |
| `AreaRadius` | double | SummonChessSkillTable | 技能生效范围 |

## 自我技能识别

```csharp
// 特殊技能 ID 标记
if (skill.Config.Id == 13)  // 后羿技能一
    return true;  // 不需要范围检查
```

## 测试验证点

- [ ] **邪灵大招（ID=34）**
  - CastRange = 10
  - 需要移动到敌人 10 格内才能释放
  - 法阵范围 (AreaRadius) = 4

- [ ] **后羿技能一（ID=13）**
  - 自我技能
  - 不需要范围检查
  - 不需要移动

- [ ] **嫦娥技能一/大招**
  - 检查是否正确移动到 CastRange 内

- [ ] **邪灵被动/技能一**
  - 检查范围管理是否正常

## 日志输出示例

```
[FSMMeleeAI] 邪灵 到达技能范围，返回待机释放技能
[ChessAIBase] 邪灵 技能2的目标不在施法范围内，切换到移动
[ChessAIBase] 邪灵 请求 Controller 执行大招
```

## 兼容性说明

- ✅ 与现有的普攻逻辑兼容
- ✅ 与现有的 SkillStrategy 兼容
- ✅ 向后兼容（所有棋子自动获得该功能）
- ⚠️ 需要确保所有技能都有正确的 CastRange 配置

## 后续工作

1. **配置检查**：确保所有技能的 CastRange 已正确配置
2. **特殊技能维护**：若有新的自我技能，需要添加 ID 检查
3. **性能监控**：监控范围检查的计算成本
4. **日志清理**：发布版本时可关闭详细日志

