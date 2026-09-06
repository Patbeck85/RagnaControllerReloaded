@echo off
cd /d "%~dp0"
title RagnaController v2.0.0 - Build ^& Publish Tool
setlocal enabledelayedexpansion
color 0E

set VERSION=2.0.0
set PROJECT_PATH=src\RagnaController\RagnaController.csproj
set OUT_DIR=publish\

:MENU
cls
echo.
echo  ########################################
echo  ##   RagnaController v%VERSION%       ##
echo  ##   Build ^& Publish Tool             ##
echo  ########################################
echo.
echo  [1]  Build  --  Framework-Dependent   (small EXE, braucht .NET 8 auf dem PC)
echo  [2]  Build  --  Self-Contained        (grosses EXE, laeuft ohne .NET 8)
echo  [3]  Build  --  ROG Ally / Legion Go  (win-x64 Self-Contained, Handheld-Mode)
echo.
echo  [4]  Deep Clean  (loescht bin/obj/publish, behebt Ghost-Build-Fehler)
echo  [5]  .NET 8 installieren              (oeffnet Installer)
echo  [6]  publish\ Ordner oeffnen
echo.
echo  [0]  Beenden
echo.
set /p choice="  Auswahl (0-6): "

if "%choice%"=="0" exit /b
if "%choice%"=="4" goto DEEPCLEAN
if "%choice%"=="5" goto INSTALL_DOTNET
if "%choice%"=="6" goto OPEN_OUTPUT

:: ── Publish-Kommando zusammenbauen ──────────────────────────────────────────
if "%choice%"=="1" (
    set PUB_CMD=dotnet publish "%PROJECT_PATH%" -r win-x64 --no-self-contained -c Release -p:PublishSingleFile=true -p:AssemblyVersion=%VERSION%.0 -p:FileVersion=%VERSION%.0 -o "%OUT_DIR%"
)
if "%choice%"=="2" (
    set PUB_CMD=dotnet publish "%PROJECT_PATH%" -r win-x64 --self-contained true -c Release -p:PublishSingleFile=true -p:AssemblyVersion=%VERSION%.0 -p:FileVersion=%VERSION%.0 -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUT_DIR%"
)
if "%choice%"=="3" (
    set PUB_CMD=dotnet publish "%PROJECT_PATH%" -r win-x64 --self-contained true -c Release -p:PublishSingleFile=true -p:AssemblyVersion=%VERSION%.0 -p:FileVersion=%VERSION%.0 -p:IncludeNativeLibrariesForSelfExtract=true -p:DefineConstants=HANDHELD_BUILD -o "%OUT_DIR%"
)

if not defined PUB_CMD (
    echo  Ungueltige Auswahl.
    pause
    goto MENU
)

:: ── STEP 1: Build-Check (nur Fehler) ───────────────────────────────────────
cls
echo.
echo  ########################################
echo  ##  STEP 1/2: Code-Check...           ##
echo  ########################################
echo.
dotnet build "%PROJECT_PATH%" -c Release -nologo -clp:ErrorsOnly > build_errors.log 2>&1

if %errorlevel% neq 0 (
    color 0C
    echo.
    echo  [X] KOMPILIERUNG FEHLGESCHLAGEN!
    echo  -------------------------------------------
    type build_errors.log
    echo  -------------------------------------------
    echo  Alle Fehler wurden in 'build_errors.log' gespeichert.
    echo  Fehler beheben und erneut versuchen.
    echo.
    pause
    color 0E
    goto MENU
)

echo  [OK] Kein Fehler gefunden!

:: ── STEP 2: Publish ────────────────────────────────────────────────────────
echo.
echo  ########################################
echo  ##  STEP 2/2: EXE wird erstellt...    ##
echo  ########################################
echo.

%PUB_CMD% > publish_log.txt 2>&1

if %errorlevel% neq 0 (
    color 0C
    echo.
    echo  [X] Publish fehlgeschlagen!
    echo  Bitte 'publish_log.txt' pruefen.
    echo.
    type publish_log.txt
    pause
    color 0E
    goto MENU
)

:: ── SDL2.dll pruefen ────────────────────────────────────────────────────────
if not exist "%OUT_DIR%SDL2.dll" (
    echo.
    echo  [!] WARNUNG: SDL2.dll fehlt im publish-Ordner!
    echo      Controller-Erkennung wird nicht funktionieren.
    echo      SDL2.dll aus dem NuGet-Cache kopieren oder manuell hinzufuegen.
    echo.
)

:: ── crash.log loeschen (frischer Start) ─────────────────────────────────────
if exist "%OUT_DIR%crash.log" del "%OUT_DIR%crash.log"

color 0A
echo.
echo  ########################################
echo  ##  BUILD ERFOLGREICH! v%VERSION%     ##
echo  ##  Dateien in: %OUT_DIR%             ##
echo  ########################################
echo.
pause
color 0E
goto MENU

:: ── Deep Clean ───────────────────────────────────────────────────────────────
:DEEPCLEAN
cls
echo.
echo  ########################################
echo  ##  Deep Clean...                     ##
echo  ########################################
echo.
for /d /r . %%d in (bin,obj) do @if exist "%%d" (
    echo  Loesche %%d
    rd /s /q "%%d" 2>nul
)
if exist "%OUT_DIR%" (
    echo  Loesche %OUT_DIR%
    rd /s /q "%OUT_DIR%"
)
if exist "build_errors.log" del "build_errors.log"
if exist "publish_log.txt"  del "publish_log.txt"
echo.
echo  [OK] Clean abgeschlossen!
pause
goto MENU

:: ── .NET 8 installieren ──────────────────────────────────────────────────────
:INSTALL_DOTNET
start "" "src\RagnaController\GetDotNet8.bat"
goto MENU

:: ── Output-Ordner oeffnen ────────────────────────────────────────────────────
:OPEN_OUTPUT
if exist "%OUT_DIR%" (
    explorer "%OUT_DIR%"
) else (
    echo  Ordner '%OUT_DIR%' existiert noch nicht. Erst bauen!
    pause
)
goto MENU
