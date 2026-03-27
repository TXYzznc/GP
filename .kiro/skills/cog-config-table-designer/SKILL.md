---
name: cog-config-table-designer
description: COG项目配置表设计与生成工具。专注于配置表的设计规范和TXT格式生成。强制规范：配置表中使用资源ID（int类型）而非资源路径。使用DataTableGenerator的DataTable创建方式，确保与游戏框架兼容。TXT转XLSX格式转换由用户手动执行。
keywords: 配置表, 数据表, DataTable, 配置表设计, 资源ID, COG项目
license: 项目专用工具
---

# COG项目配置表设计与生成工具

## 🎯 快速开始

### 何时使用
- 设计和创建游戏配置表
- 生成符合项目规范的配置表（强制使用资源ID）
- 生成符合DataTableProcessor格式的TXT配置文件

### ⚠️ 重要规范

**配置表设计强制规范**：
1. **禁止在配置表中填入资源路径**（如 `UI/Items/Gold.png`）
2. **必须使用资源ID**（int类型，如 `1101`）
3. **所有资源路径统一在 ResourceConfigTable 中管理**
4. **使用 ResourceIds 常量类引用资源**

**错误示例**：
```
❌ 错误：ItemTable 配置表
| ID | Name   | IconPath              |
|----|--------|-----------------------|
| 1  | 金币   | UI/Items/Gold.png     |
```

**正确示例**：
```
✅ 正确：ItemTable 配置表
| ID | Name   | IconId |
|----|--------|--------|
| 1  | 金币   | 1101   |
```

**资源ID命名规范**：
- 图标资源：`IconId` 或 `IconIds`（数组）
- 预制体资源：`PrefabId` 或 `PrefabIds`（数组）
- 特效资源：`EffectId` 或 `EffectIds`（数组）
- 材质资源：`MaterialId` 或 `MaterialIds`（数组）
- 纹理资源：`TextureId` 或 `TextureIds`（数组）
- 配置资源：`ConfigId` 或 `ConfigIds`（数组）

### 标准流程（2步）

#### 步骤1: 分析需求并生成TXT
```python
# 1. 读取相关文件了解现有系统
readMultipleFiles(["现有配置表", "相关脚本"])

# 2. 生成TXT配置表到AI工作区
fsWrite("AI工作区/配置表/TableName.txt", content)
```

**TXT格式要求**：
```
#	TableName
#	ID		Field1	Field2	Field3
#	int		type1	type2	type3
#	ID编号	备注	说明1	说明2	说明3
	1		value1	value2	value3
```

**关键规则**：
- Tab分隔符（\t）
- 第3列固定为空
- 数据行第1列为空
- 元数据行第1列是#
- ID列在第2列
- ⚠️ **重要**：表名行（第1行）末尾不要有多余的Tab，否则会生成多余的空列
- ⚠️ **数组分隔符规则**：
  - **数组元素必须使用英文逗号 `,` 分隔**
  - **禁止使用中文顿号 `、` 或其他分隔符**
  - 正确示例：`1001,1002,1003` 或 `30001,30002,30003`
  - 错误示例：`1001、1002、1003`（会导致解析失败）
  - 适用于所有数组类型字段：`int[]`, `float[]`, `string[]` 等

#### 步骤2: 手动转换TXT为XLSX
```
用户使用外部工具（如Excel、Python脚本等）将生成的TXT文件转换为XLSX格式。
转换后的XLSX文件应保存到项目的 AAAGameData/DataTables/ 目录。
```

## 📋 TXT格式规范

### DataTableProcessor兼容格式
```
行索引  内容类型     DataTableProcessor参数
0      表名行      表名在第2列
1      字段名行    name_row=1
2      字段类型行  type_row=2
3      注释行      comment_row=3
4+     数据行      content_start_row=4, id_column=1
```

### 支持的数据类型
**基础类型**：int, float, double, string, bool, datetime
**Unity类型**：vector2, vector3, vector4, quaternion, color, rect
**数组类型**：int[], float[], string[], vector3[], 等
**二维数组**：int[,], float[,], 等

详细类型列表请参考：`DATA_TYPES.md`

### 数组列自动文本格式化
转换工具会自动识别数组类型的列（字段类型包含 `[]` 或 `[,]`），并将这些列设置为 Excel 的文本格式。

**优势**：
- 防止 Excel 自动格式化数组数据（如将 `2002,2006` 格式化为 `20,022,006`）
- 保持数组数据的原始格式
- 无需手动设置单元格格式

**示例**：
```
字段类型：int[]
数据：2001,2002,2003
结果：在 Excel 中保持为 "2001,2002,2003"（文本格式）
```

## 🔧 AI生成配置表模板

```
我来生成{表名}配置表，使用DataTableProcessor兼容格式。

📁 保存位置：`AI工作区/配置表/{表名}.txt`

字段设计：
- ID (int): 唯一标识
- {字段名} ({类型}): {说明}

TXT内容：
#	{TableName}													
#	ID		{Field1}	{Field2}
#	int		{Type1}	{Type2}
#	ID编号	备注	{说明1}	{说明2}
	1		{值1}	{值2}
```

## ⚠️ 故障排除

### Windows Bash环境问题
`executePwsh`工具存在工作目录管理问题，推荐使用 `controlPwshProcess` + 临时脚本方式。

详细解决方案请参考：`TROUBLESHOOTING.md`

### 常见问题
- **格式错误**：检查Tab分隔符和空列
- **转换失败**：确认UTF-8编码
- **服务器连接失败**：确认TCP服务器已启动

## ⚠️ 重要原则

### 核心职责
**这个SKILL的唯一职责：设计和生成符合项目规范的TXT格式配置表**

✅ **应该做的**：
1. 分析需求，设计配置表字段
2. 生成 TXT 配置表到 `AI工作区/配置表/`
3. 验证 TXT 格式的正确性
4. 提供配置表设计指导和最佳实践

❌ **不应该做的**：
1. **不负责 TXT 转 XLSX 的转换**（用户手动执行）
2. **不启动或管理 TCP 服务器**
3. **不调用 MCP 工具执行转换**
4. **不自动复制文件到项目其他目录**
5. **不假设用户的工作流程**

### 为什么这样设计？

**简化原则**：
- 这个SKILL专注于一件事：配置表的设计和TXT生成
- TXT 转 XLSX 的转换交给用户手动执行（使用Excel、Python脚本等）
- 降低工具的复杂度，提高可维护性

**用户工作流程**：
```
1. 使用此SKILL生成TXT文件到 AI工作区/配置表/
2. 用户手动使用外部工具转换 TXT → XLSX
3. 用户将XLSX文件复制到项目的 AAAGameData/DataTables/
4. 用户在Unity中运行 DataTableGenerator 生成代码
```

### 经验教训

1. **明确任务边界** - 工具的职责是设计和生成，不是部署
2. **避免过度复杂** - 不要集成太多功能，保持工具简单
3. **让用户决定** - 不要假设用户的后续操作

## 📚 相关文档

- `README.md` - 完整使用说明
- `DATA_TYPES.md` - 数据类型详细列表
- `TROUBLESHOOTING.md` - 故障排除指南
- `EXAMPLES.md` - 配置表示例集合
