# Graphviz 便携版安装脚本（无需管理员权限）

param(
    [string]$InstallPath = "$env:LocalAppData\graphviz"
)

Write-Host "[INFO] Graphviz 便携版安装程序" -ForegroundColor Cyan
Write-Host "[PATH] 安装目录: $InstallPath" -ForegroundColor Yellow

# 创建安装目录
if (!(Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Host "[OK] 目录已创建"
}

# 尝试多个下载源
$urls = @(
    "https://github.com/graphviz/graphviz/releases/download/stable_release_14.1.4/windows_10_cmake_Release_x64.zip",
    "https://github.com/graphviz/graphviz/releases/download/14.1.3/windows_10_cmake_Release_x64.zip"
)

$downloaded = $false
$zipFile = "$env:TEMP\graphviz.zip"

foreach ($url in $urls) {
    Write-Host "[DOWNLOAD] 尝试: $url"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $url -OutFile $zipFile -ErrorAction Stop
        $fileSize = (Get-Item $zipFile).Length / 1MB
        Write-Host "[OK] 下载成功 ({0:F1} MB)" -f $fileSize
        $downloaded = $true
        break
    } catch {
        Write-Host "[SKIP] 下载失败: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

if (!$downloaded) {
    Write-Host "[ERROR] 所有下载源都失败，请检查网络连接" -ForegroundColor Red
    Write-Host ""
    Write-Host "替代方案:"
    Write-Host "1. 访问 https://graphviz.org/download/windows/"
    Write-Host "2. 下载 Windows x64 版本（MSI or ZIP）"
    Write-Host "3. 解压或安装到: $InstallPath"
    exit 1
}

# 解压文件
Write-Host "[EXTRACT] 正在解压文件..."
try {
    Expand-Archive -Path $zipFile -DestinationPath $InstallPath -Force
    Write-Host "[OK] 解压完成"
} catch {
    Write-Host "[ERROR] 解压失败: $_" -ForegroundColor Red
    exit 1
}

# 寻找 dot.exe
Write-Host "[SEARCH] 寻找 dot.exe..."
$dotExe = Get-ChildItem -Path $InstallPath -Recurse -Name "dot.exe" | Select-Object -First 1
if ($dotExe) {
    Write-Host "[FOUND] 找到 dot.exe: $dotExe"
} else {
    Write-Host "[WARNING] 找不到 dot.exe，可能解压有问题" -ForegroundColor Yellow
}

# 添加到 PATH
Write-Host "[PATH] 添加到用户 PATH..."
$binPath = Join-Path $InstallPath "bin"
if (Test-Path $binPath) {
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($currentPath -notlike "*$binPath*") {
        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$binPath", "User")
        Write-Host "[OK] PATH 已更新"
    } else {
        Write-Host "[OK] 已在 PATH 中"
    }
}

# 验证安装
Write-Host "[VERIFY] 验证安装..."
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
$dotPath = Get-Command dot -ErrorAction SilentlyContinue
if ($dotPath) {
    Write-Host "[SUCCESS] dot 命令可用!" -ForegroundColor Green
    Write-Host "[PATH] $($dotPath.Source)"
    dot -V
} else {
    Write-Host "[WARNING] dot 命令还不可用，请重启 PowerShell 或命令行" -ForegroundColor Yellow
    Write-Host "[INFO] 手动设置环境变量: PATH=$binPath" -ForegroundColor Cyan
}

# 清理
Remove-Item $zipFile -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "[DONE] 安装完成！" -ForegroundColor Green
Write-Host ""
Write-Host "下一步："
Write-Host "1. 重启 PowerShell/命令行以刷新 PATH"
Write-Host "2. 运行: cd '$((Get-Location).Path)' && python render_graphviz_online.py"
