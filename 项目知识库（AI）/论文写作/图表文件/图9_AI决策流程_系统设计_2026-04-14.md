# 图9：AI 决策流程

**位置**: 第4章 战斗系统  
**章节**: 4.3 AI 系统  
**类型**: 流程图  
**用途**: 说明 AI 的决策机制

## Mermaid 代码

```mermaid
flowchart TD
    Start([AI 决策开始]) --> GetState["获取当前战斗状态"]
    GetState --> GetOptions["获取可用行动列表"]
    GetOptions --> EvaluateAll["评估所有行动"]
    
    EvaluateAll --> Loop["遍历每个行动"]
    Loop --> CalcScore["计算行动评分"]
    CalcScore --> CheckDamage["评估伤害输出"]
    CheckDamage --> CheckDefense["评估防御效果"]
    CheckDefense --> CheckBuff["评估 Buff 效果"]
    CheckBuff --> CheckHeal["评估治疗效果"]
    CheckHeal --> CalcPriority["计算优先级"]
    
    CalcPriority --> MoreActions{还有行动?}
    MoreActions -->|是| Loop
    MoreActions -->|否| SelectBest["选择评分最高的行动"]
    
    SelectBest --> CheckRandom{随机因子?}
    CheckRandom -->|是| ApplyRandom["应用随机波动"]
    CheckRandom -->|否| FinalAction["最终行动"]
    ApplyRandom --> FinalAction
    
    FinalAction --> End([返回选定行动])
    
    style Start fill:#90EE90
    style End fill:#FFB6C6
    style GetState fill:#87CEEB
    style GetOptions fill:#87CEEB
    style EvaluateAll fill:#FFD700
    style CalcScore fill:#FFD700
    style SelectBest fill:#DDA0DD
    style FinalAction fill:#DDA0DD
```

## 说明

AI 决策流程采用评分系统：

1. **获取状态** - 收集当前战斗状态信息
2. **获取行动** - 列出 AI 可用的所有行动
3. **评估行动** - 对每个行动进行多维度评估：
   - 伤害输出评估
   - 防御效果评估
   - Buff 效果评估
   - 治疗效果评估
4. **计算评分** - 综合各维度计算行动评分
5. **选择最优** - 选择评分最高的行动
6. **随机波动** - 可选的随机因子增加 AI 的不可预测性
7. **返回行动** - 返回最终选定的行动

