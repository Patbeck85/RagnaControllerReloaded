@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM RagnaController Benchmark Runner - Optimized Version
REM ============================================================
REM Diese Batch-Datei führt die Benchmarks im Release-Modus aus
REM mit automatischen Checks und Ergebnisanalyse.
REM 
REM Zielwerte (moderne CPU):
REM   ControllerSnapshot Build:  < 50 ns
REM   ComboEngine.Update:        < 100 ns
REM   MovementEngine.Update:     < 100 ns
REM   Messenger.Publish (10 sub): < 200 ns
REM ============================================================

echo ========================================
echo RagnaController Benchmark Runner v1.0
echo ========================================
echo.

REM Prüfen, ob .NET SDK installiert ist
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [FEHLER] .NET SDK nicht gefunden!
    echo Bitte installieren unter: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [OK] .NET SDK gefunden
echo.

REM Projektverzeichnis wechseln
cd /d "%~dp0"

REM Prüfen, ob das Benchmark-Projekt existiert
if not exist "RagnaController.Benchmarks.csproj" (
    echo [FEHLER] Benchmark-Projekt nicht gefunden!
    echo Bitte stelle sicher, dass du im RagnaController-Verzeichnis bist.
    pause
    exit /b 1
)

echo [OK] Benchmark-Projekt gefunden
echo.

REM Prüfen, ob Release-Ordner existiert
if not exist "bin\Release" (
    echo [INFO] Erstelle Release-Ordner...
    mkdir bin\Release >nul 2>&1
)

echo [OK] Release-Ordner vorhanden
echo.

REM Clean Build für Release-Modus
echo ========================================
echo Schritt 1: Clean Build (Release)...
echo ========================================
dotnet clean -c Release --verbosity quiet
if errorlevel 1 (
    echo [FEHLER] Clean Build fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Schritt 2: Restore NuGet-Pakete...
echo ========================================
dotnet restore -c Release --verbosity quiet
if errorlevel 1 (
    echo [FEHLER] Restore fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Schritt 3: Benchmarks ausfuehren...
echo ========================================
echo.

REM Benchmarks im Release-Modus ausfuehren mit Export
dotnet run -c Release --project RagnaController.Benchmarks.csproj ^
    --exporters:html,console,github ^
    --export-path:results ^
    --summary

if errorlevel 1 (
    echo [FEHLER] Benchmark-Ausfuehrung fehlgeschlagen!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Schritt 4: Ergebnisse analysieren...
echo ========================================
echo.

REM Ergebnisse in eine Textdatei schreiben
dotnet run -c Release --project RagnaController.Benchmarks.csproj ^
    --exporters:console ^
    --summary > benchmarks-summary.txt

echo [OK] Benchmarks abgeschlossen!
echo.

REM Zusammenfassung anzeigen
if exist "benchmarks-summary.txt" (
    echo ========================================
    echo Benchmark-Zusammenfassung
    echo ========================================
    type benchmarks-summary.txt
    echo.
)

echo ========================================
echo Ergebnisse gespeichert in:
echo ========================================
echo   - results\BenchmarkReport.html (HTML-Bericht)
echo   - results\BenchmarkDotNet.Artifacts\ (Grafiken)
echo   - benchmarks-summary.txt (Text-Zusammenfassung)
echo.

REM Prüfen, ob HTML-Bericht existiert
if exist "results\BenchmarkReport.html" (
    echo [INFO] Öffne den HTML-Bericht im Browser:
    echo         results\BenchmarkReport.html
    echo.
)

echo ========================================
echo Benchmark-Status: %ERRORLEVEL%
echo ========================================
echo.

REM Optional: Ergebnisse in Clipboard kopieren
set "COPY_TO_CLIPBOARD=0"
if "%~1"=="--copy" (
    set "COPY_TO_CLIPBOARD=1"
)

if "%COPY_TO_CLIPBOARD%"=="1" (
    echo [INFO] Ergebnisse werden in das Clipboard kopiert...
    type benchmarks-summary.txt | clip
)

echo.
echo ========================================
echo Benchmark Runner abgeschlossen!
echo ========================================
echo.
pause
