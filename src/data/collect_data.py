"""
Datensammlung für Sportwetten-Vorhersagen
Sammelt historische Spieldaten, Teamstatistiken und weitere relevante Daten
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import requests
import json
import os
from pathlib import Path


class SportsDataCollector:
    """Sammelt Sportdaten aus verschiedenen Quellen"""

    def __init__(self, data_dir="data/raw"):
        self.data_dir = Path(data_dir)
        self.data_dir.mkdir(parents=True, exist_ok=True)

    def generate_sample_data(self, num_matches=1000):
        """
        Generiert Beispieldaten für Demonstrationszwecke
        In der Produktion würde dies durch echte API-Aufrufe ersetzt werden
        """
        print(f"Generiere {num_matches} Beispiel-Spiele...")

        teams = [
            "Bayern München", "Borussia Dortmund", "RB Leipzig", "Bayer Leverkusen",
            "Union Berlin", "SC Freiburg", "Eintracht Frankfurt", "VfL Wolfsburg",
            "Borussia Mönchengladbach", "FSV Mainz 05", "1. FC Köln", "TSG Hoffenheim",
            "VfB Stuttgart", "Hertha BSC", "FC Augsburg", "VfL Bochum",
            "FC Schalke 04", "Werder Bremen"
        ]

        matches = []
        start_date = datetime.now() - timedelta(days=365*2)

        for i in range(num_matches):
            home_team = np.random.choice(teams)
            away_team = np.random.choice([t for t in teams if t != home_team])

            # Simuliere Teamstärken (einfaches Modell)
            home_strength = np.random.uniform(40, 90)
            away_strength = np.random.uniform(40, 90)

            # Heimvorteil
            home_advantage = 10

            # Tore basierend auf Stärke berechnen
            home_goals = max(0, int(np.random.poisson((home_strength + home_advantage) / 30)))
            away_goals = max(0, int(np.random.poisson(away_strength / 30)))

            # Ergebnis
            if home_goals > away_goals:
                result = "H"  # Home Win
            elif away_goals > home_goals:
                result = "A"  # Away Win
            else:
                result = "D"  # Draw

            match_date = start_date + timedelta(days=i)

            match = {
                'date': match_date.strftime('%Y-%m-%d'),
                'home_team': home_team,
                'away_team': away_team,
                'home_goals': home_goals,
                'away_goals': away_goals,
                'result': result,
                'home_shots': np.random.randint(5, 25),
                'away_shots': np.random.randint(5, 25),
                'home_shots_on_target': np.random.randint(2, 12),
                'away_shots_on_target': np.random.randint(2, 12),
                'home_possession': np.random.uniform(35, 75),
                'away_possession': 0,  # Wird berechnet
                'home_fouls': np.random.randint(5, 20),
                'away_fouls': np.random.randint(5, 20),
                'home_corners': np.random.randint(2, 12),
                'away_corners': np.random.randint(2, 12),
                'home_yellow_cards': np.random.randint(0, 5),
                'away_yellow_cards': np.random.randint(0, 5),
                'home_red_cards': np.random.randint(0, 2) if np.random.random() < 0.1 else 0,
                'away_red_cards': np.random.randint(0, 2) if np.random.random() < 0.1 else 0,
            }

            match['away_possession'] = 100 - match['home_possession']
            matches.append(match)

        df = pd.DataFrame(matches)
        return df

    def save_data(self, df, filename="matches.csv"):
        """Speichert Daten als CSV"""
        filepath = self.data_dir / filename
        df.to_csv(filepath, index=False)
        print(f"Daten gespeichert unter: {filepath}")
        return filepath

    def load_data(self, filename="matches.csv"):
        """Lädt Daten aus CSV"""
        filepath = self.data_dir / filename
        if filepath.exists():
            df = pd.read_csv(filepath)
            print(f"Daten geladen: {len(df)} Einträge")
            return df
        else:
            print(f"Datei nicht gefunden: {filepath}")
            return None

    def collect_team_stats(self, df):
        """Berechnet Teamstatistiken aus Spieldaten"""
        print("Berechne Teamstatistiken...")

        stats = []
        teams = set(df['home_team'].unique()) | set(df['away_team'].unique())

        for team in teams:
            home_matches = df[df['home_team'] == team]
            away_matches = df[df['away_team'] == team]
            all_matches = len(home_matches) + len(away_matches)

            if all_matches == 0:
                continue

            # Siege
            home_wins = len(home_matches[home_matches['result'] == 'H'])
            away_wins = len(away_matches[away_matches['result'] == 'A'])
            total_wins = home_wins + away_wins

            # Unentschieden
            home_draws = len(home_matches[home_matches['result'] == 'D'])
            away_draws = len(away_matches[away_matches['result'] == 'D'])
            total_draws = home_draws + away_draws

            # Niederlagen
            total_losses = all_matches - total_wins - total_draws

            # Tore
            goals_scored = (home_matches['home_goals'].sum() +
                          away_matches['away_goals'].sum())
            goals_conceded = (home_matches['away_goals'].sum() +
                            away_matches['home_goals'].sum())

            stats.append({
                'team': team,
                'matches_played': all_matches,
                'wins': total_wins,
                'draws': total_draws,
                'losses': total_losses,
                'win_rate': total_wins / all_matches if all_matches > 0 else 0,
                'goals_scored': goals_scored,
                'goals_conceded': goals_conceded,
                'goal_difference': goals_scored - goals_conceded,
                'avg_goals_scored': goals_scored / all_matches if all_matches > 0 else 0,
                'avg_goals_conceded': goals_conceded / all_matches if all_matches > 0 else 0,
            })

        stats_df = pd.DataFrame(stats)
        return stats_df


def main():
    """Hauptfunktion für Datensammlung"""
    print("=== Sportwetten Datensammlung ===\n")

    collector = SportsDataCollector()

    # Generiere Beispieldaten
    matches_df = collector.generate_sample_data(num_matches=1000)

    # Speichere Spieldaten
    collector.save_data(matches_df, "matches.csv")

    # Berechne und speichere Teamstatistiken
    team_stats = collector.collect_team_stats(matches_df)
    collector.save_data(team_stats, "team_stats.csv")

    print("\nDatensammlung abgeschlossen!")
    print(f"- Spiele: {len(matches_df)}")
    print(f"- Teams: {len(team_stats)}")


if __name__ == "__main__":
    main()
