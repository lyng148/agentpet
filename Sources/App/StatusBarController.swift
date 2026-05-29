import AppKit
import SwiftUI

/// Owns the menu bar status item and a non-activating panel that drops down
/// beneath it. Using a panel (not NSPopover) means showing it never activates
/// the app, so the user's current window keeps keyboard focus.
@MainActor
final class StatusBarController: NSObject {
    static let shared = StatusBarController()

    private var statusItem: NSStatusItem?
    private var panel: NSPanel?
    private var outsideClickMonitor: Any?

    func start() {
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        item.button?.image = NSImage(systemSymbolName: "pawprint.fill", accessibilityDescription: "AgentPet")
        item.button?.target = self
        item.button?.action = #selector(toggle)
        statusItem = item
    }

    @objc private func toggle() {
        if panel != nil { close() } else { open() }
    }

    private func open() {
        guard let button = statusItem?.button, let buttonWindow = button.window else { return }

        let width: CGFloat = 300
        let buttonFrame = buttonWindow.convertToScreen(button.convert(button.bounds, to: nil))
        let screen = buttonWindow.screen ?? NSScreen.main
        let iconCenterX = buttonFrame.midX

        // Place the panel under the icon, clamped to the screen, and point the
        // arrow at the icon center.
        var originX = iconCenterX - width / 2
        if let visible = screen?.visibleFrame {
            originX = min(max(originX, visible.minX + 8), visible.maxX - width - 8)
        }
        let arrowOffset = max(-width / 2 + 18, min(width / 2 - 18, iconCenterX - (originX + width / 2)))

        let hosting = NSHostingView(rootView: MenuContentView(
            dismiss: { [weak self] in self?.close() },
            arrowOffset: arrowOffset
        ))
        hosting.setFrameSize(NSSize(width: width, height: 500))
        let size = hosting.fittingSize

        let panel = NSPanel(
            contentRect: NSRect(origin: .zero, size: size),
            styleMask: [.borderless, .nonactivatingPanel],
            backing: .buffered, defer: false
        )
        panel.level = .popUpMenu
        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.hasShadow = true
        panel.becomesKeyOnlyIfNeeded = true
        panel.appearance = NSAppearance(named: .darkAqua)
        panel.contentView = hosting

        panel.setFrameOrigin(NSPoint(x: originX, y: buttonFrame.minY - size.height - 2))
        panel.orderFrontRegardless()
        self.panel = panel

        outsideClickMonitor = NSEvent.addGlobalMonitorForEvents(matching: [.leftMouseDown, .rightMouseDown]) { [weak self] _ in
            self?.close()
        }
    }

    private func close() {
        if let outsideClickMonitor {
            NSEvent.removeMonitor(outsideClickMonitor)
            self.outsideClickMonitor = nil
        }
        panel?.orderOut(nil)
        panel = nil
    }
}
