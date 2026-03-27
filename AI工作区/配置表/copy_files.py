#!/usr/bin/env python3
import shutil
import os

# 源目录和目标目录
source_dir = "MCP工作区/配置表"
target_dir = "AAAGameData/DataTables"

# 要复制的文件列表
files = [
    "ItemTable.xlsx",
    "SpecialEffectTable.xlsx",
    "AffixTable.xlsx",
    "SynergyTable.xlsx"
]

# 复制文件
for file in files:
    source_file = os.path.join(source_dir, file)
    target_file = os.path.join(target_dir, file)
    
    if os.path.exists(source_file):
        # 先删除目标文件（如果存在）
        if os.path.exists(target_file):
            os.remove(target_file)
            print(f"🗑️  已删除旧文件: {file}")
        
        # 复制新文件
        shutil.copy2(source_file, target_file)
        print(f"✅ 已复制: {file}")
    else:
        print(f"❌ 文件不存在: {source_file}")

print("\n📁 所有文件已复制到 AAAGameData/DataTables/")
