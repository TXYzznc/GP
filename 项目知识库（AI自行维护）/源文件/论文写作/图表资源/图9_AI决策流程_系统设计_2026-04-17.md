# 图9：AI 决策流程

**位置**: 第4章 战斗系统  
**章节**: 4.3 AI 系统  
**类型**: 流程图  
**用途**: 说明 AI 的决策机制

## Mermaid 代码

```dot
digraph {
    rankdir=TB;
    nodesep=0.8;
    ranksep=0.8;
    splines=ortho;
    
    node [fontname="SimHei,sans-serif"];
    edge [fontname="SimHei,sans-serif"];
    
    Start [label="AI 决策开始", style="filled", fillcolor="#90EE90", shape="ellipse"];
    GetState [label="获取当前战斗状态", style="filled", fillcolor="#87CEEB", shape="box"];
    GetOptions [label="获取可用行动列表", style="filled", fillcolor="#87CEEB", shape="box"];
    EvaluateAll [label="评估所有行动", style="filled", fillcolor="#FFD700", shape="box"];
    Loop [label="遍历每个行动", style="filled", fillcolor="#87CEEB", shape="box"];
    CalcScore [label="计算行动评分", style="filled", fillcolor="#FFD700", shape="box"];
    CheckDamage [label="评估伤害输出", style="filled", fillcolor="#87CEEB", shape="box"];
    CheckDefense [label="评估防御效果", style="filled", fillcolor="#87CEEB", shape="box"];
    CheckBuff [label="评估 Buff 效果", style="filled", fillcolor="#87CEEB", shape="box"];
    CheckHeal [label="评估治疗效果", style="filled", fillcolor="#87CEEB", shape="box"];
    CalcPriority [label="计算优先级", style="filled", fillcolor="#87CEEB", shape="box"];
    MoreActions [label="还有行动?", style="filled", fillcolor="#87CEEB", shape="diamond"];
    SelectBest [label="选择评分最高的行动", style="filled", fillcolor="#DDA0DD", shape="box"];
    CheckRandom [label="随机因子?", style="filled", fillcolor="#87CEEB", shape="diamond"];
    ApplyRandom [label="应用随机波动", style="filled", fillcolor="#DDA0DD", shape="box"];
    FinalAction [label="最终行动", style="filled", fillcolor="#DDA0DD", shape="box"];
    End [label="返回选定行动", style="filled", fillcolor="#FFB6C6", shape="ellipse"];
    
    // 第1行：右→左（蛇形）
    {rank=same; Start; GetState; GetOptions; EvaluateAll}
    EvaluateAll -> GetOptions -> GetState -> Start [style=invis];
    Start -> GetState -> GetOptions -> EvaluateAll [constraint=false];
    
    // 循环入口（EvaluateAll在左侧，直接向下连Loop）
    {rank=same; Loop; CalcScore}
    EvaluateAll -> Loop;
    Loop -> CalcScore;
    
    // 四个评估维度并排（关键：展开宽度）
    {rank=same; CheckDamage; CheckDefense; CheckBuff; CheckHeal}
    CalcScore -> CheckDamage;
    CalcScore -> CheckDefense;
    CalcScore -> CheckBuff;
    CalcScore -> CheckHeal;
    
    // 汇聚到优先级计算
    {rank=same; CalcPriority; MoreActions}
    CheckDamage -> CalcPriority;
    CheckDefense -> CalcPriority;
    CheckBuff -> CalcPriority;
    CheckHeal -> CalcPriority;
    CalcPriority -> MoreActions;
    
    // 循环回路
    MoreActions -> Loop [xlabel="是", constraint=false];
    
    // 第5行：右→左（蛇形）
    {rank=same; SelectBest; CheckRandom; ApplyRandom; FinalAction; End}
    End -> FinalAction -> ApplyRandom -> CheckRandom -> SelectBest [style=invis];
    MoreActions -> SelectBest [xlabel="否"];
    SelectBest -> CheckRandom [constraint=false];
    CheckRandom -> ApplyRandom [xlabel="是", constraint=false];
    CheckRandom -> FinalAction [xlabel="否", constraint=false];
    ApplyRandom -> FinalAction [constraint=false];
    FinalAction -> End [constraint=false];
}
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

