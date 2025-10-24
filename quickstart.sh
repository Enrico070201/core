#!/bin/bash
# Quick Start Script für Sportwetten-Vorhersage System

echo "======================================"
echo "Sportwetten Vorhersage - Quick Start"
echo "======================================"
echo ""

# Überprüfe Python Installation
if ! command -v python3 &> /dev/null; then
    echo "❌ Python 3 ist nicht installiert!"
    exit 1
fi

echo "✓ Python 3 gefunden"

# Erstelle virtuelle Umgebung wenn nicht vorhanden
if [ ! -d "venv" ]; then
    echo ""
    echo "[1/4] Erstelle virtuelle Umgebung..."
    python3 -m venv venv
    echo "✓ Virtuelle Umgebung erstellt"
fi

# Aktiviere virtuelle Umgebung
echo ""
echo "[2/4] Aktiviere virtuelle Umgebung..."
source venv/bin/activate
echo "✓ Virtuelle Umgebung aktiviert"

# Installiere Dependencies
echo ""
echo "[3/4] Installiere Abhängigkeiten..."
echo "    (Dies kann einige Minuten dauern...)"
pip install --quiet --upgrade pip
pip install --quiet -r requirements.txt
echo "✓ Abhängigkeiten installiert"

# Führe Pipeline aus
echo ""
echo "[4/4] Führe ML-Pipeline aus..."
echo ""
python main.py --full

echo ""
echo "======================================"
echo "✓ Setup abgeschlossen!"
echo "======================================"
echo ""
echo "Das System ist einsatzbereit. Verfügbare Befehle:"
echo ""
echo "  python main.py --predict          # Beispiel-Vorhersagen"
echo "  python main.py --api              # Starte API Server"
echo "  python src/models/predict.py --match 'Team A vs Team B'"
echo ""
echo "Weitere Infos: siehe README.md"
echo ""
