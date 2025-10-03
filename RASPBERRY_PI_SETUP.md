# 🐧 Ubuntu - Instalační Návod

## 📋 O Aplikaci

**Ubuntu PLC API Sender** - Aplikace běžící na Ubuntu, která:
- Čte data z PLC (Siemens S7) přes IP adresu 192.168.0.10
- Odesílá data na API drevostroj.app každou sekundu
- Běží jako systemd služba na pozadí
- Automaticky se reconnectuje při výpadku PLC
- **Vytvoří ikonu na ploše pro snadné spuštění**

## 🚀 RYCHLÁ INSTALACE (Krok za krokem)

### 1️⃣ Stáhni aplikaci z GitHubu

```bash
# Naklonuj repozitář
git clone https://github.com/MlynarPa/UbuntuPlcApiSender.git

# Přejdi do složky s buildem
cd UbuntuPlcApiSender/publish-linux
```

### 2️⃣ Spusť instalaci (NOVÝ způsob s desktop ikonou)

```bash
# Nastav práva pro install script
chmod +x install-ubuntu.sh

# Spusť instalaci (vyžaduje sudo)
sudo ./install-ubuntu.sh
```

**✅ HOTOVO!** Aplikace běží na pozadí jako služba + máš ikonu na ploše!

**📌 ALTERNATIVA (pouze služba bez ikony):**
```bash
chmod +x install-linux-service.sh
sudo ./install-linux-service.sh
```

### 3️⃣ Zkontroluj, že funguje

```bash
# Zkontroluj stav služby
sudo systemctl status ubuntu-plc-sender

# Sleduj logy v reálném čase
sudo journalctl -u ubuntu-plc-sender -f
```

---

## ⚙️ KONFIGURACE

### Než spustíš instalaci, ZKONTROLUJ v souboru Program.cs:

```bash
# Otevři konfiguraci
nano ~/UbuntuPlcApiSender/publish-linux/Program.cs
```

**Důležité nastavení (řádky 10-15):**
```csharp
var plcIpAddress = "192.168.0.10";  // ← IP ADRESA TVÉHO PLC
var plcRack = (short)0;              // ← RACK číslo (obvykle 0)
var plcSlot = (short)1;              // ← SLOT číslo (obvykle 1)
var apiBaseUrl = "https://drevostroj.app";
var apiKey = "drevostrojapi2024";    // ← API KLÍČ
var readInterval = 1000;             // ← Interval čtení (1 sekunda)
```

**Pokud potřebuješ změnit IP nebo API:**
1. Uprav `Program.cs`
2. Restartuj službu: `sudo systemctl restart ubuntu-plc-sender`

---

## 📊 SPRÁVA SLUŽBY

### Základní příkazy:

```bash
# Spustit službu
sudo systemctl start ubuntu-plc-sender

# Zastavit službu
sudo systemctl stop ubuntu-plc-sender

# Restartovat službu
sudo systemctl restart ubuntu-plc-sender

# Zobrazit stav
sudo systemctl status ubuntu-plc-sender

# Povolit automatický start při bootu
sudo systemctl enable ubuntu-plc-sender

# Zakázat automatický start
sudo systemctl disable ubuntu-plc-sender
```

### Sledování logů:

```bash
# Živé logy (stiskni Ctrl+C pro ukončení)
sudo journalctl -u ubuntu-plc-sender -f

# Posledních 100 řádků
sudo journalctl -u ubuntu-plc-sender -n 100

# Logy od určitého času
sudo journalctl -u ubuntu-plc-sender --since "10 minutes ago"
sudo journalctl -u ubuntu-plc-sender --since "2024-10-02 14:00"
```

---

## ❓ ŘEŠENÍ PROBLÉMŮ

### Služba se nespustí

```bash
# Zobraz chybovou hlášku
sudo journalctl -u ubuntu-plc-sender -n 50

# Zkontroluj oprávnění
ls -la /opt/ubuntu-plc-sender/
```

### PLC se nepřipojuje

```bash
# Ověř síťové připojení k PLC
ping 192.168.0.10

# Zkontroluj IP adresu v konfiguraci
grep "plcIpAddress" /opt/ubuntu-plc-sender/Program.cs

# Zkontroluj firewall (pokud je aktivní)
sudo ufw status
```

### API nefunguje

```bash
# Test internetového připojení
ping drevostroj.app

# Zkontroluj DNS
nslookup drevostroj.app

# Test API přístupu
curl -v https://drevostroj.app/api/MachinesApi/Bulk
```

### Aplikace spadla

```bash
# Služba se automaticky restartuje po 10 sekundách
# Zkontroluj logy pro chybovou hlášku
sudo journalctl -u ubuntu-plc-sender -n 100
```

---

## 🔧 AKTUALIZACE APLIKACE

```bash
# Zastavit službu
sudo systemctl stop ubuntu-plc-sender

# Stáhnout novou verzi z GitHubu
cd ~/UbuntuPlcApiSender
git pull

# Zkopírovat nové soubory
sudo cp -r publish-linux/* /opt/ubuntu-plc-sender/
sudo chmod +x /opt/ubuntu-plc-sender/UbuntuPlcApiSender
sudo chown -R plcuser:plcuser /opt/ubuntu-plc-sender

# Spustit službu
sudo systemctl start ubuntu-plc-sender
```

---

## 🗑️ ODINSTALACE

```bash
# Zastavit a zakázat službu
sudo systemctl stop ubuntu-plc-sender
sudo systemctl disable ubuntu-plc-sender

# Smazat soubory
sudo rm /etc/systemd/system/ubuntu-plc-sender.service
sudo rm -rf /opt/ubuntu-plc-sender

# Smazat uživatele
sudo userdel plcuser

# Reload systemd
sudo systemctl daemon-reload
```

---

## 📱 CO APLIKACE DĚLÁ

### Dva asynchronní procesy:

**1. PLC Process:**
- Každou sekundu se pokouší připojit k PLC
- Čte data z datablocku (4 stroje: DRST_0001 - DRST_0004)
- Při výpadku automaticky reconnectuje

**2. API Process:**
- Každou sekundu odesílá bulk data na API
- Odesílá stav všech 4 strojů najednou
- Obsahuje flag `plcConnected` (true/false)

### Monitorovaná data pro každý stroj:
- `IsRunning` - Běží/Stojí
- `ElectricityConsumption` - Spotřeba energie (W)
- `Stav1, Stav2, Stav3, Stav4` - Číselné stavy

---

## 🌐 SÍŤOVÉ POŽADAVKY

Raspberry Pi musí mít přístup k:
- **PLC**: `192.168.0.10` (lokální síť)
- **API**: `https://drevostroj.app` (internet)

Pokud používáš firewall, otevři porty:
- Port 102 (S7 komunikace s PLC)
- Port 443 (HTTPS pro API)

---

## 📞 KONTAKT PRO POMOC

**GitHub Repository:**
https://github.com/MlynarPa/UbuntuPlcApiSender

**Při problémech vždy pošli:**
1. Výstup: `sudo systemctl status ubuntu-plc-sender`
2. Logy: `sudo journalctl -u ubuntu-plc-sender -n 100`
3. Síťový test: `ping 192.168.0.10` a `ping drevostroj.app`
