# 第五周周报：存档与数据持久化系统

## 一、思维导图状态
- **本周已完成的内容（绿色）**：
  - 基础系统（存档/读档系统）
  - UI系统（NewGameUI、LoadGameUI）

## 二、本周完成的具体工作

本周工作围绕"存档系统"与"角色创建流程"两大功能展开，完成了数据结构设计、存档管理器实现、UI界面开发以及完整的数据流联调，并在测试阶段发现并修复了多处3D模型显示问题：

### 1. 数据结构设计
存档系统是玩家数据持久化的核心模块。本周设计了 `PlayerSaveData` 数据结构，包含以下核心字段：
- **基础信息**：存档ID、存档名称、创建时间、最后保存时间
- **角色属性**：等级、经验值、金币、钻石等货币系统
- **召唤师数据**：当前选择的召唤师ID、召唤师等级
- **棋子数据**：已解锁的棋子ID列表（`UnlockedChessIds`）、拥有的单位卡牌ID列表（`OwnedUnitCardIds`）
- **场景进度**：当前场景ID、已解锁的场景列表
- **装备背包**：装备列表、物品列表

数据结构采用 JSON 序列化存储，每个存档对应一个独立的 `.json` 文件，存放在 `Application.persistentDataPath/Saves/{AccountId}/` 目录下。

### 2. 存档管理器实现
实现了 `PlayerAccountDataManager` 单例管理器，负责存档的增删改查操作：
- **多账号支持**：通过 `SetCurrentAccountId()` 方法切换账号，每个账号拥有独立的存档目录
- **多存档管理**：支持同一账号下创建多个存档，每个存档拥有唯一的 GUID 标识
- **自动保存**：提供 `SaveCurrentSave()` 方法，在关键节点（场景切换、数据变更）自动保存
- **最近存档加载**：实现 `AutoLoadLastSave()` 方法，根据账号信息中的 `LastSaveId` 自动加载最近使用的存档
- **存档简要信息**：提供 `GetAllSaveBriefInfos()` 方法，快速获取存档列表的摘要信息（存档名、创建时间、召唤师ID），避免加载完整存档数据

存档管理器还订阅了棋子解锁事件，确保 `OwnedUnitCardIds` 和 `UnlockedChessIds` 两个字段保持同步，避免数据不一致问题。

### 3. UI界面开发

#### NewGameUI（新游戏界面）
新游戏界面是玩家创建角色的入口，本周完成了完整的创建流程：
- **召唤师选择**：从 `SummonerTable` 配置表加载可选召唤师列表，玩家可通过左右箭头切换
- **3D模型展示**：集成 `UIModelViewer` 组件，实时展示选中召唤师的3D模型。模型支持拖拽旋转、双击播放动画等交互功能
- **角色命名**：提供输入框让玩家输入存档名称（默认使用召唤师名称）
- **数据创建**：点击"开始游戏"后，调用 `PlayerAccountDataManager.CreateNewSave()` 创建新存档，并自动跳转到游戏主场景

#### LoadGameUI（加载存档界面）
加载存档界面用于展示和选择已有存档：
- **存档列表**：使用 `SpawnItem` 对象池技术优化滚动列表性能，每个存档项显示存档名、创建时间、召唤师信息
- **存档选择**：点击存档项高亮选中，双击直接加载进入游戏
- **删除功能**：提供删除按钮，支持删除不需要的存档（需二次确认）
- **空状态处理**：当没有存档时，显示提示信息引导玩家创建新存档

#### UIModelViewer（3D模型查看器）
为了在UI界面中展示3D角色模型，开发了独立的 `UIModelViewer` 组件：
- **独立渲染环境**：创建专用的 `Camera` 和 `Light`，使用独立的 Layer（`UI3DModel`），避免与主场景渲染冲突
- **RenderTexture映射**：将3D模型渲染到 `RenderTexture`，再映射到UI的 `RawImage` 组件，实现UI与3D的无缝集成
- **交互功能**：支持鼠标拖拽旋转模型、双击播放动画、滚轮缩放等交互
- **异步加载**：使用 `SetModelAsync()` 方法异步加载模型资源，避免阻塞主线程

### 4. 数据流联调
本周打通了从创建存档到加载存档进入游戏的完整数据流：
- **创建流程**：StartMenuUI → NewGameUI → 创建存档 → 保存到本地 → 跳转到游戏场景
- **加载流程**：StartMenuUI → LoadGameUI → 选择存档 → 加载存档数据 → 设置为当前存档 → 跳转到游戏场景
- **自动加载**：StartMenuUI 启动时自动检测是否有存档，有则显示"继续游戏"按钮，点击后自动加载最近的存档

联调过程中发现2个典型问题：
- **问题1**：存档文件路径在不同平台下不一致，导致存档丢失。解决方案是统一使用 `Application.persistentDataPath` 作为根目录，确保跨平台兼容性。
- **问题2**：存档数据中的时间戳格式不统一，前端显示为"2026-03-05T14:30:00Z"。解决方案是使用 `DateTime.ToString("yyyy-MM-dd HH:mm:ss")` 统一格式化为"2026年3月5日 14:30"。

## 三、遇到的问题及解决方案

### 问题1：UI上显示3D模型时背景穿帮
**现象**：在 `NewGameUI` 中直接将3D模型放置在UI Canvas下，导致模型背后的场景内容穿帮，且光照效果不佳（模型过暗或过亮）。

**原因分析**：
- UI Canvas 使用 `Screen Space - Overlay` 模式，直接渲染在屏幕上，无法正确处理3D模型的深度和光照
- 主场景的光照环境不适合UI模型展示，导致模型显示效果不理想

**解决方案**：
1. 创建独立的 `UIModelViewer` 组件，负责管理3D模型的渲染环境
2. 在远离主场景的位置（如 `Vector3(10000, 0, 0)`）创建模型根节点，避免与主场景冲突
3. 为模型创建专用的 `Camera` 和 `Light`，使用独立的 Layer（`UI3DModel`）进行渲染隔离
4. 使用 `RenderTexture` 将模型渲染结果映射到UI的 `RawImage` 组件，实现UI与3D的无缝集成
5. 调整光照参数（方向光角度、强度、颜色），确保模型显示效果符合预期

**效果**：模型显示清晰，背景纯净，光照效果良好，支持拖拽旋转等交互功能。

### 问题2：存档数据同步问题
**现象**：玩家解锁新棋子后，`UnlockedChessIds` 字段更新了，但 `OwnedUnitCardIds` 字段没有同步更新，导致背包中看不到新解锁的棋子。

**原因分析**：
- 棋子解锁事件只更新了 `UnlockedChessIds` 字段，没有同步更新 `OwnedUnitCardIds`
- 两个字段的语义不同：`UnlockedChessIds` 表示已解锁的棋子，`OwnedUnitCardIds` 表示背包中拥有的单位卡牌

**解决方案**：
在 `PlayerAccountDataManager` 的构造函数中订阅棋子解锁事件，当棋子解锁时自动将棋子ID添加到 `OwnedUnitCardIds` 列表中，确保两个字段保持同步。

```csharp
private PlayerAccountDataManager()
{
    // 订阅棋子解锁事件，确保 OwnedUnitCardIds 和 UnlockedChessIds 同步
    GameEntry.Event.Subscribe(ChessUnlockedEventArgs.EventId, OnChessUnlocked);
}

private void OnChessUnlocked(object sender, GameEventArgs e)
{
    var args = e as ChessUnlockedEventArgs;
    if (args != null && m_CurrentSaveData != null)
    {
        int chessId = args.ChessId;
        if (!m_CurrentSaveData.OwnedUnitCardIds.Contains(chessId))
        {
            m_CurrentSaveData.OwnedUnitCardIds.Add(chessId);
            DebugEx.LogModule("PlayerAccountDataManager", $"同步已解锁棋子 {chessId} 到 OwnedUnitCardIds");
        }
    }
}
```

## 四、下周工作计划
1. 搭建技能系统架构，设计 `IPlayerSkill` 接口和 `SkillFactory` 工厂类
2. 实现基础技能效果（位移、伤害、治疗）
3. 开发技能配置管理系统（ScriptableObject）
