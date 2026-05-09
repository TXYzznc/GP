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

# 战士职业的三种功能 - 多品质六维属性定义
# 每个品质的权重经过精心设计，不是线性倍数，而是反映不同属性的差异增长
warrior_data = {
    '输出': {
        '蓝色(2.4点)':  [2.0, 0.3, 0.8, 0.2, 0.2, 2.5],    # 基础版本：重攻击
        '紫色(2.7点)':  [3.0, 0.3, 2.3, 0.7, 0.2, 4.5],    # 攻击+2.0, 生命+1.0, 护甲+1.5, 魔抗+0.5
        '金色(3.0点)':  [3.5, 0.3, 2.8, 1.0, 0.2, 5.5],    # 攻击+1.0, 生命+0.5, 护甲+0.5, 魔抗+0.3
        '超模(3.5点)':  [4.0, 0.5, 3.5, 1.5, 0.3, 6.5],    # Boss全面加强
    },
    '辅助': {
        '蓝色(2.4点)':  [2.5, 0.3, 1.0, 0.7, 0.2, 1.8],    # 基础版本：防守+中等输出
        '紫色(2.7点)':  [3.3, 0.3, 2.2, 1.3, 0.2, 2.5],    # 生命+0.8, 护甲+1.2, 魔抗+0.6, 攻击+0.7
        '金色(3.0点)':  [3.8, 0.3, 2.8, 1.8, 0.2, 3.0],    # 生命+0.5, 护甲+0.6, 魔抗+0.5, 攻击+0.5
        '超模(3.5点)':  [4.5, 0.5, 3.5, 2.2, 0.3, 3.8],    # Boss加强
    },
    '控制': {
        '蓝色(2.4点)':  [2.5, 0.3, 1.2, 1.0, 0.2, 1.5],    # 基础版本：坦克型控制
        '紫色(2.7点)':  [3.3, 0.3, 2.4, 1.8, 0.2, 2.0],    # 生命+0.8, 护甲+1.2, 魔抗+0.8
        '金色(3.0点)':  [3.8, 0.3, 2.8, 2.3, 0.2, 2.5],    # 生命+0.5, 护甲+0.4, 魔抗+0.5
        '超模(3.5点)':  [4.5, 0.5, 3.5, 2.8, 0.3, 3.2],    # Boss加强
    },
}

# 颜色映射（金色改为深黄/橙色）
colors = {
    '蓝色(2.4点)': '#0066CC',      # 蓝色
    '紫色(2.7点)': '#9933FF',      # 紫色
    '金色(3.0点)': '#CC7700',      # 深橙黄（明度更低）
    '超模(3.5点)': '#DD3333',      # 深红（Boss特殊）
}

# 品质顺序
quality_order = ['蓝色(2.4点)', '紫色(2.7点)', '金色(3.0点)', '超模(3.5点)']

def draw_warrior_radar(function_type, function_data, filename_index):
    """绘制战士职业某种功能的六维雷达图"""

    # 绘制雷达图
    angles = np.linspace(0, 2 * np.pi, N, endpoint=False).tolist()
    angles += angles[:1]  # 闭合多边形

    fig, ax = plt.subplots(figsize=(13, 13), subplot_kw=dict(projection='polar'))

    # 绘制各品质的雷达图
    for quality in quality_order:
        values = function_data[quality]
        values_plot = values + values[:1]  # 闭合

        ax.plot(angles, values_plot, 'o-', linewidth=2.5, label=quality,
                color=colors[quality], markersize=8)
        ax.fill(angles, values_plot, alpha=0.15, color=colors[quality])

    # 设置属性标签
    ax.set_xticks(angles[:-1])
    ax.set_xticklabels(attributes, size=12, fontweight='bold')

    # 设置径向刻度
    max_value = max([max(function_data[q]) for q in quality_order])
    ax.set_ylim(0, max_value * 1.2)
    ax.set_rlabel_position(45)

    # 设置网格
    ax.grid(True, linestyle='--', alpha=0.7)

    # 标题
    function_names = {'输出': '输出型', '辅助': '辅助型', '控制': '控制型'}
    title = f'战士 + {function_names[function_type]}\n职业-功能六维属性对比（四种品质）'
    plt.title(title, fontsize=16, fontweight='bold', pad=30)

    # 图例
    plt.legend(loc='upper right', bbox_to_anchor=(1.28, 1.12), fontsize=12,
               framealpha=0.95, edgecolor='black')

    # 添加说明文字
    plt.figtext(0.5, 0.02, '说明：数值为相对权重，不同品质间的增长不是线性的，而是根据职业特性灵活设计',
                ha='center', fontsize=10, style='italic', color='#333333')

    plt.tight_layout()

    # 保存
    output_path = os.path.join(output_dir, f'{filename_index:02d}_Warrior_{function_type}.png')
    plt.savefig(output_path, dpi=150, bbox_inches='tight', facecolor='white')
    print(f"[{filename_index:02d}] Saved: Warrior_{function_type}.png")
    plt.close()


# 生成战士的三张图
print("Generating Warrior profession radar charts...")
print("-" * 60)

draw_warrior_radar('输出', warrior_data['输出'], 1)
draw_warrior_radar('辅助', warrior_data['辅助'], 2)
draw_warrior_radar('控制', warrior_data['控制'], 3)

print("-" * 60)
print("Successfully generated 3 Warrior charts!")
print(f"Output directory: {output_dir}")
print("\nKey improvements:")
print("- Gold color changed to deep orange (#CC7700) for better contrast")
print("- Each quality level has independent attribute weights (not linear)")
print("- Attributes grow differently based on profession-function archetype")
