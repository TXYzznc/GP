> **最后更新**: 2026-03-23
> **状态**: 有效
---

# MCP TCP解决方案部署指南

## 📋 目录

- [概述](#概述)
- [问题背景](#问题背景)
- [架构对比](#架构对比)
- [部署步骤](#部署步骤)
- [验证部署](#验证部署)
- [文件清单](#文件清单)
- [优势](#优势)
- [故障排除](#故障排除)
- [性能考虑](#性能考虑)
- [监控和维护](#监控和维护)
- [总结](#总结)
- [更新日志](#更新日志)

---

## 问题背景

**原问题**: executePwsh工具的输出干扰了MCP服务器的JSON-RPC通信,导致解析错误:
```
ERROR: Invalid JSON: expected value at line 1 column 1
```

**解决方案**: 使用TCP端口进行MCP通信,完全隔离stdio通道。

[↑ 返回目录](#目录)

---

## 架构对比

### 原架构 (有问题)
```
Kiro IDE
  ├─ executePwsh → stdout → MCP Server (冲突!)
  └─ MCP Client → stdin/stdout → MCP Server
```

### 新架构 (TCP版本)
```
Kiro IDE
  ├─ executePwsh → 独立进程 (无冲突)
  └─ MCP Client → TCP:9000 → MCP Server
```

[↑ 返回目录](#目录)

---

## 部署步骤

### 自动部署 (推荐)

1. **运行部署脚本**
   ```bash
   cd .kiro/skills/txt-to-xlsx-converter/Tool
   deploy_tcp_solution.bat
   ```

2. **更新MCP配置**
   - 复制 `mcp_config_tcp.json` 的内容
   - 粘贴到 `~/.kiro/settings/mcp.json`
   - 重启Kiro IDE

### 手动部署

#### 步骤1: 启动TCP服务器

```bash
cd .kiro/skills/txt-to-xlsx-converter/Tool
python mcp_server_tcp.py
```

服务器启动后会显示:
```
[INFO] MCP Server started on localhost:9000
[INFO] Waiting for connections...
```

#### 步骤2: 测试连接

```bash
python tcp_client_test.py
```

预期输出:
```
[SUCCESS] 所有测试通过! TCP MCP服务器工作正常
```

#### 步骤3: 更新MCP配置

将以下配置添加到 `~/.kiro/settings/mcp.json`:

```json
{
  "mcpServers": {
    "txt-to-xlsx-converter-tcp": {
      "transport": {
        "type": "tcp",
        "host": "localhost",
        "port": 9000
      },
      "disabled": false,
      "autoApprove": [
        "convert_txt_to_xlsx",
        "convert_directory_to_xlsx",
        "validate_txt_format"
      ]
    }
  }
}
```

#### 步骤4: 重启Kiro IDE

重启后,新的TCP MCP服务器将生效。

[↑ 返回目录](#目录)

---

## 验证部署

### 1. 检查服务器状态

TCP服务器应该显示:
```
[INFO] MCP Server started on localhost:9000
[INFO] Client connected from ('127.0.0.1', xxxxx)
```

### 2. 测试工具调用

在Kiro IDE中测试:
```python
# 现在可以安全地混用这两个工具
executePwsh("Get-Location")  # 不会干扰MCP
mcp_txt_to_xlsx_converter_tcp_convert_txt_to_xlsx(...)  # 正常工作
```

### 3. 检查日志

- 无JSON解析错误
- 转换功能正常
- executePwsh工具正常

[↑ 返回目录](#目录)

---

## 文件清单

### 新增文件

```
.kiro/skills/txt-to-xlsx-converter/Tool/
├── mcp_server_tcp.py           # TCP版本MCP服务器
├── tcp_client_test.py          # TCP客户端测试工具
├── start_tcp_server.bat        # 服务器启动脚本
├── deploy_tcp_solution.bat     # 自动部署脚本
└── mcp_config_tcp.json         # TCP版本MCP配置
```

### 保留文件

```
.kiro/skills/txt-to-xlsx-converter/Tool/
├── mcp_server.py               # 原版本(备用)
├── mcp_server_fixed.py         # 修复编码版本(备用)
└── txt_to_xlsx_converter.py    # 核心转换逻辑(共用)
```

[↑ 返回目录](#目录)

---

## 优势

### 1. 完全隔离
- executePwsh使用独立进程
- MCP通信使用TCP端口
- 两者不共享任何IO通道

### 2. 稳定性提升
- 消除JSON解析冲突
- 支持并发连接
- 错误隔离更好

### 3. 扩展性
- 可以支持多个客户端
- 便于监控和调试
- 支持网络部署

### 4. 向后兼容
- 保留原有工具接口
- 配置简单切换
- 不影响其他MCP服务器

[↑ 返回目录](#目录)

---

## 故障排除

### 问题1: 端口被占用

**现象**: 
```
[ERROR] Server start failed: [Errno 10048] Only one usage of each socket address
```

**解决**: 
1. 检查端口9000是否被占用: `netstat -an | findstr 9000`
2. 修改 `mcp_server_tcp.py` 中的 `TCP_PORT` 为其他端口
3. 同步更新 `mcp_config_tcp.json` 中的端口号

### 问题2: 连接超时

**现象**: 
```
[ERROR] Connection failed: [Errno 10060] A connection attempt failed
```

**解决**:
1. 确认TCP服务器已启动
2. 检查防火墙设置
3. 确认端口号配置正确

### 问题3: 工具调用失败

**现象**: MCP工具无响应或报错

**解决**:
1. 检查TCP服务器日志
2. 验证文件路径是否正确(使用绝对路径)
3. 确认openpyxl等依赖已安装

[↑ 返回目录](#目录)

---

## 性能考虑

### TCP vs stdio

| 方面 | TCP | stdio |
|------|-----|-------|
| 延迟 | 稍高(~1ms) | 更低 |
| 稳定性 | 更好 | 易冲突 |
| 并发 | 支持 | 不支持 |
| 调试 | 容易 | 困难 |

### 优化建议

1. **连接复用**: 客户端保持长连接,避免频繁建立连接
2. **批量操作**: 合并多个小请求为批量请求
3. **异步处理**: 对于大文件转换,使用异步模式

[↑ 返回目录](#目录)

---

## 监控和维护

### 日志监控

TCP服务器会输出详细日志:
```
[INFO] MCP Server started on localhost:9000
[INFO] Client connected from ('127.0.0.1', 12345)
[INFO] Client ('127.0.0.1', 12345) disconnected
```

### 健康检查

定期运行测试脚本:
```bash
python tcp_client_test.py
```

### 服务器重启

如需重启服务器:
1. 按Ctrl+C停止当前服务器
2. 重新运行 `python mcp_server_tcp.py`

[↑ 返回目录](#目录)

---

## 总结

TCP解决方案彻底解决了executePwsh与MCP工具的冲突问题,提供了更稳定、可扩展的架构。部署简单,维护方便,是生产环境的推荐方案。

[↑ 返回目录](#目录)

---

## 更新日志

- 2026-03-05: 初始版本,实现TCP MCP服务器和完整部署方案

[↑ 返回目录](#目录)
