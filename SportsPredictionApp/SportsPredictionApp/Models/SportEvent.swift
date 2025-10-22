//
//  SportEvent.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import Foundation

/// Represents a sports event/match
struct SportEvent: Identifiable, Codable {
    let id: UUID
    let homeTeam: Team
    let awayTeam: Team
    let sport: SportType
    let league: String
    let dateTime: Date
    let venue: String
    var status: EventStatus
    var actualResult: MatchResult?
    var prediction: Prediction?

    init(
        id: UUID = UUID(),
        homeTeam: Team,
        awayTeam: Team,
        sport: SportType,
        league: String,
        dateTime: Date,
        venue: String,
        status: EventStatus = .upcoming,
        actualResult: MatchResult? = nil,
        prediction: Prediction? = nil
    ) {
        self.id = id
        self.homeTeam = homeTeam
        self.awayTeam = awayTeam
        self.sport = sport
        self.league = league
        self.dateTime = dateTime
        self.venue = venue
        self.status = status
        self.actualResult = actualResult
        self.prediction = prediction
    }
}

/// Types of sports supported
enum SportType: String, Codable, CaseIterable {
    case football = "Fußball"
    case basketball = "Basketball"
    case tennis = "Tennis"
    case hockey = "Eishockey"
    case handball = "Handball"

    var icon: String {
        switch self {
        case .football: return "⚽️"
        case .basketball: return "🏀"
        case .tennis: return "🎾"
        case .hockey: return "🏒"
        case .handball: return "🤾"
        }
    }
}

/// Status of the event
enum EventStatus: String, Codable {
    case upcoming = "Anstehend"
    case live = "Live"
    case finished = "Beendet"
    case postponed = "Verschoben"
    case cancelled = "Abgesagt"
}

/// Match result
struct MatchResult: Codable, Equatable {
    let homeScore: Int
    let awayScore: Int

    var winner: Winner {
        if homeScore > awayScore { return .home }
        if awayScore > homeScore { return .away }
        return .draw
    }
}

enum Winner: String, Codable {
    case home = "Heim"
    case away = "Auswärts"
    case draw = "Unentschieden"
}
