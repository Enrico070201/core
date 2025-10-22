//
//  SettingsView.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import SwiftUI

struct SettingsView: View {
    @AppStorage("notificationsEnabled") private var notificationsEnabled = true
    @AppStorage("predictionAlgorithm") private var selectedAlgorithm = "ensemble"
    @AppStorage("autoRefresh") private var autoRefresh = false

    var body: some View {
        NavigationView {
            Form {
                // App Information
                Section(header: Text("App-Informationen")) {
                    HStack {
                        Text("Version")
                        Spacer()
                        Text("1.0.0")
                            .foregroundColor(.secondary)
                    }
                    HStack {
                        Text("Build")
                        Spacer()
                        Text("2025.1")
                            .foregroundColor(.secondary)
                    }
                }

                // Prediction Settings
                Section(header: Text("Vorhersage-Einstellungen")) {
                    Picker("Algorithmus", selection: $selectedAlgorithm) {
                        Text("Ensemble-Methode").tag("ensemble")
                        Text("Neuronales Netzwerk").tag("neural")
                        Text("Random Forest").tag("forest")
                        Text("Logistische Regression").tag("regression")
                    }

                    Toggle("Automatische Aktualisierung", isOn: $autoRefresh)
                }

                // Notifications
                Section(header: Text("Benachrichtigungen")) {
                    Toggle("Benachrichtigungen aktiviert", isOn: $notificationsEnabled)

                    if notificationsEnabled {
                        NavigationLink("Benachrichtigungs-Einstellungen") {
                            NotificationSettingsView()
                        }
                    }
                }

                // Data & Privacy
                Section(header: Text("Daten & Datenschutz")) {
                    Button("Datenschutzerklärung") {
                        // Open privacy policy
                    }
                    Button("Nutzungsbedingungen") {
                        // Open terms of service
                    }
                    Button("Cache löschen") {
                        // Clear cache
                    }
                    .foregroundColor(.red)
                }

                // About
                Section(header: Text("Über")) {
                    NavigationLink("Entwicklerinfo") {
                        DeveloperInfoView()
                    }
                    NavigationLink("Lizenzen") {
                        LicensesView()
                    }
                }
            }
            .navigationTitle("Einstellungen")
        }
    }
}

struct NotificationSettingsView: View {
    @AppStorage("notifyBeforeMatch") private var notifyBeforeMatch = true
    @AppStorage("notifyMatchStart") private var notifyMatchStart = true
    @AppStorage("notifyResults") private var notifyResults = true
    @AppStorage("minutesBefore") private var minutesBefore = 30.0

    var body: some View {
        Form {
            Section(header: Text("Benachrichtigungs-Typen")) {
                Toggle("Vor dem Spiel", isOn: $notifyBeforeMatch)
                if notifyBeforeMatch {
                    VStack(alignment: .leading) {
                        Text("Minuten vorher: \(Int(minutesBefore))")
                            .font(.caption)
                            .foregroundColor(.secondary)
                        Slider(value: $minutesBefore, in: 5...120, step: 5)
                    }
                }
                Toggle("Bei Spielbeginn", isOn: $notifyMatchStart)
                Toggle("Bei Ergebnissen", isOn: $notifyResults)
            }
        }
        .navigationTitle("Benachrichtigungen")
    }
}

struct DeveloperInfoView: View {
    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                Image(systemName: "person.circle.fill")
                    .font(.system(size: 80))
                    .foregroundColor(.blue)

                Text("Sports Prediction App")
                    .font(.title2)
                    .bold()

                Text("Eine KI-gestützte App zur Vorhersage von Sportergebnissen")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal)

                Divider()
                    .padding(.vertical)

                VStack(alignment: .leading, spacing: 15) {
                    Text("Entwickelt mit:")
                        .font(.headline)

                    TechnologyRow(icon: "swift", name: "SwiftUI", description: "Moderne UI-Entwicklung")
                    TechnologyRow(icon: "brain", name: "Machine Learning", description: "KI-Vorhersage-Algorithmen")
                    TechnologyRow(icon: "chart.bar", name: "Data Analytics", description: "Statistische Analyse")
                }
                .padding()

                Divider()

                Text("© 2025 Sports Prediction App")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            .padding()
        }
        .navigationTitle("Entwicklerinfo")
    }
}

struct TechnologyRow: View {
    let icon: String
    let name: String
    let description: String

    var body: some View {
        HStack(spacing: 15) {
            Image(systemName: icon)
                .font(.title2)
                .foregroundColor(.blue)
                .frame(width: 40)

            VStack(alignment: .leading, spacing: 3) {
                Text(name)
                    .font(.subheadline)
                    .bold()
                Text(description)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
    }
}

struct LicensesView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                Text("Open Source Lizenzen")
                    .font(.headline)
                    .padding(.bottom, 10)

                LicenseBlock(
                    library: "SwiftUI",
                    license: "Apple SDK License"
                )

                LicenseBlock(
                    library: "Combine",
                    license: "Apple SDK License"
                )

                LicenseBlock(
                    library: "Foundation",
                    license: "Apple SDK License"
                )
            }
            .padding()
        }
        .navigationTitle("Lizenzen")
    }
}

struct LicenseBlock: View {
    let library: String
    let license: String

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(library)
                .font(.subheadline)
                .bold()
            Text(license)
                .font(.caption)
                .foregroundColor(.secondary)
            Divider()
        }
    }
}

struct SettingsView_Previews: PreviewProvider {
    static var previews: some View {
        SettingsView()
    }
}
