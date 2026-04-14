#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
合并论文章节文件为完整版本
"""

import os

# 章节文件列表（按顺序）
chapters = [
    "00_目录.md",
    "01_摘要.md",
    "02_第1章_绪论.md",
    "03_第2章_相关技术与理论基础.md",
    "04_第3章_系统需求分析与总体设计.md",
    "05_第4章_核心系统详细设计与实现.md",
    "06_第5章_技术美术与性能优化.md",
    "07_第6章_系统测试.md",
    "08_第7章_总结与展望.md"
]

# 输入输出路径
input_dir = r"d:\unity\UnityProject\GP\Clash_Of_Gods\项目知识库（AI）\论文写作\章节文件_v2"
output_file = r"d:\unity\UnityProject\GP\Clash_Of_Gods\项目知识库（AI）\论文写作\Clash_Of_Gods_毕业论文_完整版.md"

# 合并文件
with open(output_file, 'w', encoding='utf-8') as outfile:
    for chapter in chapters:
        input_path = os.path.join(input_dir, chapter)
        if os.path.exists(input_path):
            with open(input_path, 'r', encoding='utf-8') as infile:
                content = infile.read()
                outfile.write(content)
                outfile.write('\n\n---\n\n')  # 章节分隔
            print(f"✓ 已合并: {chapter}")
        else:
            print(f"✗ 文件不存在: {chapter}")

print(f"\n✓ 论文合并完成！")
print(f"输出文件: {output_file}")

# 统计字数
try:
    with open(output_file, 'r', encoding='utf-8') as f:
        content = f.read()
        # 估算中文字数（去除标点和空格）
        import re
        chinese_chars = len(re.findall(r'[\u4e00-\u9fff]', content))
        total_chars = len(content)
        print(f"\n字数统计:")
        print(f"  - 中文字符数: ~{chinese_chars} 字")
        print(f"  - 总字符数: {total_chars} 字符")
except Exception as e:
    print(f"字数统计失败: {e}")
