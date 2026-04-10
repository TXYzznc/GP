"""生成 DictionariesUI 界面布局 mockup PNG"""
from PIL import Image, ImageDraw, ImageFont
import os

W, H = 1920, 1080
BG = (18, 18, 28)
PANEL_BG = (25, 25, 40)
DARK_BG = (30, 30, 48)
GOLD = (212, 175, 85)
GOLD_DIM = (140, 115, 55)
WHITE = (240, 240, 240)
GRAY = (120, 120, 140)
DIM = (60, 60, 80)
ACCENT = (80, 65, 35)
SELECTED_BG = (55, 45, 25)
SLOT_BG = (35, 35, 55)
SLOT_BORDER = (55, 55, 80)
LOCK_BG = (20, 20, 30, 180)
QUALITY_COLORS = {
    "blue": (70, 130, 220),
    "purple": (160, 80, 200),
    "gold": (220, 180, 60),
    "green": (80, 180, 80),
    "gray": (100, 100, 120),
}

img = Image.new("RGBA", (W, H), BG)
draw = ImageDraw.Draw(img)

# Try to load a font
try:
    font_paths = [
        "C:/Windows/Fonts/msyh.ttc",
        "C:/Windows/Fonts/simhei.ttf",
        "C:/Windows/Fonts/simsun.ttc",
    ]
    font_path = None
    for fp in font_paths:
        if os.path.exists(fp):
            font_path = fp
            break

    if font_path:
        font_title = ImageFont.truetype(font_path, 32)
        font_large = ImageFont.truetype(font_path, 22)
        font_normal = ImageFont.truetype(font_path, 16)
        font_small = ImageFont.truetype(font_path, 13)
        font_big_title = ImageFont.truetype(font_path, 40)
    else:
        raise Exception("No font")
except:
    font_title = ImageFont.load_default()
    font_large = font_title
    font_normal = font_title
    font_small = font_title
    font_big_title = font_title


def draw_rounded_rect(draw, xy, fill, radius=8, outline=None):
    x0, y0, x1, y1 = xy
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline)


def draw_text_centered(draw, xy, text, font, fill):
    x, y, w, h = xy
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text((x + (w - tw) / 2, y + (h - th) / 2), text, font=font, fill=fill)


# ===== 半透明背景覆盖 =====
overlay = Image.new("RGBA", (W, H), (0, 0, 0, 160))
img = Image.alpha_composite(img, overlay)
draw = ImageDraw.Draw(img)

# ===== 左侧分类栏 (220px) =====
LEFT_W = 220
draw_rounded_rect(draw, (0, 0, LEFT_W, H), fill=(20, 20, 35), radius=0)

# 左侧标题
draw.text((40, 30), "图  鉴", font=font_big_title, fill=GOLD)

# 分隔线
draw.line((20, 90, LEFT_W - 20, 90), fill=GOLD_DIM, width=1)

# 分类Tab
categories = [
    ("棋子", "12/20", True),
    ("策略卡", "8/15", False),
    ("敌人", "3/10", False),
    ("装备", "5/12", False),
    ("宝物", "2/8", False),
    ("消耗品", "6/10", False),
    ("任务道具", "4/6", False),
]

tab_y = 110
tab_h = 58
for i, (name, count, selected) in enumerate(categories):
    y = tab_y + i * (tab_h + 6)
    bg = SELECTED_BG if selected else (25, 25, 40)
    outline_c = GOLD_DIM if selected else (40, 40, 60)

    draw_rounded_rect(draw, (12, y, LEFT_W - 12, y + tab_h), fill=bg, radius=6, outline=outline_c)

    if selected:
        # 左侧金色指示条
        draw_rounded_rect(draw, (12, y + 8, 16, y + tab_h - 8), fill=GOLD, radius=2)

    name_color = GOLD if selected else WHITE
    draw.text((30, y + 10), name, font=font_large, fill=name_color)
    draw.text((30, y + 34), count, font=font_small, fill=GRAY)

# 底部总进度
prog_y = H - 100
draw.line((20, prog_y - 10, LEFT_W - 20, prog_y - 10), fill=(40, 40, 60), width=1)
draw.text((20, prog_y), "总收集进度", font=font_normal, fill=GRAY)
draw.text((20, prog_y + 24), "40 / 81", font=font_large, fill=WHITE)

# 进度条
bar_y = prog_y + 55
draw_rounded_rect(draw, (20, bar_y, LEFT_W - 20, bar_y + 8), fill=(40, 40, 60), radius=4)
prog_w = int((LEFT_W - 40) * 40 / 81)
draw_rounded_rect(draw, (20, bar_y, 20 + prog_w, bar_y + 8), fill=GOLD, radius=4)

# ===== 主内容区 =====
MAIN_X = LEFT_W + 10
MAIN_W = W - LEFT_W - 20  # 不含详情面板时
DETAIL_W = 340

# 顶部工具栏
toolbar_y = 15
draw.text((MAIN_X + 20, toolbar_y + 5), "棋子", font=font_title, fill=WHITE)

# 分类进度
cat_prog_x = MAIN_X + 120
draw.text((cat_prog_x, toolbar_y + 12), "12 / 20", font=font_normal, fill=GRAY)

# 分类进度条
cpb_x = cat_prog_x + 80
cpb_y = toolbar_y + 18
draw_rounded_rect(draw, (cpb_x, cpb_y, cpb_x + 200, cpb_y + 6), fill=(40, 40, 60), radius=3)
draw_rounded_rect(draw, (cpb_x, cpb_y, cpb_x + 120, cpb_y + 6), fill=GOLD, radius=3)

# 关闭按钮
close_x = W - 50
close_y = 15
draw_rounded_rect(draw, (close_x, close_y, close_x + 36, close_y + 36), fill=(60, 30, 30), radius=6)
draw.text((close_x + 10, close_y + 5), "✕", font=font_large, fill=(220, 100, 100))

# ===== 网格区域 =====
grid_x = MAIN_X + 20
grid_y = toolbar_y + 60
cell_w, cell_h = 140, 186
spacing = 12
cols = 5

# 棋子数据模拟
chess_items = [
    ("烈焰狮王", "★3", "gold", True),
    ("冰霜巨龙", "★3", "gold", True),
    ("暗影刺客", "★2", "purple", True),
    ("雷霆法师", "★2", "purple", True),
    ("圣光骑士", "★2", "purple", True),
    ("森林猎手", "★1", "blue", True),
    ("海洋祭司", "★1", "blue", True),
    ("大地守卫", "★1", "blue", True),
    ("风暴使者", "★1", "blue", True),
    ("月影忍者", "★2", "purple", True),
    ("星辰魔导", "★3", "gold", True),
    ("血族领主", "★2", "purple", True),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
    ("???", "", "gray", False),
]

for idx, (name, star, quality, unlocked) in enumerate(chess_items):
    row = idx // cols
    col = idx % cols
    cx = grid_x + col * (cell_w + spacing)
    cy = grid_y + row * (cell_h + spacing)

    if cy + cell_h > H - 10:
        break

    # 格子背景
    slot_fill = SLOT_BG if unlocked else (22, 22, 32)
    draw_rounded_rect(draw, (cx, cy, cx + cell_w, cy + cell_h), fill=slot_fill, radius=8, outline=SLOT_BORDER)

    if unlocked:
        # 品质背景色条（底部3px）
        qcolor = QUALITY_COLORS.get(quality, GRAY)
        draw_rounded_rect(draw, (cx + 2, cy + cell_h - 5, cx + cell_w - 2, cy + cell_h - 2), fill=qcolor, radius=2)

        # 模拟图标区域（大色块）
        icon_margin = 15
        icon_size = cell_w - icon_margin * 2
        icon_y = cy + 12
        qcolor_dim = tuple(max(0, c - 60) for c in qcolor)
        draw_rounded_rect(
            draw,
            (cx + icon_margin, icon_y, cx + icon_margin + icon_size, icon_y + icon_size),
            fill=qcolor_dim,
            radius=6,
        )
        # 图标中心文字
        draw_text_centered(
            draw,
            (cx + icon_margin, icon_y, icon_size, icon_size),
            name[0],
            font=font_title,
            fill=(*qcolor, 200),
        )

        # 名称
        name_y = cy + cell_h - 50
        bbox = draw.textbbox((0, 0), name, font=font_normal)
        nw = bbox[2] - bbox[0]
        draw.text((cx + (cell_w - nw) / 2, name_y), name, font=font_normal, fill=WHITE)

        # 星级
        if star:
            bbox2 = draw.textbbox((0, 0), star, font=font_small)
            sw = bbox2[2] - bbox2[0]
            draw.text((cx + (cell_w - sw) / 2, name_y + 20), star, font=font_small, fill=GOLD)
    else:
        # 未解锁：暗色 + 锁图标
        lock_overlay = Image.new("RGBA", (cell_w, cell_h), (15, 15, 22, 200))
        img.paste(Image.alpha_composite(Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0)), lock_overlay), (cx, cy))
        draw = ImageDraw.Draw(img)

        # 锁图标
        lock_cx = cx + cell_w // 2
        lock_cy = cy + cell_h // 2 - 15
        draw.text((lock_cx - 10, lock_cy - 12), "🔒", font=font_large, fill=DIM)

        # ???
        draw.text((cx + cell_w // 2 - 15, cy + cell_h - 45), "???", font=font_normal, fill=DIM)

# ===== 右侧详情面板 =====
detail_x = W - DETAIL_W - 10
detail_y = 10
detail_h = H - 20

draw_rounded_rect(
    draw, (detail_x, detail_y, detail_x + DETAIL_W, detail_y + detail_h), fill=(22, 22, 38), radius=10, outline=(50, 50, 70)
)

# 详情关闭按钮
dcx = detail_x + DETAIL_W - 40
draw_rounded_rect(draw, (dcx, detail_y + 8, dcx + 30, detail_y + 38), fill=(50, 30, 30), radius=4)
draw.text((dcx + 7, detail_y + 11), "✕", font=font_normal, fill=(200, 80, 80))

# 大图标背景
icon_bg_y = detail_y + 30
icon_bg_size = 200
icon_bg_x = detail_x + (DETAIL_W - icon_bg_size) // 2
draw_rounded_rect(
    draw,
    (icon_bg_x, icon_bg_y, icon_bg_x + icon_bg_size, icon_bg_y + icon_bg_size),
    fill=(40, 35, 20),
    radius=12,
    outline=GOLD_DIM,
)
# 图标文字
draw_text_centered(
    draw, (icon_bg_x, icon_bg_y, icon_bg_size, icon_bg_size), "烈", font=font_big_title, fill=GOLD
)

# 名称
dy = icon_bg_y + icon_bg_size + 20
draw_text_centered(draw, (detail_x, dy, DETAIL_W, 30), "烈焰狮王", font=font_title, fill=GOLD)

# 品质
dy += 40
draw_text_centered(draw, (detail_x, dy, DETAIL_W, 24), "品质: 金", font=font_normal, fill=QUALITY_COLORS["gold"])

# 分隔线
dy += 35
draw.line((detail_x + 25, dy, detail_x + DETAIL_W - 25, dy), fill=(50, 50, 70), width=1)

# 描述
dy += 15
desc_text = "传说中的烈焰之王，拥有\n强大的火焰力量，能够\n焚烧一切敌人。"
for line in desc_text.split("\n"):
    draw.text((detail_x + 25, dy), line, font=font_normal, fill=(200, 200, 210))
    dy += 24

# 属性区域
dy += 15
draw.line((detail_x + 25, dy, detail_x + DETAIL_W - 25, dy), fill=(50, 50, 70), width=1)
dy += 15

attrs = [("生命", "1200"), ("攻击", "85"), ("护甲", "45"), ("攻速", "0.80")]
attr_col_w = (DETAIL_W - 50) // 2
for i, (label, value) in enumerate(attrs):
    ax = detail_x + 25 + (i % 2) * attr_col_w
    ay = dy + (i // 2) * 35
    draw.text((ax, ay), f"{label}:", font=font_normal, fill=GRAY)
    draw.text((ax + 50, ay), value, font=font_normal, fill=WHITE)

# ===== 标注说明 =====
# 左下角加个小标注
note_y = H - 30
draw.text((LEFT_W + 30, note_y), "DictionariesUI 界面布局 — Clash of Gods 图鉴系统", font=font_small, fill=(80, 80, 100))

# ===== 保存 =====
output_path = os.path.dirname(os.path.abspath(__file__))
out_file = os.path.join(output_path, "DictionariesUI_Mockup.png")
img_rgb = img.convert("RGB")
img_rgb.save(out_file, "PNG", quality=95)
print(f"Saved: {out_file}")
