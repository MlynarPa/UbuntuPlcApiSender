# PLC API Sender - Windows Installation Script
# This script builds the application and creates desktop shortcuts

param(
    [string]$InstallPath = "$env:ProgramFiles\PlcApiSender",
    [switch]$CreateDesktopShortcut = $true,
    [switch]$CreateStartMenuShortcut = $true
)

Write-Host "=== PLC API Sender - Windows Installer ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Warning: Not running as administrator. Installation will proceed to user directory." -ForegroundColor Yellow
    $InstallPath = "$env:LOCALAPPDATA\PlcApiSender"
}

# Create icon if it doesn't exist
$iconPath = Join-Path $PSScriptRoot "app.ico"
if (-not (Test-Path $iconPath)) {
    Write-Host "Creating application icon..." -ForegroundColor Yellow
    $createIconScript = Join-Path $PSScriptRoot "create-icon.ps1"
    if (Test-Path $createIconScript) {
        & $createIconScript
    }
}

# Build the application
Write-Host "Building application for Windows..." -ForegroundColor Yellow
$publishPath = Join-Path $PSScriptRoot "bin\Release\net8.0\win-x64\publish"

# Clean previous build
if (Test-Path $publishPath) {
    Remove-Item -Path $publishPath -Recurse -Force
}

# Build self-contained executable
$buildResult = dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create installation directory
Write-Host "Creating installation directory: $InstallPath" -ForegroundColor Yellow
if (-not (Test-Path $InstallPath)) {
    New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
}

# Copy files to installation directory
Write-Host "Copying files to installation directory..." -ForegroundColor Yellow
$exePath = Join-Path $publishPath "UbuntuPlcApiSender.exe"
$configPath = Join-Path $PSScriptRoot "config.json"

if (Test-Path $exePath) {
    Copy-Item -Path $exePath -Destination $InstallPath -Force
    Write-Host "  - Copied executable" -ForegroundColor Green
}

if (Test-Path $configPath) {
    Copy-Item -Path $configPath -Destination $InstallPath -Force
    Write-Host "  - Copied config.json" -ForegroundColor Green
}

if (Test-Path $iconPath) {
    Copy-Item -Path $iconPath -Destination $InstallPath -Force
    Write-Host "  - Copied icon" -ForegroundColor Green
}

# Create shortcuts
$WshShell = New-Object -ComObject WScript.Shell
$installedExePath = Join-Path $InstallPath "UbuntuPlcApiSender.exe"
$installedIconPath = Join-Path $InstallPath "app.ico"

# Desktop shortcut
if ($CreateDesktopShortcut) {
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "PLC API Sender.lnk"

    Write-Host "Creating desktop shortcut..." -ForegroundColor Yellow
    $shortcut = $WshShell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $installedExePath
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "PLC API Sender Application"
    if (Test-Path $installedIconPath) {
        $shortcut.IconLocation = $installedIconPath
    }
    $shortcut.Save()
    Write-Host "  - Desktop shortcut created" -ForegroundColor Green
}

# Start Menu shortcut
if ($CreateStartMenuShortcut) {
    $startMenuPath = [Environment]::GetFolderPath("StartMenu")
    $programsPath = Join-Path $startMenuPath "Programs"
    $shortcutPath = Join-Path $programsPath "PLC API Sender.lnk"

    Write-Host "Creating Start Menu shortcut..." -ForegroundColor Yellow
    $shortcut = $WshShell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $installedExePath
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "PLC API Sender Application"
    if (Test-Path $installedIconPath) {
        $shortcut.IconLocation = $installedIconPath
    }
    $shortcut.Save()
    Write-Host "  - Start Menu shortcut created" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Installation directory: $InstallPath" -ForegroundColor Cyan
Write-Host "Executable: $installedExePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now run the application by:" -ForegroundColor Yellow
Write-Host "  - Double-clicking the desktop shortcut" -ForegroundColor White
Write-Host "  - Searching for 'PLC API Sender' in Start Menu" -ForegroundColor White
Write-Host "  - Running: $installedExePath" -ForegroundColor White
Write-Host ""

# Ask if user wants to run the application now
$runNow = Read-Host "Do you want to run the application now? (Y/N)"
if ($runNow -eq 'Y' -or $runNow -eq 'y') {
    Start-Process $installedExePath -WorkingDirectory $InstallPath
}
