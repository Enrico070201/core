# Xcode Setup Anleitung

Da dieses Projekt in einer Linux-Umgebung erstellt wurde, müssen Sie es in Xcode auf macOS öffnen, um es vollständig zu konfigurieren.

## Schnellstart

1. **Projekt in Xcode öffnen**
   ```bash
   cd SportsPredictionApp
   open SportsPredictionApp.xcodeproj
   ```

2. **Neues Xcode-Projekt erstellen** (empfohlen)

   Da die `.xcodeproj` Datei vereinfacht ist, ist es am besten, ein neues Xcode-Projekt zu erstellen:

   a. Öffnen Sie Xcode
   b. Wählen Sie "Create a new Xcode project"
   c. Wählen Sie "iOS" → "App"
   d. Konfiguration:
      - Product Name: `SportsPredictionApp`
      - Interface: `SwiftUI`
      - Language: `Swift`
      - Organization Identifier: Ihre eigene

   e. Speichern Sie es im gleichen Ordner

3. **Dateien hinzufügen**

   Fügen Sie die vorhandenen Swift-Dateien zum Projekt hinzu:

   - Rechtsklick auf den Projektordner in Xcode
   - "Add Files to SportsPredictionApp..."
   - Wählen Sie die Ordner:
     - Models/
     - Views/
     - ViewModels/
     - Services/
     - Utils/
   - Aktivieren Sie "Copy items if needed"
   - "Create groups" auswählen
   - Klicken Sie "Add"

4. **Dateien strukturieren**

   Organisieren Sie die Dateien in Xcode Navigator:
   ```
   SportsPredictionApp/
   ├── SportsPredictionApp.swift
   ├── Models/
   │   ├── SportEvent.swift
   │   ├── Team.swift
   │   └── Prediction.swift
   ├── Views/
   │   ├── ContentView.swift
   │   ├── EventsListView.swift
   │   ├── EventDetailView.swift
   │   ├── EventRowView.swift
   │   ├── StatisticsView.swift
   │   └── SettingsView.swift
   ├── ViewModels/
   │   └── SportsViewModel.swift
   ├── Services/
   │   ├── SportsService.swift
   │   └── PredictionService.swift
   └── Utils/
       └── DateFormatters.swift
   ```

5. **Info.plist konfigurieren**

   - Die Info.plist ist bereits vorhanden
   - Xcode sollte sie automatisch erkennen
   - Falls nicht: Target → Build Settings → Info.plist File

6. **Assets hinzufügen**

   a. Erstellen Sie Asset Catalog:
      - File → New → File → Asset Catalog
      - Name: `Assets.xcassets`

   b. Fügen Sie App-Icons hinzu:
      - Klicken Sie auf Assets.xcassets
      - Fügen Sie AppIcon hinzu
      - Laden Sie Icons in verschiedenen Größen hoch

7. **Launch Screen konfigurieren**

   Xcode erstellt automatisch einen Launch Screen. Passen Sie ihn an:
   - Wählen Sie LaunchScreen.storyboard
   - Fügen Sie Ihr App-Logo hinzu

8. **Bundle Identifier setzen**

   - Wählen Sie das Projekt im Navigator
   - Wählen Sie das Target
   - General → Identity → Bundle Identifier
   - Setzen Sie z.B.: `com.ihrefirma.sportspredictionapp`

9. **Signing & Capabilities**

   - Wählen Sie Ihr Development Team
   - Automatisches Signing aktivieren

   Falls Sie Capabilities benötigen:
   - Push Notifications
   - Background Modes (für Live-Updates)

10. **Build Settings überprüfen**

    - iOS Deployment Target: 17.0 oder höher
    - Swift Language Version: Swift 5
    - Optimization Level: -Onone (Debug), -O (Release)

## Build & Run

1. **Simulator auswählen**
   - Toolbar: Wählen Sie iPhone 15 Pro oder ähnlich
   - Oder ein physisches Gerät

2. **Projekt builden**
   ```
   ⌘ + B
   ```

3. **App ausführen**
   ```
   ⌘ + R
   ```

## Häufige Probleme & Lösungen

### Problem: "Cannot find 'SportsViewModel' in scope"

**Lösung**: Stellen Sie sicher, dass alle Dateien zum Target hinzugefügt sind:
- Wählen Sie die Datei
- File Inspector (rechtes Panel)
- Target Membership: Aktivieren Sie SportsPredictionApp

### Problem: Build Errors in SwiftUI Preview

**Lösung**:
```swift
// Fügen Sie Preview-Provider-Mock hinzu
struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
            .environmentObject(SportsViewModel())
    }
}
```

### Problem: Info.plist nicht gefunden

**Lösung**:
- Target → Build Settings
- Suchen Sie "Info.plist File"
- Setzen Sie Pfad: `SportsPredictionApp/Info.plist`

### Problem: Module-Import Fehler

**Lösung**: Stellen Sie sicher, dass alle Imports korrekt sind:
```swift
import SwiftUI
import Foundation
import Combine
```

## Projekt-Struktur in Xcode

Nach dem Setup sollte Ihre Xcode-Projektstruktur so aussehen:

```
SportsPredictionApp (Projekt)
├── SportsPredictionApp (Target)
│   ├── SportsPredictionApp.swift
│   ├── ContentView.swift
│   ├── Models
│   ├── Views
│   ├── ViewModels
│   ├── Services
│   ├── Utils
│   ├── Assets.xcassets
│   └── Info.plist
└── Products
    └── SportsPredictionApp.app
```

## Tipps für die Entwicklung

1. **Xcode Shortcuts**
   - `⌘ + B`: Build
   - `⌘ + R`: Run
   - `⌘ + .`: Stop
   - `⌘ + Shift + K`: Clean Build Folder
   - `⌘ + Option + P`: Resume Preview

2. **SwiftUI Previews nutzen**
   - Jede View hat einen Preview Provider
   - Canvas öffnen: Editor → Canvas
   - Live Preview: Klicken Sie auf Play in der Preview

3. **Debugging**
   - Breakpoints setzen
   - `po` command in Console für Print-Output
   - View Debugger: Debug → View Debugging → Capture View Hierarchy

4. **Code-Formatierung**
   - Editor → Structure → Re-Indent (⌃ + I)

## Alternative: Command Line Build

Falls Sie lieber die Command Line nutzen:

```bash
# Build
xcodebuild -project SportsPredictionApp.xcodeproj \
           -scheme SportsPredictionApp \
           -configuration Debug \
           -destination 'platform=iOS Simulator,name=iPhone 15 Pro'

# Run Tests
xcodebuild test -project SportsPredictionApp.xcodeproj \
                -scheme SportsPredictionApp \
                -destination 'platform=iOS Simulator,name=iPhone 15 Pro'
```

## Fertig!

Jetzt sollte Ihre App bereit sein zum Entwickeln und Testen. Viel Erfolg! 🚀
