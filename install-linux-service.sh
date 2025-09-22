#!/bin/bash

# Script pro instalaci na Linux (Ubuntu/Debian) jako systemd service

set -e

SERVICE_NAME="ubuntu-plc-sender"
SERVICE_USER="plcuser"
INSTALL_DIR="/opt/ubuntu-plc-sender"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

echo "=== Ubuntu PLC API Sender - Linux Service Installer ==="

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "❌ Tento script musí být spuštěn jako root (sudo)"
    exit 1
fi

# Create service user if doesn't exist
if ! id "$SERVICE_USER" &>/dev/null; then
    echo "📝 Vytvářím uživatele $SERVICE_USER..."
    useradd --system --no-create-home --shell /bin/false $SERVICE_USER
else
    echo "✅ Uživatel $SERVICE_USER již existuje"
fi

# Create installation directory
echo "📁 Vytvářím adresář $INSTALL_DIR..."
mkdir -p $INSTALL_DIR

# Check if binary exists
if [ ! -f "./UbuntuPlcApiSender" ]; then
    echo "❌ Nenalezen soubor UbuntuPlcApiSender v současném adresáři"
    echo "   Spusťte nejdříve: dotnet publish -c Release -r linux-x64 --self-contained"
    exit 1
fi

# Copy application files
echo "📦 Kopíruji aplikaci do $INSTALL_DIR..."
cp -r ./* $INSTALL_DIR/
chmod +x $INSTALL_DIR/UbuntuPlcApiSender

# Set ownership
echo "🔐 Nastavuji oprávnění..."
chown -R $SERVICE_USER:$SERVICE_USER $INSTALL_DIR

# Install systemd service
echo "⚙️  Instaluji systemd service..."
cp ubuntu-plc-sender.service $SERVICE_FILE

# Reload systemd and enable service
echo "🔄 Reloaduji systemd..."
systemctl daemon-reload

echo "✅ Povolení služby pro automatický start..."
systemctl enable $SERVICE_NAME

# Start the service
echo "🚀 Spouštím službu..."
systemctl start $SERVICE_NAME

# Show status
echo ""
echo "📊 Stav služby:"
systemctl status $SERVICE_NAME --no-pager

echo ""
echo "✅ Služba úspěšně nainstalována!"
echo ""
echo "Užitečné příkazy:"
echo "  sudo systemctl start $SERVICE_NAME      # Spustit službu"
echo "  sudo systemctl stop $SERVICE_NAME       # Zastavit službu"
echo "  sudo systemctl restart $SERVICE_NAME    # Restartovat službu"
echo "  sudo systemctl status $SERVICE_NAME     # Zobrazit stav"
echo "  sudo journalctl -u $SERVICE_NAME -f     # Zobrazit logy v reálném čase"
echo "  sudo systemctl disable $SERVICE_NAME    # Zakázat automatický start"
echo ""
echo "Pro odinstalaci:"
echo "  sudo systemctl stop $SERVICE_NAME"
echo "  sudo systemctl disable $SERVICE_NAME"
echo "  sudo rm $SERVICE_FILE"
echo "  sudo rm -rf $INSTALL_DIR"
echo "  sudo userdel $SERVICE_USER"
