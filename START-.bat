@echo off
title RagnaController Compiler Tool
setlocal enabledelayedexpansion
color 0E

:MENU
cls
echo ===================================================
echo   RagnaController - Pro Build ^& Publish Tool
echo ===================================================
echo.
echo   1. Build Framework-Dependent (Small EXE, needs .NET 8)
echo   2. Build Self-Contained     (Large EXE, standalone)
echo   3. Build for Steam Deck     (Linux standalone)
echo.
echo   4. Deep Clean (Fixes "ghost" build errors)
echo   0. Exit
echo.
set /p choice="Select an option (0-4): "

if "%choice%"=="0" exit /b
if "%choice%"=="4" goto DEEPCLEAN

set PROJECT_PATH=src\RagnaController\RagnaController.csproj
set OUT_DIR=publish\

if "%choice%"=="1" set PUB_CMD=dotnet publish "%PROJECT_PATH%" -r win-x64 --no-self-contained -c Release -p:PublishSingleFile=true -o "%OUT_DIR%"
if "%choice%"=="2" set PUB_CMD=dotnet publish "%PROJECT_PATH%" -r win-x64 --self-contained true -c Release -p:PublishSingleFile=true -o "%OUT_DIR%"
if "%choice%"=="3" set PUB_CMD=dotnet publish "%PROJECT_PATH%" -r linux-x64 --self-contained true -c Release -p:PublishSingleFile=true -o "%OUT_DIR%linux"

:: ---------------------------------------------------------
:: STEP 1: PRE-BUILD CHECK (Errors Only)
:: ---------------------------------------------------------
cls
echo ===================================================
echo   STEP 1: Checking code for errors...
echo ===================================================
dotnet build "%PROJECT_PATH%" -c Release -nologo -clp:ErrorsOnly > build_errors.log 2>&1

if %errorlevel% neq 0 (
    color 0C
    echo.
    echo [X] COMPILATION FAILED!
    echo ---------------------------------------------------
    type build_errors.log
    echo ---------------------------------------------------
    echo All errors have also been saved to 'build_errors.log'.
    echo Fix the code, then try again.
    echo.
    pause
    color 0E
    goto MENU
)

:: ---------------------------------------------------------
:: STEP 2: PUBLISH
:: ---------------------------------------------------------
echo.
echo [V] Code is clean! No errors found.
echo.
echo ===================================================
echo   STEP 2: Generating EXE files...
echo   (This might take a few seconds)
echo ===================================================

%PUB_CMD% > publish_log.txt 2>&1

if %errorlevel% neq 0 (
    color 0C
    echo.
    echo [X] Publish failed!
    echo Please check 'publish_log.txt' for exact details.
    pause
    color 0E
    goto MENU
)

color 0A
echo.
echo ===================================================
echo   BUILD SUCCESSFUL!
echo   Your files are ready in: %OUT_DIR%
echo ===================================================
pause
color 0E
goto MENU

:DEEPCLEAN
cls
echo ===================================================
echo   Performing Deep Clean...
echo ===================================================
for /d /r . %%d in (bin,obj) do @if exist "%%d" (
    echo Deleting %%d
    rd /s /q "%%d" 2>nul
)
if exist "%OUT_DIR%" rd /s /q "%OUT_DIR%"
if exist "build_errors.log" del build_errors.log
if exist "publish_log.txt" del publish_log.txt
echo.
echo Clean complete!
pause
goto MENU
