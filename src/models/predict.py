"""
Vorhersage-Modul für Sportwetten
Macht Vorhersagen mit trainierten Modellen
"""

import pandas as pd
import numpy as np
from pathlib import Path
import joblib
import json
import argparse


class MatchPredictor:
    """Macht Vorhersagen für Spiele"""

    def __init__(self, model_type="random_forest", model_dir="models"):
        self.model_type = model_type
        self.model_dir = Path(model_dir)
        self.model = None
        self.feature_names = None
        self.load_model()

    def load_model(self):
        """Lädt trainiertes Modell"""
        model_file = self.model_dir / f"{self.model_type}_model.pkl"

        if not model_file.exists():
            raise FileNotFoundError(
                f"Modell nicht gefunden: {model_file}\n"
                "Bitte zuerst Modell trainieren mit: python src/models/train_model.py"
            )

        self.model = joblib.load(model_file)

        # Lade Feature Names
        features_file = self.model_dir / f"{self.model_type}_features.json"
        if features_file.exists():
            with open(features_file, 'r') as f:
                data = json.load(f)
                self.feature_names = data['features']

        print(f"Modell geladen: {self.model_type}")

    def predict_match(self, home_team, away_team, features=None):
        """
        Vorhersage für ein einzelnes Spiel

        Args:
            home_team: Heimteam
            away_team: Auswärtsteam
            features: Dict mit Features (optional, sonst Durchschnittswerte)

        Returns:
            prediction: 'Home Win', 'Draw', oder 'Away Win'
            probabilities: Wahrscheinlichkeiten für jedes Ergebnis
        """
        if features is None:
            # Verwende Durchschnittswerte wenn keine Features gegeben
            features = self._get_default_features()

        # Erstelle Feature-Vektor
        X = pd.DataFrame([features])

        # Stelle sicher, dass alle Features vorhanden sind
        for feature in self.feature_names:
            if feature not in X.columns:
                X[feature] = 0

        # Sortiere Features in richtiger Reihenfolge
        X = X[self.feature_names]

        # Vorhersage
        prediction = self.model.predict(X)[0]
        probabilities = self.model.predict_proba(X)[0]

        # Mappe Prediction zu Label
        result_map = {0: 'Home Win', 1: 'Draw', 2: 'Away Win'}
        result = result_map[prediction]

        return result, probabilities

    def _get_default_features(self):
        """Gibt Standard-Feature-Werte zurück"""
        # Diese Werte sollten aus historischen Daten berechnet werden
        # Hier verwenden wir neutrale Durchschnittswerte
        return {
            'home_win_rate': 0.45,
            'away_win_rate': 0.40,
            'home_avg_goals_scored': 1.5,
            'away_avg_goals_scored': 1.3,
            'home_avg_goals_conceded': 1.2,
            'away_avg_goals_conceded': 1.4,
            'home_goal_difference': 0.3,
            'away_goal_difference': -0.1,
            'home_form': 1.5,
            'away_form': 1.3,
            'form_difference': 0.2,
            'h2h_home_wins': 2,
            'h2h_away_wins': 1,
            'h2h_draws': 2,
            'home_possession': 52.0,
            'away_possession': 48.0,
            'home_shots': 12,
            'away_shots': 10,
        }

    def predict_matches_batch(self, matches_df):
        """Vorhersagen für mehrere Spiele"""
        predictions = []

        for _, match in matches_df.iterrows():
            result, probs = self.predict_match(
                match.get('home_team', 'Unknown'),
                match.get('away_team', 'Unknown'),
                match.to_dict() if 'home_win_rate' in match else None
            )

            predictions.append({
                'home_team': match.get('home_team', 'Unknown'),
                'away_team': match.get('away_team', 'Unknown'),
                'prediction': result,
                'prob_home_win': probs[0],
                'prob_draw': probs[1],
                'prob_away_win': probs[2],
                'confidence': max(probs)
            })

        return pd.DataFrame(predictions)

    def print_prediction(self, home_team, away_team, result, probabilities):
        """Gibt Vorhersage formatiert aus"""
        print(f"\n{'='*60}")
        print(f"Spiel: {home_team} vs {away_team}")
        print(f"{'='*60}")
        print(f"\nVorhersage: {result}")
        print(f"\nWahrscheinlichkeiten:")
        print(f"  Heimsieg:       {probabilities[0]*100:.2f}%")
        print(f"  Unentschieden:  {probabilities[1]*100:.2f}%")
        print(f"  Auswärtssieg:   {probabilities[2]*100:.2f}%")
        print(f"\nKonfidenz: {max(probabilities)*100:.2f}%")
        print(f"{'='*60}\n")


def main():
    """Hauptfunktion für Vorhersagen"""
    parser = argparse.ArgumentParser(description='Sportwetten Vorhersage')
    parser.add_argument('--match', type=str, help='Spiel im Format "Team A vs Team B"')
    parser.add_argument('--model', type=str, default='random_forest',
                       choices=['random_forest', 'xgboost'],
                       help='Modelltyp')

    args = parser.parse_args()

    print("=== Sportwetten Vorhersage ===\n")

    try:
        predictor = MatchPredictor(model_type=args.model)

        if args.match:
            # Parse Spiel
            parts = args.match.split(' vs ')
            if len(parts) != 2:
                print("Fehler: Spiel muss im Format 'Team A vs Team B' sein")
                return

            home_team = parts[0].strip()
            away_team = parts[1].strip()

            # Vorhersage
            result, probabilities = predictor.predict_match(home_team, away_team)
            predictor.print_prediction(home_team, away_team, result, probabilities)

        else:
            # Beispiel-Vorhersagen
            example_matches = [
                ("Bayern München", "Borussia Dortmund"),
                ("RB Leipzig", "Bayer Leverkusen"),
                ("Union Berlin", "SC Freiburg"),
            ]

            print("Beispiel-Vorhersagen:\n")

            for home, away in example_matches:
                result, probs = predictor.predict_match(home, away)
                predictor.print_prediction(home, away, result, probs)

    except FileNotFoundError as e:
        print(f"Fehler: {e}")
        print("\nBitte zuerst folgende Schritte ausführen:")
        print("1. python src/data/collect_data.py")
        print("2. python src/data/preprocessing.py")
        print("3. python src/models/train_model.py")


if __name__ == "__main__":
    main()
