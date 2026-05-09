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

# 现有5个棋子的品质确定属性（基于品质和职业-功能模型）
# 值是品质等级的六维权重，乘以阶级倍数后得到实际属性
current_chess_data = {
    '后羿': {
        'id': 1,
        'profession': '射手',
        'function': '输出',
        'quality': '紫色(2.7点)',
        'base_weight': [2.5, 0.3, 1.8, 1.3, 0.3, 4.2],  # 紫色射手输出
    },
    '嫦娥': {
        'id': 4,
        'profession': '法师',
        'function': '辅助',
        'quality': '紫色(2.7点)',
        'base_weight': [2.5, 1.8, 1.5, 1.5, 2.5, 0.3],  # 紫色法师辅助
    },
    '邪灵': {
        'id': 10,
        'profession': '法师',
        'function': '输出',
        'quality': '蓝色(2.4点)',
        'base_weight': [1.2, 1.6, 0.3, 0.3, 3.0, 0.3],  # 蓝色法师输出
    },
    '恶魂': {
        'id': 11,
        'profession': '法师',
        'function': '控制',
        'quality': '蓝色(2.4点)',
        'base_weight': [1.2, 1.6, 0.4, 0.8, 2.2, 0.3],  # 蓝色法师控制
    },
    '黑暗杨戬': {
        'id': 12,
        'profession': '坦克',
        'function': '输出',
        'quality': '金色(3.0点)',
        'base_weight': [4.2, 0.3, 3.3, 1.8, 0.2, 3.0],  # 金色坦克输出
    },
}

# 阶级倍数
tier_multipliers = {
    '一阶': 1.0,
    '二阶': 1.6,
    '三阶': 2.4,
}

# 颜色映射
colors = {
    '一阶': '#0066CC',
    '二阶': '#9933FF',
    '三阶': '#CC7700',
}

tier_order = ['一阶', '二阶', '三阶']

def draw_chess_tier_radar(chess_name, chess_info):
    """绘制单个棋子的三阶级六维雷达图"""

    # 计算各阶级的属性值
    tier_values = {}
    base_weight = chess_info['base_weight']
    for tier, multiplier in tier_multipliers.items():
        tier_values[tier] = [w * multiplier for w in base_weight]

    angles = np.linspace(0, 2 * np.pi, N, endpoint=False).tolist()
    angles += angles[:1]

    fig, ax = plt.subplots(figsize=(13, 13), subplot_kw=dict(projection='polar'))

    # 绘制各阶级
    for tier in tier_order:
        values = tier_values[tier]
        values_plot = values + values[:1]

        ax.plot(angles, values_plot, 'o-', linewidth=2.5, label=tier,
                color=colors[tier], markersize=8)
        ax.fill(angles, values_plot, alpha=0.15, color=colors[tier])

    # 设置标签和刻度
    ax.set_xticks(angles[:-1])
    ax.set_xticklabels(attributes, size=12, fontweight='bold')

    max_value = max([max(tier_values[t]) for t in tier_order])
    ax.set_ylim(0, max_value * 1.2)
    ax.set_rlabel_position(45)
    ax.grid(True, linestyle='--', alpha=0.7)

    # 标题
    title = f'{chess_name} (ID: {chess_info["id"]})\n{chess_info["profession"]} + {chess_info["function"]}型\n品质：{chess_info["quality"]} | 不同阶级的六维属性对比'
    plt.title(title, fontsize=16, fontweight='bold', pad=30)

    # 图例
    plt.legend(loc='upper right', bbox_to_anchor=(1.28, 1.12), fontsize=12,
               framealpha=0.95, edgecolor='black')

    # 添加说明文字
    note = '显示同品质在不同阶级（一阶→二阶→三阶，倍数 1.0x→1.6x→2.4x）下的属性变化'
    plt.figtext(0.5, 0.02, note, ha='center', fontsize=10, style='italic', color='#333333')

    plt.tight_layout()

    # 中文文件名
    filename = f'{chess_name}_阶级对比_六维属性.png'
    output_path = os.path.join(output_dir, filename)
    plt.savefig(output_path, dpi=150, bbox_inches='tight', facecolor='white')
    print(f"[OK] {chess_name} - Saved as {filename}")
    plt.close()


# 生成5个棋子的阶级对比雷达图
print("=" * 70)
print("Generating tier comparison radar charts for 5 chess pieces...")
print("=" * 70)

for chess_name in ['后羿', '嫦娥', '邪灵', '恶魂', '黑暗杨戬']:
    chess_info = current_chess_data[chess_name]
    draw_chess_tier_radar(chess_name, chess_info)

print("\n" + "=" * 70)
print("Successfully generated 5 chess tier comparison charts!")
print("=" * 70)
print("\nFiles saved to:")
print(output_dir)
print("\n棋子品质及阶级对比：")
for name, info in current_chess_data.items():
    print(f"  - {name} (ID: {info['id']}) - {info['profession']} + {info['function']}型")
    print(f"    品质：{info['quality']} | 基础权重：{info['base_weight']}")
