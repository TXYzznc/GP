# 项目开发经验与最佳实践

当需要**加载UI资源（如Sprite、Prefab等）**时，应该优先使用ResourceExtension中的方法来实现：
- **快速初始化**：使用`SetSpriteById(id)`或`SetSprite(path)`（UIExtension扩展方法），在OnInit中快速加载，无需等待
- **直接加载到Image对象**：使用`await ResourceExtension.LoadSpriteAsync(id, targetImage, alpha, size)`重载，自动完成赋值+透明度+缩放，无需手动赋值
  - UniTask版（推荐）：`await LoadSpriteAsync(id, image)` 或 `await LoadSpriteAsync(id, image, 0.8f, Vector3.one * 1.2f)`
  - 回调版：`ResourceExtension.LoadSpriteAsync(id, image, onFailureCallback, 0.8f, null)`（需要错误处理时使用）
- **火发即忘（fire-and-forget）**：使用`_ = ResourceExtension.LoadSpriteAsync(id, image)`，明确忽略返回值（避免CS4014警告）
- **获取Sprite对象本身**：使用原有`await LoadSpriteAsync(id)`获取结果后手动赋值（需要Sprite而非直接加载到Image时）
- **避免**：不要使用GF.Resource.LoadSprite()等基础方法，功能不完整且缺少配置表ID支持

当需要**实现UI组件对数据的动态响应（如Buff增删、层数变化）**时，应该优先在数据管理器（如BuffManager）中添加事件系统（OnBuffAdded、OnBuffRemoved、OnBuffStackChanged等），让UI组件订阅这些事件，通过事件驱动的方式实现动态更新，而不是轮询或手动刷新。同时应提供数据查询方法（如GetAllBuffs()）供初始化使用。

当需要**在UI中隐藏模板预制体或动态项**时，不应该在OnInit/Awake中直接对预制体引用调用SetActive(false)（如varBuffItem.SetActive(false)），因为这会修改Editor中的预制体资源状态，导致退出Play Mode后预制体保持隐藏。
**方案选择标准**：
1. 如果有Instantiate调用后再隐藏 → 删除SetActive(false)，保持预制体激活
2. 如果预制体模板本身不需要显示 → 在Prefab Inspector中设置为非激活状态

## 资源加载代码示例

### ✅ 快速初始化（OnInit中使用）
```csharp
protected override void OnInit()
{
    varImgBackground.SetSpriteById(ResourceIds.MENU_BACKGROUND);  // 配置表ID方式
    varIcon.SetSprite("some/path");                                // 直接路径方式
}
```

### ✅ 直接加载到Image对象（推荐新方式）
```csharp
// UniTask版 - 自动赋值+透明度+缩放，一行代码完成
private async void LoadIconAsync(int iconId)
{
    await ResourceExtension.LoadSpriteAsync(iconId, varImage);
    // 图片已自动赋值、alpha设为1.0、scale设为(1,1,1)
}

// 自定义透明度和缩放
private async void LoadIconWithAlpha(int iconId)
{
    await ResourceExtension.LoadSpriteAsync(iconId, varImage, 0.8f, Vector3.one * 1.2f);
    // 图片已自动赋值、alpha设为0.8、scale设为(1.2,1.2,1.2)
}

// 火发即忘（fire-and-forget）
private void UpdateBuffIcon(int spriteId)
{
    if (varBuffImg != null)
    {
        _ = ResourceExtension.LoadSpriteAsync(spriteId, varBuffImg);  // ✓ 使用 _ = 忽略返回值
    }
}

// 回调版 - 需要错误处理时使用
ResourceExtension.LoadSpriteAsync(
    iconId,
    varImage,
    error => DebugEx.Error($"加载失败: {error}"),  // onFailure是必需参数
    0.8f,    // alpha
    null     // size
);
```

### ✅ 异步加载获取Sprite对象（需要手动赋值时使用）
```csharp
private async void SetQualityUI(int quality)
{
    var sprite = await ResourceExtension.LoadSpriteAsync(cardFrameId);
    if (sprite != null && varCardFrame != null)
    {
        varCardFrame.sprite = sprite;  // 手动赋值
    }
}
```

### ❌ 应避免的写法
```csharp
GF.Resource.LoadSprite(path);  // 避免，功能不完整
varIcon.SetSprite("path/to/sprite")  // 避免，与项目风格不符
// varBG.SetSpriteById(...);     // 避免留下注释，应删除

// ❌ 不要这样写（会产生CS4014警告）
ResourceExtension.LoadSpriteAsync(id, image);  // 没有 await 也没有 _ =

// ✅ 正确做法
_ = ResourceExtension.LoadSpriteAsync(id, image);  // 显式忽略返回值
```