//
//  Team.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import Foundation

/// Represents a sports team
struct Team: Identifiable, Codable, Equatable {
    let id: UUID
    let name: String
    let shortName: String
    let logo: String? // URL string to team logo
    let country: String
    var stats: TeamStats?

    init(
        id: UUID = UUID(),
        name: String,
        shortName: String,
        logo: String? = nil,
        country: String = "Deutschland",
        stats: TeamStats? = nil
    ) {
        self.id = id
        self.name = name
        self.shortName = shortName
        self.logo = logo
        self.country = country
        self.stats = stats
    }
}

/// Team statistics used for predictions
struct TeamStats: Codable, Equatable {
    let gamesPlayed: Int
    let wins: Int
    let draws: Int
    let losses: Int
    let goalsScored: Int
    let goalsConceded: Int
    let currentForm: [MatchOutcome] // Last 5 matches
    let homeRecord: Record
    let awayRecord: Record
    let rank: Int?

    var winPercentage: Double {
        guard gamesPlayed > 0 else { return 0 }
        return Double(wins) / Double(gamesPlayed) * 100
    }

    var goalDifference: Int {
        return goalsScored - goalsConceded
    }
}

struct Record: Codable, Equatable {
    let played: Int
    let wins: Int
    let draws: Int
    let losses: Int
}

enum MatchOutcome: String, Codable {
    case win = "S" // Sieg
    case draw = "U" // Unentschieden
    case loss = "N" // Niederlage
}
