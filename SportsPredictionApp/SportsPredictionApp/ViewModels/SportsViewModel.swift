//
//  SportsViewModel.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import Foundation
import Combine

@MainActor
class SportsViewModel: ObservableObject {
    @Published var events: [SportEvent] = []
    @Published var filteredEvents: [SportEvent] = []
    @Published var selectedSport: SportType?
    @Published var isLoading = false
    @Published var errorMessage: String?

    private let sportsService: SportsService
    private let predictionService: PredictionService
    private var cancellables = Set<AnyCancellable>()

    init(sportsService: SportsService = SportsService(),
         predictionService: PredictionService = PredictionService()) {
        self.sportsService = sportsService
        self.predictionService = predictionService
        loadEvents()
    }

    // MARK: - Public Methods

    func loadEvents() {
        isLoading = true
        errorMessage = nil

        Task {
            do {
                events = try await sportsService.fetchUpcomingEvents()
                applyFilters()

                // Generate predictions for events without predictions
                for (index, event) in events.enumerated() where event.prediction == nil {
                    if let prediction = await predictionService.generatePrediction(for: event) {
                        events[index].prediction = prediction
                    }
                }

                isLoading = false
            } catch {
                errorMessage = "Fehler beim Laden der Events: \(error.localizedDescription)"
                isLoading = false
            }
        }
    }

    func refreshPrediction(for event: SportEvent) async {
        guard let index = events.firstIndex(where: { $0.id == event.id }) else { return }

        if let prediction = await predictionService.generatePrediction(for: event) {
            events[index].prediction = prediction
        }
    }

    func filterBySport(_ sport: SportType?) {
        selectedSport = sport
        applyFilters()
    }

    func upcomingEvents() -> [SportEvent] {
        return filteredEvents.filter { $0.status == .upcoming }
            .sorted { $0.dateTime < $1.dateTime }
    }

    func liveEvents() -> [SportEvent] {
        return filteredEvents.filter { $0.status == .live }
    }

    func finishedEvents() -> [SportEvent] {
        return filteredEvents.filter { $0.status == .finished }
            .sorted { $0.dateTime > $1.dateTime }
    }

    // MARK: - Private Methods

    private func applyFilters() {
        if let sport = selectedSport {
            filteredEvents = events.filter { $0.sport == sport }
        } else {
            filteredEvents = events
        }
    }

    // MARK: - Analytics

    func predictionAccuracy() -> Double {
        let finishedEventsWithPredictions = events.filter {
            $0.status == .finished && $0.prediction != nil && $0.actualResult != nil
        }

        guard !finishedEventsWithPredictions.isEmpty else { return 0 }

        let correctPredictions = finishedEventsWithPredictions.filter { event in
            guard let prediction = event.prediction,
                  let result = event.actualResult else { return false }
            return prediction.predictedWinner == result.winner
        }

        return Double(correctPredictions.count) / Double(finishedEventsWithPredictions.count) * 100
    }
}
