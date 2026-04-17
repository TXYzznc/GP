> **最后更新**: 2026-04-17
> **状态**: 有效
> **分类**: 技术文档

---

# UI 模型预览器（UIModelViewer）

## 目录

- [实现方案](#实现方案)
- [Unity 配置步骤](#unity-配置步骤)
- [组件参数](#组件参数)
- [使用示例](#使用示例)
- [动画控制](#动画控制)
- [NewGameUI 配置](#newgameuiconfigurations)
- [注意事项](#注意事项)

---

## 实现方案

使用 **RenderTexture** 方案在 UI 上显示 3D 模型：
- 创建专用相机渲染模型到 RenderTexture
- 将 RenderTexture 显示在 UI 的 RawImage 上
- 模型放置在远离主场景的位置（默认 (1000, 0, 1000)），避免干扰

### 文件结构

```
Assets/AAAGame/Scripts/
├── UI/
│   ├── NewGameUI.cs
│   └── Components/
│       ├── UIModelViewer.cs      # 模型查看器组件
│       └── ModelController.cs   # UI 模型动画控制器
```

---

## Unity 配置步骤

### 1. 创建 Layer

Edit → Project Settings → Tags and Layers，添加名为 `UI3DModel` 的 Layer。

### 2. 主摄像机配置

主摄像机的 Culling Mask 应**排除** `UI3DModel` Layer，避免重复渲染。

### 3. 配置 RawImage

找到预制体中对应的 `varOccupationImage` 对象，将 `Image` 组件替换为 `RawImage`（脚本会自动替换，或手动操作）。

### 4. 配置资源配置表

在 `ResourceConfigTable` 数据表中添加召唤师模型配置：

| Id | Type | Path |
|----|------|------|
| 9001 | 2 | Summoner/Model_1001 |
| 9002 | 2 | Summoner/Model_1002 |

- Id = 8000 + 召唤师ID
- Type = 2（预制体类型）

### 5. 模型预制体规范

- 存放路径：`Assets/AAAGame/Prefabs/Summoner/`
- 命名规则：`Model_召唤师ID.prefab`（如 `Model_1001.prefab`）
- 面向 Z 轴正方向，原点在脚底，建议高度约 2 单位

---

## 组件参数

### 渲染设置

| 参数 | 说明 | 默认值 |
|------|------|--------|
| renderTextureWidth | 渲染纹理宽度 | 512 |
| renderTextureHeight | 渲染纹理高度 | 512 |
| cameraDistance | 相机距离 | 3 |
| cameraHeight | 相机高度 | 1 |
| modelOffset | 模型偏移 | (0,0,0) |
| modelWorldPosition | 模型世界位置 | (1000,0,1000) |

### 交互设置

| 参数 | 说明 | 默认值 |
|------|------|--------|
| rotationSpeed | 旋转速度 | 0.5 |
| doubleClickThreshold | 双击阈值 | 0.3 秒 |
| enableDragRotation | 启用拖拽旋转 | true |
| enableDoubleClick | 启用双击 | true |

### 光照设置

| 参数 | 说明 | 默认值 |
|------|------|--------|
| lightType | 光源类型 | Directional |
| lightIntensity | 光照强度 | 1.0 |
| lightColor | 光照颜色 | 白色 |
| lightPosition | 光源位置 | (0, 3, -2) |
| lightRotation | 光源旋转（欧拉角） | (0, 0, 0) |
| spotAngle | 聚光灯光圈大小 | 30° |
| lightRange | 点光源/聚光灯范围 | 10 |

---

## 使用示例

### 基础使用

```csharp
// NewGameUI 中加载模型
int modelConfigId = ResourceIds.GetSummonerModelId(summonerId);
await m_ModelViewer.SetModelAsync(modelConfigId);
```

### 光照控制 API

```csharp
// 基础控制
m_ModelViewer.SetLightIntensity(1.5f);
m_ModelViewer.SetLightColor(Color.yellow);
m_ModelViewer.SetLightPosition(new Vector3(2, 3, -1));
m_ModelViewer.SetLightRotation(new Vector3(45, 0, 0));

// 聚光灯专用
m_ModelViewer.SetSpotAngle(45f);
m_ModelViewer.SetLightRange(15f);

// 光源类型切换
m_ModelViewer.SetLightType(LightType.Spot);
m_ModelViewer.SetLightType(LightType.Directional);

// 辅助
m_ModelViewer.LookAtModel();           // 让光源朝向模型中心
Light light = m_ModelViewer.GetLight(); // 获取光源组件
```

### 推荐光照配置

**标准角色展示（平行光）**：
```
Light Type: Directional, Intensity: 1.0
Color: (255, 242, 230), Position: (0, 3, -2)
```

**英雄选择（聚光灯舞台效果）**：
```
Light Type: Spot, Intensity: 1.8, SpotAngle: 40
Position: (0, 5, -1), LightRange: 12
```

**神秘氛围（点光源从下方）**：
```
Light Type: Point, Intensity: 1.5
Color: (0.6, 0.8, 1.0, 冷色), Position: (0, 0.5, 0), Range: 8
```

### 动态光照效果

```csharp
// 呼吸灯效果
private async void PlayBreathingLight()
{
    float time = 0f;
    while (m_ModelViewer.HasModel())
    {
        time += Time.deltaTime;
        float intensity = 0.8f + Mathf.Sin(time * 2f) * 0.3f;
        m_ModelViewer.SetLightIntensity(intensity);
        await UniTask.Yield();
    }
}
```

---

## 动画控制

### 背景问题

角色模型的 `PlayerController` 脚本在 UI 预览时被禁用，导致动画无法正常工作。解决方案：创建轻量级的 `ModelController` 专门用于 UI 模型预览。

### ModelController 组件

**文件**：`Assets/AAAGame/Scripts/UI/Components/ModelController.cs`

**核心方法**：
- `PlayIdleAnimation()` — 播放待机动画
- `PlayInteractAnimation(int interactIndex)` — 播放交互动画
- `StopInteractAnimation()` — 强制停止交互动画
- `HasValidAnimator()` — 检查是否有有效的 Animator

### UIModelViewer 动态管理

UIModelViewer 在 `SetModel()` 时动态添加 ModelController，在 `ClearModel()` 时自动清理：

```csharp
// SetModel 时添加
m_ModelController = m_CurrentModel.AddComponent<ModelController>();

// ClearModel 时清理
m_ModelController?.StopInteractAnimation();
```

### 动画控制器配置要求

- `State` (Int) — 控制主状态转换
- `Speed` (Float) — 控制 Movement 混合树
- `InteractIndex` (Int) — 控制交互动画索引

推荐状态机配置：
```
Entry → Movement (Blend Tree, Speed=0 时播放 Idle)
Movement → 交互状态 (State == 4)
交互状态 → Movement (State == 0, Has Exit Time: true)
```

### 双击交互示例

```csharp
private void OnModelDoubleClick()
{
    var summoner = GetCurrentSummoner();
    if (summoner != null && m_ModelViewer != null)
    {
        m_ModelViewer.PlayInteractAnimation(0);
    }
}
```

---

## NewGameUI 配置

NewGameUI 使用 UIModelViewer 显示召唤师模型，步骤：

1. `varOccupationImage` 替换为 RawImage
2. ResourceConfigTable 中添加召唤师模型配置（ID = 8000 + 召唤师ID）
3. 模型预制体放入 `Assets/AAAGame/Prefabs/Summoner/`，命名 `Model_召唤师ID.prefab`

技能悬浮提示（FloatingBoxTip）在 NewGameUI 中的应用见 FloatingBoxTip 文档。

---

## 注意事项

1. **Layer 必须存在**：确保 `UI3DModel` Layer 已创建，否则模型不会被渲染
2. **主相机排除**：主摄像机 Culling Mask 应排除 `UI3DModel` Layer
3. **光照强度**：建议范围 0.5 ~ 2.0，过高会过曝
4. **聚光灯角度**：建议 20° ~ 60°
5. **透明背景**：RenderTexture 使用透明背景，模型自然融入 UI
6. **ModelController 与 PlayerController 完全隔离**，不影响游戏中的角色控制

### 常见问题

| 问题 | 原因 | 解决 |
|------|------|------|
| 动画不播放 | Animator 组件缺失或参数不存在 | 检查 State/Speed 参数是否配置 |
| 交互动画卡住 | 状态转换条件有误 | 检查 Has Exit Time 设置 |
| 模型不显示 | UI3DModel Layer 未创建 | Edit → Project Settings → Tags and Layers |
