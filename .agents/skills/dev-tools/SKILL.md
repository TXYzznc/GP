---
name: dev-tools
description: "开发辅助工具集。当用户需要创建 MCP Server、创建/改进 Skill、或为文档/幻灯片/报告应用主题样式时使用。触发场景：提到 'MCP'、'MCP Server'、'创建 skill'、'改进 skill'、'主题样式'、'theme'、'模板主题' 等。"
---

# Dev Tools — 开发辅助工具

## 子技能列表

| 子技能 | 适用场景 | 参考文件 |
|--------|---------|---------|
| **mcp-builder** | 创建 MCP (Model Context Protocol) Server，集成外部 API 和服务 | `references/mcp-builder.md` |
| **skill-creator** | 创建新 Skill、改进现有 Skill、运行评估测试 Skill 性能 | `references/skill-creator.md` |
| **theme-factory** | 为文档/幻灯片/报告/HTML 页面应用主题样式（10 个预设主题 + 自定义） | `references/theme-factory.md` |

## 使用流程

1. 根据用户需求匹配子技能
2. 读取对应的 `references/*.md` 获取详细指令
3. 按照指令执行

## 注意事项

- skill-creator 的支持文件（agents、scripts、eval-viewer 等）位于 `references/skill-creator-tools-*` 目录下
