@echo off
chcp 65001 >nul
REM TXT to XLSX Converter GUI 启动脚本
REM 支持双击运行

setlocal enabledelayedexpansion

REM 获取脚本所在目录
set SCRIPT_DIR=%~dp0

REM 检查 Python 是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo [错误] 未找到 Python
    echo.
    echo 请确保已安装 Python 3.7 或更高版本
    echo 下载地址: https://www.python.org/downloads/
    echo.
    pause
    exit /b 1
)

REM 检查 PyQt6 是否安装
python -c "import PyQt6" >nul 2>&1
if errorlevel 1 (
    echo.
    echo [信息] 检测到缺少 PyQt6 库，正在自动安装...
    echo.
    
    REM 升级 pip
    python -m pip install --upgrade pip >nul 2>&1
    
    REM 安装 PyQt6
    python -m pip install PyQt6 -i https://pypi.org/simple/
    
    if errorlevel 1 (
        echo.
        echo [错误] PyQt6 安装失败
        echo.
        echo 请尝试手动安装:
        echo    python -m pip install PyQt6
        echo.
        pause
        exit /b 1
    )
    
    echo.
    echo [成功] PyQt6 安装完成
    echo.
)

REM 启动 GUI
echo [信息] 启动 TXT to XLSX 转换工具...
echo.

python "%SCRIPT_DIR%txt_to_xlsx_converter_gui.py"

if errorlevel 1 (
    echo.
    echo [错误] 程序运行出错，请检查错误信息
    echo.
    pause
)

endlocal
