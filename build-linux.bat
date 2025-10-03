@echo off
REM Build script pro Linux deployment (self-contained) - Windows verze

echo === Building Ubuntu PLC API Sender for Linux ===

REM Clean previous build
echo Cistim predchozi build...
if exist publish-linux rmdir /s /q publish-linux

REM Build self-contained for linux-arm64 (Raspberry Pi)
echo Sestavuji aplikaci pro Linux ARM64 (Raspberry Pi, self-contained)...
dotnet publish -c Release -r linux-arm64 --self-contained true -o ./publish-linux

if %ERRORLEVEL% EQU 0 (
    echo ✅ Build uspesny!
    echo.
    echo Soubory jsou v: ./publish-linux
    echo.
    echo Dalsi kroky:
    echo 1. Zkopiruj slozku publish-linux/ na Ubuntu/Raspberry Pi
    echo 2. Spust: sudo ./install-linux-service.sh
    echo.
    echo Tip: Muzes pouzit SCP nebo GitHub:
    echo   scp -r ./publish-linux/* user@raspberry-pi:/home/user/ubuntu-plc-sender/
    echo   NEBO commitnout publish-linux/ do GitHubu a pulnout na Pi
) else (
    echo ❌ Build selhal!
    exit /b 1
)
