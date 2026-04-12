#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
独立的 TXT to XLSX 转换器 - 不依赖 openpyxl
使用 zipfile 和 xml 直接生成 XLSX 文件
"""

import os
import sys
import zipfile
import xml.etree.ElementTree as ET
from io import BytesIO
from datetime import datetime


class SimpleXlsxGenerator:
    """简单的 XLSX 生成器 - 不依赖 openpyxl"""
    
    def __init__(self):
        self.sheets = []
        self.workbook_rels = []
        self.sheet_counter = 0
    
    def add_sheet(self, sheet_name, data):
        """添加工作表"""
        self.sheet_counter += 1
        sheet_id = self.sheet_counter
        
        self.sheets.append({
            'id': sheet_id,
            'name': sheet_name,
            'data': data
        })
        
        return sheet_id
    
    def _escape_xml(self, text):
        """转义 XML 特殊字符"""
        if text is None:
            return ""
        text = str(text)
        text = text.replace('&', '&amp;')
        text = text.replace('<', '&lt;')
        text = text.replace('>', '&gt;')
        text = text.replace('"', '&quot;')
        text = text.replace("'", '&apos;')
        return text
    
    def _col_index_to_letter(self, col_index):
        """将列索引转换为列字母"""
        result = ""
        col_index += 1  # 从1开始
        while col_index > 0:
            col_index -= 1
            result = chr(65 + (col_index % 26)) + result
            col_index //= 26
        return result
    
    def _create_sheet_xml(self, sheet_data):
        """创建工作表 XML"""
        xml_lines = [
            '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>',
            '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">',
            '<sheetData>'
        ]
        
        for row_idx, row_data in enumerate(sheet_data, start=1):
            xml_lines.append(f'<row r="{row_idx}">')
            
            for col_idx, cell_value in enumerate(row_data):
                col_letter = self._col_index_to_letter(col_idx)
                cell_ref = f"{col_letter}{row_idx}"
                
                if cell_value is None or cell_value == "":
                    xml_lines.append(f'<c r="{cell_ref}"/>')
                else:
                    # 尝试转换为数字
                    try:
                        if isinstance(cell_value, (int, float)):
                            num_value = float(cell_value)
                            xml_lines.append(f'<c r="{cell_ref}"><v>{num_value}</v></c>')
                        else:
                            str_value = str(cell_value)
                            # 检查是否为数字字符串
                            try:
                                float(str_value)
                                xml_lines.append(f'<c r="{cell_ref}"><v>{str_value}</v></c>')
                            except:
                                # 作为字符串处理
                                escaped_value = self._escape_xml(str_value)
                                xml_lines.append(f'<c r="{cell_ref}" t="inlineStr"><is><t>{escaped_value}</t></is></c>')
                    except:
                        escaped_value = self._escape_xml(str(cell_value))
                        xml_lines.append(f'<c r="{cell_ref}" t="inlineStr"><is><t>{escaped_value}</t></is></c>')
            
            xml_lines.append('</row>')
        
        xml_lines.append('</sheetData>')
        xml_lines.append('</worksheet>')
        
        return '\n'.join(xml_lines)
    
    def save(self, output_path):
        """保存为 XLSX 文件"""
        with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_DEFLATED) as xlsx:
            # 1. 创建 [Content_Types].xml
            content_types_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>'''
            
            for sheet in self.sheets:
                content_types_xml += f'\n<Override PartName="/xl/worksheets/sheet{sheet["id"]}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
            
            content_types_xml += '\n<Override PartName="/xl/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>\n</Types>'
            
            xlsx.writestr('[Content_Types].xml', content_types_xml)
            
            # 2. 创建 _rels/.rels
            rels_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>'''
            xlsx.writestr('_rels/.rels', rels_xml)
            
            # 3. 创建 xl/_rels/workbook.xml.rels
            workbook_rels_xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\n<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
            
            for sheet in self.sheets:
                workbook_rels_xml += f'\n<Relationship Id="rId{sheet["id"]}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{sheet["id"]}.xml"/>'
            
            workbook_rels_xml += '\n<Relationship Id="rId' + str(len(self.sheets) + 1) + '" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="theme/theme1.xml"/>\n</Relationships>'
            
            xlsx.writestr('xl/_rels/workbook.xml.rels', workbook_rels_xml)
            
            # 4. 创建 xl/workbook.xml
            workbook_xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\n<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">\n<sheets>'
            
            for sheet in self.sheets:
                workbook_xml += f'\n<sheet name="{self._escape_xml(sheet["name"])}" sheetId="{sheet["id"]}" r:id="rId{sheet["id"]}"/>'
            
            workbook_xml += '\n</sheets>\n</workbook>'
            
            xlsx.writestr('xl/workbook.xml', workbook_xml)
            
            # 5. 创建工作表 XML
            for sheet in self.sheets:
                sheet_xml = self._create_sheet_xml(sheet['data'])
                xlsx.writestr(f'xl/worksheets/sheet{sheet["id"]}.xml', sheet_xml)
            
            # 6. 创建最小的 theme1.xml
            theme_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Office Theme">
<a:themeElements>
<a:clrScheme name="Office">
<a:dk1><a:srgbClr val="000000"/></a:dk1>
<a:lt1><a:srgbClr val="FFFFFF"/></a:lt1>
<a:dk2><a:srgbClr val="1F497D"/></a:dk2>
<a:lt2><a:srgbClr val="EBEBEB"/></a:lt2>
<a:accent1><a:srgbClr val="4472C4"/></a:accent1>
<a:accent2><a:srgbClr val="ED7D31"/></a:accent2>
<a:accent3><a:srgbClr val="A5A5A5"/></a:accent3>
<a:accent4><a:srgbClr val="FFC000"/></a:accent4>
<a:accent5><a:srgbClr val="5B9BD5"/></a:accent5>
<a:accent6><a:srgbClr val="70AD47"/></a:accent6>
<a:hyperlink><a:srgbClr val="0563C1"/></a:hyperlink>
<a:folHyperlink><a:srgbClr val="954F72"/></a:folHyperlink>
</a:clrScheme>
<a:fontScheme name="Office">
<a:majorFont>
<a:latin typeface="Calibri Light" pitchFamily="2" charset="0"/>
</a:majorFont>
<a:minorFont>
<a:latin typeface="Calibri" pitchFamily="2" charset="0"/>
</a:minorFont>
</a:fontScheme>
<a:fmtScheme name="Office">
<a:fillStyleLst>
<a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill>
<a:gradFill rotWithShape="1">
<a:gsLst>
<a:gs pos="0"><a:srgbClr val="000000"/></a:gs>
<a:gs pos="100000"><a:srgbClr val="FFFFFF"/></a:gs>
</a:gsLst>
<a:lin ang="5400000" scaled="0"/>
</a:gradFill>
<a:patternFill prst="ltDnDiag"><a:fgClr><a:srgbClr val="D0CECE"/></a:fgClr><a:bgClr><a:srgbClr val="FFFFFF"/></a:bgClr></a:patternFill>
</a:fillStyleLst>
<a:lnStyleLst>
<a:ln w="9525" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:srgbClr val="000000"/></a:solidFill><a:prstDash val="solid"/><a:round/></a:ln>
<a:ln w="25400" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:srgbClr val="000000"/></a:solidFill><a:prstDash val="solid"/><a:round/></a:ln>
<a:ln w="38100" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:srgbClr val="000000"/></a:solidFill><a:prstDash val="solid"/><a:round/></a:ln>
</a:lnStyleLst>
<a:effectStyleLst>
<a:effectLst/>
<a:effectLst/>
<a:effectLst/>
</a:effectStyleLst>
<a:bgFillStyleLst>
<a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill>
<a:gradFill rotWithShape="1">
<a:gsLst>
<a:gs pos="0"><a:srgbClr val="000000"/></a:gs>
<a:gs pos="100000"><a:srgbClr val="FFFFFF"/></a:gs>
</a:gsLst>
<a:lin ang="5400000" scaled="0"/>
</a:gradFill>
<a:patternFill prst="ltDnDiag"><a:fgClr><a:srgbClr val="D0CECE"/></a:fgClr><a:bgClr><a:srgbClr val="FFFFFF"/></a:bgClr></a:patternFill>
</a:bgFillStyleLst>
</a:fmtScheme>
</a:themeElements>
</a:theme>'''
            
            xlsx.writestr('xl/theme/theme1.xml', theme_xml)


class DataTableProcessor:
    """数据表处理器"""
    
    def __init__(self, data_table_file, encoding='utf-8-sig', name_row=1, type_row=2, 
                 default_value_row=None, comment_row=3, content_start_row=4, id_column=1):
        self.data_table_file = data_table_file
        self.encoding = encoding
        self.name_row = name_row
        self.type_row = type_row
        self.default_value_row = default_value_row
        self.comment_row = comment_row
        self.content_start_row = content_start_row
        self.id_column = id_column
        
        self.raw_values = []
        self.load_raw_data()
        
        self.name_row_data = self.raw_values[self.name_row] if self.name_row < len(self.raw_values) else []
        self.type_row_data = self.raw_values[self.type_row] if self.type_row < len(self.raw_values) else []
        self.comment_row_data = self.raw_values[self.comment_row] if self.comment_row is not None and self.comment_row < len(self.raw_values) else []
    
    def load_raw_data(self):
        """加载原始数据"""
        with open(self.data_table_file, 'r', encoding=self.encoding) as f:
            lines = f.readlines()
        
        for line in lines:
            line = line.rstrip('\n\r')
            if not line.strip():
                continue
            
            raw_value = line.split('\t')
            raw_value = [cell.strip() for cell in raw_value]
            self.raw_values.append(raw_value)
    
    def get_raw_row_count(self):
        return len(self.raw_values)
    
    def get_raw_column_count(self):
        if not self.raw_values or len(self.raw_values) < 2:
            return 0
        return len(self.raw_values[1])
    
    def get_name(self, raw_column):
        if raw_column < len(self.name_row_data):
            return self.name_row_data[raw_column]
        return ""
    
    def get_type(self, raw_column):
        if raw_column < len(self.type_row_data):
            return self.type_row_data[raw_column]
        return "string"
    
    def get_comment(self, raw_column):
        if self.comment_row_data and raw_column < len(self.comment_row_data):
            return self.comment_row_data[raw_column]
        return ""
    
    def get_value(self, raw_row, raw_column):
        if raw_row < len(self.raw_values) and raw_column < len(self.raw_values[raw_row]):
            return self.raw_values[raw_row][raw_column]
        return ""
    
    def is_comment_row(self, raw_row):
        if raw_row < len(self.raw_values):
            first_cell = self.raw_values[raw_row][0] if self.raw_values[raw_row] else ""
            return first_cell.startswith('#')
        return False
    
    def get_table_name(self):
        if len(self.raw_values) > 0 and len(self.raw_values[0]) > 1:
            return self.raw_values[0][1]
        return ""


class TxtToXlsxConverter:
    """TXT 转 XLSX 转换器"""
    
    def convert_file(self, txt_path, output_dir=None):
        """转换单个 txt 文件"""
        if not os.path.exists(txt_path):
            print(f"✗ 文件不存在: {txt_path}")
            return False
        
        try:
            processor = DataTableProcessor(txt_path, 'utf-8-sig', 1, 2, None, 3, 4, 1)
            
            table_name = processor.get_table_name()
            raw_column_count = processor.get_raw_column_count()
            
            # 构建数据
            sheet_data = []
            
            # 第1行: # + 表名
            row1 = ['#', table_name]
            row1.extend([''] * (raw_column_count - 2))
            sheet_data.append(row1)
            
            # 第2行: # + 字段名
            row2 = ['#']
            for col in range(1, raw_column_count):
                row2.append(processor.get_name(col))
            sheet_data.append(row2)
            
            # 第3行: # + 字段类型
            row3 = ['#']
            for col in range(1, raw_column_count):
                row3.append(processor.get_type(col))
            sheet_data.append(row3)
            
            # 第4行: # + 注释
            row4 = ['#']
            for col in range(1, raw_column_count):
                row4.append(processor.get_comment(col))
            sheet_data.append(row4)
            
            # 数据行
            for row_idx in range(processor.content_start_row, processor.get_raw_row_count()):
                if processor.is_comment_row(row_idx):
                    continue
                
                data_row = ['']  # 第一列为空
                for col in range(1, raw_column_count):
                    data_row.append(processor.get_value(row_idx, col))
                sheet_data.append(data_row)
            
            # 生成 XLSX
            if output_dir is None:
                output_dir = "output"
            
            os.makedirs(output_dir, exist_ok=True)
            
            base_name = os.path.basename(txt_path)
            xlsx_name = os.path.splitext(base_name)[0] + '.xlsx'
            output_path = os.path.join(output_dir, xlsx_name)
            
            generator = SimpleXlsxGenerator()
            generator.add_sheet("Sheet 1", sheet_data)
            generator.save(output_path)
            
            print(f"✓ 已生成: {output_path}")
            return True
            
        except Exception as e:
            print(f"✗ 转换失败: {e}")
            import traceback
            traceback.print_exc()
            return False


def main():
    if len(sys.argv) < 2:
        print("使用方法: python txt_to_xlsx_converter_standalone.py <txt文件路径> [输出目录]")
        return
    
    txt_path = sys.argv[1]
    output_dir = sys.argv[2] if len(sys.argv) > 2 else "output"
    
    converter = TxtToXlsxConverter()
    converter.convert_file(txt_path, output_dir)


if __name__ == "__main__":
    main()
