"""
Datenvorverarbeitung für ML-Modelle
Bereinigung, Transformation und Feature Engineering
"""

import pandas as pd
import numpy as np
from sklearn.preprocessing import StandardScaler, LabelEncoder
from pathlib import Path


class DataPreprocessor:
    """Verarbeitet Rohdaten für ML-Modelle"""

    def __init__(self):
        self.scaler = StandardScaler()
        self.label_encoder = LabelEncoder()

    def load_raw_data(self, data_dir="data/raw"):
        """Lädt Rohdaten"""
        data_path = Path(data_dir)
        matches_df = pd.read_csv(data_path / "matches.csv")
        team_stats_df = pd.read_csv(data_path / "team_stats.csv")
        return matches_df, team_stats_df

    def create_features(self, matches_df, team_stats_df):
        """
        Erstellt Features für ML-Modell
        - Form der letzten Spiele
        - Head-to-Head Statistiken
        - Teamstatistiken
        """
        print("Erstelle Features...")

        features_list = []

        for idx, match in matches_df.iterrows():
            home_team = match['home_team']
            away_team = match['away_team']

            # Hole Teamstatistiken
            home_stats = team_stats_df[team_stats_df['team'] == home_team]
            away_stats = team_stats_df[team_stats_df['team'] == away_team]

            if len(home_stats) == 0 or len(away_stats) == 0:
                continue

            home_stats = home_stats.iloc[0]
            away_stats = away_stats.iloc[0]

            # Berechne Form (letzte 5 Spiele)
            home_form = self._calculate_form(matches_df, home_team, match['date'], n=5)
            away_form = self._calculate_form(matches_df, away_team, match['date'], n=5)

            # Head-to-Head
            h2h_stats = self._calculate_h2h(matches_df, home_team, away_team, match['date'])

            features = {
                # Teamstatistiken
                'home_win_rate': home_stats['win_rate'],
                'away_win_rate': away_stats['win_rate'],
                'home_avg_goals_scored': home_stats['avg_goals_scored'],
                'away_avg_goals_scored': away_stats['avg_goals_scored'],
                'home_avg_goals_conceded': home_stats['avg_goals_conceded'],
                'away_avg_goals_conceded': away_stats['avg_goals_conceded'],
                'home_goal_difference': home_stats['goal_difference'],
                'away_goal_difference': away_stats['goal_difference'],

                # Form
                'home_form': home_form,
                'away_form': away_form,
                'form_difference': home_form - away_form,

                # Head-to-Head
                'h2h_home_wins': h2h_stats['home_wins'],
                'h2h_away_wins': h2h_stats['away_wins'],
                'h2h_draws': h2h_stats['draws'],

                # Spielstatistiken
                'home_possession': match['home_possession'],
                'away_possession': match['away_possession'],
                'home_shots': match['home_shots'],
                'away_shots': match['away_shots'],

                # Zielvariable
                'result': match['result']
            }

            features_list.append(features)

        features_df = pd.DataFrame(features_list)
        print(f"Features erstellt: {len(features_df)} Datensätze mit {len(features_df.columns)-1} Features")
        return features_df

    def _calculate_form(self, matches_df, team, current_date, n=5):
        """Berechnet Form der letzten n Spiele (3 Punkte für Sieg, 1 für Unentschieden)"""
        # Filter Spiele vor dem aktuellen Datum
        past_matches = matches_df[matches_df['date'] < current_date]

        # Spiele des Teams
        team_matches = past_matches[
            (past_matches['home_team'] == team) |
            (past_matches['away_team'] == team)
        ].tail(n)

        if len(team_matches) == 0:
            return 0

        points = 0
        for _, match in team_matches.iterrows():
            if match['home_team'] == team:
                if match['result'] == 'H':
                    points += 3
                elif match['result'] == 'D':
                    points += 1
            else:  # away team
                if match['result'] == 'A':
                    points += 3
                elif match['result'] == 'D':
                    points += 1

        return points / len(team_matches)

    def _calculate_h2h(self, matches_df, home_team, away_team, current_date, n=5):
        """Berechnet Head-to-Head Statistiken"""
        # Filter bisherige Begegnungen
        h2h_matches = matches_df[
            (matches_df['date'] < current_date) &
            (
                ((matches_df['home_team'] == home_team) & (matches_df['away_team'] == away_team)) |
                ((matches_df['home_team'] == away_team) & (matches_df['away_team'] == home_team))
            )
        ].tail(n)

        home_wins = 0
        away_wins = 0
        draws = 0

        for _, match in h2h_matches.iterrows():
            if match['result'] == 'D':
                draws += 1
            elif match['home_team'] == home_team and match['result'] == 'H':
                home_wins += 1
            elif match['away_team'] == home_team and match['result'] == 'A':
                home_wins += 1
            else:
                away_wins += 1

        return {
            'home_wins': home_wins,
            'away_wins': away_wins,
            'draws': draws
        }

    def prepare_for_training(self, features_df):
        """Bereitet Daten für Training vor"""
        print("Bereite Daten für Training vor...")

        # Trenne Features und Target
        X = features_df.drop('result', axis=1)
        y = features_df['result']

        # Encode Target (H=0, D=1, A=2)
        y_encoded = y.map({'H': 0, 'D': 1, 'A': 2})

        # Skaliere Features
        X_scaled = self.scaler.fit_transform(X)
        X_scaled_df = pd.DataFrame(X_scaled, columns=X.columns)

        print(f"Trainingsdaten vorbereitet: {X_scaled_df.shape}")
        return X_scaled_df, y_encoded

    def save_processed_data(self, features_df, X, y, output_dir="data/processed"):
        """Speichert verarbeitete Daten"""
        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        features_df.to_csv(output_path / "features.csv", index=False)
        X.to_csv(output_path / "X_train.csv", index=False)
        pd.DataFrame(y, columns=['result']).to_csv(output_path / "y_train.csv", index=False)

        print(f"Verarbeitete Daten gespeichert unter: {output_path}")


def main():
    """Hauptfunktion für Datenvorverarbeitung"""
    print("=== Datenvorverarbeitung ===\n")

    preprocessor = DataPreprocessor()

    # Lade Rohdaten
    matches_df, team_stats_df = preprocessor.load_raw_data()

    # Erstelle Features
    features_df = preprocessor.create_features(matches_df, team_stats_df)

    # Bereite für Training vor
    X, y = preprocessor.prepare_for_training(features_df)

    # Speichere verarbeitete Daten
    preprocessor.save_processed_data(features_df, X, y)

    print("\nDatenvorverarbeitung abgeschlossen!")


if __name__ == "__main__":
    main()
