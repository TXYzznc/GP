---
paths: ["Assets/AAAGame/Scripts/DataTable/**/*.cs", "Assets/AAAGame/DataTable/**/*.txt", "AAAGameData/DataTables/**/*.xlsx"]
---

# DataTable 规则

## 核心原则

DataTable 下的 `.cs` 文件**全部由 DataTableGenerator 自动生成**，禁止手动修改。

如需扩展某个 DataTable，在 `Scripts/DataTablePartial/` 下创建同名 partial 类文件。

## 新增 DataTable 的流程

1. 在 `AAAGameData/DataTables/` 下创建或修改 `.xlsx` 文件
2. 第一行：注释（中文说明）
3. 第二行：字段名（英文，PascalCase）
4. 第三行：字段类型（`int`、`string`、`float`、`bool`、`int[]` 等）
5. 第四行：字段说明（中文）
6. 从第五行开始：数据行，第一列必须是 `Id`（int）
7. 在 Unity 编辑器中执行菜单：**GameFramework → DataTable → Generate**
8. 生成后检查 `Scripts/DataTable/` 下对应的 `.cs` 文件

## 字段类型速查

| 类型写法 | C# 类型 | 说明 |
|---------|---------|------|
| `int` | `int` | 整数 |
| `float` | `float` | 浮点数 |
| `bool` | `bool` | 布尔 |
| `string` | `string` | 字符串 |
| `int[]` | `int[]` | 整数数组，逗号分隔 |
| `string[]` | `string[]` | 字符串数组 |

## 读取 DataTable 的标准写法

```csharp
// 获取表
IDataTable<DRBuffTable> dtBuff = GF.DataTable.GetDataTable<DRBuffTable>();

// 按 ID 获取一行
DRBuffTable dr = dtBuff.GetDataRow(buffId);

// 遍历所有行
DRBuffTable[] allRows = dtBuff.GetAllDataRows();

// 按条件查找
DRBuffTable dr = dtBuff.GetDataRow(row => row.SomeField == someValue);
```

## 注意事项

- 生成的类名格式：`DR[TableName]`（如 `DRBuffTable`）
- 数组字段使用 `DataTableExtension.ParseArray<T>()` 解析
- 不要在 DataTable 行里存储 Unity 对象引用，只存 ID 或字符串路径
