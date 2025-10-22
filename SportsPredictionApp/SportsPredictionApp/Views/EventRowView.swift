//
//  EventRowView.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import SwiftUI

struct EventRowView: View {
    let event: SportEvent

    var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 15) {
                // Sport icon
                Text(event.sport.icon)
                    .font(.system(size: 30))

                VStack(alignment: .leading, spacing: 8) {
                    // League and date
                    HStack {
                        Text(event.league)
                            .font(.caption)
                            .foregroundColor(.secondary)

                        Spacer()

                        Text(event.dateTime, style: .date)
                            .font(.caption)
                            .foregroundColor(.secondary)
                        Text(event.dateTime, style: .time)
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }

                    // Teams
                    HStack(spacing: 10) {
                        VStack(alignment: .leading, spacing: 4) {
                            Text(event.homeTeam.shortName)
                                .font(.headline)
                                .foregroundColor(.primary)
                            Text(event.awayTeam.shortName)
                                .font(.headline)
                                .foregroundColor(.primary)
                        }

                        Spacer()

                        // Score or prediction
                        if let result = event.actualResult {
                            VStack(spacing: 4) {
                                Text("\(result.homeScore)")
                                    .font(.headline)
                                    .bold()
                                Text("\(result.awayScore)")
                                    .font(.headline)
                                    .bold()
                            }
                        } else if let prediction = event.prediction {
                            VStack(spacing: 4) {
                                predictionBadge(prediction: prediction)
                            }
                        }
                    }

                    // Prediction confidence
                    if let prediction = event.prediction, event.actualResult == nil {
                        HStack {
                            Text("Vorhersage:")
                                .font(.caption2)
                                .foregroundColor(.secondary)
                            Text(prediction.confidencePercentage)
                                .font(.caption2)
                                .bold()
                                .foregroundColor(confidenceColor(prediction.confidenceLevel))
                            Spacer()
                            if let score = prediction.predictedScore {
                                Text("\(score.homeScore):\(score.awayScore)")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                        }
                    }

                    // Status badge
                    statusBadge
                }
            }
            .padding()
        }
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(color: Color.black.opacity(0.1), radius: 3, x: 0, y: 2)
        .padding(.horizontal)
        .padding(.vertical, 5)
    }

    private func predictionBadge(prediction: Prediction) -> some View {
        let winnerText: String
        let winnerColor: Color

        switch prediction.predictedWinner {
        case .home:
            winnerText = "H"
            winnerColor = .green
        case .away:
            winnerText = "A"
            winnerColor = .orange
        case .draw:
            winnerText = "U"
            winnerColor = .blue
        }

        return Text(winnerText)
            .font(.caption)
            .bold()
            .foregroundColor(.white)
            .frame(width: 30, height: 30)
            .background(winnerColor)
            .cornerRadius(15)
    }

    private var statusBadge: some View {
        HStack {
            Circle()
                .fill(statusColor)
                .frame(width: 8, height: 8)
            Text(event.status.rawValue)
                .font(.caption2)
                .foregroundColor(.secondary)
            Spacer()
        }
    }

    private var statusColor: Color {
        switch event.status {
        case .live:
            return .red
        case .upcoming:
            return .green
        case .finished:
            return .gray
        case .postponed, .cancelled:
            return .orange
        }
    }

    private func confidenceColor(_ confidence: Double) -> Color {
        if confidence > 0.7 {
            return .green
        } else if confidence > 0.5 {
            return .orange
        } else {
            return .red
        }
    }
}
