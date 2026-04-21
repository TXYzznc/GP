## Why

项目已经完成60-70%的开发进度，进入功能验收和优化阶段。当前代码库中存在多个影响稳定性、性能和可维护性的问题（async void 滥用、代码重复、事件订阅泄漏、单例不一致等），需要系统化地识别和优化，确保代码质量为后续的热修复和维护奠定基础。

## What Changes

- **修复 async void 滥用**（2级别）：10+ 处 async void 改为 UniTask，符合异步编程规则
- **消除代码重复**（3级别）：BuffManager/CardManager/UI 系统中重复代码的重构
- **事件订阅管理**（2级别）：检查并修复未取消订阅导致的内存泄漏
- **性能优化**（2-3级别）：
  - GetBuff/GetComponent 缓存优化
  - Update 中的参数检测优化
  - 集合遍历方式优化
- **单例管理统一**（1级别）：CardManager/ChessPlacementManager 等单例实现规范化
- **错误处理完善**（2级别）：对象销毁后异步执行的保护、配置表查询失败的处理

## Capabilities

### New Capabilities
- `async-void-refactoring`: 全面替换 async void 为 UniTask，确保异步操作可被 await 和取消
- `buff-manager-optimization`: 重构 BuffManager 双重载 AddBuff 方法，消除 80% 的代码重复
- `event-subscription-audit`: 完整审计并修复事件订阅泄漏，建立订阅生命周期规范
- `performance-optimization`: GetBuff/GetComponent 缓存、Update 热路径优化、集合遍历优化
- `singleton-standardization`: 统一单例实现方式（CardManager/ChessPlacementManager 等）
- `cancellation-token-safeguard`: 为所有 MonoBehaviour 异步方法加入销毁令牌保护

### Modified Capabilities
- `buff-system`: Buff 系统的 AddBuff API 保持向后兼容，内部重构消除重复
- `async-programming`: 异步编程规则的完整实施和检查

## Impact

**影响范围**：
- **代码文件**：500+ 个脚本，重点关注 Manager/System 系统（50+ 个）
- **系统**：Buff 系统、战斗系统、UI 系统、事件系统
- **API 兼容性**：重构为内部优化，公共 API 保持向后兼容
- **性能收益**：
  - Buff 更新：减少 GetBuff 调用次数，性能提升 15-20%
  - Event 系统：修复泄漏后，长期运行内存稳定性提升
  - Update 热路径：减少不必要的参数检测，帧率提升 1-2%
- **稳定性**：消除 async void 导致的异常吞没、销毁后继续执行等隐性 bug

**分级说明**：
- 1 级别：优化，无新功能，无 API 变更
- 2 级别：修复，影响稳定性或性能，无 API 变更
- 3 级别：重构，涉及 API 设计优化，需谨慎向后兼容
