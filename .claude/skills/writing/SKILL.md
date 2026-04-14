---
name: writing
description: "写作与图表工具集。当用户需要撰写学术论文、研究报告、技术文档，或创建 Mermaid 图表（类图、序列图、流程图、ER 图、C4 架构图等）时使用。触发场景：提到'论文'、'学术写作'、'研究报告'、'手稿'、'引用格式'、'IMRAD'、'图表'、'类图'、'序列图'、'流程图'、'架构图'、'Mermaid' 等。"
---

# Writing — 写作与图表

## 子技能列表

| 子技能 | 适用场景 | 参考文件 |
|--------|---------|---------|
| **paper-writing** | 学术论文撰写：框架构建、修改润色，支持从规划到最终定稿全阶段 | `references/paper-writing.md` |
| **scientific-writing** | 科学手稿深度写作：IMRAD 结构、多种引用格式（APA/AMA/Vancouver）、完整段落散文式 | `references/scientific-writing.md` |
| **mermaid-diagrams** | Mermaid 图表创建：类图、序列图、流程图、ER 图、C4 架构图、状态图、Git 图等 | `references/mermaid-diagrams.md` |

## 使用流程

1. 根据用户需求类型匹配子技能
2. 读取对应的 `references/*.md` 获取详细指令（文件较大，包含了所有参考资料）
3. 按照指令执行

## 选择指南

- **毕业论文/学术论文/研究文章** → paper-writing
- **科学手稿/期刊投稿/技术报告**（需要严格 IMRAD 结构和引用规范） → scientific-writing
- **软件图表/架构可视化/流程图** → mermaid-diagrams
