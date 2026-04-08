#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Graphviz .dot 文件渲染工具
使用 graphviz 库将 .dot 文件渲染为 PNG 和 SVG
"""

import os
import sys
import io
from pathlib import Path

# 修复 Windows 编码问题
if sys.platform == 'win32':
    import codecs
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

try:
    import graphviz
except ImportError:
    print("[ERROR] graphviz 库未安装")
    print("运行: pip install graphviz")
    sys.exit(1)


def render_dot_file(dot_file_path, output_dir=None, formats=None):
    """
    渲染 .dot 文件

    Args:
        dot_file_path: .dot 文件路径
        output_dir: 输出目录，默认为 .dot 文件所在目录
        formats: 输出格式列表，默认为 ['png', 'svg']
    """
    if formats is None:
        formats = ['png', 'svg']

    # 读取 .dot 文件
    dot_path = Path(dot_file_path)
    if not dot_path.exists():
        print(f"[ERROR] 文件不存在: {dot_file_path}")
        return False

    with open(dot_path, 'r', encoding='utf-8') as f:
        dot_content = f.read()

    if output_dir is None:
        output_dir = str(dot_path.parent)

    output_name = dot_path.stem  # 文件名不含扩展名

    try:
        # 创建 graphviz.Source 对象
        src = graphviz.Source(dot_content, format='png')

        print(f"[RENDER] 正在渲染: {dot_path.name}")
        print(f"[OUTPUT] 输出目录: {output_dir}")

        # 渲染多个格式
        for fmt in formats:
            print(f"[GENERATING] 生成 {fmt.upper()}...", end=" ", flush=True)
            src.format = fmt
            output_path = os.path.join(output_dir, output_name)
            src.render(output_path, cleanup=True)
            file_size = os.path.getsize(f"{output_path}.{fmt}") / 1024
            print(f"[OK] ({file_size:.1f}KB)")

        print(f"\n[SUCCESS] 渲染完成!")
        print(f"[OUTPUT] PNG: {output_dir}/{output_name}.png")
        print(f"[OUTPUT] SVG: {output_dir}/{output_name}.svg")
        return True

    except Exception as e:
        print(f"\n[ERROR] 渲染失败: {e}")
        print("\n[INFO] 可能原因:")
        print("   1. 系统未安装 Graphviz (访问 https://graphviz.org/download/)")
        print("   2. .dot 文件语法错误")
        print("\n[SOLUTION] 解决方案:")
        print("   • Windows: choco install graphviz")
        print("   • macOS: brew install graphviz")
        print("   • Linux: sudo apt-get install graphviz")
        return False


if __name__ == "__main__":
    # 定位 .dot 文件
    script_dir = Path(__file__).parent
    dot_file = script_dir / "项目架构图-正交线条版.dot"

    if len(sys.argv) > 1:
        dot_file = Path(sys.argv[1])

    success = render_dot_file(str(dot_file), formats=['png', 'svg'])
    sys.exit(0 if success else 1)
