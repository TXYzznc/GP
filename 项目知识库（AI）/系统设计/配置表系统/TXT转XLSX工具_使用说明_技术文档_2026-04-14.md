# txt-to-xlsx-converter 技能更新说明

> **最后更新**: 2026-03-23
> **状态**: 有效
> **更新时间**: 2026-03-05

## 📋 目录

- [更新内容](#更新内容)

---


### 📁 新增：统一输出路径规范

为了更好地管理生成的文件，现在所有通过 txt-to-xlsx-converter 技能生成的文件都会保存到统一的工作区目录。

### 主要变更

1. **新增 MCP工作区 文件夹**
   - 位置：项目根目录/MCP工作区/
   - 用途：存放所有生成的 TXT 和 XLSX 文件
   - 自动创建：如果不存在会自动创建

2. **更新 SKILL.md 文档**
   - 添加了详细的路径规范说明
   - 更新了 AI 使用指南，强调输出路径要求
   - 提供了完整的路径使用示例
   - 添加了文件组织建议

3. **路径规则**
   - 所有 TXT 文件必须生成到 `MCP工作区/` 或其子文件夹
   - 所有 XLSX 文件必须输出到 `MCP工作区/` 或其子文件夹
   - 支持在 `MCP工作区/` 内创建子文件夹进行分类管理
   - 调用 MCP 工具时必须使用绝对路径

### 推荐的文件组织方式

```
MCP工作区/
├── 配置表/
│   ├── ItemTable.txt
│   ├── ItemTable.xlsx
│   ├── SkillTable.txt
│   └── SkillTable.xlsx
├── 临时文件/
│   └── temp_data.txt
└── 其他分类/
    └── ...
```

### 使用示例

**创建工作区和子文件夹：**
```powershell
# 创建 MCP工作区
New-Item -ItemType Directory -Force -Path 'MCP工作区'

# 创建子文件夹
New-Item -ItemType Directory -Force -Path 'MCP工作区/配置表'
```

**生成文件到工作区：**
```python
# 生成 TXT 文件
fsWrite("MCP工作区/配置表/ItemTable.txt", content)

# 获取绝对路径
executePwsh("(Get-Item 'MCP工作区/配置表/ItemTable.txt').FullName")

# 调用转换工具
mcp_tool_call(
    tool="convert_txt_to_xlsx",
    arguments={
        "txt_file_path": "D:/项目路径/MCP工作区/配置表/ItemTable.txt",
        "output_dir": "D:/项目路径/MCP工作区/配置表"
    }
)
```

### 优势

1. **统一管理**：所有生成的文件集中在一个位置，便于查找和管理
2. **避免混乱**：不会在项目各处散落临时文件
3. **灵活组织**：支持子文件夹分类，可根据需求自由组织
4. **易于清理**：需要清理临时文件时，只需处理 MCP工作区 文件夹
5. **版本控制友好**：可以将 MCP工作区 添加到 .gitignore，避免提交临时文件

### 兼容性

- 此更新不影响现有功能
- 仅改变文件的默认输出位置
- 所有 MCP 工具功能保持不变
- DataTableProcessor 兼容性不受影响

### 注意事项

- 如果你之前有文件保存在其他位置，建议手动移动到 `MCP工作区/`
- `MCP工作区/` 文件夹已自动创建，包含一个 README.md 说明文件
- 建议定期备份重要的配置表文件
- 可以将 `MCP工作区/` 添加到 .gitignore（如果不需要版本控制）

### 相关文件

- `.kiro/skills/txt-to-xlsx-converter/SKILL.md` - 已更新的技能文档
- `MCP工作区/README.md` - 工作区说明文档
- `.kiro/skills/txt-to-xlsx-converter/Tool/mcp_server.py` - MCP 服务器（功能未变）
