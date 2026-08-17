@echo off
title RagnaController - Test Suite
cls

echo ========================================================
echo   RagnaController - Automatischer Testlauf
echo ========================================================
echo.
echo   Optionen:
echo     RUN_TESTS.bat           - Alle Tests
echo     RUN_TESTS.bat core      - Nur Core-Engine Tests (schnell)
echo     RUN_TESTS.bat vm        - Nur ViewModel Tests
echo     RUN_TESTS.bat io        - Nur IO/Roundtrip Tests
echo     RUN_TESTS.bat fast      - Alle ausser IO und Network
echo.

set FILTER=
if /I "%1"=="core"  set FILTER=--filter "Category=Core"
if /I "%1"=="vm"    set FILTER=--filter "Category=ViewModel"
if /I "%1"=="io"    set FILTER=--filter "Category=IO"
if /I "%1"=="fast"  set FILTER=--filter "Category!=IO&Category!=Network"

echo Starte Tests...
echo.

dotnet test tests\RagnaController.Tests\RagnaController.Tests.csproj --verbosity minimal %FILTER%

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    color 0A
    echo   ERFOLG: Alle Tests wurden fehlerfrei bestanden!
    echo ========================================================
) else (
    echo.
    echo ========================================================
    color 0C
    echo   FEHLER: Mindestens ein Test ist fehlgeschlagen!
    echo   Bitte scrolle nach oben, um die Mangel-Liste zu sehen.
    echo ========================================================
)

echo.
pause
