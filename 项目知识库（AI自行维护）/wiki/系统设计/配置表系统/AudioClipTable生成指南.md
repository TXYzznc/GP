# AudioClipTable 配置表生成指南

> **版本**: 1.0  
> **生成日期**: 2026-04-30  
> **状态**: 就绪，待用户转换和部署  

---

## 📋 概览

已生成 **AudioClipTable.txt** 配置表文件，包含：
- ✅ **23 个 BGM** (ID: 10001-10305)
- ✅ **82 个 SFX** (ID: 20000-20409)
- ✅ **105 条音效配置** (完整)

**文件位置**: `项目知识库/outputs/AudioClipTable.txt`

---

## 📊 配置表结构

### 字段定义 (14 列)

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| **Id** | int | 唯一标识符（主键） | 10001 |
| **AudioName** | string | 音效名称 | BGM_StartGame |
| **AudioType** | int | 音效类型 (1=BGM, 2=SFX, 3=环境音, 4=语音) | 1 |
| **ResourcePath** | string | 资源路径 | BGM/StartGame |
| **Duration** | float | 音效时长（秒） | 120.5 |
| **Volume** | float | 默认音量 (0-1) | 0.8 |
| **Pitch** | float | 音调 (0.5-1.5) | 1.0 |
| **IsLoop** | bool | 是否循环 | 1 |
| **Is3D** | bool | 是否 3D 定位 | 0 |
| **MaxDistance** | float | 3D 最大距离 | 0 |
| **Priority** | int | 优先级 (0-256) | 128 |
| **FadeInTime** | float | 淡入时长 (BGM 用) | 0.5 |
| **FadeOutTime** | float | 淡出时长 (BGM 用) | 0.5 |
| **Tag** | string | 分类标签 | menu |

---

## 🔄 使用流程 (3 步)

### 步骤 1️⃣: TXT 文件已生成 ✅

文件: `项目知识库/outputs/AudioClipTable.txt`

**验证文件内容**:
```bash
# 查看前几行
head -5 AudioClipTable.txt

# 输出应该是:
# #	AudioClipTable
# #	Id	AudioName	AudioType	ResourcePath	Duration	Volume	Pitch	IsLoop	Is3D	MaxDistance	Priority	FadeInTime	FadeOutTime	Tag
# #	int	string	int	string	float	float	float	bool	bool	float	int	float	float	string
# #	音效ID	音效名称	音效类型	资源路径	时长	音量	音调	循环	3D定位	3D最大距离	优先级	淡入时长	淡出时长	分类标签
	10001	BGM_StartGame	1	BGM/StartGame	120.5	0.8	1.0	1	0	0	128	0.5	0.5	menu
```

### 步骤 2️⃣: 转换 TXT 为 XLSX (用户手动)

**选项 A: 使用 Python 脚本**

创建 `convert_txt_to_xlsx.py`:

```python
import openpyxl
from openpyxl.styles import Font, Alignment

def convert_txt_to_xlsx(txt_path, xlsx_path):
    """转换 TXT 为 XLSX"""
    
    # 1. 读取 TXT 文件
    rows = []
    with open(txt_path, 'r', encoding='utf-8-sig') as f:
        for line in f:
            rows.append(line.rstrip('\n\r').split('\t'))
    
    # 2. 创建 XLSX
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "AudioClipTable"
    
    # 3. 写入数据
    for row_idx, row in enumerate(rows, 1):
        for col_idx, val in enumerate(row, 1):
            cell = ws.cell(row=row_idx, column=col_idx, value=val if val else None)
            
            # 4. 设置格式
            if row_idx <= 3:  # 前三行为元数据行
                cell.font = Font(bold=True)
            
            # 5. 数组列设置为文本格式
            if row_idx > 3 and isinstance(val, str) and ',' in val:
                cell.number_format = '@'
    
    # 6. 调整列宽
    for col_idx in range(1, len(rows[0]) + 1):
        ws.column_dimensions[openpyxl.utils.get_column_letter(col_idx)].width = 18
    
    # 7. 保存
    wb.save(xlsx_path)
    print(f"✅ 转换完成: {xlsx_path}")

# 运行转换
convert_txt_to_xlsx('AudioClipTable.txt', 'AudioClipTable.xlsx')
```

运行命令:
```bash
python convert_txt_to_xlsx.py
```

**选项 B: 使用 Excel 手动处理**

1. 在 Excel 中打开 `AudioClipTable.txt` (选择制表符分隔)
2. 验证数据无误
3. 另存为 `.xlsx` 格式

**选项 C: 使用项目的 TXT 转 XLSX 工具**

如果项目中已有配置表转换工具，按照该工具的说明执行。

---

### 步骤 3️⃣: 复制 XLSX 到项目 (用户手动)

转换完成后，将 `AudioClipTable.xlsx` 复制到:

```
Assets/AAAGame/DataTable/AudioClipTable.xlsx
```

**验证文件位置**:
```bash
ls -la Assets/AAAGame/DataTable/AudioClipTable.xlsx
```

---

### 步骤 4️⃣: 执行 DataTableGenerator (Unity)

在 Unity 编辑器中:

1. 菜单: `GameFramework → DataTable → Generate`
2. 等待生成完成
3. 检查输出:
   - ✅ `Assets/AAAGame/Scripts/DataTable/AudioClipTable.cs`
   - ✅ `Assets/AAAGame/DataTable/AudioClipTable.bytes`

**验证生成成功**:
```csharp
// 在 Unity Console 查看日志
// 应该看到 "AudioClipTable" 的生成日志
```

---

## 📝 配置表内容清单

### BGM (ID: 10001-10305)

#### 菜单/全局 (10001-10030)
| ID | 名称 | 用途 | 时长 | 循环 |
|----|------|------|------|------|
| 10001 | BGM_StartGame | 启动/主菜单 | 120.5s | ✓ |
| 10010 | BGM_BaseRoom | 基地场景 | 90s | ✓ |
| 10020 | BGM_WorldScene | 大世界探索 | 150s | ✓ |
| 10030 | BGM_TutorialScene | 教程场景 | 60s | ✓ |

#### 副本 (10101-10107)
| ID | 名称 | 风格 | 时长 |
|----|------|------|------|
| 10101 | BGM_Tiangong | 中国古风 | 130s |
| 10102 | BGM_Herheim | 北欧神话 | 140s |
| 10103 | BGM_Takamahara | 日本神话 | 140s |
| 10104 | BGM_Olympus | 希腊神话 | 135s |
| 10105 | BGM_Babylon | 美索不达米亚 | 145s |
| 10106 | BGM_Avalon | 凯尔特神话 | 140s |
| 10107 | BGM_Abyss | 通用黑暗 | 160s |

#### 战斗 (10201-10203)
| ID | 名称 | 难度 | 时长 |
|----|------|------|------|
| 10201 | BGM_CombatNormal | 普通敌人 | 120s |
| 10202 | BGM_CombatElite | Elite 敌人 | 130s |
| 10203 | BGM_CombatBoss | Boss 敌人 | 150s |

#### 特殊场景 (10301-10305)
| ID | 名称 | 用途 | 时长 |
|----|------|------|------|
| 10301 | BGM_Settlement | 结算界面 | 120s |
| 10302 | BGM_BattlePreset | 出战预设 | 90s |
| 10303 | BGM_Inventory | 背包界面 | 60s |
| 10304 | BGM_Shop | 商城界面 | 100s |
| 10305 | BGM_Upgrade | 升级界面 | 90s |

### SFX (ID: 20000-20409)

#### UI 音效 (20000-20011)
- 按钮点击、UI 打开/关闭、确认、取消、错误、成功等

#### 战斗音效 (20100-20202)
- 棋子选中、移动、攻击、技能、Buff、伤害、战斗开始/结束等

#### 探索音效 (20200-20353)
- 脚步声、物品捡取、宝箱、门、NPC、任务、传送等

#### 环境音 (20300-20305)
- 风声、雷声、水流、火焰、阴冷、诡异低语等

#### 奖励音效 (20400-20409)
- 成就解锁、等级提升、物品获得、消费等

---

## ✅ 验证清单

### TXT 文件验证
- [ ] 文件编码: UTF-8 with BOM
- [ ] 行尾: Windows 格式 (`\r\n`)
- [ ] 分隔符: 制表符 (`\t`)
- [ ] 第1列: 行标记 (`#` 或空)
- [ ] 第2列: ID 字段
- [ ] 第3列: 空（对齐用）
- [ ] 总行数: 109 (4 元数据 + 105 数据)

### XLSX 文件验证
- [ ] 文件格式: `.xlsx`
- [ ] 行数: 109 行
- [ ] 列数: 14 列
- [ ] 第4-109 行: 数据行
- [ ] 所有 ID 唯一且按升序排列

### DataTable 生成验证
- [ ] 文件生成: `AudioClipTable.cs` 和 `AudioClipTable.bytes`
- [ ] 代码编译: 无错误
- [ ] 配置表加载: 成功 (检查控制台日志)

---

## 📊 字段数据参考

### AudioType（音效类型）
```csharp
1 = BGM
2 = SFX
3 = Ambient (环境音)
4 = Voice (语音，预留)
```

### Volume（音量参考）
```
BGM: 0.6-0.9  // 不要太大声，给 SFX 反馈空间
SFX: 0.8-1.0  // 清晰反馈
Ambient: 0.3-0.6  // 底层背景
```

### Priority（优先级）
```
BGM: 128 (中等)
战斗 SFX: 64 (高)
UI SFX: 64 (高)
环境音: 192 (低)
```

### IsLoop（是否循环）
```
1 = true (循环)
0 = false (不循环)
```

### Is3D（是否 3D 定位）
```
1 = true (3D)
0 = false (2D)
```

只有特定音效 (如脚步声) 使用 3D 定位。

---

## 🚀 后续步骤

### 完成配置表部署后
1. ✅ 运行游戏验证 AudioManager 初始化成功
2. ✅ 测试基本音效播放
3. ✅ 验证音量控制正常工作
4. ✅ 为各系统接入音效

### 常见问题排查

**Q: 生成的 DataTable 为空？**
- A: 检查 XLSX 文件是否正确复制到 `Assets/AAAGame/DataTable/`
- 重新执行 DataTableGenerator

**Q: 某些音效无法播放？**
- A: 检查 ResourcePath 是否正确
- 验证音效文件是否在 `Assets/AAAGame/Resources/Audio/` 对应目录下

**Q: ID 重复报错？**
- A: 确认 ID 在配置表中唯一
- 重新生成并检查

---

## 📚 相关文件

- **音效 ID 配置指南**: 详细的 ID 定义和使用说明
- **音效系统集成教程**: 各系统的集成示例
- **技术实现方案**: 完整的系统设计文档

---

## 💾 文件交付

| 文件 | 位置 | 状态 | 说明 |
|------|------|------|------|
| AudioClipTable.txt | outputs/ | ✅ 已生成 | TXT 源文件 |
| AudioClipTable.xlsx | 待创建 | ⏳ 用户转换 | Excel 格式 |
| 项目位置 | Assets/AAAGame/DataTable/ | ⏳ 待复制 | 最终位置 |

---

## ⏱️ 时间预估

| 步骤 | 耗时 | 说明 |
|------|------|------|
| 转换 TXT→XLSX | 2-5 分钟 | Python 脚本或 Excel 手动 |
| 复制文件 | 1 分钟 | 复制到 DataTable 目录 |
| 执行 Generator | 1-2 分钟 | Unity 编辑器执行 |
| 验证 | 1-2 分钟 | 检查生成文件和日志 |
| **总计** | **5-10 分钟** | 完整配置表部署 |

---

**配置表已就绪！** 🎵

按照上述步骤完成转换和部署，即可开始使用 AudioManager 系统。

