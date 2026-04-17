> **最后更新**: 2026-04-17
> **状态**: 已验证有效（工具路径已更新）
> **分类**: 技术文档

---

# TXT 转 XLSX 工具

实时工具：将 DataTableProcessor 格式的 TXT 配置表快速转换为 XLSX。

---

## 工具位置

**当前路径**：`.Tools/COG-txt2xlsx/`

```
.Tools/COG-txt2xlsx/
├── txt_to_xlsx_converter.py  # 核心转换器类
├── txt_converter.py          # 通用转换客户端（如存在）
├── start_server.bat          # 便捷启动脚本（如需）
└── stop_server.bat           # 便捷停止脚本（如需）
```

**历史路径**（已弃用）：`.kiro/skills/txt-to-xlsx-converter/Tool/` ⚠️ 不要使用

---

## 使用方法

### 当前用法

从 `.Tools/COG-txt2xlsx/txt_to_xlsx_converter.py` 直接调用转换器。

在项目中的标准用法示例：

```python
# 转换单个 TXT 文件为 XLSX
from Tools.COG-txt2xlsx.txt_to_xlsx_converter import TxtToXlsxConverter

converter = TxtToXlsxConverter()
result = converter.convert(
    txt_file_path="Assets/AAAGame/DataTable/ItemTable.txt",
    output_path="Assets/AAAGame/DataTable/ItemTable.xlsx"
)
```

### 旧方法（已弃用）

以下方式已过时，**不要使用**：
- ❌ `server_manager.py` 启动 TCP 服务器
- ❌ `mcp_tool_call()` 通过 MCP 工具调用
- ❌ `MCP工作区/` 作为输出目录

**原因**：项目已统一将配置表转换纳入 DataTableGenerator 工具链，不再需要独立的转换服务器。

---

## 输出规范

**原则**：所有配置表的 XLSX 输出应保存到 `Assets/AAAGame/DataTable/` 目录。

```
Assets/AAAGame/DataTable/
├── ItemTable.txt        # 源配置表
├── ItemTable.xlsx       # 转换后的 XLSX
├── BuffTable.txt
├── BuffTable.xlsx
└── ...
```

---

## 性能参考

| 场景 | 转换时间 |
|------|---------|
| 小表（<100行） | <50 ms |
| 中表（100-1000行） | 50-200 ms |
| 大表（>1000行） | 200-500 ms |

---

## 注意事项

- `txt_to_xlsx_converter.py` 是核心转换器，不可删除
- 转换前请确保 TXT 格式符合 DataTable 规范（第2行字段名、第3行字段类型、第4行字段说明）
- 转换后的 XLSX 需通过 DataTableGenerator 处理，才能生成 .cs 类文件
