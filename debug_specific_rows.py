#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
调试特定行的列结构
"""

with open('Assets/AAAGame/DataTable/SummonerSkillTable.txt', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

print('=== 调试特定行 ===\n')
print(f'总行数: {len(lines)}\n')

# 检查所有数据行
for row_idx in range(4, len(lines)):
    line = lines[row_idx].rstrip('\n\r')
    if not line or line.startswith('#'):
        continue
    
    cells = line.split('\t')
    skill_id = cells[0] if len(cells) > 0 else "?"
    skill_name = cells[2] if len(cells) > 2 else "?"
    params = cells[21] if len(cells) > 21 else "缺失"
    
    if skill_id in ['105', '107']:
        print(f'行 {row_idx} (ID {skill_id}, {skill_name}):')
        print(f'  总列数: {len(cells)}')
        print(f'  列 22 (Params): {params}')
        print()
