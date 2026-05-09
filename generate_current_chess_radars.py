import matplotlib.pyplot as plt
import numpy as np
import os

# 设置中文字体
plt.rcParams['font.sans-serif'] = ['SimHei', 'DejaVu Sans']
plt.rcParams['axes.unicode_minus'] = False

# 创建输出目录
output_dir = r"D:\unity\UnityProject\GP\Clash_Of_Gods\项目知识库（AI自行维护）\实现"
if not os.path.exists(output_dir):
    os.makedirs(output_dir)

# 六维属性标签
attributes = ['最大生命值\n(MaxHp)', '最大法力值\n(MaxMp)', '护甲\n(Armor)',
              '魔抗\n(MagicResist)', '法术强度\n(SpellPower)', '攻击力\n(AtkDamage)']
N = len(attributes)

# 现有5个棋子的新设计属性（基于职业-功能模型）
current_chess_data = {
    '后羿': {
        'id': 1,
        'profession': '射手',
        'function': '输出',
        'current_quality': '紫色(2.7点)',
        '蓝色(2.4点)':  [1.8, 0.3, 0.8, 0.8, 0.3, 2.8],
        '紫色(2.7点)':  [2.5, 0.3, 1.8, 1.3, 0.3, 4.2],
        '金色(3.0点)':  [3.0, 0.3, 2.3, 1.8, 0.3, 5.0],
        '超模(3.5点)':  [3.8, 0.5, 2.8, 2.3, 0.4, 6.0],
    },
    '嫦娥': {
        'id': 4,
        'profession': '法师',
        'function': '辅助',
        'current_quality': '紫色(2.7点)',
        '蓝色(2.4点)':  [1.8, 1.6, 0.8, 0.8, 1.8, 0.2],
        '紫色(2.7点)':  [2.5, 1.8, 1.5, 1.5, 2.5, 0.3],
        '金色(3.0点)':  [3.0, 2.0, 2.0, 2.0, 3.0, 0.3],
        '超模(3.5点)':  [3.8, 2.5, 2.5, 2.5, 3.8, 0.5],
    },
    '邪灵': {
        'id': 10,
        'profession': '法师',
        'function': '输出',
        'current_quality': '蓝色(2.4点)',
        '蓝色(2.4点)':  [1.2, 1.6, 0.3, 0.3, 3.0, 0.3],
        '紫色(2.7点)':  [1.8, 1.8, 0.8, 0.8, 4.2, 0.3],
        '金色(3.0点)':  [2.2, 2.0, 1.2, 1.2, 5.0, 0.4],
        '超模(3.5点)':  [2.8, 2.5, 1.5, 1.5, 6.2, 0.5],
    },
    '恶魂': {
        'id': 11,
        'profession': '法师',
        'function': '控制',
        'current_quality': '蓝色(2.4点)',
        '蓝色(2.4点)':  [1.2, 1.6, 0.4, 0.8, 2.2, 0.3],
        '紫色(2.7点)':  [1.8, 1.8, 0.9, 1.3, 3.0, 0.3],
        '金色(3.0点)':  [2.2, 2.0, 1.3, 1.8, 3.6, 0.4],
        '超模(3.5点)':  [2.8, 2.5, 1.8, 2.3, 4.4, 0.5],
    },
    '黑暗杨戬': {
        'id': 12,
        'profession': '坦克',
        'function': '输出',
        'current_quality': '金色(3.0点)',
        '蓝色(2.4点)':  [2.8, 0.3, 1.8, 0.8, 0.2, 1.8],
        '紫色(2.7点)':  [3.6, 0.3, 2.8, 1.3, 0.2, 2.5],
        '金色(3.0点)':  [4.2, 0.3, 3.3, 1.8, 0.2, 3.0],
        '超模(3.5点)':  [5.0, 0.5, 4.0, 2.3, 0.3, 3.8],
    },
}

# 颜色映射
colors = {
    '蓝色(2.4点)': '#0066CC',
    '紫色(2.7点)': '#9933FF',
    '金色(3.0点)': '#CC7700',
    '超模(3.5点)': '#DD3333',
}

quality_order = ['蓝色(2.4点)', '紫色(2.7点)', '金色(3.0点)', '超模(3.5点)']

def draw_chess_radar(chess_name, chess_info):
    """绘制单个棋子的六维雷达图"""

    angles = np.linspace(0, 2 * np.pi, N, endpoint=False).tolist()
    angles += angles[:1]

    fig, ax = plt.subplots(figsize=(13, 13), subplot_kw=dict(projection='polar'))

    # 绘制各品质
    for quality in quality_order:
        values = chess_info[quality]
        values_plot = values + values[:1]

        # 当前品质用更粗的线
        linewidth = 3.5 if quality == chess_info['current_quality'] else 2.5
        ax.plot(angles, values_plot, 'o-', linewidth=linewidth, label=quality,
                color=colors[quality], markersize=9 if quality == chess_info['current_quality'] else 7)
        ax.fill(angles, values_plot, alpha=0.15, color=colors[quality])

    # 设置标签和刻度
    ax.set_xticks(angles[:-1])
    ax.set_xticklabels(attributes, size=12, fontweight='bold')

    max_value = max([max(chess_info[q]) for q in quality_order])
    ax.set_ylim(0, max_value * 1.2)
    ax.set_rlabel_position(45)
    ax.grid(True, linestyle='--', alpha=0.7)

    # 标题
    title = f'{chess_name} (ID: {chess_info["id"]})\n{chess_info["profession"]} + {chess_info["function"]}型\n六维属性设计（当前品质：{chess_info["current_quality"]}）'
    plt.title(title, fontsize=16, fontweight='bold', pad=30)

    # 图例
    plt.legend(loc='upper right', bbox_to_anchor=(1.28, 1.12), fontsize=11,
               framealpha=0.95, edgecolor='black')

    # 添加说明文字
    note = '粗线表示当前品质。数值为相对权重，可用于验证新的属性设计是否符合需求'
    plt.figtext(0.5, 0.02, note, ha='center', fontsize=10, style='italic', color='#333333')

    plt.tight_layout()

    # 中文文件名
    filename = f'{chess_name}_六维属性设计确认.png'
    output_path = os.path.join(output_dir, filename)
    plt.savefig(output_path, dpi=150, bbox_inches='tight', facecolor='white')
    print(f"[OK] {chess_name} - Saved")
    plt.close()


# 生成5个棋子的雷达图
print("=" * 70)
print("Generating radar charts for current 5 chess pieces...")
print("=" * 70)

for chess_name in ['后羿', '嫦娥', '邪灵', '恶魂', '黑暗杨戬']:
    chess_info = current_chess_data[chess_name]
    draw_chess_radar(chess_name, chess_info)

print("\n" + "=" * 70)
print("Successfully generated 5 chess radar charts!")
print("=" * 70)
print("\nFiles saved to:")
print(output_dir)
print("\n待确认的棋子属性设计：")
for name, info in current_chess_data.items():
    print(f"  - {name} (ID: {info['id']}) - {info['profession']} + {info['function']}型 - 当前品质：{info['current_quality']}")
