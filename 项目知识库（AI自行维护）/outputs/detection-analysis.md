# 检测系统对比分析

## 现有系统概览

### VisionConeDetector（敌人视野检测）
**特点：** 复杂、多阶段、主动轮询
- **检测方式**：Two-stage（周围圈 + 方向锥形）
- **更新机制**：每帧主动轮询，计算距离和角度
- **状态机制**：渐进式警觉度（0-1 float，缓慢增加/衰减）
- **特殊处理**：隐身、战斗中、全局屏蔽检测等多个条件判断
- **优点**：
  - 灵敏度高，能准确识别隐身、偷袭等特殊情况
  - 渐进式状态避免二值化的抖动
  - 支持复杂业务逻辑（警觉度系统）

### InteractionDetector（交互检测）
**特点：** 简洁、响应式、实用性强
- **检测方式**：Collider 触发器（二值化）
- **更新机制**：被动事件驱动 + 每帧评分
- **状态机制**：候选列表 + 评分系统（priority + distance + facing）
- **特殊处理**：CanInteract 回调让交互对象决定是否可交互
- **优点**：
  - 实现简洁，不需轮询距离/角度
  - 性能成本低
  - 评分机制支持多目标优先级
  - 灵活性好（扩展性强）

### TreasureChestInteractable（宝箱交互）
**当前设计**：基于 InteractionDetector，简单可靠
- 使用 Collider trigger 检测范围内进入/离开
- CanInteract 返回 `!m_IsAnimating`（动画播放中不可交互）
- OutlineController 动态显示/隐藏描边反馈

---

## 对标分析

| 特性 | VisionConeDetector | InteractionDetector | TreasureChestInteractable |
|------|------------------|-------------------|--------------------------|
| 检测可靠性 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| 实现复杂度 | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ |
| 性能开销 | 中等 | 低 | 低 |
| 支持多目标 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 单目标（不需要） |
| 特殊状态处理 | 丰富 | 基础 | 基础（足够） |

---

## 改进建议

### 方案 A：保持现状（推荐）
**适用场景**：宝箱交互不需要复杂的状态管理
- ✅ 实现已完成且功能正常
- ✅ 性能开销最低
- ✅ 代码易维护
- ✅ Collider trigger 足够可靠

**结论**：当前设计合理，不需改动。

---

### 方案 B：轻量级增强（可选）
如果未来需要更灵敏的检测，可考虑在 InteractableBase 中补充：

```csharp
// 在 InteractableBase 中添加主动检测
protected virtual void Update()
{
    // 轻量级：当 InteractionDetector 距离边界时，增加朝向权重
    // 示例：靠近宝箱时，即使不正对也能更容易被检测到
}
```

但这只在以下情况需要：
1. 玩家反馈说"靠近时容易忽视"
2. 多个可交互对象竞争时优先级显示不稳定
3. 边界情况下描边闪烁

---

## 当前 TreasureChestInteractable 设计评估

✅ **满足需求**：
- 宝箱状态管理（Locked → Opened）
- 动画播放与等待（Animator + WaitUntil normalizedTime）
- 视觉反馈（OutlineController 动态显示）
- 防重复触发（m_IsAnimating 标志）

✅ **兼容架构**：
- 遵循 InteractionDetector 的评分系统
- 继承 InteractableBase 的 trigger collider 自动创建
- 符合项目的 UniTask + DebugEx 规范

✅ **可扩展性**：
- OpenChestUI() 占位清晰，后续 UI 接入无阻碍
- 外部数据通过 DataTable 注入，不硬编码

---

## 后续建议

### 立即行动
1. ✅ 完成 Section 6 的测试验证（6 个测试步骤）
   - 验证各个交互状态下的描边显示/隐藏
   - 验证动画播放中的 CanInteract 防护
   - 验证状态转换后的提示文本更新

2. 准备 InteractTip Prefab
   - 世界空间 UI，显示"按【F】打开宝箱"等提示
   - 集成到 InteractionDetector.OnTargetChanged 事件

### 后续优化
- 如果玩家反馈检测不灵敏 → 考虑方案 B 的轻量级增强
- 如果宝箱有其他状态（如锁定/已解锁/被破坏）→ 扩展 ChestState 枚举

