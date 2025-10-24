"""
Flask REST API für Sportwetten-Vorhersagen
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import sys
from pathlib import Path

# Füge src zum Path hinzu
sys.path.insert(0, str(Path(__file__).parent.parent.parent))

from src.models.predict import MatchPredictor

app = Flask(__name__)
CORS(app)

# Lade Modelle beim Start
predictors = {}

try:
    predictors['random_forest'] = MatchPredictor(model_type='random_forest')
    print("✓ Random Forest Modell geladen")
except Exception as e:
    print(f"⚠ Random Forest Modell nicht geladen: {e}")

try:
    predictors['xgboost'] = MatchPredictor(model_type='xgboost')
    print("✓ XGBoost Modell geladen")
except Exception as e:
    print(f"⚠ XGBoost Modell nicht geladen: {e}")


@app.route('/')
def home():
    """API Info"""
    return jsonify({
        'name': 'Sportwetten Vorhersage API',
        'version': '1.0.0',
        'endpoints': {
            '/predict': 'POST - Vorhersage für ein Spiel',
            '/models': 'GET - Verfügbare Modelle',
            '/health': 'GET - Health Check'
        }
    })


@app.route('/health')
def health():
    """Health Check"""
    return jsonify({
        'status': 'healthy',
        'models_loaded': list(predictors.keys())
    })


@app.route('/models', methods=['GET'])
def get_models():
    """Liste verfügbare Modelle"""
    models_info = []

    for model_name, predictor in predictors.items():
        models_info.append({
            'name': model_name,
            'type': predictor.model_type,
            'status': 'ready'
        })

    return jsonify({
        'models': models_info,
        'count': len(models_info)
    })


@app.route('/predict', methods=['POST'])
def predict():
    """
    Vorhersage für ein Spiel

    Request Body:
    {
        "home_team": "Bayern München",
        "away_team": "Borussia Dortmund",
        "model": "random_forest",  # optional
        "features": { ... }  # optional
    }
    """
    try:
        data = request.get_json()

        if not data:
            return jsonify({'error': 'Keine Daten übermittelt'}), 400

        # Validiere Input
        home_team = data.get('home_team')
        away_team = data.get('away_team')

        if not home_team or not away_team:
            return jsonify({
                'error': 'home_team und away_team sind erforderlich'
            }), 400

        # Wähle Modell
        model_type = data.get('model', 'random_forest')

        if model_type not in predictors:
            return jsonify({
                'error': f'Modell {model_type} nicht verfügbar',
                'available_models': list(predictors.keys())
            }), 400

        predictor = predictors[model_type]

        # Features (optional)
        features = data.get('features', None)

        # Vorhersage
        result, probabilities = predictor.predict_match(
            home_team, away_team, features
        )

        # Response
        response = {
            'match': {
                'home_team': home_team,
                'away_team': away_team
            },
            'prediction': result,
            'probabilities': {
                'home_win': float(probabilities[0]),
                'draw': float(probabilities[1]),
                'away_win': float(probabilities[2])
            },
            'confidence': float(max(probabilities)),
            'model': model_type
        }

        return jsonify(response)

    except Exception as e:
        return jsonify({
            'error': 'Interner Fehler',
            'message': str(e)
        }), 500


@app.route('/predict/batch', methods=['POST'])
def predict_batch():
    """
    Vorhersagen für mehrere Spiele

    Request Body:
    {
        "matches": [
            {"home_team": "...", "away_team": "..."},
            {"home_team": "...", "away_team": "..."}
        ],
        "model": "random_forest"  # optional
    }
    """
    try:
        data = request.get_json()

        if not data or 'matches' not in data:
            return jsonify({'error': 'Keine Spiele übermittelt'}), 400

        matches = data.get('matches', [])
        model_type = data.get('model', 'random_forest')

        if model_type not in predictors:
            return jsonify({
                'error': f'Modell {model_type} nicht verfügbar'
            }), 400

        predictor = predictors[model_type]

        # Vorhersagen für alle Spiele
        predictions = []
        for match in matches:
            home_team = match.get('home_team')
            away_team = match.get('away_team')

            if not home_team or not away_team:
                continue

            result, probs = predictor.predict_match(home_team, away_team)

            predictions.append({
                'match': {
                    'home_team': home_team,
                    'away_team': away_team
                },
                'prediction': result,
                'probabilities': {
                    'home_win': float(probs[0]),
                    'draw': float(probs[1]),
                    'away_win': float(probs[2])
                },
                'confidence': float(max(probs))
            })

        return jsonify({
            'predictions': predictions,
            'count': len(predictions),
            'model': model_type
        })

    except Exception as e:
        return jsonify({
            'error': 'Interner Fehler',
            'message': str(e)
        }), 500


def main():
    """Startet API Server"""
    print("\n" + "="*60)
    print("Sportwetten Vorhersage API")
    print("="*60)
    print(f"\nVerfügbare Modelle: {list(predictors.keys())}")
    print("\nStarte Server auf http://localhost:5000")
    print("Drücke CTRL+C zum Beenden\n")

    app.run(host='0.0.0.0', port=5000, debug=True)


if __name__ == '__main__':
    main()
