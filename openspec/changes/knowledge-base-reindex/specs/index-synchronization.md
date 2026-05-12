## ADDED Requirements

### Requirement: 更新INDEX.md索引
完成文件分类后，应更新INDEX.md以反映知识库的最新结构。

#### Scenario: 索引新增文档
- **WHEN** 新的重写或合并文档添加到wiki分类
- **THEN** 在INDEX.md的对应分类章节中添加新文档的条目

#### Scenario: 移除过时索引
- **WHEN** 某些过时文件被删除或合并
- **THEN** 从INDEX.md中删除对应的条目

#### Scenario: 更新分类总数
- **WHEN** 索引更新完成
- **THEN** 更新INDEX.md中的总文档数统计，反映最新的知识库规模

### Requirement: 保持INDEX.md的格式一致性
更新INDEX.md时应遵循现有的格式和结构。

#### Scenario: 查阅现有格式
- **WHEN** 开始更新INDEX.md
- **THEN** 阅读现有的INDEX.md，了解其格式结构（分类、条目格式、链接方式等）

#### Scenario: 一致的链接格式
- **WHEN** 添加新的索引条目
- **THEN** 使用与现有条目相同的链接格式和相对路径，确保链接有效

#### Scenario: 分类顺序保持
- **WHEN** 更新INDEX.md
- **THEN** 保持现有的分类顺序和层级结构，在相应分类下添加新条目

### Requirement: 建立完整的导航体系
INDEX.md应能完整反映知识库的全部文档，便于用户导航。

#### Scenario: 完整性检查
- **WHEN** INDEX.md更新完成
- **THEN** 检查wiki目录中的所有文档是否都在INDEX.md中有索引，避免遗漏

#### Scenario: 分类清晰
- **WHEN** 索引信息很多
- **THEN** 使用清晰的分类标题和缩进，帮助用户快速定位所需文档

### Requirement: 添加文档描述信息
INDEX.md中的索引条目应包含简要的文档描述。

#### Scenario: 条目描述
- **WHEN** 在INDEX.md中添加新的索引条目
- **THEN** 为该条目添加一行简要描述（1-2句），说明该文档的内容和用途

#### Scenario: 查询效率
- **WHEN** 用户查看INDEX.md
- **THEN** 通过条目描述能快速了解每个文档的功能，判断是否需要打开

### Requirement: 同步wiki的分类导航
确保wiki各分类下的导航文件与INDEX.md保持同步。

#### Scenario: 分类导航更新
- **WHEN** INDEX.md已更新
- **THEN** 检查每个wiki分类下的导航文件（如README或总结文件），更新其中的文档列表

#### Scenario: 双向链接
- **WHEN** 完成导航同步
- **THEN** 确保INDEX.md和各分类导航文件中的链接相互对应，用户可以从两个地方都能找到相同的文档

### Requirement: 创建知识库统计信息
在INDEX.md中添加知识库的统计信息，便于了解知识库规模。

#### Scenario: 统计数据
- **WHEN** 索引更新完成
- **THEN** 在INDEX.md顶部或专门章节添加统计信息：总文档数、各分类文档数、最后更新时间

#### Scenario: 更新标记
- **WHEN** 重索引完成
- **THEN** 在INDEX.md中标注本次重索引的完成时间和主要变化（删除过时文档数、合并文件数等）
