//
//  Prediction.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import Foundation

/// Represents an AI prediction for a sports event
struct Prediction: Codable, Identifiable {
    let id: UUID
    let eventId: UUID
    let predictedWinner: Winner
    let predictedScore: MatchResult?
    let confidenceLevel: Double // 0.0 to 1.0
    let probabilities: WinProbabilities
    let factors: [PredictionFactor]
    let timestamp: Date
    let algorithm: PredictionAlgorithm

    init(
        id: UUID = UUID(),
        eventId: UUID,
        predictedWinner: Winner,
        predictedScore: MatchResult? = nil,
        confidenceLevel: Double,
        probabilities: WinProbabilities,
        factors: [PredictionFactor],
        timestamp: Date = Date(),
        algorithm: PredictionAlgorithm = .neuralNetwork
    ) {
        self.id = id
        self.eventId = eventId
        self.predictedWinner = predictedWinner
        self.predictedScore = predictedScore
        self.confidenceLevel = confidenceLevel
        self.probabilities = probabilities
        self.factors = factors
        self.timestamp = timestamp
        self.algorithm = algorithm
    }

    var confidencePercentage: String {
        return String(format: "%.1f%%", confidenceLevel * 100)
    }
}

/// Probabilities for each outcome
struct WinProbabilities: Codable {
    let homeWin: Double
    let draw: Double
    let awayWin: Double

    var highest: (outcome: Winner, probability: Double) {
        if homeWin >= draw && homeWin >= awayWin {
            return (.home, homeWin)
        } else if awayWin >= draw {
            return (.away, awayWin)
        } else {
            return (.draw, draw)
        }
    }
}

/// Factors that influenced the prediction
struct PredictionFactor: Codable, Identifiable {
    let id: UUID
    let name: String
    let description: String
    let impact: FactorImpact
    let weight: Double

    init(
        id: UUID = UUID(),
        name: String,
        description: String,
        impact: FactorImpact,
        weight: Double
    ) {
        self.id = id
        self.name = name
        self.description = description
        self.impact = impact
        self.weight = weight
    }
}

enum FactorImpact: String, Codable {
    case high = "Hoch"
    case medium = "Mittel"
    case low = "Niedrig"

    var color: String {
        switch self {
        case .high: return "red"
        case .medium: return "orange"
        case .low: return "green"
        }
    }
}

enum PredictionAlgorithm: String, Codable {
    case neuralNetwork = "Neuronales Netzwerk"
    case randomForest = "Random Forest"
    case logisticRegression = "Logistische Regression"
    case ensemble = "Ensemble-Methode"
}
