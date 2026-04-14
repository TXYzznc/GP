# ColorTable 配置表实现步骤

> **最后更新**: 2026-04-01
> **状态**: 有效
> **版本**: 1.0

---

## 📋 当前状态
- ✅ `ColorTable.txt` 已创建（规范格式）
- 📍 位置：`项目知识库（AI）/配置表/ColorTable.txt`
- 📍 原始文件：`Assets/AAAGame/DataTable/ColorTable.txt`

## 🔄 实现步骤

### 步骤 1：转换 TXT 为 XLSX
**工具位置**：`.Tools/COG-txt2xlsx/run_converter_gui.bat`

**操作方式**：
1. 双击运行 `run_converter_gui.bat`
2. 在 GUI 中选择源文件：`项目知识库（AI）/配置表/ColorTable.txt`
3. 选择输出目录：`AAAGameData/DataTables/`
4. 点击转换按钮
5. 转换完成后，会生成 `ColorTable.xlsx`

**或使用命令行**：
```bash
python .Tools/COG-txt2xlsx/txt_to_xlsx_converter.py "项目知识库（AI）/配置表/ColorTable.txt" "AAAGameData/DataTables/ColorTable.xlsx"
```

### 步骤 2：运行 DataTableGenerator
1. 在 Unity Editor 中打开项目
2. 运行 DataTableGenerator（具体位置根据项目配置）
3. 生成 `ColorTable.cs` 和 `ColorTable.bytes`

### 步骤 3：修改 RarityColorHelper
**文件**：`Assets/AAAGame/Scripts/UI/Components/RarityColorHelper.cs`

**修改内容**：
- 从硬编码的颜色数组改为从 `ColorTable` 读取
- 使用 `ColorTable.GetRow(id)` 获取颜色数据
- 解析 `ColorHex` 字段转换为 Unity Color

**实现方式**：
```csharp
public static Color GetColor(int rarity)
{
    // 从 ColorTable 读取颜色
    var colorRow = ColorTable.GetRow(rarity);
    if (colorRow != null && !string.IsNullOrEmpty(colorRow.ColorHex))
    {
        return HexToColor(colorRow.ColorHex);
    }
    return DefaultBg;
}

private static Color HexToColor(string hex)
{
    // 将十六进制色值转换为 Unity Color
    // 例如：#61F34A → Color(0.38f, 0.95f, 0.29f, 1f)
}
```

## 📝 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 颜色唯一标识（1-8） |
| ColorName | string | 颜色名称（Green、Orange 等） |
| ColorHex | string | 十六进制色值（#RRGGBB 格式） |
| Description | string | 颜色描述/用途 |

## ✅ 验证清单

- [ ] ColorTable.xlsx 已生成到 `AAAGameData/DataTables/`
- [ ] DataTableGenerator 已运行，生成 `ColorTable.cs`
- [ ] `RarityColorHelper.cs` 已修改为从表读取
- [ ] 编译无错误
- [ ] 背包UI中稀有度背景色显示正确

## 🔗 相关文件

- 配置表 TXT：`项目知识库（AI）/配置表/ColorTable.txt`
- 转换工具：`.Tools/COG-txt2xlsx/`
- 颜色助手：`Assets/AAAGame/Scripts/UI/Components/RarityColorHelper.cs`
- 格子UI：`Assets/AAAGame/Scripts/UI/Item/InventorySlotUI.cs`
