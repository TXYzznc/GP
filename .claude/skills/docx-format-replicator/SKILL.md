---
name: docx-format-replicator
description: 从现有Word文档中提取格式，并生成格式相同但内容不同的新文档。当用户需要创建格式一致的多份文档、复制文档模板，或在不同内容中保持企业文档标准时，可使用此功能。
tags: docx-format-extraction, document-generation, template-replication, batch-document-processing,
  python-docx
tags_cn: DOCX格式提取, 文档生成, 模板复制, 批量文档处理, python-docx工具
---

# DOCX 格式复制工具

## 概述

从现有Word文档（.docx）中提取格式信息，并使用该信息生成格式完全相同但内容不同的新文档。此功能可用于创建文档模板、在多份文档中保持一致格式，以及复制复杂的Word文档结构。

## 适用场景

当用户有以下需求时，可使用此功能：
- 想要从现有Word文档中提取格式
- 需要创建多份格式相同的文档
- 已有模板文档，想要生成内容不同的同类文档
- 要求“复制格式”“使用相同样式”“创建类似文档”
- 提及文档模板、企业标准或格式一致性

## 工作流程

### 步骤1：从模板中提取格式

从现有Word文档中提取格式信息，创建可复用的格式配置文件。

```bash
python scripts/extract_format.py <template.docx> <output.json>
```

**示例**:
```bash
python scripts/extract_format.py "HY研制任务书.docx" format_template.json
```

**提取内容**:
- 样式定义（字体、字号、颜色、对齐方式）
- 段落和字符样式
- 编号方案（1、1.1、1.1.1等）
- 表格结构和样式
- 页眉和页脚配置

**输出**：包含所有格式信息的JSON文件（详情请参阅`references/format_config_schema.md`）

### 步骤2：准备内容数据

创建包含新文档实际内容的JSON文件，内容结构需符合`references/content_data_schema.md`中的定义。

**内容结构**:
```json
{
  "metadata": {
    "title": "Document Title",
    "author": "Author Name",
    "version": "1.0",
    "date": "2025-01-15"
  },
  "sections": [
    {
      "type": "heading",
      "content": "Section Title",
      "level": 1,
      "number": "1"
    },
    {
      "type": "paragraph",
      "content": "Paragraph text content."
    },
    {
      "type": "table",
      "rows": 3,
      "cells": [
        ["Header 1", "Header 2"],
        ["Data 1", "Data 2"]
      ]
    }
  ]
}
```

**支持的章节类型**:
- `heading` - 可带编号的标题
- `paragraph` - 文本段落
- `table` - 可配置行列的表格
- `page_break` - 分页符

完整示例请参阅`assets/example_content.json`。

### 步骤3：生成新文档

使用提取的格式和准备好的内容生成新的Word文档。

```bash
python scripts/generate_document.py <format.json> <content.json> <output.docx>
```

**示例**:
```bash
python scripts/generate_document.py format_template.json new_content.json output_document.docx
```

**结果**：一个新的.docx文件，将模板中的格式应用到新内容上。

## 完整示例流程

用户提问：“我有一份研究任务文档，需要再创建5份格式相同但内容不同的文档。”

1. **提取格式**:
```bash
python scripts/extract_format.py research_task_template.docx template_format.json
```

2. **为每份新文档创建内容文件**（content1.json、content2.json等）

3. **生成文档**:
```bash
python scripts/generate_document.py template_format.json content1.json document1.docx
python scripts/generate_document.py template_format.json content2.json document2.docx
# ... 重复此步骤生成所有文档
```

## 常见使用场景

### 企业文档模板

从公司模板中提取格式，生成格式一致、带有品牌标识的报告、提案或规范文档。

```bash
# 一次性操作：提取公司模板格式
python scripts/extract_format.py "Company Template.docx" company_format.json

# 生成每份新文档时：
python scripts/generate_document.py company_format.json new_report.json "Monthly Report.docx"
```

### 技术文档系列

创建多份格式完全相同的技术文档（规范、测试计划、手册等）。

```bash
# 从规范模板提取格式
python scripts/extract_format.py spec_template.docx spec_format.json

# 生成多份规范文档
python scripts/generate_document.py spec_format.json product_a_spec.json "Product A Spec.docx"
python scripts/generate_document.py spec_format.json product_b_spec.json "Product B Spec.docx"
```

### 研究任务文档

附带的示例模板（`assets/hy_template_format.json`）展示了一份完整技术研究任务文档的格式，包括：
- 页眉中的审批/评审表格
- 多级编号（1、1.1、1.1.1）
- 技术规范表格
- 结构化章节

可将其作为同类技术文档的起点。

## 高级用法

### 自定义提取

修改`scripts/extract_format.py`以提取默认未覆盖的额外属性：
- 自定义XML元素
- 高级表格功能（合并单元格、边框）
- 嵌入对象
- 自定义属性

### 扩展内容类型

在`scripts/generate_document.py`中添加新的章节类型：
- 带标题的图片
- 项目符号或编号列表
- 脚注和尾注
- 自定义内容块

扩展指南请参阅`references/content_data_schema.md`。

### 批量处理

创建包装脚本以批量生成多份文档：

```python
import json
import subprocess

format_file = "template_format.json"
content_files = ["content1.json", "content2.json", "content3.json"]

for i, content_file in enumerate(content_files, 1):
    output = f"document_{i}.docx"
    subprocess.run([
        "python", "scripts/generate_document.py",
        format_file, content_file, output
    ])
```

## 依赖项

这些脚本需要：
- Python 3.7+
- `python-docx`库：`pip install python-docx`

核心功能无需其他额外依赖。

## 资源

### scripts/

- **extract_format.py** - 从Word文档中提取格式
- **generate_document.py** - 基于格式+内容生成新文档

两个脚本均内置帮助信息：
```bash
python scripts/extract_format.py --help
python scripts/generate_document.py --help
```

### references/

- **format_config_schema.md** - 格式配置文件的完整 schema
- **content_data_schema.md** - 内容数据文件的完整 schema

如需了解文件结构和可用选项的详细信息，请阅读这些文档。

### assets/

- **hy_template_format.json** - 从技术研究任务文档提取的示例格式
- **example_content.json** - 展示所有章节类型的示例内容数据

创建自己的格式和内容文件时，可将这些作为参考。

## 故障排除

**输出中缺少样式**：确保内容数据中的样式ID与格式配置中的一致。请查看`format.json`获取可用的样式ID。

**表格格式问题**：验证内容数据和格式配置中的表格尺寸（行/列）是否匹配。有关表格结构的详情，请参阅`format_config_schema.md`。

**字体显示不正确**：部分字体可能并非在所有系统上都可用，请检查引用的字体是否已安装。

**缺少依赖项**：安装所需的Python包：
```bash
pip install python-docx
```

## 提示

1. **先使用示例测试**：在提取自己的格式之前，先使用附带的`hy_template_format.json`和`example_content.json`了解工作流程。
2. **从简单开始**：先从基本标题和段落入手，再添加表格和复杂格式。
3. **验证JSON**：在生成文档之前，使用JSON验证工具检查内容数据文件。
4. **保存格式配置**：保存提取的格式配置，以便在多个项目中重复使用。
5. **版本控制**：将格式配置和内容数据纳入版本控制，以实现可复现的文档生成。