//
//  SportsPredictionApp.swift
//  SportsPredictionApp
//
//  Created by Claude
//  Copyright © 2025. All rights reserved.
//

import SwiftUI

@main
struct SportsPredictionApp: App {
    @StateObject private var sportsViewModel = SportsViewModel()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(sportsViewModel)
        }
    }
}
