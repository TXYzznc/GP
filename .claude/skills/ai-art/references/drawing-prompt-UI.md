
# UI 元素提示词生成器

## 核心设计理念

Clash of Gods 的 UI 风格：**古典中式 + 黑暗奇幻金属感**，界面元素以深色为底，金色/铜色为装饰线，融入神话纹样（祥云、龙纹、符文），整体厚重庄严，带有史诗感。

---

## UI 元素分类

| 类型 | 典型元素 | 色调 | 用途 |
|------|---------|------|------|
| 主按钮 | 金边矩形/圆角，内光效 | 深棕底+金边，按下变暗 | 确认、进入 |
| 危险按钮 | 红色边框，警示感 | 深红底+血红光效 | 放弃、危险操作 |
| 面板/弹窗 | 木质/石质底纹，四角装饰 | 深棕、暗灰+金色边线 | 信息展示 |
| 边框装饰 | 龙纹、祥云、符文转角 | 金铜色、暗金 | 装饰性边框 |
| 血条/进度条 | 水晶质感，渐变填充 | 红→暗红（HP），蓝→深蓝（MP） | 战斗 HUD |
| 状态图标框 | 小圆形/方形底板 | 深色底+彩色边（根据效果） | Buff/Debuff |

---

## 提示词结构

```
[分辨率] [UI 元素类型] [功能描述]
Style: dark fantasy game UI, chinese mythological ornaments
Material: [材质描述：木质/石质/金属/水晶]
Color palette: [色彩代码]
Decorative elements: [装饰纹样描述]
State: [正常/高亮/按下/禁用 — 需要哪种状态]
Transparent background / [或具体背景色]
No border on the outside, internal decorative borders only
Clean edges suitable for Unity UI sprite slicing
Professional game UI art style, similar to Clash of Gods, [主题]
```

---

## 关键规范

- **透明背景**：UI 素材通常需要 `transparent background (PNG)`，方便 Unity 中直接使用
- **九宫格适配**：面板/按钮描述中加 `designed for 9-slice sprite (corners and edges tileable)`
- **尺寸清晰**：注明具体像素尺寸，如 `200x60 pixel button`
- **状态完整**：重要按钮最好说明需要 Normal/Hover/Pressed 三种状态

---

## 推理示例

**需求**：主界面确认按钮，金色风格

**推理**：
1. 类型→主按钮，确认操作，正向功能
2. 材质→深色金属底板 + 金色雕刻边框
3. 装饰→角落有龙纹，按钮中央有浅浮雕纹理
4. 状态→Normal 态（常亮金边）

```
400x100 pixel game UI button, confirm action button, dark fantasy style
Style: dark fantasy game UI, chinese mythological ornaments
Material: dark iron base plate with hammered texture, raised gold filigree border, subtle inner glow
Color palette: dark iron (#2A2A2A), antique gold (#B8860B), warm gold highlight (#FFD700), deep shadow (#111111)
Decorative elements: dragon scale pattern on corners, subtle cloud motif along edges, chinese knot embossed in center
State: normal state — gold border glowing softly, slight inner warmth
Transparent background (PNG)
No border on the outside, internal decorative borders only
Clean edges suitable for Unity UI sprite slicing
Designed for 9-slice sprite (corners and edges tileable)
Professional game UI art style, similar to Clash of Gods, golden confirm button theme
```
