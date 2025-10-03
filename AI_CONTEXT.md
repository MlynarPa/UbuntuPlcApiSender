# 🤖 AI Assistant Context - Ubuntu PLC API Sender

> **Pro AI asistenty pracující s touto aplikací na Raspberry Pi/Ubuntu**

## 📦 Co je tato aplikace?

**.NET 8.0 konzolová aplikace** běžící jako **systemd služba** na Linux (Raspberry Pi/Ubuntu).

**Účel:**
- Čte data z **Siemens S7 PLC** (IP: 192.168.0.10) každou sekundu
- Odesílá data na **REST API** (https://drevostroj.app) bulk požadavkem
- Monitoruje **4 stroje** (DRST_0001 až DRST_0004)
- Běží **24/7 na pozadí** s automatickým restartem při pádu

---

## 🏗️ Architektura

### Dva asynchronní tasky běžící paralelně:

**1. PLC Task (Services/PlcReader.cs):**
```
- Každou 1s: Pokus o připojení k PLC (pokud není připojeno)
- Při úspěchu: Čtení dat z DB1 (offset 0, 64 bytes)
- Při selhání: Fallback data s plcConnected=false
- Auto-reconnect: Nekonečná smyčka s resilient připojením
```

**2. API Task (Services/ApiClient.cs):**
```
- Každou 1s: Odeslání bulk PUT na /api/MachinesApi/Bulk
- Data: List<Machine> (4 stroje)
- Header: X-API-Key: drevostrojapi2024
- Fallback: Při nedostupném PLC odesílá placeholder data
```

### Data flow:
```
PLC (S7-1200/1500)
  ↓ (S7netplus library)
PlcReader.ReadAllMachinesAsync()
  ↓ (lock-protected shared list)
latestMachines (List<Machine>)
  ↓
ApiClient.SendBulkMachineDataAsync()
  ↓ (HTTPS PUT)
drevostroj.app API
```

---

## 📂 Struktura projektu

```
UbuntuPlcApiSender/
├── Program.cs                    # Main entry point, dva async tasky
├── Services/
│   ├── PlcReader.cs             # PLC komunikace (S7netplus)
│   └── ApiClient.cs             # HTTP API klient
├── Models/
│   └── Machine.cs               # Data model stroje
├── publish-linux/               # Self-contained Linux build
├── install-linux-service.sh     # Systemd instalační script
├── ubuntu-plc-sender.service    # Systemd unit file
└── RASPBERRY_PI_SETUP.md        # User guide pro Raspberry Pi
```

---

## 🔧 Konfigurace

### Hardcoded v Program.cs (řádky 10-15):
```csharp
var plcIpAddress = "192.168.0.10";
var plcRack = (short)0;
var plcSlot = (short)1;
var apiBaseUrl = "https://drevostroj.app";
var apiKey = "drevostrojapi2024";
var readInterval = 1000; // ms
```

### Systemd služba:
- **Cesta:** `/etc/systemd/system/ubuntu-plc-sender.service`
- **User:** `plcuser` (system account)
- **WorkingDir:** `/opt/ubuntu-plc-sender`
- **Restart:** Vždy po 10s
- **Logs:** `journalctl -u ubuntu-plc-sender`

---

## 🛠️ Technické detaily

### Dependencies:
- **S7netplus** (0.20.0) - Siemens S7 PLC komunikace
- **System.Text.Json** (8.0.5) - JSON serialization
- **.NET 8.0 runtime** - Self-contained build (žádná instalace potřeba)

### PLC Communication (PlcReader.cs):
```csharp
// DB struktura (DB1, 64 bytes celkem, 16 bytes na stroj):
Offset 0-15:   DRST_0001
Offset 16-31:  DRST_0002
Offset 32-47:  DRST_0003
Offset 48-63:  DRST_0004

// Data pro každý stroj (16 bytes):
[0-1]:   ElectricityConsumption (Int16)
[2]:     IsRunning (Byte, 0/1)
[4-7]:   Stav1 (Real/Float)
[8-11]:  Stav2 (Real/Float)
[12-15]: Stav3 (Real/Float)
// Stav4 se počítá jako: Stav1 + Stav2 + Stav3
```

### API Communication (ApiClient.cs):
```http
PUT https://drevostroj.app/api/MachinesApi/Bulk
Headers:
  Content-Type: application/json
  X-API-Key: drevostrojapi2024
Body:
[
  {
    "externalId": "DRST_0001",
    "isRunning": true,
    "electricityConsumption": 1500,
    "stav1": 10.5,
    "stav2": 20.3,
    "stav3": 15.7,
    "stav4": 46.5,
    "plcConnected": true
  },
  // ... 3 další stroje
]
```

---

## 🚨 Známé problémy a řešení

### 1. PLC nepřipojeno
**Symptom:** `plcConnected: false` v API datech
**Důvod:** Síť, špatná IP, PLC vypnuté
**Řešení:**
```bash
ping 192.168.0.10
sudo systemctl restart ubuntu-plc-sender
```

### 2. API selhává
**Symptom:** "Chyba při odesílání bulk dat" v logách
**Důvod:** Internet down, špatný API key, API nedostupné
**Řešení:**
```bash
curl -v https://drevostroj.app/api/MachinesApi/Bulk
# Zkontroluj API key v Program.cs
```

### 3. Služba se nespouští
**Symptom:** `systemctl status` = failed
**Důvod:** Chybí oprávnění, poškozený binary
**Řešení:**
```bash
sudo journalctl -u ubuntu-plc-sender -n 50
sudo chmod +x /opt/ubuntu-plc-sender/UbuntuPlcApiSender
sudo systemctl restart ubuntu-plc-sender
```

---

## 💡 Časté úkoly pro AI asistenta

### "Aplikace nefunguje"
1. `sudo systemctl status ubuntu-plc-sender`
2. `sudo journalctl -u ubuntu-plc-sender -n 100`
3. Zkontroluj síť: `ping 192.168.0.10` a `ping drevostroj.app`

### "Potřebuji změnit IP PLC"
1. `sudo systemctl stop ubuntu-plc-sender`
2. `sudo nano /opt/ubuntu-plc-sender/Program.cs` (řádek 10)
3. `sudo systemctl start ubuntu-plc-sender`

### "Jak vidím data v reálném čase?"
```bash
sudo journalctl -u ubuntu-plc-sender -f
```

### "Potřebuji aktualizovat aplikaci"
```bash
cd ~/UbuntuPlcApiSender
git pull
sudo systemctl stop ubuntu-plc-sender
sudo cp -r publish-linux/* /opt/ubuntu-plc-sender/
sudo chmod +x /opt/ubuntu-plc-sender/UbuntuPlcApiSender
sudo chown -R plcuser:plcuser /opt/ubuntu-plc-sender
sudo systemctl start ubuntu-plc-sender
```

---

## 📊 Monitoring výstup

**Normální běh vypadá:**
```
[14:30:01] 📊 PLC #1 - Data načtena pro 4 strojů
  • DRST_0001: ▶️ BĚŽÍ | 1500W | Stavy: 10.5,20.3,15.7,46.5
  • DRST_0002: ⏸️ STOJÍ | 0W | Stavy: 0,0,0,0
  ...
[14:30:01] ✓ API #1 - Bulk data odeslána (4 strojů, PLC ✅)
```

**PLC disconnect:**
```
[14:30:02] ❌ PLC #2 - Chyba při čtení dat
[14:30:02] 🔄 Pokus o připojení k PLC #1...
[14:30:02] ✓ API #2 - Bulk data odeslána (4 strojů, PLC ❌)
```

---

## 🔗 Odkazy

- **GitHub:** https://github.com/MlynarPa/UbuntuPlcApiSender
- **User Guide:** [RASPBERRY_PI_SETUP.md](RASPBERRY_PI_SETUP.md)
- **Deployment:** [DEPLOYMENT.md](DEPLOYMENT.md)

---

## ⚡ Quick Commands Cheatsheet

```bash
# Status
sudo systemctl status ubuntu-plc-sender

# Logy live
sudo journalctl -u ubuntu-plc-sender -f

# Restart
sudo systemctl restart ubuntu-plc-sender

# Test PLC
ping 192.168.0.10

# Test API
curl https://drevostroj.app/api/MachinesApi/Bulk

# Edit config
sudo nano /opt/ubuntu-plc-sender/Program.cs
```
