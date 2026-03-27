# 技能UI系统 - SkillParamRegistry 配置说明

> **最后更新**: 2026-03-23
> **状态**: 有效
> **类型**: 配置说明

## 📋 目录

- [问题描述](#问题描述)
- [解决方案](#解决方案)
- [技术要点](#技术要点)
- [下一步工作](#下一步工作)
- [相关文件](#相关文件)
- [注意事项](#注意事项)

---

`PlayerSkillManager` 需要 `SkillParamRegistrySO` 配置才能正常工作，但项目使用基于 `ResourceConfigTable` 的资源加载系统，不能直接使用 `Resources.Load`。

## 解决方案

### 1. ResourceIds 配置规则
在 `Assets/AAAGame/Scripts/Config/ResourceIds.cs` 中新增了 ScriptableObject 配置区间：

```csharp
#region ScriptableObject配置 (13000-13999)
/// <summary>
/// 技能参数注册表
/// </summary>
public const int SO_SKILL_PARAM_REGISTRY = 13001;
#endregion
```

**ID分配规则：**
- **13000-13999**: ScriptableObject 配置文件
- **13001**: 技能参数注册表 (`SkillParamRegistrySO`)

### 2. 资源配置表添加
在 `Assets/AAAGame/DataTable/ResourceConfigTable.txt` 中添加了以下配置：

```
13001		6	Skills/SkillParamRegistry	技能参数注册表
```

**配置说明：**
- **ID**: `13001` - 对应 `ResourceIds.SO_SKILL_PARAM_REGISTRY`
- **Type**: `6` - ScriptableObject 类型
- **Path**: `Skills/SkillParamRegistry` - 相对路径（完整路径为 `Assets/AAAGame/ScriptableObjects/Skills/SkillParamRegistry.asset`）
- **Name**: `技能参数注册表` - 资源名称

### 3. GameProcedure 修改
在 `Assets/AAAGame/Scripts/Procedures/GameProcedure.cs` 的 `InitializeSkillManager()` 方法中：

```csharp
// 使用 ResourceExtension 异步加载技能参数注册表
const int SKILL_PARAM_REGISTRY_ID = 13001; // ResourceConfigTable 中的配置ID

try
{
    var paramRegistry = await GameExtension.ResourceExtension.LoadScriptableObjectAsync<SkillParamRegistrySO>(SKILL_PARAM_REGISTRY_ID);

    if (paramRegistry != null)
    {
        m_SkillManager.SetParamRegistry(paramRegistry);
        Log.Info("GameProcedure: 技能参数注册表已加载");
    }
    else
    {
        Log.Warning("GameProcedure: 未找到技能参数注册表，技能可能无法正常工作");
    }
}
catch (System.Exception ex)
{
    Log.Error($"GameProcedure: 加载技能参数注册表失败: {ex.Message}");
}
```

[↑ 返回目录](#目录)

---

## 技术要点

### ResourceExtension 支持
`ResourceExtension.cs` 已经支持 ScriptableObject 加载：
- 资源类型枚举：`ResourceType.ScriptableObject = 6`
- 加载方法：`LoadScriptableObjectAsync<T>(int configId)`
- 路径规则：`Assets/AAAGame/ScriptableObjects/{相对路径}.asset`

### 加载时序
1. **GameProcedure.OnEnter()** 进入时
2. **InitializeSkillManager()** 创建技能管理器
3. **异步加载** SkillParamRegistry（使用 UniTask）
4. **SetParamRegistry()** 设置到技能管理器
5. **OpenUIForm()** 加载技能UI
6. **SpawnPlayerCharacter()** 生成角色
7. **UpdateSkillsFromPlayerData()** 加载技能数据

### 异常处理
- 使用 `try-catch` 捕获加载异常
- 加载失败时记录警告日志
- 技能系统可以在没有参数注册表的情况下运行（降级模式）

[↑ 返回目录](#目录)

---

## 下一步工作

### 必须完成
1. ✅ 在 Unity 编辑器中重新生成配置表代码（使用配置表工具）
2. ✅ 确认 `SkillParamRegistry.asset` 资源存在于正确路径
3. ⏳ 测试技能UI加载和显示是否正常
4. ⏳ 确认技能冷却、图标等功能是否正常

### 可选优化
- 添加资源预加载机制（在 PreloadProcedure 中）
- 添加资源缓存避免重复加载
- 优化错误提示信息

[↑ 返回目录](#目录)

---

## 相关文件
- `Assets/AAAGame/DataTable/ResourceConfigTable.txt` - 资源配置表
- `Assets/AAAGame/Scripts/Procedures/GameProcedure.cs` - 游戏流程
- `Assets/AAAGame/Scripts/Extension/ResourceExtension.cs` - 资源加载扩展
- `Assets/AAAGame/Scripts/Game/Player/PlayerSkill/PlayerSkillManager.cs` - 技能管理器
- `Assets/AAAGame/Scripts/Game/Player/PlayerSkill/SkillParamRegistrySO.cs` - 技能参数注册表
- `Assets/AAAGame/ScriptableObjects/Skills/SkillParamRegistry.asset` - 技能参数资源

[↑ 返回目录](#目录)

---

## 注意事项
1. **配置表ID不要冲突** - 13001 是新分配的ID，确保不与其他资源冲突
2. **路径大小写敏感** - 确保路径与实际文件路径完全一致
3. **资源类型正确** - ScriptableObject 必须使用类型 6
4. **异步加载** - 使用 UniTask 确保不阻塞主线程

[↑ 返回目录](#目录)
