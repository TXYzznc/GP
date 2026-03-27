@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ============================================================
echo 项目路径配置初始化工具
echo ============================================================
echo.

REM 获取当前脚本所在目录（.kiro目录）
set "KIRO_DIR=%~dp0"
set "KIRO_DIR=%KIRO_DIR:~0,-1%"

REM 获取项目根目录（.kiro的上级目录）
for %%i in ("%KIRO_DIR%") do set "PROJECT_ROOT=%%~dpi"
set "PROJECT_ROOT=%PROJECT_ROOT:~0,-1%"

echo 🔍 检测到的路径:
echo    项目根目录: %PROJECT_ROOT%
echo    .kiro目录: %KIRO_DIR%
echo.

REM 检查关键文件夹是否存在
set "MISSING_FOLDERS="
if not exist "%PROJECT_ROOT%\Assets" set "MISSING_FOLDERS=!MISSING_FOLDERS! Assets"
if not exist "%PROJECT_ROOT%\AI工作区" set "MISSING_FOLDERS=!MISSING_FOLDERS! AI工作区"
if not exist "%PROJECT_ROOT%\项目知识库（AI）" set "MISSING_FOLDERS=!MISSING_FOLDERS! 项目知识库（AI）"

if not "!MISSING_FOLDERS!"=="" (
    echo ⚠️  警告：以下必要文件夹不存在:!MISSING_FOLDERS!
    echo    请确认项目根目录是否正确
    echo.
)

REM 生成配置文件路径
set "CONFIG_FILE=%KIRO_DIR%\project_paths.json"

REM 获取当前时间戳
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YEAR=%dt:~0,4%"
set "MONTH=%dt:~4,2%"
set "DAY=%dt:~6,2%"
set "HOUR=%dt:~8,2%"
set "MINUTE=%dt:~10,2%"
set "SECOND=%dt:~12,2%"
set "TIMESTAMP=%YEAR%-%MONTH%-%DAY%T%HOUR%:%MINUTE%:%SECOND%"

REM 转换路径分隔符（Windows的\转为JSON的/）
set "JSON_PROJECT_ROOT=%PROJECT_ROOT:\=/%"
set "JSON_AI_WORKSPACE=%PROJECT_ROOT:\=/%/AI工作区"
set "JSON_ASSETS=%PROJECT_ROOT:\=/%/Assets"
set "JSON_DATA_TABLES=%PROJECT_ROOT:\=/%/Assets/AAAGame/DataTable"
set "JSON_GAME_DATA=%PROJECT_ROOT:\=/%/AAAGameData"
set "JSON_SCRIPTS=%PROJECT_ROOT:\=/%/Assets/AAAGame/Scripts"
set "JSON_AB_WORKING=%PROJECT_ROOT:\=/%/AB/Working"
set "JSON_KNOWLEDGE_BASE=%PROJECT_ROOT:\=/%/项目知识库（AI）"
set "JSON_KIRO_CONFIG=%PROJECT_ROOT:\=/%/.kiro"
set "JSON_KIRO_SKILLS=%PROJECT_ROOT:\=/%/.kiro/skills"

echo 📝 生成配置文件: %CONFIG_FILE%

REM 创建JSON配置文件
(
echo {
echo   "project_root": "%JSON_PROJECT_ROOT%",
echo   "paths": {
echo     "AI_workspace": "%JSON_AI_WORKSPACE%",
echo     "assets": "%JSON_ASSETS%",
echo     "data_tables": "%JSON_DATA_TABLES%",
echo     "game_data": "%JSON_GAME_DATA%",
echo     "scripts": "%JSON_SCRIPTS%",
echo     "ab_working": "%JSON_AB_WORKING%",
echo     "knowledge_base": "%JSON_KNOWLEDGE_BASE%",
echo     "kiro_config": "%JSON_KIRO_CONFIG%",
echo     "kiro_skills": "%JSON_KIRO_SKILLS%"
echo   },
echo   "last_updated": "%TIMESTAMP%",
echo   "version": "1.0",
echo   "description": "项目路径配置文件 - 提供快速路径查找，避免慢速文件夹遍历"
echo }
) > "%CONFIG_FILE%"

if exist "%CONFIG_FILE%" (
    echo ✅ 项目路径配置已生成: %CONFIG_FILE%
    echo.
    echo 📁 配置的路径:
    if exist "%PROJECT_ROOT%\AI工作区" (
        echo   AI_workspace         ✅ %JSON_AI_WORKSPACE%
    ) else (
        echo   AI_workspace         ❌ %JSON_AI_WORKSPACE%
    )
    if exist "%PROJECT_ROOT%\Assets" (
        echo   assets               ✅ %JSON_ASSETS%
    ) else (
        echo   assets               ❌ %JSON_ASSETS%
    )
    if exist "%PROJECT_ROOT%\Assets\AAAGame\DataTable" (
        echo   data_tables          ✅ %JSON_DATA_TABLES%
    ) else (
        echo   data_tables          ❌ %JSON_DATA_TABLES%
    )
    if exist "%PROJECT_ROOT%\AAAGameData" (
        echo   game_data            ✅ %JSON_GAME_DATA%
    ) else (
        echo   game_data            ❌ %JSON_GAME_DATA%
    )
    if exist "%PROJECT_ROOT%\Assets\AAAGame\Scripts" (
        echo   scripts              ✅ %JSON_SCRIPTS%
    ) else (
        echo   scripts              ❌ %JSON_SCRIPTS%
    )
    if exist "%PROJECT_ROOT%\AB\Working" (
        echo   ab_working           ✅ %JSON_AB_WORKING%
    ) else (
        echo   ab_working           ❌ %JSON_AB_WORKING%
    )
    if exist "%PROJECT_ROOT%\项目知识库（AI）" (
        echo   knowledge_base       ✅ %JSON_KNOWLEDGE_BASE%
    ) else (
        echo   knowledge_base       ❌ %JSON_KNOWLEDGE_BASE%
    )
    if exist "%PROJECT_ROOT%\.kiro" (
        echo   kiro_config          ✅ %JSON_KIRO_CONFIG%
    ) else (
        echo   kiro_config          ❌ %JSON_KIRO_CONFIG%
    )
    if exist "%PROJECT_ROOT%\.kiro\skills" (
        echo   kiro_skills          ✅ %JSON_KIRO_SKILLS%
    ) else (
        echo   kiro_skills          ❌ %JSON_KIRO_SKILLS%
    )
    echo.
    echo 🚀 配置完成！现在可以使用快速路径查找了
    echo    - 首次访问: ~5ms
    echo    - 后续访问: ~0.1ms ^(内存缓存^)
    echo.
    echo 💡 使用方法:
    echo    python path_utils.py  # 测试路径工具
    echo    python txt_converter.py  # 使用转换工具
) else (
    echo ❌ 配置文件生成失败
    exit /b 1
)

echo.
echo 按任意键退出...
pause >nul