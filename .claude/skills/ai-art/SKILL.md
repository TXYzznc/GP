---
name: ai-art
description: "AI 绘图提示词生成工具集。当用户需要生成 AI 绘图提示词、描述图片需求、或从参考图反推提示词时使用。覆盖类型：技能/Buff 图标（ICON）、角色立绘（CHARACTER）、场景图（SCENE）、UI 元素（UI）、以及从图片反推提示词。触发场景：用户提到'绘图提示词'、'画一个'、'图标设计'、'角色立绘'、'场景图'、'参考图分析'、'反推提示词'等。"
---

# AI Art — AI 绘图提示词生成

## 子技能列表

| 子技能 | 适用场景 | 参考文件 |
|--------|---------|---------|
| **drawing-prompt-generator** | 总入口/路由器：根据绘图需求自动判断类型并调用子模块 | `references/drawing-prompt-generator.md` |
| **drawing-prompt-CHARACTER** | 角色立绘提示词：根据角色职业、外貌、性格生成立绘/头像提示词 | `references/drawing-prompt-CHARACTER.md` |
| **drawing-prompt-ICON** | 技能/Buff 图标提示词：根据技能机制和效果描述生成图标提示词 | `references/drawing-prompt-ICON.md` |
| **drawing-prompt-SCENE** | 场景图提示词：根据场景类型、地域风格、氛围生成背景提示词 | `references/drawing-prompt-SCENE.md` |
| **drawing-prompt-UI** | UI 元素提示词：根据 UI 组件类型生成按钮、面板、边框等提示词 | `references/drawing-prompt-UI.md` |
| **image-to-prompt-generator** | 从参考图片逆向分析，反推出 AI 绘图提示词（输出 JSON） | `references/image-to-prompt-generator.md` |

## 使用流程

1. 先读取 `references/drawing-prompt-generator.md`（总入口），了解分类判断逻辑
2. 根据用户描述的绘图需求类型，读取对应子模块的参考文件
3. 如果用户提供了参考图片要反推提示词，读取 `references/image-to-prompt-generator.md`

## 选择指南

- 用户要**生成提示词**（描述需求） → 先读总入口，再按类型读子模块
- 用户提供**参考图片**要反推 → image-to-prompt-generator
- 不确定类型 → 读总入口，它会帮你判断
