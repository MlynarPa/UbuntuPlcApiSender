# PLC API Sender - Instalace pro Windows

Tento návod popisuje, jak nainstalovat a spustit aplikaci PLC API Sender na Windows.

## Rychlá instalace

### Metoda 1: Automatická instalace (doporučeno)

1. **Spusťte instalační soubor**
   - Poklepejte na soubor `INSTALL.bat`
   - Instalace vytvoří ikonu na ploše a v Start Menu

2. **Spusťte aplikaci**
   - Poklepejte na ikonu "PLC API Sender" na ploše
   - Nebo najděte "PLC API Sender" v Start Menu

### Metoda 2: Pouze build (bez instalace)

1. **Spusťte build**
   - Poklepejte na soubor `BUILD.bat`
   - Executable bude v `bin\Release\net8.0\win-x64\publish\`

2. **Spusťte aplikaci**
   - Spusťte `bin\Release\net8.0\win-x64\publish\UbuntuPlcApiSender.exe`

## Ruční instalace

Pokud preferujete ruční instalaci, postupujte takto:

### 1. Vytvoření ikony aplikace

```powershell
powershell -ExecutionPolicy Bypass -File create-icon.ps1
```

### 2. Build aplikace

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

### 3. Instalace

```powershell
powershell -ExecutionPolicy Bypass -File install-windows.ps1
```

**Volitelné parametry:**
- `-InstallPath "C:\MojeCesta"` - vlastní instalační cesta
- `-CreateDesktopShortcut $false` - nevytvářet zástupce na ploše
- `-CreateStartMenuShortcut $false` - nevytvářet zástupce v Start Menu

Příklad:
```powershell
powershell -ExecutionPolicy Bypass -File install-windows.ps1 -InstallPath "C:\MyApps\PlcSender"
```

## Konfigurace

Před prvním spuštěním zkontrolujte soubor `config.json`:

```json
{
  "PlcIpAddress": "192.168.0.1",
  "PlcRack": 0,
  "PlcSlot": 1,
  "ApiUrl": "http://localhost:5000/api/data",
  "ReadIntervalMs": 1000
}
```

Upravte hodnoty podle vašeho PLC a API.

## Odinstalace

Pro odinstalaci:

1. Smažte zástupce z plochy a Start Menu
2. Smažte instalační složku (výchozí: `C:\Program Files\PlcApiSender`)

## Řešení problémů

### "PowerShell není rozpoznán"
- Ujistěte se, že máte nainstalovaný PowerShell (měl by být součástí Windows)

### "Aplikace se nespustí"
- Zkontrolujte, že je správně nakonfigurovaný `config.json`
- Zkontrolujte, že máte oprávnění ke čtení/zápisu v instalační složce

### "Build selhal"
- Ujistěte se, že máte nainstalovaný .NET 8.0 SDK
- Stáhněte ho z: https://dotnet.microsoft.com/download/dotnet/8.0

### Aplikace potřebuje administrátorská práva pro instalaci
- Poklepejte pravým tlačítkem na `INSTALL.bat`
- Vyberte "Spustit jako správce"
- Nebo použijte vlastní cestu bez admin práv:
  ```powershell
  powershell -ExecutionPolicy Bypass -File install-windows.ps1 -InstallPath "$env:LOCALAPPDATA\PlcApiSender"
  ```

## Další informace

- Aplikace je sestavena jako self-contained (obsahuje .NET runtime)
- Velikost executable: ~60-80 MB (kvůli embedded .NET runtime)
- Podporované verze Windows: Windows 10, Windows 11, Windows Server 2019+

## Automatické spuštění při startu Windows

Pokud chcete, aby se aplikace spouštěla automaticky při startu Windows:

1. Stiskněte `Win + R`
2. Napište `shell:startup` a stiskněte Enter
3. Zkopírujte zástupce aplikace do této složky

Nebo použijte PowerShell:
```powershell
$shortcutPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\PLC API Sender.lnk"
$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "C:\Program Files\PlcApiSender\UbuntuPlcApiSender.exe"
$shortcut.Save()
```
