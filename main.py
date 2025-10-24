#!/usr/bin/env python3
"""
Hauptskript für Sportwetten-Vorhersage Pipeline
Führt alle Schritte aus: Datensammlung, Preprocessing, Training, Vorhersagen
"""

import sys
import argparse
from pathlib import Path

# Füge src zum Path hinzu
sys.path.insert(0, str(Path(__file__).parent))

from src.data.collect_data import SportsDataCollector
from src.data.preprocessing import DataPreprocessor
from src.models.train_model import SportsPredictor, train_all_models
from src.models.predict import MatchPredictor


def run_full_pipeline():
    """Führt die komplette Pipeline aus"""
    print("="*60)
    print("SPORTWETTEN VORHERSAGE - VOLLSTÄNDIGE PIPELINE")
    print("="*60)

    # Schritt 1: Datensammlung
    print("\n[1/4] Datensammlung...")
    print("-"*60)
    collector = SportsDataCollector()
    matches_df = collector.generate_sample_data(num_matches=1000)
    collector.save_data(matches_df, "matches.csv")
    team_stats = collector.collect_team_stats(matches_df)
    collector.save_data(team_stats, "team_stats.csv")

    # Schritt 2: Preprocessing
    print("\n[2/4] Datenvorverarbeitung...")
    print("-"*60)
    preprocessor = DataPreprocessor()
    matches_df, team_stats_df = preprocessor.load_raw_data()
    features_df = preprocessor.create_features(matches_df, team_stats_df)
    X, y = preprocessor.prepare_for_training(features_df)
    preprocessor.save_processed_data(features_df, X, y)

    # Schritt 3: Model Training
    print("\n[3/4] Model Training...")
    print("-"*60)
    train_all_models()

    # Schritt 4: Beispiel-Vorhersagen
    print("\n[4/4] Beispiel-Vorhersagen...")
    print("-"*60)
    predictor = MatchPredictor(model_type='random_forest')

    example_matches = [
        ("Bayern München", "Borussia Dortmund"),
        ("RB Leipzig", "Bayer Leverkusen"),
        ("Union Berlin", "SC Freiburg"),
    ]

    for home, away in example_matches:
        result, probs = predictor.predict_match(home, away)
        predictor.print_prediction(home, away, result, probs)

    print("\n" + "="*60)
    print("PIPELINE ABGESCHLOSSEN!")
    print("="*60)
    print("\nNächste Schritte:")
    print("- API starten: python src/api/app.py")
    print("- Vorhersage: python src/models/predict.py --match 'Team A vs Team B'")
    print("="*60 + "\n")


def main():
    """Hauptfunktion mit CLI-Optionen"""
    parser = argparse.ArgumentParser(
        description='Sportwetten Vorhersage KI-System',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Beispiele:
  python main.py --full              # Führt komplette Pipeline aus
  python main.py --collect           # Nur Datensammlung
  python main.py --preprocess        # Nur Preprocessing
  python main.py --train             # Nur Training
  python main.py --predict           # Beispiel-Vorhersagen
  python main.py --api               # Startet API Server
        """
    )

    parser.add_argument('--full', action='store_true',
                       help='Führt komplette Pipeline aus')
    parser.add_argument('--collect', action='store_true',
                       help='Datensammlung')
    parser.add_argument('--preprocess', action='store_true',
                       help='Datenvorverarbeitung')
    parser.add_argument('--train', action='store_true',
                       help='Model Training')
    parser.add_argument('--predict', action='store_true',
                       help='Beispiel-Vorhersagen')
    parser.add_argument('--api', action='store_true',
                       help='Startet API Server')

    args = parser.parse_args()

    # Wenn keine Argumente, zeige Hilfe
    if not any(vars(args).values()):
        parser.print_help()
        return

    try:
        if args.full:
            run_full_pipeline()

        elif args.collect:
            print("=== Datensammlung ===\n")
            collector = SportsDataCollector()
            matches_df = collector.generate_sample_data(num_matches=1000)
            collector.save_data(matches_df, "matches.csv")
            team_stats = collector.collect_team_stats(matches_df)
            collector.save_data(team_stats, "team_stats.csv")

        elif args.preprocess:
            print("=== Datenvorverarbeitung ===\n")
            preprocessor = DataPreprocessor()
            matches_df, team_stats_df = preprocessor.load_raw_data()
            features_df = preprocessor.create_features(matches_df, team_stats_df)
            X, y = preprocessor.prepare_for_training(features_df)
            preprocessor.save_processed_data(features_df, X, y)

        elif args.train:
            print("=== Model Training ===\n")
            train_all_models()

        elif args.predict:
            print("=== Beispiel-Vorhersagen ===\n")
            predictor = MatchPredictor(model_type='random_forest')

            example_matches = [
                ("Bayern München", "Borussia Dortmund"),
                ("RB Leipzig", "Bayer Leverkusen"),
            ]

            for home, away in example_matches:
                result, probs = predictor.predict_match(home, away)
                predictor.print_prediction(home, away, result, probs)

        elif args.api:
            from src.api.app import main as api_main
            api_main()

    except Exception as e:
        print(f"\n❌ Fehler: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
