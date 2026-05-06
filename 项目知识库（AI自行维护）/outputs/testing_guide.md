# 玩家位置修复 - 测试指南

## 快速测试流程

### 前置准备

1. **确保 PlayerAnchor 配置正确**
   - 打开战场预制体
   - 检查 PlayerAnchor 的**局部旋转（Local Rotation）**
   - 推荐设置为 (0, 0, 0)，即与战场方向一致

2. **获取测试工具**
   - 启用调试日志：`DebugEx` 模块
   - 在 Unity Console 中观察日志输出
   - 使用 Gizmos 绘制方向指示

## 测试场景 1：基础进入/离开战斗

### 步骤

1. **启动游戏**
   - 进入探索场景
   - 观察玩家初始朝向（例如：朝北）

2. **触发战斗**
   - 靠近敌人，触发战斗
   - 观察日志输出：
     ```
     玩家朝向=0°
     PlayerAnchor本地朝向=0°
     计算后战场朝向=0°
     ```

3. **验证进入战斗**
   - 检查玩家是否移动到 PlayerAnchor 位置
   - 检查玩家朝向是否仍为北（0°）
   - 使用 Gizmos 绘制朝向箭头对比

4. **离开战斗**
   - 脱战或游戏结束
   - 观察日志输出：
     ```
     玩家已通过 TeleportTo 恢复位置
     已恢复相机视角
     ```
   - 检查玩家位置和朝向是否回到原点

### 预期结果 ✅

```
进入前：位置=(0, 1, 0), 朝向=北(0°)
进入后：位置≈PlayerAnchor, 朝向=北(0°)
离开后：位置≈(0, 1, 0), 朝向=北(0°)
```

## 测试场景 2：不同方向朝向对齐

### 步骤（重复 4 次，每次改变玩家朝向）

**方向 1：玩家朝东**
1. 调整玩家朝向为东（90°）
2. 触发战斗
3. 观察日志：
   ```
   玩家朝向=90°
   计算后战场朝向=90°（或其他值，取决于 PlayerAnchor 本地朝向）
   ```
4. 验证 PlayerAnchor 朝向与玩家朝向一致

**方向 2：玩家朝南**
1. 调整玩家朝向为南（180°）
2. 重复验证

**方向 3：玩家朝西**
1. 调整玩家朝向为西（270°）
2. 重复验证

**方向 4：玩家朝北（45°）**
1. 调整玩家朝向为东北（45°）
2. 重复验证

### 预期结果 ✅

所有 4 个方向中，PlayerAnchor 的绝对朝向都与玩家朝向相同。

## 测试场景 3：重复进入/离开

### 步骤

1. **循环 5 次：进入 → 离开**
   - 每次验证位置是否准确
   - 记录每次的位置坐标

2. **累积偏差检查**
   ```
   第1次离开后位置：(1.234, 1.0, -0.567)
   第2次离开后位置：(1.234, 1.0, -0.567)
   第3次离开后位置：(1.234, 1.0, -0.567)
   ...
   第5次离开后位置：(1.234, 1.0, -0.567)  ← 应该完全相同
   ```

### 预期结果 ✅

每次离开战斗后，位置都完全相同（小数点后 3 位精度）。

## 测试场景 4：CharacterController 状态验证

### 步骤

1. **进入战斗前**
   - 玩家移动正常（WASD 可以移动）
   - CharacterController 的速度应该为零

2. **离开战斗后**
   - 玩家立即可以移动（无卡顿）
   - 移动方向正确（前后左右对应）
   - 重力恢复正常（不浮空）

3. **边界检查**
   - 站在悬崖边缘触发战斗
   - 离开战斗后，玩家不会卡在地形中
   - 不会发生位置穿透

### 预期结果 ✅

所有移动都流畅自然，无任何卡顿或异常。

## 日志检查清单

### ✅ 进入战斗时应该看到的日志

```
[SceneTransitionManager] 开始战斗准备...
[SceneTransitionManager] 记录玩家战斗前状态 - 位置: (X, Y, Z), 旋转: (0, Y°, 0)
[SceneTransitionManager] 已通知 PlayerCharacterManager 记录位置
[SceneTransitionManager] 敌人已隐藏
[SceneTransitionManager] 战斗准备完成

[BattleArenaManager] 战场旋转计算: 玩家朝向=Y1°, PlayerAnchor本地朝向=Y2°, 计算后战场朝向=Y3°
[BattleArenaManager] 战斗场地已生成

[SceneTransitionManager] 开始战场最终化...
[SceneTransitionManager] 玩家已通过 TeleportTo 移至战场 (PlayerAnchor: (X', Y', Z'), 朝向: (0, Y3°, 0))
```

### ✅ 离开战斗时应该看到的日志

```
[CombatState] 离开战斗状态

[PlayerCharacterManager] 位置恢复完成: 实际位置=(X, Y, Z), 实际旋转=(0, Y°, 0)

[SceneTransitionManager] 开始离开战斗场景转换...
[SceneTransitionManager] 玩家已通过 TeleportTo 恢复位置 (移至 (X, Y, Z), 朝向: (0, Y°, 0))
[SceneTransitionManager] 敌人已显示
[SceneTransitionManager] 交互物体已显示
[SceneTransitionManager] 离开战斗场景转换完成
```

## 常见问题排查

### 问题 1：PlayerAnchor 朝向与玩家不一致

**排查步骤：**
1. 检查日志中的"计算后战场朝向"
2. 验证 PlayerAnchor 的本地旋转是否正确设置
3. 检查预制体中 PlayerAnchor 的层级关系

**解决方案：**
```csharp
// 确保 PlayerAnchor 是战场的直接子对象
// 并且本地旋转为 (0, Y°, 0)（如果希望与战场方向一致，设为 0）
Transform playerAnchor = arena.transform.Find("PlayerAnchor");
if (playerAnchor != null)
{
    Debug.Log($"PlayerAnchor LocalRotation: {playerAnchor.localRotation.eulerAngles}");
}
```

### 问题 2：位置累积偏差

**排查步骤：**
1. 检查每次离开战斗后的 `RestorePositionAfterCombat()` 日志
2. 对比记录的原始位置和恢复的位置

**解决方案：**
- 确保 `RecordPositionBeforeCombat()` 只在战场生成前调用一次
- 确保 `RestorePositionAfterCombat()` 使用正确的记录位置

### 问题 3：玩家卡在地形中

**排查步骤：**
1. 检查 PlayerAnchor 的高度（Y 坐标）
2. 验证地形高度
3. 检查 CharacterController 的碰撞检测

**解决方案：**
```csharp
// 确保 PlayerAnchor 的 Y 坐标在地面以上
// 通常应该等于玩家的底部 Y 坐标
Vector3 playerBottomPos = EntityPositionHelper.GetBottomPosition(player);
Debug.Log($"PlayerAnchor Y: {playerAnchor.position.y}, Player Bottom Y: {playerBottomPos.y}");
```

## 性能检查

### 帧率监控

- **进入战斗**：应该没有明显的帧率波动
- **计算战场朝向**：耗时 < 0.1ms（可忽略）
- **离开战斗**：可能有短暂的溶解过渡（正常）

### 内存检查

- **无内存泄漏**：重复进入/离开 10 次，内存应该稳定
- **对象池正常**：棋子对象应该被正确回收

## 自动化测试建议

```csharp
[TestFixture]
public class PlayerPositionTests
{
    [Test]
    public void TestPlayerPositionRestoration()
    {
        Vector3 originalPos = player.transform.position;
        Quaternion originalRot = player.transform.rotation;
        
        // 进入战斗
        EnterCombat();
        
        // 离开战斗
        ExitCombat();
        
        // 验证位置恢复
        Assert.AreEqual(originalPos, player.transform.position, 0.01f);
        Assert.AreEqual(originalRot, player.transform.rotation, 0.01f);
    }
    
    [Test]
    public void TestPlayerAnchorAlignment()
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            player.transform.rotation = Quaternion.Euler(0, angle, 0);
            
            EnterCombat();
            
            // 验证 PlayerAnchor 朝向
            Vector3 playerForward = player.transform.forward;
            Vector3 anchorForward = playerAnchor.transform.forward;
            
            Assert.IsTrue(Vector3.Dot(playerForward, anchorForward) > 0.99f);
            
            ExitCombat();
        }
    }
}
```

## 提交测试记录

完成测试后，请记录：

- [ ] 日期：____
- [ ] 测试者：____
- [ ] 场景 1 进入/离开 - 结果：____ (✅ / ❌)
- [ ] 场景 2 朝向对齐 - 结果：____ (✅ / ❌)
- [ ] 场景 3 重复循环 - 结果：____ (✅ / ❌)
- [ ] 场景 4 CharacterController - 结果：____ (✅ / ❌)
- [ ] 性能检查 - 结果：____ (✅ / ❌)
- [ ] 问题列表：____

---

**测试完成后，如发现任何问题，请在 Console 中收集完整日志并提交反馈。**
