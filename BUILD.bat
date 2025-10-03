@echo off
REM PLC API Sender - Quick Build Script for Windows

echo ========================================
echo    PLC API Sender - Building...
echo ========================================
echo.

REM Create icon first
echo Creating application icon...
powershell -ExecutionPolicy Bypass -File "%~dp0create-icon.ps1"

echo.
echo Building Windows executable...
echo.

REM Build self-contained single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

if %errorlevel% neq 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo    Build completed successfully!
echo ========================================
echo.
echo Executable location:
echo %~dp0bin\Release\net8.0\win-x64\publish\UbuntuPlcApiSender.exe
echo.

pause
