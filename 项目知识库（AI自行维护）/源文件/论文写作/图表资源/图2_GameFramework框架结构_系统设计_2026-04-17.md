# 图2：GameFramework 框架结构

```dot
digraph {
    rankdir=TB;
    nodesep=0.9;
    ranksep=1.2;
    splines=orthogonal;
    
    GameFramework [label="GameFramework\n\n+ Procedure\n+ FSM\n+ Event\n+ Entity\n+ UI\n+ Resource", style="filled", fillcolor="#fff3e0", shape="box"];
    
    Procedure [label="Procedure\n\n+ Initialize()\n+ Update()\n+ Shutdown()", style="filled", fillcolor="#fff9c4", shape="box"];
    FSM [label="FSM\n\n+ AddState()\n+ ChangeState()\n+ GetCurrentState()", style="filled", fillcolor="#fff9c4", shape="box"];
    Event [label="Event\n\n+ Subscribe()\n+ Unsubscribe()\n+ Publish()", style="filled", fillcolor="#fff9c4", shape="box"];
    Entity [label="Entity\n\n+ Create()\n+ Destroy()\n+ GetComponent()", style="filled", fillcolor="#fff9c4", shape="box"];
    UI [label="UI\n\n+ OpenForm()\n+ CloseForm()\n+ GetForm()", style="filled", fillcolor="#fff9c4", shape="box"];
    Resource [label="Resource\n\n+ LoadAsset()\n+ UnloadAsset()\n+ GetAsset()", style="filled", fillcolor="#fff9c4", shape="box"];
    
    GameFramework -> Procedure;
    GameFramework -> FSM;
    GameFramework -> Event;
    GameFramework -> Entity;
    GameFramework -> UI;
    GameFramework -> Resource;
}
```

## 说明

GameFramework 是游戏的核心框架，提供了以下主要模块：

- **Procedure**：游戏流程管理
- **FSM**：有限状态机
- **Event**：事件系统
- **Entity**：实体系统
- **UI**：UI管理系统
- **Resource**：资源管理系统

这些模块相互协作，为游戏提供了完整的基础设施。
