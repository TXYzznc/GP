# ColorTable 配置表实现步骤

> **最后更新**: 2026-04-17
> **状态**: 有效
> **版本**: 1.1

---

## 📋 当前状态
- ✅ `ColorTable.txt` 已创建（规范格式）
- 📍 配置表源文件：`Assets/AAAGame/DataTable/ColorTable.txt`
- ✅ 已生成 ColorTable.cs（自动生成类）
- ✅ RarityColorHelper 已实现从表读取逻辑

## 🔄 配置表结构

ColorTable.txt 配置如下：

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| Id | int | 颜色唯一标识 | 1-8 |
| ColorName | string | 颜色名称 | Green、Orange、Red、Purple 等 |
| ColorHex | string | 十六进制色值 | #61F34A（支持#RGB、#RRGGBB、#RGBA、#RRGGBBAA 格式） |
| Description | string | 颜色用途描述 | 稀有度1-绿色（普通物品） |

## 📖 使用方法

### 方法 1：通过 RarityColorHelper 获取颜色
**文件**：`Assets/AAAGame/Scripts/UI/Components/RarityColorHelper.cs`

最便捷的使用方式——已实现从 ColorTable 自动读取+缓存：

```csharp
// 根据稀有度获取颜色
Color rarityColor = RarityColorHelper.GetColor(rarityId);

// 色值转换已在 HexToColor() 中处理
// 支持格式：#RRGGBB（带#符号）
// 返回 Color(r/255, g/255, b/255, 1f)
```

### 方法 2：直接查表（高级用法）

如需手动读表：

```csharp
// 获取 ColorTable 实例
IDataTable<ColorTable> colorTable = GF.DataTable.GetDataTable<ColorTable>();

// 按 ID 获取一行
ColorTable colorRow = colorTable.GetDataRow(colorId);

// 使用字段
string hex = colorRow.ColorHex;
string name = colorRow.ColorName;
string desc = colorRow.Description;
```

### 色值转换规则

- 输入格式：#RRGGBB（如 #61F34A）
- 输出：Color(0.38f, 0.95f, 0.29f, 1f)
- 计算：R=0x61/255=97/255=0.38f，G=0xF3/255=243/255=0.95f，B=0x4A/255=74/255=0.29f

## ✅ 实现验证清单

- ✅ ColorTable.cs 已生成（自动生成，位置：`Assets/AAAGame/Scripts/DataTable/ColorTable.cs`）
- ✅ RarityColorHelper 已实现完整的表读取+缓存逻辑
- ✅ 支持 #RGB/#RRGGBB/#RGBA/#RRGGBBAA 格式的十六进制色值
- ✅ 包含 HexToColor() 转换方法和颜色缓存机制
- ✅ 包含 ClearCache() 方法用于重新加载时清空缓存

## 🚨 可能过时的内容

**旧版本中提及的 TXT→XLSX 转换工具路径已移除**：
- 不再需要手动运行 `run_converter_gui.bat`
- 配置表更新流程已标准化：编辑 ColorTable.txt → 通过 DataTableGenerator 自动处理

**建议**：直接编辑 `Assets/AAAGame/DataTable/ColorTable.txt`，然后在 Unity Editor 中运行 GameFramework 菜单的 DataTable Generator 重新生成。

## 🔗 相关文件

- **配置表源**：`Assets/AAAGame/DataTable/ColorTable.txt`
- **生成类**：`Assets/AAAGame/Scripts/DataTable/ColorTable.cs`（自动生成，禁止手改）
- **颜色助手**：`Assets/AAAGame/Scripts/UI/Components/RarityColorHelper.cs`（已实现完整逻辑）
