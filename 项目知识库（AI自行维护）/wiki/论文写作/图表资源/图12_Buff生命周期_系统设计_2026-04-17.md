# 图12：Buff 应用与移除流程

**位置**: 第4章 战斗系统  
**章节**: 4.4 Buff 系统  
**类型**: 流程图  
**用途**: 展示 Buff 的生命周期

## Graphviz DOT 代码

```dot
digraph G {
    rankdir=TB;
    splines=orthogonal;
    nodesep=0.8;
    ranksep=1.0;
    
    // 节点定义
    Start [shape=ellipse, label="Buff 应用", style=filled, fillcolor="#90EE90"];
    GetBuff [shape=box, label="获取 Buff 数据"];
    CheckStack [shape=box, label="检查堆叠规则"];
    StackType [shape=diamond, label="堆叠类型?"];
    
    RemoveOld [shape=box, label="移除旧 Buff"];
    CheckMax [shape=box, label="检查最大层数"];
    IsMax [shape=diamond, label="达到上限?"];
    RemoveOldest [shape=box, label="移除最旧层"];
    AddLayer [shape=box, label="增加新层"];
    
    ApplyNew [shape=box, label="应用新 Buff"];
    InitBuff [shape=box, label="初始化 Buff"];
    ApplyEffect [shape=box, label="应用 Buff 效果", style=filled, fillcolor="#FFD700"];
    StartDuration [shape=box, label="启动持续时间"];
    Active [shape=box, label="Buff 激活", style=filled, fillcolor="#87CEEB"];
    
    Update [shape=box, label="每帧更新", style=filled, fillcolor="#DDA0DD"];
    CheckDuration [shape=diamond, label="持续时间到期?"];
    CheckTrigger [shape=diamond, label="触发条件?"];
    TriggerEffect [shape=box, label="触发效果"];
    
    RemoveBuff [shape=box, label="移除 Buff"];
    RemoveEffect [shape=box, label="移除 Buff 效果", style=filled, fillcolor="#FF6347"];
    Cleanup [shape=box, label="清理资源"];
    End [shape=ellipse, label="Buff 结束", style=filled, fillcolor="#FFB6C6"];
    
    // 应用阶段
    Start -> GetBuff;
    GetBuff -> CheckStack;
    CheckStack -> StackType;
    
    StackType -> RemoveOld [label="覆盖"];
    RemoveOld -> ApplyNew;
    
    StackType -> CheckMax [label="叠加"];
    CheckMax -> IsMax;
    IsMax -> RemoveOldest [label="是"];
    RemoveOldest -> AddLayer;
    IsMax -> AddLayer [label="否"];
    AddLayer -> ApplyNew;
    
    StackType -> ApplyNew [label="独立"];
    
    // 激活阶段
    ApplyNew -> InitBuff;
    InitBuff -> ApplyEffect;
    ApplyEffect -> StartDuration;
    StartDuration -> Active;
    
    // 活跃阶段
    Active -> Update;
    Update -> CheckDuration;
    CheckDuration -> CheckTrigger [label="否"];
    CheckTrigger -> TriggerEffect [label="是"];
    TriggerEffect -> Update;
    CheckTrigger -> Update [label="否"];
    
    // 移除阶段
    CheckDuration -> RemoveBuff [label="是"];
    RemoveBuff -> RemoveEffect;
    RemoveEffect -> Cleanup;
    Cleanup -> End;
}
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

