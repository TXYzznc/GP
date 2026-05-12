#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, Rectangle
import os

# 设置中文字体
plt.rcParams['font.sans-serif'] = ['SimHei', 'DejaVu Sans']
plt.rcParams['axes.unicode_minus'] = False

output_dir = 'd:/unity/UnityProject/GP/Clash_Of_Gods/项目知识库（AI自行维护）/实现/界面设计'
os.makedirs(output_dir, exist_ok=True)

# ============ 生成 1: 主界面布局设计 ============
fig, ax = plt.subplots(figsize=(14, 10), dpi=100)
ax.set_xlim(0, 100)
ax.set_ylim(0, 100)
ax.axis('off')

# 背景
ax.add_patch(Rectangle((0, 0), 100, 100, facecolor='#1a1a2e', edgecolor='none'))

# 标题
ax.text(50, 97, '《Clash of Gods》角色管理界面 - 主界面布局（已优化）',
        fontsize=18, fontweight='bold', ha='center', color='#FFD700')

# 左侧角色列表面板
left_panel = FancyBboxPatch((2, 10), 18, 83,
                            boxstyle="round,pad=0.5",
                            edgecolor='#4a90e2', facecolor='#1e3a8a', linewidth=2)
ax.add_patch(left_panel)
ax.text(11, 90, '角色列表', fontsize=11, fontweight='bold', ha='center', color='#4a90e2')
# 角色列表项
for i in range(5):
    y = 85 - i*12
    if i == 0:
        item_color = '#2d5a3d'
        text_color = '#4ade80'
    else:
        item_color = '#2a3f5f'
        text_color = '#b0c4de'
    ax.add_patch(Rectangle((3, y-8), 16, 9, facecolor=item_color, edgecolor='#4a90e2', linewidth=1))
    ax.text(11, y-3.5, f'角色{i+1}', fontsize=9, ha='center', color=text_color)

# 中央立绘展示区域 - 拉长至底部（只留10像素边距）
center_panel = FancyBboxPatch((22, 10), 40, 83,
                             boxstyle="round,pad=0.5",
                             edgecolor='#e2994a', facecolor='#2a1810', linewidth=2)
ax.add_patch(center_panel)
ax.text(42, 89, '角色立绘/建模', fontsize=11, fontweight='bold', ha='center', color='#e2994a')

# 立绘展示区域（扩大）
ax.add_patch(Rectangle((24, 20), 36, 65, facecolor='#1a0f08', edgecolor='#6b4423', linewidth=1, linestyle='--'))
ax.text(42, 52, '[3D模型/立绘展示]', fontsize=10, ha='center', color='#8b6f47', style='italic')

# 立绘相关按钮
ax.text(28, 16, '旋转', fontsize=8, ha='center', color='#daa520')
ax.text(42, 16, '缩放', fontsize=8, ha='center', color='#daa520')
ax.text(56, 16, '全屏', fontsize=8, ha='center', color='#daa520')

# 右侧信息面板 - 拉长至底部（只留10像素边距）
right_panel = FancyBboxPatch((64, 10), 34, 83,
                            boxstyle="round,pad=0.5",
                            edgecolor='#a555a0', facecolor='#2a1a3a', linewidth=2)
ax.add_patch(right_panel)
ax.text(81, 89, '角色信息面板', fontsize=11, fontweight='bold', ha='center', color='#a555a0')

# 标签页切换
tab_y = 84
for i, tab_name in enumerate(['属性', '装备', '进阶', '资料']):
    tab_x = 68 + i * 7.5
    if i == 0:
        ax.add_patch(Rectangle((tab_x, tab_y), 6.5, 3.5, facecolor='#4a3a6a', edgecolor='#a555a0', linewidth=1.5))
        ax.text(tab_x + 3.25, tab_y + 1.75, tab_name, fontsize=8, ha='center', color='#ff69b4', fontweight='bold')
    else:
        ax.add_patch(Rectangle((tab_x, tab_y), 6.5, 3.5, facecolor='#2a1a3a', edgecolor='#6b4a7a', linewidth=1))
        ax.text(tab_x + 3.25, tab_y + 1.75, tab_name, fontsize=8, ha='center', color='#b0a0c0')

# 信息内容显示区域（扩大）
ax.add_patch(Rectangle((66, 18), 30, 62, facecolor='#1a0a2a', edgecolor='#7a5a9a', linewidth=1, linestyle='--'))
ax.text(81, 75, '属性信息内容', fontsize=9, ha='center', color='#9a7aba')
ax.text(81, 68, '等级 | 经验 | 突破', fontsize=7, ha='center', color='#7a6aaa')
ax.text(81, 61, '攻击 | 防御 | 生命', fontsize=7, ha='center', color='#7a6aaa')
ax.text(81, 54, '法攻 | 法抗 | 暴击', fontsize=7, ha='center', color='#7a6aaa')
ax.text(81, 47, '（具体数值显示）', fontsize=7, ha='center', style='italic', color='#6a5a9a')

plt.tight_layout()
plt.savefig(os.path.join(output_dir, 'CharacterUI_MainLayout.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] CharacterUI_MainLayout.png generated")
plt.close()

# ============ 生成 2: 标签页详细设计 ============
fig, axes = plt.subplots(2, 2, figsize=(14, 10), dpi=100)
fig.suptitle('角色管理界面 - 标签页详细设计', fontsize=16, fontweight='bold', color='#FFD700', y=0.98)

tabs_info = [
    {
        'title': '属性标签页',
        'content': [
            '基础属性',
            '等级：Lv.50',
            '经验：9500/10000',
            '',
            '战斗属性',
            '攻击力：500',
            '防御力：300',
            '法攻：450',
            '法抗：280',
            '生命值：1500',
            '暴击率：25%',
        ],
        'color': '#4a90e2'
    },
    {
        'title': '装备标签页',
        'content': [
            '装备槽位',
            '武器：[装备A]',
            '防具：[装备B]',
            '首饰：[装备C]',
            '特殊：[装备D]',
            '',
            '装备信息',
            '名称：古剑',
            '品质：★★★',
            '属性加成...',
        ],
        'color': '#e2994a'
    },
    {
        'title': '进阶标签页',
        'content': [
            '进阶信息',
            '当前阶级：★★★',
            '',
            '下级所需材料：',
            '灵晶：50',
            '金币：10000',
            '碎片：20',
            '',
            '[进阶按钮] [预览]',
            '',
            '进阶后获得属性提升',
        ],
        'color': '#a555a0'
    },
    {
        'title': '资料标签页',
        'content': [
            '角色资料',
            '名称：后羿',
            '职业：射手',
            '',
            '背景故事：',
            '古代神射手，',
            '擅长远程输出。',
            '可靠的队伍核心。',
            '',
            '[查看完整故事]',
        ],
        'color': '#4ade80'
    }
]

for idx, (ax, tab_info) in enumerate(zip(axes.flat, tabs_info)):
    ax.set_xlim(0, 10)
    ax.set_ylim(0, 10)
    ax.axis('off')
    ax.add_patch(Rectangle((0, 0), 10, 10, facecolor='#1a1a2e', edgecolor='none'))

    # 标题
    ax.text(5, 9.5, tab_info['title'], fontsize=12, fontweight='bold',
            ha='center', color=tab_info['color'])

    # 内容
    y_pos = 8.8
    for line in tab_info['content']:
        ax.text(0.5, y_pos, line, fontsize=8, family='monospace',
                color='#b0c4de', verticalalignment='top')
        y_pos -= 0.4

plt.tight_layout()
plt.savefig(os.path.join(output_dir, 'CharacterUI_TabPages.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] CharacterUI_TabPages.png generated")
plt.close()

# ============ 生成 3: 交互流程设计 ============
fig, ax = plt.subplots(figsize=(14, 10), dpi=100)
ax.set_xlim(0, 100)
ax.set_ylim(0, 100)
ax.axis('off')

ax.add_patch(Rectangle((0, 0), 100, 100, facecolor='#1a1a2e', edgecolor='none'))
ax.text(50, 97, '角色管理界面 - 交互流程与弹窗设计',
        fontsize=16, fontweight='bold', ha='center', color='#FFD700')

# 主界面
main_box = FancyBboxPatch((35, 75), 30, 15, boxstyle="round,pad=0.3",
                         edgecolor='#4a90e2', facecolor='#1e3a8a', linewidth=2)
ax.add_patch(main_box)
ax.text(50, 85, '主界面', fontsize=12, fontweight='bold', ha='center', color='#4a90e2')
ax.text(50, 80, '角色列表/立绘/属性/操作', fontsize=9, ha='center', color='#b0c4de')

# 装备选择弹窗
equip_box = FancyBboxPatch((5, 50), 25, 18, boxstyle="round,pad=0.3",
                          edgecolor='#e2994a', facecolor='#2a1810', linewidth=2)
ax.add_patch(equip_box)
ax.text(17.5, 65, '装备选择弹窗', fontsize=11, fontweight='bold', ha='center', color='#e2994a')
ax.text(17.5, 59, '拥有装备列表', fontsize=8, ha='center', color='#daa520')
ax.text(17.5, 55, '[装备1] [装备2]', fontsize=8, ha='center', family='monospace', color='#8b6f47')
ax.text(17.5, 52, '[装备3] [装备4]', fontsize=8, ha='center', family='monospace', color='#8b6f47')

# 进阶确认弹窗
advance_box = FancyBboxPatch((37, 50), 25, 18, boxstyle="round,pad=0.3",
                           edgecolor='#a555a0', facecolor='#2a1a3a', linewidth=2)
ax.add_patch(advance_box)
ax.text(49.5, 65, '进阶确认弹窗', fontsize=11, fontweight='bold', ha='center', color='#a555a0')
ax.text(49.5, 59, '确认进阶?', fontsize=8, ha='center', color='#ff69b4')
ax.text(49.5, 55, '消耗资源确认', fontsize=8, ha='center', color='#b0a0c0')
ax.text(49.5, 52, '[确认] [取消]', fontsize=8, ha='center', color='#ff69b4')

# 故事全文查看UI
story_box = FancyBboxPatch((69, 50), 25, 18, boxstyle="round,pad=0.3",
                         edgecolor='#4ade80', facecolor='#1a3a1a', linewidth=2)
ax.add_patch(story_box)
ax.text(81.5, 65, '故事全文查看', fontsize=11, fontweight='bold', ha='center', color='#4ade80')
ax.text(81.5, 59, '角色故事', fontsize=8, ha='center', color='#4ade80')
ax.text(81.5, 55, '[完整故事内容]', fontsize=8, ha='center', color='#7aaa7a')
ax.text(81.5, 52, '[返回主界面]', fontsize=8, ha='center', color='#4ade80')

# 箭头关系
ax.annotate('点击装备按钮', xy=(17.5, 68), xytext=(40, 75),
            arrowprops=dict(arrowstyle='->', lw=1.5, color='#e2994a'),
            fontsize=9, color='#e2994a', ha='center')

ax.annotate('点击进阶按钮', xy=(49.5, 68), xytext=(50, 75),
            arrowprops=dict(arrowstyle='->', lw=1.5, color='#a555a0'),
            fontsize=9, color='#a555a0', ha='center')

ax.annotate('查看完整故事', xy=(81.5, 68), xytext=(60, 75),
            arrowprops=dict(arrowstyle='->', lw=1.5, color='#4ade80'),
            fontsize=9, color='#4ade80', ha='center')

# 数据流说明
ax.text(50, 40, '交互流程说明', fontsize=11, fontweight='bold', ha='center', color='#FFD700')

flow_text = [
    '1. 角色列表选择 → 加载对应角色数据',
    '2. 标签页切换 → 动态显示不同信息（属性/装备/进阶/资料）',
    '3. 装备按钮 → 打开装备选择弹窗 → 选择后更新角色装备',
    '4. 进阶按钮 → 打开进阶确认弹窗 → 确认后更新阶级',
    '5. 资料按钮 → 打开故事全文UI → 显示完整故事'
]

y_pos = 36
for text in flow_text:
    ax.text(50, y_pos, text, fontsize=8.5, ha='center', color='#b0c4de')
    y_pos -= 3.5

# 架构建议
ax.text(50, 12, '推荐代码架构：', fontsize=9, fontweight='bold', ha='center', color='#FFD700')
ax.text(50, 8, 'CharacterUIForm（主控制器） + CharacterListPanel + CharacterPreviewPanel',
        fontsize=8, ha='center', color='#daa520')
ax.text(50, 4, '+ TabPanel（属性/装备/进阶/资料） + Dialog（装备/进阶/故事弹窗）',
        fontsize=8, ha='center', color='#daa520')

plt.tight_layout()
plt.savefig(os.path.join(output_dir, 'CharacterUI_Interaction.png'),
            dpi=150, bbox_inches='tight', facecolor='#0a0a1a')
print("[OK] CharacterUI_Interaction.png generated")
plt.close()

print("\n[SUCCESS] All PNG design files generated!")
print(f"[PATH] {output_dir}")
print("[FILES]")
print("   1. CharacterUI_MainLayout.png - Main interface layout")
print("   2. CharacterUI_TabPages.png - Tab pages detail design")
print("   3. CharacterUI_Interaction.png - Interaction flow and dialog design")
