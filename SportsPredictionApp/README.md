# Sports Prediction App

Eine iOS-App zur Vorhersage von Sportergebnissen mit künstlicher Intelligenz.

![Swift](https://img.shields.io/badge/Swift-5.9-orange.svg)
![iOS](https://img.shields.io/badge/iOS-17.0+-blue.svg)
![SwiftUI](https://img.shields.io/badge/SwiftUI-Latest-green.svg)

## 📱 Überblick

Die **Sports Prediction App** ist eine iOS-Anwendung, die maschinelle Lernalgorithmen verwendet, um Ergebnisse von Sportveranstaltungen vorherzusagen. Die App bietet detaillierte Analysen, Statistiken und Vorhersagewahrscheinlichkeiten für verschiedene Sportarten.

## ✨ Features

### 🎯 Kernfunktionen
- **KI-gestützte Vorhersagen**: Fortschrittliche Algorithmen analysieren Team-Statistiken, Form, Heim-/Auswärtsbilanz und weitere Faktoren
- **Mehrere Sportarten**: Unterstützung für Fußball, Basketball, Tennis, Eishockey und Handball
- **Detaillierte Analysen**: Umfassende Team-Statistiken und Leistungsvergleiche
- **Echtzeit-Updates**: Live-Status von Sportveranstaltungen
- **Genauigkeits-Tracking**: Überwachung der Vorhersage-Genauigkeit über Zeit

### 📊 Vorhersage-Algorithmen
Die App verwendet verschiedene ML-Algorithmen:
- **Ensemble-Methode** (empfohlen): Kombiniert mehrere Modelle für bessere Genauigkeit
- **Neuronales Netzwerk**: Deep Learning basierte Vorhersagen
- **Random Forest**: Entscheidungsbaum-basierte Klassifikation
- **Logistische Regression**: Statistische Vorhersagemodelle

### 📈 Berücksichtigte Faktoren
- Aktuelle Form (letzte 5 Spiele)
- Tordifferenz / Punktdifferenz
- Siegquote
- Heim- und Auswärtsbilanz
- Tabellenposition
- Head-to-Head Statistiken
- Spielort (Heimvorteil)

## 🏗️ Architektur

Die App folgt dem **MVVM (Model-View-ViewModel)** Architekturmuster:

```
SportsPredictionApp/
├── SportsPredictionApp/
│   ├── SportsPredictionApp.swift      # App Entry Point
│   ├── Models/                         # Datenmodelle
│   │   ├── SportEvent.swift           # Sportveranstaltung Model
│   │   ├── Team.swift                 # Team Model
│   │   └── Prediction.swift           # Vorhersage Model
│   ├── Views/                          # SwiftUI Views
│   │   ├── ContentView.swift          # Haupt-Tab View
│   │   ├── EventsListView.swift       # Events Liste
│   │   ├── EventDetailView.swift      # Event Details
│   │   ├── EventRowView.swift         # Event Zeile
│   │   ├── StatisticsView.swift       # Statistiken
│   │   └── SettingsView.swift         # Einstellungen
│   ├── ViewModels/                     # Business Logik
│   │   └── SportsViewModel.swift      # Haupt ViewModel
│   ├── Services/                       # Services
│   │   ├── SportsService.swift        # API Integration
│   │   └── PredictionService.swift    # Vorhersage Engine
│   ├── Utils/                          # Hilfsfunktionen
│   │   └── DateFormatters.swift       # Datum Formatierung
│   └── Info.plist                      # App Konfiguration
└── README.md                           # Diese Datei
```

## 🚀 Installation & Setup

### Voraussetzungen
- **macOS** 14.0 oder neuer
- **Xcode** 15.0 oder neuer
- **iOS Simulator** oder physisches Gerät mit iOS 17.0+
- **Apple Developer Account** (für Geräte-Tests)

### Schritte

1. **Repository klonen**
   ```bash
   cd SportsPredictionApp
   ```

2. **Xcode öffnen**
   ```bash
   open SportsPredictionApp.xcodeproj
   ```

3. **Dependencies installieren** (falls vorhanden)
   - Die App verwendet nur native iOS-Frameworks
   - Keine zusätzlichen Pakete erforderlich

4. **Projekt konfigurieren**
   - Wählen Sie Ihr Entwicklungsteam in den Signing-Einstellungen
   - Ändern Sie die Bundle Identifier wenn nötig

5. **Build & Run**
   - Wählen Sie einen Simulator oder Ihr Gerät
   - Drücken Sie `⌘ + R` oder klicken Sie auf den Play-Button

## 🎨 UI/UX Design

Die App verwendet **SwiftUI** für eine moderne, native iOS-Erfahrung:

- **Tab-basierte Navigation**: Schneller Zugriff auf Events, Statistiken und Einstellungen
- **Responsive Design**: Funktioniert auf iPhone und iPad
- **Dark Mode Support**: Automatische Anpassung an Systemeinstellungen
- **Animationen**: Flüssige Übergänge und Interaktionen
- **SF Symbols**: Native iOS-Icons für konsistente UI

## 🔮 Vorhersage-Engine

### Algorithmus-Details

Die Vorhersage-Engine analysiert mehrere Faktoren:

```swift
// Gewichtung der Faktoren
- Win Rate: 25%
- Aktuelle Form: 20%
- Tordifferenz: 15%
- Heimvorteil: 15%
- Heim/Auswärts Record: 25%
```

### Berechnungsbeispiel

Für ein Spiel Bayern München vs. Borussia Dortmund:

1. **Statistik-Analyse**: Sammlung von Team-Daten
2. **Faktor-Berechnung**: Bewertung jedes Faktors
3. **Wahrscheinlichkeits-Berechnung**: Gewichtete Kombination
4. **Ergebnis-Vorhersage**: Score-Prediction basierend auf Wahrscheinlichkeiten

```
Ergebnis:
- Bayern München Sieg: 55.2%
- Unentschieden: 23.8%
- Dortmund Sieg: 21.0%
Vorhersage: 3:1 für Bayern München
```

## 📊 Datenquellen

### Mock Data (Entwicklung)
Die App enthält Mock-Daten für Entwicklung und Tests:
- Deutsche Bundesliga Teams
- NBA Teams
- Realistische Statistiken

### Produktions-API (Erweiterung)
Für den Produktionseinsatz können folgende APIs integriert werden:
- **API-Football**: Football/Soccer Daten
- **The Sports DB**: Multi-Sport Datenbank
- **ESPN API**: Umfassende Sport-Daten
- **Rapid API Sports**: Verschiedene Sportarten

### Integration einer echten API

Ersetzen Sie in `SportsService.swift`:

```swift
func fetchUpcomingEvents() async throws -> [SportEvent] {
    let url = URL(string: "https://api.sports-data.com/v1/events")!
    var request = URLRequest(url: url)
    request.addValue("YOUR_API_KEY", forHTTPHeaderField: "Authorization")

    let (data, _) = try await session.data(for: request)
    let events = try JSONDecoder().decode([SportEvent].self, from: data)
    return events
}
```

## 🧪 Testing

### Unit Tests
```bash
# Tests ausführen
⌘ + U in Xcode
```

### Test Coverage
- Model Tests: Datenmodell-Validierung
- ViewModel Tests: Business-Logik-Tests
- Service Tests: API und Vorhersage-Tests

## 📱 Screenshots

### Events List
Liste aller kommenden und abgeschlossenen Sportveranstaltungen mit Filteroptionen nach Sportart.

### Event Detail
Detaillierte Ansicht mit:
- Team-Informationen und Statistiken
- KI-Vorhersage mit Wahrscheinlichkeiten
- Einflussfaktoren-Analyse
- Vergleich der Team-Performance

### Statistics
Übersicht über:
- Vorhersage-Genauigkeit
- Verteilung nach Sportart
- Verlauf der letzten Vorhersagen

### Settings
Anpassbare Einstellungen für:
- Vorhersage-Algorithmus
- Benachrichtigungen
- App-Präferenzen

## 🔄 Zukünftige Erweiterungen

### Geplante Features
- [ ] Push-Benachrichtigungen für Live-Spiele
- [ ] Benutzer-Favoriten für Teams
- [ ] Social Sharing von Vorhersagen
- [ ] Historische Daten-Analyse
- [ ] Wett-Tipps Integration
- [ ] Apple Watch App
- [ ] Widgets für Home Screen
- [ ] Siri Shortcuts
- [ ] iCloud Sync

### Algorithmus-Verbesserungen
- [ ] Integration von CoreML Models
- [ ] Create ML basiertes Training mit echten Daten
- [ ] Erweiterte Statistik-Features (Spieler-Form, Verletzungen)
- [ ] Wetter- und Platzverhältnisse
- [ ] Schiedsrichter-Statistiken

## 🛠️ Technologien

- **Swift 5.9**: Programmiersprache
- **SwiftUI**: UI Framework
- **Combine**: Reaktive Programmierung
- **Foundation**: Core Frameworks
- **Async/Await**: Moderne Concurrency
- **MVVM**: Architekturmuster

## 📝 Lizenz

© 2025 Sports Prediction App. Alle Rechte vorbehalten.

## 👨‍💻 Entwicklung

### Entwickelt mit
- Moderne SwiftUI-Best-Practices
- Type-Safe API Design
- Async/Await für asynchrone Operationen
- Environment Objects für State Management
- App Storage für Persistenz

### Code-Qualität
- Swift Style Guide konform
- Dokumentierte Funktionen
- Modulare Architektur
- Wiederverwendbare Components

## 🆘 Support

Bei Fragen oder Problemen:
1. Überprüfen Sie die Dokumentation
2. Konsultieren Sie die Code-Kommentare
3. Erstellen Sie ein Issue im Repository

## 🌟 Credits

Entwickelt als moderne iOS-App-Demonstration für KI-gestützte Sportvorhersagen.

---

**Hinweis**: Diese App ist ein Demonstrationsprojekt. Für den Produktionseinsatz sollten echte Sport-APIs integriert und erweiterte ML-Modelle mit historischen Daten trainiert werden.
