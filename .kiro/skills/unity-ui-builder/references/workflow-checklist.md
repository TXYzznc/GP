# 工作流检查清单

每个阶段完成时对照此清单，确保没有遗漏。

---

## Phase 1: 需求分析 ✓

- [ ] 已阅读 INDEX.md 定位相关文档
- [ ] 已用 Graphify/GitNexus 分析相关系统
- [ ] 已明确数据来源（DataTable、Manager、SaveData）
- [ ] 已识别可复用的现有 UI 组件
- [ ] 已梳理 UI 打开/关闭/跳转流程
- [ ] 所有模糊需求已向用户确认

---

## Phase 2: 界面设计 ✓

- [ ] 已确定整体布局和视觉风格
- [ ] 信息层级清晰（主要信息 > 次要信息 > 辅助信息）
- [ ] 交互方式直觉化（按钮位置、点击反馈）
- [ ] 动效方案已设计（开关动画、选中反馈、状态过渡）
- [ ] 适配 1920×1080 参考分辨率

---

## Phase 3: HTML 原型 ✓

- [ ] HTML 文件可独立运行（内联 CSS/JS）
- [ ] 包含所有子界面和交互状态
- [ ] 尺寸基于 1920×1080
- [ ] 已输出到 `AI工作区/` 目录
- [ ] 已向用户展示并获得确认
- [ ] 用户明确表示"没问题"

---

## Phase 4: 参考图 ✓

- [ ] 所有界面状态已截取为 PNG
- [ ] 覆盖完整，无遗漏的子界面或状态
- [ ] 图片保存在与 HTML 同目录下

---

## Phase 5: 脚本创建 ✓

- [ ] Variables 文件已创建（命名规范，字段完整）
- [ ] 主逻辑脚本已创建（继承 UIFormBase，partial class）
- [ ] 使用 UniTask 而非协程
- [ ] 日志使用 DebugEx
- [ ] 不硬编码数值
- [ ] 触发 Unity 重新编译
- [ ] `debug_get_errors` 返回零错误

---

## Phase 6: 准备构建 ✓

- [ ] 已提醒用户启动 UnitySkills 服务器
- [ ] 已获取端口号

---

## Phase 7: Unity 场景构建 ✓

- [ ] 根对象是 Canvas，已挂载 UI 脚本
- [ ] 对象命名与 Variables 字段名对应（去掉 var 前缀）
- [ ] 坐标、锚点、尺寸与参考图一致
- [ ] 颜色值与参考图一致
- [ ] 非交互对象已关闭 Raycast Target
- [ ] 模板对象默认隐藏（SetActive false）
- [ ] 结构层级清晰规范

---

## Phase 8: 保存预制体 ✓

- [ ] 已保存为预制体到 `Assets/AAAGame/Prefabs/UI/`
- [ ] 已告知用户需手动完成的步骤：
  - [ ] Inspector 中连接 Variables 引用
  - [ ] UITable.txt 添加记录
  - [ ] UIViews.cs 添加枚举项
  - [ ] （可选）重新生成 Variables 文件
