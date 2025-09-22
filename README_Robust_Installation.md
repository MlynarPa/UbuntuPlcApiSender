# Ubuntu PLC API Sender - Robustní verze

## 🚀 Nové funkce

### ✅ Automatické obnovování spojení s PLC
- Pokus o připojení s retry logikou (3 pokusy s exponenciálním zpožděním)
- Kontinuální monitoring spojení
- Automatické obnovení po výpadku

### ✅ Fallback režim při výpadku PLC
- **Po 10 sekundách** bez spojení s PLC se aktivuje fallback režim
- **Záporná spotřeba (-999W)** indikuje výpadek spojení u běžících strojů
- **Nulová spotřeba (0W)** u vypnutých strojů (normální stav)
- **Zachování stavů** DI1/DI2 z posledního úspěšného čtení

### ✅ Persistentní ukládání dat
- Automatické ukládání stavů strojů na disk (`last_machine_states.json`)
- Obnovení stavů po restartu aplikace
- Graceful shutdown s uložením dat

### ✅ Kontinuální API odesílání
- API funguje **nezávisle na PLC** spojení
- Odesílá buď živá data nebo fallback data
- Označení typu dat v logech (ŽIVÁ/FALLBACK)

## 📦 Instalace

### Windows Service

1. **Kompilace:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

2. **Instalace jako služba (jako Administrator):**
```powershell
.\install-service.ps1
```

3. **Správa služby:**
```powershell
# Spustit
Start-Service -Name UbuntuPlcApiSender

# Zastavit  
Stop-Service -Name UbuntuPlcApiSender

# Restart
Restart-Service -Name UbuntuPlcApiSender

# Stav
Get-Service -Name UbuntuPlcApiSender
```

### Linux Service (Ubuntu/Debian)

1. **Kompilace:**
```bash
dotnet publish -c Release -r linux-x64 --self-contained
cd bin/Release/net8.0/linux-x64/publish/
```

2. **Instalace jako systemd service:**
```bash
sudo ./install-linux-service.sh
```

3. **Správa služby:**
```bash
# Spustit
sudo systemctl start ubuntu-plc-sender

# Zastavit
sudo systemctl stop ubuntu-plc-sender

# Restart
sudo systemctl restart ubuntu-plc-sender

# Stav
sudo systemctl status ubuntu-plc-sender

# Logy v reálném čase
sudo journalctl -u ubuntu-plc-sender -f
```

## ⚙️ Konfigurace

### appsettings.json
```json
{
  "PlcConfiguration": {
    "IpAddress": "192.168.0.10",
    "ReadIntervalMs": 1000
  },
  "ApiConfiguration": {
    "BaseUrl": "https://drevostroj.app",
    "ApiKey": "drevostrojapi2024", 
    "SendIntervalMs": 1000,
    "MachinesWithApi": ["DRS_0001"]
  }
}
```

### Podporované aliasy strojů
- `DRS_0001` → `DRST_0001`
- `DRS1` → `DRST_0001` 
- `DRST_0001` → `DRST_0001`
- `DRST1` → `DRST_0001`
- ... (totéž pro stroje 2, 3, 4)

## 🔍 Monitoring

### Výstup aplikace
```
[14:30:01] 📊 PLC #1 - Načteno 4 strojů (ŽIVÁ DATA):
  • DRS_0001: 1250W | DI1:True | DI2:False | Běží:True (→API)
  • DRS_0002: 890W | DI1:False | DI2:True | Běží:False (lokálně)

[14:30:01] ✓ API #1 - DRS_0001 odeslán (ŽIVÁ data)

--- Po výpadku PLC ---

[14:30:15] 🔄 PLC #15 - Spojení selhalo, používám FALLBACK data:
  • DRS_0001: -999W | DI1:True | DI2:False | Běží:True (→API) [FALLBACK]
  • DRS_0002: 0W | DI1:False | DI2:True | Běží:False (lokálně) [CACHED]

[14:30:15] ✓ API #15 - DRS_0001 odeslán (FALLBACK data)
```

### Health reporting (každých 30s)
```
[14:30:30] 🏥 HEALTH: Spojení=False, Selhání=25, Bez spojení=00:00:25
```

### Přehled strojů (každých 30s)
```
[14:30:30] 📈 === PŘEHLED VŠECH STROJŮ ===
  ✅ DRS_0001: 1250W | DI1:True | DI2:False | Běží:True | ✓ API | Stáří:1.2s [ŽIVÁ]
  ❌ DRS_0002: -999W | DI1:False | DI2:True | Běží:True | ○ Lokálně | Stáří:15.4s [FALLBACK]
  🔗 PLC: ODPOJENO | Selhání: 25 | Bez spojení: 00:00:25
```

## 🛡️ Ochranná opatření

### 1. Automatické obnovování spojení
- Retry logika s exponenciálním zpožděním
- Kontinuální pokusy o připojení
- Automatické obnovení po výpadku

### 2. Fallback mechanismus
- Aktivace po 10 sekundách výpadku
- Záporná spotřeba (-999W) indikuje problém
- Zachování posledních známých stavů

### 3. Persistentní data
- Automatické ukládání na disk
- Obnovení po restartu
- Graceful shutdown

### 4. Service monitoring
- Windows Service s automatickým restartem
- Linux systemd s restart policy
- Health monitoring

### 5. Error handling
- Zachycení všech výjimek
- Detailní logování chyb
- Pokračování provozu i při chybách

## 🔧 Testování

### Test aliasů
```bash
# Zkopírovat test program
cp Program_TestAliases.cs Program.cs
dotnet run
```

### Test robustního režimu  
```bash
# Zkopírovat robustní program
cp Program_Robust.cs Program.cs
dotnet run
```

## 📁 Soubory

- `Program_Robust.cs` - Hlavní robustní program
- `Services/ConnectionManager.cs` - Správa spojení a fallback dat
- `Services/PlcReader.cs` - Rozšířený PLC reader s retry logikou
- `install-service.ps1` - Windows Service installer
- `install-linux-service.sh` - Linux systemd installer
- `ubuntu-plc-sender.service` - Systemd service definice

## 💡 Doporučení

1. **Testujte nejdříve v konzoli** před instalací jako služba
2. **Nastavte firewall** pro PLC komunikaci (port 102)
3. **Monitorujte logy** pro včasné odhalení problémů
4. **Zálohujte konfiguraci** před změnami
5. **Testujte výpadky** pro ověření fallback funkčnosti
