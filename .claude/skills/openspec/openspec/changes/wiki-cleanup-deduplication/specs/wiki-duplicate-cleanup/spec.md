## ADDED Requirements

### Requirement: 重复文件删除
系统应能安全地删除识别出的重复文件，同时创建备份以便恢复。

#### Scenario: 删除图表多版本
- **WHEN** 用户批准删除重复的图表文件
- **THEN** 系统删除所有重复副本，仅保留一个版本，备份副本存储至 outputs/wiki_cleanup_backup/

#### Scenario: 删除格式多版本
- **WHEN** 用户批准删除格式多版本文件（.svg、.png）
- **THEN** 系统删除 .svg 和 .png 文件，保留 .md 版本，因为 INDEX.md 链接的是 .md 文件

#### Scenario: 删除旧版本文件
- **WHEN** 用户批准删除同功能的旧版本文件
- **THEN** 系统删除日期较早的版本，保留最新版本（如保留 2026-05-12 的敌人AI指南，删除 2026-04-17 版本）

### Requirement: 删除备份管理
被删除的文件应保存备份，以便需要时恢复。

#### Scenario: 创建备份目录
- **WHEN** 开始删除文件时
- **THEN** 系统在 outputs/wiki_cleanup_backup/ 目录下创建子目录，按分类存放备份文件

#### Scenario: 备份完整性记录
- **WHEN** 备份完成时
- **THEN** 系统生成 backup_manifest.md，记录所有备份文件的原始路径、删除理由、备份时间

### Requirement: 删除操作日志
系统应记录所有删除操作，便于审计和恢复。

#### Scenario: 记录删除日志
- **WHEN** 删除文件时
- **THEN** 系统记录删除操作（文件路径、删除时间、理由、备份位置）到 deletion_log.md
