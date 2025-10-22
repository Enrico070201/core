//
//  EventsListView.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import SwiftUI

struct EventsListView: View {
    @EnvironmentObject var viewModel: SportsViewModel
    @State private var selectedSportFilter: SportType?

    var body: some View {
        NavigationView {
            ZStack {
                if viewModel.isLoading {
                    ProgressView("Lade Events...")
                } else if let error = viewModel.errorMessage {
                    VStack(spacing: 20) {
                        Image(systemName: "exclamationmark.triangle")
                            .font(.system(size: 50))
                            .foregroundColor(.orange)
                        Text(error)
                            .multilineTextAlignment(.center)
                            .foregroundColor(.secondary)
                        Button("Erneut versuchen") {
                            viewModel.loadEvents()
                        }
                        .buttonStyle(.borderedProminent)
                    }
                    .padding()
                } else {
                    ScrollView {
                        VStack(spacing: 0) {
                            // Sport filter
                            sportFilterView
                                .padding(.horizontal)
                                .padding(.top, 10)

                            // Live events
                            if !viewModel.liveEvents().isEmpty {
                                sectionHeader(title: "Live", icon: "🔴")
                                ForEach(viewModel.liveEvents()) { event in
                                    NavigationLink(destination: EventDetailView(event: event)) {
                                        EventRowView(event: event)
                                    }
                                }
                            }

                            // Upcoming events
                            if !viewModel.upcomingEvents().isEmpty {
                                sectionHeader(title: "Anstehend", icon: "📅")
                                ForEach(viewModel.upcomingEvents()) { event in
                                    NavigationLink(destination: EventDetailView(event: event)) {
                                        EventRowView(event: event)
                                    }
                                }
                            }

                            // Finished events
                            if !viewModel.finishedEvents().isEmpty {
                                sectionHeader(title: "Beendet", icon: "✓")
                                ForEach(viewModel.finishedEvents().prefix(5)) { event in
                                    NavigationLink(destination: EventDetailView(event: event)) {
                                        EventRowView(event: event)
                                    }
                                }
                            }

                            if viewModel.filteredEvents.isEmpty {
                                VStack(spacing: 15) {
                                    Image(systemName: "sportscourt")
                                        .font(.system(size: 60))
                                        .foregroundColor(.gray)
                                    Text("Keine Events verfügbar")
                                        .font(.headline)
                                        .foregroundColor(.secondary)
                                }
                                .padding(.top, 50)
                            }
                        }
                    }
                }
            }
            .navigationTitle("Sports Predictions")
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button(action: {
                        viewModel.loadEvents()
                    }) {
                        Image(systemName: "arrow.clockwise")
                    }
                }
            }
        }
    }

    private var sportFilterView: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 10) {
                FilterChip(
                    title: "Alle",
                    isSelected: selectedSportFilter == nil,
                    action: {
                        selectedSportFilter = nil
                        viewModel.filterBySport(nil)
                    }
                )

                ForEach(SportType.allCases, id: \.self) { sport in
                    FilterChip(
                        title: "\(sport.icon) \(sport.rawValue)",
                        isSelected: selectedSportFilter == sport,
                        action: {
                            selectedSportFilter = sport
                            viewModel.filterBySport(sport)
                        }
                    )
                }
            }
            .padding(.vertical, 8)
        }
    }

    private func sectionHeader(title: String, icon: String) -> some View {
        HStack {
            Text("\(icon) \(title)")
                .font(.headline)
                .foregroundColor(.primary)
            Spacer()
        }
        .padding(.horizontal)
        .padding(.top, 15)
        .padding(.bottom, 5)
    }
}

struct FilterChip: View {
    let title: String
    let isSelected: Bool
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Text(title)
                .font(.subheadline)
                .fontWeight(isSelected ? .semibold : .regular)
                .padding(.horizontal, 16)
                .padding(.vertical, 8)
                .background(isSelected ? Color.blue : Color.gray.opacity(0.2))
                .foregroundColor(isSelected ? .white : .primary)
                .cornerRadius(20)
        }
    }
}

struct EventsListView_Previews: PreviewProvider {
    static var previews: some View {
        EventsListView()
            .environmentObject(SportsViewModel())
    }
}
