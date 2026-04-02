# 背包系统 ColorTable 配置开发总结

> **最后更新**: 2026-03-30
> **状态**: 有效
> **版本**: 1.0

---

## 时间范围
- 开始时间：2026-03-30
- 完成时间：2026-03-30

## 完成的任务

### 1. ColorTable 配置表设计与生成
- **文件**：`项目知识库（AI）/配置表/ColorTable.txt`
- **格式**：DataTableProcessor 标准格式
- **字段**：
  - Id (int) - 颜色唯一标识
  - ColorName (string) - 颜色名称
  - ColorHex (string) - 十六进制色值
  - Description (string) - 颜色描述/用途
- **数据**：8 种颜色配置（绿、橙、红、紫、白、黑、黄、灰）

### 2. TXT 转 XLSX 转换
- 使用 `.Tools/COG-txt2xlsx/run_converter_gui.bat` 工具
- 生成 `AAAGameData/DataTables/ColorTable.xlsx`
- 运行 DataTableGenerator 生成 `ColorTable.cs`

### 3. RarityColorHelper 重构
- **文件**：`Assets/AAAGame/Scripts/UI/Components/RarityColorHelper.cs`
- **改进**：
  - 从硬编码颜色数组改为从 ColorTable 配置表读取
  - 添加颜色缓存机制，避免频繁查表
  - 实现十六进制色值解析（#RRGGBB 格式）
  - 添加详细的日志输出（使用 DebugEx）
  - 提供 ClearCache() 方法用于配置表重新加载

## 技术要点

### 数据表访问方式
```csharp
// 获取数据表实例
var colorTable = GF.DataTable.GetDataTable<ColorTable>();

// 获取指定行数据
var colorRow = colorTable.GetDataRow(rarity);

// 访问字段
string colorHex = colorRow.ColorHex;
```

### 十六进制色值解析
- 支持格式：`#RRGGBB` 或 `RRGGBB`
- 转换为 Unity Color (0-1 范围)
- 示例：`#61F34A` → `Color(0.38f, 0.95f, 0.29f, 1f)`

### 性能优化
- 使用 Dictionary 缓存已解析的颜色
- 避免重复的十六进制解析操作
- 减少数据表查询次数

## 相关文件

| 文件 | 说明 |
|------|------|
| `项目知识库（AI）/配置表/ColorTable.txt` | 配置表 TXT 源文件 |
| `AAAGameData/DataTables/ColorTable.xlsx` | 转换后的 XLSX 文件 |
| `Assets/AAAGame/Scripts/DataTable/ColorTable.cs` | 生成的数据表类 |
| `Assets/AAAGame/Scripts/UI/Components/RarityColorHelper.cs` | 颜色助手类（已重构） |
| `Assets/AAAGame/Scripts/UI/Item/InventorySlotUI.cs` | 背包格子 UI（使用颜色） |

## 验证清单

- [x] ColorTable.txt 格式正确（第2列ID，第3列备注，第4列开始有效字段）
- [x] XLSX 转换成功
- [x] DataTableGenerator 生成 ColorTable.cs
- [x] RarityColorHelper 编译无错误
- [x] 十六进制色值解析逻辑正确
- [x] 缓存机制实现完整
- [x] 日志输出规范（使用 DebugEx）

## 下一步计划

1. **测试验证**：在 Unity 中运行游戏，验证背包格子的稀有度背景色显示是否正确
2. **继续实现**：
   - 3.7 实现左键点击格子显示详情面板
   - 3.8 实现右键点击弹出上下文菜单
   - 3.9 实现整理按钮功能

## 注意事项

- ColorTable 必须在游戏启动时加载，否则 GetDataTable 会返回 null
- 如果修改了 ColorTable 配置，需要调用 `RarityColorHelper.ClearCache()` 清空缓存
- 十六进制色值必须是有效的 6 位十六进制数，否则会输出错误日志并返回白色
