# NewGameUI 模型显示配置说明

> **最后更新**: 2026-03-23
> **状态**: 有效
> **技术**: RenderTexture + UI3D

## 📋 目录

- [实现方案](#实现方案)
- [Unity 配置步骤](#unity-配置步骤)
- [UIModelViewer 组件参数](#uimodelviewer-组件参数)
- [交互功能](#交互功能)
- [光照控制 API](#光照控制-api)
- [注意事项](#注意事项)
- [文件结构](#文件结构)

---


使用 **RenderTexture** 方案在 UI 上显示 3D 模型：
- 创建专用相机渲染模型到 RenderTexture
- 将 RenderTexture 显示在 UI 的 RawImage 上
- 模型放置在远离主场景的位置（避免干扰）

## Unity 配置步骤

### 1. 创建 Layer

在 Unity 中创建一个新的 Layer：
1. Edit → Project Settings → Tags and Layers
2. 在 Layers 中找到一个空位，添加名为 `UI3DModel` 的 Layer

### 2. 配置 NewGameUI 预制体

1. 找到 `varOccupationImage` 对象
2. 如果是 `Image` 组件，脚本会自动替换为 `RawImage`
3. 或者手动将 `Image` 组件替换为 `RawImage` 组件

### 3. 配置资源配置表

在 `ResourceConfigTable` 数据表中添加召唤师模型配置：

| Id | Type | Path |
|----|------|------|
| 9001 | 2 | Summoner/Model_1001 |
| 9002 | 2 | Summoner/Model_1002 |
| ... | ... | ... |

- **Id**: 8000 + 召唤师ID（如召唤师ID=1001，则配置ID=9001）
- **Type**: 2（预制体类型）
- **Path**: 模型预制体的相对路径

### 4. 准备模型预制体

1. 将召唤师模型放在 `Assets/AAAGame/Prefabs/Summoner/` 目录下
2. 命名规则：`Model_召唤师ID.prefab`（如 `Model_1001.prefab`）
3. 模型预制体要求：
   - 面向 Z 轴正方向
   - 原点在脚底
   - 适当的缩放（建议高度约 2 单位）

[↑ 返回目录](#目录)

---

## UIModelViewer 组件参数

### 渲染设置
| 参数 | 说明 | 默认值 |
|------|------|--------|
| renderTextureWidth | 渲染纹理宽度 | 512 |
| renderTextureHeight | 渲染纹理高度 | 512 |
| cameraDistance | 相机距离 | 3 |
| cameraHeight | 相机高度 | 1 |
| modelOffset | 模型偏移 | (0,0,0) |

### 交互设置
| 参数 | 说明 | 默认值 |
|------|------|--------|
| rotationSpeed | 旋转速度 | 0.5 |
| doubleClickThreshold | 双击阈值 | 0.3秒 |
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

### 其他设置
| 参数 | 说明 | 默认值 |
|------|------|--------|
| modelWorldPosition | 模型世界位置 | (1000,0,1000) |

[↑ 返回目录](#目录)

---

## 交互功能

- **拖拽旋转**: 在模型区域内按住鼠标左键拖拽可旋转模型
- **双击交互**: 双击模型触发交互事件（可自定义）

[↑ 返回目录](#目录)

---

## 光照控制 API

UIModelViewer 提供了丰富的光照控制方法：

```csharp
// 基础光照控制
modelViewer.SetLightIntensity(1.5f);           // 设置光照强度
modelViewer.SetLightColor(Color.yellow);       // 设置光照颜色
modelViewer.SetLightPosition(new Vector3(2, 3, -1)); // 设置光源位置
modelViewer.SetLightRotation(new Vector3(45, 0, 0)); // 设置光源旋转

// 聚光灯专用
modelViewer.SetSpotAngle(45f);                 // 设置聚光灯光圈大小
modelViewer.SetLightRange(15f);                // 设置光照范围

// 光源类型切换
modelViewer.SetLightType(LightType.Spot);      // 切换为聚光灯
modelViewer.SetLightType(LightType.Point);     // 切换为点光源
modelViewer.SetLightType(LightType.Directional); // 切换为平行光

// 辅助方法
modelViewer.LookAtModel();                     // 让光源朝向模型中心
Light light = modelViewer.GetLight();          // 获取光源组件进行更多自定义
```

### 光源类型说明

| 类型 | 说明 | 适用场景 |
|------|------|----------|
| Directional | 平行光，模拟太阳光 | 通用，性能最好 |
| Point | 点光源，向四周发光 | 需要环境光效果 |
| Spot | 聚光灯，锥形光束 | 需要聚焦效果，舞台感 |

[↑ 返回目录](#目录)

---

## 注意事项

1. **Layer 必须存在**: 确保 `UI3DModel` Layer 已创建，否则模型不会被渲染
2. **主相机排除**: 主相机的 Culling Mask 应排除 `UI3DModel` Layer
3. **光照**: UIModelViewer 会自动创建专用光源，无需额外配置
4. **透明背景**: RenderTexture 使用透明背景，模型会自然融入 UI

[↑ 返回目录](#目录)

---

## 文件结构

```
Assets/AAAGame/Scripts/
├── UI/
│   ├── NewGameUI.cs              # 主UI脚本
│   └── Components/
│       └── UIModelViewer.cs      # 模型查看器组件
└── Config/
    └── ResourceIds.cs            # 资源ID配置
```

[↑ 返回目录](#目录)
