## Context

项目使用 Unity GameFramework + DOTween。UIFormBase 基类已内置 DOTweenSequence 的 Open/Close 动画机制，但大部分界面未配置动画。当前仅 CombatPreparationUI（棋子选中脉冲）和 SettingDialog（振动开关滑块）有少量 DOTween 动画。

**技术约束：**
- 动画引擎：DOTween（已集成），使用 `CanvasGroup.alpha` + `transform`（GPU 加速）
- UI 模式：Screen Space - Camera，坐标系需注意
- 对象池：UI 关闭时必须 `DOTween.Kill` 防止动画泄漏
- 性能目标：60fps，单个动画时长 100-500ms

**UI 分类（按动效策略分组）：**

| 分类 | 界面 | 动效策略 |
|------|------|---------|
| A. 全屏页面 | StartMenuUI, MenuUIForm, LoadGameUI, NewGameUI, GameUIForm | 整体淡入 + 子元素编排入场 |
| B. 战斗系统 | CombatPreparationUI, CombatUI, GameOverUIForm, EscapeResultUI | 强调感 + 状态驱动动效 |
| C. 面板/弹窗 | ShopUIForm, InventoryUI, WarehouseUI, CloudArchiveUI | 滑入/缩放弹出 |
| D. HUD 信息条 | GamePlayInfoUI, CurrencyUI, PlayerSkillUI, StarPhoneUI, OutsiderFunctionUI | 轻量淡入滑入 |
| E. 对话框 | SettingDialog, RatingDialog, LanguagesDialog, CommonDialog | 居中缩放弹出 + 遮罩淡入 |
| F. 通知/提示 | UITopbar, ToastTips, FloatingBoxTip | 边缘滑入 + 自动消失 |
| G. 其他 | AimUI | 淡入淡出 |

## Goals / Non-Goals

**Goals:**
- 为 26 个 UIForm 添加统一风格的打开/关闭过渡动画
- 关键界面（战斗、结算）添加子元素编排入场动效
- 所有动画纯代码实现（DOTween），不依赖 Prefab 修改
- 动画可被正确打断和清理（防止对象池泄漏）

**Non-Goals:**
- 不修改 UIItem 子组件的动画（本次仅处理 UIForm 级别）
- 不修改 Prefab 结构
- 不引入新依赖
- 不做按钮微交互（hover/press 反馈）

## Decisions

### 决策 1：纯代码动画 vs Inspector 配置 DOTweenSequence

**选择：纯代码实现**

理由：
- UIFormBase 的 `m_OpenAnimation`/`m_CloseAnimation` 是 Inspector 上的 DOTweenSequence，需要逐个 Prefab 手动配置，工作量大
- 代码方式可以统一管理、批量应用，且易于维护
- 子元素编排动画（stagger）用代码更灵活

**实现方式：** 在每个 UI 脚本中 override `OnOpen` / `OnClose`，使用 DOTween API 编写动画。

### 决策 2：动画基础设施 — UIAnimationHelper 工具类

创建一个静态工具类 `UIAnimationHelper`，提供常用动画模板：

```csharp
public static class UIAnimationHelper
{
    // 淡入（CanvasGroup alpha 0→1）
    public static Tween FadeIn(CanvasGroup cg, float duration = 0.3f)
    
    // 淡出（CanvasGroup alpha 1→0）
    public static Tween FadeOut(CanvasGroup cg, float duration = 0.25f)
    
    // 从底部滑入（localPosition.y 偏移 → 0）
    public static Tween SlideInFromBottom(RectTransform rt, float offset = 100f, float duration = 0.35f)
    
    // 从顶部滑入
    public static Tween SlideInFromTop(RectTransform rt, float offset = 100f, float duration = 0.3f)
    
    // 从左侧滑入
    public static Tween SlideInFromLeft(RectTransform rt, float offset = 200f, float duration = 0.35f)
    
    // 从右侧滑入
    public static Tween SlideInFromRight(RectTransform rt, float offset = 200f, float duration = 0.35f)
    
    // 缩放弹出（scale 0.85→1 + alpha 0→1）
    public static Sequence PopIn(RectTransform rt, CanvasGroup cg, float duration = 0.3f)
    
    // 缩放收回（scale 1→0.85 + alpha 1→0）
    public static Sequence PopOut(RectTransform rt, CanvasGroup cg, float duration = 0.2f)
    
    // 子元素依次入场（stagger）
    public static Sequence StaggerChildren(Transform parent, float staggerDelay = 0.05f, float duration = 0.25f)
    
    // 清理对象上的所有动画
    public static void KillAll(Component target)
}
```

**缓动曲线统一：**
- 入场：`Ease.OutQuart`（自然减速）
- 退场：`Ease.InQuart`（快速收回）
- 弹出：`Ease.OutQuart`（不用 Bounce/Elastic）

### 决策 3：动画生命周期管理

在 UIFormBase 或各 UI 脚本中确保：
1. `OnOpen` 开始时 `DOTween.Kill(gameObject)` 清理残留动画
2. `OnClose` 开始时 `DOTween.Kill(gameObject, true)` 强制完成残留动画
3. 关闭动画使用 `CloseWithAnimation()` 路径，动画完成后才真正关闭

### 决策 4：StateAwareUIForm 的 ShowUI/HideUI 动画

StateAwareUIForm 子类（GamePlayInfoUI、CurrencyUI 等）通过 `ShowUI()`/`HideUI()` 切换显示。需要在这两个方法中注入淡入/淡出动画，而非直接 SetActive。

**方案：** override StateAwareUIForm 的 ShowUI/HideUI，加入 CanvasGroup 淡入淡出。

---

## 每个界面的动效设计

### A. 全屏页面

#### 1. StartMenuUI — 开始菜单
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 背景淡入(0.4s) → 标题Logo缩放入场(0→1, 0.5s) → 按钮组从底部依次滑入(stagger 0.08s) | Ease.OutQuart |
| 关闭 | 整体淡出(0.3s) | Ease.InQuart |

#### 2. MenuUIForm — 主菜单
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 整体淡入(0.3s) + 内容区域从底部轻微滑入(offset=50, 0.35s) | Ease.OutQuart |
| 关闭 | 整体淡出(0.25s) | Ease.InQuart |

#### 3. LoadGameUI — 读档界面
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 面板缩放弹出 PopIn(0.3s) + 存档列表项依次入场(stagger 0.06s) | Ease.OutQuart |
| 关闭 | PopOut(0.2s) | Ease.InQuart |

#### 4. NewGameUI — 新游戏创建
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 整体淡入(0.3s) | Ease.OutQuart |
| 步骤切换 | 当前步骤淡出+左滑(0.25s) → 新步骤淡入+右滑入(0.3s) | 交叉过渡 |
| 关闭 | 整体淡出(0.25s) | Ease.InQuart |

#### 5. GameUIForm — 游戏主界面
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 整体淡入(0.3s) | Ease.OutQuart |
| 关闭 | 整体淡出(0.2s) | Ease.InQuart |

### B. 战斗系统

#### 6. CombatPreparationUI — 战斗准备
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 整体淡入(0.3s) → 棋子面板从底部滑入(0.35s) → 装备面板从右侧滑入(0.35s) → 倒计时文本缩放脉冲 | stagger 0.1s |
| Buff选择面板 | 缩放弹出 PopIn(0.3s) | 已有alpha淡入，增加缩放 |
| 关闭 | 整体淡出(0.25s) | Ease.InQuart |

#### 7. CombatUI — 战斗进行
| 动效 | 描述 | 参数 |
|------|------|------|
| 显示 | 顶部敌人信息从上方滑入(0.3s) + 底部卡牌区从下方滑入(0.35s) + 左侧HP/MP条从左滑入(0.3s) | 同时进行 |
| 隐藏 | 各区域反向滑出(0.2s) | Ease.InQuart |

#### 8. GameOverUIForm — 游戏结束
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 背景遮罩淡入(0.3s) → 标题文本缩放弹入(0.4s, scale 1.2→1) → 按钮淡入+上滑(0.3s) | 序列播放 |
| 关闭 | 整体淡出(0.3s) | Ease.InQuart |

#### 9. EscapeResultUI — 脱战结果
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 从屏幕上方滑入+淡入(0.3s) | Ease.OutQuart |
| 自动关闭 | 向上滑出+淡出(0.25s) | 替代现有的直接关闭 |

### C. 面板/弹窗

#### 10. ShopUIForm — 商城（代码已注释，跳过）

#### 11. InventoryUI — 背包
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 面板从右侧滑入(0.35s) + 淡入(0.3s) | Ease.OutQuart |
| 关闭 | 向右滑出(0.25s) + 淡出(0.2s) | Ease.InQuart |

#### 12. WarehouseUI — 仓库
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 面板从左侧滑入(0.35s) + 淡入(0.3s) | Ease.OutQuart |
| 关闭 | 向左滑出(0.25s) + 淡出(0.2s) | Ease.InQuart |

#### 13. CloudArchiveUI — 云存档
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 缩放弹出 PopIn(0.3s) | Ease.OutQuart |
| 关闭 | PopOut(0.2s) | Ease.InQuart |

### D. HUD 信息条（StateAwareUIForm）

#### 14. GamePlayInfoUI — 游戏信息
| 动效 | 描述 | 参数 |
|------|------|------|
| 显示 | 从左侧轻微滑入(offset=80, 0.3s) + 淡入 | Ease.OutQuart |
| 隐藏 | 向左滑出+淡出(0.2s) | Ease.InQuart |

#### 15. CurrencyUI — 货币显示
| 动效 | 描述 | 参数 |
|------|------|------|
| 显示 | 从上方轻微滑入(offset=30, 0.25s) + 淡入 | Ease.OutQuart |
| 隐藏 | 向上滑出+淡出(0.2s) | Ease.InQuart |

#### 16. PlayerSkillUI — 玩家技能
| 动效 | 描述 | 参数 |
|------|------|------|
| 显示 | 从底部滑入(offset=60, 0.3s) + 淡入 | Ease.OutQuart |
| 隐藏 | 向下滑出+淡出(0.2s) | Ease.InQuart |

#### 17. StarPhoneUI — 星盘手机
| 动效 | 描述 | 参数 |
|------|------|------|
| 显示 | 缩放入场(0.8→1, 0.25s) + 淡入 | Ease.OutQuart |
| 隐藏 | 缩放缩小(1→0.8, 0.2s) + 淡出 | Ease.InQuart |

#### 18. OutsiderFunctionUI — 局外功能菜单
| 动效 | 描述 | 参数 |
|------|------|------|
| 显示 | 从底部滑入(offset=50, 0.3s) + 淡入 + 功能项依次入场(stagger 0.05s) | Ease.OutQuart |
| 隐藏 | 向下滑出+淡出(0.2s) | Ease.InQuart |

### E. 对话框

#### 19. SettingDialog — 设置
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 缩放弹出 PopIn(0.3s) | Ease.OutQuart |
| 关闭 | PopOut(0.2s) | Ease.InQuart |

#### 20. RatingDialog — 评分
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 缩放弹出 PopIn(0.3s) | Ease.OutQuart |
| 关闭 | PopOut(0.2s) | Ease.InQuart |

#### 21. LanguagesDialog — 语言选择
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 缩放弹出 PopIn(0.3s) | Ease.OutQuart |
| 关闭 | PopOut(0.2s) | Ease.InQuart |

#### 22. CommonDialog — 通用对话框
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 缩放弹出 PopIn(0.3s) | Ease.OutQuart |
| 关闭 | PopOut(0.2s) | Ease.InQuart |

### F. 通知/提示

#### 23. UITopbar — 顶部信息栏
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 从顶部滑入(offset=80, 0.3s) + 淡入 | Ease.OutQuart |
| 关闭 | 向上滑出+淡出(0.2s) | Ease.InQuart |

#### 24. ToastTips — 浮动提示
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 从顶部滑入(offset=50, 0.3s) + 淡入 + 轻微缩放(0.95→1) | Ease.OutQuart |
| 自动关闭 | 向上滑出+淡出(0.25s) | Ease.InQuart |

#### 25. FloatingBoxTip — 浮动提示框
| 动效 | 描述 | 参数 |
|------|------|------|
| 打开 | 缩放弹出(0.9→1, 0.2s) + 淡入 | Ease.OutQuart |
| 关闭 | 缩放缩小+淡出(0.15s) | Ease.InQuart |

### G. 其他

#### 26. AimUI — 瞄准UI（空实现，跳过）

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| DOTween 动画泄漏（UI 关闭后动画仍在播放） | 每个 UI 的 OnOpen/OnClose 中强制 `DOTween.Kill(gameObject)` |
| 动画期间用户快速操作（连续打开/关闭） | UIFormBase 已有 `Interactable = false` 机制，动画完成才可交互 |
| StateAwareUIForm 快速切换状态导致动画叠加 | ShowUI/HideUI 中先 Kill 再播放新动画 |
| ShopUIForm 代码已注释 | 跳过该界面，不添加动效 |
| AimUI 为空实现 | 跳过该界面 |
| 性能影响 | 仅使用 transform + alpha 动画（GPU 加速），避免 layout 属性 |
