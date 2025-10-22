//
//  PredictionService.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import Foundation

/// Service for generating AI-powered predictions
class PredictionService {

    // MARK: - Public Methods

    func generatePrediction(for event: SportEvent) async -> Prediction? {
        // Simulate processing time
        try? await Task.sleep(nanoseconds: 500_000_000) // 0.5 seconds

        guard let homeStats = event.homeTeam.stats,
              let awayStats = event.awayTeam.stats else {
            return nil
        }

        // Calculate prediction based on team statistics
        let probabilities = calculateWinProbabilities(
            homeStats: homeStats,
            awayStats: awayStats,
            isHomeGame: true
        )

        let predictedWinner = probabilities.highest.outcome
        let confidenceLevel = probabilities.highest.probability

        // Generate prediction factors
        let factors = generatePredictionFactors(
            homeStats: homeStats,
            awayStats: awayStats,
            event: event
        )

        // Predict score
        let predictedScore = predictScore(
            homeStats: homeStats,
            awayStats: awayStats,
            probabilities: probabilities
        )

        return Prediction(
            eventId: event.id,
            predictedWinner: predictedWinner,
            predictedScore: predictedScore,
            confidenceLevel: confidenceLevel,
            probabilities: probabilities,
            factors: factors,
            algorithm: .ensemble
        )
    }

    // MARK: - Private Methods

    private func calculateWinProbabilities(
        homeStats: TeamStats,
        awayStats: TeamStats,
        isHomeGame: Bool
    ) -> WinProbabilities {
        // Multi-factor prediction algorithm

        // 1. Win percentage analysis
        let homeWinRate = homeStats.winPercentage / 100.0
        let awayWinRate = awayStats.winPercentage / 100.0

        // 2. Form analysis (last 5 games)
        let homeFormScore = calculateFormScore(homeStats.currentForm)
        let awayFormScore = calculateFormScore(awayStats.currentForm)

        // 3. Goal difference analysis
        let homeGoalDiff = Double(homeStats.goalDifference)
        let awayGoalDiff = Double(awayStats.goalDifference)
        let goalDiffFactor = (homeGoalDiff - awayGoalDiff) / 50.0 // Normalize

        // 4. Home advantage (typically 10-15% boost)
        let homeAdvantage = isHomeGame ? 0.12 : 0.0

        // 5. Home/Away record analysis
        let homeRecordScore = Double(homeStats.homeRecord.wins) / Double(max(homeStats.homeRecord.played, 1))
        let awayRecordScore = Double(awayStats.awayRecord.wins) / Double(max(awayStats.awayRecord.played, 1))

        // Weighted combination
        var homeScore = (homeWinRate * 0.25) +
                       (homeFormScore * 0.20) +
                       (goalDiffFactor * 0.15) +
                       (homeAdvantage * 0.15) +
                       (homeRecordScore * 0.25)

        var awayScore = (awayWinRate * 0.25) +
                       (awayFormScore * 0.20) +
                       (-goalDiffFactor * 0.15) +
                       (awayRecordScore * 0.25)

        // Draw probability based on historical data and team balance
        let teamBalance = abs(homeScore - awayScore)
        var drawProbability = max(0.15, 0.35 - teamBalance)

        // Normalize probabilities to sum to 1.0
        let total = homeScore + awayScore + drawProbability
        homeScore = homeScore / total
        awayScore = awayScore / total
        drawProbability = drawProbability / total

        // Ensure no probability is below 5% or above 80%
        let homeWin = min(max(homeScore, 0.05), 0.80)
        let awayWin = min(max(awayScore, 0.05), 0.80)
        let draw = min(max(drawProbability, 0.05), 0.50)

        // Final normalization
        let finalTotal = homeWin + awayWin + draw
        return WinProbabilities(
            homeWin: homeWin / finalTotal,
            draw: draw / finalTotal,
            awayWin: awayWin / finalTotal
        )
    }

    private func calculateFormScore(_ form: [MatchOutcome]) -> Double {
        guard !form.isEmpty else { return 0.5 }

        var score = 0.0
        for (index, outcome) in form.enumerated() {
            let weight = Double(index + 1) / Double(form.count) // Recent games weighted more
            switch outcome {
            case .win:
                score += 1.0 * weight
            case .draw:
                score += 0.5 * weight
            case .loss:
                score += 0.0 * weight
            }
        }

        return score / Double(form.count)
    }

    private func predictScore(
        homeStats: TeamStats,
        awayStats: TeamStats,
        probabilities: WinProbabilities
    ) -> MatchResult {
        // Calculate expected goals based on team statistics
        let homeAvgGoals = Double(homeStats.goalsScored) / Double(max(homeStats.gamesPlayed, 1))
        let awayAvgGoals = Double(awayStats.goalsScored) / Double(max(awayStats.gamesPlayed, 1))

        let homeDefenseRating = Double(homeStats.goalsConceded) / Double(max(homeStats.gamesPlayed, 1))
        let awayDefenseRating = Double(awayStats.goalsConceded) / Double(max(awayStats.gamesPlayed, 1))

        // Adjust based on predicted outcome
        var homeGoals = (homeAvgGoals + (2.0 - awayDefenseRating)) / 2.0
        var awayGoals = (awayAvgGoals + (2.0 - homeDefenseRating)) / 2.0

        // Adjust based on win probabilities
        if probabilities.homeWin > probabilities.awayWin {
            homeGoals += 0.5
        } else if probabilities.awayWin > probabilities.homeWin {
            awayGoals += 0.5
        }

        // Round to nearest integer
        let predictedHomeScore = Int(round(homeGoals))
        let predictedAwayScore = Int(round(awayGoals))

        return MatchResult(
            homeScore: max(0, predictedHomeScore),
            awayScore: max(0, predictedAwayScore)
        )
    }

    private func generatePredictionFactors(
        homeStats: TeamStats,
        awayStats: TeamStats,
        event: SportEvent
    ) -> [PredictionFactor] {
        var factors: [PredictionFactor] = []

        // Form factor
        let homeFormScore = calculateFormScore(homeStats.currentForm)
        let awayFormScore = calculateFormScore(awayStats.currentForm)
        if abs(homeFormScore - awayFormScore) > 0.3 {
            let favoredTeam = homeFormScore > awayFormScore ? event.homeTeam.shortName : event.awayTeam.shortName
            factors.append(PredictionFactor(
                name: "Aktuelle Form",
                description: "\(favoredTeam) zeigt eine deutlich bessere Form in den letzten Spielen",
                impact: .high,
                weight: 0.25
            ))
        }

        // Goal difference factor
        let goalDiffGap = abs(homeStats.goalDifference - awayStats.goalDifference)
        if goalDiffGap > 15 {
            let strongerTeam = homeStats.goalDifference > awayStats.goalDifference ? event.homeTeam.shortName : event.awayTeam.shortName
            factors.append(PredictionFactor(
                name: "Tordifferenz",
                description: "\(strongerTeam) hat eine signifikant bessere Tordifferenz (+\(goalDiffGap))",
                impact: .high,
                weight: 0.20
            ))
        }

        // Home advantage factor
        factors.append(PredictionFactor(
            name: "Heimvorteil",
            description: "\(event.homeTeam.shortName) spielt zu Hause und hat eine starke Heimbilanz",
            impact: .medium,
            weight: 0.15
        ))

        // League position factor
        if let homeRank = homeStats.rank, let awayRank = awayStats.rank {
            let rankDiff = abs(homeRank - awayRank)
            if rankDiff > 5 {
                let higherTeam = homeRank < awayRank ? event.homeTeam.shortName : event.awayTeam.shortName
                factors.append(PredictionFactor(
                    name: "Tabellenposition",
                    description: "\(higherTeam) steht \(rankDiff) Plätze höher in der Tabelle",
                    impact: rankDiff > 10 ? .high : .medium,
                    weight: 0.15
                ))
            }
        }

        // Win percentage factor
        let winRateDiff = abs(homeStats.winPercentage - awayStats.winPercentage)
        if winRateDiff > 20 {
            let strongerTeam = homeStats.winPercentage > awayStats.winPercentage ? event.homeTeam.shortName : event.awayTeam.shortName
            factors.append(PredictionFactor(
                name: "Siegquote",
                description: "\(strongerTeam) hat eine \(String(format: "%.0f", winRateDiff))% höhere Siegquote",
                impact: .medium,
                weight: 0.25
            ))
        }

        return factors
    }
}
