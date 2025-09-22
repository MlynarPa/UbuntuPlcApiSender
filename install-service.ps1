# PowerShell script pro instalaci jako Windows Service
# Spustit jako Administrator

param(
    [string]$ServiceName = "UbuntuPlcApiSender",
    [string]$ServiceDisplayName = "Ubuntu PLC API Sender",
    [string]$ServiceDescription = "Služba pro čtení dat z PLC a odesílání na API",
    [string]$ExePath = ""
)

# Automaticky najít exe soubor pokud není zadán
if ([string]::IsNullOrEmpty($ExePath)) {
    $ExePath = Get-ChildItem -Path "." -Name "UbuntuPlcApiSender.exe" -Recurse | Select-Object -First 1
    if ($ExePath) {
        $ExePath = Resolve-Path $ExePath
        Write-Host "Nalezen exe soubor: $ExePath"
    } else {
        Write-Error "Nenalezen UbuntuPlcApiSender.exe. Spusťte 'dotnet publish -c Release' nejdříve."
        exit 1
    }
}

# Zkontrolovat že exe existuje
if (-not (Test-Path $ExePath)) {
    Write-Error "Soubor $ExePath neexistuje!"
    exit 1
}

Write-Host "=== Instalace Windows Service ==="
Write-Host "Název služby: $ServiceName"
Write-Host "Zobrazený název: $ServiceDisplayName"
Write-Host "Popis: $ServiceDescription"
Write-Host "Cesta k exe: $ExePath"
Write-Host ""

try {
    # Zastavit službu pokud běží
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Zastavuji existující službu..."
        Stop-Service -Name $ServiceName -Force
        
        Write-Host "Odstraňuji existující službu..."
        sc.exe delete $ServiceName
        Start-Sleep -Seconds 2
    }

    # Vytvořit novou službu
    Write-Host "Vytvářím novou službu..."
    New-Service -Name $ServiceName -BinaryPathName $ExePath -DisplayName $ServiceDisplayName -Description $ServiceDescription -StartupType Automatic

    # Nastavit restart policy
    Write-Host "Nastavuji restart policy..."
    sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000

    # Spustit službu
    Write-Host "Spouštím službu..."
    Start-Service -Name $ServiceName

    # Zobrazit stav
    $service = Get-Service -Name $ServiceName
    Write-Host ""
    Write-Host "✅ Služba úspěšně nainstalována!"
    Write-Host "Stav: $($service.Status)"
    Write-Host ""
    Write-Host "Pro správu služby použijte:"
    Write-Host "  Start-Service -Name $ServiceName"
    Write-Host "  Stop-Service -Name $ServiceName"
    Write-Host "  Restart-Service -Name $ServiceName"
    Write-Host "  Get-Service -Name $ServiceName"
    Write-Host ""
    Write-Host "Pro odinstalaci:"
    Write-Host "  Stop-Service -Name $ServiceName"
    Write-Host "  sc.exe delete $ServiceName"

} catch {
    Write-Error "Chyba při instalaci služby: $($_.Exception.Message)"
    exit 1
}
