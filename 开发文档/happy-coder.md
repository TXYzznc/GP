# Happy Coder 使用指南

# Happy Coder 使用指南

Happy Coder 是基于 Claude Code 的 AI 编程助手，可在手机、平板或浏览器上远程控制本地 AI 编码代理，支持端到端加密。


---

## 🚀 自建中继服务器配置（必读）

公司内部统一使用自建中继 `https://sz-ai-relay.galasports.com`，流量走内网，速度更快。

### 网络架构对比

官方云中转：

```
开发机（happy CLI）→ Happy 云服务器 → 手机 App
```

自建（推荐）：

```
开发机（happy CLI）→ sz-ai-relay.galasports.com → 手机 App
```

| 方式  | 入口  |
|-----|-----|
| 官方  | https://app.happy.engineering |
| 自建（推荐） | https://sz-ai-relay.galasports.com |

### 第一步：CLI 端配置（开发机）

```bash
# zsh
echo 'export HAPPY_SERVER_URL="https://sz-ai-relay.galasports.com"' >> ~/.zshrc
source ~/.zshrc

# bash
echo 'export HAPPY_SERVER_URL="https://sz-ai-relay.galasports.com"' >> ~/.bashrc
source ~/.bashrc
```

Windows PowerShell：

```powershell
[System.Environment]::SetEnvironmentVariable("HAPPY_SERVER_URL","https://sz-ai-relay.galasports.com","User")
```

### 第二步：手机 App 配置

打开 Happy App → 进入「设置」页 → 点击右上角的 **数据库图标**（如下图红圈位置）→ 选择「手动输入 URL」→ 填入 `https://sz-ai-relay.galasports.com` → 保存。

\n

 ![设置页右上角数据库图标即服务器切换入口](http://oss.wckj.com/mcp-script/happy-coder/mobile-server-url-entry.jpg)

> 提示：新版 App 没有「中继服务器 URL」这个字面选项，入口就是设置页右上角的数据库图标。

> 账户管理：点进账户 → 拉到最后 → 登出

 ![账户设置界面 - 登出位置](http://oss.wckj.com/platform-backend/happy-coder/account-settings.jpg)

配置完成后，`happy --auth` 和 `happy` 命令照常使用，流量走自建服务器。

### 备用方案：直接用浏览器

不想装 App 可以直接用手机浏览器访问 `https://sz-ai-relay.galasports.com`，体验与 App 一致，支持「添加到主屏幕」当 App 使用。


---

## 一、Windows 端

### 前提条件

* Node.js 18+（[官网下载](https://nodejs.org)）
* 已注册 Happy 账号

### 安装步骤


1. **安装 Happy CLI**

   ```powershell
   npm install -g happy
   ```
2. **扫码认证** 运行以下命令，会显示二维码：

   ```powershell
   happy auth login
   ```

   用手机 Happy App 扫码，即完成设备绑定。

   ![CLI 认证界面](https://happy.engineering/img/docs/cli-auth-example.png)
3. **启动会话** 在项目目录下运行：

   ```powershell
   happy
   ```

   手机端即可看到该会话，开始远程控制。


---

## 二、Linux 端

### 前提条件

* Node.js 18+（推荐用 `nvm` 安装）

### 安装步骤


1. **安装 Happy CLI**

   ```bash
   npm install -g happy
   ```
2. **扫码认证**

   ```bash
   happy auth login
   ```

   终端显示二维码后，用手机 Happy App 扫码完成绑定。

   ![CLI 认证界面](https://happy.engineering/img/docs/cli-auth-example.png)
3. **启动会话**

   ```bash
   cd 你的项目目录
   happy
   ```


---

## 三、别机（其他电脑/服务器）

适用于远程服务器、云主机、公司内网其他机器。

### 步骤


1. **安装 Happy CLI 并认证**（同 Linux 步骤）
2. **SSH 远程使用**

   ```bash
   ssh user@服务器IP
   cd 项目目录
   happy
   ```
3. **保持会话（推荐）** 使用 `tmux` 防止断线丢失进度：

   ```bash
   tmux new -s happy
   happy
   # Ctrl+B, D 挂起；tmux attach -t happy 恢复
   ```


---

## 四、手机端

无需安装 CLI，直接用手机控制已启动的 Happy 会话。

### 4.1 安装 Happy App

| 平台  | 下载方式 |
|-----|------|
| iOS | [App Store 搜索 Happy Coder](https://apps.apple.com/search?term=happy+coder) |
| Android | [Google Play 搜索 Happy Coder](https://play.google.com/store/search?q=happy+coder) |
| 浏览器 | 直接访问 [app.happy.engineering](https://app.happy.engineering) |

### 4.2 演示视频

**👉** [**点击播放手机端演示视频**](http://oss.wckj.com/mcp-script/happy-coder/mobile-demo.mp4)

### 4.3 连接会话

打开 Happy App 登录后，首页「终端」标签显示所有活跃会话，点击即可查看进度、发送指令。

 ![手机会话列表](http://oss.wckj.com/mcp-script/happy-coder/mobile-session-list.jpg)

### 4.4 查看代码变更

会话中可实时看到 AI 正在修改的代码 diff，红绿高亮展示增删内容。

 ![查看代码 diff](http://oss.wckj.com/mcp-script/happy-coder/mobile-code-diff.jpg)

### 4.5 查看执行过程

AI 执行命令、读写文件的全过程都会实时同步到手机端。

 ![查看执行过程](http://oss.wckj.com/mcp-script/happy-coder/mobile-execution.jpg)

### 4.6 添加到主屏幕（可选）

* **iOS**：Safari → 分享 → 「添加到主屏幕」
* **Android**：Chrome → 菜单 → 「添加到主屏幕」


---

## 快速参考

| 平台  | 方式  | 入口  |
|-----|-----|-----|
| Windows / Linux | CLI | `npm install -g happy` → `happy --auth` → `happy` |
| 别机/服务器 | SSH + CLI | SSH 后运行 `happy` |
| 手机 App | 官方  | App Store / Google Play |
| 手机 Web（官方） | 浏览器 | https://app.happy.engineering |
| 手机 Web（自建） | 浏览器 | https://sz-ai-relay.galasports.com |


---

## 五、AI 使用心得

### 方法

**1. 所有工作在聊天窗口内完成**

不要跳出 AI 会话去手动操作。查资料、改代码、运行测试、提交 GitLab、更新任务状态——所有步骤都让 AI 在同一个聊天窗口内完成，保持上下文连贯，减少人工切换。

**2. Plan 优先，拉长任务**

花足够时间写 Plan，过程中同步沉淀文档。不要急着让 AI 写代码——单次任务的目标是至少跑半小时以上，任务越完整，AI 的价值越高。

**3. 一次会话跑完全流程**

让 AI 在一次会话里完成从拉需求到交付的完整链路：

* 拉需求 → 写 Plan → 写代码 → 测试 → 联调
* **后端**：含跨应用联调、中间数据验证
* **前端**：用自动化测试框架拉起页面测试、打包、塞进模拟器
* 提交 GitLab、更新任务状态，全部用 MCP 由 AI 完成

**4. 手机做零散确认**

任务拉长后，不需要守在电脑前。用手机随时查看进度、做关键确认，让长期跑的任务持续往前推进。


---

### 目标

**个人目标**

* 尽快熟悉 AI 使用方式，解构原先自己的工作方式，重构新的工作方式
* 并行跑多个 Claude 会话：普通开发 3 个，Leader / 核心开发 5\~10 个

**团队目标**

* Leader 解构现有流程，重新整理出适合 AI 的开发流程
* 持续提升团队个人素养