# 玩家生成位置从配置表管理

## 修改内容

### 问题
原来的机制：玩家进入游戏时从存档读取 `PlayerPos`，退出游戏时保存当前位置到存档。这导致玩家位置与场景配置解耦。

### 新机制
✅ **玩家生成位置统一从配置表管理**
- 删除从存档读取位置的逻辑
- 删除保存位置到存档的逻辑
- 新增：从 `SceneTable.DefaultSpawnPosId` → `PosTable` 读取出生点

## 实现细节

### 1. SceneTable 配置

`SceneTable` 已有 `DefaultSpawnPosId` 字段：

```csharp
/// <summary>
/// 默认出生点ID（对应 PosTable）
/// </summary>
public int DefaultSpawnPosId
{
    get;
    private set;
}
```

### 2. PosTable 配置

`PosTable` 存储坐标信息：

```csharp
/// <summary>
/// 坐标
/// </summary>
public Vector3 Position
{
    get;
    private set;
}

/// <summary>
/// 描述
/// </summary>
public string Description
{
    get;
    private set;
}
```

### 3. 修改的方法

#### PlayerCharacterManager.SpawnPlayerCharacterFromSave()

**旧逻辑：**
```csharp
Vector3 spawnPosition = saveData.PlayerPos;  // ❌ 从存档读取
```

**新逻辑：**
```csharp
Vector3 spawnPosition = GetDefaultSpawnPositionForCurrentScene();  // ✅ 从配置表读取
```

#### 新增方法：GetDefaultSpawnPositionForCurrentScene()

```csharp
/// <summary>
/// 从当前场景的配置表读取默认出生点坐标
/// </summary>
private Vector3 GetDefaultSpawnPositionForCurrentScene()
{
    // 1. 获取当前场景名
    string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    
    // 2. 从 SceneTable 查找场景配置
    var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
    var sceneRow = sceneTable.GetDataRow(row => row.SceneName == currentSceneName);
    
    // 3. 获取场景的 DefaultSpawnPosId
    int defaultSpawnPosId = sceneRow.DefaultSpawnPosId;
    
    // 4. 从 PosTable 查找坐标
    var posTable = GF.DataTable.GetDataTable<PosTable>();
    var posRow = posTable.GetDataRow(defaultSpawnPosId);
    
    // 5. 返回坐标
    return posRow.Position;
}
```

### 4. 删除的内容

#### PlayerCharacterManager.SaveCurrentPosition()
- 标记为 `[Obsolete]`
- 不再实际保存位置

#### PlayerController.OnDestroy()
- 删除 `SaveCurrentPosition()` 调用
- 不再自动保存位置到存档

## 配置步骤

### 1. 打开配置表
编辑 `AAAGameData/DataTables/SceneTable.xlsx`

### 2. 为每个场景配置出生点

| SceneId | SceneName | DefaultSpawnPosId |
|---------|-----------|------------------|
| 1 | MainBase | 1 |
| 2 | TestScene | 2 |
| 3 | CityMap | 3 |

### 3. 在 PosTable 中定义坐标

编辑 `AAAGameData/DataTables/PosTable.xlsx`

| Id | Position | Description |
|----|-----------| ------------|
| 1 | (0, 1, 0) | 主基地出生点 |
| 2 | (10, 5, 10) | 测试场景出生点 |
| 3 | (50, 2, 50) | 城市地图出生点 |

Position 格式：`(x, y, z)` 用逗号分隔

### 4. 重新生成 DataTable

在 Unity 编辑器中：
```
GameFramework → DataTable → Generate
```

## 完整的玩家生成流程

```
启动游戏
  ↓
GameProcedure.OnEnter()
  ↓
PlayerCharacterManager.SpawnPlayerCharacterFromSave()
  ↓
GetDefaultSpawnPositionForCurrentScene()
  ├─ 获取当前场景名
  ├─ SceneTable 查询场景配置
  ├─ 读取 DefaultSpawnPosId
  ├─ PosTable 查询坐标
  └─ 返回 Position
  ↓
SpawnCharacter(prefabId, spawnPosition, callback)
  ↓
在出生点生成玩家 ✓
```

## 优势

| 特性 | 旧机制 | 新机制 |
|------|------|------|
| **管理方式** | 存档 + 自动保存 | 配置表 |
| **灵活性** | 固定位置 | 可配置多个出生点 |
| **场景切换** | 位置不变 | 自动切换到对应出生点 |
| **测试** | 需要改存档 | 直接改配置表 |
| **多场景** | 容易混淆 | 清晰的映射关系 |
| **传送系统** | 难以实现 | 易于扩展 |

## 验证步骤

1. **启动游戏**
   - 打开不同的场景
   - 检查玩家是否在 `DefaultSpawnPosId` 对应的位置生成 ✓

2. **修改配置**
   - 改变 `DefaultSpawnPosId`
   - 重新生成 DataTable
   - 检查玩家生成位置是否改变 ✓

3. **多场景测试**
   - 在场景 A 生成（出生点 1）
   - 切换到场景 B（出生点 2）
   - 检查位置是否切换 ✓

## 注意事项

### ⚠️ 配置表必须包含所有字段

```excel
SceneId | SceneName | SceneType | DisplayName | ... | DefaultSpawnPosId | RecommendLevel
```

如果 `DefaultSpawnPosId` 为 0 或无效，会输出警告：
```
⚠️ 无法读取默认出生点，使用原点 (0, 0, 0)
```

### ⚠️ PosTable 必须包含对应的 ID

```excel
Id | Position | Description
1  | (0,1,0)  | MainBase Spawn
2  | (10,5,10)| TestScene Spawn
```

如果找不到 ID，会输出错误：
```
❌ 未找到出生点 ID X 的配置
```

### ⚠️ 不要手动修改生成的 DataTable 代码

- PosTable.cs 和 SceneTable.cs 是自动生成的
- 修改在 `XXXXXDATATABLE.xlsx` 文件中
- 重新生成 DataTable

## 扩展功能建议

### 1. 动态传送系统
```csharp
public void TeleportToSpawnPoint(int sceneId, int spawnPosId)
{
    var posTable = GF.DataTable.GetDataTable<PosTable>();
    var posRow = posTable.GetDataRow(spawnPosId);
    PlayerCharacterManager.Instance.TeleportTo(posRow.Position);
}
```

### 2. 多出生点支持
在 PosTable 中添加多个出生点 ID，通过不同的场景配置使用不同的点。

### 3. 检查点系统
添加"最后访问的出生点"到存档，进入场景时自动使用。

```csharp
if (saveData.LastCheckpointId > 0)
    spawnPosition = GetSpawnPosition(saveData.LastCheckpointId);
else
    spawnPosition = GetDefaultSpawnPositionForCurrentScene();
```

## 文件修改总结

| 文件 | 修改 | 说明 |
|------|------|------|
| PlayerCharacterManager.cs | 修改 `SpawnPlayerCharacterFromSave()` | 从配置表读取位置 |
| PlayerCharacterManager.cs | 新增 `GetDefaultSpawnPositionForCurrentScene()` | 查询出生点坐标 |
| PlayerCharacterManager.cs | 标记 `SaveCurrentPosition()` 为 Obsolete | 删除存档位置保存 |
| PlayerController.cs | 清空 `OnDestroy()` | 删除自动保存位置 |

---

**修改完成。** ✅ 玩家生成位置现在完全由配置表管理。
