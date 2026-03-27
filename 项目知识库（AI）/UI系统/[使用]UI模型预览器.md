# UIModelViewer 使用示例

> **最后更新**: 2026-03-23
> **状态**: 有效
> **核心类**: UIModelViewer

## 📋 目录

- [基础使用](#基础使用)
- [光照控制示例](#光照控制示例)
- [完整配置示例](#完整配置示例)
- [Inspector 配置建议](#inspector-配置建议)
- [注意事项](#注意事项)

---


```csharp
// 在 NewGameUI.cs 中已经自动初始化
// m_ModelViewer 会在 OnInit 时创建

// 加载模型
int modelConfigId = ResourceIds.GetSummonerModelId(summonerId);
await m_ModelViewer.SetModelAsync(modelConfigId);
```

## 光照控制示例

### 示例 1: 调整光照强度和颜色

```csharp
// 增强光照，让模型更明亮
m_ModelViewer.SetLightIntensity(1.5f);

// 设置暖色调光照
m_ModelViewer.SetLightColor(new Color(1f, 0.9f, 0.8f));
```

### 示例 2: 使用聚光灯效果

```csharp
// 切换为聚光灯
m_ModelViewer.SetLightType(LightType.Spot);

// 设置聚光灯参数
m_ModelViewer.SetSpotAngle(45f);        // 光圈大小 45 度
m_ModelViewer.SetLightRange(10f);       // 照射范围 10 单位
m_ModelViewer.SetLightIntensity(2f);    // 增强亮度

// 调整聚光灯位置（从上方照射）
m_ModelViewer.SetLightPosition(new Vector3(0f, 4f, 0f));
m_ModelViewer.LookAtModel();            // 朝向模型中心
```

### 示例 3: 多角度光照

```csharp
// 主光源（从前上方）
m_ModelViewer.SetLightType(LightType.Directional);
m_ModelViewer.SetLightPosition(new Vector3(0f, 3f, -2f));
m_ModelViewer.SetLightIntensity(1.2f);
m_ModelViewer.LookAtModel();

// 如果需要补光，可以获取 Light 组件手动添加第二个光源
Light mainLight = m_ModelViewer.GetLight();
// 然后在 UIModelViewer_Root 下手动添加补光...
```

### 示例 4: 动态光照效果

```csharp
// 呼吸灯效果
private async void PlayBreathingLight()
{
    float time = 0f;
    while (m_ModelViewer.HasModel())
    {
        time += Time.deltaTime;
        float intensity = 0.8f + Mathf.Sin(time * 2f) * 0.3f; // 0.5 ~ 1.1
        m_ModelViewer.SetLightIntensity(intensity);
        await UniTask.Yield();
    }
}

// 旋转光源效果
private async void RotateLight()
{
    float angle = 0f;
    while (m_ModelViewer.HasModel())
    {
        angle += Time.deltaTime * 30f; // 每秒旋转 30 度
        float x = Mathf.Sin(angle * Mathf.Deg2Rad) * 3f;
        float z = Mathf.Cos(angle * Mathf.Deg2Rad) * 3f;
        m_ModelViewer.SetLightPosition(new Vector3(x, 3f, z));
        m_ModelViewer.LookAtModel();
        await UniTask.Yield();
    }
}
```

[↑ 返回目录](#目录)

---

## 完整配置示例

### 场景 1: 角色展示（柔和光照）

```csharp
private void SetupCharacterDisplay()
{
    // 使用平行光，模拟自然光
    m_ModelViewer.SetLightType(LightType.Directional);
    m_ModelViewer.SetLightPosition(new Vector3(1f, 3f, -2f));
    m_ModelViewer.SetLightIntensity(1.0f);
    m_ModelViewer.SetLightColor(new Color(1f, 0.95f, 0.9f)); // 微暖色调
    m_ModelViewer.LookAtModel();
}
```

### 场景 2: 英雄选择（舞台效果）

```csharp
private void SetupHeroSelection()
{
    // 使用聚光灯，营造舞台感
    m_ModelViewer.SetLightType(LightType.Spot);
    m_ModelViewer.SetLightPosition(new Vector3(0f, 5f, -1f));
    m_ModelViewer.SetSpotAngle(40f);
    m_ModelViewer.SetLightRange(12f);
    m_ModelViewer.SetLightIntensity(1.8f);
    m_ModelViewer.SetLightColor(Color.white);
    m_ModelViewer.LookAtModel();
}
```

### 场景 3: 神秘氛围（点光源）

```csharp
private void SetupMysteriousAtmosphere()
{
    // 使用点光源，从下方照射
    m_ModelViewer.SetLightType(LightType.Point);
    m_ModelViewer.SetLightPosition(new Vector3(0f, 0.5f, 0f));
    m_ModelViewer.SetLightRange(8f);
    m_ModelViewer.SetLightIntensity(1.5f);
    m_ModelViewer.SetLightColor(new Color(0.6f, 0.8f, 1f)); // 冷色调
}
```

[↑ 返回目录](#目录)

---

## Inspector 配置建议

在 Unity Inspector 中，你可以直接调整 UIModelViewer 组件的参数：

### 推荐配置 1: 标准角色展示
```
Light Type: Directional
Light Intensity: 1.0
Light Color: (255, 242, 230) # 微暖白色
Light Position: (0, 3, -2)
Light Rotation: (0, 0, 0) # 自动朝向模型
```

### 推荐配置 2: 聚光灯舞台效果
```
Light Type: Spot
Light Intensity: 1.8
Light Color: (255, 255, 255)
Light Position: (0, 5, -1)
Spot Angle: 40
Light Range: 12
```

### 推荐配置 3: 柔和环境光
```
Light Type: Point
Light Intensity: 1.2
Light Color: (255, 250, 240)
Light Position: (1, 2, -1)
Light Range: 10
```

[↑ 返回目录](#目录)

---

## 注意事项

1. **光照强度**: 建议范围 0.5 ~ 2.0，过高会过曝
2. **聚光灯角度**: 建议范围 20° ~ 60°，太小会看不清，太大失去聚焦效果
3. **光源位置**: 建议在模型上方或侧上方，避免从下方照射（除非特殊效果）
4. **性能**: Directional 性能最好，Point 和 Spot 稍差，但在 UI 模型展示中影响不大

[↑ 返回目录](#目录)
