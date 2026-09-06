@echo off
setlocal enabledelayedexpansion
title RO Sprite Enhancer — Compiler

:: WICHTIG: Wechselt immer in den Ordner in dem DIESE .bat-Datei liegt —
:: unabhängig davon wie sie gestartet wird (Doppelklick, Verknüpfung,
:: "Als Administrator ausführen", aus einem anderen Arbeitsverzeichnis
:: heraus per Skript, etc.). Ohne das würde die spätere Prüfung auf
:: "RoSpriteEnhancer.csproj" fehlschlagen, obwohl beide Dateien im
:: selben Ordner liegen — %CD% ist nicht immer automatisch der
:: Ordner der .bat-Datei.
cd /d "%~dp0"

echo.
echo  =====================================================
echo    RO Sprite Enhancer — Build ^& Compiler Script
echo    Teil des Yggdrasil.Studio Oekosystems
echo  =====================================================
echo.

:: ─── VERSION ──────────────────────────────────────────────────────────────────
set VERSION=1.18.0
set EXE_NAME=RoSpriteEnhancer
set OUT_DIR=bin\Release\%EXE_NAME%_v%VERSION%
set ZIP_NAME=%EXE_NAME%_v%VERSION%.zip

:: ─── .NET SDK PRUEFEN ─────────────────────────────────────────────────────────
echo [1/5] Prüfe .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo  FEHLER: .NET SDK nicht gefunden!
    echo  Bitte installieren: https://dotnet.microsoft.com/download
    echo  Benoetigt: .NET 8 SDK oder hoeher
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set DOTNET_VER=%%v
echo  ✓ .NET SDK gefunden: v%DOTNET_VER%

:: ─── PROJEKTDATEI PRUEFEN ─────────────────────────────────────────────────────
echo [2/5] Prüfe Projektstruktur...
if not exist "%EXE_NAME%.csproj" (
    echo.
    echo  FEHLER: %EXE_NAME%.csproj nicht gefunden!
    echo  Bitte build.bat aus dem Projektordner starten.
    echo.
    pause
    exit /b 1
)
echo  ✓ Projektdatei gefunden

:: ─── ALTEN BUILD AUFRAUMEN ────────────────────────────────────────────────────
echo [3/5] Bereinige alten Build...
if exist "%OUT_DIR%" (
    rmdir /s /q "%OUT_DIR%"
    echo  ✓ Alter Build-Ordner geleert
) else (
    echo  ✓ Kein alter Build vorhanden
)

:: ─── KOMPILIEREN ──────────────────────────────────────────────────────────────
echo [4/5] Kompiliere Release...
echo.
echo  dotnet publish -c Release -r win-x86 --self-contained true
echo       -p:PublishSingleFile=true
echo       -p:IncludeNativeLibrariesForSelfExtract=true
echo       -p:DebugType=none -p:DebugSymbols=false
echo       -o %OUT_DIR%
echo.

dotnet publish -c Release -r win-x86 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -p:AssemblyVersion=%VERSION%.0 ^
    -p:FileVersion=%VERSION%.0 ^
    -o "%OUT_DIR%"

if %errorlevel% neq 0 (
    echo.
    echo  ╔══════════════════════════════════════╗
    echo  ║   BUILD FEHLGESCHLAGEN!              ║
    echo  ║   Fehler siehe Ausgabe oben.         ║
    echo  ╚══════════════════════════════════════╝
    echo.
    pause
    exit /b 1
)

:: ─── EXE GROESSE ANZEIGEN ─────────────────────────────────────────────────────
if exist "%OUT_DIR%\%EXE_NAME%.exe" (
    for %%f in ("%OUT_DIR%\%EXE_NAME%.exe") do (
        set /a SIZE_KB=%%~zf / 1024
        set /a SIZE_MB=%%~zf / 1048576
    )
    echo  ✓ EXE erstellt: %EXE_NAME%.exe ^(!SIZE_MB! MB^)
)

:: ─── README KOPIEREN ──────────────────────────────────────────────────────────
if exist "README.md" (
    copy /y "README.md" "%OUT_DIR%\README.md" >nul
    echo  ✓ README.md kopiert
)

:: ─── ZIP PACKEN (optional, braucht PowerShell 5+) ────────────────────────────
echo [5/5] Erstelle ZIP-Paket...
set ZIP_OUT=bin\Release\%ZIP_NAME%

if exist "%ZIP_OUT%" del /q "%ZIP_OUT%"

powershell -NoProfile -Command ^
    "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_OUT%' -Force" ^
    >nul 2>&1

if %errorlevel% equ 0 (
    for %%f in ("%ZIP_OUT%") do set /a ZIP_KB=%%~zf / 1024
    echo  ✓ ZIP erstellt: %ZIP_NAME% ^(!ZIP_KB! KB^)
) else (
    echo  ! ZIP-Erstellung fehlgeschlagen ^(PowerShell nicht verfuegbar?^)
    echo    EXE liegt direkt in: %OUT_DIR%\
)

:: ─── ERGEBNIS ─────────────────────────────────────────────────────────────────
echo.
echo  ╔══════════════════════════════════════════════════════╗
echo  ║   BUILD ERFOLGREICH  ✓                              ║
echo  ╠══════════════════════════════════════════════════════╣
echo  ║   Version  : %VERSION%                                   ║
echo  ║   EXE      : %OUT_DIR%\                ║
echo  ║   ZIP      : bin\Release\%ZIP_NAME%       ║
echo  ╚══════════════════════════════════════════════════════╝
echo.
echo  Starte mit:   %OUT_DIR%\%EXE_NAME%.exe
echo.

:: Optional: EXE direkt starten?
set /p START_NOW= EXE jetzt starten? (j/n): 
if /i "%START_NOW%"=="j" (
    start "" "%OUT_DIR%\%EXE_NAME%.exe"
)

endlocal
pause
