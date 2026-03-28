#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
检查TXT文件的列数
"""

with open('AI工作区/配置表/SummonerSkillTable.txt', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

print('=== 检查TXT文件列数 ===')
for i, line in enumerate(lines):
    if i < 3:  # 跳过注释行
        continue
    cells = line.rstrip('\n\r').split('\t')
    col_count = len(cells)
    skill_id = cells[1] if len(cells) > 1 else "?"
    
    if col_count != 28:
        print(f'行 {i}: {col_count} 列 ❌ (ID={skill_id})')
    else:
        print(f'行 {i}: {col_count} 列 ✓ (ID={skill_id})')
