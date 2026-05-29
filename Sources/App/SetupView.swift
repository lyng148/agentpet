import SwiftUI
import AgentPetCore

/// Onboarding / Settings content: grant notifications and choose which agents
/// to wire up.
struct SetupView: View {
    @ObservedObject private var model = SettingsModel.shared
    @ObservedObject private var pet = PetController.shared
    @ObservedObject private var imagePets = ImagePetStore.shared
    var onClose: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            VStack(alignment: .leading, spacing: 4) {
                Text("AgentPet")
                    .font(.title2.bold())
                Text("Get notified and watch your pet react as your AI agents run.")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            GroupBox {
                HStack {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Notifications").font(.headline)
                        Text(notificationDescription)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    Spacer()
                    notificationButton
                }
                .padding(4)
            }

            GroupBox {
                VStack(alignment: .leading, spacing: 10) {
                    Text("Agent integrations").font(.headline)
                    ForEach(model.agents) { agent in
                        AgentRow(agent: agent,
                                 installed: model.isInstalled(agent.kind),
                                 toggle: { model.toggleInstall(agent.kind) })
                        if agent.id != model.agents.last?.id { Divider() }
                    }
                }
                .padding(4)
            }

            GroupBox {
                VStack(alignment: .leading, spacing: 10) {
                    HStack {
                        Text("Pet").font(.headline)
                        Spacer()
                        Button("Import...") { model.importPet() }
                    }
                    LazyVGrid(columns: [GridItem(.adaptive(minimum: 88), spacing: 12)], spacing: 12) {
                        ForEach(PetKind.allCases) { kind in
                            PetCard(selection: .builtin(kind),
                                    title: kind.displayName,
                                    selected: pet.selection == .builtin(kind),
                                    select: { pet.selection = .builtin(kind) })
                        }
                        ForEach(imagePets.packs) { pack in
                            PetCard(selection: .imported(pack.id),
                                    title: pack.displayName,
                                    selected: pet.selection == .imported(pack.id),
                                    select: { pet.selection = .imported(pack.id) })
                        }
                    }

                    if case .imported(let id) = pet.selection, let pack = imagePets.pack(id: id) {
                        Divider()
                        Text("Animations").font(.subheadline.weight(.semibold))
                        Text("Pick which animation plays for each state.")
                            .font(.caption).foregroundStyle(.secondary)
                        ForEach(PetMood.allCases, id: \.self) { mood in
                            AnimationBindingRow(pack: pack, mood: mood)
                        }
                    }
                }
                .padding(4)
            }

            HStack {
                Spacer()
                Button("Done") { onClose() }
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(20)
        .frame(width: 440)
        .onAppear { model.refresh() }
    }

    private var notificationDescription: String {
        switch model.notificationState {
        case .unavailable: return "Available once installed as AgentPet.app"
        case .notDetermined: return "Allow AgentPet to notify you when an agent finishes or needs input"
        case .enabled: return "Enabled"
        case .denied: return "Denied. Enable in System Settings to get alerts"
        }
    }

    @ViewBuilder private var notificationButton: some View {
        switch model.notificationState {
        case .enabled:
            Label("Enabled", systemImage: "checkmark.circle.fill")
                .foregroundStyle(.green)
        case .denied:
            Button("Open Settings") { model.openSystemNotificationSettings() }
        case .notDetermined:
            Button("Enable") { model.enableNotifications() }
        case .unavailable:
            Text("Unavailable").foregroundStyle(.secondary)
        }
    }
}

private struct AnimationBindingRow: View {
    let pack: ImagePetPack
    let mood: PetMood
    @ObservedObject private var store = PetBindingsStore.shared

    var body: some View {
        let current = store.clipIndex(packId: pack.id, clipCount: pack.clipCount, mood: mood)
        HStack(spacing: 10) {
            Text(label)
                .font(.caption)
                .frame(width: 72, alignment: .leading)
            ImageSpriteView(frames: pack.clip(current), mood: .idle, size: 38)
                .frame(width: 38, height: 38)
            Spacer()
            Picker("", selection: Binding(
                get: { current },
                set: { store.setClip($0, mood: mood, packId: pack.id, clipCount: pack.clipCount) }
            )) {
                ForEach(0..<pack.clipCount, id: \.self) { i in
                    Text("Clip \(i + 1)").tag(i)
                }
            }
            .labelsHidden()
            .frame(width: 110)
        }
    }

    private var label: String {
        switch mood {
        case .idle: return "Idle"
        case .working: return "Working"
        case .waiting: return "Waiting"
        case .done: return "Done"
        case .celebrate: return "Celebrate"
        }
    }
}

private struct PetCard: View {
    let selection: PetSelection
    let title: String
    let selected: Bool
    let select: () -> Void

    var body: some View {
        Button(action: select) {
            VStack(spacing: 4) {
                preview.frame(width: 64, height: 56)
                Text(title).font(.caption).lineLimit(1)
            }
            .frame(width: 88, height: 92)
            .background(
                RoundedRectangle(cornerRadius: 12)
                    .fill(selected ? Color.accentColor.opacity(0.16) : Color.secondary.opacity(0.08))
            )
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .strokeBorder(selected ? Color.accentColor : .clear, lineWidth: 2)
            )
        }
        .buttonStyle(.plain)
    }

    @ViewBuilder private var preview: some View {
        switch selection {
        case .builtin(let kind):
            PetSpriteView(kind: kind, mood: .idle, size: 64)
        case .imported(let id):
            if let pack = ImagePetStore.shared.pack(id: id) {
                ImageSpriteView(frames: pack.clip(0), mood: .idle, size: 64)
            } else {
                Image(systemName: "pawprint")
            }
        }
    }
}

private struct AgentRow: View {
    let agent: AgentIntegration
    let installed: Bool
    let toggle: () -> Void

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text(agent.displayName)
                if let note = agent.note {
                    Text(note).font(.caption).foregroundStyle(.secondary)
                } else if installed {
                    Text("Hook installed").font(.caption).foregroundStyle(.green)
                }
            }
            Spacer()
            if agent.isSupported {
                Button(installed ? "Remove" : "Install") { toggle() }
            } else {
                Text("Coming soon")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
    }
}
