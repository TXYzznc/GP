# 棋子卡片点击交互与 DetailInfoUI 集成 — 实现总结

**完成时间**: 2026-04-02
**涉及文件**:
- `CombatPreparationUI.cs`
- `ChessItemUI.cs`

---

## 需求分析

1. 棋子卡片点击交互：点击表示选中，再次点击已选中的表示取消选中（参考策略卡实现）
2. 显示棋子详细信息：选中时调用 `DetailInfoUI` 显示棋子数据
3. 隐藏棋子详细信息：取消选中时隐藏 `DetailInfoUI`
4. 拖拽交互：拖拽开始时清除选中状态

---

## 核心设计

### 单选模式实现
- 使用 `m_SelectedChessInstanceId` 记录当前选中的棋子实例ID
- 点击新棋子时自动取消之前选中的棋子
- 再次点击已选中的棋子时取消选中

### 三个关键回调
1. **点击回调** (`OnChessItemSelected`): 实现选中/取消选中切换
2. **拖拽开始回调** (`OnChessItemDragBegin`): 进入放置模式时清除选中状态
3. **拖拽结束回调** (`OnChessItemDragEnd`): 确认放置

### DetailInfoUI 集成
- 选中棋子时：
  1. 从 `ChessDeploymentTracker` 获取棋子实例数据
  2. 调用 `detailUI.SetChessUnitData(entity)` 设置棋子数据
  3. 调用 `detailUI.RefreshUI()` 刷新显示
  4. 调用 `detailUI.ShowWithAnimation()` 播放滑入动画

- 取消选中时：
  1. 调用 `varDetailInfoUI.SetActive(false)` 隐藏面板

---

## 文件修改详情

### CombatPreparationUI.cs

#### 新增字段
```csharp
/// <summary>当前选中的棋子实例ID</summary>
private string m_SelectedChessInstanceId = string.Empty;
```

#### 修改的方法

**OnOpen()**
- 初始化 `varDetailInfoUI` 为隐藏状态

**RefreshChessPanel()**
- 修改 `SetData` 调用，传递 3 个回调：
  - `OnChessItemSelected` (点击回调)
  - `OnChessItemDragEnd` (拖拽结束回调)
  - `OnChessItemDragBegin` (拖拽开始回调)

**OnChessItemSelected(string instanceId)**
- 重写为点击逻辑
- 判断是否需要取消选中或选中新棋子

#### 新增方法

**SelectChess(string instanceId)**
```csharp
private void SelectChess(string instanceId)
{
    // 1. 标记为选中
    m_SelectedChessInstanceId = instanceId;

    // 2. 获取棋子实例数据
    var instance = ChessDeploymentTracker.Instance.GetInstance(instanceId);

    // 3. 设置 DetailInfoUI 数据并显示
    if (instance.Entity != null)
    {
        detailUI.SetChessUnitData(instance.Entity);
    }
    detailUI.RefreshUI();
    detailUI.ShowWithAnimation();
}
```

**DeselectChess()**
```csharp
private void DeselectChess()
{
    // 1. 清除选中状态
    m_SelectedChessInstanceId = string.Empty;

    // 2. 隐藏 DetailInfoUI
    varDetailInfoUI.SetActive(false);
}
```

**OnChessItemDragBegin(string instanceId)**
- 清除选中状态
- 调用 `ChessPlacementManager.StartPlacement(instanceId, true)`

**OnChessRecalledHandler()**
- 增加逻辑：如果撤回的是选中的棋子，清除选中状态

**ClearSpawnedItems()**
- 增加逻辑：清除 `m_SelectedChessInstanceId`

---

### ChessItemUI.cs

#### 修改字段
```csharp
private Action<string> m_OnSelectCallback;        // 点击回调
private Action<string> m_OnDragBeginCallback;     // 拖拽开始回调 (新增)
private Action<string> m_OnDragEndCallback;       // 拖拽结束回调
```

#### 修改 SetData 方法签名
```csharp
public void SetData(
    string instanceId,
    int chessId,
    Action<string> onSelectCallback = null,
    Action<string> onDragEndCallback = null,
    Action<string> onDragBeginCallback = null      // 新增参数
)
{
    m_OnSelectCallback = onSelectCallback;
    m_OnDragBeginCallback = onDragBeginCallback;   // 新增
    m_OnDragEndCallback = onDragEndCallback;
}
```

#### 修改 OnBeginDrag 方法
- 在调用 `ChessPlacementManager.StartPlacement` 前，触发拖拽开始回调：
  ```csharp
  m_OnDragBeginCallback?.Invoke(m_InstanceId);
  ```

---

## 交互流程

```
┌─── 用户点击棋子卡片 ───┐
│                       │
├─→ OnChessItemSelected ─┬─→ 检查是否已选中
│                       │
│  ┌─ 是 ─→ DeselectChess ─→ 隐藏 DetailInfoUI
│  │
│  └─ 否 ─→ SelectChess ─→ 显示 DetailInfoUI + 播放动画
│
├─── 用户拖拽棋子 ───┐
│                  │
├─→ OnBeginDrag ──→ OnChessItemDragBegin
│                  ├─→ DeselectChess (清除选中)
│                  └─→ ChessPlacementManager.StartPlacement
│
├─── 用户释放拖拽 ───┐
│                  │
└─→ OnEndDrag ────→ OnChessItemDragEnd
                   └─→ ChessPlacementManager.ConfirmPlacementFromDrag
```

---

## 关键特性

✅ **单选模式**: 同时只能选中一个棋子
✅ **动画支持**: DetailInfoUI 使用滑入动画
✅ **拖拽集成**: 拖拽时自动清除选中状态
✅ **状态同步**: 棋子被撤回时自动清除选中状态
✅ **生命周期管理**: UI 关闭时正确清理资源

---

## 兼容性说明

- 与棋子部署系统兼容（通过 `ChessDeploymentTracker`）
- 与放置系统兼容（通过 `ChessPlacementManager`）
- 完全参考 `CardSlotItem` 的设计模式，保证一致性

---

## 已验证

- ✅ 代码逻辑完整性
- ✅ 回调传递链路正确
- ✅ 选中/取消选中状态管理
- ✅ DetailInfoUI 集成点
- ✅ 拖拽与点击交互分离
- ✅ 资源清理
