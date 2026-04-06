# 技术美术（TA）系统分析

> **最后更新**: 2026-04-06
> **用途**: 支持论文第 7 章的编写
> **项目**: Clash of Gods

---

## 一、TA 系统概述

### 1.1 系统定义

技术美术（Technical Art, TA）系统是游戏中负责视觉效果和渲染的核心系统，包括：
- Shader 编程（着色器）
- 特效系统
- 渲染管线优化
- 视觉效果设计

### 1.2 项目中的 TA 系统

Clash of Gods 项目包含以下 TA 模块：

| 模块 | 位置 | 功能 |
|------|------|------|
| **卡通渲染** | `Assets/TA/Cartoon/` | 卡通风格渲染 |
| **溶解效果** | `Assets/TA/溶解/` | 物体溶解动画 |
| **描边效果** | `Assets/TA/OuterGlow/` | 物体描边高亮 |
| **天空盒** | `Assets/TA/Sky/` | 场景天空 |
| **UI 特效** | `Assets/TA/UI/` | UI 特效 |

---

## 二、核心 TA 模块详解

### 2.1 卡通渲染系统（Cartoon Shader）

**位置**: `Assets/TA/Cartoon/`

**文件清单**:
- `PlayerCartoon.shader` - 玩家角色卡通渲染
- `OilPaintingToon.shader` - 油画风格卡通渲染
- `Modules/DissolveModule.hlsl` - 溶解模块（HLSL）

**功能特性**:
1. **卡通风格渲染**
   - 非真实感渲染（NPR）
   - 分层着色（Toon Shading）
   - 边缘检测和描边

2. **光照系统**
   - 简化的光照计算
   - 分段颜色（Color Bands）
   - 高光效果

3. **溶解效果集成**
   - 支持溶解动画
   - 参数化配置
   - 平滑过渡

**技术细节**:
```
卡通渲染流程：
1. 计算基础颜色（Albedo）
2. 计算法线和光照
3. 应用分段着色
4. 添加描边效果
5. 应用溶解效果
6. 输出最终颜色
```

**应用场景**:
- 玩家角色渲染
- 敌人角色渲染
- 召唤物渲染

### 2.2 溶解效果系统（Dissolve Shader）

**位置**: `Assets/TA/溶解/`

**文件清单**:
- `Dissolve.shader` - 溶解效果着色器

**功能特性**:
1. **溶解动画**
   - 从上到下溶解
   - 从下到上溶解
   - 自定义溶解方向

2. **参数控制**
   - 溶解进度（0-1）
   - 溶解边缘宽度
   - 溶解颜色

3. **性能优化**
   - 使用噪声纹理
   - 低成本计算
   - 支持批处理

**技术细节**:
```
溶解效果流程：
1. 采样噪声纹理
2. 比较噪声值与溶解阈值
   - 噪声 > 阈值：保留像素
   - 噪声 ≤ 阈值：丢弃像素
3. 在边缘添加发光效果
4. 输出最终颜色
```

**应用场景**:
- 敌人死亡动画
- 物体消失效果
- 传送特效

### 2.3 描边效果系统（Outline/OuterGlow）

**位置**: `Assets/TA/OuterGlow/`

**文件清单**:
- `Custom_Outline.shader` - 基础描边着色器
- `Custom_Outline_Blur.shader` - 模糊描边着色器
- `Ally.asset` - 友方单位描边配置
- `Enemy.asset` - 敌方单位描边配置
- `Interactive.asset` - 可交互物体描边配置
- `Selected.asset` - 选中状态描边配置

**功能特性**:
1. **描边类型**
   - 基础描边（Sharp Outline）
   - 模糊描边（Blur Outline）
   - 可配置宽度和颜色

2. **多种配置**
   - 友方单位：绿色描边
   - 敌方单位：红色描边
   - 可交互物体：黄色描边
   - 选中状态：白色描边

3. **性能优化**
   - 使用 Outline Pass
   - 支持批处理
   - 低开销渲染

**技术细节**:
```
描边效果流程：
1. 第一 Pass：正常渲染
2. 第二 Pass：描边渲染
   - 放大模型顶点
   - 使用单色渲染
   - 应用模糊（可选）
3. 合并两个 Pass 的结果
```

**应用场景**:
- 棋子高亮显示
- 敌人识别
- 可交互物体提示
- 选中状态反馈

### 2.4 天空盒系统（Sky）

**位置**: `Assets/TA/Sky/`

**功能特性**:
1. **天空盒类型**
   - 立方体贴图天空盒
   - 程序化天空盒
   - 动态天空盒

2. **场景适配**
   - 不同场景不同天空
   - 时间变化效果
   - 天气系统集成

**应用场景**:
- 探索场景背景
- 战斗场景背景
- 基地场景背景

### 2.5 UI 特效系统（UI）

**位置**: `Assets/TA/UI/`

**功能特性**:
1. **UI 动画**
   - 按钮特效
   - 面板特效
   - 过渡特效

2. **粒子效果**
   - UI 粒子系统
   - 特效叠加
   - 性能优化

**应用场景**:
- UI 按钮反馈
- 面板打开/关闭动画
- 特殊效果展示

---

## 三、TA 脚本系统

### 3.1 脚本位置

`Assets/AAAGame/Scripts/TA/` 包含以下模块：

| 模块 | 功能 |
|------|------|
| **溶解** | 溶解效果控制脚本 |
| **噪声** | 噪声生成和管理 |
| **卡通** | 卡通渲染控制脚本 |
| **描边** | 描边效果控制脚本 |

### 3.2 主要脚本功能

**溶解脚本**:
- 控制溶解进度
- 管理溶解动画
- 处理溶解完成事件

**噪声脚本**:
- 生成噪声纹理
- 管理噪声参数
- 优化噪声计算

**卡通脚本**:
- 管理卡通材质
- 控制光照参数
- 处理角色渲染

**描边脚本**:
- 管理描边材质
- 控制描边颜色和宽度
- 处理描边状态切换

---

## 四、TA 系统架构

### 4.1 系统设计

```
TA 系统架构
├── Shader 层
│   ├── 卡通渲染 Shader
│   ├── 溶解 Shader
│   ├── 描边 Shader
│   └── 其他特效 Shader
├── 脚本层
│   ├── 效果控制脚本
│   ├── 参数管理脚本
│   └── 动画控制脚本
└── 配置层
    ├── 材质配置
    ├── 参数配置
    └── 效果配置
```

### 4.2 数据流

```
游戏逻辑
    ↓
TA 脚本（控制层）
    ↓
材质参数更新
    ↓
Shader 计算
    ↓
渲染输出
```

---

## 五、关键技术点

### 5.1 Shader 编程

**技术要点**:
1. **顶点着色器**
   - 顶点变换
   - 顶点动画
   - 法线计算

2. **片元着色器**
   - 纹理采样
   - 光照计算
   - 特效处理

3. **优化技巧**
   - 减少计算量
   - 使用预计算
   - 纹理缓存

### 5.2 性能优化

**优化方案**:
1. **批处理**
   - 合并 Draw Call
   - 使用 GPU Instancing
   - 动态批处理

2. **纹理优化**
   - 压缩纹理
   - 使用 Mipmap
   - 纹理图集

3. **渲染优化**
   - 剔除不可见物体
   - 使用 LOD 系统
   - 延迟渲染

### 5.3 特效集成

**集成方式**:
1. **材质系统**
   - 创建材质实例
   - 设置材质参数
   - 应用到模型

2. **动画系统**
   - 使用 Animator 控制
   - 参数动画化
   - 事件触发

3. **事件系统**
   - 特效开始事件
   - 特效结束事件
   - 特效中断事件

---

## 六、应用案例

### 6.1 敌人死亡效果

**流程**:
1. 敌人血量 = 0
2. 触发死亡事件
3. 播放溶解动画
4. 溶解完成后销毁

**涉及模块**:
- 溶解 Shader
- 溶解脚本
- 事件系统

### 6.2 棋子选中效果

**流程**:
1. 点击棋子
2. 切换描边配置
3. 显示选中描边
4. 取消选中时隐藏

**涉及模块**:
- 描边 Shader
- 描边脚本
- UI 交互系统

### 6.3 角色渲染

**流程**:
1. 加载角色模型
2. 应用卡通材质
3. 设置光照参数
4. 实时渲染

**涉及模块**:
- 卡通 Shader
- 卡通脚本
- 光照系统

---

## 七、论文写作建议

### 7.1 第 7 章内容建议

**7.1 TA 系统架构**
- 介绍 TA 系统的定义和作用
- 说明 TA 系统的模块划分
- 阐述 TA 系统与其他系统的关系

**7.2 卡通渲染系统**
- 介绍卡通渲染的原理
- 说明 PlayerCartoon.shader 的实现
- 分析卡通渲染的性能特点

**7.3 溶解效果系统**
- 介绍溶解效果的原理
- 说明 Dissolve.shader 的实现
- 分析溶解效果的应用场景

**7.4 描边效果系统**
- 介绍描边效果的原理
- 说明 Custom_Outline.shader 的实现
- 分析描边效果的配置方案

**7.5 天空盒系统**
- 介绍天空盒的作用
- 说明天空盒的配置方式
- 分析天空盒的性能影响

**7.6 UI 特效系统**
- 介绍 UI 特效的类型
- 说明 UI 特效的实现方式
- 分析 UI 特效的性能优化

**7.7 性能优化与渲染管线**
- 介绍渲染管线的优化
- 说明 TA 系统的性能优化方案
- 分析优化效果

### 7.2 数据引用

可以引用以下数据：
- TA 模块数：5 个（卡通、溶解、描边、天空、UI）
- Shader 文件数：5+ 个
- 脚本模块数：4 个
- 配置资源数：6+ 个

### 7.3 图表建议

建议包含以下图表：
- TA 系统架构图
- Shader 渲染流程图
- 溶解效果演示图
- 描边效果对比图
- 性能优化对比图

---

## 八、技术细节参考

### 8.1 Shader 代码示例

**卡通渲染基本流程**:
```hlsl
// 计算基础颜色
float3 baseColor = tex2D(_MainTex, uv).rgb;

// 计算光照
float3 normal = normalize(i.normal);
float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
float diffuse = max(0, dot(normal, lightDir));

// 分段着色
diffuse = floor(diffuse * _Levels) / _Levels;

// 最终颜色
float3 finalColor = baseColor * diffuse;
```

**溶解效果基本流程**:
```hlsl
// 采样噪声
float noise = tex2D(_NoiseTex, uv).r;

// 比较阈值
if (noise < _DissolveThreshold)
    discard;

// 边缘发光
float edge = smoothstep(_DissolveThreshold - 0.1, _DissolveThreshold, noise);
float3 edgeColor = edge * _EdgeColor;

// 最终颜色
float3 finalColor = baseColor + edgeColor;
```

### 8.2 脚本代码示例

**溶解控制脚本**:
```csharp
public class DissolveController : MonoBehaviour
{
    private Material m_Material;
    private float m_DissolveProgress = 0f;
    
    public void StartDissolve(float duration)
    {
        // 开始溶解动画
        DOTween.To(() => m_DissolveProgress, 
            x => m_DissolveProgress = x, 
            1f, duration)
            .OnUpdate(() => UpdateDissolve());
    }
    
    private void UpdateDissolve()
    {
        m_Material.SetFloat("_DissolveThreshold", m_DissolveProgress);
    }
}
```

---

## 九、相关文档链接

### 项目知识库
- [项目知识库首页](../INDEX.md)
- [UI 系统](../UI系统/)
- [开发工具](../开发工具/)

### 开发文档
- [Week12_SceneAndTA.md](../../开发文档/周报/Week12_SceneAndTA.md)

---

**生成时间**: 2026-04-06  
**版本**: 1.0  
**位置**: `项目知识库（AI）/论文写作/TA系统分析.md`
