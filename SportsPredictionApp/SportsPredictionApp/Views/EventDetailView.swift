//
//  EventDetailView.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import SwiftUI

struct EventDetailView: View {
    let event: SportEvent
    @EnvironmentObject var viewModel: SportsViewModel
    @State private var isRefreshing = false

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Header with teams
                eventHeaderView

                // Match info
                matchInfoView

                // Prediction section
                if let prediction = event.prediction {
                    predictionView(prediction: prediction)
                } else {
                    noPredictionView
                }

                // Team statistics comparison
                if event.homeTeam.stats != nil && event.awayTeam.stats != nil {
                    teamStatsComparisonView
                }

                // Actual result (if finished)
                if let result = event.actualResult {
                    actualResultView(result: result)
                }
            }
            .padding()
        }
        .navigationTitle(event.league)
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button(action: refreshPrediction) {
                    if isRefreshing {
                        ProgressView()
                    } else {
                        Image(systemName: "arrow.clockwise")
                    }
                }
                .disabled(isRefreshing || event.status == .finished)
            }
        }
    }

    // MARK: - Event Header

    private var eventHeaderView: some View {
        VStack(spacing: 15) {
            Text(event.sport.icon)
                .font(.system(size: 50))

            HStack(spacing: 30) {
                // Home team
                VStack(spacing: 8) {
                    Text(event.homeTeam.shortName)
                        .font(.title2)
                        .bold()
                    Text(event.homeTeam.name)
                        .font(.caption)
                        .foregroundColor(.secondary)
                        .multilineTextAlignment(.center)
                    if let rank = event.homeTeam.stats?.rank {
                        Text("Platz \(rank)")
                            .font(.caption2)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background(Color.blue.opacity(0.2))
                            .cornerRadius(8)
                    }
                }
                .frame(maxWidth: .infinity)

                // VS or Score
                if let result = event.actualResult {
                    VStack {
                        Text("\(result.homeScore):\(result.awayScore)")
                            .font(.system(size: 36, weight: .bold))
                            .foregroundColor(.primary)
                    }
                } else {
                    Text("VS")
                        .font(.title3)
                        .foregroundColor(.secondary)
                }

                // Away team
                VStack(spacing: 8) {
                    Text(event.awayTeam.shortName)
                        .font(.title2)
                        .bold()
                    Text(event.awayTeam.name)
                        .font(.caption)
                        .foregroundColor(.secondary)
                        .multilineTextAlignment(.center)
                    if let rank = event.awayTeam.stats?.rank {
                        Text("Platz \(rank)")
                            .font(.caption2)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background(Color.orange.opacity(0.2))
                            .cornerRadius(8)
                    }
                }
                .frame(maxWidth: .infinity)
            }
        }
        .padding()
        .background(Color(.systemGray6))
        .cornerRadius(15)
    }

    // MARK: - Match Info

    private var matchInfoView: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Match-Informationen")
                .font(.headline)

            InfoRow(icon: "calendar", title: "Datum", value: event.dateTime.formatted(date: .long, time: .omitted))
            InfoRow(icon: "clock", title: "Uhrzeit", value: event.dateTime.formatted(date: .omitted, time: .shortened))
            InfoRow(icon: "location", title: "Stadion", value: event.venue)
            InfoRow(icon: "flag", title: "Status", value: event.status.rawValue)
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(15)
        .shadow(color: Color.black.opacity(0.05), radius: 5)
    }

    // MARK: - Prediction View

    private func predictionView(prediction: Prediction) -> some View {
        VStack(alignment: .leading, spacing: 15) {
            HStack {
                Text("KI-Vorhersage")
                    .font(.headline)
                Spacer()
                Text(prediction.algorithm.rawValue)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            // Predicted winner
            HStack {
                VStack(alignment: .leading, spacing: 5) {
                    Text("Erwarteter Sieger")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                    Text(winnerText(prediction.predictedWinner))
                        .font(.title2)
                        .bold()
                }
                Spacer()
                VStack(alignment: .trailing, spacing: 5) {
                    Text("Konfidenz")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                    Text(prediction.confidencePercentage)
                        .font(.title2)
                        .bold()
                        .foregroundColor(confidenceColor(prediction.confidenceLevel))
                }
            }

            // Predicted score
            if let score = prediction.predictedScore {
                Divider()
                HStack {
                    Text("Vorhergesagtes Ergebnis")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                    Spacer()
                    Text("\(score.homeScore):\(score.awayScore)")
                        .font(.title3)
                        .bold()
                }
            }

            // Win probabilities
            Divider()
            Text("Wahrscheinlichkeiten")
                .font(.subheadline)
                .foregroundColor(.secondary)

            VStack(spacing: 10) {
                ProbabilityBar(
                    label: event.homeTeam.shortName,
                    probability: prediction.probabilities.homeWin,
                    color: .green
                )
                ProbabilityBar(
                    label: "Unentschieden",
                    probability: prediction.probabilities.draw,
                    color: .blue
                )
                ProbabilityBar(
                    label: event.awayTeam.shortName,
                    probability: prediction.probabilities.awayWin,
                    color: .orange
                )
            }

            // Prediction factors
            if !prediction.factors.isEmpty {
                Divider()
                Text("Einflussfaktoren")
                    .font(.subheadline)
                    .foregroundColor(.secondary)

                ForEach(prediction.factors) { factor in
                    FactorRow(factor: factor)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(15)
        .shadow(color: Color.black.opacity(0.05), radius: 5)
    }

    private var noPredictionView: some View {
        VStack(spacing: 15) {
            Image(systemName: "brain")
                .font(.system(size: 40))
                .foregroundColor(.gray)
            Text("Keine Vorhersage verfügbar")
                .font(.headline)
                .foregroundColor(.secondary)
            Button("Vorhersage generieren") {
                refreshPrediction()
            }
            .buttonStyle(.borderedProminent)
        }
        .padding()
        .frame(maxWidth: .infinity)
        .background(Color(.systemGray6))
        .cornerRadius(15)
    }

    // MARK: - Team Stats Comparison

    private var teamStatsComparisonView: some View {
        VStack(alignment: .leading, spacing: 15) {
            Text("Team-Statistiken")
                .font(.headline)

            if let homeStats = event.homeTeam.stats,
               let awayStats = event.awayTeam.stats {

                StatComparisonRow(
                    label: "Spiele",
                    homeValue: "\(homeStats.gamesPlayed)",
                    awayValue: "\(awayStats.gamesPlayed)"
                )
                StatComparisonRow(
                    label: "Siege",
                    homeValue: "\(homeStats.wins)",
                    awayValue: "\(awayStats.wins)",
                    homeHighlight: homeStats.wins > awayStats.wins,
                    awayHighlight: awayStats.wins > homeStats.wins
                )
                StatComparisonRow(
                    label: "Siegquote",
                    homeValue: String(format: "%.1f%%", homeStats.winPercentage),
                    awayValue: String(format: "%.1f%%", awayStats.winPercentage),
                    homeHighlight: homeStats.winPercentage > awayStats.winPercentage,
                    awayHighlight: awayStats.winPercentage > homeStats.winPercentage
                )
                StatComparisonRow(
                    label: "Tordifferenz",
                    homeValue: "\(homeStats.goalDifference > 0 ? "+" : "")\(homeStats.goalDifference)",
                    awayValue: "\(awayStats.goalDifference > 0 ? "+" : "")\(awayStats.goalDifference)",
                    homeHighlight: homeStats.goalDifference > awayStats.goalDifference,
                    awayHighlight: awayStats.goalDifference > homeStats.goalDifference
                )

                // Current form
                HStack {
                    Text("Form")
                        .font(.subheadline)
                        .frame(width: 100, alignment: .leading)
                    Spacer()
                    HStack(spacing: 3) {
                        ForEach(homeStats.currentForm.reversed(), id: \.self) { outcome in
                            FormBadge(outcome: outcome)
                        }
                    }
                    Spacer()
                    HStack(spacing: 3) {
                        ForEach(awayStats.currentForm.reversed(), id: \.self) { outcome in
                            FormBadge(outcome: outcome)
                        }
                    }
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(15)
        .shadow(color: Color.black.opacity(0.05), radius: 5)
    }

    // MARK: - Actual Result

    private func actualResultView(result: MatchResult) -> some View {
        VStack(spacing: 10) {
            Text("Endergebnis")
                .font(.headline)
            Text("\(result.homeScore):\(result.awayScore)")
                .font(.system(size: 36, weight: .bold))
            Text("Gewinner: \(winnerText(result.winner))")
                .font(.subheadline)
                .foregroundColor(.secondary)

            // Check if prediction was correct
            if let prediction = event.prediction {
                Divider()
                    .padding(.vertical, 5)
                HStack {
                    Image(systemName: prediction.predictedWinner == result.winner ? "checkmark.circle.fill" : "xmark.circle.fill")
                        .foregroundColor(prediction.predictedWinner == result.winner ? .green : .red)
                    Text(prediction.predictedWinner == result.winner ? "Vorhersage korrekt!" : "Vorhersage inkorrekt")
                        .font(.subheadline)
                }
            }
        }
        .padding()
        .background(Color(.systemGray6))
        .cornerRadius(15)
    }

    // MARK: - Helper Methods

    private func winnerText(_ winner: Winner) -> String {
        switch winner {
        case .home:
            return event.homeTeam.name
        case .away:
            return event.awayTeam.name
        case .draw:
            return "Unentschieden"
        }
    }

    private func confidenceColor(_ confidence: Double) -> Color {
        if confidence > 0.7 { return .green }
        else if confidence > 0.5 { return .orange }
        else { return .red }
    }

    private func refreshPrediction() {
        isRefreshing = true
        Task {
            await viewModel.refreshPrediction(for: event)
            isRefreshing = false
        }
    }
}

// MARK: - Supporting Views

struct InfoRow: View {
    let icon: String
    let title: String
    let value: String

    var body: some View {
        HStack {
            Image(systemName: icon)
                .foregroundColor(.blue)
                .frame(width: 25)
            Text(title)
                .font(.subheadline)
                .foregroundColor(.secondary)
            Spacer()
            Text(value)
                .font(.subheadline)
        }
    }
}

struct ProbabilityBar: View {
    let label: String
    let probability: Double
    let color: Color

    var body: some View {
        VStack(alignment: .leading, spacing: 5) {
            HStack {
                Text(label)
                    .font(.caption)
                Spacer()
                Text(String(format: "%.1f%%", probability * 100))
                    .font(.caption)
                    .bold()
            }
            GeometryReader { geometry in
                ZStack(alignment: .leading) {
                    Rectangle()
                        .fill(Color.gray.opacity(0.2))
                        .frame(height: 8)
                        .cornerRadius(4)

                    Rectangle()
                        .fill(color)
                        .frame(width: geometry.size.width * CGFloat(probability), height: 8)
                        .cornerRadius(4)
                }
            }
            .frame(height: 8)
        }
    }
}

struct FactorRow: View {
    let factor: PredictionFactor

    var body: some View {
        HStack(alignment: .top, spacing: 10) {
            Image(systemName: "chart.bar.fill")
                .foregroundColor(impactColor)
                .font(.caption)

            VStack(alignment: .leading, spacing: 3) {
                Text(factor.name)
                    .font(.subheadline)
                    .bold()
                Text(factor.description)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }

            Spacer()

            Text(factor.impact.rawValue)
                .font(.caption2)
                .padding(.horizontal, 8)
                .padding(.vertical, 4)
                .background(impactColor.opacity(0.2))
                .cornerRadius(8)
        }
        .padding(.vertical, 5)
    }

    private var impactColor: Color {
        switch factor.impact {
        case .high: return .red
        case .medium: return .orange
        case .low: return .green
        }
    }
}

struct StatComparisonRow: View {
    let label: String
    let homeValue: String
    let awayValue: String
    var homeHighlight: Bool = false
    var awayHighlight: Bool = false

    var body: some View {
        HStack {
            Text(label)
                .font(.subheadline)
                .frame(width: 100, alignment: .leading)
            Spacer()
            Text(homeValue)
                .font(.subheadline)
                .bold(homeHighlight)
                .foregroundColor(homeHighlight ? .green : .primary)
            Spacer()
            Text(awayValue)
                .font(.subheadline)
                .bold(awayHighlight)
                .foregroundColor(awayHighlight ? .green : .primary)
        }
    }
}

struct FormBadge: View {
    let outcome: MatchOutcome

    var body: some View {
        Text(outcome.rawValue)
            .font(.system(size: 10, weight: .bold))
            .foregroundColor(.white)
            .frame(width: 20, height: 20)
            .background(backgroundColor)
            .cornerRadius(10)
    }

    private var backgroundColor: Color {
        switch outcome {
        case .win: return .green
        case .draw: return .blue
        case .loss: return .red
        }
    }
}
