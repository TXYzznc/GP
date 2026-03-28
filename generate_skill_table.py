#!/usr/bin/env python3
# -*- coding: utf-8 -*-

# 读取原始文件，分析列数和格式
with open('Assets/AAAGame/DataTable/SummonerSkillTable.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 打印前几行，分析列数
for i, line in enumerate(lines[:4]):
    cols = line.rstrip('\n').split('\t')
    print(f"Line {i}: {len(cols)} columns")
    for j, col in enumerate(cols):
        print(f"  Col {j}: '{col}'")

print("\n" + "="*80 + "\n")

# 分析数据行
for i in range(4, min(7, len(lines))):
    line = lines[i]
    cols = line.rstrip('\n').split('\t')
    print(f"Data Line {i-3}: {len(cols)} columns")
    for j, col in enumerate(cols):
        print(f"  Col {j}: '{col}'")
