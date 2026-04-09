## Why

项目中大部分 UI 界面缺少过渡动效，打开/关闭时生硬突兀，交互缺乏反馈感。为所有 UI 界面添加统一、有目的性的动效，可以显著提升用户体验和游戏品质感。项目已有 DOTween 基础设施（UIFormBase 支持 DOTweenSequence 的 Open/Close 动画），但多数界面尚未配置。

## What Changes

### 主要 UIForm 界面（28个）

#### 游戏主流程 UI（5个）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 1 | **StartMenuUI** | StartMenuUI.prefab | 开始菜单：开始、继续、设置入口 |
| 2 | **MenuUIForm** | MenuUIForm.prefab | 主菜单：玩家数据和主菜单功能 |
| 3 | **LoadGameUI** | LoadGameUI.prefab | 读档界面：存档选择 |
| 4 | **NewGameUI** | NewGameUI.prefab | 新游戏/角色创建界面 |
| 5 | **GameUIForm** | GameUIForm.prefab | 游戏主界面：金币等基础信息 |

#### 战斗相关 UI（6个）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 6 | **CombatPreparationUI** | CombatPreparationUI.prefab | 战斗准备：配置棋子、倒计时 |
| 7 | **CombatUI** | CombatUI.prefab | 战斗进行中：棋子详情、技能（StateAwareUIForm） |
| 9 | **GameOverUIForm** | GameOverUIForm.prefab | 游戏结束：胜利/失败结果 |
| 11 | **EscapeResultUI** | EscapeResultUI.prefab | 脱战结果：成功/失败 |

#### 商城与背包系统 UI（3个）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 12 | **ShopUIForm** | ShopUIForm.prefab | 商城：商品展示、购买 |
| 13 | **InventoryUI** | InventoryUI.prefab | 背包：物品管理、快捷栏 |
| 14 | **WarehouseUI** | WarehouseUI.prefab | 仓库：物品存储 |

#### 局内信息展示 UI（3个，StateAwareUIForm）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 15 | **GamePlayInfoUI** | GamePlayInfoUI.prefab | 游戏过程信息展示 |
| 16 | **CurrencyUI** | CurrencyUI.prefab | 货币显示 |
| 17 | **PlayerSkillUI** | PlayerSkillUI.prefab | 玩家技能界面 |

#### 手机/功能菜单 UI（2个，StateAwareUIForm）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 18 | **StarPhoneUI** | StarPhoneUI.prefab | 手机界面：局外功能菜单 |
| 19 | **OutsiderFunctionUI** | OutsiderFunctionUI.prefab | 局外功能菜单 |

#### 对话框与系统 UI（5个）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 20 | **SettingDialog** | SettingDialog.prefab | 设置对话框 |
| 21 | **RatingDialog** | RatingDialog.prefab | 评分对话框 |
| 22 | **LanguagesDialog** | LanguagesDialog.prefab | 语言选择对话框 |
| 23 | **CommonDialog** | CommonDialog.prefab | 通用确认/提示对话框 |
| 24 | **CloudArchiveUI** | CloudArchiveUI.prefab | 云存档管理界面 |

#### 顶部信息栏与提示 UI（3个）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 25 | **UITopbar** | Topbar.prefab | 顶部信息栏 |
| 26 | **ToastTips** | ToastTips.prefab | 浮动提示条 |
| 27 | **FloatingBoxTip** | FloatingBoxTip.prefab | 浮动提示框 |

#### 其他 UI（1个）
| 序号 | 类名 | Prefab | 功能描述 |
|-----|------|--------|--------|
| 28 | **AimUI** | AimUI.prefab | 瞄准UI |

### UIItem 子组件（20个）

| 序号 | 类名 | 功能描述 |
|-----|------|--------|
| 1 | ChessItemUI | 棋子槽位UI |
| 2 | BuffItem | Buff项显示 |
| 3 | CardSlotItem | 卡牌槽位 |
| 4 | BuffChooseItem | Buff选择项 |
| 5 | SummonChessStateUI | 召唤棋子状态 |
| 6 | DetailInfoUI | 详情信息UI |
| 7 | InventorySlotUI | 背包格子 |
| 8 | InventoryItemUI | 背包物品 |
| 9 | ItemContextMenu | 物品上下文菜单 |
| 10 | AwardItemUI | 奖励项 |
| 11 | FunctionItem | 功能项 |
| 12 | GameItem | 游戏存档项 |
| 13 | PlayerSkillSlot | 技能槽位 |
| 14 | CurrencyItem | 货币项 |
| 15 | LanguageItem | 语言项 |
| 16 | ItemsInfoItem | 物品信息项 |
| 17 | PlayerInfoItem | 玩家信息项 |
| 18 | TimeInfoItem | 时间信息项 |
| 19 | EnemyMask | 敌人警示指示器 |
| 20 | PageLabelItem | 页签项 |

## Capabilities

### New Capabilities
- `ui-open-close-animation`: 所有 UIForm 的打开/关闭过渡动画（淡入淡出、缩放、滑动等）
- `ui-element-micro-interaction`: UI 内部元素的微交互动效（按钮反馈、列表项入场、状态变化等）

### Modified Capabilities
（无需修改现有 spec）

## Impact

- **代码变更**：28 个 UIForm 脚本 + 20 个 UIItem 脚本，主要是在脚本中通过 DOTween 编写动画代码
- **框架依赖**：复用现有 UIFormBase 的 DOTweenSequence 机制（m_OpenAnimation / m_CloseAnimation），部分界面可能需要在代码中手动编排 DOTween 动画
- **性能考量**：所有动画使用 transform + CanvasGroup.alpha（GPU 加速），避免布局属性动画；时长控制在 100-500ms
- **技术栈**：DOTween（已集成），无需引入新依赖
- **风险**：UI 关闭时 DOTween 动画需正确 Kill/Complete，防止对象池回收后动画仍在播放
