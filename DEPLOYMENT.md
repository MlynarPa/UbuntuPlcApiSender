# Deployment na Raspberry Pi / Ubuntu

## 🚀 Rychlý postup

### 1️⃣ Build na Windows

```bash
# Spusť build script
build-linux.bat
```

Vytvoří složku `publish-linux/` s aplikací pro Linux.

### 2️⃣ Přenos na Raspberry Pi/Ubuntu

**VARIANTA A - Přes GitHub (jednodušší):**

```bash
# Na Windows:
git add publish-linux/
git commit -m "Add Linux build"
git push

# Na Raspberry Pi/Ubuntu:
git clone https://github.com/TVOJE_REPO/UbuntuPlcApiSender.git
cd UbuntuPlcApiSender/publish-linux
```

**VARIANTA B - Přes SCP (bez GitHub):**

```bash
# Na Windows (přizpůsob IP a uživatele):
scp -r ./publish-linux/* pi@192.168.0.100:/home/pi/ubuntu-plc-sender/
```

**VARIANTA C - Přes USB/síťovou složku:**
- Zkopíruj `publish-linux/` na USB
- Připoj USB k Raspberry Pi
- Zkopíruj do `/home/pi/ubuntu-plc-sender/`

### 3️⃣ Instalace na Raspberry Pi/Ubuntu

```bash
# Přejdi do složky s aplikací
cd /cesta/k/publish-linux/

# Nastav oprávnění pro install script
chmod +x install-linux-service.sh

# Spusť instalaci (potřebuje sudo)
sudo ./install-linux-service.sh
```

### 4️⃣ Ověření

```bash
# Zkontroluj stav služby
sudo systemctl status ubuntu-plc-sender

# Sleduj logy v reálném čase
sudo journalctl -u ubuntu-plc-sender -f
```

## 📝 Užitečné příkazy

```bash
# Start/Stop služby
sudo systemctl start ubuntu-plc-sender
sudo systemctl stop ubuntu-plc-sender
sudo systemctl restart ubuntu-plc-sender

# Zobrazit logy
sudo journalctl -u ubuntu-plc-sender -n 100  # posledních 100 řádků
sudo journalctl -u ubuntu-plc-sender -f      # sledovat live

# Zakázat/Povolit automatický start
sudo systemctl disable ubuntu-plc-sender
sudo systemctl enable ubuntu-plc-sender
```

## ⚙️ Konfigurace

Před spuštěním uprav v `Program.cs`:
- **PLC IP adresa**: Řádek 10 - `var plcIpAddress = "192.168.0.10";`
- **API klíč**: Řádek 14 - `var apiKey = "drevostrojapi2024";`

## 🔧 Odinstalace

```bash
sudo systemctl stop ubuntu-plc-sender
sudo systemctl disable ubuntu-plc-sender
sudo rm /etc/systemd/system/ubuntu-plc-sender.service
sudo rm -rf /opt/ubuntu-plc-sender
sudo userdel plcuser
sudo systemctl daemon-reload
```

## ❓ Problémy

**Služba se nespustí:**
```bash
sudo journalctl -u ubuntu-plc-sender -n 50
```

**PLC se nepřipojuje:**
- Zkontroluj IP adresu PLC v konfiguraci
- Ověř síťové připojení: `ping 192.168.0.10`
- Zkontroluj firewall na Raspberry Pi

**API nefunguje:**
- Ověř internetové připojení
- Zkontroluj API klíč a URL
