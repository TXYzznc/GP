import matplotlib.pyplot as plt
import numpy as np
import os
from matplotlib import font_manager as fm

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

# 从 role_attribute_model.md 提取的职业-功能六维权重
role_function_data = {
    '战士_输出': [2.5, 0.5, 1.5, 0.5, 0.5, 3.0],
    '战士_辅助': [3.0, 0.5, 1.5, 1.0, 0.5, 2.0],
    '战士_控制': [3.0, 0.5, 1.5, 1.5, 0.5, 1.5],
    '刺客_输出': [1.5, 0.5, 0.5, 0.5, 0.5, 4.0],
    '刺客_控制': [1.5, 0.5, 1.0, 1.0, 1.0, 3.0],
    '法师_输出': [1.5, 2.0, 0.5, 0.5, 3.0, 0.5],
    '法师_辅助': [2.0, 2.0, 1.0, 1.0, 2.0, 0.0],
    '法师_控制': [1.5, 2.0, 0.5, 1.0, 2.5, 0.5],
    '坦克_输出': [3.0, 0.5, 2.0, 1.0, 0.5, 1.5],
    '坦克_辅助': [3.0, 1.0, 2.0, 1.5, 1.0, 0.0],
    '坦克_控制': [3.0, 1.0, 2.0, 1.5, 1.0, 0.0],
    '射手_输出': [2.0, 0.5, 1.0, 1.0, 0.5, 3.0],
    '射手_辅助': [2.5, 0.5, 1.5, 1.0, 0.5, 2.0],
    '射手_控制': [2.0, 1.0, 1.5, 1.5, 1.0, 2.0],
}

# 品质系数（相对倍数）
quality_coefficients = {
    '蓝色(2.4点)': 1.0,
    '紫色(2.7点)': 1.27,
    '金色(3.0点)': 1.57,
    '超模(3.5点)': 1.89
}

# 颜色映射
colors = {
    '蓝色(2.4点)': '#0066CC',
    '紫色(2.7点)': '#9933FF',
    '金色(3.0点)': '#FFD700',
    '超模(3.5点)': '#FF6666'
}

def draw_radar(profession_function, data, output_filename):
    """绘制单个职业-功能的雷达图"""

    # 计算各品质的属性值
    quality_values = {}
    for quality, coeff in quality_coefficients.items():
        quality_values[quality] = [v * coeff for v in data]

    # 绘制雷达图
    angles = np.linspace(0, 2 * np.pi, N, endpoint=False).tolist()
    angles += angles[:1]  # 闭合多边形

    fig, ax = plt.subplots(figsize=(12, 12), subplot_kw=dict(projection='polar'))

    # 绘制各品质的雷达图
    for quality, values in quality_values.items():
        values_plot = values + values[:1]  # 闭合
        ax.plot(angles, values_plot, 'o-', linewidth=2.5, label=quality,
                color=colors[quality], markersize=8)
        ax.fill(angles, values_plot, alpha=0.15, color=colors[quality])

    # 设置属性标签
    ax.set_xticks(angles[:-1])
    ax.set_xticklabels(attributes, size=12, fontweight='bold')

    # 设置径向刻度
    max_value = max([max(v) for v in quality_values.values()])
    ax.set_ylim(0, max_value * 1.2)
    ax.set_rlabel_position(45)

    # 设置网格
    ax.grid(True, linestyle='--', alpha=0.7)

    # 标题
    profession, function = profession_function.split('_')
    function_name = {'输出': '输出型', '辅助': '辅助型', '控制': '控制型'}[function]
    profession_name = {'战士': '战士', '刺客': '刺客', '法师': '法师',
                       '坦克': '坦克', '射手': '射手'}[profession]

    title = f'{profession_name} + {function_name}\n职业-功能六维属性对比（四种品质）'
    plt.title(title, fontsize=16, fontweight='bold', pad=30)

    # 图例
    plt.legend(loc='upper right', bbox_to_anchor=(1.25, 1.12), fontsize=12,
               framealpha=0.95, edgecolor='black')

    # 添加说明文字
    plt.figtext(0.5, 0.02, '说明：数值为相对权重，代表该属性在职业-功能中的重要程度',
                ha='center', fontsize=10, style='italic')

    plt.tight_layout()

    # 保存
    output_path = os.path.join(output_dir, output_filename)
    plt.savefig(output_path, dpi=150, bbox_inches='tight', facecolor='white')
    print("[OK] Saved: " + output_filename)
    plt.close()

# 先绘制第一张作为示例：战士+输出
print("Start generating radar charts...")
print("-" * 50)

draw_radar('战士_输出', role_function_data['战士_输出'],
           '01_warrior_output.png')

print("-" * 50)
print("First chart generated successfully!")
print("Save location: " + output_dir)
