# 任务执行参数确认

## 用户决策

| 参数 | 决策 | 备注 |
|------|------|------|
| 阶段顺序 | 1-2-3（按优先级） | 配置表 → UI/战斗 → 其他系统 |
| 验证深度 | 全面审查 | 不采抽样，每文件逐一核验 |
| 会话时间 | 无限制 | 尽量在单会话内完成更多内容 |
| 启动时间 | 稍后启动 | 等待用户明确"开始"指令 |

## 启动指令

**开始第1阶段时，用户应输入：**
```
/opsx:apply
```

**或直接说：**
```
现在开始第1阶段审核
```

系统将自动加载tasks.md，从1.1开始逐一执行。

---

## 执行流程确认

每个文件的标准工作流：

```
1. 【读文档】读wiki/.../文件.md → 理解核心内容
2. 【查代码】/graphify query "关键系统" → 快速定位相关脚本
3. 【核验】读脚本 → 对标配置表 → 检查一致性
4. 【判断】是否过时？
   ├─ YES → 【改】更新内容 + 时间戳2026-04-17
   └─ NO  → 【标】已验证无误，附注时间戳
5. 【提交】修改完毕，打勾任务清单
```

---

## 阶段时间预估

| 阶段 | 文件数 | 预估时间 | 状态 |
|------|--------|---------|------|
| 第1 | 14 | 1.5-2h | 準備待啟動 |
| 第2 | 21 | 2-2.5h | 待第1完成 |
| 第3 | 86 | 4-5h | 待第2完成 |
| **合計** | **121** | **7.5-9.5h** | - |

因用户设置"无限制"时间，可在单会话内完成1-2个阶段。

---

## 关键文件路径

- **openspec变更**: `d:\unity\UnityProject\GP\Clash_Of_Gods\.claude\skills\openspec\openspec\changes\update-wiki-content\`
- **任务清单**: `tasks.md`（核心控制文件，/opsx:apply会读这个）
- **wiki根目录**: `d:\unity\UnityProject\GP\Clash_Of_Gods\项目知识库（AI自行维护）\wiki\`
- **代码根目录**: `d:\unity\UnityProject\GP\Clash_Of_Gods\Assets\AAAGame\Scripts\Game\`
- **配置表目录**: `d:\unity\UnityProject\GP\Clash_Of_Gods\Assets\AAAGame\DataTable\`

---

## graphify快速查询示例

第1阶段常用查询：

```bash
/graphify query "ItemManager"              # 物品系统核心
/graphify query "BuffTable Buff机制"       # Buff应用流程
/graphify query "DamageCalculator"         # 伤害公式
/graphify query "ItemTable配置表"          # 配置表结构
/graphify query "UIModelViewer"            # UI模型预览
```

---

## 准备完毕

✅ openspec规划完成
✅ 用户决策已记录
✅ 启动参数已设定

**等待用户启动指令...**
