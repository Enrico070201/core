"""
Training von ML-Modellen für Sportwetten-Vorhersagen
Unterstützt Random Forest, XGBoost und Neural Networks
"""

import pandas as pd
import numpy as np
from pathlib import Path
import joblib
import json
from datetime import datetime

from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
import xgboost as xgb


class SportsPredictor:
    """ML-Modell für Sportwetten-Vorhersagen"""

    def __init__(self, model_type="random_forest"):
        """
        Initialisiert Predictor

        Args:
            model_type: 'random_forest', 'xgboost', oder 'neural_network'
        """
        self.model_type = model_type
        self.model = None
        self.feature_names = None
        self.metrics = {}

    def create_model(self):
        """Erstellt ML-Modell basierend auf Typ"""
        if self.model_type == "random_forest":
            self.model = RandomForestClassifier(
                n_estimators=200,
                max_depth=15,
                min_samples_split=10,
                min_samples_leaf=5,
                random_state=42,
                n_jobs=-1
            )
        elif self.model_type == "xgboost":
            self.model = xgb.XGBClassifier(
                n_estimators=200,
                max_depth=8,
                learning_rate=0.1,
                objective='multi:softmax',
                num_class=3,
                random_state=42,
                n_jobs=-1
            )
        else:
            raise ValueError(f"Unbekannter Modelltyp: {self.model_type}")

        print(f"Modell erstellt: {self.model_type}")

    def load_data(self, data_dir="data/processed"):
        """Lädt vorverarbeitete Daten"""
        data_path = Path(data_dir)

        X = pd.read_csv(data_path / "X_train.csv")
        y = pd.read_csv(data_path / "y_train.csv")['result']

        self.feature_names = X.columns.tolist()

        print(f"Daten geladen: {X.shape[0]} Samples, {X.shape[1]} Features")
        return X, y

    def train(self, X, y, test_size=0.2):
        """Trainiert das Modell"""
        print(f"\nTrainiere {self.model_type} Modell...")

        # Split in Train/Test
        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=test_size, random_state=42, stratify=y
        )

        print(f"Trainingsdaten: {X_train.shape[0]} samples")
        print(f"Testdaten: {X_test.shape[0]} samples")

        # Training
        self.model.fit(X_train, y_train)

        # Evaluierung
        y_pred_train = self.model.predict(X_train)
        y_pred_test = self.model.predict(X_test)

        train_accuracy = accuracy_score(y_train, y_pred_train)
        test_accuracy = accuracy_score(y_test, y_pred_test)

        print(f"\nTraining Accuracy: {train_accuracy:.4f}")
        print(f"Test Accuracy: {test_accuracy:.4f}")

        # Speichere Metriken
        self.metrics = {
            'model_type': self.model_type,
            'train_accuracy': float(train_accuracy),
            'test_accuracy': float(test_accuracy),
            'train_samples': int(X_train.shape[0]),
            'test_samples': int(X_test.shape[0]),
            'n_features': int(X.shape[1]),
            'timestamp': datetime.now().isoformat()
        }

        # Classification Report
        print("\nClassification Report (Test Set):")
        print(classification_report(y_test, y_pred_test,
                                   target_names=['Home Win', 'Draw', 'Away Win']))

        # Feature Importance (falls verfügbar)
        if hasattr(self.model, 'feature_importances_'):
            self._print_feature_importance()

        return X_test, y_test, y_pred_test

    def _print_feature_importance(self, top_n=10):
        """Zeigt wichtigste Features"""
        if self.feature_names is None:
            return

        importances = self.model.feature_importances_
        indices = np.argsort(importances)[::-1]

        print(f"\nTop {top_n} wichtigste Features:")
        for i in range(min(top_n, len(indices))):
            idx = indices[i]
            print(f"{i+1}. {self.feature_names[idx]}: {importances[idx]:.4f}")

    def cross_validate(self, X, y, cv=5):
        """Führt Cross-Validation durch"""
        print(f"\nCross-Validation mit {cv} Folds...")

        scores = cross_val_score(self.model, X, y, cv=cv, n_jobs=-1)

        print(f"CV Scores: {scores}")
        print(f"Mean: {scores.mean():.4f} (+/- {scores.std() * 2:.4f})")

        self.metrics['cv_scores'] = scores.tolist()
        self.metrics['cv_mean'] = float(scores.mean())
        self.metrics['cv_std'] = float(scores.std())

    def predict(self, X):
        """Macht Vorhersagen"""
        if self.model is None:
            raise ValueError("Modell muss erst trainiert werden")

        predictions = self.model.predict(X)
        probabilities = self.model.predict_proba(X)

        return predictions, probabilities

    def save_model(self, output_dir="models"):
        """Speichert trainiertes Modell"""
        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        # Speichere Modell
        model_file = output_path / f"{self.model_type}_model.pkl"
        joblib.dump(self.model, model_file)
        print(f"\nModell gespeichert: {model_file}")

        # Speichere Metriken
        metrics_file = output_path / f"{self.model_type}_metrics.json"
        with open(metrics_file, 'w') as f:
            json.dump(self.metrics, f, indent=2)
        print(f"Metriken gespeichert: {metrics_file}")

        # Speichere Feature Names
        features_file = output_path / f"{self.model_type}_features.json"
        with open(features_file, 'w') as f:
            json.dump({'features': self.feature_names}, f, indent=2)

    def load_model(self, model_dir="models"):
        """Lädt gespeichertes Modell"""
        model_path = Path(model_dir)
        model_file = model_path / f"{self.model_type}_model.pkl"

        if not model_file.exists():
            raise FileNotFoundError(f"Modell nicht gefunden: {model_file}")

        self.model = joblib.load(model_file)

        # Lade Feature Names
        features_file = model_path / f"{self.model_type}_features.json"
        if features_file.exists():
            with open(features_file, 'r') as f:
                data = json.load(f)
                self.feature_names = data['features']

        print(f"Modell geladen: {model_file}")


def train_all_models():
    """Trainiert alle verfügbaren Modelle"""
    model_types = ["random_forest", "xgboost"]

    for model_type in model_types:
        print(f"\n{'='*60}")
        print(f"Trainiere {model_type.upper()} Modell")
        print(f"{'='*60}")

        predictor = SportsPredictor(model_type=model_type)
        predictor.create_model()

        # Lade Daten
        X, y = predictor.load_data()

        # Trainiere
        predictor.train(X, y)

        # Cross-Validation
        predictor.cross_validate(X, y)

        # Speichere
        predictor.save_model()


def main():
    """Hauptfunktion für Model Training"""
    print("=== Training von Sportwetten-Vorhersage Modellen ===\n")

    train_all_models()

    print("\n" + "="*60)
    print("Training abgeschlossen!")
    print("="*60)


if __name__ == "__main__":
    main()
