#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import sys

# 添加工具目录到路径
sys.path.insert(0, '.Tools/COG-txt2xlsx')

from txt_to_xlsx_converter import TxtToXlsxConverter

# 删除旧文件
xlsx_path = 'AAAGameData/DataTables/SummonerSkillTable.xlsx'
if os.path.exists(xlsx_path):
    try:
        os.remove(xlsx_path)
        print(f"✓ 已删除旧文件: {xlsx_path}")
    except Exception as e:
        print(f"⚠ 无法删除旧文件: {e}")

# 运行转换
converter = TxtToXlsxConverter(auto_open_folder=False)
txt_path = 'AI工作区/配置表/SummonerSkillTable.txt'
output_dir = 'AAAGameData/DataTables'

print(f"开始转换: {txt_path}")
success = converter.convert_file(txt_path, output_dir)

if success:
    print(f"\n✓ 转换成功！")
    print(f"输出文件: {xlsx_path}")
else:
    print(f"\n✗ 转换失败")
    sys.exit(1)
