@echo off
cd /d "%~dp0"
title RagnaController - Mutation Tests (Stryker.NET)
cls

echo ========================================================
echo   RagnaController - Mutation Test (Stryker.NET)
echo   Prueft ob Tests wirklich Fehler erkennen
echo ========================================================
echo.

:: Stryker.NET installieren falls noch nicht vorhanden
dotnet tool install --global dotnet-stryker >NUL 2>&1

echo Starte Mutation Testing (kann 3-10 Minuten dauern)...
echo.

dotnet stryker --config-file stryker-config.json

if %ERRORLEVEL% EQU 0 (
    color 0A
    echo.
    echo ========================================================
    echo   BESTANDEN: Mutation Score ueber Threshold!
    echo   Report: mutation-report\mutation-report.html
    echo ========================================================
) else (
    color 0C
    echo.
    echo ========================================================
    echo   ZU VIELE UEBERLEBENDE MUTANTEN!
    echo   Tests erkennen zu wenige Fehler - mehr Tests schreiben.
    echo   Report: mutation-report\mutation-report.html
    echo ========================================================
)

echo.
color 07
pause
