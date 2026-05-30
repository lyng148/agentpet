import AppKit
import SwiftUI
import Combine

/// A borderless, always-on-top, draggable floating window that hosts the pet.
/// Visibility is user-toggleable; size follows the pet-size setting.
@MainActor
final class PetWindowController: ObservableObject {
    static let shared = PetWindowController()

    @Published var isVisible: Bool = true {
        didSet { applyVisibility(isVisible) }
    }

    private var panel: NSPanel?
    private var sizeCancellable: AnyCancellable?

    func start() {
        let size = PetController.shared.petSize.windowSize
        let panel = NSPanel(
            contentRect: NSRect(origin: .zero, size: size),
            styleMask: [.borderless, .nonactivatingPanel],
            backing: .buffered,
            defer: false
        )
        panel.level = .floating
        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.hasShadow = false
        panel.isMovableByWindowBackground = true
        panel.becomesKeyOnlyIfNeeded = true
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        panel.contentView = ClickThroughHostingView(rootView: FloatingPetView())
        self.panel = panel

        position(size: size)
        applyVisibility(isVisible)

        sizeCancellable = PetController.shared.$petSize.sink { [weak self] newSize in
            self?.resize(to: newSize.windowSize)
        }
    }

    private func resize(to size: CGSize) {
        panel?.setContentSize(size)
        position(size: size)
    }

    /// Anchors the panel to the bottom-right of the main screen.
    private func position(size: CGSize) {
        guard let screen = NSScreen.main else { return }
        let frame = screen.visibleFrame
        panel?.setFrameOrigin(NSPoint(x: frame.maxX - size.width - 16, y: frame.minY + 24))
    }

    private func applyVisibility(_ visible: Bool) {
        if visible {
            panel?.orderFrontRegardless()
        } else {
            panel?.orderOut(nil)
        }
    }
}
