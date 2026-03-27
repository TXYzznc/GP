# Requirements Document

## Introduction

本需求文档描述将 PlayerAccountDataManager 的数据存储方式从基于 PlayerPrefs 的系统存储改为直接文件存储的功能需求。当前系统使用 `GF.Setting`（基于 Unity PlayerPrefs）将玩家账号数据存储在平台特定位置（Windows 注册表、Android SharedPreferences 等），这种方式不便于数据管理、备份和迁移。新的文件存储方式将使用 JSON 文件直接保存在应用程序的持久化数据目录中，提供更好的可访问性和可维护性。

## Glossary

- **PlayerAccountDataManager**: 玩家账号数据管理器，负责管理玩家账号的创建、保存、加载和删除
- **PlayerAccountData**: 玩家账号数据类，包含玩家的所有游戏数据（等级、资源、卡牌、背包等）
- **GF.Setting**: GameFramework 的设置组件，基于 Unity PlayerPrefs 实现
- **PlayerPrefs**: Unity 提供的键值对存储系统，在不同平台存储位置不同
- **PersistentDataPath**: Unity 提供的持久化数据路径，跨平台统一的文件存储位置
- **SaveSlot**: 存档槽位，系统支持最多 3 个独立的玩家存档
- **JSON Serialization**: JSON 序列化，将对象转换为 JSON 格式字符串的过程
- **File I/O**: 文件输入输出操作，包括读取和写入文件

## Requirements

### Requirement 1

**User Story:** 作为开发者，我希望玩家账号数据以 JSON 文件形式存储在持久化数据目录中，以便于数据管理、备份和调试。

#### Acceptance Criteria

1. WHEN the system saves player account data THEN the system SHALL write the data to a JSON file in the persistent data path
2. WHEN the system loads player account data THEN the system SHALL read the data from the JSON file in the persistent data path
3. WHEN a save file is created THEN the system SHALL use a consistent naming convention with the save slot index
4. WHEN the persistent data directory does not exist THEN the system SHALL create the directory before saving files
5. WHERE file operations are performed THEN the system SHALL use UTF-8 encoding for all JSON files

### Requirement 2

**User Story:** 作为开发者，我希望系统能够优雅地处理文件操作错误，以确保数据安全和系统稳定性。

#### Acceptance Criteria

1. WHEN a file write operation fails THEN the system SHALL log the error and preserve the existing save file
2. WHEN a file read operation fails due to missing file THEN the system SHALL return null without throwing exceptions
3. WHEN a file read operation fails due to corrupted data THEN the system SHALL log the error and return null
4. IF a JSON deserialization error occurs THEN the system SHALL catch the exception and log detailed error information
5. WHEN file I/O exceptions occur THEN the system SHALL handle them gracefully without crashing the application

### Requirement 3

**User Story:** 作为开发者，我希望保持与现有代码的兼容性，以便平滑迁移到新的存储方式。

#### Acceptance Criteria

1. WHEN the storage implementation changes THEN the public API of PlayerAccountDataManager SHALL remain unchanged
2. WHEN existing code calls save or load methods THEN the system SHALL function correctly with the new file-based storage
3. WHEN the system checks for save data existence THEN the system SHALL check for file existence instead of PlayerPrefs keys
4. WHEN the system deletes save data THEN the system SHALL delete the corresponding JSON file
5. WHEN the system retrieves all save infos THEN the system SHALL scan the persistent data directory for save files

### Requirement 4

**User Story:** 作为开发者，我希望系统提供清晰的日志输出，以便于调试和监控文件操作。

#### Acceptance Criteria

1. WHEN a save file is written THEN the system SHALL log the complete file path and save slot index
2. WHEN a save file is loaded THEN the system SHALL log the file path and player name
3. WHEN a save file is deleted THEN the system SHALL log the deletion operation and file path
4. WHEN file operations fail THEN the system SHALL log detailed error messages including file paths and exception details
5. WHEN the persistent data path is accessed THEN the system SHALL log the full directory path for debugging purposes

### Requirement 5

**User Story:** 作为玩家，我希望我的存档数据能够被安全地保存和恢复，即使在异常情况下也不会丢失数据。

#### Acceptance Criteria

1. WHEN the system saves account data THEN the system SHALL ensure the JSON file is completely written before considering the operation successful
2. WHEN multiple save operations occur rapidly THEN the system SHALL handle them sequentially without data corruption
3. WHEN the application terminates unexpectedly THEN the system SHALL ensure previously saved data remains intact
4. WHEN a save operation is interrupted THEN the system SHALL not leave partial or corrupted files
5. WHEN loading a save file THEN the system SHALL validate the JSON structure before deserializing

### Requirement 6

**User Story:** 作为开发者，我希望能够轻松定位和访问存档文件，以便于测试、备份和数据迁移。

#### Acceptance Criteria

1. WHEN save files are created THEN the system SHALL store them in a dedicated subdirectory within the persistent data path
2. WHEN querying the save file location THEN the system SHALL provide the complete file path for each save slot
3. WHEN listing all saves THEN the system SHALL enumerate all JSON files in the save directory
4. WHEN accessing save files externally THEN the files SHALL be in human-readable JSON format
5. WHEN the save directory structure is needed THEN the system SHALL use a clear and organized folder hierarchy
