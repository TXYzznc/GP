# DataTableGenerator 运行步骤

## 📋 当前状态

✅ **代码完成**：所有效果类、工厂、上下文类已创建  
✅ **配置表修改**：SpecialEffectTable.txt 已更新，新增 EffectParams 列和 26 个卡牌解锁效果  
⏳ **待完成**：运行 DataTableGenerator 生成新的代码

---

## 🚀 执行步骤

### Step 1: 打开 Unity 编辑器

确保项目已加载，没有编译错误。

### Step 2: 运行 DataTableGenerator

```
Unity 菜单 → GameFramework → DataTable → Generate
```

**预期过程**：
1. 编辑器会扫描 `Assets/AAAGame/DataTable/` 下的所有 `.txt` 文件
2. 检测到 `SpecialEffectTable.txt` 的变化
3. 解析新增的 `EffectParams` 列
4. 生成 `Assets/AAAGame/Scripts/DataTable/SpecialEffectTable.cs`（含新字段）
5. 生成 `Assets/AAAGame/DataTable/SpecialEffectTable.bytes`

### Step 3: 验证生成结果

运行完成后，检查以下文件是否已更新：

#### 文件 1: SpecialEffectTable.cs
```bash
# 检查是否新增了 EffectParams 属性
# 位置：Assets/AAAGame/Scripts/DataTable/SpecialEffectTable.cs
grep -n "EffectParams" SpecialEffectTable.cs
```

**期望输出**：应该包含类似以下内容的代码
```csharp
public string EffectParams
{
    get;
    private set;
}
```

#### 文件 2: SpecialEffectTable.bytes
```bash
# 检查文件是否存在且时间戳是最新的
ls -lh Assets/AAAGame/DataTable/SpecialEffectTable.bytes
```

### Step 4: 检查数据表是否正确加载

在 Unity Console 中运行以下测试脚本：

```csharp
// 临时测试代码，可在任何 MonoBehaviour 的 Start() 中运行
void TestSpecialEffectTable()
{
    var table = GF.DataTable.GetDataTable<SpecialEffectTable>();
    
    if (table == null)
    {
        Debug.LogError("特殊效果配置表加载失败！");
        return;
    }
    
    // 测试解锁神圣庇护效果
    var row = table.GetDataRow(2001);
    if (row != null)
    {
        Debug.Log($"✓ 效果 2001: {row.Name}");
        Debug.Log($"  EffectParams: {row.EffectParams}");
        
        // 验证参数格式
        var effectType = row.GetParamValue<string>("type", "");
        var cardId = row.GetParamValue<int>("cardId", 0);
        Debug.Log($"  解析后: type={effectType}, cardId={cardId}");
    }
    else
    {
        Debug.LogError("效果 2001 未找到！");
    }
    
    // 统计总数
    var allRows = table.GetAllDataRows();
    Debug.Log($"✓ 特殊效果总数: {allRows.Length}");
}
```

### Step 5: 确保工厂注册成功

检查 GameProcedure 是否正确调用了工厂初始化：

```csharp
// 在 GameProcedure.OnEnter() 中，应该看到以下日志：
// [ItemEffectFactory] 注册了 8 个物品效果
```

---

## ❌ 常见问题排查

### 问题 1: "EffectParams 属性不存在"
**原因**：DataTableGenerator 未成功更新代码  
**解决**：
1. 检查 SpecialEffectTable.txt 的格式（确保新增列在最后）
2. 删除旧的 SpecialEffectTable.cs 和 SpecialEffectTable.bytes
3. 重新运行 Generate

### 问题 2: "找不到 2001-2026 的效果"
**原因**：配置表未正确加载  
**解决**：
1. 检查 SpecialEffectTable.txt 是否包含这些数据行
2. 确保 .bytes 文件是最新生成的
3. 检查项目是否有配置表加载模块正确初始化 ItemManager

### 问题 3: "EffectParams 为 null 或空"
**原因**：配置表中该行的 EffectParams 列为空  
**解决**：
1. 检查 SpecialEffectTable.txt 中对应行是否有 JSON 数据
2. 确保 JSON 格式正确（无多余空格或特殊字符）
3. 重新生成 .bytes 文件

### 问题 4: 编译错误 "CS0103: 名称 'SpecialEffectTable' 不在当前上下文中"
**原因**：SpecialEffectTable 类未被正确生成  
**解决**：
1. 检查 SpecialEffectTable.cs 是否存在
2. 确保脚本关联到正确的程序集（应该是 Assembly-CSharp.csproj）
3. 尝试重新导入项目

---

## 📊 验证检查表

生成完成后，依次检查以下项目：

- [ ] SpecialEffectTable.cs 中包含 `EffectParams` 属性
- [ ] SpecialEffectTable.bytes 文件时间戳是最新的
- [ ] 配置表在 Unity 中正确加载（无警告）
- [ ] 能访问效果 2001-2026（使用 GetDataRow(id)）
- [ ] EffectParams 返回正确的 JSON 字符串
- [ ] ItemManager.ConvertToEffectData() 正确读取 EffectParams
- [ ] ItemEffectFactory.RegisterAll() 成功注册所有 8 种效果
- [ ] GameProcedure 启动时输出日志（确认工厂注册）

---

## 💡 下一步操作

DataTableGenerator 成功运行后：

1. **编译验证** - 确保项目无编译错误
2. **加载场景** - 进入游戏场景，检查 Console 日志
3. **功能测试** - 使用卡牌解锁物品进行测试
4. **版本控制** - 提交所有修改到 Git

---

**预期时间**：DataTableGenerator 运行通常需要 10-30 秒  
**联系**：如遇问题，参考本文档或检查 Console 错误日志

