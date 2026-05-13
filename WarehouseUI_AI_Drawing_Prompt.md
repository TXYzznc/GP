# 游戏仓库UI - AI绘图提示词

## 预制体结构分析

**预制体路径**: `Assets/AAAGame/Prefabs/UI/WarehouseUI.prefab`

**布局类型**: RPG游戏 - 物品仓库/物品存储界面

**主要组成部分**:
- 背景蒙版 - 深灰蓝色半透明背景
- 仓库面板 - 白色半透明面板，包含标题和网格
- 物品网格 - 6列×5行的物品槽位网格（30个槽位）
- 控制按钮 - 关闭按钮、全部存储按钮
- 标题文本 - "仓库"

**配色方案**:
- 背景蒙版: RGB(0.08, 0.08, 0.12) = 深灰蓝色，78%透明度
- 面板背景: RGBA(1, 1, 1, 0.392) = 白色，39.2%透明度
- 标题文本: 白色 RGB(1, 1, 1)
- 按钮: 白色 RGB(1, 1, 1)
- 网格线框: 浅灰色

---

## 提示词版本 A：英文详细版（推荐用Midjourney）

```
A clean, minimal RPG game inventory storage interface UI mockup

OVERALL LAYOUT:
- Canvas resolution: 1920×1080 (16:9 widescreen)
- Centered modal dialog with semi-transparent dark background overlay
- Dark grayish-blue background (#14141F with 78% opacity) fills entire screen
- Semi-transparent white panel (#FFFFFF at 39% opacity) as main container

MAIN PANEL:
- Background: semi-transparent white, slightly frosted glass effect
- Border: subtle light gray outline, 2px thickness
- Rounded corners: 8px
- Padding: 20px internal margin
- Title bar: "warehouse" or "仓库" in white text, centered at top
- Title position: 40px from top of panel
- Title font: bold sans-serif, 20-24px size

INVENTORY GRID LAYOUT:
- Grid structure: 6 columns × 5 rows = 30 inventory slots total
- Each slot: square grid cells, approximately 100×100 pixels
- Slot background: light gray (#E8E8E8) with subtle texture
- Slot borders: 1px light gray dividing lines between cells
- Empty slots shown with subtle background gradient
- Item placeholder icons centered in each slot
- Grid spacing: uniform gaps between cells (4-6px)
- Grid area width: approximately 600px
- Grid area height: approximately 500px
- Grid positioned: below title, with 20px top margin

CONTROL BUTTONS:
- Bottom section: 20px from bottom of panel
- Button 1 - "Store All" / "全部存储" (left side)
  * Background: light gray (#E0E0E0) or white with outline
  * Text: dark gray (#333333)
  * Size: 120×40px
  * Rounded corners: 4px
  * Hover state: slightly brighter background
  
- Button 2 - "Close" / "关闭" (right side)
  * Background: light gray (#E0E0E0) or white with outline
  * Text: dark gray (#333333)
  * Size: 120×40px
  * Rounded corners: 4px
  * Position: bottom-right corner

VISUAL EFFECTS:
- Semi-transparent overlay: dark grayish-blue (#0F0F18 with reduced opacity)
- Panel transparency: frosted glass effect with slight blur
- Grid lines: subtle dividing lines, 1px thickness
- Lighting: soft, even illumination with no harsh shadows
- Texture: minimal, clean appearance

COLOR PALETTE:
- Background overlay: #0F0F18 (dark grayish-blue), 78% opacity
- Panel: #FFFFFF (white), 39% opacity
- Title text: #FFFFFF (white)
- Grid cells: #E8E8E8 (light gray)
- Grid borders: #CCCCCC (light gray)
- Button text: #333333 (dark gray)
- Button background: #E0E0E0 (light gray) or white with 1px border

TYPOGRAPHY:
- Title: Bold, 20-24px, white, centered
- Button text: Regular, 14px, dark gray, centered
- Font family: Modern sans-serif (Helvetica, Arial, or similar)

OVERALL AESTHETIC:
- Style: Clean, minimal, professional game UI
- Theme: Light theme with dark overlay
- Quality: AAA game UI mockup, high polish, professional appearance
- Atmosphere: Organized, functional, user-friendly
- Rendering: Clean vector-like appearance, sharp UI elements

REFERENCE STYLE:
Similar to modern RPG game inventory systems (Final Fantasy, Skyrim, World of Warcraft UI style)
Focus on clarity and organization for storage management interface
```

---

## 提示词版本 B：英文精简版

```
Game warehouse/storage inventory UI interface

Layout: 1920×1080 centered modal dialog

Background: Dark grayish-blue overlay (#0F0F18), 78% opacity over entire screen

Main Panel:
- White background (#FFFFFF), 39% opacity
- Rounded corners (8px)
- 20px padding
- Frosted glass effect

Title: "Warehouse" in bold white text, top center, 20-24px font

Content: 6×5 inventory grid (30 slots)
- Square cells, light gray backgrounds (#E8E8E8)
- 1px dividing lines between cells
- Empty slot placeholders
- Uniform spacing

Buttons at bottom:
- "Store All" button (left) - light gray background, dark text
- "Close" button (right) - light gray background, dark text
- Size: 120×40px each
- Rounded corners (4px)

Colors:
- Overlay: #0F0F18 (dark grayish-blue)
- Panel: #FFFFFF (white, semi-transparent)
- Text: #FFFFFF (white for title)
- Grid: #E8E8E8 (light gray cells)
- Buttons: #E0E0E0 (light gray background)

Style: Clean, minimal, professional RPG inventory interface
Quality: AAA game UI mockup, high polish, organized storage design
```

---

## 提示词版本 C：中文详细版

```
RPG游戏仓库物品存储界面UI设计

整体布局：
- 分辨率：1920×1080（16:9宽屏）
- 居中的模态对话框，覆盖整个屏幕的半透明深色背景
- 背景蒙版：深灰蓝色（RGB: 0.08, 0.08, 0.12 = #14141F），78%透明度
- 主面板：白色（RGB: 1, 1, 1），39.2%透明度，呈现半透明玻璃态效果

仓库面板：
- 背景：半透明白色，具有毛玻璃效果
- 边框：轻微的浅灰色轮廓，2像素厚度
- 圆角：8像素半径
- 内边距：20像素
- 标题栏：顶部居中显示"仓库"，白色文本，22像素粗体字体

物品网格布局：
- 网格结构：6列×5行 = 30个物品槽位
- 每个槽位：正方形网格单元，约100×100像素
- 槽位背景：浅灰色（#E8E8E8），带有细微纹理
- 槽位边框：1像素浅灰色分割线
- 空槽位：显示占位符图标，居中
- 网格间距：均匀分布，单元格之间4-6像素的间隙
- 网格宽度：约600像素
- 网格高度：约500像素
- 网格位置：标题下方，顶部间距20像素

控制按钮（位于面板底部，距离底部20像素）：
1. 全部存储按钮（左侧）
   - 背景颜色：浅灰色（#E0E0E0）或白色带边框
   - 文本颜色：深灰色（#333333）
   - 尺寸：120×40像素
   - 圆角：4像素
   - 悬停状态：背景略微变亮

2. 关闭按钮（右侧）
   - 背景颜色：浅灰色（#E0E0E0）或白色带边框
   - 文本颜色：深灰色（#333333）
   - 尺寸：120×40像素
   - 圆角：4像素
   - 位置：右下角

视觉效果：
- 半透明蒙版：深灰蓝色（#0F0F18）覆盖背景
- 面板透明效果：毛玻璃质感，轻微模糊效果
- 网格线条：细微分割线，1像素厚度
- 光照：柔和均匀的照明，无刺眼阴影
- 纹理：最小化设计，清爽干净的外观

色彩方案：
- 背景蒙版：#0F0F18（深灰蓝），78%透明度
- 面板背景：#FFFFFF（白色），39%透明度
- 标题文本：#FFFFFF（白色）
- 网格单元：#E8E8E8（浅灰）
- 网格边框：#CCCCCC（浅灰）
- 按钮文本：#333333（深灰）
- 按钮背景：#E0E0E0（浅灰）或白色带1像素边框

字体排版：
- 标题：粗体，22像素，白色，居中
- 按钮文本：常规，14像素，深灰，居中
- 字体家族：现代无衬线字体（Helvetica、Arial或类似）

整体美学：
- 风格：清洁、极简、专业的游戏UI设计
- 主题：浅色主题配以深色叠层
- 质量：AAA游戏级别的UI设计，高度抛光，专业外观
- 气氛：有组织、功能性强、用户友好
- 渲染：矢量化外观，锐利的UI元素

参考游戏：
类似现代RPG游戏的物品系统（最终幻想、上古卷轴、魔兽世界UI风格）
强调清晰度和组织性以便物品管理和存储
```

---

## 提示词版本 D：中文精简版

```
RPG游戏仓库存储界面UI

布局：1920×1080居中模态对话框

背景：深灰蓝色蒙版（#14141F），78%透明度覆盖全屏

主面板：
- 白色背景（#FFFFFF），39%透明度
- 圆角8像素
- 内边距20像素
- 毛玻璃效果

标题：顶部居中"仓库"，粗体白色，22像素

内容：6×5物品网格（30个槽位）
- 正方形单元格，浅灰色背景（#E8E8E8）
- 1像素分割线
- 空槽位占位符
- 均匀间距

底部按钮：
- 全部存储按钮（左侧）- 浅灰背景，深灰文字
- 关闭按钮（右侧）- 浅灰背景，深灰文字
- 尺寸：120×40像素
- 圆角4像素

色彩：
- 蒙版：#0F0F18（深灰蓝）
- 面板：#FFFFFF（白色，半透明）
- 文字：#FFFFFF（白色标题）
- 网格：#E8E8E8（浅灰单元格）
- 按钮：#E0E0E0（浅灰背景）

风格：清洁、极简、专业的RPG物品存储界面
质量：AAA游戏级UI设计，高度抛光，有组织感
```

---

## 快速对比表

| 特征 | 数值/说明 |
|------|----------|
| **分辨率** | 1920×1080 (16:9) |
| **背景蒙版色** | #14141F (深灰蓝) |
| **蒙版透明度** | 78% |
| **面板色** | #FFFFFF (白色) |
| **面板透明度** | 39.2% |
| **网格结构** | 6列 × 5行 = 30槽位 |
| **网格单元尺寸** | 约100×100像素 |
| **栅格线条** | 1像素浅灰色 |
| **圆角半径** | 8像素（面板）, 4像素（按钮） |
| **标题文字** | 粗体、白色、22px |
| **按钮数量** | 2个 (全部存储、关闭) |
| **按钮尺寸** | 120×40像素 |
| **内边距** | 20像素 |
| **网格单元色** | #E8E8E8（浅灰） |
| **按钮背景** | #E0E0E0（浅灰）或白色 |

---

## 使用建议

### 🎨 **Midjourney 参数**

```
/imagine [使用版本A全文]
--ar 16:9 --niji 6 --quality 2 --no "dark, gritty"
```

### 🎨 **DALL-E / Stable Diffusion**

使用版本B或C，不需要特殊参数

### 🎨 **中文AI绘图工具（墨竹、Lisa等）**

使用版本C或D

---

## 生成检查清单

✅ 检查以下要素是否完整：

- [ ] 1920×1080分辨率，16:9宽屏
- [ ] 深灰蓝色半透明背景蒙版覆盖全屏
- [ ] 中央白色半透明面板（39%透明度）
- [ ] 毛玻璃/frosted glass效果
- [ ] 顶部标题"仓库"（白色文本）
- [ ] 6列×5行物品网格（30个槽位）
- [ ] 浅灰色网格单元格
- [ ] 1像素的网格分割线
- [ ] 底部两个按钮（全部存储、关闭）
- [ ] 按钮为浅灰色背景，深灰文字
- [ ] 圆角设计（面板8px、按钮4px）
- [ ] 清洁、极简的专业UI风格
- [ ] 无复杂装饰，功能导向设计

---

## 优化建议

如果生成的结果需要调整：

**需要更清晰**：
- 加入：`sharp crisp grid lines, clean interface, high contrast`

**需要更有科技感**：
- 加入：`futuristic UI, neon accents, tech aesthetic`

**需要更温暖/友好**：
- 替换：`warm light colors, friendly appearance`

**需要更暗黑**：
- 替换：`dark background theme, low-key lighting`

