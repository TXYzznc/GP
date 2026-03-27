# Unity MCP 开发记录

本文件夹用于记录所有通过 Unity Skills MCP 工具执行的操作。

## 记录规范

### 文件命名
- 格式: `Unity操作记录_YYYY-MM-DD.md`
- 示例: `Unity操作记录_2026-03-06.md`

### 记录格式

每条记录包含以下信息:

```markdown
## [HH:MM] 操作描述

**工具名称**: `tool_name`

**调用参数**:
- param1: value1
- param2: value2

**执行结果**: ✅ 成功 / ❌ 失败

**关键输出**:
- 输出信息1
- 输出信息2

**备注**: 
- 相关说明或注意事项

---
```

## 快速记录模板

复制以下模板到当天的记录文件中:

```markdown
## [HH:MM] 操作描述

**工具名称**: `工具名称`

**调用参数**:
- 参数1: 值1
- 参数2: 值2

**执行结果**: ✅ 成功 / ❌ 失败

**关键输出**:
- 输出信息

**备注**: 
- 相关说明

---
```

## 常用工具分类

### GameObject 操作
- `gameobject_create` - 创建游戏对象
- `gameobject_delete` - 删除游戏对象
- `gameobject_set_transform` - 设置变换
- `gameobject_set_active` - 激活/禁用对象

### Component 操作
- `component_add` - 添加组件
- `component_remove` - 移除组件
- `component_set_property` - 设置组件属性

### Material 操作
- `material_create` - 创建材质
- `material_set_color` - 设置颜色
- `material_set_texture` - 设置纹理

### Scene 操作
- `scene_save` - 保存场景
- `scene_load` - 加载场景

### 其他操作
- 查看完整工具列表: `.kiro/skills/unity-skills/SKILL.md`

## 使用建议

1. **及时记录** - 完成操作后立即记录,避免遗忘
2. **详细描述** - 记录足够的上下文信息,便于后续查阅
3. **标注结果** - 明确标注操作是否成功
4. **添加备注** - 记录遇到的问题和解决方案
