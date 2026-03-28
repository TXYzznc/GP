#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
详细检查TXT文件的列结构
"""

with open('AI工作区/配置表/SummonerSkillTable.txt', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

print('=== 详细检查TXT文件列结构 ===\n')

# 检查第6行（ID 103）
for row_idx in [6, 7, 8, 10]:
    line = lines[row_idx].rstrip('\n\r')
    cells = line.split('\t')
    
    skill_id = cells[1] if len(cells) > 1 else "?"
    skill_name = cells[3] if len(cells) > 3 else "?"
    
    print(f'行 {row_idx} (ID {skill_id}, {skill_name}):')
    print(f'  总列数: {len(cells)}')
    print(f'  列 22 (Params): {cells[21] if len(cells) > 21 else "缺失"}')
    print(f'  列 23 (EffectId): {cells[22] if len(cells) > 22 else "缺失"}')
    print(f'  列 24 (HitEffectId): {cells[23] if len(cells) > 23 else "缺失"}')
    print(f'  列 25 (EffectSpawnHeight): {cells[24] if len(cells) > 24 else "缺失"}')
    print(f'  列 26 (IconId): {cells[25] if len(cells) > 25 else "缺失"}')
    print(f'  列 27 (Desc): {cells[26][:50] if len(cells) > 26 else "缺失"}...')
    print()
