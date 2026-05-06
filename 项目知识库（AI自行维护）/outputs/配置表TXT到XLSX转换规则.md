# 配置表 TXT → XLSX 转换规则详解

## 概述

项目的配置表存储在两个位置：
- **源文件**（TXT）: `Assets/AAAGame/DataTable/SpecialEffectTable.txt` 
- **Excel 文件**（XLSX）: `AAAGameData/DataTables/SpecialEffectTable.xlsx`

这两个文件必须保持一致。转换流程需要遵循特定的格式规则。

---

## TXT 文件格式规范

### 1. 文件编码
- **编码**: UTF-8（带 BOM）
- **行尾**: Windows 格式（`\r\n`）

### 2. 行结构

TXT 文件共有 **4 个元数据行** + **N 个数据行**：

```
第 1 行: # <Tab> 表名 <Tab> <Tab> ... (多个尾部制表符)
第 2 行: # <Tab> 列名1 <Tab> <Tab> 列名2 <Tab> 列名3 ... <Tab> 列名N
第 3 行: # <Tab> 类型1 <Tab> <Tab> 类型2 <Tab> 类型3 ... <Tab> 类型N
第 4 行: # <Tab> 说明1 <Tab> 表说明(长) <Tab> 字段说明 ... <Tab> 字段说明N
第 5+行: <Tab> ID值 <Tab> <Tab> 字段2 <Tab> 字段3 ... <Tab> 字段N
```

### 3. 制表符使用模式

**关键原则**: 所有列之间**都使用制表符（TAB）分隔**，不使用空格。

#### 第 1 行（表名行）
```
# [TAB] SpecialEffectTable [TAB] [TAB] [TAB] [TAB] [TAB] [TAB] [TAB] [TAB] [TAB] [TAB] [TAB] [TAB]
```
- 开头: `#` + `\t` 
- 表名: `SpecialEffectTable`
- 尾部: 12 个制表符（对齐所有列）

#### 第 2 行（列名行）
```
# [TAB] ID [TAB] [TAB] Name [TAB] EffectCategory [TAB] EffectType [TAB] Description [TAB] BuffIds [TAB] SelfBuffIds [TAB] Cooldown [TAB] IconId [TAB] Rarity [TAB] Weight [TAB] EffectParams
```

**列对应关系**（原始位置）：
| 位置 | 制表符后内容 | XLSX 列 | 说明 |
|------|------------|---------|------|
| 1 | `#` | - | 注释标记 |
| 2 | `ID` | 2 | 主键 ID |
| 3 | 空 | 3 | 对齐用（对应 XLSX 的空列） |
| 4 | `Name` | 4 | 效果名称 |
| 5 | `EffectCategory` | 5 | 效果分类 |
| 6 | `EffectType` | 6 | 效果类型 |
| 7 | `Description` | 7 | 效果描述 |
| 8 | `BuffIds` | 8 | 附加的 Buff IDs |
| 9 | `SelfBuffIds` | 9 | 自身 Buff IDs |
| 10 | `Cooldown` | 10 | 冷却时间 |
| 11 | `IconId` | 11 | 图标 ID |
| 12 | `Rarity` | 12 | 稀有度 |
| 13 | `Weight` | 13 | 权重 |
| 14 | `EffectParams` | 14 | 效果参数 |

#### 第 3-4 行（类型和说明行）
格式同第 2 行，只是内容不同。

#### 第 5+ 行（数据行）
```
[TAB] 101 [TAB] [TAB] 玩家先手·速度爆发 [TAB] 1 [TAB] 1 [TAB] 移动速度提升... [TAB] [TAB] 2001 [TAB] -1 [TAB] 10001 [TAB] 1 [TAB] 100 [TAB]
```

**数据行规则**：
1. 开头: `\t`（一个制表符，作为行缩进）
2. 之后每个字段用 `\t` 分隔
3. 空字段保留：如果某列没有值，写 `\t\t` 表示空列
4. 如果第 3 列（Name 前的对齐列）为空，写 `\t\t` 

---

## XLSX 文件格式规范

### 1. 文件编码
- **格式**: `.xlsx`（Excel 2007+ 格式）
- **工作表**: 单个工作表（通常命名为 "Sheet" 或表名）

### 2. 行结构

XLSX 中的前 4 行对应 TXT 的元数据，第 5+ 行是数据：

```
第 1 行: # | SpecialEffectTable | (空) | (空) | ... | (空)
第 2 行: # | ID | (空) | Name | EffectCategory | EffectType | Description | BuffIds | SelfBuffIds | Cooldown | IconId | Rarity | Weight | EffectParams
第 3 行: # | int | (空) | string | int | int | string | int[] | int[] | double | int | int | int | string
第 4 行: # | 唯一标识... | SpecialEffectTable中的效果... | 效果名称 | ...
第 5+ 行: (空) | 101 | (空) | 玩家先手·速度爆发 | 1 | 1 | 移动速度提升... | (空) | 2001 | -1 | 10001 | 1 | 100
```

### 3. 列映射规则

**TXT 的列数** = **XLSX 的列数**（直接 1:1 映射）

关键差异：
- TXT 第 1 列：`#`（注释标记）→ XLSX 第 1 列：`#`（保留）
- TXT 第 2 列：`ID` → XLSX 第 2 列：`ID`
- TXT 第 3 列：`(空)` → XLSX 第 3 列：`(空)`
- TXT 第 4 列：`Name` → XLSX 第 4 列：`Name`
- ...以此类推

### 4. 特殊处理

- **第 3 列（对齐列）**: 必须保留为空（用于 TXT 和 XLSX 的对齐）
- **第 1 列**: 第 1 列为 `#` 标记，数据行第 1 列为空
- **超长字段**: 某些字段（如 `Description`）可能包含中文、特殊字符，XLSX 会自动处理宽度

---

## 数据一致性检查

### 1. 检查方法

使用以下方式验证 TXT 和 XLSX 是否同步：

```python
import pandas as pd
import openpyxl

# 读取 TXT（需自己解析 TAB 分隔）
with open('SpecialEffectTable.txt', 'r', encoding='utf-8-sig') as f:
    txt_lines = [line.rstrip('\n\r').split('\t') for line in f]

# 读取 XLSX
wb = openpyxl.load_workbook('SpecialEffectTable.xlsx')
ws = wb.active
xlsx_rows = []
for row in ws.iter_rows(values_only=True):
    xlsx_rows.append(row)

# 逐行对比
for i, (txt_row, xlsx_row) in enumerate(zip(txt_lines, xlsx_rows)):
    if len(txt_row) != len(xlsx_row):
        print(f"第 {i+1} 行：列数不匹配 TXT={len(txt_row)}, XLSX={len(xlsx_row)}")
    else:
        for j, (txt_val, xlsx_val) in enumerate(zip(txt_row, xlsx_row)):
            # 处理空值
            txt_val = txt_val.strip() if txt_val else None
            xlsx_val = str(xlsx_val).strip() if xlsx_val else None
            if txt_val != xlsx_val:
                print(f"第 {i+1} 行，第 {j+1} 列不匹配：TXT='{txt_val}', XLSX='{xlsx_val}'")
```

### 2. 常见问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 列数不匹配 | TXT 缺少制表符 | 确保每列间都有 `\t`，空列也要有 `\t` |
| 数据错位 | 某行缺少对齐制表符 | 检查第 3 列（对齐列）是否为空 |
| Excel 显示不正确 | 字符编码问题 | 使用 UTF-8 with BOM 编码，不要用 ANSI |
| 尾部制表符缺失 | 数据行末尾漏写 | 某些工具会自动删除尾部制表符，需手动补充 |

---

## 转换工作流

### 从 TXT 生成 XLSX

1. **验证 TXT 格式**
   - 检查每行的制表符数量是否一致
   - 验证编码是 UTF-8（BOM）
   - 检查行尾是 `\r\n`

2. **使用工具转换**
   ```python
   import openpyxl
   
   # 读取 TXT
   rows = []
   with open('SpecialEffectTable.txt', 'r', encoding='utf-8-sig') as f:
       for line in f:
           rows.append(line.rstrip('\n\r').split('\t'))
   
   # 写入 XLSX
   wb = openpyxl.Workbook()
   ws = wb.active
   for row_idx, row in enumerate(rows, 1):
       for col_idx, val in enumerate(row, 1):
           ws.cell(row=row_idx, column=col_idx, value=val if val else None)
   
   wb.save('SpecialEffectTable.xlsx')
   ```

3. **验证转换结果**
   - 打开 XLSX，检查行数和列数
   - 采样检查几行数据
   - 使用上述 Python 脚本对比两个文件

### 从 XLSX 生成 TXT

1. **读取 XLSX 所有数据**
   ```python
   import openpyxl
   
   wb = openpyxl.load_workbook('SpecialEffectTable.xlsx')
   ws = wb.active
   ```

2. **转换为 TAB 分隔格式**
   ```python
   rows = []
   for row in ws.iter_rows(values_only=True):
       row_str = '\t'.join(str(val) if val else '' for val in row)
       rows.append(row_str)
   ```

3. **写入 TXT 文件**
   ```python
   with open('SpecialEffectTable.txt', 'w', encoding='utf-8-sig', newline='') as f:
       for row in rows:
           f.write(row + '\r\n')
   ```

4. **验证转换结果**
   - 用文本编辑器打开，检查制表符显示（显示为 `^I`）
   - 检查各字段是否对齐
   - 验证数据行前的缩进制表符是否存在

---

## 重要提示

### ✅ 推荐做法

1. **始终使用工具维护**：手工编辑容易出错，推荐用 Python 脚本自动化
2. **定期验证一致性**：在提交代码前，检查 TXT 和 XLSX 是否同步
3. **备份元数据行**：在修改前保存前 4 行（元数据），确保不被破坏
4. **使用版本控制**：TXT 和 XLSX 都应纳入 Git 追踪

### ❌ 避免的做法

1. **手工编辑 XLSX 后期望 TXT 自动同步**：不会同步
2. **用空格代替制表符**：会导致列对齐错乱
3. **删除第 3 列（对齐列）**：会破坏列结构
4. **修改前 4 行的结构**：会导致 DataTableGenerator 无法正确解析
5. **混合使用 ANSI 和 UTF-8 编码**：会导致字符显示错乱

---

## 示例对比

### 正确格式（SpecialEffectTable 的行 2）

**TXT 格式**（用 `|` 表示制表符位置）：
```
#|ID||Name|EffectCategory|EffectType|Description|BuffIds|SelfBuffIds|Cooldown|IconId|Rarity|Weight|EffectParams
```

**XLSX 显示**（第 2 行）：
```
列1: #
列2: ID
列3: (空)
列4: Name
列5: EffectCategory
列6: EffectType
列7: Description
列8: BuffIds
列9: SelfBuffIds
列10: Cooldown
列11: IconId
列12: Rarity
列13: Weight
列14: EffectParams
```

### 数据行示例（ID=101）

**TXT 格式**：
```
|101||玩家先手·速度爆发|1|1|移动速度提升20%，持续8秒||2001|-1|10001|1|100|
```

**XLSX 显示**：
```
列1: (空)
列2: 101
列3: (空)
列4: 玩家先手·速度爆发
列5: 1
列6: 1
列7: 移动速度提升20%，持续8秒
列8: (空)
列9: 2001
列10: -1
列11: 10001
列12: 1
列13: 100
列14: (空)
```

---

## 总结

| 方面 | TXT 格式 | XLSX 格式 |
|------|---------|----------|
| **编码** | UTF-8（BOM） | 自动（通常 UTF-8） |
| **分隔符** | TAB（`\t`） | 单元格列 |
| **对齐列** | 第 3 列（保留空） | 第 3 列（保留空） |
| **元数据行** | 前 4 行（带 `#`） | 前 4 行（带 `#`） |
| **数据行缩进** | TAB | 空（第 1 列为空） |
| **列数** | 13-14 列 | 13-14 列 |
| **行数** | 39 行（含元数据） | 39 行（含元数据） |
