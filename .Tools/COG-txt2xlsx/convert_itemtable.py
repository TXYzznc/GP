#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
直接调用转换工具转换 ItemTable.txt
"""

import sys
import os

# 添加当前目录到路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from txt_to_xlsx_converter import TxtToXlsxConverter

def main():
    # 源文件路径
    txt_path = r"D:\Sourcetree\Clash_Of_Gods\Assets\AAAGame\DataTable\ItemTable.txt"
    
    # 输出目录
    output_dir = r"D:\Sourcetree\Clash_Of_Gods\Assets\AAAGame\DataTable"
    
    print(f"[信息] 开始转换 ItemTable.txt...")
    print(f"[信息] 源文件: {txt_path}")
    print(f"[信息] 输出目录: {output_dir}")
    
    # 创建转换器（不自动打开文件夹）
    converter = TxtToXlsxConverter(auto_open_folder=False)
    
    # 执行转换
    success = converter.convert_file(txt_path, output_dir)
    
    if success:
        print(f"[成功] ItemTable.txt 转换完成！")
        print(f"[信息] 输出文件: {os.path.join(output_dir, 'ItemTable.xlsx')}")
        return 0
    else:
        print(f"[错误] ItemTable.txt 转换失败！")
        return 1

if __name__ == "__main__":
    sys.exit(main())
