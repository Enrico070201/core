//
//  DateFormatters.swift
//  SportsPredictionApp
//
//  Created by Claude
//

import Foundation

extension DateFormatter {
    static let eventDate: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .none
        formatter.locale = Locale(identifier: "de_DE")
        return formatter
    }()

    static let eventTime: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateStyle = .none
        formatter.timeStyle = .short
        formatter.locale = Locale(identifier: "de_DE")
        return formatter
    }()

    static let eventDateTime: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        formatter.locale = Locale(identifier: "de_DE")
        return formatter
    }()
}

extension Date {
    var isToday: Bool {
        Calendar.current.isDateInToday(self)
    }

    var isTomorrow: Bool {
        Calendar.current.isDateInTomorrow(self)
    }

    var isInPast: Bool {
        self < Date()
    }

    var relativeString: String {
        if isToday {
            return "Heute"
        } else if isTomorrow {
            return "Morgen"
        } else {
            return DateFormatter.eventDate.string(from: self)
        }
    }
}
