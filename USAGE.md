# Verwendungsanleitung - Sportwetten Vorhersage KI

## Schnellstart

Der einfachste Weg, das System zu starten:

```bash
./quickstart.sh
```

Dies installiert alle Abhängigkeiten und führt die komplette Pipeline aus.

## Schritt-für-Schritt Anleitung

### 1. Installation

```bash
# Virtuelle Umgebung erstellen
python3 -m venv venv

# Aktivieren
source venv/bin/activate  # Linux/Mac
# oder
venv\Scripts\activate  # Windows

# Abhängigkeiten installieren
pip install -r requirements.txt
```

### 2. Daten sammeln

```bash
python src/data/collect_data.py
```

Generiert Beispieldaten mit 1000 Spielen und Teamstatistiken.

### 3. Daten vorverarbeiten

```bash
python src/data/preprocessing.py
```

Erstellt Features und bereitet Daten für ML-Training vor.

### 4. Modelle trainieren

```bash
python src/models/train_model.py
```

Trainiert Random Forest und XGBoost Modelle.

### 5. Vorhersagen erstellen

```bash
# Beispiel-Vorhersagen
python src/models/predict.py

# Spezifisches Spiel
python src/models/predict.py --match "Bayern München vs Borussia Dortmund"

# Mit anderem Modell
python src/models/predict.py --match "RB Leipzig vs Bayer Leverkusen" --model xgboost
```

## Komplette Pipeline

Führt alle Schritte automatisch aus:

```bash
python main.py --full
```

## API Server

### Server starten

```bash
python src/api/app.py
# oder
python main.py --api
```

Server läuft auf: http://localhost:5000

### API Endpunkte

#### 1. Einzelne Vorhersage

```bash
curl -X POST http://localhost:5000/predict \
  -H "Content-Type: application/json" \
  -d '{
    "home_team": "Bayern München",
    "away_team": "Borussia Dortmund",
    "model": "random_forest"
  }'
```

Antwort:
```json
{
  "match": {
    "home_team": "Bayern München",
    "away_team": "Borussia Dortmund"
  },
  "prediction": "Home Win",
  "probabilities": {
    "home_win": 0.65,
    "draw": 0.20,
    "away_win": 0.15
  },
  "confidence": 0.65,
  "model": "random_forest"
}
```

#### 2. Batch-Vorhersagen

```bash
curl -X POST http://localhost:5000/predict/batch \
  -H "Content-Type: application/json" \
  -d '{
    "matches": [
      {"home_team": "Bayern München", "away_team": "Borussia Dortmund"},
      {"home_team": "RB Leipzig", "away_team": "Bayer Leverkusen"}
    ],
    "model": "xgboost"
  }'
```

#### 3. Verfügbare Modelle abrufen

```bash
curl http://localhost:5000/models
```

## Python API Verwendung

```python
from src.models.predict import MatchPredictor

# Initialisiere Predictor
predictor = MatchPredictor(model_type='random_forest')

# Vorhersage
result, probabilities = predictor.predict_match(
    home_team="Bayern München",
    away_team="Borussia Dortmund"
)

print(f"Vorhersage: {result}")
print(f"Heimsieg: {probabilities[0]*100:.2f}%")
print(f"Unentschieden: {probabilities[1]*100:.2f}%")
print(f"Auswärtssieg: {probabilities[2]*100:.2f}%")
```

## Jupyter Notebook

Für interaktive Analysen:

```bash
jupyter notebook notebooks/beispiel_analyse.ipynb
```

## Eigene Daten verwenden

### Format der Eingabedaten

Spieldaten (`data/raw/matches.csv`):
```csv
date,home_team,away_team,home_goals,away_goals,result,...
2024-01-15,Bayern München,Borussia Dortmund,3,1,H,...
```

Teamstatistiken (`data/raw/team_stats.csv`):
```csv
team,matches_played,wins,draws,losses,win_rate,...
Bayern München,34,25,5,4,0.735,...
```

### Daten einlesen

```python
from src.data.collect_data import SportsDataCollector

collector = SportsDataCollector()
matches_df = collector.load_data("your_matches.csv")
team_stats = collector.collect_team_stats(matches_df)
```

## Modell-Konfiguration

Anpassen in `config.yaml`:

```yaml
training:
  test_size: 0.2
  cv_folds: 5

  models:
    random_forest:
      n_estimators: 200
      max_depth: 15
```

## Performance-Metriken

Nach dem Training werden Metriken gespeichert:
- `models/random_forest_metrics.json`
- `models/xgboost_metrics.json`

```bash
cat models/random_forest_metrics.json
```

## Troubleshooting

### Modell nicht gefunden

```bash
# Stelle sicher, dass Modelle trainiert wurden
python src/models/train_model.py
```

### Daten fehlen

```bash
# Generiere Beispieldaten
python src/data/collect_data.py
python src/data/preprocessing.py
```

### Import-Fehler

```bash
# Stelle sicher, dass du in der virtuellen Umgebung bist
source venv/bin/activate

# Installiere Dependencies neu
pip install -r requirements.txt
```

## Erweiterte Nutzung

### Eigene Features hinzufügen

Bearbeite `src/data/preprocessing.py` und füge neue Features in der `create_features()` Methode hinzu:

```python
def create_features(self, matches_df, team_stats_df):
    # ... bestehende Features ...

    features['my_custom_feature'] = calculate_custom_feature()

    return features_df
```

### Neues Modell hinzufügen

In `src/models/train_model.py`:

```python
elif self.model_type == "neural_network":
    from tensorflow import keras
    self.model = keras.Sequential([
        keras.layers.Dense(64, activation='relu'),
        keras.layers.Dense(32, activation='relu'),
        keras.layers.Dense(3, activation='softmax')
    ])
```

## Best Practices

1. **Regelmäßiges Retraining**: Trainiere Modelle regelmäßig mit aktuellen Daten
2. **Backtesting**: Teste Modelle auf historischen Daten
3. **Ensemble-Methoden**: Kombiniere mehrere Modelle für bessere Ergebnisse
4. **Feature Engineering**: Experimentiere mit verschiedenen Features
5. **Cross-Validation**: Nutze CV für robuste Evaluierung

## Lizenz & Haftungsausschluss

⚠️ **WICHTIG**:
- Dieses Projekt ist NUR für Bildungs- und Forschungszwecke
- Sportwetten bergen finanzielle Risiken
- Keine Garantie für Vorhersagegenauigkeit
- Wetten Sie verantwortungsvoll!

Siehe LICENSE für Details.
