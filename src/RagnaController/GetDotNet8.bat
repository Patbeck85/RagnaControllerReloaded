@echo off
title .NET Runtime – Check

:: Check if .NET 8 OR NEWER (Windows Desktop Runtime) is installed
:: WPF requires Microsoft.WindowsDesktop.App, NOT Microsoft.NETCore.App!
set "FOUND="
for /f "tokens=1,2" %%A in ('dotnet --list-runtimes 2^>nul') do (
    if "%%A"=="Microsoft.WindowsDesktop.App" (
        for /f "tokens=1 delims=." %%M in ("%%B") do (
            if %%M GEQ 8 set "FOUND=1"
        )
    )
)

if defined FOUND (
    echo .NET Windows Desktop Runtime 8 or newer found.
    timeout /t 2 /nobreak >nul
    exit
)

:: ── Nicht gefunden – herunterladen ────────────────────────────────────────
:: Not found - download
echo Will be downloaded (~55 MB) ...
echo.
echo IMPORTANT: Please install "Windows Desktop Runtime", not just the Base Runtime!
echo.

powershell -NoProfile -Command ^
  "Invoke-WebRequest -Uri 'https://aka.ms/dotnet-8-windowsdesktop-x64' -OutFile '%TEMP%\dotnet8desktop.exe' -UseBasicParsing; ^
   Start-Process -FilePath '%TEMP%\dotnet8desktop.exe' -Wait"

echo.
echo Fertig. Du kannst RagnaController.exe jetzt starten.
pause
