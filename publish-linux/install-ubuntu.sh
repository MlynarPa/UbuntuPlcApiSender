#!/bin/bash

# Kompletní instalační script pro Ubuntu
# Nainstaluje aplikaci jako systemd službu + vytvoří ikonu na ploše

set -e

SERVICE_NAME="ubuntu-plc-sender"
SERVICE_USER="plcuser"
INSTALL_DIR="/opt/ubuntu-plc-sender"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"
DESKTOP_FILE="${SERVICE_NAME}.desktop"

echo "=== Ubuntu PLC API Sender - Kompletní Instalace ==="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "❌ Tento script musí být spuštěn jako root (sudo)"
    exit 1
fi

# Get the actual user (not root when using sudo)
ACTUAL_USER=${SUDO_USER:-$USER}
ACTUAL_HOME=$(getent passwd "$ACTUAL_USER" | cut -d: -f6)

echo "👤 Instaluji pro uživatele: $ACTUAL_USER"
echo "🏠 Domovský adresář: $ACTUAL_HOME"
echo ""

# Check if binary exists
if [ ! -f "./UbuntuPlcApiSender" ]; then
    echo "❌ Nenalezen soubor UbuntuPlcApiSender v současném adresáři"
    echo "   Zkontrolujte, že jste ve složce publish-linux/"
    exit 1
fi

# 1. Create service user if doesn't exist
if ! id "$SERVICE_USER" &>/dev/null; then
    echo "📝 Vytvářím systémového uživatele $SERVICE_USER..."
    useradd --system --no-create-home --shell /bin/false $SERVICE_USER
else
    echo "✅ Uživatel $SERVICE_USER již existuje"
fi

# 2. Create installation directory
echo "📁 Vytvářím adresář $INSTALL_DIR..."
mkdir -p $INSTALL_DIR

# 3. Copy application files
echo "📦 Kopíruji aplikaci do $INSTALL_DIR..."
cp -r ./* $INSTALL_DIR/
chmod +x $INSTALL_DIR/UbuntuPlcApiSender

# 4. Set ownership for service files
echo "🔐 Nastavuji oprávnění pro službu..."
chown -R $SERVICE_USER:$SERVICE_USER $INSTALL_DIR

# 5. Install systemd service
echo "⚙️  Instaluji systemd service..."
if [ -f "./ubuntu-plc-sender.service" ]; then
    cp ubuntu-plc-sender.service $SERVICE_FILE
else
    echo "❌ Nenalezen soubor ubuntu-plc-sender.service"
    exit 1
fi

# 6. Reload systemd and enable service
echo "🔄 Reloaduji systemd..."
systemctl daemon-reload

echo "✅ Povolení služby pro automatický start..."
systemctl enable $SERVICE_NAME

# 7. Start the service
echo "🚀 Spouštím službu..."
systemctl start $SERVICE_NAME

# 8. Install desktop icon
echo ""
echo "🖥️  Instaluji desktop ikonu..."

# Create user's local applications directory
USER_APPS_DIR="$ACTUAL_HOME/.local/share/applications"
mkdir -p "$USER_APPS_DIR"
chown $ACTUAL_USER:$ACTUAL_USER "$USER_APPS_DIR"

# Copy desktop file
if [ -f "./ubuntu-plc-sender.desktop" ]; then
    cp ubuntu-plc-sender.desktop "$USER_APPS_DIR/"
    chown $ACTUAL_USER:$ACTUAL_USER "$USER_APPS_DIR/ubuntu-plc-sender.desktop"
    chmod +x "$USER_APPS_DIR/ubuntu-plc-sender.desktop"
    echo "✅ Desktop soubor nainstalován do $USER_APPS_DIR"
else
    echo "⚠️  Desktop soubor nenalezen, přeskakuji..."
fi

# Copy desktop file to Desktop folder if exists
DESKTOP_DIR="$ACTUAL_HOME/Desktop"
if [ -d "$DESKTOP_DIR" ]; then
    cp "$USER_APPS_DIR/ubuntu-plc-sender.desktop" "$DESKTOP_DIR/" 2>/dev/null || true
    chown $ACTUAL_USER:$ACTUAL_USER "$DESKTOP_DIR/ubuntu-plc-sender.desktop" 2>/dev/null || true
    chmod +x "$DESKTOP_DIR/ubuntu-plc-sender.desktop" 2>/dev/null || true

    # Allow launching (Ubuntu 20.04+)
    gio set "$DESKTOP_DIR/ubuntu-plc-sender.desktop" metadata::trusted true 2>/dev/null || true

    echo "✅ Ikona zkopírována na plochu"
fi

# Try Czech Desktop folder name as well
DESKTOP_DIR_CZ="$ACTUAL_HOME/Plocha"
if [ -d "$DESKTOP_DIR_CZ" ]; then
    cp "$USER_APPS_DIR/ubuntu-plc-sender.desktop" "$DESKTOP_DIR_CZ/" 2>/dev/null || true
    chown $ACTUAL_USER:$ACTUAL_USER "$DESKTOP_DIR_CZ/ubuntu-plc-sender.desktop" 2>/dev/null || true
    chmod +x "$DESKTOP_DIR_CZ/ubuntu-plc-sender.desktop" 2>/dev/null || true

    # Allow launching
    gio set "$DESKTOP_DIR_CZ/ubuntu-plc-sender.desktop" metadata::trusted true 2>/dev/null || true

    echo "✅ Ikona zkopírována na plochu (Plocha)"
fi

# Update desktop database
if command -v update-desktop-database &> /dev/null; then
    sudo -u $ACTUAL_USER update-desktop-database "$USER_APPS_DIR" 2>/dev/null || true
fi

# 9. Show status
echo ""
echo "📊 Stav služby:"
systemctl status $SERVICE_NAME --no-pager || true

echo ""
echo "======================================"
echo "✅ INSTALACE ÚSPĚŠNĚ DOKONČENA!"
echo "======================================"
echo ""
echo "📱 CO BYLO NAINSTALOVÁNO:"
echo "  ✓ Systemd služba (běží na pozadí)"
echo "  ✓ Ikona v aplikacích menu"
echo "  ✓ Ikona na ploše"
echo ""
echo "🎯 DŮLEŽITÉ PŘÍKAZY:"
echo "  Sledovat logy:     sudo journalctl -u $SERVICE_NAME -f"
echo "  Restartovat:       sudo systemctl restart $SERVICE_NAME"
echo "  Zastavit:          sudo systemctl stop $SERVICE_NAME"
echo "  Spustit:           sudo systemctl start $SERVICE_NAME"
echo "  Status:            sudo systemctl status $SERVICE_NAME"
echo ""
echo "🖥️  SPUŠTĚNÍ Z IKONY:"
echo "  • Ikona na ploše: Klikni pravým tlačítkem → 'Povolit spuštění' → Dvojklik"
echo "  • Aplikace menu: Hledej 'Ubuntu PLC Sender'"
echo ""
echo "⚙️  KONFIGURACE:"
echo "  Upravit nastavení: sudo nano $INSTALL_DIR/Program.cs"
echo "  Po změně:          sudo systemctl restart $SERVICE_NAME"
echo ""
echo "🗑️  ODINSTALACE:"
echo "  sudo systemctl stop $SERVICE_NAME"
echo "  sudo systemctl disable $SERVICE_NAME"
echo "  sudo rm $SERVICE_FILE"
echo "  sudo rm -rf $INSTALL_DIR"
echo "  sudo userdel $SERVICE_USER"
echo "  rm ~/.local/share/applications/ubuntu-plc-sender.desktop"
echo "  rm ~/Desktop/ubuntu-plc-sender.desktop"
echo ""
