#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, Rectangle, FancyArrowPatch, Circle
import os

# 设置中文字体
plt.rcParams['font.sans-serif'] = ['SimHei', 'DejaVu Sans']
plt.rcParams['axes.unicode_minus'] = False

output_dir = 'd:/unity/UnityProject/GP/Clash_Of_Gods/项目知识库（AI自行维护）/实现/界面设计'
os.makedirs(output_dir, exist_ok=True)

def create_tab_button(ax, x, y, width, height, text, is_active=False):
    """绘制标签页按钮"""
    if is_active:
        color = '#4a3a6a'
        text_color = '#ff69b4'
        linewidth = 1.5
    else:
        color = '#2a1a3a'
        text_color = '#b0a0c0'
        linewidth = 1
    ax.add_patch(Rectangle((x, y), width, height, facecolor=color, edgecolor='#a555a0', linewidth=linewidth))
    ax.text(x + width/2, y + height/2, text, fontsize=8, ha='center', va='center', color=text_color, fontweight='bold' if is_active else 'normal')

# ============ 生成 1: 主界面布局（完整交互设计） ============
fig, ax = plt.subplots(figsize=(16, 10), dpi=100)
ax.set_xlim(0, 120)
ax.set_ylim(0, 100)
ax.axis('off')

# 背景
ax.add_patch(Rectangle((0, 0), 120, 100, facecolor='#1a1a2e', edgecolor='none'))

# 标题
ax.text(60, 97, '《Clash of Gods》角色管理界面 - 完整交互设计',
        fontsize=18, fontweight='bold', ha='center', color='#FFD700')

# ===== 左侧：角色列表（竖直滑动列表）=====
left_panel = FancyBboxPatch((2, 10), 16, 83,
                            boxstyle="round,pad=0.5",
                            edgecolor='#4a90e2', facecolor='#1e3a8a', linewidth=2)
ax.add_patch(left_panel)
ax.text(10, 90, '角色列表', fontsize=10, fontweight='bold', ha='center', color='#4a90e2')
ax.text(10, 87, '(竖直滑动)', fontsize=7, ha='center', color='#b0c4de', style='italic')

# 角色图标列表项
for i in range(5):
    y = 83 - i*14
    if i == 0:
        item_color = '#2d5a3d'
        text_color = '#4ade80'
        border_color = '#4ade80'
    else:
        item_color = '#2a3f5f'
        text_color = '#b0c4de'
        border_color = '#4a90e2'
    ax.add_patch(Rectangle((3, y-10), 14, 11, facecolor=item_color, edgecolor=border_color, linewidth=2 if i == 0 else 1))
    ax.text(10, y-4, f'[图标{i+1}]', fontsize=7, ha='center', color=text_color, fontweight='bold')
    ax.text(10, y-7, f'棋子{i+1}', fontsize=7, ha='center', color=text_color)

ax.text(10, 12, '(可滑动...)', fontsize=7, ha='center', color='#888', style='italic')

# ===== 中央：角色展示界面 =====
center_panel = FancyBboxPatch((20, 10), 38, 83,
                             boxstyle="round,pad=0.5",
                             edgecolor='#e2994a', facecolor='#2a1810', linewidth=2)
ax.add_patch(center_panel)
ax.text(39, 90, '角色立绘/建模', fontsize=10, fontweight='bold', ha='center', color='#e2994a')

# 立绘展示区域
ax.add_patch(Rectangle((22, 25), 34, 60, facecolor='#1a0f08', edgecolor='#6b4423', linewidth=1, linestyle='--'))
ax.text(39, 55, '[3D模型/立绘展示]', fontsize=9, ha='center', color='#8b6f47', style='italic')

# 显示方式切换按钮（模型/立绘）
ax.add_patch(Rectangle((24, 16), 7, 6, facecolor='#3a2a1a', edgecolor='#daa520', linewidth=1.5))
ax.text(27.5, 19, '模型', fontsize=7, ha='center', color='#daa520', fontweight='bold')

ax.add_patch(Rectangle((33, 16), 7, 6, facecolor='#2a1a1a', edgecolor='#8b6f47', linewidth=1))
ax.text(36.5, 19, '立绘', fontsize=7, ha='center', color='#8b6f47')

ax.add_patch(Rectangle((42, 16), 7, 6, facecolor='#2a1a1a', edgecolor='#8b6f47', linewidth=1))
ax.text(45.5, 19, '全屏', fontsize=7, ha='center', color='#8b6f47')

# ===== 右侧：角色信息面板 =====
right_panel = FancyBboxPatch((60, 10), 58, 83,
                            boxstyle="round,pad=0.5",
                            edgecolor='#a555a0', facecolor='#2a1a3a', linewidth=2)
ax.add_patch(right_panel)
ax.text(89, 90, '角色信息面板', fontsize=10, fontweight='bold', ha='center', color='#a555a0')

# 右侧四个标签页按钮
tab_y = 84
tab_width = 13
tab_height = 3.5
create_tab_button(ax, 63, tab_y, tab_width, tab_height, '属性', is_active=True)
create_tab_button(ax, 77.5, tab_y, tab_width, tab_height, '宝物', is_active=False)
create_tab_button(ax, 92, tab_y, tab_width, tab_height, '进阶', is_active=False)
create_tab_button(ax, 106.5, tab_y, tab_width, tab_height, '资料', is_active=False)

# 属性面板内容显示区域（示例：属性标签页）
ax.add_patch(Rectangle((62, 18), 54, 62, facecolor='#1a0a2a', edgecolor='#7a5a9a', linewidth=1, linestyle='--'))

# 显示属性信息
ax.text(89, 75, '属性面板内容', fontsize=9, ha='center', color='#9a7aba', fontweight='bold')
attr_info = [
    '等级：Lv.50  |  经验：9500/10000',
    '─────────────────────',
    '攻击力：500    防御力：300',
    '法攻：450      法抗：280',
    '生命值：1500  暴击率：25%',
    '命中率：85%  闪避率：15%',
]
y_pos = 69
for line in attr_info:
    ax.text(89, y_pos, line, fontsize=7, ha='center', color='#7a6aaa', family='monospace')
    y_pos -= 4

ax.text(89, 35, '[点击标签页切换界面]', fontsize=8, ha='center', color='#9a7aba', style='italic')
ax.text(89, 30, '宝物 / 进阶 / 资料', fontsize=8, ha='center', color='#9a7aba', style='italic')

# ===== 添加交互箭头和说明 =====
# 点击角色切换
ax.annotate('', xy=(20, 50), xytext=(18.5, 50),
            arrowprops=dict(arrowstyle='->', lw=2, color='#FFD700'))
ax.text(15, 54, '点击切换', fontsize=8, ha='center', color='#FFD700', fontweight='bold')

# 模型/立绘切换
ax.annotate('', xy=(39, 24), xytext=(39, 10),
            arrowprops=dict(arrowstyle='->', lw=1.5, color='#daa520'))
ax.text(44, 17, '切换显示', fontsize=7, ha='right', color='#daa520')

# 标签页切换
ax.annotate('', xy=(89, 81), xytext=(89, 23),
            arrowprops=dict(arrowstyle='<->', lw=1.5, color='#ff69b4', linestyle='--'))
ax.text(98, 52, '切换面板', fontsize=7, ha='left', color='#ff69b4')

plt.tight_layout()
plt.savefig(os.path.join(output_dir, '01_CharacterUI_MainLayout.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] 01_CharacterUI_MainLayout.png generated")
plt.close()

# ============ 生成 2: 宝物界面详细设计 ============
fig, ax = plt.subplots(figsize=(16, 12), dpi=100)
ax.set_xlim(0, 120)
ax.set_ylim(0, 130)
ax.axis('off')

ax.add_patch(Rectangle((0, 0), 120, 130, facecolor='#1a1a2e', edgecolor='none'))
ax.text(60, 127, '宝物界面 - 详细交互设计', fontsize=16, fontweight='bold', ha='center', color='#FFD700')

# 上半部分：宝物槽位面板
ax.text(30, 120, '宝物槽位面板', fontsize=11, fontweight='bold', ha='center', color='#e2994a')

slot_panel = FancyBboxPatch((5, 75), 50, 40, boxstyle="round,pad=0.5",
                           edgecolor='#e2994a', facecolor='#2a1810', linewidth=2)
ax.add_patch(slot_panel)

# 三个宝物槽位
slot_y_start = 108
slot_height = 12
slot_width = 14

# 宝物槽位1
ax.add_patch(Rectangle((10, slot_y_start), slot_width, slot_height,
                       facecolor='#3a2a1a', edgecolor='#daa520', linewidth=2))
ax.text(17, slot_y_start+9, '宝物槽位1', fontsize=8, ha='center', color='#daa520', fontweight='bold')
ax.text(17, slot_y_start+2, '[点击装备]', fontsize=7, ha='center', color='#8b6f47')

# 宝物槽位2
ax.add_patch(Rectangle((28, slot_y_start), slot_width, slot_height,
                       facecolor='#3a2a1a', edgecolor='#daa520', linewidth=2))
ax.text(35, slot_y_start+9, '宝物槽位2', fontsize=8, ha='center', color='#daa520', fontweight='bold')
ax.text(35, slot_y_start+2, '[点击装备]', fontsize=7, ha='center', color='#8b6f47')

# 宝物槽位3
ax.add_patch(Rectangle((46, slot_y_start), slot_width, slot_height,
                       facecolor='#3a2a1a', edgecolor='#daa520', linewidth=2))
ax.text(53, slot_y_start+9, '宝物槽位3', fontsize=8, ha='center', color='#daa520', fontweight='bold')
ax.text(53, slot_y_start+2, '[点击装备]', fontsize=7, ha='center', color='#8b6f47')

ax.text(30, 78, '宝物属性总加成：', fontsize=9, ha='center', color='#daa520', fontweight='bold')

# 下半部分：从底部滑入的宝物列表
ax.text(90, 120, '从底部滑入的宝物列表', fontsize=11, fontweight='bold', ha='center', color='#4ade80')
ax.text(90, 115, '(竖直方向滑动列表，10列宽)', fontsize=8, ha='center', color='#7aaa7a', style='italic')

list_panel = FancyBboxPatch((65, 30), 50, 78, boxstyle="round,pad=0.5",
                           edgecolor='#4ade80', facecolor='#1a3a1a', linewidth=2)
ax.add_patch(list_panel)

ax.text(90, 103, '玩家拥有的宝物列表', fontsize=9, ha='center', color='#4ade80', fontweight='bold')

# 绘制10列宝物网格 (示例显示3行)
cols = 10
item_width = 4
item_height = 6
start_x = 68
start_y = 95

for row in range(3):
    for col in range(cols):
        x = start_x + col * item_width
        y = start_y - row * item_height
        ax.add_patch(Rectangle((x, y-item_height), item_width, item_height,
                              facecolor='#2a2a1a', edgecolor='#4ade80', linewidth=1))
        ax.text(x + item_width/2, y - item_height/2, '[宝]', fontsize=5, ha='center', va='center', color='#4ade80')

ax.text(90, 45, '(可竖直滑动浏览...)', fontsize=8, ha='center', color='#888', style='italic')

# 交互说明
ax.text(30, 68, '交互流程：', fontsize=9, ha='center', color='#FFD700', fontweight='bold')
interaction_text = [
    '1. 点击上方宝物槽位 → 展开下方列表',
    '2. 从列表中选择宝物 → 装备到对应槽位',
    '3. 列表可竖直滑动，显示更多宝物',
    '4. 支持从背包和仓库中获取宝物',
]
y_pos = 63
for text in interaction_text:
    ax.text(30, y_pos, text, fontsize=7, ha='center', color='#b0c4de')
    y_pos -= 4

# 箭头指示
ax.annotate('点击打开', xy=(30, 75), xytext=(30, 55),
            arrowprops=dict(arrowstyle='->', lw=1.5, color='#daa520'))
ax.annotate('滑动选择', xy=(90, 70), xytext=(85, 75),
            arrowprops=dict(arrowstyle='->', lw=1.5, color='#4ade80'))

plt.tight_layout()
plt.savefig(os.path.join(output_dir, '02_TreasureUI_Detailed.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] 02_TreasureUI_Detailed.png generated")
plt.close()

# ============ 生成 3: 进阶界面设计 ============
fig, ax = plt.subplots(figsize=(14, 10), dpi=100)
ax.set_xlim(0, 100)
ax.set_ylim(0, 100)
ax.axis('off')

ax.add_patch(Rectangle((0, 0), 100, 100, facecolor='#1a1a2e', edgecolor='none'))
ax.text(50, 97, '进阶界面 - 进阶路线设计', fontsize=16, fontweight='bold', ha='center', color='#FFD700')

# 进阶面板
advance_panel = FancyBboxPatch((5, 10), 90, 82, boxstyle="round,pad=0.5",
                              edgecolor='#a555a0', facecolor='#2a1a3a', linewidth=2)
ax.add_patch(advance_panel)

ax.text(50, 87, '棋子进阶路线', fontsize=11, fontweight='bold', ha='center', color='#a555a0')

# 进阶树状图
# 第一阶
ax.add_patch(Rectangle((43, 75), 14, 6, facecolor='#3a2a3a', edgecolor='#ff69b4', linewidth=2))
ax.text(50, 78, '★ 初始状态', fontsize=8, ha='center', color='#ff69b4', fontweight='bold')

# 第二阶
ax.add_patch(Rectangle((20, 60), 14, 6, facecolor='#3a2a3a', edgecolor='#a555a0', linewidth=1.5))
ax.text(27, 63, '★★ 一阶', fontsize=8, ha='center', color='#a555a0')

ax.add_patch(Rectangle((66, 60), 14, 6, facecolor='#3a2a3a', edgecolor='#a555a0', linewidth=1.5))
ax.text(73, 63, '★★ 一阶', fontsize=8, ha='center', color='#a555a0')

# 连接线
ax.plot([50, 27], [75, 66], color='#7a5a9a', linewidth=1.5)
ax.plot([50, 73], [75, 66], color='#7a5a9a', linewidth=1.5)

# 第三阶
ax.add_patch(Rectangle((10, 45), 14, 6, facecolor='#3a2a3a', edgecolor='#a555a0', linewidth=1.5))
ax.text(17, 48, '★★★ 二阶', fontsize=8, ha='center', color='#a555a0')

ax.add_patch(Rectangle((37, 45), 14, 6, facecolor='#3a2a3a', edgecolor='#a555a0', linewidth=1.5))
ax.text(44, 48, '★★★ 二阶', fontsize=8, ha='center', color='#a555a0')

ax.add_patch(Rectangle((63, 45), 14, 6, facecolor='#3a2a3a', edgecolor='#a555a0', linewidth=1.5))
ax.text(70, 48, '★★★ 二阶', fontsize=8, ha='center', color='#a555a0')

ax.add_patch(Rectangle((76, 45), 14, 6, facecolor='#3a2a3a', edgecolor='#a555a0', linewidth=1.5))
ax.text(83, 48, '★★★ 二阶', fontsize=8, ha='center', color='#a555a0')

# 连接线
ax.plot([27, 17], [60, 51], color='#7a5a9a', linewidth=1.5)
ax.plot([27, 44], [60, 51], color='#7a5a9a', linewidth=1.5)
ax.plot([73, 70], [60, 51], color='#7a5a9a', linewidth=1.5)
ax.plot([73, 83], [60, 51], color='#7a5a9a', linewidth=1.5)

# 选中状态（示例：当前在二阶）
ax.add_patch(Rectangle((37, 45), 14, 6, facecolor='#4a3a6a', edgecolor='#ff69b4', linewidth=2.5))
ax.text(44, 48, '★★★ 二阶', fontsize=8, ha='center', color='#ff69b4', fontweight='bold')

ax.text(50, 35, '进阶所需资源', fontsize=10, ha='center', color='#FFD700', fontweight='bold')

# 资源显示
resources = [
    '灵晶：100 / 100   金币：50000 / 50000',
    '棋子碎片：20 / 20',
]

y_pos = 30
for res in resources:
    ax.text(50, y_pos, res, fontsize=8, ha='center', color='#daa520', family='monospace')
    y_pos -= 4

# 按钮
ax.add_patch(Rectangle((30, 18), 12, 4, facecolor='#4a3a4a', edgecolor='#ff69b4', linewidth=1.5))
ax.text(36, 20, '确认进阶', fontsize=8, ha='center', color='#ff69b4', fontweight='bold')

ax.add_patch(Rectangle((45, 18), 12, 4, facecolor='#3a3a3a', edgecolor='#888', linewidth=1))
ax.text(51, 20, '预览效果', fontsize=8, ha='center', color='#888')

ax.add_patch(Rectangle((60, 18), 12, 4, facecolor='#3a3a3a', edgecolor='#888', linewidth=1))
ax.text(66, 20, '返回', fontsize=8, ha='center', color='#888')

plt.tight_layout()
plt.savefig(os.path.join(output_dir, '03_AdvanceUI_Tree.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] 03_AdvanceUI_Tree.png generated")
plt.close()

# ============ 生成 4: 资料界面设计 ============
fig, ax = plt.subplots(figsize=(14, 10), dpi=100)
ax.set_xlim(0, 100)
ax.set_ylim(0, 100)
ax.axis('off')

ax.add_patch(Rectangle((0, 0), 100, 100, facecolor='#1a1a2e', edgecolor='none'))
ax.text(50, 97, '资料界面 - 角色背景故事', fontsize=16, fontweight='bold', ha='center', color='#FFD700')

# 资料面板
info_panel = FancyBboxPatch((5, 10), 90, 82, boxstyle="round,pad=0.5",
                           edgecolor='#4ade80', facecolor='#1a3a1a', linewidth=2)
ax.add_patch(info_panel)

# 角色基本信息
ax.text(50, 88, '后羿 · 射手', fontsize=12, ha='center', color='#4ade80', fontweight='bold')
ax.text(50, 84, '古代神射手，擅长远程输出', fontsize=9, ha='center', color='#7aaa7a', style='italic')

# 分隔线
ax.plot([10, 90], [82, 82], color='#4ade80', linewidth=1)

# 故事内容
story_text = [
    '【背景故事】',
    '',
    '后羿是上古时代的传奇射手，以精准的弓术闻名于天地。',
    '当太阳神羿化作十日横行于天，灼烧大地之时，后羿',
    '挺身而出，以金箭射落九日，拯救了人类。',
    '',
    '如今，虽然已是上古的传说，但后羿的故事依然在',
    '被后人传颂。他的弓箭技艺已被修炼成了非凡的力量，',
    '可以穿透一切虚实，是队伍中可靠的远程输出核心。',
    '',
    '在这个新时代，后羿再次踏上了旅程，与其他英雄',
    '携手对抗黑暗的威胁。',
]

y_pos = 78
for line in story_text:
    if line.startswith('【'):
        ax.text(50, y_pos, line, fontsize=8, ha='center', color='#4ade80', fontweight='bold')
    elif line == '':
        pass
    else:
        ax.text(50, y_pos, line, fontsize=7, ha='center', color='#7aaa7a')
    if line != '':
        y_pos -= 3

# 额外信息
ax.plot([10, 90], [30, 30], color='#4ade80', linewidth=1)

ax.text(20, 26, '【基础数据】', fontsize=8, ha='left', color='#4ade80', fontweight='bold')
ax.text(20, 22, '职业：射手', fontsize=7, ha='left', color='#7aaa7a')
ax.text(20, 18, '属性：敏捷', fontsize=7, ha='left', color='#7aaa7a')

ax.text(50, 22, '稀有度：★★★★', fontsize=7, ha='left', color='#7aaa7a')
ax.text(50, 18, '获得方式：地下城掉落', fontsize=7, ha='left', color='#7aaa7a')

ax.text(80, 22, '配音演员：[待定]', fontsize=7, ha='left', color='#7aaa7a')
ax.text(80, 18, '出装推荐：远程DPS', fontsize=7, ha='left', color='#7aaa7a')

# 按钮
ax.add_patch(Rectangle((35, 12), 12, 4, facecolor='#2a3a2a', edgecolor='#4ade80', linewidth=1.5))
ax.text(41, 14, '查看语音', fontsize=8, ha='center', color='#4ade80', fontweight='bold')

ax.add_patch(Rectangle((53, 12), 12, 4, facecolor='#3a3a3a', edgecolor='#888', linewidth=1))
ax.text(59, 14, '返回', fontsize=8, ha='center', color='#888')

plt.tight_layout()
plt.savefig(os.path.join(output_dir, '04_InfoUI_Story.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] 04_InfoUI_Story.png generated")
plt.close()

# ============ 生成 5: 完整交互流程图 ============
fig, ax = plt.subplots(figsize=(16, 12), dpi=100)
ax.set_xlim(0, 140)
ax.set_ylim(0, 130)
ax.axis('off')

ax.add_patch(Rectangle((0, 0), 140, 130, facecolor='#1a1a2e', edgecolor='none'))
ax.text(70, 127, '完整交互流程与界面关系图', fontsize=16, fontweight='bold', ha='center', color='#FFD700')

# 主界面盒子
main_box = FancyBboxPatch((50, 100), 40, 18, boxstyle="round,pad=0.3",
                         edgecolor='#4a90e2', facecolor='#1e3a8a', linewidth=2)
ax.add_patch(main_box)
ax.text(70, 112, '主界面', fontsize=11, fontweight='bold', ha='center', color='#4a90e2')
ax.text(70, 106, '左：角色列表 | 中：立绘 | 右：四个标签页', fontsize=8, ha='center', color='#b0c4de')
ax.text(70, 101, '(属性/宝物/进阶/资料)', fontsize=8, ha='center', color='#b0c4de')

# 四个子界面
# 属性界面
attr_box = FancyBboxPatch((5, 55), 25, 30, boxstyle="round,pad=0.3",
                         edgecolor='#daa520', facecolor='#2a1a1a', linewidth=1.5)
ax.add_patch(attr_box)
ax.text(17.5, 80, '属性界面', fontsize=10, fontweight='bold', ha='center', color='#daa520')
attr_content = [
    '基础属性：',
    '等级、经验',
    '─────────',
    '战斗属性：',
    '攻击、防御、',
    '法攻、法抗等',
]
y = 76
for line in attr_content:
    ax.text(17.5, y, line, fontsize=6.5, ha='center', color='#8b6f47')
    y -= 3

# 宝物界面
treasure_box = FancyBboxPatch((35, 55), 25, 30, boxstyle="round,pad=0.3",
                             edgecolor='#e2994a', facecolor='#2a1810', linewidth=1.5)
ax.add_patch(treasure_box)
ax.text(47.5, 80, '宝物界面', fontsize=10, fontweight='bold', ha='center', color='#e2994a')
treasure_content = [
    '宝物槽位：3个',
    '─────────',
    '点击槽位→',
    '底部滑入列表',
    '─────────',
    '10列宽网格',
]
y = 76
for line in treasure_content:
    ax.text(47.5, y, line, fontsize=6.5, ha='center', color='#daa520')
    y -= 3

# 进阶界面
advance_box = FancyBboxPatch((65, 55), 25, 30, boxstyle="round,pad=0.3",
                            edgecolor='#a555a0', facecolor='#2a1a3a', linewidth=1.5)
ax.add_patch(advance_box)
ax.text(77.5, 80, '进阶界面', fontsize=10, fontweight='bold', ha='center', color='#a555a0')
advance_content = [
    '进阶树状图：',
    '展示所有阶级',
    '─────────',
    '资源消耗显示',
    '─────────',
    '确认进阶按钮',
]
y = 76
for line in advance_content:
    ax.text(77.5, y, line, fontsize=6.5, ha='center', color='#b0a0c0')
    y -= 3

# 资料界面
info_box = FancyBboxPatch((95, 55), 25, 30, boxstyle="round,pad=0.3",
                         edgecolor='#4ade80', facecolor='#1a3a1a', linewidth=1.5)
ax.add_patch(info_box)
ax.text(107.5, 80, '资料界面', fontsize=10, fontweight='bold', ha='center', color='#4ade80')
info_content = [
    '角色背景故事：',
    '文本内容显示',
    '─────────',
    '基础数据：',
    '职业、属性、',
    '稀有度等',
]
y = 76
for line in info_content:
    ax.text(107.5, y, line, fontsize=6.5, ha='center', color='#7aaa7a')
    y -= 3

# 箭头指向四个界面
for x, color in [(17.5, '#daa520'), (47.5, '#e2994a'), (77.5, '#a555a0'), (107.5, '#4ade80')]:
    ax.annotate('', xy=(x, 85), xytext=(70, 100),
                arrowprops=dict(arrowstyle='->', lw=1.5, color=color))

# 宝物列表子界面
treasure_list_box = FancyBboxPatch((35, 20), 25, 28, boxstyle="round,pad=0.3",
                                  edgecolor='#4ade80', facecolor='#1a3a1a', linewidth=1.5)
ax.add_patch(treasure_list_box)
ax.text(47.5, 45, '宝物列表界面', fontsize=9, fontweight='bold', ha='center', color='#4ade80')
ax.text(47.5, 40, '(从底部滑入)', fontsize=7, ha='center', color='#7aaa7a', style='italic')
ax.text(47.5, 35, '───────────', fontsize=7, ha='center', color='#7aaa7a')

list_content = [
    '显示拥有宝物：',
    '┌─────────┐',
    '│ 10列宽网格 │',
    '│ 竖直滑动   │',
    '└─────────┘',
]
y = 31
for line in list_content:
    ax.text(47.5, y, line, fontsize=6, ha='center', color='#7aaa7a', family='monospace')
    y -= 2.5

# 箭头：宝物界面到宝物列表
ax.annotate('', xy=(47.5, 55), xytext=(47.5, 48),
            arrowprops=dict(arrowstyle='<->', lw=1.5, color='#4ade80', linestyle='--'))
ax.text(52, 51.5, '打开/关闭', fontsize=6.5, color='#4ade80')

# 交互说明框
info_text_box = FancyBboxPatch((5, 2), 130, 14, boxstyle="round,pad=0.3",
                              edgecolor='#FFD700', facecolor='#2a2a1a', linewidth=1)
ax.add_patch(info_text_box)

ax.text(70, 13, '交互关键点说明', fontsize=9, ha='center', color='#FFD700', fontweight='bold')

interaction_points = [
    '① 左侧列表：点击角色图标→切换棋子 | ② 中央：点击模型/立绘按钮→切换显示',
    '③ 右侧按钮：点击属性/宝物/进阶/资料→切换对应界面',
    '④ 宝物界面：点击宝物槽位→底部滑入宝物列表 | ⑤ 进阶界面：显示树状进阶路线，可点击确认进阶',
]

y = 9.5
for text in interaction_points:
    ax.text(70, y, text, fontsize=7, ha='center', color='#b0c4de')
    y -= 2.5

plt.tight_layout()
plt.savefig(os.path.join(output_dir, '05_InteractionFlow_Complete.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] 05_InteractionFlow_Complete.png generated")
plt.close()

print("\n[SUCCESS] All interactive design PNG files generated!")
print(f"[PATH] {output_dir}")
print("[FILES]")
print("   1. 01_CharacterUI_MainLayout.png - Main interface complete design")
print("   2. 02_TreasureUI_Detailed.png - Treasure interface with sliding list")
print("   3. 03_AdvanceUI_Tree.png - Advance interface with progression tree")
print("   4. 04_InfoUI_Story.png - Info interface with story text")
print("   5. 05_InteractionFlow_Complete.png - Complete interaction flow diagram")
