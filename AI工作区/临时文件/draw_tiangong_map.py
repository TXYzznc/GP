"""
天庭场景设计说明图生成器
分辨率：4096x4096
"""
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import matplotlib.patheffects as pe
import numpy as np

# ============================================================
# 区域分类数据
# ============================================================

# 颜色方案
COLORS = {
    'core':       '#C0392B',   # 深红 - 核心区域（Boss/主殿）
    'explore':    '#2471A3',   # 蓝色 - 探索区域（有敌人）
    'event':      '#1E8449',   # 绿色 - 事件区域（商人/宝箱/祭坛）
    'special':    '#7D3C98',   # 紫色 - 特殊功能区（封神台/斩妖台）
    'passage':    '#D4AC0D',   # 金色 - 通道/云路
    'deco':       '#717D7E',   # 灰色 - 纯装饰区域
    'start':      '#E67E22',   # 橙色 - 起始/入口区域
    'exit':       '#148F77',   # 青绿 - 撤离点
    'bg':         '#FDF6E3',   # 背景
    'grid':       '#E8D5B0',   # 网格线
    'border':     '#8B4513',   # 边框
    'text_dark':  '#1A1A1A',
    'text_light': '#FFFFFF',
}

# 建筑数据：(名称, 类型, x, y, w, h, 游戏功能说明)
# 坐标系：左下为原点，x向右，y向上，画布约100x100单位
BUILDINGS = [
    # ── 起始区（南端）──────────────────────────────────────
    ('接引殿',   'start',   48, 2,  8, 5,  '入口 · 新手引导 · 场景说明'),
    ('朱雀宫',   'explore', 35, 2,  8, 5,  '探索区 · 普通敌人 · 宝箱'),
    ('白虎殿',   'explore', 61, 2,  8, 5,  '探索区 · 普通敌人 · 宝箱'),

    # ── 南侧探索区 ─────────────────────────────────────────
    ('画知',     'event',   48, 9,  8, 4,  '公告牌 · 任务提示 · 世界观说明'),
    ('太阴殿',   'explore',  2, 14, 8, 5,  '探索区 · 普通敌人'),
    ('毗沙宫',   'explore', 22, 14, 8, 5,  '探索区 · 普通敌人'),
    ('星月宫',   'explore', 12, 20, 8, 5,  '探索区 · 普通敌人'),
    ('玉明宫',   'explore', 22, 20, 8, 5,  '探索区 · 普通敌人'),
    ('通明宫',   'explore', 32, 20, 8, 5,  '探索区 · 普通敌人'),
    ('遣云宫',   'explore', 42, 20, 8, 5,  '探索区 · 普通敌人'),
    ('太阳殿',   'explore', 92, 14, 8, 5,  '探索区 · 普通敌人'),
    ('星日宫',   'explore', 82, 20, 8, 5,  '探索区 · 普通敌人'),
    ('青龙殿',   'explore', 72, 20, 8, 5,  '探索区 · 普通敌人'),
    ('天王殿',   'explore', 62, 20, 8, 5,  '探索区 · 普通敌人'),
    ('华乐宫',   'explore', 52, 20, 8, 5,  '探索区 · 普通敌人'),

    # ── 朝会殿（中南主殿）─────────────────────────────────
    ('朝会殿',   'explore', 43, 14, 16, 7, '探索区 · 精英敌人 · 大型战斗'),

    # ── 云路（左右通道）───────────────────────────────────
    ('云路(左)', 'passage', 14, 28,  8, 4,  '过渡通道 · 连接南北'),
    ('云路(右)', 'passage', 80, 28,  8, 4,  '过渡通道 · 连接南北'),
    ('珍宝阁',   'event',    2, 28,  8, 5,  '商人 · 购买装备/道具'),
    ('御马监',   'event',   92, 28,  8, 5,  '商人 · 购买召唤物强化'),

    # ── 中核区（凌霄殿周围）───────────────────────────────
    ('御花园',   'event',   28, 35, 12, 10, '休息区 · 商人 · 回复点 · 特殊事件'),
    ('兜率宫',   'explore', 16, 38,  8, 6,  '探索区 · 精英敌人'),
    ('云楼宫',   'explore', 24, 46,  8, 5,  '探索区 · 精英敌人'),
    ('净居宫',   'explore',  8, 46,  8, 5,  '探索区 · 精英敌人'),
    ('三清宫',   'explore',  2, 54,  8, 5,  '探索区 · 精英敌人'),
    ('封神台',   'special',  2, 62,  8, 5,  '特殊功能 · 强化祭坛 · 消耗材料获得Buff'),

    ('凌霄殿',   'core',    38, 36, 26, 16, '核心主殿 · Boss战场 · 击败后掉落钥匙'),

    ('兵马司',   'explore', 64, 38, 10, 8,  '精英区 · 精英敌人 · 高难度'),
    ('紫微宫',   'explore', 76, 46,  8, 5,  '探索区 · 精英敌人'),
    ('宝光殿',   'explore', 86, 46,  8, 5,  '探索区 · 精英敌人'),
    ('灵虚殿',   'explore', 76, 54,  8, 5,  '探索区 · 精英敌人'),
    ('灵官殿',   'explore', 86, 54,  8, 5,  '探索区 · 精英敌人'),
    ('斩妖台',   'special', 92, 62,  8, 5,  '特殊功能 · 净化敌人 · 获得召唤卡牌'),

    # ── 北侧深处区 ─────────────────────────────────────────
    ('披香宫',   'explore', 28, 56,  8, 5,  '探索区 · 精英敌人'),
    ('后宫',     'core',    40, 58, 22, 10, '核心区 · 精英Boss · 稀有掉落'),
    ('彩风',     'explore', 34, 70,  8, 5,  '探索区 · 精英敌人'),
    ('金龙',     'explore', 60, 70,  8, 5,  '探索区 · 精英敌人'),
    ('瞳卢宫',   'explore', 44, 70, 14, 5,  '探索区 · 精英敌人 · 稀有宝箱'),

    # ── 北天门（终点）─────────────────────────────────────
    ('北天门',   'exit',    42, 78, 18, 6,  '撤离点 · 携带战利品离开 · 章节终点'),
]

# ============================================================
# 绘图
# ============================================================
FIG_SIZE = 40  # inches，配合dpi=102.4 → 4096px
DPI = 102.4

fig, ax = plt.subplots(figsize=(FIG_SIZE, FIG_SIZE), dpi=DPI)
fig.patch.set_facecolor(COLORS['bg'])
ax.set_facecolor('#E8D5A0')

# 背景网格
ax.set_xlim(0, 102)
ax.set_ylim(0, 88)
for x in range(0, 103, 5):
    ax.axvline(x, color=COLORS['grid'], lw=0.5, alpha=0.5)
for y in range(0, 89, 5):
    ax.axhline(y, color=COLORS['grid'], lw=0.5, alpha=0.5)

# 中轴线
ax.axvline(51, color='#C8960C', lw=2, alpha=0.4, linestyle='--')

# 绘制建筑
for name, btype, x, y, w, h, desc in BUILDINGS:
    color = COLORS[btype]
    # 主体矩形
    rect = FancyBboxPatch(
        (x, y), w, h,
        boxstyle="round,pad=0.3",
        facecolor=color,
        edgecolor='white',
        linewidth=2,
        alpha=0.88,
        zorder=3
    )
    ax.add_patch(rect)

    # 建筑名称
    font_size = 11 if w >= 14 else (9 if w >= 8 else 7)
    ax.text(
        x + w/2, y + h/2 + 0.3,
        name,
        ha='center', va='center',
        fontsize=font_size,
        fontweight='bold',
        color='white',
        zorder=5,
        fontfamily='SimHei',
        path_effects=[pe.withStroke(linewidth=2, foreground='black')]
    )

    # 功能说明（小字）
    if h >= 6:
        ax.text(
            x + w/2, y + 1.2,
            desc,
            ha='center', va='bottom',
            fontsize=6,
            color='#FFEECC',
            zorder=5,
            fontfamily='SimHei',
            wrap=True
        )

# ── 图例 ──────────────────────────────────────────────────
legend_data = [
    ('start',   '入口/起始区'),
    ('explore',  '探索区（有敌人）'),
    ('core',    '核心区（Boss/主殿）'),
    ('event',   '事件区（商人/宝箱）'),
    ('special', '特殊功能区'),
    ('passage', '通道/云路'),
    ('exit',    '撤离点'),
    ('deco',    '装饰区域'),
]
patches = [mpatches.Patch(color=COLORS[k], label=v) for k, v in legend_data]
legend = ax.legend(
    handles=patches,
    loc='lower right',
    bbox_to_anchor=(1.0, 0.0),
    fontsize=14,
    title='区域类型图例',
    title_fontsize=15,
    framealpha=0.92,
    edgecolor=COLORS['border'],
    prop={'family': 'SimHei', 'size': 14}
)
legend.get_title().set_fontfamily('SimHei')

# ── 标题 ──────────────────────────────────────────────────
ax.set_title(
    '天庭场景设计说明图  ·  Clash of Gods  ·  第一章',
    fontsize=28,
    fontweight='bold',
    color=COLORS['border'],
    fontfamily='SimHei',
    pad=20
)

# ── 方向标注 ──────────────────────────────────────────────
ax.text(51, 85.5, '↑ 北  (深入方向)', ha='center', va='center',
        fontsize=14, color='#8B4513', fontfamily='SimHei', fontweight='bold')
ax.text(51, 0.5, '↓ 南  (入口方向)', ha='center', va='center',
        fontsize=14, color='#8B4513', fontfamily='SimHei', fontweight='bold')

ax.axis('off')
plt.tight_layout(pad=1.5)

# 保存
out_path = 'AI工作区/临时文件/天庭场景设计说明图.png'
plt.savefig(out_path, dpi=DPI, bbox_inches='tight',
            facecolor=COLORS['bg'], format='png')
plt.close()
print(f"已保存：{out_path}")
print(f"实际分辨率约：{int(FIG_SIZE*DPI)}x{int(FIG_SIZE*DPI)} px")
