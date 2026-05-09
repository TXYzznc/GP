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

# 全部职业-功能的多品质六维属性定义
# 基于role_attribute_model.md和战士的设计模式扩展
all_professions_data = {
    '战士': {
        '输出': {
            '蓝色(2.4点)':  [2.0, 0.3, 0.8, 0.2, 0.2, 2.5],
            '紫色(2.7点)':  [3.0, 0.3, 2.3, 0.7, 0.2, 4.5],
            '金色(3.0点)':  [3.5, 0.3, 2.8, 1.0, 0.2, 5.5],
            '超模(3.5点)':  [4.0, 0.5, 3.5, 1.5, 0.3, 6.5],
        },
        '辅助': {
            '蓝色(2.4点)':  [2.5, 0.3, 1.0, 0.7, 0.2, 1.8],
            '紫色(2.7点)':  [3.3, 0.3, 2.2, 1.3, 0.2, 2.5],
            '金色(3.0点)':  [3.8, 0.3, 2.8, 1.8, 0.2, 3.0],
            '超模(3.5点)':  [4.5, 0.5, 3.5, 2.2, 0.3, 3.8],
        },
        '控制': {
            '蓝色(2.4点)':  [2.5, 0.3, 1.2, 1.0, 0.2, 1.5],
            '紫色(2.7点)':  [3.3, 0.3, 2.4, 1.8, 0.2, 2.0],
            '金色(3.0点)':  [3.8, 0.3, 2.8, 2.3, 0.2, 2.5],
            '超模(3.5点)':  [4.5, 0.5, 3.5, 2.8, 0.3, 3.2],
        },
    },
    '刺客': {
        '输出': {
            '蓝色(2.4点)':  [1.2, 0.3, 0.3, 0.2, 0.2, 3.5],    # 极高攻击，极低防御
            '紫色(2.7点)':  [1.8, 0.3, 0.8, 0.5, 0.2, 5.5],
            '金色(3.0点)':  [2.2, 0.3, 1.2, 0.8, 0.2, 6.5],
            '超模(3.5点)':  [2.8, 0.5, 1.5, 1.2, 0.3, 7.8],
        },
        '控制': {
            '蓝色(2.4点)':  [1.2, 0.8, 0.6, 0.6, 0.8, 2.8],    # 高攻击+控制法强
            '紫色(2.7点)':  [1.8, 1.0, 1.2, 1.2, 1.2, 4.0],
            '金色(3.0点)':  [2.2, 1.2, 1.8, 1.8, 1.5, 4.8],
            '超模(3.5点)':  [2.8, 1.5, 2.2, 2.2, 1.8, 5.8],
        },
    },
    '法师': {
        '输出': {
            '蓝色(2.4点)':  [1.2, 1.6, 0.3, 0.3, 3.0, 0.3],    # 极高法强，极低生命防御
            '紫色(2.7点)':  [1.8, 1.8, 0.8, 0.8, 4.2, 0.3],
            '金色(3.0点)':  [2.2, 2.0, 1.2, 1.2, 5.0, 0.4],
            '超模(3.5点)':  [2.8, 2.5, 1.5, 1.5, 6.2, 0.5],
        },
        '辅助': {
            '蓝色(2.4点)':  [1.8, 1.6, 0.8, 0.8, 1.8, 0.2],    # 法强+生命平衡
            '紫色(2.7点)':  [2.5, 1.8, 1.5, 1.5, 2.5, 0.3],
            '金色(3.0点)':  [3.0, 2.0, 2.0, 2.0, 3.0, 0.3],
            '超模(3.5点)':  [3.8, 2.5, 2.5, 2.5, 3.8, 0.5],
        },
        '控制': {
            '蓝色(2.4点)':  [1.2, 1.6, 0.4, 0.8, 2.2, 0.3],    # 法强为主+控制能力
            '紫色(2.7点)':  [1.8, 1.8, 0.9, 1.3, 3.0, 0.3],
            '金色(3.0点)':  [2.2, 2.0, 1.3, 1.8, 3.6, 0.4],
            '超模(3.5点)':  [2.8, 2.5, 1.8, 2.3, 4.4, 0.5],
        },
    },
    '坦克': {
        '输出': {
            '蓝色(2.4点)':  [2.8, 0.3, 1.8, 0.8, 0.2, 1.8],    # 生命+防御为主，低输出
            '紫色(2.7点)':  [3.6, 0.3, 2.8, 1.3, 0.2, 2.5],
            '金色(3.0点)':  [4.2, 0.3, 3.3, 1.8, 0.2, 3.0],
            '超模(3.5点)':  [5.0, 0.5, 4.0, 2.3, 0.3, 3.8],
        },
        '辅助': {
            '蓝色(2.4点)':  [2.8, 1.0, 1.8, 1.2, 1.0, 0.2],    # 生命+防御+法强辅助
            '紫色(2.7点)':  [3.6, 1.2, 2.8, 2.0, 1.5, 0.3],
            '金色(3.0点)':  [4.2, 1.4, 3.3, 2.5, 2.0, 0.3],
            '超模(3.5点)':  [5.0, 1.8, 4.0, 3.0, 2.5, 0.5],
        },
        '控制': {
            '蓝色(2.4点)':  [2.8, 1.0, 1.8, 1.2, 1.0, 0.2],    # 均衡防守+控制
            '紫色(2.7点)':  [3.6, 1.2, 2.8, 2.0, 1.5, 0.3],
            '金色(3.0点)':  [4.2, 1.4, 3.3, 2.5, 2.0, 0.3],
            '超模(3.5点)':  [5.0, 1.8, 4.0, 3.0, 2.5, 0.5],
        },
    },
    '射手': {
        '输出': {
            '蓝色(2.4点)':  [1.8, 0.3, 0.8, 0.8, 0.3, 2.8],    # 高攻击，中生命，低防御
            '紫色(2.7点)':  [2.5, 0.3, 1.8, 1.3, 0.3, 4.2],
            '金色(3.0点)':  [3.0, 0.3, 2.3, 1.8, 0.3, 5.0],
            '超模(3.5点)':  [3.8, 0.5, 2.8, 2.3, 0.4, 6.0],
        },
        '辅助': {
            '蓝色(2.4点)':  [2.0, 0.3, 1.2, 0.8, 0.3, 1.8],    # 平衡输出+防御
            '紫色(2.7点)':  [2.7, 0.3, 2.0, 1.3, 0.3, 2.8],
            '金色(3.0点)':  [3.2, 0.3, 2.5, 1.8, 0.3, 3.3],
            '超模(3.5点)':  [4.0, 0.5, 3.0, 2.3, 0.4, 4.2],
        },
        '控制': {
            '蓝色(2.4点)':  [1.8, 0.8, 1.2, 1.2, 0.8, 1.8],    # 平衡输出+控制
            '紫色(2.7点)':  [2.5, 1.0, 2.0, 2.0, 1.2, 2.8],
            '金色(3.0点)':  [3.0, 1.2, 2.5, 2.5, 1.5, 3.3],
            '超模(3.5点)':  [3.8, 1.5, 3.0, 3.0, 1.8, 4.2],
        },
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

# 职业中文显示名称
profession_display = {
    '战士': 'Warrior',
    '刺客': 'Assassin',
    '法师': 'Mage',
    '坦克': 'Tank',
    '射手': 'Archer',
}

function_display = {
    '输出': 'Output',
    '辅助': 'Support',
    '控制': 'Control',
}

def draw_profession_radar(profession, function_type, function_data, chart_index):
    """绘制某职业某功能的六维雷达图"""

    angles = np.linspace(0, 2 * np.pi, N, endpoint=False).tolist()
    angles += angles[:1]

    fig, ax = plt.subplots(figsize=(13, 13), subplot_kw=dict(projection='polar'))

    # 绘制各品质
    for quality in quality_order:
        values = function_data[quality]
        values_plot = values + values[:1]
        ax.plot(angles, values_plot, 'o-', linewidth=2.5, label=quality,
                color=colors[quality], markersize=8)
        ax.fill(angles, values_plot, alpha=0.15, color=colors[quality])

    # 设置标签和刻度
    ax.set_xticks(angles[:-1])
    ax.set_xticklabels(attributes, size=12, fontweight='bold')

    max_value = max([max(function_data[q]) for q in quality_order])
    ax.set_ylim(0, max_value * 1.2)
    ax.set_rlabel_position(45)
    ax.grid(True, linestyle='--', alpha=0.7)

    # 标题
    function_names = {'输出': '输出型', '辅助': '辅助型', '控制': '控制型'}
    title = f'{profession} + {function_names[function_type]}\n职业-功能六维属性对比（四种品质）'
    plt.title(title, fontsize=16, fontweight='bold', pad=30)

    # 图例
    plt.legend(loc='upper right', bbox_to_anchor=(1.28, 1.12), fontsize=12,
               framealpha=0.95, edgecolor='black')

    plt.figtext(0.5, 0.02, '数值为相对权重，体现各职业-功能的属性侧重点与品质差异',
                ha='center', fontsize=10, style='italic', color='#333333')

    plt.tight_layout()

    # 保存
    output_path = os.path.join(output_dir,
                               f'{chart_index:02d}_{profession_display[profession]}_{function_display[function_type]}.png')
    plt.savefig(output_path, dpi=150, bbox_inches='tight', facecolor='white')
    print(f"[{chart_index:02d}] {profession} + {function_names[function_type]} - Saved")
    plt.close()


# 生成所有图表
print("=" * 70)
print("Generating all profession-function radar charts (14 total)...")
print("=" * 70)

professions_list = ['战士', '刺客', '法师', '坦克', '射手']
functions_list = ['输出', '辅助', '控制']

chart_index = 1

for profession in professions_list:
    print(f"\n{profession}:")
    for function_type in functions_list:
        # 跳过不存在的组合
        if function_type not in all_professions_data[profession]:
            print(f"  [{chart_index:02d}] {function_type} - SKIPPED (not available)")
            chart_index += 1
            continue

        data = all_professions_data[profession][function_type]
        draw_profession_radar(profession, function_type, data, chart_index)
        chart_index += 1

print("\n" + "=" * 70)
print(f"Successfully generated 14 profession-function radar charts!")
print(f"Output directory: {output_dir}")
print("=" * 70)
