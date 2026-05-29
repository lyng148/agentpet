import Foundation

/// One animation: a list of frames played in a loop at `fps`.
/// For `emoji` packs a frame is a glyph string; for `sprite` packs (v2) it is
/// a relative image path.
public struct PetAnimation: Codable, Equatable, Sendable {
    public var frames: [String]
    public var fps: Double

    public init(frames: [String], fps: Double) {
        self.frames = frames
        self.fps = fps
    }
}

public enum PetRenderKind: String, Codable, Sendable {
    case emoji
    case sprite
}

/// A pet pack: metadata plus one animation per `PetMood`. This is the open
/// format third parties contribute to the dex (issue #11). Built-in pets ship
/// as `Pets/<name>/manifest.json` resources and use the same format.
public struct PetPack: Codable, Equatable, Sendable, Identifiable {
    public var name: String
    public var author: String?
    public var version: Int
    public var kind: PetRenderKind
    public var states: [String: PetAnimation]

    public var id: String { name }

    public init(name: String, author: String? = nil, version: Int, kind: PetRenderKind, states: [String: PetAnimation]) {
        self.name = name
        self.author = author
        self.version = version
        self.kind = kind
        self.states = states
    }

    /// Animation for a mood, falling back to `idle`, then to a paw print so the
    /// pet is never blank even for a malformed pack.
    public func animation(for mood: PetMood) -> PetAnimation {
        states[mood.rawValue]
            ?? states[PetMood.idle.rawValue]
            ?? PetAnimation(frames: ["🐾"], fps: 1)
    }

    /// Moods missing an animation. An empty result means the pack is complete.
    public var missingMoods: [PetMood] {
        PetMood.allCases.filter { states[$0.rawValue] == nil }
    }
}

public enum PetPackLoader {
    /// Loads a single pack from its `manifest.json` URL.
    public static func load(manifestURL: URL) -> PetPack? {
        guard let data = try? Data(contentsOf: manifestURL) else { return nil }
        return try? JSONDecoder().decode(PetPack.self, from: data)
    }

    /// Loads the built-in packs bundled under `Pets/`, sorted by name.
    public static func loadBuiltins() -> [PetPack] {
        guard let root = Bundle.module.resourceURL?.appendingPathComponent("Pets") else { return [] }
        let fm = FileManager.default
        guard let dirs = try? fm.contentsOfDirectory(at: root, includingPropertiesForKeys: nil) else { return [] }
        return dirs
            .compactMap { load(manifestURL: $0.appendingPathComponent("manifest.json")) }
            .sorted { $0.name < $1.name }
    }
}
