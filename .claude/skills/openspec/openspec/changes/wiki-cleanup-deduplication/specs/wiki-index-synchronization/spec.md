## ADDED Requirements

### Requirement: INDEX.md 链接验证
系统应扫描 INDEX.md 中的所有链接，检查是否指向现存文件。

#### Scenario: 检测悬挂链接
- **WHEN** 扫描 INDEX.md 中的所有 wiki 链接时
- **THEN** 识别出指向已删除文件的链接，生成悬挂链接清单

#### Scenario: 链接格式验证
- **WHEN** 验证链接时
- **THEN** 检查链接格式是否正确，路径是否相对于 wiki/ 目录

### Requirement: 索引条目删除
系统应删除 INDEX.md 中指向已删除文件的所有索引条目。

#### Scenario: 删除过期条目
- **WHEN** 用户批准删除某个文件时
- **THEN** 系统自动从 INDEX.md 中删除该文件的索引条目

#### Scenario: 保持目录结构
- **WHEN** 删除索引条目时
- **THEN** 保持目录分类的结构完整，只删除具体文件条目，不删除分类标题

### Requirement: 文件计数更新
INDEX.md 中每个分类的文件数应精确反映实际文件数。

#### Scenario: 更新分类计数
- **WHEN** 删除文件后
- **THEN** 更新 INDEX.md 中对应分类的文件数统计（如"战斗系统 (17 篇)" → "(16 篇)"）

#### Scenario: 更新总计统计
- **WHEN** 所有删除和索引更新完成时
- **THEN** 更新知识库总计文件数统计

### Requirement: 最终验证报告
系统应生成最终验证报告，确保所有删除和索引更新都是正确的。

#### Scenario: 生成验证报告
- **WHEN** 所有清理操作完成时
- **THEN** 生成 cleanup_verification_report.md，包含：
  - 删除文件统计（个数、分类、备份位置）
  - INDEX.md 更新统计（删除条目数、更新的分类数）
  - 悬挂链接检查结果（应为0个）
  - 完整性检查结果

#### Scenario: 确保无悬挂链接
- **WHEN** 生成验证报告时
- **THEN** 扫描 INDEX.md 的所有链接，确保指向的文件都存在，如有悬挂链接则标记为错误
