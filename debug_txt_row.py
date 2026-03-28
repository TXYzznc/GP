#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
调试TXT文件的行结构
"""

with open('AI工作区/配置表/SummonerSkillTable.txt', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

print('=== 调试TXT文件行结构 ===\n')

# 检查第5行（第一个数据行，ID 101）
line = lines[4].rstrip('\n\r')
cells = line.split('\t')

print(f'第5行（ID 101）:')
print(f'总列数: {len(cells)}')
for i, cell in enumerate(cells[:10]):
    print(f'  列 {i+1}: {cell}')
