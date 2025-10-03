@echo off
REM PLC API Sender - Windows Installation Launcher
REM This batch file launches the PowerShell installation script

echo ========================================
echo    PLC API Sender - Windows Installer
echo ========================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: PowerShell is not found on this system.
    echo Please install PowerShell to continue.
    pause
    exit /b 1
)

REM Run the PowerShell installation script
echo Starting installation...
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0install-windows.ps1"

if %errorlevel% neq 0 (
    echo.
    echo Installation failed with error code %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo Installation process completed.
pause
