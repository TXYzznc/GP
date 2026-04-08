# 图12：Buff 应用与移除流程

**位置**: 第4章 战斗系统  
**章节**: 4.4 Buff 系统  
**类型**: 流程图  
**用途**: 展示 Buff 的生命周期

## Mermaid 代码

```mermaid
flowchart TD
    Start([Buff 应用]) --> GetBuff["获取 Buff 数据"]
    GetBuff --> CheckStack["检查堆叠规则"]
    CheckStack --> StackType{堆叠类型?}
    
    StackType -->|覆盖| RemoveOld["移除旧 Buff"]
    RemoveOld --> ApplyNew["应用新 Buff"]
    
    StackType -->|叠加| CheckMax["检查最大层数"]
    CheckMax --> IsMax{达到上限?}
    IsMax -->|是| RemoveOldest["移除最旧层"]
    RemoveOldest --> AddLayer["增加新层"]
    IsMax -->|否| AddLayer
    
    StackType -->|独立| ApplyNew
    
    ApplyNew --> InitBuff["初始化 Buff"]
    InitBuff --> ApplyEffect["应用 Buff 效果"]
    ApplyEffect --> StartDuration["启动持续时间"]
    StartDuration --> Active["Buff 激活"]
    
    Active --> Update["每帧更新"]
    Update --> CheckDuration{持续时间到期?}
    CheckDuration -->|否| CheckTrigger{触发条件?}
    CheckTrigger -->|是| TriggerEffect["触发效果"]
    TriggerEffect --> Update
    CheckTrigger -->|否| Update
    
    CheckDuration -->|是| RemoveBuff["移除 Buff"]
    RemoveBuff --> RemoveEffect["移除 Buff 效果"]
    RemoveEffect --> Cleanup["清理资源"]
    Cleanup --> End([Buff 结束])
    
    style Start fill:#90EE90
    style End fill:#FFB6C6
    style Active fill:#87CEEB
    style ApplyEffect fill:#FFD700
    style RemoveEffect fill:#FF6347
    style Update fill:#DDA0DD
```

## 说明

Buff 的完整生命周期：

1. **应用阶段**
   - 获取 Buff 数据
   - 根据堆叠规则处理现有 Buff
   - 应用新 Buff 的效果

2. **激活阶段**
   - 初始化 Buff 参数
   - 应用 Buff 效果到目标
   - 启动持续时间计时

3. **活跃阶段**
   - 每帧更新 Buff 状态
   - 检查触发条件
   - 执行周期性效果

4. **移除阶段**
   - 持续时间到期时移除 Buff
   - 移除 Buff 效果
   - 清理相关资源

