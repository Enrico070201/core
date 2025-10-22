//
//  ContentView.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import SwiftUI

struct ContentView: View {
    @EnvironmentObject var viewModel: SportsViewModel
    @State private var selectedTab = 0

    var body: some View {
        TabView(selection: $selectedTab) {
            EventsListView()
                .tabItem {
                    Label("Events", systemImage: "calendar")
                }
                .tag(0)

            StatisticsView()
                .tabItem {
                    Label("Statistiken", systemImage: "chart.bar.fill")
                }
                .tag(1)

            SettingsView()
                .tabItem {
                    Label("Einstellungen", systemImage: "gear")
                }
                .tag(2)
        }
        .accentColor(.blue)
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
            .environmentObject(SportsViewModel())
    }
}
