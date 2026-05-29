import SwiftUI

/// Petdex-inspired dark navy / indigo theme for the Settings window.
enum Theme {
    static let bgTop = Color(red: 0.05, green: 0.07, blue: 0.16)
    static let bgBottom = Color(red: 0.10, green: 0.08, blue: 0.24)
    static let accent = Color(red: 0.45, green: 0.40, blue: 0.98)
    static let card = Color.white.opacity(0.05)
    static let cardStroke = Color.white.opacity(0.09)
    static let muted = Color(red: 0.62, green: 0.65, blue: 0.82)

    static var background: some View {
        LinearGradient(colors: [bgTop, bgBottom], startPoint: .topLeading, endPoint: .bottomTrailing)
            .overlay(
                RadialGradient(colors: [accent.opacity(0.20), .clear],
                               center: .topTrailing, startRadius: 8, endRadius: 520)
            )
            .ignoresSafeArea()
    }
}

/// Small uppercase tracked label, e.g. "YOUR PET".
struct EyebrowLabel: View {
    let text: String
    init(_ text: String) { self.text = text }
    var body: some View {
        Text(text.uppercased())
            .font(.system(size: 11, weight: .semibold))
            .tracking(1.6)
            .foregroundStyle(Theme.muted)
    }
}

/// Rounded translucent card container.
private struct CardModifier: ViewModifier {
    func body(content: Content) -> some View {
        content
            .padding(16)
            .background(RoundedRectangle(cornerRadius: 16, style: .continuous).fill(Theme.card))
            .overlay(RoundedRectangle(cornerRadius: 16, style: .continuous).strokeBorder(Theme.cardStroke, lineWidth: 1))
    }
}

extension View {
    func themedCard() -> some View { modifier(CardModifier()) }
}
