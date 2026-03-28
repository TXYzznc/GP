#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
详细检查XLSX文件的列结构
"""

import openpyxl

# 打开XLSX文件
wb = openpyxl.load_workbook('AAAGameData/DataTables/SummonerSkillTable.xlsx')
ws = wb.active

print('=== 详细检查XLSX文件列结构 ===\n')

# 打印第5行（第一个数据行）的所有列
print('第5行（ID 101）的所有列:')
for col_idx in range(1, 29):
    cell = ws.cell(5, col_idx)
    print(f'  列 {col_idx}: {cell.value}')
