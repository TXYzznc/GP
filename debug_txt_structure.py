#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
调试TXT文件结构 - 逐行逐列分析
"""

def analyze_txt_file(filepath):
    """详细分析TXT文件的结构"""
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        lines = f.readlines()
    
    print(f"=== TXT文件分析 ===")
    print(f"总行数: {len(lines)}\n")
    
    # 分析每一行
    for line_idx, line in enumerate(lines):
        line_clean = line.rstrip('\n\r')
        cells = line_clean.split('\t')
        
        print(f"行{line_idx} (共{len(cells)}列):")
        print(f"  原始: {repr(line_clean[:100])}")
        print(f"  列数: {len(cells)}")
        
        # 显示前10列的内容
        for col_idx in range(min(10, len(cells))):
            cell_value = cells[col_idx]
            if len(cell_value) > 30:
                cell_value = cell_value[:30] + "..."
            print(f"    列{col_idx}: {repr(cell_value)}")
        
        if len(cells) > 10:
            print(f"    ... 还有 {len(cells) - 10} 列")
        
        print()

if __name__ == "__main__":
    analyze_txt_file('AI工作区/配置表/SummonerSkillTable.txt')
