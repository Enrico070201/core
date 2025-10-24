# Sportwetten Vorhersage KI-Modell

Ein Machine Learning System zur Vorhersage von Sportwettenergebnissen mit modernen KI-Techniken.

## Überblick

Dieses Projekt nutzt Machine Learning Algorithmen, um Sportwettenergebnisse zu analysieren und vorherzusagen. Das System verarbeitet historische Spieldaten, Teamstatistiken und weitere relevante Features, um präzise Vorhersagen zu treffen.

## Features

- **Datenverarbeitung**: Automatisierte Sammlung und Aufbereitung von Sportdaten
- **Feature Engineering**: Intelligente Extraktion relevanter Merkmale aus Rohdaten
- **ML-Modelle**: Multiple Algorithmen (Random Forest, XGBoost, Neural Networks)
- **Vorhersage-API**: REST API für einfache Integration
- **Backtesting**: Evaluierung der Modellperformance auf historischen Daten
- **Echtzeitanalyse**: Live-Vorhersagen für aktuelle Spiele

## Projektstruktur

```
.
├── src/
│   ├── data/           # Datensammlung und -verarbeitung
│   ├── features/       # Feature Engineering
│   ├── models/         # ML-Modelle
│   └── utils/          # Hilfsfunktionen
├── data/
│   ├── raw/           # Rohdaten
│   └── processed/     # Verarbeitete Daten
├── models/            # Trainierte Modelle
├── notebooks/         # Jupyter Notebooks für Analysen
└── tests/            # Unit Tests
```

## Installation

1. Repository klonen:
```bash
git clone <repository-url>
cd core
```

2. Virtuelle Umgebung erstellen:
```bash
python -m venv venv
source venv/bin/activate  # Linux/Mac
# oder
venv\Scripts\activate  # Windows
```

3. Abhängigkeiten installieren:
```bash
pip install -r requirements.txt
```

## Verwendung

### 1. Daten sammeln
```bash
python src/data/collect_data.py
```

### 2. Modell trainieren
```bash
python src/models/train_model.py
```

### 3. Vorhersagen erstellen
```bash
python src/models/predict.py --match "Team A vs Team B"
```

### 4. API starten
```bash
python src/api/app.py
```

Die API ist dann unter `http://localhost:5000` erreichbar.

## API Endpoints

- `POST /predict` - Erstellt eine Vorhersage für ein Spiel
- `GET /models` - Listet verfügbare Modelle auf
- `GET /stats` - Zeigt Modellstatistiken

## Modelle

Das System unterstützt verschiedene ML-Algorithmen:

1. **Random Forest**: Robust und interpretierbar
2. **XGBoost**: Hohe Genauigkeit durch Gradient Boosting
3. **Neural Networks**: Deep Learning für komplexe Muster
4. **Ensemble**: Kombiniert mehrere Modelle für beste Ergebnisse

## Performance

Die Modelle werden anhand folgender Metriken evaluiert:
- Accuracy (Genauigkeit)
- Precision (Präzision)
- Recall (Trefferquote)
- F1-Score
- ROI (Return on Investment)

## Hinweise

⚠️ **Wichtiger Haftungsausschluss**:
Dieses Projekt dient ausschließlich zu Bildungs- und Forschungszwecken. Sportwetten bergen finanzielle Risiken. Die Vorhersagen sind keine Garantie für Gewinne. Wetten Sie verantwortungsvoll und nur mit Geld, das Sie sich leisten können zu verlieren.

## Lizenz

MIT License

## Beitragen

Contributions sind willkommen! Bitte erstellen Sie einen Pull Request oder öffnen Sie ein Issue für Vorschläge und Fehlerberichte.

## Support

Bei Fragen oder Problemen öffnen Sie bitte ein Issue auf GitHub.
