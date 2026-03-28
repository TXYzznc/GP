---
name: drawing-prompt-generator:CHARACTER
description: |
  Clash of Gods 角色立绘 AI 绘图提示词生成器。
  根据角色职业、外貌描述、性格特征，生成符合游戏风格的立绘/头像提示词。
---

# 角色立绘提示词生成器

## 核心设计理念

Clash of Gods 的角色风格定位：**东方奇幻 + 黑暗史诗**，人物造型厚重有力，服饰融合神话元素，色彩饱和度高但不失沉稳。

---

## 职业视觉语言

| 职业 | 体型 | 服饰风格 | 主色调 | 气质 |
|------|------|---------|--------|------|
| 狂战士 | 魁梧、肌肉感 | 残破铠甲、皮革、战斗痕迹 | 血红、暗铁、焦橙 | 狂暴、嗜血 |
| 术士 | 修长、阴柔 | 长袍、符文刺绣、兜帽 | 深紫、幽蓝、黑金 | 神秘、腐化 |
| 混沌 | 不对称、异变 | 扭曲甲胄、混沌纹路 | 紫黑、腐绿、血色 | 癫狂、变异 |
| 德鲁伊 | 自然、流畅 | 藤蔓、皮毛、自然材料 | 深绿、棕褐、土黄 | 沉静、原始 |

---

## 提示词结构

```
[分辨率] [画面类型] [角色职业] [外貌描述]
Costume: [服饰描述]
Expression: [表情/神态]
Color palette: [主色调代码]
Lighting: [光效描述]
Background: [背景描述（立绘通常简洁）]
Art style: eastern fantasy, dark epic, high detail character illustration
No border, no frame, no rounded corners, full bleed image filling the entire canvas
Professional game art style, similar to Clash of Gods, [职业主题]
```

---

## 通用规范

- **立绘**：半身或全身，背景简洁（渐变或虚化场景），人物占画面 70% 以上
- **头像**：胸部以上，特写表情，背景极简
- **姿势**：有力量感的站姿或动态姿势，避免平淡的正面站立
- **细节**：武器/法器在画面中清晰可见，体现职业特征

---

## 推理示例

**需求**：狂战士召唤师，男性，头发凌乱，眼神狂暴

**推理**：
1. 职业→魁梧体型，残破血染铠甲，狂怒神态
2. 色彩→血红、暗铁灰，焦橙高光
3. 背景→简洁战场轮廓，烟尘虚化
4. 光效→来自下方的火焰光源，营造危险感

```
1024x1024 pixel character illustration, half-body portrait, berserker summoner male character
Wild disheveled dark hair, bloodshot manic eyes, scarred battle-worn face, fierce expression
Costume: shattered iron pauldrons, blood-stained leather straps, war runes carved into skin, crude fur cloak
Expression: wild battle rage, teeth gritted, veins visible on forehead
Color palette: blood red (#8B0000), iron gray (#4A4A4A), char black (#1A0000), ember orange (#FF4500)
Lighting: dramatic underlighting from fire below, casting harsh upward shadows on face
Background: blurred dark battlefield, smoke and embers, silhouettes of fallen warriors
Art style: eastern fantasy, dark epic, high detail character illustration
No border, no frame, no rounded corners, full bleed image filling the entire canvas
Professional game art style, similar to Clash of Gods, berserker warrior theme
```
