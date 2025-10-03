#!/bin/bash

# Build script pro Linux deployment (self-contained)

echo "=== Building Ubuntu PLC API Sender for Linux ==="

# Clean previous build
echo "🧹 Čistím předchozí build..."
rm -rf ./publish-linux

# Build self-contained for linux-x64
echo "🔨 Sestavuji aplikaci pro Linux (self-contained)..."
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish-linux

if [ $? -eq 0 ]; then
    echo "✅ Build úspěšný!"

    # Copy additional files needed for installation
    echo "📋 Kopíruji instalační soubory..."

    # Copy systemd service file
    if [ -f "./ubuntu-plc-sender.service" ]; then
        cp ubuntu-plc-sender.service ./publish-linux/
        echo "  ✓ ubuntu-plc-sender.service"
    fi

    # Copy desktop file
    if [ -f "./ubuntu-plc-sender.desktop" ]; then
        cp ubuntu-plc-sender.desktop ./publish-linux/
        echo "  ✓ ubuntu-plc-sender.desktop"
    fi

    # Copy icon
    if [ -f "./app.png" ]; then
        cp app.png ./publish-linux/
        echo "  ✓ app.png"
    fi

    # Copy new installation script
    if [ -f "./install-ubuntu.sh" ]; then
        cp install-ubuntu.sh ./publish-linux/
        chmod +x ./publish-linux/install-ubuntu.sh
        echo "  ✓ install-ubuntu.sh"
    fi

    # Copy old installation script for backward compatibility
    if [ -f "./install-linux-service.sh" ]; then
        cp install-linux-service.sh ./publish-linux/
        chmod +x ./publish-linux/install-linux-service.sh
        echo "  ✓ install-linux-service.sh"
    fi

    echo ""
    echo "📦 Soubory jsou v: ./publish-linux"
    echo ""
    echo "📋 Obsah balíčku:"
    echo "  • UbuntuPlcApiSender (hlavní aplikace)"
    echo "  • install-ubuntu.sh (nový instalátor s desktop ikonou)"
    echo "  • install-linux-service.sh (starý instalátor - jen služba)"
    echo "  • ubuntu-plc-sender.service (systemd konfigurace)"
    echo "  • ubuntu-plc-sender.desktop (desktop ikona)"
    echo "  • app.png (ikona aplikace)"
    echo ""
    echo "🚀 INSTALACE NA UBUNTU:"
    echo "1. Přenes složku publish-linux/ na Ubuntu"
    echo "2. cd publish-linux"
    echo "3. sudo ./install-ubuntu.sh"
    echo ""
    echo "💡 Tip: Můžeš použít SCP pro přenos:"
    echo "   scp -r ./publish-linux user@ubuntu:/home/user/ubuntu-plc-sender"
    echo "   ssh user@ubuntu"
    echo "   cd ubuntu-plc-sender"
    echo "   sudo ./install-ubuntu.sh"
else
    echo "❌ Build selhal!"
    exit 1
fi
