//
//  StatisticsView.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import SwiftUI

struct StatisticsView: View {
    @EnvironmentObject var viewModel: SportsViewModel

    var body: some View {
        NavigationView {
            ScrollView {
                VStack(spacing: 20) {
                    // Overall accuracy card
                    accuracyCard

                    // Predictions by sport
                    predictionsBySportCard

                    // Recent predictions
                    recentPredictionsCard
                }
                .padding()
            }
            .navigationTitle("Statistiken")
        }
    }

    private var accuracyCard: some View {
        VStack(spacing: 15) {
            Text("Vorhersage-Genauigkeit")
                .font(.headline)

            ZStack {
                Circle()
                    .stroke(Color.gray.opacity(0.2), lineWidth: 15)
                    .frame(width: 150, height: 150)

                Circle()
                    .trim(from: 0, to: CGFloat(viewModel.predictionAccuracy() / 100))
                    .stroke(
                        LinearGradient(
                            colors: [.green, .blue],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        style: StrokeStyle(lineWidth: 15, lineCap: .round)
                    )
                    .frame(width: 150, height: 150)
                    .rotationEffect(.degrees(-90))

                VStack(spacing: 5) {
                    Text(String(format: "%.1f%%", viewModel.predictionAccuracy()))
                        .font(.system(size: 32, weight: .bold))
                    Text("Genauigkeit")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }

            Text("Basierend auf \(viewModel.events.filter { $0.status == .finished && $0.prediction != nil }.count) abgeschlossenen Spielen")
                .font(.caption)
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
        }
        .padding()
        .frame(maxWidth: .infinity)
        .background(Color(.systemBackground))
        .cornerRadius(15)
        .shadow(color: Color.black.opacity(0.05), radius: 5)
    }

    private var predictionsBySportCard: some View {
        VStack(alignment: .leading, spacing: 15) {
            Text("Vorhersagen nach Sportart")
                .font(.headline)

            ForEach(SportType.allCases, id: \.self) { sport in
                let sportEvents = viewModel.events.filter { $0.sport == sport }
                if !sportEvents.isEmpty {
                    HStack {
                        Text(sport.icon)
                            .font(.title2)
                        Text(sport.rawValue)
                            .font(.subheadline)
                        Spacer()
                        Text("\(sportEvents.count)")
                            .font(.subheadline)
                            .bold()
                    }
                    .padding(.vertical, 5)
                }
            }
        }
        .padding()
        .frame(maxWidth: .infinity)
        .background(Color(.systemBackground))
        .cornerRadius(15)
        .shadow(color: Color.black.opacity(0.05), radius: 5)
    }

    private var recentPredictionsCard: some View {
        VStack(alignment: .leading, spacing: 15) {
            Text("Letzte Vorhersagen")
                .font(.headline)

            let recentFinished = viewModel.finishedEvents().prefix(5)
            if recentFinished.isEmpty {
                Text("Noch keine abgeschlossenen Vorhersagen")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                    .frame(maxWidth: .infinity, alignment: .center)
                    .padding()
            } else {
                ForEach(Array(recentFinished)) { event in
                    recentPredictionRow(event: event)
                }
            }
        }
        .padding()
        .frame(maxWidth: .infinity)
        .background(Color(.systemBackground))
        .cornerRadius(15)
        .shadow(color: Color.black.opacity(0.05), radius: 5)
    }

    private func recentPredictionRow(event: SportEvent) -> some View {
        HStack(spacing: 10) {
            // Result indicator
            if let prediction = event.prediction,
               let result = event.actualResult {
                Image(systemName: prediction.predictedWinner == result.winner ? "checkmark.circle.fill" : "xmark.circle.fill")
                    .foregroundColor(prediction.predictedWinner == result.winner ? .green : .red)
                    .font(.title3)
            }

            VStack(alignment: .leading, spacing: 4) {
                Text("\(event.homeTeam.shortName) vs \(event.awayTeam.shortName)")
                    .font(.subheadline)
                    .bold()
                if let result = event.actualResult {
                    Text("\(result.homeScore):\(result.awayScore)")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }

            Spacer()

            if let prediction = event.prediction {
                Text(prediction.confidencePercentage)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
        .padding(.vertical, 8)
    }
}

struct StatisticsView_Previews: PreviewProvider {
    static var previews: some View {
        StatisticsView()
            .environmentObject(SportsViewModel())
    }
}
