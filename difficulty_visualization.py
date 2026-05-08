import numpy as np
import matplotlib.pyplot as plt
from matplotlib import cm
import matplotlib.patches as mpatches
import matplotlib
# 使用无衬线字体避免中文显示问题
matplotlib.rcParams['font.sans-serif'] = ['SimHei', 'DejaVu Sans']
matplotlib.rcParams['axes.unicode_minus'] = False

def get_summoner_coefficient(global_level, max_level=60):
    """计算召唤师系数"""
    if max_level <= 0:
        return 1.0
    player_ratio = global_level / max_level
    return 1 + np.power(player_ratio, 0.4) * 1.5

def calculate_enemy_difficulty(level_coefficient, summoner_coef, enemy_base_difficulty=3):
    """计算敌人难度值"""
    map_influence = np.power(level_coefficient / 50, 0.6)  # 改从 0.5 到 0.6
    summoner_influence = (summoner_coef - 1) / 1.5
    final_difficulty = (map_influence * 0.4 + summoner_influence * 0.6) * enemy_base_difficulty
    return final_difficulty

def map_difficulty_to_level(difficulty):
    """难度值映射到等级 1-10"""
    if difficulty < 0.5: return 1
    if difficulty < 0.7: return 2
    if difficulty < 0.9: return 3
    if difficulty < 1.2: return 4
    if difficulty < 1.65: return 5
    if difficulty < 2.2: return 6
    if difficulty < 2.9: return 7
    if difficulty < 3.6: return 8
    if difficulty < 4.5: return 9
    return 10

# 创建数据
player_levels = np.arange(0, 61, 5)  # 0, 5, 10, ..., 60
map_coefficients = np.array([1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100])

# 创建热力图数据
heatmap_data = np.zeros((len(player_levels), len(map_coefficients)))

for i, player_level in enumerate(player_levels):
    for j, map_coef in enumerate(map_coefficients):
        summoner_coef = get_summoner_coefficient(player_level)
        difficulty = calculate_enemy_difficulty(map_coef, summoner_coef)
        level = map_difficulty_to_level(difficulty)
        heatmap_data[i, j] = level

# 绘制热力图
fig, ax = plt.subplots(figsize=(14, 8))

# 使用自定义颜色映射（1-10 对应不同颜色）
im = ax.imshow(heatmap_data, cmap='RdYlGn_r', aspect='auto', vmin=1, vmax=10)

# 设置 X 轴和 Y 轴标签
ax.set_xticks(np.arange(len(map_coefficients)))
ax.set_yticks(np.arange(len(player_levels)))
ax.set_xticklabels(map_coefficients)
ax.set_yticklabels(player_levels)

ax.set_xlabel('地图等级系数', fontsize=12, fontweight='bold')
ax.set_ylabel('玩家等级', fontsize=12, fontweight='bold')
ax.set_title('敌人难度等级分布（敌人基础难度=3）', fontsize=14, fontweight='bold')

# 在每个格子中添加数值
for i in range(len(player_levels)):
    for j in range(len(map_coefficients)):
        text = ax.text(j, i, int(heatmap_data[i, j]),
                      ha="center", va="center", color="black", fontweight='bold', fontsize=10)

# 添加颜色条
cbar = plt.colorbar(im, ax=ax)
cbar.set_label('难度等级', fontsize=12, fontweight='bold')
cbar.set_ticks(np.arange(1, 11))

# 添加网格
ax.set_xticks(np.arange(len(map_coefficients))-.5, minor=True)
ax.set_yticks(np.arange(len(player_levels))-.5, minor=True)
ax.grid(which="minor", color="gray", linestyle='-', linewidth=0.5)

plt.tight_layout()
plt.savefig('difficulty_heatmap.png', dpi=150, bbox_inches='tight')
print("[OK] Heatmap generated: difficulty_heatmap.png")
plt.show()

# 额外：打印详细数据表
print("\n" + "="*80)
print("Enemy Difficulty Level Detail Table (Enemy Base Difficulty=3)")
print("="*80)
print(f"{'Player Lvl':<10}", end='')
for map_coef in map_coefficients:
    print(f"{map_coef:>6}", end='')
print()
print("-" * 80)

for i, player_level in enumerate(player_levels):
    print(f"{player_level:<10}", end='')
    for j, map_coef in enumerate(map_coefficients):
        print(f"{int(heatmap_data[i, j]):>6}", end='')
    print()

# 额外：绘制不同敌人基础难度的对比
fig2, axes = plt.subplots(1, 3, figsize=(16, 5))
enemy_difficulties = [1, 3, 5]
titles = ['敌人基础难度=1（弱）', '敌人基础难度=3（中）', '敌人基础难度=5（强）']

for idx, (enemy_diff, title) in enumerate(zip(enemy_difficulties, titles)):
    heatmap = np.zeros((len(player_levels), len(map_coefficients)))

    for i, player_level in enumerate(player_levels):
        for j, map_coef in enumerate(map_coefficients):
            summoner_coef = get_summoner_coefficient(player_level)
            difficulty = calculate_enemy_difficulty(map_coef, summoner_coef, enemy_diff)
            level = map_difficulty_to_level(difficulty)
            heatmap[i, j] = level

    ax = axes[idx]
    im = ax.imshow(heatmap, cmap='RdYlGn_r', aspect='auto', vmin=1, vmax=10)

    ax.set_xticks(np.arange(len(map_coefficients)))
    ax.set_yticks(np.arange(len(player_levels)))
    ax.set_xticklabels(map_coefficients, fontsize=9)
    ax.set_yticklabels(player_levels, fontsize=9)

    ax.set_xlabel('地图等级系数', fontsize=10)
    ax.set_ylabel('玩家等级', fontsize=10)
    ax.set_title(title, fontsize=11, fontweight='bold')

    # 添加数值
    for i in range(len(player_levels)):
        for j in range(len(map_coefficients)):
            ax.text(j, i, int(heatmap[i, j]),
                   ha="center", va="center", color="black", fontsize=8)

    ax.grid(which="minor", color="gray", linestyle='-', linewidth=0.3, alpha=0.3)

plt.tight_layout()
plt.savefig('difficulty_comparison.png', dpi=150, bbox_inches='tight')
print("\n[OK] Comparison chart generated: difficulty_comparison.png")
plt.show()

print("\nCharts saved to current directory")
