#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""提取PDF文件内容"""

import pdfplumber
import sys

def extract_pdf(input_path, output_path):
    content = []
    
    with pdfplumber.open(input_path) as pdf:
        for i, page in enumerate(pdf.pages):
            text = page.extract_text()
            if text:
                content.append(f"=== 第 {i+1} 页 ===")
                content.append(text)
            
            # 提取表格
            tables = page.extract_tables()
            for j, table in enumerate(tables):
                content.append(f"\n--- 表格 {j+1} ---")
                for row in table:
                    row_text = [str(cell) if cell else '' for cell in row]
                    content.append(' | '.join(row_text))
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(content))
    
    print(f'已提取 {len(pdf.pages)} 页')

if __name__ == '__main__':
    if len(sys.argv) >= 3:
        extract_pdf(sys.argv[1], sys.argv[2])