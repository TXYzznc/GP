# Animator 动画控制器详细配置指南

## 一、参数配置

### 1. 必需参数

在 Animator 中创建以下参数：

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| **AnimType** | Integer | 0 | 动画类型（0-6） |
| **MoveSpeed** | Float | 1.0 | 移动速度倍数 |

### 2. 参数创建步骤

1. 打开 Animator 窗口
2. 点击 Parameters 标签
3. 点击 + 按钮，选择 Int，命名为 `AnimType`
4. 再次点击 + 按钮，选择 Float，命名为 `MoveSpeed`

---

## 二、动画类型映射

敌人系统共有 **6 个动画类型**，对应不同的 AI 状态：

```csharp
public enum EnemyAnimationType
{
    Idle = 0,       // 原地休息 - 敌人原地待机，随机时长后进入巡逻或深度休息
    Walk = 1,       // 巡逻 - 敌人在巡逻范围内移动
    Alert = 2,      // 警戒 - 敌人发现玩家后原地警戒
    Run = 3,        // 追击 - 敌人警觉度满后追击玩家
    Death = 5,      // 死亡 - 敌人死亡动画（暂未实现）
    Rest = 6,       // 深度休息 - 敌人深度休息，不会被玩家接近惊醒
}
```

### 状态转移流程

```
巡逻 (Walk)
  ↓ 检测到玩家
警戒 (Alert)
  ↓ 警觉度达到阈值 或 警戒时间结束
追击 (Run)
  ↓ 接近战斗距离
战斗 (无动画) ← 敌人进入战斗状态时不播放动画
  ↓ 战斗结束
巡逻 (Walk)

原地休息 (Idle)
  ↓ 随机进入深度休息
深度休息 (Rest)
  ↓ 休息时间结束
巡逻 (Walk)
```

### 特殊说明

- **战斗状态**：敌人进入战斗状态时**不播放任何动画**，因为此时玩家也已进入战斗状态，游戏直接进入战斗准备阶段。敌人的战斗动画由战斗系统管理。
- **广播追击**：精英敌人在追击玩家时可能广播玩家位置，附近敌人收到广播后也会进入追击状态（Run 动画）。
- **Attack 类型已移除**：原 Attack (4) 类型已删除，因为敌人不使用此动画。

---

## 三、状态机架构设计

### 推荐方案：混合状态机 + 混合树

**为什么这样设计：**
- 移动动画（Walk/Run）使用混合树，根据 MoveSpeed 平滑过渡
- 其他动画使用独立状态，便于管理和转换
- 减少状态数量，提高性能

### 状态层级结构

```
Base Layer
├── Idle (AnimType == 0)
├── Movement (混合树)
│   ├── Walk (MoveSpeed: 0.0 ~ 1.0)
│   └── Run (MoveSpeed: 1.0 ~ 2.0)
├── Alert (AnimType == 2)
├── Death (AnimType == 5)
└── Rest (AnimType == 6)
```

**注意**：不再需要 Attack 状态，因为敌人进入战斗状态时不播放动画。

---

## 四、详细配置步骤

### 步骤 1：创建基础状态

#### 1.1 创建 Idle 状态
1. 右键 → New State → Empty
2. 命名为 `Idle`
3. 拖入 Idle 动画
4. 设置 Motion 为 Idle 动画

#### 1.2 创建其他单独状态
重复上述步骤创建：
- `Alert` - 警戒动画
- `Death` - 死亡动画
- `Rest` - 休息动画

**不需要创建 Attack 状态**，因为敌人进入战斗状态时不播放动画。

### 步骤 2：创建 Movement 混合树

#### 2.1 创建混合树
1. 右键 → New State → Blend Tree
2. 命名为 `Movement`
3. 双击打开混合树编辑器

#### 2.2 配置混合树参数
1. 在混合树编辑器中，设置 Blend Type 为 `1D Blend`
2. 设置 Parameter 为 `MoveSpeed`

#### 2.3 添加动画到混合树

在混合树中添加以下动画：

| 动画 | Threshold | 说明 |
|------|-----------|------|
| Walk_Slow | 0.0 | 缓慢行走 |
| Walk_Normal | 0.5 | 正常行走 |
| Walk_Fast | 1.0 | 快速行走 |
| Run_Normal | 1.5 | 正常奔跑 |
| Run_Fast | 2.0 | 快速奔跑 |

**配置方式：**
1. 在混合树编辑器中，点击 + 按钮
2. 选择 Add Motion Field
3. 拖入动画，设置 Threshold 值
4. 重复添加所有动画

### 步骤 3：配置状态转换

#### 3.1 从 Idle 转换到其他状态

**Idle → Movement**
- 条件：`AnimType == 1` 或 `AnimType == 3`
- 转换时间：0.1s
- 代码：
  ```csharp
  animator.SetInteger("AnimType", 1); // Walk
  animator.SetFloat("MoveSpeed", 0.5f);
  ```

**Idle → Alert**
- 条件：`AnimType == 2`
- 转换时间：0.2s

**Idle → Death**
- 条件：`AnimType == 5`
- 转换时间：0.3s

**Idle → Rest**
- 条件：`AnimType == 6`
- 转换时间：0.2s

#### 3.2 从 Movement 转换到其他状态

**Movement → Idle**
- 条件：`AnimType == 0`
- 转换时间：0.2s

**Movement → Alert**
- 条件：`AnimType == 2`
- 转换时间：0.2s

**Movement → Death**
- 条件：`AnimType == 5`
- 转换时间：0.3s

#### 3.3 其他状态转换

**Alert → Idle**
- 条件：`AnimType == 0`
- 转换时间：0.2s

**Alert → Movement**
- 条件：`AnimType == 1` 或 `AnimType == 3`
- 转换时间：0.2s

**Alert → Death**
- 条件：`AnimType == 5`
- 转换时间：0.3s

**Alert → Death**
- 条件：`AnimType == 5`
- 转换时间：0.3s

**Attack → Idle**
- 条件：`AnimType == 0`
- 转换时间：0.2s
- 勾选 "Has Exit Time"（攻击动画播放完后自动返回）

**Death → (无转换)**
- Death 是终态，不需要转换出去

**Rest → Idle**
- 条件：`AnimType == 0`
- 转换时间：0.2s

---

## 五、转换条件配置详解

### 5.1 创建转换条件

1. 右键状态 → Make Transition
2. 点击目标状态
3. 点击转换箭头，在 Inspector 中配置

### 5.2 转换条件设置

**基础条件配置：**
```
Conditions:
  - AnimType Equals 1  (Walk)
  - AnimType Equals 2  (Alert)
  - AnimType Equals 3  (Run)
  - AnimType Equals 4  (Attack)
  - AnimType Equals 5  (Death)
  - AnimType Equals 6  (Rest)
```

**转换参数：**
- **Transition Duration**: 0.1 ~ 0.3s（根据动画类型调整）
- **Transition Offset**: 0（从目标动画开始播放）
- **Interruption Source**: None（不允许被打断）
- **Ordered Interruption**: 不勾选
- **Has Exit Time**: 
  - Attack 状态勾选（等待动画播放完）
  - 其他状态不勾选（立即转换）

---

## 六、代码集成示例

### 6.1 播放动画的标准方式

```csharp
public class EnemyAnimator : MonoBehaviour
{
    private Animator m_Animator;
    private static readonly int PARAM_ANIM_TYPE = Animator.StringToHash("AnimType");
    private static readonly int PARAM_MOVE_SPEED = Animator.StringToHash("MoveSpeed");

    public void PlayAnimation(EnemyAnimationType animType, float moveSpeed = 1f)
    {
        if (m_Animator == null) return;

        // 设置动画类型
        m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)animType);
        
        // 设置移动速度（仅在 Walk/Run 时有效）
        if (animType == EnemyAnimationType.Walk || animType == EnemyAnimationType.Run)
        {
            m_Animator.SetFloat(PARAM_MOVE_SPEED, moveSpeed);
        }

        DebugEx.LogModule("EnemyAnimator", $"播放动画: {animType} (速度: {moveSpeed:F2})");
    }
}
```

### 6.2 在 AI 状态中调用

```csharp
public class EnemyPatrolState : EnemyAIStateBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        // 播放行走动画
        var animator = m_Entity.GetComponent<EnemyAnimator>();
        animator?.PlayAnimation(EnemyAnimationType.Walk, 0.8f);
    }

    public override void OnExit()
    {
        base.OnExit();
        
        // 可选：返回待机
        var animator = m_Entity.GetComponent<EnemyAnimator>();
        animator?.PlayAnimation(EnemyAnimationType.Idle);
    }
}
```

---

## 七、混合树详细配置

### 7.1 为什么使用混合树

**优点：**
- 平滑过渡：根据 MoveSpeed 参数平滑混合动画
- 减少状态：不需要为每个速度创建单独状态
- 性能优化：减少状态转换开销
- 灵活控制：可以实时调整移动速度

**缺点：**
- 需要多个移动动画
- 配置相对复杂

### 7.2 混合树参数范围

```
MoveSpeed 范围：0.0 ~ 2.0

0.0 ─────────────────────────────────────── 2.0
│                                           │
Walk_Slow  Walk_Normal  Walk_Fast  Run_Normal  Run_Fast
0.0        0.5         1.0        1.5         2.0
```

**映射关系：**
- MoveSpeed = 0.0 ~ 0.5：Walk_Slow → Walk_Normal
- MoveSpeed = 0.5 ~ 1.0：Walk_Normal → Walk_Fast
- MoveSpeed = 1.0 ~ 1.5：Walk_Fast → Run_Normal
- MoveSpeed = 1.5 ~ 2.0：Run_Normal → Run_Fast

### 7.3 混合树配置步骤

1. 创建 Movement 混合树
2. 设置 Blend Type 为 `1D Blend`
3. 设置 Parameter 为 `MoveSpeed`
4. 添加 5 个动画，设置对应的 Threshold 值
5. 调整动画之间的过渡时间

---

## 八、常见配置错误

### ❌ 错误 1：转换条件设置错误
```
错误：Conditions 为空
正确：Conditions: AnimType Equals 1
```

### ❌ 错误 2：混合树参数范围不合理
```
错误：Threshold 值不连续 (0, 0.5, 2.0, 3.0)
正确：Threshold 值连续递增 (0, 0.5, 1.0, 1.5, 2.0)
```

### ❌ 错误 3：没有设置默认状态
```
错误：没有设置 Idle 为默认状态
正确：右键 Idle → Set as Layer Default State
```

### ❌ 错误 4：转换时间过长
```
错误：Transition Duration = 1.0s（太长，动画卡顿）
正确：Transition Duration = 0.1 ~ 0.3s
```

---

## 九、调试技巧

### 9.1 在 Animator 窗口调试

1. 进入 Play Mode
2. 打开 Animator 窗口
3. 观察当前状态和参数值
4. 手动修改参数测试转换

### 9.2 添加调试日志

```csharp
// 在 EnemyAnimator 中添加
public void PlayAnimation(EnemyAnimationType animType, float moveSpeed = 1f)
{
    if (m_Animator == null) return;

    m_CurrentAnimType = animType;
    m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)animType);
    m_Animator.SetFloat(PARAM_MOVE_SPEED, moveSpeed);

    // 调试日志
    DebugEx.LogModule("EnemyAnimator", 
        $"播放动画: {animType} (速度: {moveSpeed:F2})");
}
```

### 9.3 检查转换是否生效

```csharp
// 在 Update 中检查
if (Input.GetKeyDown(KeyCode.Space))
{
    animator.SetInteger("AnimType", 1); // 切换到 Walk
    DebugEx.LogModule("Test", "切换到 Walk");
}
```

---

## 十、性能优化建议

### 10.1 使用 StringToHash 缓存参数

```csharp
// ✅ 推荐：缓存哈希值
private static readonly int PARAM_ANIM_TYPE = Animator.StringToHash("AnimType");
m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)animType);

// ❌ 避免：每次都计算哈希值
m_Animator.SetInteger("AnimType", (int)animType);
```

### 10.2 避免频繁设置相同参数

```csharp
// ✅ 推荐：检查是否需要更新
if (m_CurrentAnimType != animType)
{
    m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)animType);
}

// ❌ 避免：每帧都设置
m_Animator.SetInteger(PARAM_ANIM_TYPE, (int)animType);
```

### 10.3 使用 Animator 的 culling mode

- 设置 Culling Mode 为 `Cull Update Transforms`
- 当敌人不可见时，停止更新动画

---

## 十一、完整配置清单

- [ ] 创建 AnimType (Integer) 参数
- [ ] 创建 MoveSpeed (Float) 参数
- [ ] 创建 Idle 状态
- [ ] 创建 Movement 混合树（包含 5 个动画）
- [ ] 创建 Alert 状态
- [ ] 创建 Attack 状态
- [ ] 创建 Death 状态
- [ ] 创建 Rest 状态
- [ ] 设置 Idle 为默认状态
- [ ] 配置所有状态转换条件
- [ ] 测试所有动画转换
- [ ] 验证混合树平滑过渡
- [ ] 添加调试日志
- [ ] 性能优化（StringToHash、Culling Mode）

