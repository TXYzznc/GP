# 游戏角色管理界面 - AI绘图提示词

## 预制体结构分析

**布局类型**: 东方奇幻题材 RPG/自走棋游戏 - 角色管理UI

**主要组成部分**:
- 左侧：竖直滑动列表 - 显示已获得角色卡片网格
- 中央：角色展示区 - 3D模型或立绘查看器
- 右侧：信息面板 - 职业、属性、宝物、进阶、资料等标签页
- 配色：极暗背景 + 白色文本 + 彩色标签页强调

---

## 提示词版本 A：高质量游戏风格（推荐用Midjourney）

```
A professional RPG game character management UI screen with East Asian dark fantasy theme,
similar style to auto-battler games and oriental fantasy RPGs

LEFT PANEL (20% width):
- Vertical scrolling list showing 2-column × 5-row grid of character card portraits
- Each card: 145×212 pixels displaying a fantasy warrior/mage/archer character portrait
- Cards show large character head and upper body on card background
- Level badge displayed at bottom corner (e.g., "Lv.50")
- Card background: deep saturated blue (#1E3A8A) with bright cyan glowing border (#4A90E2)
- Selected/active card: enhanced golden glow (#FFD700) with 3px border
- Cards have rounded corners (8px radius) and subtle 20% transparent dark overlay
- Soft drop shadow below each card (2px offset)

CENTER PANEL (50% width):
- Large character display area with dark background for 3D model or portrait viewing
- Shows full-body standing pose of a fantasy character (warrior, mage, assassin, etc.)
- Background: dark brown gradient fading from #1a0f08 (top-left) to #2a1810 (bottom-right)
- Character portrait centered with soft lighting halos around silhouette
- Border: 2px solid orange-brown (#E2994A) with subtle inner glow effect
- Three control buttons positioned at bottom: [Model] [Portrait] [Fullscreen]
- Button style: dark background (#3A2A1A) with gold text (#DAA520) when active/hovered
- Subtle floating particle effects and light flares in dark background
- Slight lens distortion or depth-of-field effect around character

RIGHT PANEL (30% width):
- Character information and stats display with semi-transparent dark purple background
- Background color: dark purple (#2A1A3A) with 90% opacity
- Purple border (#A555A0) with 2px thickness and slight glow
- Header section: profession icon on left, character name in white text on right
- Four horizontal tab buttons below header: Attributes | Treasures | Advance | Story
- Active/selected tab: purple background (#4A3A6A) with hot pink text (#FF69B4)
- Inactive tabs: dark purple background (#2A1A3A) with muted purple-gray text
- Content scrolling area: displays stat information in organized rows
- Text styling: white (#FFFFFF) on dark background, 14-16px sans-serif font
- Right edge: thin dark vertical scrollbar (10px width, barely visible)

OVERALL VISUAL STYLE & ATMOSPHERE:
- Resolution: 1920×1080 pixels (16:9 widescreen cinematic aspect ratio)
- Color scheme: Very dark overall - primary background nearly black (#0A0A1A)
  - Accent colors: deep blue, cyan, orange-brown, hot pink, purple, gold
- Visual effects: Modern glass-morphism UI with subtle transparency, frosted glass appearance
- Lighting: Soft volumetric light rays, gentle edge-glow around panel borders
- Atmosphere: Floating particles, subtle animated glows, smooth transitions
- Border treatment: Rounded corners (8px), thin glowing outlines, 2px line weight
- Typography: Modern geometric sans-serif, high contrast against dark background
- Polish: AAA game quality, professional UI mockup, cinematic color grading
- Rendering: High fidelity, studio lighting, sharp UI with soft atmospheric background

Overall aesthetic: Professional game UI concept art, studio quality rendering,
polished AAA game production design, dramatic dark fantasy atmosphere,
neon accents on dark surfaces, cinematic lighting
```

---

## 提示词版本 B：精简版（用于其他AI绘图工具）

```
Dark fantasy RPG character management interface, auto-battler game style

Layout: Three-panel dashboard (1920×1080)
- Left panel (20%): Vertical scrolling character roster with portrait cards
- Center panel (50%): Full-body character model viewer with control buttons
- Right panel (30%): Stats and information panel with 4 tabbed sections

COLOR SCHEME:
- Main background: #0A0A1A (almost black, very dark)
- Left cards: #1E3A8A (deep blue) + #4A90E2 (bright cyan borders)
- Center frame: #2A1810 (dark brown) background, #E2994A (orange) border
- Right panel: #2A1A3A (dark purple) background, #A555A0 (purple) border
- Text: #FFFFFF (white) for normal, #FF69B4 (hot pink) for active tabs
- Accents: #DAA520 (warm gold), #4ADE80 (green) for tags

COMPONENT DETAILS:
- Character cards: 145×212px, rounded corners, semi-transparent overlay
- Level badges: white text in bottom corner of cards
- Card borders: glowing effect, enhanced on selection (#FFD700 gold)
- Buttons: dark background with gold text when active
- Tab buttons: 4 equal sections, pink highlight when selected
- Scrollbars: thin (10px), dark, barely visible

VISUAL EFFECTS:
- Glass-morphism transparency effect on panels
- Soft volumetric lighting and halos
- Floating particle effects in background
- Glowing edge borders on all panels
- Cinematic atmospheric lighting

STYLE: Professional AAA game UI, polished, modern, dark fantasy aesthetic
```

---

## 提示词版本 C：技术描述版（最精确）

```
RPG auto-battler game character roster UI mockup, 1920×1080px resolution, dark fantasy style:

TECHNICAL LAYOUT SPECIFICATION:
├─ Left section (20% width): Character roster scroll panel
│  ├─ Grid layout: 2 columns × 5 rows visible (scrollable vertically)
│  ├─ Card dimensions: 145px width × 212px height (16:9 portrait aspect)
│  ├─ Card background: rgba(30, 58, 138, 0.8) - semi-transparent dark blue
│  ├─ Card border: 2px solid #4A90E2 (bright cyan)
│  ├─ Selected/hover card: 3px solid #FFD700 (gold) with outer glow
│  ├─ Border radius: 8px on all corners
│  ├─ Card content: Character portrait image + level badge
│  ├─ Level badge: positioned bottom-right, white text, 12-14px font
│  ├─ Card shadow: 2px offset drop shadow, 20% opacity
│  └─ Scrollbar: 10px width, vertical orientation, dark color (#1A1A2E)
│
├─ Center section (50% width): Character display and controls
│  ├─ Background: linear gradient 135° from #1a0f08 (top-left) to #2a1810 (bottom-right)
│  ├─ Content: Full-body 3D character model or portrait, centered
│  ├─ Border: 2px solid #E2994A (orange-brown) with subtle inner glow effect
│  ├─ Inner glow color: rgba(226, 153, 74, 0.3) - orange transparent
│  ├─ Control buttons area: positioned at bottom, 3 buttons in horizontal row
│  │  ├─ Button 1: "Model" - Shows 3D game model
│  │  ├─ Button 2: "Portrait" - Shows character illustration
│  │  └─ Button 3: "Fullscreen" - Expands view
│  ├─ Button styling: 
│  │  ├─ Default: #3A2A1A (dark brown) background, #888888 text
│  │  ├─ Active: #3A2A1A (dark brown) background, #DAA520 (gold) text
│  │  ├─ Border: 1px solid #6B4423 (brown)
│  │  └─ Hover: slight brightness increase
│  └─ Effects: Floating particle veil (0.5-1% opacity), soft light halos
│
└─ Right section (30% width): Character information panel
   ├─ Background: rgba(42, 26, 58, 0.9) - dark purple, slightly transparent
   ├─ Outer border: 2px solid #A555A0 (purple) with subtle glow
   ├─ Corner radius: 8px
   ├─ Tab button row (below header): 4 buttons, equally distributed
   │  ├─ Button labels: "Attributes" | "Treasures" | "Advance" | "Story"
   │  ├─ Button height: 35px, full width divided by 4
   │  ├─ ACTIVE TAB:
   │  │  ├─ Background: #4A3A6A (purple)
   │  │  ├─ Text color: #FF69B4 (hot pink)
   │  │  ├─ Border-bottom: 2px solid #FF69B4 (pink underline)
   │  │  └─ Text weight: Bold (700)
   │  ├─ INACTIVE TABS:
   │  │  ├─ Background: #2A1A3A (dark purple, darker)
   │  │  ├─ Text color: #B0A0C0 (muted purple-gray)
   │  │  └─ Text weight: Regular (400)
   │  └─ Font size: 13-14px sans-serif
   ├─ Content scrolling area (below tabs):
   │  ├─ Height: remaining space in panel
   │  ├─ Background: #1A0A2A (very dark purple) or same as panel
   │  ├─ Content: Text-based stats display
   │  ├─ Text styling:
   │  │  ├─ Color: #FFFFFF (white) at 96% opacity
   │  │  ├─ Font family: Modern sans-serif (Segoe UI, Helvetica, etc.)
   │  │  ├─ Font size: 14-16px for labels, 13-15px for values
   │  │  ├─ Font weight: Bold (700) for labels, Regular (400) for values
   │  │  ├─ Text shadow: 1px 1px 2px rgba(0,0,0,0.8)
   │  │  └─ Line height: 1.6 (24px for 14px font)
   │  ├─ Text layout: Two-column or table format for stat pairs
   │  └─ Padding: 15px internal margin
   └─ Vertical scrollbar:
      ├─ Width: 10px
      ├─ Color: #555555 (barely visible on dark background)
      ├─ Thumb color: #888888
      └─ Track: transparent

COMPLETE COLOR PALETTE:
Primary: #0A0A1A (background), #1A1A2E, #1E3A8A (deep blue)
Secondary: #2A1810 (brown), #2A1A3A (dark purple), #2A3F5F
Accents: #3A2A1A (button bg), #3A2A3A, #4A3A6A (tab active)
Bright: #4A90E2 (cyan borders), #6B4423 (brown), #7A5A9A
Gold: #8B6F47, #DAA520 (warm gold), #E2994A (orange-brown)
Pink: #A555A0 (purple), #B0A0C0, #B0C4DE (pale purple), #FF69B4 (hot pink)
Text: #FFFFFF (white), #FFD700 (gold selection), #000000 @ 0.1-0.5 alpha

RENDERING NOTES:
- Aspect ratio: 16:9 (1920×1080)
- Lighting: Volumetric soft lighting, edge glow on panel borders
- Visual effects: Floating particles, transparency/glass-morphism, soft shadows
- Polish: AAA game quality, professional UI mockup, cinematic rendering
- Texture: Subtle noise or grain for visual depth
- Saturation: Slightly desaturated for dark fantasy mood
```

---

## 提示词版本 D：中文版（用于中文AI绘图工具）

```
RPG游戏角色管理界面，暗黑奇幻风格，自走棋游戏美学

整体布局：三面板设计，分辨率 1920×1080

左侧面板（20%宽度）：角色名册竖直滑动列表
- 网格布局：2列×5行可见的角色卡片（竖向可滚动）
- 卡片尺寸：145像素宽×212像素高，宽屏竖向比例
- 卡片背景：深蓝色（#1E3A8A），透明度80%
- 卡片边框：2像素青色（#4A90E2）发光边框
- 选中卡片：金色边框（#FFD700），3像素厚度，有发光效果
- 圆角：8像素
- 等级徽章：卡片右下角，白色小字显示"Lv.XX"
- 卡片阴影：2像素偏移，20%透明度
- 竖直滚条：10像素宽，深色，不显眼

中央面板（50%宽度）：角色模型查看器
- 背景：深棕色渐变，从#1a0f08（左上）到#2a1810（右下）
- 显示内容：幻想角色全身站立姿态，3D模型或精细插画
- 边框：2像素橙褐色（#E2994A），有柔和的内发光效果
- 控制按钮：底部3个按钮排成一行 - [模型] [立绘] [全屏]
- 按钮样式：深棕色背景（#3A2A1A），默认暗灰文本，激活时显示金色（#DAA520）
- 光效：角色周围有柔和的光晕，背景有细微浮动的粒子
- 深度效果：角色周围有轻微的景深模糊

右侧面板（30%宽度）：角色信息和属性显示
- 背景：深紫色（#2A1A3A），90%不透明度
- 边框：紫色（#A555A0），2像素，轻微发光
- 圆角：8像素
- 标签页区域（顶部）：4个等宽按钮 - [属性] [宝物] [进阶] [资料]
- 活跃标签页：紫色背景（#4A3A6A），热粉红色文字（#FF69B4），粗体
- 非活跃标签页：深紫色背景（#2A1A3A），暗紫灰色文字（#B0A0C0）
- 内容区域：可滚动的属性信息显示
- 文本样式：白色（#FFFFFF），96%不透明，现代无衬线字体
- 文本阴影：1像素1像素2像素的黑色阴影（80%透明）
- 行距：1.6倍
- 竖直滚条：10像素宽，深色，右边缘

色彩方案：
- 主色：极暗黑色（#0A0A1A）背景
- 蓝色系：#1E3A8A（深蓝）、#4A90E2（青蓝）
- 棕色系：#1a0f08、#2A1810、#3A2A1A
- 紫色系：#2A1A3A、#4A3A6A、#A555A0、#B0A0C0
- 强调色：#DAA520（温金色）、#E2994A（橙褐色）
- 亮色：#FF69B4（热粉红）、#FFD700（金色选择）
- 文字：#FFFFFF（白色主文本）

视觉效果：
- 玻璃态效果：面板具有微妙的透明度和毛玻璃外观
- 发光边框：面板周围有柔和的发光轮廓
- 浮动粒子：背景中有细微的粒子效果
- 光线效果：体积光、边缘光、场景光照
- 阴影：柔和的投影和内阴影

整体风格：
- 分辨率：1920×1080（16:9电影比例）
- 质感：AAA游戏品质，专业UI设计
- 光照：电影级别的光效，柔和的体积光
- 大气：暗黑奇幻，东方美学，现代科技感
- 质量：高保真渲染，工作室级别的质量，锐利的UI元素与柔和的环境对比

参考风格：类似原神、崩坏星穹铁道、鸣潮等游戏的角色管理界面设计
```

---

## 使用建议

### 🎨 **Midjourney 推荐参数**

```
/imagine [提示词版本A]
--ar 16:9 --niji 6 --quality 2
--style raw --no "realistic photograph"
```

### 🎨 **Stable Diffusion 推荐参数**

```
Prompt: [提示词版本A]
Negative: blurry, low quality, distorted UI, cartoon
Steps: 50
CFG Scale: 7.5
Sampler: DPM++ 2M Karras
```

### 🎨 **其他AI绘图工具 (DALL-E、Stable Diffusion等)**

直接使用**提示词版本B**的内容，或混合使用B+C的详细信息

---

## 🎨 **最简洁复制版（快速测试）**

```
RPG game character management UI, dark fantasy aesthetic, 
auto-battler game style, 1920×1080 resolution

Three-panel layout:
LEFT (20%): Character roster - 2×5 grid of blue cards with cyan borders, level badges
CENTER (50%): Full-body character display with dark brown gradient, orange border, control buttons
RIGHT (30%): Stats panel with purple background, 4 pink/purple tabs for Attributes/Treasures/Advance/Story

Colors: Very dark background (#0A0A1A), deep blue (#1E3A8A), cyan (#4A90E2), hot pink (#FF69B4), gold (#DAA520)
Effects: Glowing borders, glass-morphism transparency, floating particles, volumetric lighting
Style: AAA game quality, professional UI mockup, cinematic lighting, modern sans-serif font

References: Similar to Genshin Impact, Honkai Star Rail, or Wuthering Waves character management interfaces
```

---

## 预制体关键数据速查表

| 组件 | 尺寸 | 颜色 | 说明 |
|------|------|------|------|
| ChessImg | 145x212px | RGBA(1,1,1,1) | 角色立绘 |
| Scroll View | 100%×100% | RGBA(1,1,1,0.004) | 滑动容器 |
| Scrollbar | 10px宽 | RGB(1,1,1) | 竖直滚条 |
| occupationImage | - | RGB(0.27,0.27,0.27) | 职业图标 |
| UITitle | - | RGB(1,1,1,1) | 标题文本 |
| Content Text | - | RGB(0,0,0,1) | 属性文本内容 |

---

## 生成检查清单

生成后请确认包含以下要素：

- [ ] 左侧：5行×2列的角色卡片网格
- [ ] 每张卡片显示角色头像和等级标签
- [ ] 左侧面板采用深蓝色背景和青色边框
- [ ] 中央：大型角色3D模型显示区域
- [ ] 中央区域有"模型/立绘/全屏"三个控制按钮
- [ ] 右侧：信息面板，顶部有4个标签页（属性/宝物/进阶/资料）
- [ ] 右侧采用深紫色背景和粉红色活跃标签
- [ ] 整体采用暗色主题配色
- [ ] UI有轻微的发光/阴影效果
- [ ] 背景有细微的粒子效果
- [ ] 文本清晰可读，采用白色或金色
- [ ] 圆角边框和现代化设计风格
- [ ] 分辨率：1920×1080（16:9）
- [ ] 质感：AAA游戏级别，专业且抛光

---

## 额外建议

1. **参考游戏**：
   - 原神（Genshin Impact）- 角色管理界面
   - 崩坏：星穹铁道 - 角色展示
   - 鸣潮 - UI设计风格

2. **渲染优化**：
   - 使用最高质量设置
   - 启用光线追踪
   - 保留暗部细节

3. **后期处理**：
   - 适度增加对比度
   - 增强光晕效果
   - 修复UI文字清晰度

4. **迭代方向**：
   - 第一版：获得整体布局
   - 第二版：优化颜色和光效
   - 第三版：微调细节和特效
