> **最后更新**: 2026-03-23
> **状态**: 有效
---

# 相机 FOV 配置开发注意事项

## 📋 目录

- [核心问题](#核心问题)
- [关键原则](#关键原则)
- [最佳实践](#最佳实践)
- [常见陷阱](#常见陷阱)

---


### 1. 不同视角模式的 FOV 管理
- **第三人称模式**：使用 `defaultFOV` 和 `sprintFOV`
- **俯视角模式**：使用 `topDownFOV`（从配置表读取）
- **战斗模式**：继承第三人称的 FOV 逻辑

### 2. 配置表集成
- 从 `CombatRuleTable.CameraView` 字段读取俯视角 FOV 值
- 在 `Start()` 方法中调用 `LoadTopDownFOVFromConfig()` 加载配置
- 提供默认值作为配置读取失败时的备用方案

### 3. FOV 处理流程
```csharp
// 俯视角模式专用 FOV 处理
private void HandleTopDownFOV()
{
    if (m_IsFOVOverridden)
        m_TargetFOV = m_OverrideFOV;  // 覆盖优先
    else
        m_TargetFOV = topDownFOV;     // 使用配置值
    
    // 平滑过渡
    m_CurrentFOV = Mathf.SmoothDamp(m_CurrentFOV, m_TargetFOV, ref m_FOVVelocity, fovSmoothTime);
}
```

### 4. 视角切换日志
- 记录视角模式切换过程
- 记录 FOV 值的变化
- 便于调试和问题定位

## 最佳实践
1. **配置驱动**：FOV 值从配置表读取，便于策划调整
2. **平滑过渡**：使用 `SmoothDamp` 确保 FOV 切换自然
3. **异常处理**：配置读取失败时使用默认值
4. **日志记录**：关键操作添加日志便于调试

[↑ 返回目录](#目录)

---

## 常见陷阱
- 忘记为新视角模式添加专用的 FOV 处理逻辑
- 配置表字段变更时未同步更新代码
- 视角切换时 FOV 过渡不平滑

[↑ 返回目录](#目录)
