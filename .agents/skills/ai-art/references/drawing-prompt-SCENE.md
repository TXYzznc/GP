
# 场景图提示词生成器

## 核心设计理念

Clash of Gods 的场景风格：**东方古典 + 黑暗奇幻**，世界观融合神话与腐化元素，场景兼具宏大壮观与压迫感。光影对比强烈，氛围浓郁。

---

## 场景类型分类

| 类型 | 典型元素 | 色调 | 氛围 |
|------|---------|------|------|
| 战斗场景 | 战场、废墟、血迹、硝烟 | 灰棕、暗红、焦黑 | 紧张、残酷 |
| 城镇/营地 | 建筑、篝火、旗帜、商贩 | 暖黄、木褐、灯光橙 | 安全感、生活气 |
| 自然荒野 | 森林、山脉、河流、荒原 | 深绿、土黄、天蓝 | 辽阔、危险 |
| 神秘遗迹 | 古柱、符文、腐化光效、雾气 | 紫灰、苔绿、幽光蓝 | 神秘、压迫 |
| 战棋棋盘 | 格子地形、区域标记、战略视角 | 根据地形变化 | 清晰、策略感 |

---

## 提示词结构

```
[分辨率] [场景类型] [地域/世界特征]
Key elements: [主要场景元素列表]
Color palette: [色彩代码]
Lighting: [光效/时间描述]
Atmosphere: [氛围关键词]
Background layers: [远景/中景/近景描述]
No border, no frame, no rounded corners, full bleed image filling the entire canvas
Professional game art style, similar to Clash of Gods, [场景主题]
```

---

## 推理示例

**需求**：战场遗址，远古神庙废墟，黄昏，有腐化能量

**推理**：
1. 场景类型→战斗+遗迹混合，黄昏时分
2. 元素→断裂的神庙柱子、散落的盔甲残骸、腐化紫色光芒从地面裂缝溢出
3. 色调→暗橙黄昏天空 + 腐化紫绿 + 焦黑废墟
4. 层次→远景：残破神庙轮廓；中景：战场废墟；近景：腐化裂缝和碎石

```
2048x1024 pixel game scene background, ancient battlefield ruins, dusk setting
Key elements: crumbling stone temple pillars, shattered armor scattered on ground, corruption energy seeping from ground cracks, dead withered trees, ravens circling
Color palette: dusk orange (#CC6600), corruption purple (#7B2FBE), char black (#1A1A1A), pale bone white (#D4C5A0)
Lighting: dramatic dusk backlighting, long dark shadows, purple-green corruption glow from ground
Atmosphere: desolate, ominous, ancient tragedy, corruption spreading
Background layers: distant - broken temple skyline against sunset; mid - littered battlefield with fallen warriors; foreground - glowing corruption cracks and debris
No border, no frame, no rounded corners, full bleed image filling the entire canvas
Professional game art style, similar to Clash of Gods, corrupted ancient battlefield theme
```
