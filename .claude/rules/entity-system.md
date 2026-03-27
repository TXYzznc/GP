---
paths: ["Assets/AAAGame/Scripts/Entity/**/*.cs", "Assets/AAAGame/Prefabs/Entity/**/*.prefab"]
---

# 实体系统规则

## 架构说明

实体（Entity）通过 GameFramework 的对象池管理，不要直接 `Instantiate` / `Destroy`。

```
GF.Entity.ShowEntity()  →  从对象池取出  →  OnShow() 初始化
GF.Entity.HideEntity()  →  回到对象池   →  OnHide() 清理
```

## 创建新实体的流程

1. 创建预制体到 `Assets/AAAGame/Prefabs/Entity/`
2. 在 `EntityGroupTable` 中配置所属的实体组
3. 创建继承 `EntityBase` 的脚本

```csharp
public class SomeEntity : EntityBase
{
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        var entityParams = userData as SomeEntityParams; // 接收参数
        // 初始化逻辑
    }

    protected override void OnHide(bool isShutdown, object userData)
    {
        // 清理逻辑（重置状态、停止动画等）
        base.OnHide(isShutdown, userData);
    }
}
```

## 显示/隐藏实体

```csharp
// 显示实体（传参用 EntityParams 或自定义类）
var entityParams = EntityParams.Create(entityId);
GF.Entity.ShowEntity(typeof(SomeEntity), "Entity/SomePrefab", "EntityGroup", entityParams);

// 隐藏实体
GF.Entity.HideEntity(entity);
GF.Entity.HideEntity(entityId);
```

## 注意事项

- `OnShow` 中不要假设字段是初始状态，上一次使用的对象可能留有残留数据
- 粒子效果实体（`ParticleEntity`）在播放完毕后会自动归还对象池
- 不要在 `OnHide` 之后访问该实体的任何字段（对象可能已被其他地方重用）
- Billboard 实体（始终朝向摄像机）继承 `BillboardEntity`
