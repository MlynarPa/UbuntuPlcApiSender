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
    echo ""
    echo "📦 Soubory jsou v: ./publish-linux"
    echo ""
    echo "📋 Další kroky:"
    echo "1. Zkopíruj složku publish-linux/ na Ubuntu/Raspberry Pi"
    echo "2. Spusť: sudo ./install-linux-service.sh"
    echo ""
    echo "💡 Tip: Můžeš použít SCP pro přenos:"
    echo "   scp -r ./publish-linux/* user@raspberry-pi:/home/user/ubuntu-plc-sender/"
else
    echo "❌ Build selhal!"
    exit 1
fi
